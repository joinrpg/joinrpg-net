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
    public const string MailGunRecepientName = "%recipient." + MailGunName + "%";

    public static IMessageBuilder AddUsers(this IMessageBuilder builder, IEnumerable<User> users)
    {
      foreach (var user in users.WhereNotNull().Distinct())
      {
        builder.AddToRecipient(user.ToRecipient());
      }
      return builder;
    }

    public static Recipient ToRecipient(this User user)
    {
      return new Recipient() { DisplayName = user.DisplayName, Email = user.Email };
    }

    public static JObject ToRecepientVariables(this IReadOnlyCollection<User> recepients)
    {
      var recipientVars = new JObject();
      foreach (var r in recepients)
      {
        var jobj = new JObject();
        jobj.Add(MailGunName, r.DisplayName);
        recipientVars.Add(r.Email, jobj);
      }
      return recipientVars;
    }

  }
}
