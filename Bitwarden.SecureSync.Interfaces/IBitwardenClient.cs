namespace Bitwarden.SecureSync.Interfaces;

public interface IBitwardenClient : IDisposable
{
    Task UnlockVault(CancellationToken cancellationToken = default);

    Task ExportVault(string exportDirectory, string encryptionKey, bool includeOrganisationItems,
        CancellationToken cancellationToken = default);
}