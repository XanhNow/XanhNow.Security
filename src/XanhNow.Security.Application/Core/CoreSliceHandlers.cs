using System.Text.Json;
using XanhNow.Security.Application.Abstractions.Audit;
using XanhNow.Security.Application.Abstractions.ChildApps;
using XanhNow.Security.Application.Abstractions.ChildApps.AuthLogin;
using XanhNow.Security.Application.Abstractions.ChildApps.Jwt;
using XanhNow.Security.Application.Abstractions.ChildApps.Passkey;
using XanhNow.Security.Application.Abstractions.ChildApps.SmartOtp;
using XanhNow.Security.Application.Abstractions.Persistence;
using XanhNow.Security.Application.Abstractions.Time;
using XanhNow.Security.Application.Common.ChildApps;
using XanhNow.Security.Application.Common.Requests;
using XanhNow.Security.Application.Common.Results;
using XanhNow.Security.Domain.Profiles;
using XanhNow.Security.Domain.Users;

namespace XanhNow.Security.Application.Core;

internal static class CoreSliceDefaults
{
    public static readonly string[] DefaultScopes = ["security.user"];
    public const string DefaultAudience = "xanhnow";
    public const string RegistrationIncompleteReason = "registration_passkey_required";
    public const string SmartOtpRequiredReason = "smart_otp_required";
    public const string LoginSmartOtpPurpose = "login_smart_otp";
    public const string SmartOtpMfaMethod = "smart_otp";
}

public abstract class CoreSliceHandler
{
    private readonly IAuditIntentWriter _audit;
    private readonly IClock _clock;

    protected CoreSliceHandler(IAuditIntentWriter audit, IClock clock)
    {
        _audit = audit;
        _clock = clock;
    }

    protected DateTimeOffset Now => _clock.UtcNow;

    protected ValueTask AuditAsync(Guid? userId, string action, string outcome, string reasonCode, CancellationToken cancellationToken)
        => _audit.AppendAsync(new AuditIntent(userId, action, outcome, reasonCode, "rb12-core-slice", Now), cancellationToken);

    public static Result<T> ChildFailure<T>(ChildCallError error) => Result<T>.Failure(ChildAppErrorMapper.ToApplicationError(error));
}

public sealed class RegisterCommandHandler : CoreSliceHandler, IRequestHandler<RegisterCommand, RegisterResult>
{
    private readonly IAuthLoginClient _authLogin;
    private readonly ISecurityUserRepository _users;
    private readonly ILocalUnitOfWork _unitOfWork;

