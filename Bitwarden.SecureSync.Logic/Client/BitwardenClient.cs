using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Bitwarden.SecureSync.Interfaces.Client;
using Bitwarden.SecureSync.Models.Cli;
using Bitwarden.SecureSync.Models.Configuration;

namespace Bitwarden.SecureSync.Logic.Client;

public class BitwardenClient : IBitwardenClient
{
    private readonly FileInfo _clientFile;
    private readonly BitwardenConfiguration _configuration;

    private string _sessionKey;

    public BitwardenClient(BitwardenConfiguration configuration)
    {
        _configuration = configuration;

        var clientFile = BitwardenClientHelper.GetClientFileInfo();
        if (!clientFile.Exists)
            throw new FileNotFoundException("Client not found.");

        _clientFile = clientFile;

        // Ensure the client is logged out.
        ExecuteCommandAsync("logout", throwOnError: false).GetAwaiter().GetResult();

        Console.WriteLine($"Connecting to {configuration.ServerUrl}...");
        ExecuteCommandAsync($"config server {configuration.ServerUrl}").GetAwaiter().GetResult();
    }

    public async Task UnlockVault(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(_sessionKey))
            throw new InvalidOperationException("Vault is already unlocked.");

        try
        {
            Environment.SetEnvironmentVariable("BW_CLIENTID", _configuration.ClientId);
            Environment.SetEnvironmentVariable("BW_CLIENTSECRET", _configuration.ClientSecret);

            // Login using the API credentials
            await ExecuteCommandAsync("login --apikey", cancellationToken);

            // Unlock the vault using the password
            Environment.SetEnvironmentVariable("BW_PASSWORD", _configuration.VaultPassword);
            var sessionData = await ExecuteCommandAsync("unlock --passwordenv BW_PASSWORD", cancellationToken);

            var sessionKeyRegex = new Regex(@"BW_SESSION=""(?<SessionKey>.+)""");
            var match = sessionKeyRegex.Match(sessionData);
            if (!match.Success)
                throw new InvalidOperationException("Failed to unlock vault.");

            _sessionKey = match.Groups["SessionKey"].Value;
        }
        finally
        {
            Environment.SetEnvironmentVariable("BW_CLIENTID", null);
            Environment.SetEnvironmentVariable("BW_CLIENTSECRET", null);
            Environment.SetEnvironmentVariable("BW_PASSWORD", null);
        }
    }

    public async Task ExportVault(string exportDirectory, string encryptionKey, bool includeOrganisationItems,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_sessionKey))
            throw new InvalidOperationException("Vault is not unlocked.");

        const string DEFAULT_FILENAME = "personal_vault.encrypted.json";

        var baseExportCommand = $"export --format encrypted_json --session {_sessionKey}";
        if (!string.IsNullOrWhiteSpace(encryptionKey))
        {
            Console.WriteLine("Using custom encryption key for export.");
            baseExportCommand += $" --password {encryptionKey}";
        }
        else
        {
            Console.WriteLine("Using account encryption key for export.");
        }

        try
        {
            Console.WriteLine("Exporting vault data...");

            var exportPath = Path.Combine(exportDirectory, DEFAULT_FILENAME);
            await ExecuteCommandAsync(baseExportCommand + $" --output \"{exportPath}\"", cancellationToken);
            Console.WriteLine($"Successfully exported personal vault to {exportPath}.");
        }
        catch
        {
            Console.WriteLine("An error occurred while exporting personal vault data, skipping...");
        }

        if (includeOrganisationItems)
        {
            Console.WriteLine("Listing organisations for export...");
            try
            {
                var organisationsResponse =
                    await ExecuteCommandAsync($"list organizations --session {_sessionKey}", cancellationToken);
                var organisations = JsonSerializer.Deserialize<List<OrganisationItem>>(organisationsResponse);
                Console.WriteLine(
                    $"Found {organisations.Count} organisation{(organisations.Count != 1 ? "s" : string.Empty)} for export.");

                foreach (var organisation in organisations)
                    try
                    {
                        var fileSafeOrganisationName = Path.GetInvalidFileNameChars().Aggregate(organisation.Name,
                            (current, c) => current.Replace(c.ToString(), "_"));
                        var outputFile = Path.Combine(exportDirectory, $"{fileSafeOrganisationName}.encrypted.json");

                        Console.WriteLine($"Exporting organisation {organisation.Name} ({organisation.Id})...");
                        await ExecuteCommandAsync(
                            baseExportCommand + $" --organizationid {organisation.Id} --output \"{outputFile}\"",
                            cancellationToken
                        );
                        Console.WriteLine($"Successfully exported organisation vault data to {outputFile}.");
                    }
                    catch
                    {
                        Console.WriteLine(
                            $"An error occurred while exporting organisation {organisation.Name}, skipping...");
                    }
            }
            catch
            {
                Console.WriteLine("An error occurred while listing organisations for export, skipping...");
            }
        }
    }

    public void Dispose()
    {
        _sessionKey = null;
        ExecuteCommandAsync("logout", throwOnError: false).GetAwaiter().GetResult();

        GC.SuppressFinalize(this);
    }

    private async Task<string> ExecuteCommandAsync(string command, CancellationToken cancellationToken = default,
        bool throwOnError = true)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _clientFile.FullName,
            Arguments = command,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = new Process();
        process.StartInfo = startInfo;
        process.Start();

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode == 0)
            return await process.StandardOutput.ReadToEndAsync(cancellationToken);

        var error = await process.StandardError.ReadToEndAsync(cancellationToken);
        if (!throwOnError)
            return error;

        throw new InvalidOperationException(
            $"Command {command} failed with exit code {process.ExitCode}.{Environment.NewLine}{error}");
    }
}