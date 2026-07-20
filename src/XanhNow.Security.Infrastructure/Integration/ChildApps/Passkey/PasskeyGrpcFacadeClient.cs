using XanhNow.Security.Application.Abstractions.ChildApps;
using XanhNow.Security.Application.Abstractions.ChildApps.Passkey;
using XanhNow.Security.Infrastructure.Integration.ChildApps;
using XanhNow.Security.Infrastructure.Integration.Options;

namespace XanhNow.Security.Infrastructure.Integration.ChildApps.Passkey;

internal sealed class PasskeyGrpcFacadeClient : ChildAppJsonClient, IPasskeyClient
{
    public PasskeyGrpcFacadeClient(HttpClient http, SecurityIntegrationOptions options)
        : base(http, options.Passkey, options.ContractVersion)
    {
    }

    public ValueTask<ChildCallResult<PasskeyBeginResult>> BeginAsync(PasskeyBeginRequest request, CancellationToken cancellationToken)
        => PostAsync<PasskeyBeginRequest, PasskeyBeginResult>("/internal/v1/passkeys/ceremonies/begin", request, cancellationToken);

    public ValueTask<ChildCallResult<PasskeyFinishResult>> FinishAsync(PasskeyFinishRequest request, CancellationToken cancellationToken)
        => PostAsync<PasskeyFinishRequest, PasskeyFinishResult>("/internal/v1/passkeys/ceremonies/finish", request, cancellationToken);

    public ValueTask<ChildCallResult<IReadOnlyCollection<PasskeyDescriptor>>> ListAsync(Guid userId, CancellationToken cancellationToken)
        => GetAsync<IReadOnlyCollection<PasskeyDescriptor>>($"/internal/v1/passkeys/users/{userId}", cancellationToken);

    public ValueTask<ChildCallResult<bool>> RevokeAsync(Guid userId, string credentialId, CancellationToken cancellationToken)
        => PostAsync<object, bool>($"/internal/v1/passkeys/users/{userId}/credentials/{Uri.EscapeDataString(credentialId)}/revoke", new { }, cancellationToken);
}