    public RegisterCommandHandler(IAuthLoginClient authLogin, ISecurityUserRepository users, ILocalUnitOfWork unitOfWork, IAuditIntentWriter audit, IClock clock)
        : base(audit, clock)
    {
        _authLogin = authLogin;
        _users = users;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<RegisterResult>> HandleAsync(RegisterCommand request, CancellationToken cancellationToken)
    {
        var child = await _authLogin.RegisterAsync(new AuthLoginRegisterRequest(request.PhoneNumber, new SensitiveString(request.Password), request.DeviceContext?.DeviceName ?? "unknown-device"), cancellationToken);
        if (child.IsFailure || child.Value is null)
        {
            await AuditAsync(null, "auth.register", "failed", child.Error?.Code ?? "auth_login_failed", cancellationToken);
            return ChildFailure<RegisterResult>(child.Error ?? new ChildCallError(SecurityErrorCodes.DownstreamUnavailable, "Auth Login register failed.", true));
        }

        var existing = await _users.FindByIdAsync(child.Value.UserId, cancellationToken);
        if (existing is null)
        {
            await _users.AddAsync(SecurityUser.CreatePendingPasskey(child.Value.UserId, Now), cancellationToken);
        }

        await AuditAsync(child.Value.UserId, "auth.register", "succeeded", "password_registered_passkey_required", cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return Result<RegisterResult>.Success(new RegisterResult(child.Value.UserId, "Active", UserRegistrationStatus.PendingPasskey.ToString(), Now));
    }
}

public sealed class PasswordLoginCommandHandler : CoreSliceHandler, IRequestHandler<PasswordLoginCommand, PasswordLoginResult>
{
    private readonly IAuthLoginClient _authLogin;
    private readonly IJwtTokenClient _jwt;
    private readonly ISecurityUserRepository _users;
    private readonly ISecurityProfileReader _profiles;
    private readonly ILocalUnitOfWork _unitOfWork;

    public PasswordLoginCommandHandler(IAuthLoginClient authLogin, IJwtTokenClient jwt, ISecurityUserRepository users, ISecurityProfileReader profiles, ILocalUnitOfWork unitOfWork, IAuditIntentWriter audit, IClock clock)
        : base(audit, clock)
    {
        _authLogin = authLogin;
        _jwt = jwt;
        _users = users;
        _profiles = profiles;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PasswordLoginResult>> HandleAsync(PasswordLoginCommand request, CancellationToken cancellationToken)
    {
        var login = await _authLogin.LoginWithPasswordAsync(new AuthLoginPasswordRequest(request.PhoneNumber, new SensitiveString(request.Password)), cancellationToken);
        if (login.IsFailure || login.Value is null)
        {
            await AuditAsync(null, "auth.password_login", "failed", login.Error?.Code ?? "auth_login_failed", cancellationToken);
            return ChildFailure<PasswordLoginResult>(login.Error ?? new ChildCallError(SecurityErrorCodes.DownstreamUnavailable, "Auth Login password login failed.", true));
        }

        var registration = await EnsureRegistrationCompletedAsync(login.Value.UserId, cancellationToken);
        if (registration is not null)
        {
            await AuditAsync(login.Value.UserId, "auth.password_login", "blocked", CoreSliceDefaults.RegistrationIncompleteReason, cancellationToken);
            return Result<PasswordLoginResult>.Success(registration);
        }

        var mfa = await RequireSmartOtpIfEnabledAsync(login.Value.UserId, cancellationToken);
        if (mfa is not null)
        {
            await AuditAsync(login.Value.UserId, "auth.password_login", "blocked", CoreSliceDefaults.SmartOtpRequiredReason, cancellationToken);
            return Result<PasswordLoginResult>.Success(mfa);
        }

        var token = await _jwt.IssueAsync(new JwtIssueRequest(login.Value.UserId, CoreSliceDefaults.DefaultAudience, CoreSliceDefaults.DefaultScopes), cancellationToken);
        if (token.IsFailure || token.Value is null)
        {
            await AuditAsync(login.Value.UserId, "auth.password_login", "partial", token.Error?.Code ?? "jwt_issue_failed", cancellationToken);
            return ChildFailure<PasswordLoginResult>(token.Error ?? new ChildCallError(SecurityErrorCodes.DownstreamUnavailable, "JWT issue failed.", true));
        }

        await AuditAsync(login.Value.UserId, "auth.password_login", "succeeded", "login_completed", cancellationToken);
        return Result<PasswordLoginResult>.Success(new PasswordLoginResult("Completed", login.Value.UserId, ToTokenPair(token.Value), null, null));
    }

    internal static TokenPairResult ToTokenPair(JwtIssueResult token) => new(token.AccessToken, token.RefreshTokenReference, token.ExpiresAt, token.RefreshTokenExpiresAt, token.SessionId);

    internal async ValueTask<PasswordLoginResult?> RequireSmartOtpIfEnabledAsync(Guid userId, CancellationToken cancellationToken)
    {
        var profile = await _profiles.FindByUserIdAsync(userId, cancellationToken);
        return profile?.SmartOtpDeviceCount > 0
            ? new PasswordLoginResult("MfaRequired", userId, null, new MfaChallengeResult(string.Empty, CoreSliceDefaults.SmartOtpMfaMethod, Now.AddMinutes(5)), CoreSliceDefaults.SmartOtpRequiredReason)
            : null;
    }

    internal async ValueTask<PasswordLoginResult?> EnsureRegistrationCompletedAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _users.FindByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            await _users.AddAsync(SecurityUser.CreatePendingPasskey(userId, Now), cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
            return new PasswordLoginResult("PasskeyRequired", userId, null, null, CoreSliceDefaults.RegistrationIncompleteReason);
        }

        if (user.IsRegistrationCompleted)
        {
            return null;
        }

        return new PasswordLoginResult("PasskeyRequired", userId, null, null, CoreSliceDefaults.RegistrationIncompleteReason);
    }
}

public sealed class RefreshSessionCommandHandler : IRequestHandler<RefreshSessionCommand, TokenPairResult>
{
    private readonly IJwtTokenClient _jwt;

    public RefreshSessionCommandHandler(IJwtTokenClient jwt) => _jwt = jwt;

    public async Task<Result<TokenPairResult>> HandleAsync(RefreshSessionCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SessionId))
        {
            return Result<TokenPairResult>.Failure(Error.Validation(SecurityErrorCodes.ValidationFailed, "sessionId is required."));
        }

        var token = await _jwt.RefreshAsync(new JwtRefreshRequest(request.UserId, request.RefreshTokenReference, request.SessionId), cancellationToken);
        if (token.IsFailure || token.Value is null)
        {
            return CoreSliceHandler.ChildFailure<TokenPairResult>(token.Error ?? new ChildCallError(SecurityErrorCodes.DownstreamUnavailable, "JWT refresh failed.", true));
        }

        if (string.IsNullOrWhiteSpace(token.Value.SessionId) || !string.Equals(token.Value.SessionId, request.SessionId, StringComparison.Ordinal))
        {
            return Result<TokenPairResult>.Failure(Error.Authentication(SecurityErrorCodes.CallerRequired, "Refresh token session mismatch."));
        }

        return Result<TokenPairResult>.Success(PasswordLoginCommandHandler.ToTokenPair(token.Value));
    }
}

