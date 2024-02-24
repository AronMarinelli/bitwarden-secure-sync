using System.IO.Compression;
using System.Runtime.InteropServices;
using Bitwarden.SecureSync.Interfaces.Client;

namespace Bitwarden.SecureSync.Logic.Client;

public class BitwardenClientDownloadLogic : IBitwardenClientDownloadLogic
{
    private readonly IHttpClientFactory _httpClientFactory;

    private readonly DirectoryInfo _clientDownloadDirectory;
    private readonly FileInfo _clientVersionFile;
    private readonly FileInfo _clientFile;

    private const string ClientVersion = "v2024.2.0";

    private const string WindowsClientUrl =
        "https://github.com/bitwarden/clients/releases/download/cli-v2024.2.0/bw-windows-2024.2.0.zip";

    private const string LinuxClientUrl =
        "https://github.com/bitwarden/clients/releases/download/cli-v2024.2.0/bw-linux-2024.2.0.zip";

    private const string OsxClientUrl =
        "https://github.com/bitwarden/clients/releases/download/cli-v2024.2.0/bw-macos-2024.2.0.zip";

    public BitwardenClientDownloadLogic(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;

        var clientDownloadDirectory = BitwardenClientHelper.GetClientDirectory();
        Directory.CreateDirectory(clientDownloadDirectory.FullName);

        _clientDownloadDirectory = clientDownloadDirectory;
        _clientVersionFile = new FileInfo(Path.Combine(_clientDownloadDirectory.FullName, "version"));
        _clientFile = BitwardenClientHelper.GetClientFileInfo();
    }

    public async Task EnsureClientAvailabilityAsync(CancellationToken cancellationToken = default)
    {
        string downloadUrl;
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            downloadUrl = LinuxClientUrl;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            downloadUrl = WindowsClientUrl;
        } 
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            downloadUrl = OsxClientUrl;
        }
        else
        {
            throw new PlatformNotSupportedException("Unsupported platform.");
        }
        
        Console.WriteLine("Checking for Bitwarden CLI client...");
        if (!_clientFile.Exists)
        {
            Console.WriteLine("Bitwarden CLI client not found. Downloading...");
            await DownloadClient(downloadUrl, cancellationToken);
            return;
        }

        try
        {
            Console.WriteLine("Bitwarden CLI client found, checking version...");
            var version = await File.ReadAllTextAsync(_clientVersionFile.FullName, cancellationToken);
            if (version != ClientVersion)
            {
                Console.WriteLine("Bitwarden CLI client version outdated. Downloading...");
                await DownloadClient(downloadUrl, cancellationToken);
            }
            else
            {
                Console.WriteLine("Up-to-date Bitwarden CLI client installation found. Skipping download.");
            }
        }
        catch
        {
            Console.WriteLine("Version file missing or corrupt. Downloading client...");
            await DownloadClient(downloadUrl, cancellationToken);
        }
    }

    private async Task EnsureLinuxClientAvailability(CancellationToken cancellationToken = default)
    {
        
    }

    private async Task DownloadClient(string url, CancellationToken cancellationToken = default)
    {
        var zipFile = new FileInfo(Path.Combine(_clientDownloadDirectory.FullName, $"{DateTime.Now:yyyyMMdd_HHmmss}-bw.zip"));
        using (var httpClient = _httpClientFactory.CreateClient())
        {
            var response = await httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var zipFileStream = zipFile.Open(FileMode.Create);
            await response.Content.CopyToAsync(zipFileStream, cancellationToken);
        }

        Console.WriteLine("Extracting Bitwarden CLI client...");
        using (var archiveStream = ZipFile.OpenRead(zipFile.FullName))
        {
            var executableEntry = archiveStream.GetEntry(_clientFile.Name);
            if (executableEntry is null)
                throw new Exception("Bitwarden CLI client not found in downloaded archive.");

            await using var executableStream = executableEntry.Open();
            await using var fileStream = _clientFile.Open(FileMode.Create);
            await executableStream.CopyToAsync(fileStream, cancellationToken);
        }

        zipFile.Delete();
        await File.WriteAllTextAsync(_clientVersionFile.FullName, ClientVersion, cancellationToken);
        Console.WriteLine($"Bitwarden CLI client {ClientVersion} downloaded successfully.");
    }
}