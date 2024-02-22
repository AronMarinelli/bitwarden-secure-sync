using System.IO.Compression;
using System.Runtime.InteropServices;
using Bitwarden.SecureSync.Interfaces;

namespace Bitwarden.SecureSync.Logic;

public class BitwardenClientDownloadLogic : IBitwardenClientDownloadLogic
{
    private readonly IHttpClientFactory _httpClientFactory;

    private readonly DirectoryInfo _clientDownloadDirectory;
    private readonly FileInfo _clientVersionFile;

    private const string ClientVersion = "v2024.2.0";

    private const string WindowsClientUrl =
        "https://github.com/bitwarden/clients/releases/download/cli-v2024.2.0/bw-windows-2024.2.0.zip";

    private const string LinuxClientUrl =
        "https://github.com/bitwarden/clients/releases/download/cli-v2024.2.0/bw-linux-2024.2.0.zip";

    public BitwardenClientDownloadLogic(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;

        var clientDownloadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "client");
        Directory.CreateDirectory(clientDownloadDirectory);

        _clientDownloadDirectory = new DirectoryInfo(clientDownloadDirectory);
        _clientVersionFile = new FileInfo(Path.Combine(_clientDownloadDirectory.FullName, "version"));
    }

    public async Task EnsureClientAvailabilityAsync(CancellationToken cancellationToken = default)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            await EnsureLinuxClientAvailability(cancellationToken);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            await EnsureWindowsClientAvailability(cancellationToken);
        }
        else
        {
            throw new PlatformNotSupportedException("Unsupported platform.");
        }
    }

    private async Task EnsureLinuxClientAvailability(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Detected Linux OS. Checking for Bitwarden CLI client...");
        var clientFile = new FileInfo(Path.Combine(_clientDownloadDirectory.FullName, "bw"));
        if (!clientFile.Exists)
        {
            Console.WriteLine("Bitwarden CLI client not found. Downloading...");
            await DownloadClient(LinuxClientUrl, clientFile, cancellationToken);
            return;
        }

        try
        {
            Console.WriteLine("Bitwarden CLI client found, checking version...");
            var version = await File.ReadAllTextAsync(_clientVersionFile.FullName, cancellationToken);
            if (version != ClientVersion)
            {
                Console.WriteLine("Bitwarden CLI client version outdated. Downloading...");
                await DownloadClient(LinuxClientUrl, clientFile, cancellationToken);
            }
            else
            {
                Console.WriteLine("Up-to-date Bitwarden CLI client installation found. Skipping download.");
            }
        }
        catch
        {
            Console.WriteLine("Version file missing or corrupt. Downloading client...");
            await DownloadClient(LinuxClientUrl, clientFile, cancellationToken);
        }
    }

    private async Task EnsureWindowsClientAvailability(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Detected Windows OS. Checking for Bitwarden CLI client...");
        var clientFile = new FileInfo(Path.Combine(_clientDownloadDirectory.FullName, "bw.exe"));
        if (!clientFile.Exists)
        {
            Console.WriteLine("Bitwarden CLI client not found. Downloading...");
            await DownloadClient(WindowsClientUrl, clientFile, cancellationToken);
            return;
        }

        try
        {
            Console.WriteLine("Bitwarden CLI client found, checking version...");
            var version = await File.ReadAllTextAsync(_clientVersionFile.FullName, cancellationToken);
            if (version != ClientVersion)
            {
                Console.WriteLine("Bitwarden CLI client version outdated. Downloading...");
                await DownloadClient(WindowsClientUrl, clientFile, cancellationToken);
            }
            else
            {
                Console.WriteLine("Up-to-date Bitwarden CLI client installation found. Skipping download.");
            }
        }
        catch
        {
            Console.WriteLine("Version file missing or corrupt. Downloading client...");
            await DownloadClient(WindowsClientUrl, clientFile, cancellationToken);
        }
    }

    private async Task DownloadClient(string url, FileInfo clientFile, CancellationToken cancellationToken = default)
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
            var executableEntry = archiveStream.GetEntry(clientFile.Name);
            if (executableEntry is null)
                throw new Exception("Bitwarden CLI client not found in downloaded archive.");

            await using var executableStream = executableEntry.Open();
            await using var fileStream = clientFile.Open(FileMode.Create);
            await executableStream.CopyToAsync(fileStream, cancellationToken);
        }

        zipFile.Delete();
        await File.WriteAllTextAsync(_clientVersionFile.FullName, ClientVersion, cancellationToken);
        Console.WriteLine($"Bitwarden CLI client {ClientVersion} downloaded successfully.");
    }
}