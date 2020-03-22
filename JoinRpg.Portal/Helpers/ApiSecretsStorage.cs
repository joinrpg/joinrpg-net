using System.Configuration;
using JetBrains.Annotations;

namespace JoinRpg.Portal.Helpers
{
    [UsedImplicitly]
    internal class ApiSecretsStorage 
    {
        internal static string GoogleClientId => ConfigurationManager.AppSettings["GoogleClientId"];

        internal static string GoogleClientSecret =>
            ConfigurationManager.AppSettings["GoogleClientSecret"];

        internal static string VkClientId => ConfigurationManager.AppSettings["VkClientId"];

        internal static string VkClientSecret => ConfigurationManager.AppSettings["VkClientSecret"];

        internal static string XsrfKey => ConfigurationManager.AppSettings["XsrfKey"];
    }
}
