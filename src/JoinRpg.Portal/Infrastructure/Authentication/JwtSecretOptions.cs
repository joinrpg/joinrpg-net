using System;

namespace JoinRpg.Portal.Infrastructure.Authentication
{
    public class JwtSecretOptions
    {
        public string SecretKey { get; set; } = null!;

        public TimeSpan JwtLifetime { get; set; } = TimeSpan.FromDays(30);

        public string Issuer { get; set; } = "https://joinrpg.ru";
    }
}
