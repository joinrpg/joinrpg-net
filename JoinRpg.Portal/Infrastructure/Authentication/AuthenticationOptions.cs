using System;

namespace JoinRpg.Portal.Infrastructure
{
    public class GoogleAuthenticationOptions
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        internal void Deconstruct(out string ClientId, out string ClientSecret)
        {
            ClientId = this.ClientId;
            ClientSecret = this.ClientSecret;
        }
    }
}
