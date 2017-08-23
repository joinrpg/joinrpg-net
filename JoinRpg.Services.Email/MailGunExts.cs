using JoinRpg.DataModel;
using JoinRpg.Helpers;
using Mailgun.Core.Messages;
using Mailgun.Messages;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace JoinRpg.Services.Email
{
  internal static class MailGunExts
  {
    private const string MailGunName = "name";
    public static readonly string MailGunRecipientName = GetUserDependentValue(MailGunName);
    public static string GetUserDependentValue(string valueKey) => "%recipient." + valueKey + "%";

    public static IMessageBuilder AddUsers(this IMessageBuilder builder, IEnumerable<MailRecipient> recipients)
    {
      foreach (var recipient in recipients.WhereNotNull().Distinct())
      {
        builder.AddToRecipient(recipient.User.ToRecipient());
      }
      return builder;
    }

    
    public static Recipient ToRecipient(this User user) 
      => new Recipient { DisplayName = user.DisplayName, Email = user.Email };

    public static JObject ToRecipientVariables(this IReadOnlyCollection<MailRecipient> recipients)
    {
      var recipientVars = new JObject();
      foreach (var r in recipients)
      {
        var jobj = new JObject();
        jobj.Add(MailGunName, r.User.DisplayName);

        foreach (var nameAndValue in r.RecipientSpecificValues)
        {
          jobj.Add(nameAndValue.Key, nameAndValue.Value);
        }

        recipientVars.Add(r.User.Email, jobj);
      }
      return recipientVars;
    }

  }
}
