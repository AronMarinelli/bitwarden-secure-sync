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

    var configurationRoot = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables(prefix: "BWSYNC:")
        .Build();

    InjectConfiguration(builder.Services, configurationRoot);

    builder.Services.AddHttpClient();

    builder.Services.AddSingleton<IBitwardenClientFactory, BitwardenClientFactory>();

    builder.Services.AddTransient<IBitwardenClientDownloadLogic, BitwardenClientDownloadLogic>();
    builder.Services.AddTransient<ISynchronisationLogic, SynchronisationLogic>();

    builder.Services.AddHostedService<ApplicationHost>();

    using var host = builder.Build();

    await host.RunAsync();
}
return;

static void InjectConfiguration(IServiceCollection services, IConfiguration configuration)
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