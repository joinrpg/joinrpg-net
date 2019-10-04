using System.Configuration;
using PscbApi;

namespace JoinRpg.Web.Helpers
{
    public class BankSecretsProvider : IBankSecretsProvider
    {
        public string MerchantId => ConfigurationManager.AppSettings["bankMerchantId"];

        public string ApiKey => ConfigurationManager.AppSettings["bankApiKey"];

        public string ApiDebugKey => ConfigurationManager.AppSettings["bankApiDebugKey"];
    }
}
