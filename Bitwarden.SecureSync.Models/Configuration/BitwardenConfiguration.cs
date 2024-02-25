using System.ComponentModel.DataAnnotations;

namespace Bitwarden.SecureSync.Models.Configuration;

public class BitwardenConfiguration
{
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string ServerUrl { get; set; }
    public string VaultPassword { get; set; }

    public void Validate()
    {
        const string DefaultServerUrl = "https://vault.bitwarden.com";
        if (string.IsNullOrWhiteSpace(ServerUrl))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Bitwarden config validation: A server url must be supplied! Falling back on default value ({DefaultServerUrl}).");
            ServerUrl = DefaultServerUrl;
        }

        if (!Uri.IsWellFormedUriString(ServerUrl, UriKind.RelativeOrAbsolute))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Bitwarden config validation: The server url could not be parsed into a valid Uri (current value: {ServerUrl}). Falling back on default value ({DefaultServerUrl}).");
            ServerUrl = DefaultServerUrl;
        }

        if (string.IsNullOrWhiteSpace(ClientId))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Bitwarden config validation: A valid ClientId must be supplied!");
            Console.ResetColor();
            throw new ValidationException("No valid ClientId was supplied.");
        }
        
        if (string.IsNullOrWhiteSpace(ClientSecret))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Bitwarden config validation: A valid ClientSecret must be supplied!");
            Console.ResetColor();
            throw new ValidationException("No valid ClientSecret was supplied.");
        }
        
        if (string.IsNullOrWhiteSpace(VaultPassword))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Bitwarden config validation: A valid VaultPassword must be supplied!");
            Console.ResetColor();
            throw new ValidationException("No valid VaultPassword was supplied.");
        }
        
        Console.ResetColor();
    }
}