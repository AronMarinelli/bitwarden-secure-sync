using NCrontab;

namespace Bitwarden.SecureSync.Models.Configuration;

public class SyncConfiguration
{
    private const string DEFAULT_CRON_SCHEDULE = "0 0 * * *";
    private const string DEFAULT_DATA_DIRECTORY = "data";
    private const int DEFAULT_FILE_RETENTION = 7;

    public string CronSchedule { get; set; }
    public bool RunOnStartup { get; set; }
    public bool IncludeOrganisationItems { get; set; }
    public bool EncryptUsingCustomKey { get; set; }
    public string EncryptionKey { get; set; }
    public int? FileRetention { get; set; }
    public string DataDirectory { get; set; }

    public void Validate()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;

        if (CrontabSchedule.TryParse(CronSchedule) is null)
        {
            Console.WriteLine(
                $"Sync config validation: The cron schedule could not be parsed (current value: {CronSchedule}). Falling back on default value ({DEFAULT_CRON_SCHEDULE}).");
            CronSchedule = DEFAULT_CRON_SCHEDULE;
        }

        if (FileRetention <= 0)
        {
            Console.WriteLine(
                $"Sync config validation: File retention should be null or larger than 0 (Current value: {FileRetention}). Falling back on default value ({DEFAULT_FILE_RETENTION}).");
            FileRetention = DEFAULT_FILE_RETENTION;
        }

        if (!EncryptUsingCustomKey && !string.IsNullOrWhiteSpace(EncryptionKey))
            Console.WriteLine(
                "Sync config validation: Custom key encryption is disabled, but an encryption key is defined. Is your configuration correct?");

        if (EncryptUsingCustomKey && string.IsNullOrWhiteSpace(EncryptionKey))
        {
            Console.WriteLine(
                "Sync config validation: Custom key encryption is enabled, but no encryption key was supplied. Using account key encryption for export.");
            EncryptUsingCustomKey = false;
        }

        if (EncryptUsingCustomKey && !string.IsNullOrWhiteSpace(EncryptionKey) && EncryptionKey.Length < 8)
            Console.WriteLine(
                "Sync config validation: Using an insecure value to encrypt your exported data is not recommended! (Custom encryption key less than 8 characters in length)");

        if (!string.IsNullOrWhiteSpace(DataDirectory) && !Path.IsPathFullyQualified(DataDirectory))
            DataDirectory = Path.GetFullPath(DataDirectory);
        else if (string.IsNullOrWhiteSpace(DataDirectory)) DataDirectory = Path.GetFullPath(DEFAULT_DATA_DIRECTORY);

        Console.ResetColor();

        Console.WriteLine($"Using {DataDirectory} for storage of exports.");
        Directory.CreateDirectory(DataDirectory);
    }

    public static SyncConfiguration GetSampleConfiguration()
    {
        return new SyncConfiguration
        {
            CronSchedule = "0 0 * * *",
            RunOnStartup = true,
            IncludeOrganisationItems = false,
            EncryptUsingCustomKey = false,
            EncryptionKey = null,
            FileRetention = 7,
            DataDirectory = "data"
        };
    }

    public CrontabSchedule GetCronSchedule()
    {
        return CrontabSchedule.Parse(CronSchedule);
    }
}