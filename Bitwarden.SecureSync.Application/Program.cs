using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional:true, reloadOnChange:true)  
    .AddEnvironmentVariables(prefix: "BWSYNC_")           
    .Build();

Console.WriteLine("Bitwarden Secure Sync tool");
