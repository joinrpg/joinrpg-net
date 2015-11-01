using System.Configuration;
using JetBrains.Annotations;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Allrpg;

namespace JoinRpg.Web.Helpers
{
  [UsedImplicitly]
  class ApiSecretsStorage : IAllrpgApiKeyStorage, IMailGunConfig
  {
    string IAllrpgApiKeyStorage.Key => ConfigurationManager.AppSettings["AllrpgInfoPassphrase"];

    string IMailGunConfig.ApiDomain => ConfigurationManager.AppSettings["MailGunApiDomain"];

    string IMailGunConfig.ApiKey => ConfigurationManager.AppSettings["MailGunApiKey"];
  }
}
