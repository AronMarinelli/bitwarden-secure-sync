namespace Bitwarden.SecureSync.Interfaces;

public interface IBitwardenClientDownloadLogic
{
    Task EnsureClientAvailabilityAsync(CancellationToken cancellationToken = default);
}