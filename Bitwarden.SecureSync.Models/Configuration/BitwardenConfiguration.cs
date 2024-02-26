using System.ComponentModel.DataAnnotations;

namespace Bitwarden.SecureSync.Models.Configuration;

public class BitwardenConfiguration
{
    private const string DEFAULT_SERVER_URL = "https://vault.bitwarden.com";

    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string ServerUrl { get; set; }
    public string VaultPassword { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ServerUrl))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(
                $"Bitwarden config validation: A server url should be supplied! Falling back on default value ({DEFAULT_SERVER_URL}).");
            ServerUrl = DEFAULT_SERVER_URL;
        }

        if (!Uri.IsWellFormedUriString(ServerUrl, UriKind.RelativeOrAbsolute))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(
                $"Bitwarden config validation: The server url could not be parsed into a valid Uri (current value: {ServerUrl}). Falling back on default value ({DEFAULT_SERVER_URL}).");
            ServerUrl = DEFAULT_SERVER_URL;
        }

        if (string.IsNullOrWhiteSpace(ClientId))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Bitwarden config validation: A valid ClientId must be supplied!");
            Console.ResetColor();
            throw new ValidationException("No valid ClientId was supplied.");
        }

        if (string.IsNullOrWhiteSpace(ClientSecret))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Bitwarden config validation: A valid ClientSecret must be supplied!");
            Console.ResetColor();
            throw new ValidationException("No valid ClientSecret was supplied.");
        }

        if (string.IsNullOrWhiteSpace(VaultPassword))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Bitwarden config validation: A valid VaultPassword must be supplied!");
            Console.ResetColor();
            throw new ValidationException("No valid VaultPassword was supplied.");
        }

        Console.ResetColor();
    }

    public static BitwardenConfiguration GetSampleConfiguration()
    {
        return new BitwardenConfiguration
        {
            ClientId = string.Empty,
            ClientSecret = string.Empty,
            ServerUrl = DEFAULT_SERVER_URL,
            VaultPassword = string.Empty
        };
    }
}