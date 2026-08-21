using XanhNow.Security.Application.Abstractions.ChildApps;

namespace XanhNow.Security.Application.Abstractions.ChildApps.AuthLogin;

public sealed record AuthLoginRegisterRequest(string PhoneNumber, SensitiveString Password, string DisplayName);
public sealed record AuthLoginRegisterResult(Guid UserId);
public sealed record AuthLoginPasswordRequest(string PhoneNumber, SensitiveString Password);
public sealed record AuthLoginPasswordResult(Guid UserId, string AssuranceLevel);
public sealed record AuthLoginOperationResult(Guid OperationId, string OperationType, string Status, string CurrentStep);
public sealed record AuthLoginAccountStatusResult(Guid UserId, string MaskedPhoneNumber, string Status, DateTimeOffset UpdatedAtUtc);
public sealed record AuthLoginAccountLookupRequest(string PhoneNumber);
public sealed record AuthLoginAccountStateChangeResult(Guid UserId, string Status, DateTimeOffset ChangedAtUtc);
public sealed record AuthLoginChangePasswordRequest(Guid UserId, SensitiveString CurrentPassword, SensitiveString NewPassword, string ReasonCode);
public sealed record AuthLoginPasswordResetStartRequest(string PhoneNumber);
public sealed record AuthLoginPasswordResetCompleteRequest(string ResetOperationId, SensitiveString NewPassword);
public sealed record AuthLoginForcePasswordChangeRequest(Guid UserId, SensitiveString NewPassword, string ReasonCode);
public sealed record AuthLoginPhoneChangeStartRequest(Guid UserId, string NewPhoneNumber, string StepUpGrant, string ReasonCode);
public sealed record AuthLoginPhoneChangeConfirmRequest(Guid UserId, Guid OperationId, SensitiveString Otp);
public sealed record AuthLoginPhoneChangeCancelRequest(Guid UserId, Guid OperationId, string ReasonCode);
public sealed record AuthLoginAccountStateChangeRequest(Guid UserId, string TargetState, string ReasonCode, string? Comment);

public interface IAuthLoginClient
{
    ValueTask<ChildCallResult<AuthLoginRegisterResult>> RegisterAsync(AuthLoginRegisterRequest request, CancellationToken cancellationToken);
    ValueTask<ChildCallResult<AuthLoginPasswordResult>> LoginWithPasswordAsync(AuthLoginPasswordRequest request, CancellationToken cancellationToken);
    ValueTask<ChildCallResult<AuthLoginOperationResult>> ChangePasswordAsync(AuthLoginChangePasswordRequest request, CancellationToken cancellationToken);
    ValueTask<ChildCallResult<AuthLoginOperationResult>> StartPasswordResetAsync(AuthLoginPasswordResetStartRequest request, CancellationToken cancellationToken);
    ValueTask<ChildCallResult<AuthLoginOperationResult>> CompletePasswordResetAsync(AuthLoginPasswordResetCompleteRequest request, CancellationToken cancellationToken);
    ValueTask<ChildCallResult<AuthLoginAccountStateChangeResult>> ForcePasswordChangeAsync(AuthLoginForcePasswordChangeRequest request, CancellationToken cancellationToken);
    ValueTask<ChildCallResult<AuthLoginOperationResult>> StartPhoneChangeAsync(AuthLoginPhoneChangeStartRequest request, CancellationToken cancellationToken);
    ValueTask<ChildCallResult<AuthLoginOperationResult>> ConfirmPhoneChangeAsync(AuthLoginPhoneChangeConfirmRequest request, CancellationToken cancellationToken);
    ValueTask<ChildCallResult<AuthLoginOperationResult>> CancelPhoneChangeAsync(AuthLoginPhoneChangeCancelRequest request, CancellationToken cancellationToken);
    ValueTask<ChildCallResult<AuthLoginAccountStatusResult>> GetAccountStatusAsync(Guid userId, CancellationToken cancellationToken);
    ValueTask<ChildCallResult<AuthLoginAccountStatusResult>> GetAccountByPhoneAsync(AuthLoginAccountLookupRequest request, CancellationToken cancellationToken);
    ValueTask<ChildCallResult<AuthLoginAccountStateChangeResult>> ChangeAccountStateAsync(AuthLoginAccountStateChangeRequest request, CancellationToken cancellationToken);
}
