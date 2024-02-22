namespace Bitwarden.SecureSync.Interfaces;

public interface ISynchronisationLogic
{
    Task RunSynchronisationAsync(CancellationToken cancellationToken = default);
}