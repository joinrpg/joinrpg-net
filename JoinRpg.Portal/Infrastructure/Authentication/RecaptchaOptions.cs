using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JoinRpg.Portal.Infrastructure.Authentication
{
    public class RecaptchaOptions
    {
        public string PublicKey { get; set; }
        public string PrivateKey { get; set; }
    }
}
