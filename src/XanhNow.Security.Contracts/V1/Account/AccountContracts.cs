using XanhNow.Security.Contracts.Common.Enums;

namespace XanhNow.Security.Contracts.V1.Account;

public sealed record SecurityProfileResponse(Guid UserId, string MaskedPhoneNumber, SecurityStatusContract Status, DeviceTrustLevelContract DeviceTrustLevel, bool HasPasskey, bool HasSmartOtp, bool IsStale, DateTimeOffset UpdatedAtUtc);
public sealed record UpdateSecurityProfileRequest(string? PreferredSecurityLevel, bool? RequireStepUpForImportantActions);
public sealed record AccountStateChangeRequest(string ReasonCode, string? Comment);
public sealed record AccountStateChangeResponse(Guid UserId, SecurityStatusContract Status, DateTimeOffset ChangedAtUtc);