public sealed class LogoutSessionCommandHandler : CoreSliceHandler, IRequestHandler<LogoutSessionCommand, LogoutSessionResult>
{
    private readonly IJwtTokenClient _jwt;

    public LogoutSessionCommandHandler(IJwtTokenClient jwt, IAuditIntentWriter audit, IClock clock)
        : base(audit, clock) => _jwt = jwt;

    public async Task<Result<LogoutSessionResult>> HandleAsync(LogoutSessionCommand request, CancellationToken cancellationToken)
    {
        var revoked = await _jwt.RevokeSessionAsync(new JwtRevokeRequest(request.UserId, request.SessionId), cancellationToken);
        if (revoked.IsFailure)
        {
            return ChildFailure<LogoutSessionResult>(revoked.Error ?? new ChildCallError(SecurityErrorCodes.DownstreamUnavailable, "JWT revoke failed.", true));
        }

        await AuditAsync(request.UserId, "session.logout", "succeeded", request.ReasonCode, cancellationToken);
        return Result<LogoutSessionResult>.Success(new LogoutSessionResult(request.SessionId, "Revoked", Now));
    }
}

public sealed class BeginPasskeyRegistrationCommandHandler : IRequestHandler<BeginPasskeyRegistrationCommand, BeginPasskeyRegistrationResult>
{
    private readonly IPasskeyClient _passkey;
    private readonly IClock _clock;

    public BeginPasskeyRegistrationCommandHandler(IPasskeyClient passkey, IClock clock)
    {
        _passkey = passkey;
        _clock = clock;
    }

    public async Task<Result<BeginPasskeyRegistrationResult>> HandleAsync(BeginPasskeyRegistrationCommand request, CancellationToken cancellationToken)
    {
        if (request.DeviceContext is null || string.IsNullOrWhiteSpace(request.DeviceContext.DeviceId))
        {
            return Result<BeginPasskeyRegistrationResult>.Failure(Error.Validation(SecurityErrorCodes.ValidationFailed, "deviceContext.deviceId is required."));
        }

        var child = await _passkey.BeginAsync(new PasskeyBeginRequest(request.UserId, "registration", request.DisplayName, null, FinishPasskeyRegistrationCommandHandler.ToPasskeyDevice(request.DeviceContext)), cancellationToken);
        return child.IsSuccess && child.Value is not null
            ? Result<BeginPasskeyRegistrationResult>.Success(new BeginPasskeyRegistrationResult(child.Value.CeremonyId, ParseJson(child.Value.PublicOptionsJson), _clock.UtcNow.AddMinutes(5)))
            : CoreSliceHandler.ChildFailure<BeginPasskeyRegistrationResult>(child.Error ?? new ChildCallError(SecurityErrorCodes.DownstreamUnavailable, "Passkey begin failed.", true));
    }

    private static JsonElement ParseJson(string json) => JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json).RootElement.Clone();
}

