using XanhNow.Security.Contracts.Common.Attributes;

namespace XanhNow.Security.Contracts.V1.SmartOtp;

public sealed record BeginSmartOtpEnrollmentRequest(string DeviceName);
public sealed record BeginSmartOtpEnrollmentResponse(string EnrollmentId, [property: SensitiveData("totp-provisioning-uri")] string ProvisioningUri, [property: SensitiveData("totp-manual-entry-key")] string ManualEntryKey, DateTimeOffset ExpiresAtUtc);
public sealed record ConfirmSmartOtpEnrollmentRequest(string EnrollmentId, [property: SensitiveData("otp")] string Otp);
public sealed record SmartOtpDeviceStateResponse(string DeviceId, bool IsEnabled, DateTimeOffset UpdatedAtUtc);
public sealed record SmartOtpDeviceSummaryResponse(string DeviceId, string DeviceName, bool IsEnabled, DateTimeOffset CreatedAtUtc, DateTimeOffset? LastUsedAtUtc);
public sealed record RevokeSmartOtpDeviceRequest(string ReasonCode);
public sealed record StartStepUpRequest(string Purpose, string TransactionDigest, DateTimeOffset ExpiresAtUtc);
public sealed record StepUpChallengeResponse(string ChallengeId, string Purpose, DateTimeOffset ExpiresAtUtc);
public sealed record VerifyStepUpRequest(string ChallengeId, [property: SensitiveData("otp")] string Otp);
public sealed record StepUpGrantResponse(string ChallengeId, [property: SensitiveData("step-up-grant")] string StepUpGrant, string Purpose, DateTimeOffset ExpiresAtUtc);
