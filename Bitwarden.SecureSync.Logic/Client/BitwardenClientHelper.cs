using System.Runtime.InteropServices;

namespace Bitwarden.SecureSync.Logic.Client;

internal static class BitwardenClientHelper
{
    internal static DirectoryInfo GetClientDirectory()
    {
        const string clientDirectoryName = "client";
        return new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), clientDirectoryName));
    }

    internal static FileInfo GetClientFileInfo()
    {
        const string windowsFileName = "bw.exe";
        const string unixFileName = "bw";
        
        var clientDirectory = GetClientDirectory();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return new FileInfo(Path.Combine(clientDirectory.FullName, unixFileName));
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new FileInfo(Path.Combine(clientDirectory.FullName, windowsFileName));
        }

        throw new PlatformNotSupportedException("Unsupported platform.");
    }
}