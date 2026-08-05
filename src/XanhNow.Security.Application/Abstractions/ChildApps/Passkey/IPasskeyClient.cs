using XanhNow.Security.Application.Abstractions.ChildApps;

namespace XanhNow.Security.Application.Abstractions.ChildApps.Passkey;

public sealed record PasskeyDeviceContext(string? DeviceId, string? DeviceName, string? Platform, string? IpAddress, string? UserAgent);
public sealed record PasskeyBeginRequest(Guid UserId, string Purpose, string? DisplayName, string? LoginIdentifier, PasskeyDeviceContext? Device);
public sealed record PasskeyBeginResult(string CeremonyId, string PublicOptionsJson);
public sealed record PasskeyFinishRequest(Guid UserId, string CeremonyId, string ClientResponseJson, PasskeyDeviceContext? Device);
public sealed record PasskeyFinishResult(Guid UserId, string CredentialId, string AssuranceLevel);
public sealed record PasskeyDescriptor(string CredentialId, string DisplayName, bool Revoked);
public sealed record PasskeyRenameRequest(Guid UserId, string CredentialId, string DisplayName);
public sealed record PasskeyStateChangeRequest(Guid UserId, string CredentialId, bool Enabled, string ReasonCode);
public sealed record PasskeyRevokeAllRequest(Guid UserId, string ReasonCode);
public sealed record PasskeyRevokeAllResult(int RevokedCount, DateTimeOffset RevokedAtUtc);

public interface IPasskeyClient
{
    ValueTask<ChildCallResult<PasskeyBeginResult>> BeginAsync(PasskeyBeginRequest request, CancellationToken cancellationToken);
    ValueTask<ChildCallResult<PasskeyFinishResult>> FinishAsync(PasskeyFinishRequest request, CancellationToken cancellationToken);
    ValueTask<ChildCallResult<IReadOnlyCollection<PasskeyDescriptor>>> ListAsync(Guid userId, CancellationToken cancellationToken);
    ValueTask<ChildCallResult<bool>> RevokeAsync(Guid userId, string credentialId, CancellationToken cancellationToken);
    ValueTask<ChildCallResult<PasskeyRevokeAllResult>> RevokeAllAsync(PasskeyRevokeAllRequest request, CancellationToken cancellationToken);
    ValueTask<ChildCallResult<bool>> RenameAsync(PasskeyRenameRequest request, CancellationToken cancellationToken);
    ValueTask<ChildCallResult<bool>> SetEnabledAsync(PasskeyStateChangeRequest request, CancellationToken cancellationToken);
}