public sealed class FinishPasskeyRegistrationCommandHandler : IRequestHandler<FinishPasskeyRegistrationCommand, PasskeyStateResult>
{
    private readonly IPasskeyClient _passkey;
    private readonly ISecurityUserRepository _users;
    private readonly ISecurityProfileWriter _profiles;
    private readonly ILocalUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public FinishPasskeyRegistrationCommandHandler(IPasskeyClient passkey, ISecurityUserRepository users, ISecurityProfileWriter profiles, ILocalUnitOfWork unitOfWork, IClock clock)
    {
        _passkey = passkey;
        _users = users;
        _profiles = profiles;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<PasskeyStateResult>> HandleAsync(FinishPasskeyRegistrationCommand request, CancellationToken cancellationToken)
    {
        if (request.DeviceContext is null || string.IsNullOrWhiteSpace(request.DeviceContext.DeviceId))
        {
            return Result<PasskeyStateResult>.Failure(Error.Validation(SecurityErrorCodes.ValidationFailed, "deviceContext.deviceId is required."));
        }

        if (await _users.FindByIdAsync(request.UserId, cancellationToken) is null)
        {
            return Result<PasskeyStateResult>.Failure(Error.NotFound("SECURITY_USER_NOT_FOUND", "Security user was not found."));
        }

        var child = await _passkey.FinishAsync(new PasskeyFinishRequest(request.UserId, request.CeremonyId, request.Credential.GetRawText(), ToPasskeyDevice(request.DeviceContext)), cancellationToken);
        if (child.IsFailure || child.Value is null)
        {
            return CoreSliceHandler.ChildFailure<PasskeyStateResult>(child.Error ?? new ChildCallError(SecurityErrorCodes.DownstreamUnavailable, "Passkey finish failed.", true));
        }

        await MarkRegistrationPasskeyCompletedAsync(request.UserId, _users, _profiles, _unitOfWork, _clock.UtcNow, cancellationToken);
        return Result<PasskeyStateResult>.Success(new PasskeyStateResult(child.Value.CredentialId, true, _clock.UtcNow));
    }

    internal static async ValueTask MarkRegistrationPasskeyCompletedAsync(Guid userId, ISecurityUserRepository users, ISecurityProfileWriter profiles, ILocalUnitOfWork unitOfWork, DateTimeOffset completedAt, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("Security user must exist before completing passkey registration.");
        user.CompletePasskeyRegistration(completedAt);

        var profile = await profiles.FindByUserIdAsync(userId, cancellationToken);
        if (profile is null)
        {
            profile = SecurityProfile.Create(userId, 1, 0, true, completedAt);
            await profiles.AddAsync(profile, cancellationToken);
        }
        else
        {
            profile.ApplySnapshot(Math.Max(profile.PasskeyCount, 1), profile.SmartOtpDeviceCount, profile.PasswordLoginEnabled, completedAt);
        }

        await unitOfWork.CommitAsync(cancellationToken);
    }

    internal static PasskeyDeviceContext? ToPasskeyDevice(DeviceContext? device)
        => device is null ? null : new PasskeyDeviceContext(device.DeviceId, device.DeviceName, device.Platform, device.IpAddress, device.UserAgent);
}

public sealed class ListPasskeysQueryHandler : IRequestHandler<ListPasskeysQuery, IReadOnlyCollection<PasskeySummaryResult>>
{
    private readonly IPasskeyClient _passkey;
    private readonly IClock _clock;

    public ListPasskeysQueryHandler(IPasskeyClient passkey, IClock clock)
    {
        _passkey = passkey;
        _clock = clock;
    }

    public async Task<Result<IReadOnlyCollection<PasskeySummaryResult>>> HandleAsync(ListPasskeysQuery request, CancellationToken cancellationToken)
    {
        var child = await _passkey.ListAsync(request.UserId, cancellationToken);
        return child.IsSuccess && child.Value is not null
            ? Result<IReadOnlyCollection<PasskeySummaryResult>>.Success(child.Value.Select(x => new PasskeySummaryResult(x.CredentialId, x.DisplayName, x.DisplayName, !x.Revoked, _clock.UtcNow, null)).ToArray())
            : CoreSliceHandler.ChildFailure<IReadOnlyCollection<PasskeySummaryResult>>(child.Error ?? new ChildCallError(SecurityErrorCodes.DownstreamUnavailable, "Passkey list failed.", true));
    }
}

public sealed class RevokePasskeyCommandHandler : IRequestHandler<RevokePasskeyCommand, PasskeyStateResult>
{
    private readonly IPasskeyClient _passkey;
    private readonly IClock _clock;

    public RevokePasskeyCommandHandler(IPasskeyClient passkey, IClock clock)
    {
        _passkey = passkey;
        _clock = clock;
    }

    public async Task<Result<PasskeyStateResult>> HandleAsync(RevokePasskeyCommand request, CancellationToken cancellationToken)
    {
        var child = await _passkey.RevokeAsync(request.UserId, request.PasskeyId, cancellationToken);
        return child.IsSuccess
            ? Result<PasskeyStateResult>.Success(new PasskeyStateResult(request.PasskeyId, false, _clock.UtcNow))
            : CoreSliceHandler.ChildFailure<PasskeyStateResult>(child.Error ?? new ChildCallError(SecurityErrorCodes.DownstreamUnavailable, "Passkey revoke failed.", true));
    }
}

public sealed class BeginPasskeyLoginCommandHandler : IRequestHandler<BeginPasskeyLoginCommand, BeginPasskeyLoginResult>
{
    private readonly IPasskeyClient _passkey;
    private readonly IClock _clock;

    public BeginPasskeyLoginCommandHandler(IPasskeyClient passkey, IClock clock)
    {
        _passkey = passkey;
        _clock = clock;
    }

