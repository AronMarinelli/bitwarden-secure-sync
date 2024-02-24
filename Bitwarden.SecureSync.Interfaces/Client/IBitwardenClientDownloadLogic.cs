namespace Bitwarden.SecureSync.Interfaces.Client;

public interface IBitwardenClientDownloadLogic
{
    Task EnsureClientAvailabilityAsync(CancellationToken cancellationToken = default);
}