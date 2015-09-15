using System.Configuration;

namespace JoinRpg.Web.Helpers
{
  public static class MailGunFacade
  {
    public static bool Configured => string.IsNullOrWhiteSpace(ApiDomain) || string.IsNullOrWhiteSpace(ApiKey);

    public static string ApiDomain => ConfigurationManager.AppSettings["MailGunApiDomain"];

    public static string ApiKey => ConfigurationManager.AppSettings["MailGunApiKey"];
  }
}
