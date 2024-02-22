namespace Bitwarden.SecureSync.Interfaces;

public interface IBitwardenClientFactory
{
    IBitwardenClient CreateClient();
}