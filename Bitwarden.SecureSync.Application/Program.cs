using Bitwarden.SecureSync.Application;
using Bitwarden.SecureSync.Interfaces.Client;
using Bitwarden.SecureSync.Interfaces.Synchronisation;
using Bitwarden.SecureSync.Logic.Client;
using Bitwarden.SecureSync.Logic.Synchronisation;
using Bitwarden.SecureSync.Models.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

Console.WriteLine("Starting Bitwarden Secure Sync tool...");

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional:true, reloadOnChange:true)  
    .AddEnvironmentVariables(prefix: "BWSYNC_")           
    .Build();

var localSettingsAvailable = File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"));

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHttpClient();

if (localSettingsAvailable)
{
    var bitwardenConfiguration = new BitwardenConfiguration();
    configuration.GetSection("Bitwarden").Bind(bitwardenConfiguration);
    builder.Services.AddSingleton(bitwardenConfiguration);
    
    var syncConfiguration = new SyncConfiguration();
    configuration.GetSection("Sync").Bind(syncConfiguration);
    builder.Services.AddSingleton(syncConfiguration);
}

builder.Services.AddSingleton<IBitwardenClientFactory, BitwardenClientFactory>();

builder.Services.AddTransient<IBitwardenClientDownloadLogic, BitwardenClientDownloadLogic>();
builder.Services.AddTransient<ISynchronisationLogic, SynchronisationLogic>();

builder.Services.AddHostedService<ApplicationHost>();

using var host = builder.Build();

await host.RunAsync();
