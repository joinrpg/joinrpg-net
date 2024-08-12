using JoinRpg.Dal.Impl;
using PscbApi;

namespace JoinRpg.Portal.Infrastructure;

/// <summary>
/// Read configuration
/// </summary>
public class ConfigurationAdapter(IConfiguration configuration) : IJoinDbContextConfiguration, IBankSecretsProvider
{
    string IJoinDbContextConfiguration.ConnectionString
    {
        get
        {

            var connString = configuration.GetConnectionString("DefaultConnection");
            /*       if (string.IsNullOrWhiteSpace(connString))
                   {
                       return System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"]?.ConnectionString
                           ?.Replace("!!!!", configuration.GetValue<string>(WebHostDefaults.ContentRootKey) + "\\App_Data") ?? "_";
                   }*/
            return connString;
        }
    }

    // TODO inject this
    internal string XsrfKey => configuration.GetValue<string>("XsrfKey");

    bool IBankSecretsProvider.Debug => configuration.GetValue<bool>("PaymentProviders:Pscb:Debug");
    string IBankSecretsProvider.ApiEndpoint => configuration.GetValue<string>("PaymentProviders:Pscb:BankApiEndpoint");
    string IBankSecretsProvider.ApiDebugEndpoint => configuration.GetValue<string>("PaymentProviders:Pscb:BankApiDebugEndpoint");
    string IBankSecretsProvider.MerchantId => configuration.GetValue<string>("PaymentProviders:Pscb:BankMerchantId");
    string IBankSecretsProvider.ApiKey => configuration.GetValue<string>("PaymentProviders:Pscb:BankApiKey");
    string IBankSecretsProvider.ApiDebugKey => configuration.GetValue<string>("PaymentProviders:Pscb:BankApiDebugKey");
}
