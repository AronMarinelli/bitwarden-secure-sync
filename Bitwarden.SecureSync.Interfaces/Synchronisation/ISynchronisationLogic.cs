namespace Bitwarden.SecureSync.Interfaces.Synchronisation;

public interface ISynchronisationLogic
{
    Task RunSynchronisationAsync(CancellationToken cancellationToken = default);
}