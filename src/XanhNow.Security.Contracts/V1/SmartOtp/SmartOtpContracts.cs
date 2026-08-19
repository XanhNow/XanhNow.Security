using XanhNow.Security.Contracts.Common.Attributes;

namespace XanhNow.Security.Contracts.V1.SmartOtp;

public sealed record BeginSmartOtpEnrollmentRequest(
    string DeviceName,
    string Platform,
    [property: SensitiveData("smart-otp-app-instance-id-hash")] string AppInstanceIdHash,
    string KeyAlgorithm,
    [property: SensitiveData("smart-otp-public-key-spki")] string CandidatePublicKeySpki,
    [property: SensitiveData("smart-otp-public-key-thumbprint")] string CandidatePublicKeyThumbprint);

public sealed record BeginSmartOtpEnrollmentResponse(
    string EnrollmentId,
    [property: SensitiveData("smart-otp-server-challenge")] string ServerChallenge,
    int ChallengeFormatVersion,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    string Status);

public sealed record ConfirmSmartOtpEnrollmentRequest(
    string EnrollmentId,
    [property: SensitiveData("smart-otp-client-nonce")] string ClientNonce,
    [property: SensitiveData("smart-otp-device-signature")] string DeviceSignature);

public sealed record SmartOtpDeviceStateResponse(string DeviceId, string DeviceKeyId, string Status, bool IsEnabled, DateTimeOffset UpdatedAtUtc);

public sealed record SmartOtpDeviceSummaryResponse(string DeviceId, string DeviceName, bool IsEnabled, DateTimeOffset CreatedAtUtc, DateTimeOffset? LastUsedAtUtc);
public sealed record RevokeSmartOtpDeviceRequest(string ReasonCode);
public sealed record StartStepUpRequest(string Purpose, string TransactionDigest, DateTimeOffset ExpiresAtUtc);
public sealed record StepUpChallengeResponse(string ChallengeId, string Purpose, DateTimeOffset ExpiresAtUtc);
public sealed record VerifyStepUpRequest(string ChallengeId, [property: SensitiveData("otp")] string Otp);
public sealed record StepUpGrantResponse(string ChallengeId, [property: SensitiveData("step-up-grant")] string StepUpGrant, string Purpose, DateTimeOffset ExpiresAtUtc);
public sealed record IssueTransactionStepUpGrantRequest(string Audience, string Purpose, string TransactionId, string TransactionDigest, string CanonicalizationVersion, string ChallengeId, [property: SensitiveData("otp")] string Otp);
