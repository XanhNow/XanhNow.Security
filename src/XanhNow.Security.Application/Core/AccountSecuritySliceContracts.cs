using XanhNow.Security.Application.Common.Requests;

namespace XanhNow.Security.Application.Core;

public sealed record AccountSecurityOperationResult(Guid OperationId, string OperationType, string Status, string CurrentStep, DateTimeOffset AcceptedAtUtc);
public sealed record AccountStateResult(Guid UserId, string Status, DateTimeOffset ChangedAtUtc);
public sealed record SecurityProfileResult(Guid UserId, string MaskedPhoneNumber, string Status, string DeviceTrustLevel, bool HasPasskey, bool HasSmartOtp, bool IsStale, DateTimeOffset UpdatedAtUtc);
public sealed record SessionSummaryResult(string SessionId, Guid UserId, string Status, string? DeviceName, string? Platform, DateTimeOffset CreatedAtUtc, DateTimeOffset LastSeenAtUtc, DateTimeOffset ExpiresAtUtc);
public sealed record LogoutAllSessionsResult(int RevokedCount, DateTimeOffset RevokedAtUtc);

public enum AccountStateTargetState
{
    Locked,
    Active,
    Disabled
}

public sealed record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword, string ReasonCode) : ICommand<AccountSecurityOperationResult>;
public sealed record StartPasswordResetCommand(string PhoneNumber) : ICommand<AccountSecurityOperationResult>;
public sealed record CompletePasswordResetCommand(string ResetOperationId, string NewPassword) : ICommand<AccountSecurityOperationResult>;
public sealed record ForcePasswordChangeCommand(Guid UserId, string NewPassword, string ReasonCode) : ICommand<AccountStateResult>;

public sealed record StartPhoneChangeCommand(Guid UserId, string NewPhoneNumber, string StepUpGrant, string ReasonCode) : ICommand<AccountSecurityOperationResult>;
public sealed record ConfirmPhoneChangeCommand(Guid UserId, Guid OperationId, string Otp) : ICommand<AccountSecurityOperationResult>;
public sealed record CancelPhoneChangeCommand(Guid UserId, Guid OperationId, string ReasonCode) : ICommand<AccountSecurityOperationResult>;

public sealed record GetSecurityProfileQuery(Guid UserId) : IQuery<SecurityProfileResult>;
public sealed record ChangeAccountStateCommand(Guid UserId, AccountStateTargetState TargetState, string ReasonCode, string? Comment) : ICommand<AccountStateResult>;

public sealed record ListSessionsQuery(Guid UserId) : IQuery<IReadOnlyCollection<SessionSummaryResult>>;
public sealed record LogoutAllSessionsCommand(Guid UserId, string ReasonCode, bool IncludeCurrentSession) : ICommand<LogoutAllSessionsResult>;

public sealed record RenamePasskeyCommand(Guid UserId, string PasskeyId, string DisplayName) : ICommand<PasskeyStateResult>;
public sealed record SetPasskeyEnabledCommand(Guid UserId, string PasskeyId, bool Enabled, string ReasonCode) : ICommand<PasskeyStateResult>;
