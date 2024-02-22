namespace Bitwarden.SecureSync.Interfaces;

public interface IClientDownloadLogic
{
    Task EnsureClientAvailabilityAsync();
}