    public async Task<Result<BeginPasskeyLoginResult>> HandleAsync(BeginPasskeyLoginCommand request, CancellationToken cancellationToken)
    {
        var child = await _passkey.BeginAsync(new PasskeyBeginRequest(Guid.Empty, "login", null, request.LoginIdentifier, FinishPasskeyRegistrationCommandHandler.ToPasskeyDevice(request.DeviceContext)), cancellationToken);
        return child.IsSuccess && child.Value is not null
            ? Result<BeginPasskeyLoginResult>.Success(new BeginPasskeyLoginResult(child.Value.CeremonyId, JsonDocument.Parse(child.Value.PublicOptionsJson).RootElement.Clone(), _clock.UtcNow.AddMinutes(5)))
            : CoreSliceHandler.ChildFailure<BeginPasskeyLoginResult>(child.Error ?? new ChildCallError(SecurityErrorCodes.DownstreamUnavailable, "Passkey login begin failed.", true));
    }
}

public sealed class FinishPasskeyLoginCommandHandler : IRequestHandler<FinishPasskeyLoginCommand, PasswordLoginResult>
{
    private readonly IPasskeyClient _passkey;
    private readonly IJwtTokenClient _jwt;
    private readonly ISecurityUserRepository _users;
    private readonly ISecurityProfileReader _profiles;
    private readonly IClock _clock;

    public FinishPasskeyLoginCommandHandler(IPasskeyClient passkey, IJwtTokenClient jwt, ISecurityUserRepository users, ISecurityProfileReader profiles, IClock clock)
    {
        _passkey = passkey;
        _jwt = jwt;
        _users = users;
        _profiles = profiles;
        _clock = clock;
    }

    public async Task<Result<PasswordLoginResult>> HandleAsync(FinishPasskeyLoginCommand request, CancellationToken cancellationToken)
    {
        var passkey = await _passkey.FinishAsync(new PasskeyFinishRequest(Guid.Empty, request.CeremonyId, request.Credential.GetRawText(), FinishPasskeyRegistrationCommandHandler.ToPasskeyDevice(request.DeviceContext)), cancellationToken);
        if (passkey.IsFailure || passkey.Value is null)
        {
            return CoreSliceHandler.ChildFailure<PasswordLoginResult>(passkey.Error ?? new ChildCallError(SecurityErrorCodes.DownstreamUnavailable, "Passkey login finish failed.", true));
        }

        var user = await _users.FindByIdAsync(passkey.Value.UserId, cancellationToken);
        if (user is not null && !user.IsRegistrationCompleted)
        {
            return Result<PasswordLoginResult>.Success(new PasswordLoginResult("PasskeyRequired", passkey.Value.UserId, null, null, CoreSliceDefaults.RegistrationIncompleteReason));
        }

        var profile = await _profiles.FindByUserIdAsync(passkey.Value.UserId, cancellationToken);
        if (profile?.SmartOtpDeviceCount > 0)
        {
            return Result<PasswordLoginResult>.Success(new PasswordLoginResult("MfaRequired", passkey.Value.UserId, null, new MfaChallengeResult(string.Empty, CoreSliceDefaults.SmartOtpMfaMethod, _clock.UtcNow.AddMinutes(5)), CoreSliceDefaults.SmartOtpRequiredReason));
        }

        var token = await _jwt.IssueAsync(new JwtIssueRequest(passkey.Value.UserId, CoreSliceDefaults.DefaultAudience, CoreSliceDefaults.DefaultScopes), cancellationToken);
        return token.IsSuccess && token.Value is not null
            ? Result<PasswordLoginResult>.Success(new PasswordLoginResult("Completed", passkey.Value.UserId, PasswordLoginCommandHandler.ToTokenPair(token.Value), null, null))
            : CoreSliceHandler.ChildFailure<PasswordLoginResult>(token.Error ?? new ChildCallError(SecurityErrorCodes.DownstreamUnavailable, "JWT issue failed.", true));
    }
}

public sealed class BeginRegistrationPasskeyCommandHandler : IRequestHandler<BeginRegistrationPasskeyCommand, BeginRegistrationPasskeyResult>
{
    private readonly IPasskeyClient _passkey;
    private readonly ISecurityUserRepository _users;
    private readonly IClock _clock;

    public BeginRegistrationPasskeyCommandHandler(IPasskeyClient passkey, ISecurityUserRepository users, IClock clock)
    {
        _passkey = passkey;
        _users = users;
        _clock = clock;
    }

    public async Task<Result<BeginRegistrationPasskeyResult>> HandleAsync(BeginRegistrationPasskeyCommand request, CancellationToken cancellationToken)
    {
        if (request.DeviceContext is null || string.IsNullOrWhiteSpace(request.DeviceContext.DeviceId))
        {
            return Result<BeginRegistrationPasskeyResult>.Failure(Error.Validation(SecurityErrorCodes.ValidationFailed, "deviceContext.deviceId is required."));
        }

        var user = await _users.FindByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result<BeginRegistrationPasskeyResult>.Failure(Error.NotFound("SECURITY_USER_NOT_FOUND", "Security user was not found."));
        }

