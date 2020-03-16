using System.Configuration;
using PscbApi;

namespace JoinRpg.Portal.Helpers
{
    public class BankSecretsProvider : IBankSecretsProvider
    {
        public string MerchantId => ConfigurationManager.AppSettings["bankMerchantId"];

        public string ApiKey => ConfigurationManager.AppSettings["bankApiKey"];

        public string ApiDebugKey => ConfigurationManager.AppSettings["bankApiDebugKey"];
    }
}
