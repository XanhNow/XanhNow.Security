using System.Text.Json;
using XanhNow.Security.Application.Abstractions.Audit;
using XanhNow.Security.Application.Abstractions.ChildApps;
using XanhNow.Security.Application.Abstractions.ChildApps.AuthLogin;
using XanhNow.Security.Application.Abstractions.ChildApps.Jwt;
using XanhNow.Security.Application.Abstractions.Grant;
using XanhNow.Security.Application.Abstractions.ChildApps.Passkey;
using XanhNow.Security.Application.Abstractions.ChildApps.SmartOtp;
using XanhNow.Security.Application.Abstractions.Ids;
using XanhNow.Security.Application.Abstractions.Outbox;
using XanhNow.Security.Application.Abstractions.Persistence;
using XanhNow.Security.Application.Abstractions.Time;
using XanhNow.Security.Application.Common.Requests;
using XanhNow.Security.Application.Common.Results;
using XanhNow.Security.Domain.Grants;
using XanhNow.Security.Domain.Operations;
using XanhNow.Security.Domain.Users;
using XanhNow.Security.Domain.ValueObjects;

namespace XanhNow.Security.Application.Core;

internal static class AccountSecuritySliceMapper
{
    public static AccountSecurityOperationResult ToOperation(AuthLoginOperationResult operation, DateTimeOffset acceptedAtUtc)
        => new(operation.OperationId, operation.OperationType, operation.Status, operation.CurrentStep, acceptedAtUtc);

    public static AccountStateResult ToAccountState(AuthLoginAccountStateChangeResult state)
        => new(state.UserId, state.Status, state.ChangedAtUtc);

    public static SecurityProfileResult ToSecurityProfile(AuthLoginAccountStatusResult status, bool hasPasskey, bool hasSmartOtp, bool isStale)
        => new(status.UserId, status.MaskedPhoneNumber, status.Status, "Unknown", hasPasskey, hasSmartOtp, isStale, status.UpdatedAtUtc);

    public static SessionSummaryResult ToSession(JwtSessionDescriptor session)
        => new(session.SessionId, session.UserId, session.Status, session.DeviceName, session.Platform, session.CreatedAtUtc, session.LastSeenAtUtc, session.ExpiresAtUtc);
}

internal static class StepUpGrantVerifier
{
    public static async ValueTask<Result<bool>> VerifyAndConsumeAsync(
        string? protectedGrant,
        string expectedPurpose,
        Guid expectedUserId,
        IGrantProtector protector,
        ISecurityGrantRepository grants,
        IClock clock,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(protectedGrant))
        {
            return Result<bool>.Failure(Error.Authentication(SecurityErrorCodes.CallerRequired, "A valid step-up grant is required."));
        }

        var verified = await protector.VerifyAsync(protectedGrant, expectedPurpose, cancellationToken);
        if (!verified.IsValid || verified.UserId != expectedUserId || verified.GrantId is null)
        {
            return Result<bool>.Failure(Error.Authentication(SecurityErrorCodes.CallerRequired, "Step-up grant is invalid."));
        }

        var grant = await grants.FindByIdAsync(verified.GrantId.Value, cancellationToken);
        if (grant is null || grant.UserId != expectedUserId || grant.Type != SecurityGrantType.StepUpGrant || grant.Status != SecurityGrantStatus.Active)
        {
            return Result<bool>.Failure(Error.Authentication(SecurityErrorCodes.CallerRequired, "Step-up grant is not active."));
        }

        if (!string.Equals(grant.Purpose.Value, expectedPurpose, StringComparison.Ordinal) || grant.ExpiresAt <= clock.UtcNow)
        {
            return Result<bool>.Failure(Error.Authentication(SecurityErrorCodes.CallerRequired, "Step-up grant is expired or has the wrong purpose."));
        }

        grant.Consume(clock.UtcNow);
        return Result<bool>.Success(true);
    }
}