        if (user.IsRegistrationCompleted)
        {
            return Result<BeginRegistrationPasskeyResult>.Failure(Error.Conflict("SECURITY_REGISTRATION_ALREADY_COMPLETED", "Registration has already been completed."));
        }

        var child = await _passkey.BeginAsync(new PasskeyBeginRequest(request.UserId, "registration", request.DisplayName, null, FinishPasskeyRegistrationCommandHandler.ToPasskeyDevice(request.DeviceContext)), cancellationToken);
        return child.IsSuccess && child.Value is not null
            ? Result<BeginRegistrationPasskeyResult>.Success(new BeginRegistrationPasskeyResult(request.UserId, child.Value.CeremonyId, JsonDocument.Parse(string.IsNullOrWhiteSpace(child.Value.PublicOptionsJson) ? "{}" : child.Value.PublicOptionsJson).RootElement.Clone(), _clock.UtcNow.AddMinutes(5)))
            : CoreSliceHandler.ChildFailure<BeginRegistrationPasskeyResult>(child.Error ?? new ChildCallError(SecurityErrorCodes.DownstreamUnavailable, "Passkey begin failed.", true));
    }
}

public sealed class FinishRegistrationPasskeyCommandHandler : IRequestHandler<FinishRegistrationPasskeyCommand, FinishRegistrationPasskeyResult>
{
    private readonly IPasskeyClient _passkey;
    private readonly ISecurityUserRepository _users;
    private readonly ISecurityProfileWriter _profiles;
    private readonly ILocalUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public FinishRegistrationPasskeyCommandHandler(IPasskeyClient passkey, ISecurityUserRepository users, ISecurityProfileWriter profiles, ILocalUnitOfWork unitOfWork, IClock clock)
    {
        _passkey = passkey;
        _users = users;
        _profiles = profiles;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<FinishRegistrationPasskeyResult>> HandleAsync(FinishRegistrationPasskeyCommand request, CancellationToken cancellationToken)
    {
        if (request.DeviceContext is null || string.IsNullOrWhiteSpace(request.DeviceContext.DeviceId))
        {
            return Result<FinishRegistrationPasskeyResult>.Failure(Error.Validation(SecurityErrorCodes.ValidationFailed, "deviceContext.deviceId is required."));
        }

        var user = await _users.FindByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result<FinishRegistrationPasskeyResult>.Failure(Error.NotFound("SECURITY_USER_NOT_FOUND", "Security user was not found."));
        }

        if (user.IsRegistrationCompleted)
        {
            return Result<FinishRegistrationPasskeyResult>.Success(new FinishRegistrationPasskeyResult(request.UserId, user.RegistrationStatus.ToString(), user.RegistrationCompletedAt ?? _clock.UtcNow));
        }

        var child = await _passkey.FinishAsync(new PasskeyFinishRequest(request.UserId, request.CeremonyId, request.Credential.GetRawText(), FinishPasskeyRegistrationCommandHandler.ToPasskeyDevice(request.DeviceContext)), cancellationToken);
        if (child.IsFailure || child.Value is null)
        {
            return CoreSliceHandler.ChildFailure<FinishRegistrationPasskeyResult>(child.Error ?? new ChildCallError(SecurityErrorCodes.DownstreamUnavailable, "Passkey finish failed.", true));
        }

        var completedAt = _clock.UtcNow;
        await FinishPasskeyRegistrationCommandHandler.MarkRegistrationPasskeyCompletedAsync(request.UserId, _users, _profiles, _unitOfWork, completedAt, cancellationToken);
        return Result<FinishRegistrationPasskeyResult>.Success(new FinishRegistrationPasskeyResult(request.UserId, UserRegistrationStatus.Completed.ToString(), completedAt));
    }
}

public sealed class BeginSmartOtpEnrollmentCommandHandler : IRequestHandler<BeginSmartOtpEnrollmentCommand, BeginSmartOtpEnrollmentResult>
{
    private readonly ISmartOtpClient _smartOtp;
    private readonly IClock _clock;

    public BeginSmartOtpEnrollmentCommandHandler(ISmartOtpClient smartOtp, IClock clock)
    {
        _smartOtp = smartOtp;
        _clock = clock;
    }

