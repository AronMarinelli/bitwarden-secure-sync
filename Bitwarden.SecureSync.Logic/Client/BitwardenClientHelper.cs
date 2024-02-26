using System.Runtime.InteropServices;

namespace Bitwarden.SecureSync.Logic.Client;

internal static class BitwardenClientHelper
{
    internal static DirectoryInfo GetClientDirectory()
    {
        const string CLIENT_DIRECTORY_NAME = "client";
        return new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), CLIENT_DIRECTORY_NAME));
    }

    internal static FileInfo GetClientFileInfo()
    {
        const string WINDOWS_FILE_NAME = "bw.exe";
        const string UNIX_FILE_NAME = "bw";

        var clientDirectory = GetClientDirectory();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return new FileInfo(Path.Combine(clientDirectory.FullName, UNIX_FILE_NAME));

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new FileInfo(Path.Combine(clientDirectory.FullName, WINDOWS_FILE_NAME));

        throw new PlatformNotSupportedException("Unsupported platform.");
    }
}