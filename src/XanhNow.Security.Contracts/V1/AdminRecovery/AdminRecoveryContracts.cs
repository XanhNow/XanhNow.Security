namespace XanhNow.Security.Contracts.V1.AdminRecovery;

public sealed record AdminRecoveryUserStatusResponse(
    Guid UserId,
    string PhoneNumber,
    string MaskedPhoneNumber,
    string Status,
    int PasskeyCredentialCount,
    int SmartOtpDeviceCount,
    DateTimeOffset UpdatedAtUtc);

public sealed record AdminApproveRecoveryRequest(
    Guid RequestId,
    Guid UserId,
    string PhoneNumber,
    string AdminId,
    string Reason);

public sealed record AdminApproveRecoveryResponse(
    Guid RecoveryGrantId,
    string RecoveryGrant,
    DateTimeOffset ExpiresAtUtc,
    string CorrelationId);
