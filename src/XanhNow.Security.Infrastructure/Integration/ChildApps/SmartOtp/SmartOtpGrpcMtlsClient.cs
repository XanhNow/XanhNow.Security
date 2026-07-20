using XanhNow.Security.Application.Abstractions.ChildApps;
using XanhNow.Security.Application.Abstractions.ChildApps.SmartOtp;
using XanhNow.Security.Infrastructure.Integration.ChildApps;
using XanhNow.Security.Infrastructure.Integration.Options;

namespace XanhNow.Security.Infrastructure.Integration.ChildApps.SmartOtp;

internal sealed class SmartOtpGrpcMtlsClient : ChildAppJsonClient, ISmartOtpClient
{
    public SmartOtpGrpcMtlsClient(HttpClient http, SecurityIntegrationOptions options)
        : base(http, options.SmartOtp, options.ContractVersion)
    {
    }

    public ValueTask<ChildCallResult<SmartOtpBindBeginResult>> BeginBindAsync(SmartOtpBindBeginRequest request, CancellationToken cancellationToken)
        => PostAsync<SmartOtpBindBeginRequest, SmartOtpBindBeginResult>("/internal/v1/smart-otp/devices/bind/begin", request, cancellationToken);

    public ValueTask<ChildCallResult<bool>> FinishBindAsync(SmartOtpBindFinishRequest request, CancellationToken cancellationToken)
        => PostAsync<SmartOtpBindFinishRequest, bool>("/internal/v1/smart-otp/devices/bind/finish", request, cancellationToken);

    public ValueTask<ChildCallResult<SmartOtpChallengeResult>> CreateChallengeAsync(SmartOtpChallengeRequest request, CancellationToken cancellationToken)
        => PostAsync<SmartOtpChallengeRequest, SmartOtpChallengeResult>("/internal/v1/smart-otp/challenges", request, cancellationToken);

    public ValueTask<ChildCallResult<SmartOtpVerifyResult>> VerifyAsync(SmartOtpVerifyRequest request, CancellationToken cancellationToken)
        => PostAsync<SmartOtpVerifyRequest, SmartOtpVerifyResult>("/internal/v1/smart-otp/challenges/verify", request, cancellationToken);
}
