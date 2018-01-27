using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JoinRpg.DataModel;

namespace JoinRpg.Services.Interfaces
{
    public class RemindPasswordEmail
    {
        public string CallbackUrl { get; set; }
        public User Recipient { get; set; }
    }

    public class ConfirmEmail
    {
        public string CallbackUrl
        { get; set; }
        public User Recipient
        { get; set; }
    }
}
