using System;
using JoinRpg.Dal.Impl;
using JoinRpg.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace JoinRpg.Portal.Infrastructure
{
    /// <summary>
    /// Read configuration
    /// </summary>
    public class ConfigurationAdapter : IMailGunConfig, IJoinDbContextConfiguration
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
            => configuration.GetConnectionString("DefaultConnection")
                ?.Replace("!!!!", configuration.GetValue<string>(WebHostDefaults.ContentRootKey)+"\\App_Data");

        // TODO inject this
        internal string XsrfKey => configuration.GetValue<string>("XsrfKey");
    }
}
