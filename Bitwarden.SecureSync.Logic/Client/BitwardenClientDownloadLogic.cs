using System.IO.Compression;
using System.Runtime.InteropServices;
using Bitwarden.SecureSync.Interfaces.Client;

namespace Bitwarden.SecureSync.Logic.Client;

public class BitwardenClientDownloadLogic : IBitwardenClientDownloadLogic
{
    private const string CLIENT_VERSION_WINDOWS = "v2024.2.1";
    private const string CLIENT_VERSION_OSX = "v2024.3.1";
    private const string CLIENT_VERSION_LINUX = "v2024.3.1";

    private const string WINDOWS_CLIENT_URL =
        "https://github.com/bitwarden/clients/releases/download/cli-v2024.2.1/bw-windows-2024.2.1.zip";

    private const string LINUX_CLIENT_URL =
        "https://github.com/bitwarden/clients/releases/download/cli-v2024.3.1/bw-linux-2024.3.1.zip";

    private const string OSX_CLIENT_URL =
        "https://github.com/bitwarden/clients/releases/download/cli-v2024.3.1/bw-macos-2024.3.1.zip";

    private readonly DirectoryInfo _clientDownloadDirectory;
    private readonly FileInfo _clientFile;
    private readonly FileInfo _clientVersionFile;
    private readonly IHttpClientFactory _httpClientFactory;

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
        string clientVersion;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            downloadUrl = LINUX_CLIENT_URL;
            clientVersion = CLIENT_VERSION_LINUX;   
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            downloadUrl = WINDOWS_CLIENT_URL;
            clientVersion = CLIENT_VERSION_WINDOWS;   
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            downloadUrl = OSX_CLIENT_URL;
            clientVersion = CLIENT_VERSION_OSX;   
        }
        else
        {
            throw new PlatformNotSupportedException("Unsupported platform.");
        }

        Console.WriteLine("Checking for Bitwarden CLI client...");
        if (!_clientFile.Exists)
        {
            Console.WriteLine("Bitwarden CLI client not found. Downloading...");
            await DownloadClient(downloadUrl, clientVersion, cancellationToken);
        }
        else
        {
            try
            {
                Console.WriteLine("Bitwarden CLI client found, checking version...");
                var version = await File.ReadAllTextAsync(_clientVersionFile.FullName, cancellationToken);
                if (version != clientVersion)
                {
                    Console.WriteLine("Bitwarden CLI client version outdated. Downloading...");
                    await DownloadClient(downloadUrl, clientVersion, cancellationToken);
                }
                else
                {
                    Console.WriteLine("Up-to-date Bitwarden CLI client installation found. Skipping download.");
                }
            }
            catch
            {
                Console.WriteLine("Version file missing or corrupt. Downloading client...");
                await DownloadClient(downloadUrl, clientVersion, cancellationToken);
            }
        }

        SetUnixFilePermissions();
    }

    private async Task DownloadClient(string url, string version, CancellationToken cancellationToken = default)
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
        await File.WriteAllTextAsync(_clientVersionFile.FullName, version, cancellationToken);
        Console.WriteLine($"Bitwarden CLI client {version} downloaded successfully.");
    }

    private void SetUnixFilePermissions()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        Console.WriteLine("Ensuring required permissions are set on CLI client file...");
        _clientFile.UnixFileMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
    }
}