public sealed class GetOperationStatusQueryHandler : IRequestHandler<GetOperationStatusQuery, OperationStatusResult>
{
    private readonly ISecurityOperationRepository _operations;

    public GetOperationStatusQueryHandler(ISecurityOperationRepository operations)
    {
        _operations = operations;
    }

    public async Task<Result<OperationStatusResult>> HandleAsync(GetOperationStatusQuery request, CancellationToken cancellationToken)
    {
        var operation = await _operations.FindByIdAsync(request.OperationId, cancellationToken);
        if (operation is null)
        {
            return Result<OperationStatusResult>.Failure(Error.NotFound("SECURITY_OPERATION_NOT_FOUND", "Security operation was not found."));
        }

        if (operation.UserId != request.UserId)
        {
            return Result<OperationStatusResult>.Failure(Error.NotFound("SECURITY_OPERATION_NOT_FOUND", "Security operation was not found."));
        }

        var currentStep = operation.Steps
            .OrderByDescending(x => x.StartedAt ?? x.CompletedAt ?? x.CreatedAt)
            .FirstOrDefault(x => x.Status is OperationStepStatus.Running or OperationStepStatus.RetryPending or OperationStepStatus.FailedSafe)
            ?? operation.Steps.OrderBy(x => x.CreatedAt).First();

        var updatedAt = operation.TerminalAt
            ?? operation.Steps.Select(x => x.CompletedAt ?? x.StartedAt ?? x.CreatedAt).DefaultIfEmpty(operation.CreatedAt).Max();

        return Result<OperationStatusResult>.Success(new OperationStatusResult(
            operation.Id,
            operation.UserId,
            operation.OperationType.Value,
            operation.Status.ToString(),
            currentStep.StepCode.Value,
            currentStep.FailureCode,
            updatedAt));
    }
}
public sealed class ChangePasswordCommandHandler : CoreSliceHandler, IRequestHandler<ChangePasswordCommand, AccountSecurityOperationResult>
{
    private readonly IAuthLoginClient _authLogin;

    public ChangePasswordCommandHandler(IAuthLoginClient authLogin, IAuditIntentWriter audit, IClock clock)
        : base(audit, clock) => _authLogin = authLogin;

    public async Task<Result<AccountSecurityOperationResult>> HandleAsync(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var child = await _authLogin.ChangePasswordAsync(new AuthLoginChangePasswordRequest(request.UserId, new SensitiveString(request.CurrentPassword), new SensitiveString(request.NewPassword), request.ReasonCode), cancellationToken);
        if (child.IsFailure || child.Value is null)
        {
            await AuditAsync(request.UserId, "password.change", "failed", child.Error?.Code ?? "auth_login_failed", cancellationToken);
            return ChildFailure<AccountSecurityOperationResult>(child.Error ?? new ChildCallError(SecurityErrorCodes.DownstreamUnavailable, "Auth Login change password failed.", true));
        }

        await AuditAsync(request.UserId, "password.change", "accepted", request.ReasonCode, cancellationToken);
        return Result<AccountSecurityOperationResult>.Success(AccountSecuritySliceMapper.ToOperation(child.Value, Now));
    }
}

public sealed class StartPasswordResetCommandHandler : CoreSliceHandler, IRequestHandler<StartPasswordResetCommand, AccountSecurityOperationResult>
{
    private readonly IAuthLoginClient _authLogin;

    public StartPasswordResetCommandHandler(IAuthLoginClient authLogin, IAuditIntentWriter audit, IClock clock)
        : base(audit, clock) => _authLogin = authLogin;

