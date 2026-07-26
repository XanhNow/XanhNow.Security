using XanhNow.Security.Application.Abstractions.Audit;
using XanhNow.Security.Application.Abstractions.ChildApps;
using XanhNow.Security.Application.Abstractions.ChildApps.AuthLogin;
using XanhNow.Security.Application.Abstractions.ChildApps.Jwt;
using XanhNow.Security.Application.Abstractions.ChildApps.Passkey;
using XanhNow.Security.Application.Abstractions.Time;
using XanhNow.Security.Application.Common.Requests;
using XanhNow.Security.Application.Common.Results;

namespace XanhNow.Security.Application.Core;

internal static class AccountSecuritySliceMapper
{
    public static AccountSecurityOperationResult ToOperation(AuthLoginOperationResult operation, DateTimeOffset acceptedAtUtc)
        => new(operation.OperationId, operation.OperationType, operation.Status, operation.CurrentStep, acceptedAtUtc);

    public static AccountStateResult ToAccountState(AuthLoginAccountStateChangeResult state)
        => new(state.UserId, state.Status, state.ChangedAtUtc);

    public static SecurityProfileResult ToSecurityProfile(AuthLoginAccountStatusResult status, bool hasPasskey)
        => new(status.UserId, status.MaskedPhoneNumber, status.Status, "Unknown", hasPasskey, false, false, status.UpdatedAtUtc);

    public static SessionSummaryResult ToSession(JwtSessionDescriptor session)
        => new(session.SessionId, session.UserId, session.Status, session.DeviceName, session.Platform, session.CreatedAtUtc, session.LastSeenAtUtc, session.ExpiresAtUtc);
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
    private readonly IAuthLoginClient _authLogin;

    public StartPhoneChangeCommandHandler(IAuthLoginClient authLogin, IAuditIntentWriter audit, IClock clock)
        : base(audit, clock) => _authLogin = authLogin;

    public async Task<Result<AccountSecurityOperationResult>> HandleAsync(StartPhoneChangeCommand request, CancellationToken cancellationToken)
    {
        var child = await _authLogin.StartPhoneChangeAsync(new AuthLoginPhoneChangeStartRequest(request.UserId, request.NewPhoneNumber, request.StepUpGrant, request.ReasonCode), cancellationToken);
        if (child.IsFailure || child.Value is null)
        {
            await AuditAsync(request.UserId, "phone.change.start", "failed", child.Error?.Code ?? "auth_login_failed", cancellationToken);
            return ChildFailure<AccountSecurityOperationResult>(child.Error ?? new ChildCallError(SecurityErrorCodes.DownstreamUnavailable, "Auth Login phone change start failed.", true));
        }

        await AuditAsync(request.UserId, "phone.change.start", "accepted", request.ReasonCode, cancellationToken);
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

    public GetSecurityProfileQueryHandler(IAuthLoginClient authLogin, IPasskeyClient passkey)
    {
        _authLogin = authLogin;
        _passkey = passkey;
    }

    public async Task<Result<SecurityProfileResult>> HandleAsync(GetSecurityProfileQuery request, CancellationToken cancellationToken)
    {
        var status = await _authLogin.GetAccountStatusAsync(request.UserId, cancellationToken);
        if (status.IsFailure || status.Value is null)
        {
            return CoreSliceHandler.ChildFailure<SecurityProfileResult>(status.Error ?? new ChildCallError(SecurityErrorCodes.DownstreamUnavailable, "Auth Login account status failed.", true));
        }

        var passkeys = await _passkey.ListAsync(request.UserId, cancellationToken);
        var hasPasskey = passkeys.IsSuccess && passkeys.Value?.Any(x => !x.Revoked) == true;
        return Result<SecurityProfileResult>.Success(AccountSecuritySliceMapper.ToSecurityProfile(status.Value, hasPasskey));
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
        var child = await _jwt.RevokeAllSessionsAsync(new JwtRevokeAllRequest(request.UserId, request.ReasonCode, request.IncludeCurrentSession), cancellationToken);
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
