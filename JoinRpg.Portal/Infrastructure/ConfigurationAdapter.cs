using System;
using JoinRpg.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace JoinRpg.Portal.Infrastructure
{
    public class ConfigurationAdapter : IMailGunConfig
    {
        private readonly IConfiguration configuration;

        public ConfigurationAdapter(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        string IMailGunConfig.ApiDomain => configuration.GetValue<string>("MailGunApiDomain");

        string IMailGunConfig.ApiKey => configuration.GetValue<string>("MailGunApiKey");

        string IMailGunConfig.ServiceEmail => "support@" + configuration.GetValue<string>("MailGunApiDomain");
    }
}
