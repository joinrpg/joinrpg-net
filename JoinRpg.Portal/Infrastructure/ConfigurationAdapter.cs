using System;
using JoinRpg.Dal.Impl;
using JoinRpg.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using PscbApi;

namespace JoinRpg.Portal.Infrastructure
{
    /// <summary>
    /// Read configuration
    /// </summary>
    public class ConfigurationAdapter : IMailGunConfig, IJoinDbContextConfiguration, IBankSecretsProvider
    {
        private readonly IConfiguration configuration;

        /// <summary>
        /// ctor
        /// </summary>
        public ConfigurationAdapter(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        string IMailGunConfig.ApiDomain => configuration.GetValue<string>("MailGunApiDomain");

        string IMailGunConfig.ApiKey => configuration.GetValue<string>("MailGunApiKey");

        string IMailGunConfig.ServiceEmail => "support@" + configuration.GetValue<string>("MailGunApiDomain");

        string IJoinDbContextConfiguration.ConnectionString
        {
            get
            {

                var connString = configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrWhiteSpace(connString))
                {
                    return System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"]?.ConnectionString
                        ?.Replace("!!!!", configuration.GetValue<string>(WebHostDefaults.ContentRootKey) + "\\App_Data") ?? "_";
                }
                return connString;
            }
        }

        // TODO inject this
        internal string XsrfKey => configuration.GetValue<string>("XsrfKey");

        string IBankSecretsProvider.MerchantId => configuration.GetValue<string>("PaymentProviders:Pscb:bankMerchantId");

        string IBankSecretsProvider.ApiKey => configuration.GetValue<string>("PaymentProviders:Pscb:bankApiKey");

        string IBankSecretsProvider.ApiDebugKey => configuration.GetValue<string>("PaymentProviders:Pscb:bankApiDebugKey");
    }
}
