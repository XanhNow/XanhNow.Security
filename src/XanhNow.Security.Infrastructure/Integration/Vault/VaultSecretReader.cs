namespace XanhNow.Security.Infrastructure.Integration.Vault;

public sealed record VaultSecretReference(string Path, string Field);

public interface IVaultSecretReader
{
    ValueTask<string?> ReadFieldAsync(VaultSecretReference reference, CancellationToken cancellationToken);
}

internal sealed class VaultSecretReader : IVaultSecretReader
{
    public ValueTask<string?> ReadFieldAsync(VaultSecretReference reference, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(reference.Path);
        ArgumentException.ThrowIfNullOrWhiteSpace(reference.Field);
        return ValueTask.FromResult<string?>(null);
    }
}