    public async Task<Result<BeginSmartOtpEnrollmentResult>> HandleAsync(BeginSmartOtpEnrollmentCommand request, CancellationToken cancellationToken)
    {
        var child = await _smartOtp.BeginBindAsync(new SmartOtpBindBeginRequest(request.UserId, request.DeviceName, request.Platform, request.AppInstanceIdHash, request.KeyAlgorithm, request.CandidatePublicKeySpki, request.CandidatePublicKeyThumbprint), cancellationToken);
        return child.IsSuccess && child.Value is not null
            ? Result<BeginSmartOtpEnrollmentResult>.Success(new BeginSmartOtpEnrollmentResult(child.Value.BindingId, child.Value.ServerChallengeBase64, child.Value.ChallengeFormatVersion, child.Value.CreatedAtUtc, child.Value.ExpiresAtUtc, child.Value.Status))
            : CoreSliceHandler.ChildFailure<BeginSmartOtpEnrollmentResult>(child.Error ?? new ChildCallError(SecurityErrorCodes.DownstreamUnavailable, "Smart OTP begin bind failed.", true));
    }
}

public sealed class ConfirmSmartOtpEnrollmentCommandHandler : IRequestHandler<ConfirmSmartOtpEnrollmentCommand, SmartOtpDeviceStateResult>
{
    private readonly ISmartOtpClient _smartOtp;
    private readonly ISecurityProfileWriter _profiles;
    private readonly ILocalUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ConfirmSmartOtpEnrollmentCommandHandler(ISmartOtpClient smartOtp, ISecurityProfileWriter profiles, ILocalUnitOfWork unitOfWork, IClock clock)
    {
        _smartOtp = smartOtp;
        _profiles = profiles;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<SmartOtpDeviceStateResult>> HandleAsync(ConfirmSmartOtpEnrollmentCommand request, CancellationToken cancellationToken)
    {
        var child = await _smartOtp.FinishBindAsync(new SmartOtpBindFinishRequest(request.UserId, request.EnrollmentId, request.ClientNonce, request.DeviceSignature), cancellationToken);
        if (child.IsFailure || child.Value is null)
        {
            return CoreSliceHandler.ChildFailure<SmartOtpDeviceStateResult>(child.Error ?? new ChildCallError(SecurityErrorCodes.DownstreamUnavailable, "Smart OTP confirm bind failed.", true));
        }

        var isActive = string.Equals(child.Value.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase);
        if (isActive)
        {
            var profile = await _profiles.FindByUserIdAsync(request.UserId, cancellationToken);
            if (profile is null)
            {
                profile = SecurityProfile.Create(request.UserId, 0, 1, true, _clock.UtcNow);
                await _profiles.AddAsync(profile, cancellationToken);
            }
            else
            {
                profile.ApplySnapshot(profile.PasskeyCount, Math.Max(profile.SmartOtpDeviceCount, 1), profile.PasswordLoginEnabled, _clock.UtcNow);
            }

            await _unitOfWork.CommitAsync(cancellationToken);
        }

        return Result<SmartOtpDeviceStateResult>.Success(new SmartOtpDeviceStateResult(child.Value.DeviceId, child.Value.DeviceKeyId, child.Value.Status, isActive, child.Value.BoundAtUtc));
    }
}

public sealed class StartStepUpCommandHandler : IRequestHandler<StartStepUpCommand, StepUpChallengeResult>
{
    private readonly ISmartOtpClient _smartOtp;

    public StartStepUpCommandHandler(ISmartOtpClient smartOtp) => _smartOtp = smartOtp;

    public async Task<Result<StepUpChallengeResult>> HandleAsync(StartStepUpCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceId))
        {
            return Result<StepUpChallengeResult>.Failure(Error.Validation(SecurityErrorCodes.ValidationFailed, "deviceId is required."));
        }

        var child = await _smartOtp.CreateChallengeAsync(new SmartOtpChallengeRequest(request.UserId, request.DeviceId, request.Purpose, request.ExternalTransactionId, request.TransactionDigest), cancellationToken);
        return child.IsSuccess && child.Value is not null
            ? Result<StepUpChallengeResult>.Success(new StepUpChallengeResult(child.Value.ChallengeId, child.Value.ExternalUserId, child.Value.DeviceId, child.Value.DeviceKeyId, request.Purpose, request.ExternalTransactionId, request.TransactionDigest, child.Value.ExpiresAt, child.Value.CodeLength, child.Value.MaxAttempts))
            : CoreSliceHandler.ChildFailure<StepUpChallengeResult>(child.Error ?? new ChildCallError(SecurityErrorCodes.DownstreamUnavailable, "Smart OTP challenge failed.", true));
    }
}

public sealed class RevealStepUpCommandHandler : IRequestHandler<RevealStepUpCommand, StepUpRevealResult>
{
    private readonly ISmartOtpClient _smartOtp;

    public RevealStepUpCommandHandler(ISmartOtpClient smartOtp) => _smartOtp = smartOtp;

