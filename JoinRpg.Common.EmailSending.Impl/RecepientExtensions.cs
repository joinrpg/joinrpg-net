using System.Collections.Generic;
using System.Linq;
using JoinRpg.Services.Interfaces.Email;
using Mailgun.Core.Messages;
using Mailgun.Messages;
using Newtonsoft.Json.Linq;

namespace JoinRpg.Common.EmailSending.Impl
{
    internal static class RecepientExtensions
    {
        public static IMessageBuilder AddUsers(this IMessageBuilder builder,
            IEnumerable<RecepientData> recipients) => builder.AddToRecipientList(recipients.Where(x => x != null).Distinct().Select(x => x.ToMailGunRecepient()));

        public static IRecipient ToMailGunRecepient(this RecepientData recipient)
        {
            return new Recipient()
            {
                DisplayName = recipient.DisplayName,
                Email = recipient.Email,
            };
        }


        public static JObject ToRecipientVariables(
            this IReadOnlyCollection<RecepientData> recipients)
        {
            var recipientVars = new JObject();
            foreach (var r in recipients)
            {
                var jobj = new JObject
                {
                    { Constants.MailGunName, r.DisplayName }
                };

                foreach (var nameAndValue in r.RecipientSpecificValues)
                {
                    jobj.Add(nameAndValue.Key, nameAndValue.Value);
                }

                recipientVars.Add(r.Email, jobj);
            }

            return recipientVars;
        }

    }
}
