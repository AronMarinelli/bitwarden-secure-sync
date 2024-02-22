namespace Bitwarden.SecureSync.Models.Configuration;

public class SyncConfiguration
{
    public string CronSchedule { get; set; }
    public bool RunOnStartup { get; set; }
    public bool IncludeOrganisationItems { get; set; }
    public bool EncryptUsingPassword { get; set; }
    public string EncryptionKey { get; set; }
    public int? FileRetention { get; set; }
}