    public async Task<Result<StepUpRevealResult>> HandleAsync(RevealStepUpCommand request, CancellationToken cancellationToken)
    {
        var child = await _smartOtp.RevealAsync(new SmartOtpRevealRequest(
            request.UserId,
            request.ChallengeId,
            request.DeviceId,
            request.DeviceKeyId,
            request.Purpose,
            request.ExternalTransactionId,
            request.TransactionDigest,
            request.RevealRequestId,
            request.IssuedAtUtc,
            request.ProofExpiresAtUtc,
            request.DeviceSignature), cancellationToken);

        return child.IsSuccess && child.Value is not null
            ? Result<StepUpRevealResult>.Success(new StepUpRevealResult(child.Value.ChallengeId, child.Value.OtpCode, child.Value.ExpiresAtUtc, child.Value.RevealCount, child.Value.ReleasedAtUtc))
            : CoreSliceHandler.ChildFailure<StepUpRevealResult>(child.Error ?? new ChildCallError(SecurityErrorCodes.DownstreamUnavailable, "Smart OTP reveal failed.", true));
    }
}

public sealed class VerifyStepUpCommandHandler : IRequestHandler<VerifyStepUpCommand, StepUpGrantResult>
{
    private readonly ISmartOtpClient _smartOtp;
    private readonly IClock _clock;

    public VerifyStepUpCommandHandler(ISmartOtpClient smartOtp, IClock clock)
    {
        _smartOtp = smartOtp;
        _clock = clock;
    }

    public async Task<Result<StepUpGrantResult>> HandleAsync(VerifyStepUpCommand request, CancellationToken cancellationToken)
    {
        var child = await _smartOtp.VerifyAsync(new SmartOtpVerifyRequest(request.UserId, request.ChallengeId, request.DeviceId, request.Purpose, request.ExternalTransactionId, request.TransactionDigest, new SensitiveString(request.Otp)), cancellationToken);
        return child.IsSuccess && child.Value is not null
            ? Result<StepUpGrantResult>.Success(new StepUpGrantResult(request.ChallengeId, $"step-up:{child.Value.UserId:N}", request.Purpose, _clock.UtcNow.AddMinutes(5)))
            : CoreSliceHandler.ChildFailure<StepUpGrantResult>(child.Error ?? new ChildCallError(SecurityErrorCodes.DownstreamUnavailable, "Smart OTP verify failed.", true));
    }
}

public sealed class CompleteLoginSmartOtpCommandHandler : CoreSliceHandler, IRequestHandler<CompleteLoginSmartOtpCommand, PasswordLoginResult>
{
    private readonly ISmartOtpClient _smartOtp;
    private readonly IJwtTokenClient _jwt;

    public CompleteLoginSmartOtpCommandHandler(ISmartOtpClient smartOtp, IJwtTokenClient jwt, IAuditIntentWriter audit, IClock clock)
        : base(audit, clock)
    {
        _smartOtp = smartOtp;
        _jwt = jwt;
    }

    public async Task<Result<PasswordLoginResult>> HandleAsync(CompleteLoginSmartOtpCommand request, CancellationToken cancellationToken)
    {
        var verified = await _smartOtp.VerifyAsync(new SmartOtpVerifyRequest(request.UserId, request.ChallengeId, request.DeviceId, request.Purpose, request.ExternalTransactionId, request.TransactionDigest, new SensitiveString(request.Otp)), cancellationToken);
        if (verified.IsFailure || verified.Value is null || verified.Value.UserId != request.UserId)
        {
            await AuditAsync(request.UserId, "auth.login_smart_otp", "failed", verified.Error?.Code ?? "smart_otp_verify_failed", cancellationToken);
            return ChildFailure<PasswordLoginResult>(verified.Error ?? new ChildCallError(SecurityErrorCodes.DownstreamUnavailable, "Smart OTP login verify failed.", true));
        }

        var token = await _jwt.IssueAsync(new JwtIssueRequest(request.UserId, CoreSliceDefaults.DefaultAudience, CoreSliceDefaults.DefaultScopes), cancellationToken);
        if (token.IsFailure || token.Value is null)
        {
            await AuditAsync(request.UserId, "auth.login_smart_otp", "partial", token.Error?.Code ?? "jwt_issue_failed", cancellationToken);
            return ChildFailure<PasswordLoginResult>(token.Error ?? new ChildCallError(SecurityErrorCodes.DownstreamUnavailable, "JWT issue failed.", true));
        }

        await AuditAsync(request.UserId, "auth.login_smart_otp", "succeeded", "login_completed", cancellationToken);
        return Result<PasswordLoginResult>.Success(new PasswordLoginResult("Completed", request.UserId, PasswordLoginCommandHandler.ToTokenPair(token.Value), null, null));
    }
}
