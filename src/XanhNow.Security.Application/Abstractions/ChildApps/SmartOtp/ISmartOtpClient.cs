namespace XanhNow.Security.Application.Abstractions.ChildApps.SmartOtp;

public sealed record SmartOtpBindBeginRequest(Guid UserId, string DeviceName, string Platform, string AppInstanceIdHashBase64, string KeyAlgorithm, string CandidatePublicKeySpkiBase64, string CandidatePublicKeyThumbprintBase64);
public sealed record SmartOtpBindBeginResult(string BindingId, string ServerChallengeBase64, int ChallengeFormatVersion, DateTimeOffset ExpiresAtUtc, string Status);
public sealed record SmartOtpBindFinishRequest(Guid UserId, string BindingId, string ClientNonceBase64, string DeviceSignatureBase64);
public sealed record SmartOtpBindFinishResult(string DeviceId, string DeviceKeyId, string Status, DateTimeOffset BoundAtUtc);
public sealed record SmartOtpChallengeRequest(Guid UserId, string Purpose, string TransactionSummary);
public sealed record SmartOtpChallengeResult(string ChallengeId, DateTimeOffset ExpiresAt);
public sealed record SmartOtpVerifyRequest(string ChallengeId, SensitiveString TotpCode);
public sealed record SmartOtpVerifyResult(Guid UserId, string AssuranceLevel);
public sealed record SmartOtpRevokeAllDevicesRequest(Guid UserId, string ReasonCode);
public sealed record SmartOtpRevokeAllDevicesResult(int RevokedCount, DateTimeOffset RevokedAtUtc);

public interface ISmartOtpClient
{
    ValueTask<ChildCallResult<SmartOtpBindBeginResult>> BeginBindAsync(SmartOtpBindBeginRequest request, CancellationToken cancellationToken);
    ValueTask<ChildCallResult<SmartOtpBindFinishResult>> FinishBindAsync(SmartOtpBindFinishRequest request, CancellationToken cancellationToken);
    ValueTask<ChildCallResult<SmartOtpChallengeResult>> CreateChallengeAsync(SmartOtpChallengeRequest request, CancellationToken cancellationToken);
    ValueTask<ChildCallResult<SmartOtpVerifyResult>> VerifyAsync(SmartOtpVerifyRequest request, CancellationToken cancellationToken);
    ValueTask<ChildCallResult<SmartOtpRevokeAllDevicesResult>> RevokeAllDevicesAsync(SmartOtpRevokeAllDevicesRequest request, CancellationToken cancellationToken);
}
