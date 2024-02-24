using Bitwarden.SecureSync.Interfaces.Client;
using Bitwarden.SecureSync.Models.Configuration;

namespace Bitwarden.SecureSync.Logic.Client;

public class BitwardenClientFactory(BitwardenConfiguration configuration) : IBitwardenClientFactory
{
    public IBitwardenClient CreateClient()
    {
        return new BitwardenClient(configuration);
    }
}