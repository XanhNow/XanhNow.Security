using XanhNow.Security.Contracts.Common.Attributes;

namespace XanhNow.Security.Contracts.V1.Password;

public sealed record ChangePasswordRequest([property: SensitiveData("password")] string CurrentPassword, [property: SensitiveData("password")] string NewPassword, string ReasonCode);
public sealed record StartPasswordResetRequest(string PhoneNumber);
public sealed record CompletePasswordResetRequest(string ResetOperationId, [property: SensitiveData("password")] string NewPassword);
public sealed record ForcePasswordChangeRequest(Guid UserId, [property: SensitiveData("password")] string NewPassword, string ReasonCode);
