namespace Bitwarden.SecureSync.Interfaces.Client;

public interface IBitwardenClient : IDisposable
{
    Task UnlockVault(CancellationToken cancellationToken = default);

    Task ExportVault(string exportDirectory, string encryptionKey, bool includeOrganisationItems,
        CancellationToken cancellationToken = default);
}