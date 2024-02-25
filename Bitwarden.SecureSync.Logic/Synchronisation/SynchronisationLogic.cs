using Bitwarden.SecureSync.Interfaces.Client;
using Bitwarden.SecureSync.Interfaces.Synchronisation;
using Bitwarden.SecureSync.Models.Configuration;

namespace Bitwarden.SecureSync.Logic.Synchronisation;

public class SynchronisationLogic : ISynchronisationLogic
{
    private readonly IBitwardenClientFactory _bitwardenClientFactory;
    private readonly SyncConfiguration _syncConfiguration;
    private readonly DirectoryInfo _syncDirectory;
    
    public SynchronisationLogic(IBitwardenClientFactory bitwardenClientFactory, SyncConfiguration syncConfiguration)
    {
        _bitwardenClientFactory = bitwardenClientFactory;
        _syncConfiguration = syncConfiguration;
        
        var syncDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), "data");
        Directory.CreateDirectory(syncDirectoryPath);
        
        _syncDirectory = new DirectoryInfo(syncDirectoryPath);
    }
    
    public async Task RunSynchronisationAsync(CancellationToken cancellationToken = default)
    {
        using var client = _bitwardenClientFactory.CreateClient();
        await client.UnlockVault(cancellationToken);
        
        var currentRunDirectory = Path.Combine(_syncDirectory.FullName, DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH-mm-ss"));
        Directory.CreateDirectory(currentRunDirectory);
        
        await client.ExportVault(
            currentRunDirectory,
            _syncConfiguration.EncryptUsingCustomKey ? _syncConfiguration.EncryptionKey : null,
            _syncConfiguration.IncludeOrganisationItems,
            cancellationToken
        );
        
        if (_syncConfiguration.FileRetention.HasValue)
        {
            var directories = _syncDirectory
                .GetDirectories()
                .OrderByDescending(d => d.Name)
                .Skip(_syncConfiguration.FileRetention.Value)
                .ToList();

            if (directories.Count != 0)
                Console.WriteLine($"Deleting {directories.Count} {(directories.Count == 1 ? "directory" : "directories")} to meet retention policy (max. {_syncConfiguration.FileRetention}).");
            
            foreach (var directory in directories)
            {
                directory.Delete(true);
            }
        }
    }
}