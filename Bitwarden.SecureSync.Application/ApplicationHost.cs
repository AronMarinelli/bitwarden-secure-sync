using Bitwarden.SecureSync.Interfaces.Client;
using Bitwarden.SecureSync.Interfaces.Synchronisation;
using Bitwarden.SecureSync.Models.Configuration;
using Microsoft.Extensions.Hosting;
using NCrontab;

namespace Bitwarden.SecureSync.Application;

public class ApplicationHost(
    IHostApplicationLifetime hostApplicationLifetime,
    IBitwardenClientDownloadLogic clientDownloadLogic,
    ISynchronisationLogic synchronisationLogic,
    SyncConfiguration syncConfiguration
) : IHostedService
{
    private CrontabSchedule? _schedule;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        hostApplicationLifetime.ApplicationStarted.Register(OnStarted);
        hostApplicationLifetime.ApplicationStopping.Register(OnStopping);
        hostApplicationLifetime.ApplicationStopped.Register(OnStopped);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void OnStarted()
    {
        var now = DateTime.Now;
        _schedule = CrontabSchedule.TryParse(syncConfiguration.CronSchedule);
        if (_schedule is null || _schedule.GetNextOccurrences(now, now.AddSeconds(5 * 60 - 1)).Count() > 1)
        {
            Console.WriteLine(
                "Invalid cron schedule defined in configuration. Using default schedule (daily at 00:00).");
            _schedule = CrontabSchedule.Parse("0 0 * * *");
        }

        Console.WriteLine($"Cron schedule: {syncConfiguration.CronSchedule}");

        clientDownloadLogic.EnsureClientAvailabilityAsync(hostApplicationLifetime.ApplicationStopping).GetAwaiter()
            .GetResult();

        Console.WriteLine("Bitwarden Secure Sync tool started.");
        if (syncConfiguration.RunOnStartup)
        {
            Console.WriteLine("Starting initial Bitwarden sync...");
            RunBitwardenSync(hostApplicationLifetime.ApplicationStopping).GetAwaiter().GetResult();
        }
        else
        {
            DelayUntilNextRun(hostApplicationLifetime.ApplicationStopping).GetAwaiter().GetResult();
            RunBitwardenSync(hostApplicationLifetime.ApplicationStopping).GetAwaiter().GetResult();
        }
    }

    private async Task RunBitwardenSync(CancellationToken cancellationToken = default)
    {
        try
        {
            await synchronisationLogic.RunSynchronisationAsync(cancellationToken);
        }
        catch (Exception e)
        {
            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - An error occurred during Bitwarden sync: {e.Message}");
        }

        await DelayUntilNextRun(cancellationToken);
    }

    private async Task DelayUntilNextRun(CancellationToken cancellationToken = default)
    {
        var now = DateTime.Now;
        var nextOccurrence = _schedule!.GetNextOccurrence(now);

        var delay = nextOccurrence - now;
        if (delay.TotalMilliseconds < 0)
        {
            delay = TimeSpan.Zero;
        }

        Console.WriteLine($"Next sync scheduled for {nextOccurrence:yyyy-MM-dd HH:mm:ss}.");
        await Task.Delay(delay, cancellationToken);
    }

    private void OnStopping()
    {
        Console.WriteLine("Stopping Bitwarden Secure Sync tool...");
    }

    private static void OnStopped()
    {
        Console.WriteLine("Bitwarden Secure Sync tool stopped.");
    }
}