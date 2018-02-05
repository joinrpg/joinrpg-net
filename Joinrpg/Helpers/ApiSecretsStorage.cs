using System.Configuration;
using JetBrains.Annotations;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Web.Helpers
{
    [UsedImplicitly]
    internal class ApiSecretsStorage : IMailGunConfig
    {
        public string ApiDomain => ConfigurationManager.AppSettings["MailGunApiDomain"];

        string IMailGunConfig.ApiKey => ConfigurationManager.AppSettings["MailGunApiKey"];
        public string ServiceEmail => "support@" + ApiDomain;

        internal static string GoogleClientId => ConfigurationManager.AppSettings["GoogleClientId"];

        internal static string GoogleClientSecret =>
            ConfigurationManager.AppSettings["GoogleClientSecret"];

        internal static string VkClientId => ConfigurationManager.AppSettings["VkClientId"];

        internal static string VkClientSecret => ConfigurationManager.AppSettings["VkClientSecret"];

        internal static string XsrfKey => ConfigurationManager.AppSettings["XsrfKey"];
    }
}
