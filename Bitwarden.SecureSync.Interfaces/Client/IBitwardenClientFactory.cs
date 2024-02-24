namespace Bitwarden.SecureSync.Interfaces.Client;

public interface IBitwardenClientFactory
{
    IBitwardenClient CreateClient();
}