    public async Task<Result<AccountSecurityOperationResult>> HandleAsync(StartPasswordResetCommand request, CancellationToken cancellationToken)
    {
        var child = await _authLogin.StartPasswordResetAsync(new AuthLoginPasswordResetStartRequest(request.PhoneNumber), cancellationToken);
        if (child.IsFailure || child.Value is null)
        {
            await AuditAsync(null, "password.reset.start", "failed", child.Error?.Code ?? "auth_login_failed", cancellationToken);
            return ChildFailure<AccountSecurityOperationResult>(child.Error ?? new ChildCallError(SecurityErrorCodes.DownstreamUnavailable, "Auth Login password reset start failed.", true));
        }

        await AuditAsync(null, "password.reset.start", "accepted", "password_reset_requested", cancellationToken);
        return Result<AccountSecurityOperationResult>.Success(AccountSecuritySliceMapper.ToOperation(child.Value, Now));
    }
}

public sealed class CompletePasswordResetCommandHandler : CoreSliceHandler, IRequestHandler<CompletePasswordResetCommand, AccountSecurityOperationResult>
{
    private readonly IAuthLoginClient _authLogin;

    public CompletePasswordResetCommandHandler(IAuthLoginClient authLogin, IAuditIntentWriter audit, IClock clock)
        : base(audit, clock) => _authLogin = authLogin;

    public async Task<Result<AccountSecurityOperationResult>> HandleAsync(CompletePasswordResetCommand request, CancellationToken cancellationToken)
    {
        var child = await _authLogin.CompletePasswordResetAsync(new AuthLoginPasswordResetCompleteRequest(request.ResetOperationId, new SensitiveString(request.NewPassword)), cancellationToken);
        if (child.IsFailure || child.Value is null)
        {
            await AuditAsync(null, "password.reset.complete", "failed", child.Error?.Code ?? "auth_login_failed", cancellationToken);
            return ChildFailure<AccountSecurityOperationResult>(child.Error ?? new ChildCallError(SecurityErrorCodes.DownstreamUnavailable, "Auth Login password reset complete failed.", true));
        }

        await AuditAsync(null, "password.reset.complete", "accepted", "password_reset_completed", cancellationToken);
        return Result<AccountSecurityOperationResult>.Success(AccountSecuritySliceMapper.ToOperation(child.Value, Now));
    }
}

public sealed class ForcePasswordChangeCommandHandler : CoreSliceHandler, IRequestHandler<ForcePasswordChangeCommand, AccountStateResult>
{
    private readonly IAuthLoginClient _authLogin;

    public ForcePasswordChangeCommandHandler(IAuthLoginClient authLogin, IAuditIntentWriter audit, IClock clock)
        : base(audit, clock) => _authLogin = authLogin;

    public async Task<Result<AccountStateResult>> HandleAsync(ForcePasswordChangeCommand request, CancellationToken cancellationToken)
    {
        var child = await _authLogin.ForcePasswordChangeAsync(new AuthLoginForcePasswordChangeRequest(request.UserId, new SensitiveString(request.NewPassword), request.ReasonCode), cancellationToken);
        if (child.IsFailure || child.Value is null)
        {
            await AuditAsync(request.UserId, "password.force_change", "failed", child.Error?.Code ?? "auth_login_failed", cancellationToken);
            return ChildFailure<AccountStateResult>(child.Error ?? new ChildCallError(SecurityErrorCodes.DownstreamUnavailable, "Auth Login force password change failed.", true));
        }

        await AuditAsync(request.UserId, "password.force_change", "succeeded", request.ReasonCode, cancellationToken);
        return Result<AccountStateResult>.Success(AccountSecuritySliceMapper.ToAccountState(child.Value));
    }
}

