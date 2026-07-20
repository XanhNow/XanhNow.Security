using XanhNow.Security.Application.Abstractions.ChildApps;

namespace XanhNow.Security.Application.Abstractions.ChildApps.Passkey;

public sealed record PasskeyBeginRequest(Guid UserId, string Purpose);
public sealed record PasskeyBeginResult(string CeremonyId, string PublicOptionsJson);
public sealed record PasskeyFinishRequest(string CeremonyId, string ClientResponseJson);
public sealed record PasskeyFinishResult(Guid UserId, string CredentialId, string AssuranceLevel);
public sealed record PasskeyDescriptor(string CredentialId, string DisplayName, bool Revoked);

public interface IPasskeyClient
{
    ValueTask<ChildCallResult<PasskeyBeginResult>> BeginAsync(PasskeyBeginRequest request, CancellationToken cancellationToken);
    ValueTask<ChildCallResult<PasskeyFinishResult>> FinishAsync(PasskeyFinishRequest request, CancellationToken cancellationToken);
    ValueTask<ChildCallResult<IReadOnlyCollection<PasskeyDescriptor>>> ListAsync(Guid userId, CancellationToken cancellationToken);
    ValueTask<ChildCallResult<bool>> RevokeAsync(Guid userId, string credentialId, CancellationToken cancellationToken);
}
