using XanhNow.Security.Contracts.Common.Attributes;

namespace XanhNow.Security.Contracts.V1.Phone;

public sealed record StartPhoneChangeRequest(string NewPhoneNumber, [property: SensitiveData("step-up-grant")] string StepUpGrant, string ReasonCode);
public sealed record ConfirmPhoneChangeRequest(Guid OperationId, [property: SensitiveData("otp")] string Otp);
public sealed record CancelPhoneChangeRequest(Guid OperationId, string ReasonCode);
public sealed record PhoneChangeHistoryItemResponse(Guid OperationId, string MaskedOldPhoneNumber, string MaskedNewPhoneNumber, string Status, DateTimeOffset RequestedAtUtc, DateTimeOffset? CompletedAtUtc);