public sealed class StartPhoneChangeCommandHandler : CoreSliceHandler, IRequestHandler<StartPhoneChangeCommand, AccountSecurityOperationResult>
{
    private const string StepUpPurpose = "phone.change";
    private readonly IAuthLoginClient _authLogin;
    private readonly IGrantProtector _grantProtector;
    private readonly ISecurityGrantRepository _grants;
    private readonly ILocalUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public StartPhoneChangeCommandHandler(IAuthLoginClient authLogin, IGrantProtector grantProtector, ISecurityGrantRepository grants, ILocalUnitOfWork unitOfWork, IAuditIntentWriter audit, IClock clock)
        : base(audit, clock)
    {
        _authLogin = authLogin;
        _grantProtector = grantProtector;
        _grants = grants;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<AccountSecurityOperationResult>> HandleAsync(StartPhoneChangeCommand request, CancellationToken cancellationToken)
    {
        var stepUp = await StepUpGrantVerifier.VerifyAndConsumeAsync(request.StepUpGrant, StepUpPurpose, request.UserId, _grantProtector, _grants, _clock, cancellationToken);
        if (stepUp.IsFailure)
        {
            await AuditAsync(request.UserId, "phone.change.start", "blocked", stepUp.Error?.Code ?? "step_up_required", cancellationToken);
            return Result<AccountSecurityOperationResult>.Failure(stepUp.Error!);
        }

        var child = await _authLogin.StartPhoneChangeAsync(new AuthLoginPhoneChangeStartRequest(request.UserId, request.NewPhoneNumber, request.StepUpGrant, request.ReasonCode), cancellationToken);
        if (child.IsFailure || child.Value is null)
        {
            await AuditAsync(request.UserId, "phone.change.start", "failed", child.Error?.Code ?? "auth_login_failed", cancellationToken);
            return ChildFailure<AccountSecurityOperationResult>(child.Error ?? new ChildCallError(SecurityErrorCodes.DownstreamUnavailable, "Auth Login phone change start failed.", true));
        }

        await AuditAsync(request.UserId, "phone.change.start", "accepted", request.ReasonCode, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return Result<AccountSecurityOperationResult>.Success(AccountSecuritySliceMapper.ToOperation(child.Value, Now));
    }
}

public sealed class ConfirmPhoneChangeCommandHandler : CoreSliceHandler, IRequestHandler<ConfirmPhoneChangeCommand, AccountSecurityOperationResult>
{
    private readonly IAuthLoginClient _authLogin;

    public ConfirmPhoneChangeCommandHandler(IAuthLoginClient authLogin, IAuditIntentWriter audit, IClock clock)
        : base(audit, clock) => _authLogin = authLogin;

    public async Task<Result<AccountSecurityOperationResult>> HandleAsync(ConfirmPhoneChangeCommand request, CancellationToken cancellationToken)
    {
        var child = await _authLogin.ConfirmPhoneChangeAsync(new AuthLoginPhoneChangeConfirmRequest(request.UserId, request.OperationId, new SensitiveString(request.Otp)), cancellationToken);
        if (child.IsFailure || child.Value is null)
        {
            await AuditAsync(request.UserId, "phone.change.confirm", "failed", child.Error?.Code ?? "auth_login_failed", cancellationToken);
            return ChildFailure<AccountSecurityOperationResult>(child.Error ?? new ChildCallError(SecurityErrorCodes.DownstreamUnavailable, "Auth Login phone change confirm failed.", true));
        }

        await AuditAsync(request.UserId, "phone.change.confirm", "accepted", "phone_change_confirmed", cancellationToken);
        return Result<AccountSecurityOperationResult>.Success(AccountSecuritySliceMapper.ToOperation(child.Value, Now));
    }
}

public sealed class CancelPhoneChangeCommandHandler : CoreSliceHandler, IRequestHandler<CancelPhoneChangeCommand, AccountSecurityOperationResult>
{
    private readonly IAuthLoginClient _authLogin;

    public CancelPhoneChangeCommandHandler(IAuthLoginClient authLogin, IAuditIntentWriter audit, IClock clock)
        : base(audit, clock) => _authLogin = authLogin;

    public async Task<Result<AccountSecurityOperationResult>> HandleAsync(CancelPhoneChangeCommand request, CancellationToken cancellationToken)
    {
        var child = await _authLogin.CancelPhoneChangeAsync(new AuthLoginPhoneChangeCancelRequest(request.UserId, request.OperationId, request.ReasonCode), cancellationToken);
        if (child.IsFailure || child.Value is null)
        {
            await AuditAsync(request.UserId, "phone.change.cancel", "failed", child.Error?.Code ?? "auth_login_failed", cancellationToken);
            return ChildFailure<AccountSecurityOperationResult>(child.Error ?? new ChildCallError(SecurityErrorCodes.DownstreamUnavailable, "Auth Login phone change cancel failed.", true));
        }

        await AuditAsync(request.UserId, "phone.change.cancel", "accepted", request.ReasonCode, cancellationToken);
        return Result<AccountSecurityOperationResult>.Success(AccountSecuritySliceMapper.ToOperation(child.Value, Now));
    }
}

public sealed class GetSecurityProfileQueryHandler : IRequestHandler<GetSecurityProfileQuery, SecurityProfileResult>
{
    private readonly IAuthLoginClient _authLogin;
    private readonly IPasskeyClient _passkey;
    private readonly ISecurityProfileReader _profiles;

    public GetSecurityProfileQueryHandler(IAuthLoginClient authLogin, IPasskeyClient passkey, ISecurityProfileReader profiles)
    {
        _authLogin = authLogin;
        _passkey = passkey;
        _profiles = profiles;
    }

    public async Task<Result<SecurityProfileResult>> HandleAsync(GetSecurityProfileQuery request, CancellationToken cancellationToken)
    {
        var status = await _authLogin.GetAccountStatusAsync(request.UserId, cancellationToken);
        if (status.IsFailure || status.Value is null)
        {
            return CoreSliceHandler.ChildFailure<SecurityProfileResult>(status.Error ?? new ChildCallError(SecurityErrorCodes.DownstreamUnavailable, "Auth Login account status failed.", true));
        }

        var passkeys = await _passkey.ListAsync(request.UserId, cancellationToken);
        var profile = await _profiles.FindByUserIdAsync(request.UserId, cancellationToken);
        var hasPasskey = passkeys.IsSuccess && passkeys.Value?.Any(x => !x.Revoked) == true;
        var hasSmartOtp = profile?.SmartOtpDeviceCount > 0;
        return Result<SecurityProfileResult>.Success(AccountSecuritySliceMapper.ToSecurityProfile(status.Value, hasPasskey, hasSmartOtp, profile?.IsStale == true));
    }
}

public sealed class ChangeAccountStateCommandHandler : CoreSliceHandler, IRequestHandler<ChangeAccountStateCommand, AccountStateResult>
{
    private readonly IAuthLoginClient _authLogin;

    public ChangeAccountStateCommandHandler(IAuthLoginClient authLogin, IAuditIntentWriter audit, IClock clock)
        : base(audit, clock) => _authLogin = authLogin;

    public async Task<Result<AccountStateResult>> HandleAsync(ChangeAccountStateCommand request, CancellationToken cancellationToken)
    {
        var child = await _authLogin.ChangeAccountStateAsync(new AuthLoginAccountStateChangeRequest(request.UserId, request.TargetState.ToString(), request.ReasonCode, request.Comment), cancellationToken);
        if (child.IsFailure || child.Value is null)
        {
            await AuditAsync(request.UserId, $"account.{request.TargetState.ToString().ToLowerInvariant()}", "failed", child.Error?.Code ?? "auth_login_failed", cancellationToken);
            return ChildFailure<AccountStateResult>(child.Error ?? new ChildCallError(SecurityErrorCodes.DownstreamUnavailable, "Auth Login account state change failed.", true));
        }

        await AuditAsync(request.UserId, $"account.{request.TargetState.ToString().ToLowerInvariant()}", "succeeded", request.ReasonCode, cancellationToken);
        return Result<AccountStateResult>.Success(AccountSecuritySliceMapper.ToAccountState(child.Value));
    }
}
public sealed class DeleteOwnAccountCommandHandler : CoreSliceHandler, IRequestHandler<DeleteOwnAccountCommand, DeleteOwnAccountResult>
{
    private const string ReasonCodeValue = "account_self_delete";
    private readonly IAuthLoginClient _authLogin;
    private readonly IJwtTokenClient _jwt;
    private readonly IPasskeyClient _passkey;
    private readonly ISmartOtpClient _smartOtp;
    private readonly IGrantProtector _grantProtector;
    private readonly ISecurityGrantRepository _grants;
    private readonly ISecurityUserRepository _users;
    private readonly IOutboxIntentWriter _outbox;
    private readonly IIdGenerator _ids;
    private readonly IClock _clock;
    private readonly ILocalUnitOfWork _unitOfWork;

    public DeleteOwnAccountCommandHandler(
        IAuthLoginClient authLogin,
        IJwtTokenClient jwt,
        IPasskeyClient passkey,
        ISmartOtpClient smartOtp,
        IGrantProtector grantProtector,
        ISecurityGrantRepository grants,
        ISecurityUserRepository users,
        IOutboxIntentWriter outbox,
        IIdGenerator ids,
        ILocalUnitOfWork unitOfWork,
        IAuditIntentWriter audit,
        IClock clock)
        : base(audit, clock)
    {
        _authLogin = authLogin;
        _jwt = jwt;
        _passkey = passkey;
        _smartOtp = smartOtp;
        _grantProtector = grantProtector;
        _grants = grants;
        _users = users;
        _outbox = outbox;
        _ids = ids;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DeleteOwnAccountResult>> HandleAsync(DeleteOwnAccountCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty)
        {
            return Result<DeleteOwnAccountResult>.Failure(Error.Authentication(SecurityErrorCodes.CallerRequired, "Authenticated user is required."));
        }

        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            return Result<DeleteOwnAccountResult>.Failure(Error.Validation(SecurityErrorCodes.ValidationFailed, "Idempotency-Key header is required."));
        }

        if (string.IsNullOrWhiteSpace(request.CorrelationId))
        {
            return Result<DeleteOwnAccountResult>.Failure(Error.Validation(SecurityErrorCodes.ValidationFailed, "X-Correlation-Id header is required."));
        }

        var stepUp = await StepUpGrantVerifier.VerifyAndConsumeAsync(request.StepUpGrant, ReasonCodeValue, request.UserId, _grantProtector, _grants, _clock, cancellationToken);
        if (stepUp.IsFailure)
        {
            await AuditAsync(request.UserId, "account.delete_self", "blocked", stepUp.Error?.Code ?? "step_up_required", cancellationToken);
            return Result<DeleteOwnAccountResult>.Failure(stepUp.Error!);
        }

        var now = Now;
        var user = await _users.FindByIdAsync(request.UserId, cancellationToken);
        if (user?.Status == UserSecurityStatus.Disabled)
        {
            await AuditAsync(request.UserId, "account.delete_self", "replayed", ReasonCodeValue, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
            return Result<DeleteOwnAccountResult>.Success(new DeleteOwnAccountResult(request.UserId, now));
        }

        var authLogin = await _authLogin.ChangeAccountStateAsync(new AuthLoginAccountStateChangeRequest(request.UserId, AccountStateTargetState.Disabled.ToString(), ReasonCodeValue, "User requested account deletion."), cancellationToken);
        if (authLogin.IsFailure || authLogin.Value is null)
        {
            await AuditAsync(request.UserId, "account.delete_self", "failed", authLogin.Error?.Code ?? "auth_login_failed", cancellationToken);
            return ChildFailure<DeleteOwnAccountResult>(authLogin.Error ?? new ChildCallError(SecurityErrorCodes.DownstreamUnavailable, "Auth Login account deletion failed.", true));
        }

        var jwt = await _jwt.RevokeAllSessionsAsync(new JwtRevokeAllRequest(request.UserId, ReasonCodeValue, true), cancellationToken);
        if (jwt.IsFailure || jwt.Value is null)
        {
            await AuditAsync(request.UserId, "account.delete_self", "partial", jwt.Error?.Code ?? "jwt_revoke_all_failed", cancellationToken);
            return ChildFailure<DeleteOwnAccountResult>(jwt.Error ?? new ChildCallError(SecurityErrorCodes.DownstreamUnavailable, "JWT session revoke failed.", true));
        }

        var passkey = await _passkey.RevokeAllAsync(new PasskeyRevokeAllRequest(request.UserId, ReasonCodeValue), cancellationToken);
        if (passkey.IsFailure || passkey.Value is null)
        {
            await AuditAsync(request.UserId, "account.delete_self", "partial", passkey.Error?.Code ?? "passkey_revoke_all_failed", cancellationToken);
            return ChildFailure<DeleteOwnAccountResult>(passkey.Error ?? new ChildCallError(SecurityErrorCodes.DownstreamUnavailable, "Passkey revoke-all failed.", true));
        }

        var smartOtp = await _smartOtp.RevokeAllDevicesAsync(new SmartOtpRevokeAllDevicesRequest(request.UserId, ReasonCodeValue), cancellationToken);
        if (smartOtp.IsFailure || smartOtp.Value is null)
        {
            await AuditAsync(request.UserId, "account.delete_self", "partial", smartOtp.Error?.Code ?? "smart_otp_revoke_all_failed", cancellationToken);
            return ChildFailure<DeleteOwnAccountResult>(smartOtp.Error ?? new ChildCallError(SecurityErrorCodes.DownstreamUnavailable, "Smart OTP revoke-all failed.", true));
        }

        if (user is null)
        {
            user = SecurityUser.Create(request.UserId, now);
            await _users.AddAsync(user, cancellationToken);
        }

        user.Disable(ReasonCode.From(ReasonCodeValue), now);
        await AuditAsync(request.UserId, "account.delete_self", "succeeded", ReasonCodeValue, cancellationToken);
        await _outbox.AppendAsync(new OutboxIntent(
            _ids.NewId(),
            "ACCOUNT_DELETED",
            nameof(SecurityUser),
            request.UserId,
            JsonSerializer.Serialize(new AccountDeletedOutboxPayload(request.UserId, now, ReasonCodeValue, request.CorrelationId)),
            now), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return Result<DeleteOwnAccountResult>.Success(new DeleteOwnAccountResult(request.UserId, now));
    }

    private sealed record AccountDeletedOutboxPayload(Guid UserId, DateTimeOffset DeletedAtUtc, string ReasonCode, string CorrelationId);
}

public sealed class ListSessionsQueryHandler : IRequestHandler<ListSessionsQuery, IReadOnlyCollection<SessionSummaryResult>>
{
    private readonly IJwtTokenClient _jwt;

    public ListSessionsQueryHandler(IJwtTokenClient jwt) => _jwt = jwt;

    public async Task<Result<IReadOnlyCollection<SessionSummaryResult>>> HandleAsync(ListSessionsQuery request, CancellationToken cancellationToken)
    {
        var child = await _jwt.ListSessionsAsync(request.UserId, cancellationToken);
        return child.IsSuccess && child.Value is not null
            ? Result<IReadOnlyCollection<SessionSummaryResult>>.Success(child.Value.Select(AccountSecuritySliceMapper.ToSession).ToArray())
            : CoreSliceHandler.ChildFailure<IReadOnlyCollection<SessionSummaryResult>>(child.Error ?? new ChildCallError(SecurityErrorCodes.DownstreamUnavailable, "JWT session list failed.", true));
    }
}

public sealed class LogoutAllSessionsCommandHandler : CoreSliceHandler, IRequestHandler<LogoutAllSessionsCommand, LogoutAllSessionsResult>
{
    private readonly IJwtTokenClient _jwt;

    public LogoutAllSessionsCommandHandler(IJwtTokenClient jwt, IAuditIntentWriter audit, IClock clock)
        : base(audit, clock) => _jwt = jwt;

    public async Task<Result<LogoutAllSessionsResult>> HandleAsync(LogoutAllSessionsCommand request, CancellationToken cancellationToken)
    {
        var child = await _jwt.RevokeAllSessionsAsync(new JwtRevokeAllRequest(request.UserId, request.ReasonCode, request.IncludeCurrentSession, request.CurrentSessionId), cancellationToken);
        if (child.IsFailure || child.Value is null)
        {
            await AuditAsync(request.UserId, "session.logout_all", "failed", child.Error?.Code ?? "jwt_revoke_all_failed", cancellationToken);
            return ChildFailure<LogoutAllSessionsResult>(child.Error ?? new ChildCallError(SecurityErrorCodes.DownstreamUnavailable, "JWT revoke all sessions failed.", true));
        }

        await AuditAsync(request.UserId, "session.logout_all", "succeeded", request.ReasonCode, cancellationToken);
        return Result<LogoutAllSessionsResult>.Success(new LogoutAllSessionsResult(child.Value.RevokedCount, child.Value.RevokedAtUtc));
    }
}

public sealed class RenamePasskeyCommandHandler : CoreSliceHandler, IRequestHandler<RenamePasskeyCommand, PasskeyStateResult>
{
    private readonly IPasskeyClient _passkey;

    public RenamePasskeyCommandHandler(IPasskeyClient passkey, IAuditIntentWriter audit, IClock clock)
        : base(audit, clock) => _passkey = passkey;

    public async Task<Result<PasskeyStateResult>> HandleAsync(RenamePasskeyCommand request, CancellationToken cancellationToken)
    {
        var child = await _passkey.RenameAsync(new PasskeyRenameRequest(request.UserId, request.PasskeyId, request.DisplayName), cancellationToken);
        if (child.IsFailure)
        {
            await AuditAsync(request.UserId, "passkey.rename", "failed", child.Error?.Code ?? "passkey_rename_failed", cancellationToken);
            return ChildFailure<PasskeyStateResult>(child.Error ?? new ChildCallError(SecurityErrorCodes.DownstreamUnavailable, "Passkey rename failed.", true));
        }

        await AuditAsync(request.UserId, "passkey.rename", "succeeded", "passkey_renamed", cancellationToken);
        return Result<PasskeyStateResult>.Success(new PasskeyStateResult(request.PasskeyId, true, Now));
    }
}

public sealed class SetPasskeyEnabledCommandHandler : CoreSliceHandler, IRequestHandler<SetPasskeyEnabledCommand, PasskeyStateResult>
{
    private readonly IPasskeyClient _passkey;

    public SetPasskeyEnabledCommandHandler(IPasskeyClient passkey, IAuditIntentWriter audit, IClock clock)
        : base(audit, clock) => _passkey = passkey;

    public async Task<Result<PasskeyStateResult>> HandleAsync(SetPasskeyEnabledCommand request, CancellationToken cancellationToken)
    {
        var child = await _passkey.SetEnabledAsync(new PasskeyStateChangeRequest(request.UserId, request.PasskeyId, request.Enabled, request.ReasonCode), cancellationToken);
        if (child.IsFailure)
        {
            await AuditAsync(request.UserId, request.Enabled ? "passkey.enable" : "passkey.disable", "failed", child.Error?.Code ?? "passkey_state_failed", cancellationToken);
            return ChildFailure<PasskeyStateResult>(child.Error ?? new ChildCallError(SecurityErrorCodes.DownstreamUnavailable, "Passkey state change failed.", true));
        }

        await AuditAsync(request.UserId, request.Enabled ? "passkey.enable" : "passkey.disable", "succeeded", request.ReasonCode, cancellationToken);
        return Result<PasskeyStateResult>.Success(new PasskeyStateResult(request.PasskeyId, request.Enabled, Now));
    }
}
