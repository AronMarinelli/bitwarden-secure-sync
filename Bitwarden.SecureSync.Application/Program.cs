using System.Text.Json;
using Bitwarden.SecureSync.Application;
using Bitwarden.SecureSync.Interfaces.Client;
using Bitwarden.SecureSync.Interfaces.Synchronisation;
using Bitwarden.SecureSync.Logic.Client;
using Bitwarden.SecureSync.Logic.Synchronisation;
using Bitwarden.SecureSync.Models.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddLogging(
        loggingBuilder =>
        {
            loggingBuilder.AddFilter("Microsoft", LogLevel.Warning)
                .AddFilter("System", LogLevel.Warning)
                .AddConsole();
        });

    Console.WriteLine("Starting Bitwarden Secure Sync tool...");

    await CheckConfigurationAvailability();

    var configurationRoot = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", true, true)
        .AddJsonFile("config/appsettings.json", true, true)
        .Build();

    BindConfiguration(builder.Services, configurationRoot);

    builder.Services.AddHttpClient();

    builder.Services.AddSingleton<IBitwardenClientFactory, BitwardenClientFactory>();

    builder.Services.AddTransient<IBitwardenClientDownloadLogic, BitwardenClientDownloadLogic>();
    builder.Services.AddTransient<ISynchronisationLogic, SynchronisationLogic>();

    builder.Services.AddHostedService<ApplicationHost>();

    using var host = builder.Build();

    await host.RunAsync();
}
return;

static async Task CheckConfigurationAvailability()
{
    if (File.Exists("appsettings.json") || File.Exists("config/appsettings.json"))
        return;

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine(
        "No configuration file found. A default appsettings.json file will be created in the /config directory.");
    Console.ResetColor();

    var bitwardenConfiguration = BitwardenConfiguration.GetSampleConfiguration();
    var syncConfiguration = SyncConfiguration.GetSampleConfiguration();

    var serializedSampleConfig = JsonSerializer.Serialize(
        new
        {
            Bitwarden = bitwardenConfiguration,
            Sync = syncConfiguration
        },
        new JsonSerializerOptions
        {
            WriteIndented = true
        }
    );

    await File.WriteAllTextAsync("config/appsettings.json", serializedSampleConfig);
}

static void BindConfiguration(IServiceCollection services, IConfiguration configuration)
{
    var bitwardenConfiguration = new BitwardenConfiguration();
    var syncConfiguration = new SyncConfiguration();

    configuration.GetSection("Bitwarden").Bind(bitwardenConfiguration);
    configuration.GetSection("Sync").Bind(syncConfiguration);

    bitwardenConfiguration.Validate();
    syncConfiguration.Validate();

    services.AddSingleton(bitwardenConfiguration);
    services.AddSingleton(syncConfiguration);
}