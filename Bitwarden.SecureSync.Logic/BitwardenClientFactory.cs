using Bitwarden.SecureSync.Interfaces;
using Bitwarden.SecureSync.Models;
using Bitwarden.SecureSync.Models.Configuration;

namespace Bitwarden.SecureSync.Logic;

public class BitwardenClientFactory(BitwardenConfiguration configuration) : IBitwardenClientFactory
{
    public IBitwardenClient CreateClient()
    {
        return new BitwardenClient(configuration);
    }
}