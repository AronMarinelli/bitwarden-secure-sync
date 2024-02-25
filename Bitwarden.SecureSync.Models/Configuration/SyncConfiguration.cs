namespace Bitwarden.SecureSync.Models.Configuration;

public class SyncConfiguration
{
    public string CronSchedule { get; set; }
    public bool RunOnStartup { get; set; }
    public bool IncludeOrganisationItems { get; set; }
    public bool EncryptUsingCustomKey { get; set; }
    public string EncryptionKey { get; set; }
    public int? FileRetention { get; set; }

    public void Validate()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;

        if (FileRetention <= 0)
        {
            Console.WriteLine($"Sync config validation: File retention should be null or larger than 0 (Current value: {FileRetention}). Falling back on default value (null).");
            FileRetention = null;
        }

        if (!EncryptUsingCustomKey && !string.IsNullOrWhiteSpace(EncryptionKey))
        {
            Console.WriteLine("Sync config validation: Custom key encryption is disabled, but an encryption key is defined. Is your configuration correct?");
        }
        
        if (EncryptUsingCustomKey && string.IsNullOrWhiteSpace(EncryptionKey))
        {
            Console.WriteLine("Sync config validation: Custom key encryption is enabled, but no encryption key was supplied. Using account key encryption for export.");
            EncryptUsingCustomKey = false;
        }

        if (EncryptUsingCustomKey && !string.IsNullOrWhiteSpace(EncryptionKey) && EncryptionKey.Length < 8)
        {
            Console.WriteLine("Sync config validation: Using an insecure value to encrypt your exported data is not recommended! (Custom encryption key less than 8 characters in length)");
        }
        
        Console.ResetColor();
    }
}