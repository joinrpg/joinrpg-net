using System.Configuration;
using JetBrains.Annotations;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Web.Helpers
{
  [UsedImplicitly]
  internal class ApiSecretsStorage : IMailGunConfig
  {
    string IMailGunConfig.ApiDomain => ConfigurationManager.AppSettings["MailGunApiDomain"];

    string IMailGunConfig.ApiKey => ConfigurationManager.AppSettings["MailGunApiKey"];

    internal static string GoogleClientId => ConfigurationManager.AppSettings["GoogleClientId"];

    internal static string GoogleClientSecret => ConfigurationManager.AppSettings["GoogleClientSecret"];

    internal static string VkClientId => ConfigurationManager.AppSettings["VkClientId"];

    internal static string VkClientSecret => ConfigurationManager.AppSettings["VkClientSecret"];

    internal static string XsrfKey => ConfigurationManager.AppSettings["XsrfKey"];
  }
}
