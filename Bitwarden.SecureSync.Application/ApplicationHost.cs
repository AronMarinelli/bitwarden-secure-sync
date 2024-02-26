using Bitwarden.SecureSync.Interfaces.Client;
using Bitwarden.SecureSync.Interfaces.Synchronisation;
using Bitwarden.SecureSync.Models.Configuration;
using Microsoft.Extensions.Hosting;
using NCrontab;
using Timer = System.Timers.Timer;

namespace Bitwarden.SecureSync.Application;

public class ApplicationHost(
    IHostApplicationLifetime hostApplicationLifetime,
    IBitwardenClientDownloadLogic clientDownloadLogic,
    ISynchronisationLogic synchronisationLogic,
    SyncConfiguration syncConfiguration
) : IHostedService
{
    private CrontabSchedule _schedule;
    private Timer _timer;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        hostApplicationLifetime.ApplicationStarted.Register(OnStarted);
        hostApplicationLifetime.ApplicationStopping.Register(OnStopping);
        hostApplicationLifetime.ApplicationStopped.Register(OnStopped);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private void OnStarted()
    {
        _schedule = syncConfiguration.GetCronSchedule();
        Console.WriteLine($"Cron schedule: {syncConfiguration.CronSchedule}");

        clientDownloadLogic.EnsureClientAvailabilityAsync(hostApplicationLifetime.ApplicationStopping).GetAwaiter()
            .GetResult();

        Console.WriteLine("Bitwarden Secure Sync tool started.");
        if (syncConfiguration.RunOnStartup)
        {
            Console.WriteLine("Starting initial Bitwarden sync...");
            RunBitwardenSync(hostApplicationLifetime.ApplicationStopping).GetAwaiter().GetResult();
            ScheduleJob(hostApplicationLifetime.ApplicationStopping).GetAwaiter().GetResult();
        }
        else
        {
            ScheduleJob(hostApplicationLifetime.ApplicationStopping).GetAwaiter().GetResult();
        }
    }

    private async Task ScheduleJob(CancellationToken cancellationToken)
    {
        var next = _schedule!.GetNextOccurrence(DateTime.Now);
        var delay = next - DateTimeOffset.Now;
        if (delay.TotalMilliseconds <= 0)
        {
            await ScheduleJob(cancellationToken);
            return;
        }

        Console.WriteLine($"Next sync scheduled for {next:yyyy-MM-dd HH:mm:ss}.");

        _timer = new Timer(delay.TotalMilliseconds);
        _timer.Elapsed += async (_, _) =>
        {
            _timer.Dispose();
            _timer = null;

            if (!cancellationToken.IsCancellationRequested) await RunBitwardenSync(cancellationToken);

            if (!cancellationToken.IsCancellationRequested) await ScheduleJob(cancellationToken);
        };

        _timer.Start();
    }

    private async Task RunBitwardenSync(CancellationToken cancellationToken = default)
    {
        try
        {
            await synchronisationLogic.RunSynchronisationAsync(cancellationToken);
        }
        catch (Exception e)
        {
            Console.WriteLine(
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - An error occurred during Bitwarden sync: {e.Message}");
        }
    }

    private void OnStopping()
    {
        Console.WriteLine("Stopping Bitwarden Secure Sync tool...");

        _timer?.Stop();
        _timer?.Dispose();
    }

    private static void OnStopped()
    {
        Console.WriteLine("Bitwarden Secure Sync tool stopped.");
    }
}