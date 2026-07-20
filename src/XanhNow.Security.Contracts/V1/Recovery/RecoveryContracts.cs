using XanhNow.Security.Contracts.Common.Enums;

namespace XanhNow.Security.Contracts.V1.Recovery;

public sealed record StartRecoveryCaseRequest(Guid? UserId, string RecoveryType, string ReasonCode, string? MaskedPhoneNumber);
public sealed record ReportLostDeviceRequest(Guid? UserId, string DeviceReference, string ReasonCode);
public sealed record RecoveryCaseResponse(Guid RecoveryCaseId, Guid? UserId, string RecoveryType, OperationStatusContract Status, string CurrentStep, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc);
