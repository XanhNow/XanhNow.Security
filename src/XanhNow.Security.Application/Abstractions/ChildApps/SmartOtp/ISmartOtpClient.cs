using XanhNow.Security.Application.Abstractions.ChildApps;

namespace XanhNow.Security.Application.Abstractions.ChildApps.SmartOtp;

public sealed record SmartOtpBindBeginRequest(Guid UserId, string DeviceName);
public sealed record SmartOtpBindBeginResult(string BindingId, string ProvisioningUri);
public sealed record SmartOtpBindFinishRequest(string BindingId, SensitiveString TotpCode);
public sealed record SmartOtpChallengeRequest(Guid UserId, string Purpose, string TransactionSummary);
public sealed record SmartOtpChallengeResult(string ChallengeId, DateTimeOffset ExpiresAt);
public sealed record SmartOtpVerifyRequest(string ChallengeId, SensitiveString TotpCode);
public sealed record SmartOtpVerifyResult(Guid UserId, string AssuranceLevel);

public interface ISmartOtpClient
{
    ValueTask<ChildCallResult<SmartOtpBindBeginResult>> BeginBindAsync(SmartOtpBindBeginRequest request, CancellationToken cancellationToken);
    ValueTask<ChildCallResult<bool>> FinishBindAsync(SmartOtpBindFinishRequest request, CancellationToken cancellationToken);
    ValueTask<ChildCallResult<SmartOtpChallengeResult>> CreateChallengeAsync(SmartOtpChallengeRequest request, CancellationToken cancellationToken);
    ValueTask<ChildCallResult<SmartOtpVerifyResult>> VerifyAsync(SmartOtpVerifyRequest request, CancellationToken cancellationToken);
}
