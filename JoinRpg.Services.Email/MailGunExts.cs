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
    //TODO: Quotes should be added on mailgun_sharp level, but while we are not here...
      => new Recipient { DisplayName = "\"" + user.DisplayName + "\"", Email = user.Email };

    public static JObject ToRecepientVariables(this IEnumerable<User> recepients)
    {
      var recipientVars = new JObject();
      foreach (var r in recepients)
      {
        recipientVars.Add(r.Email, new JObject {{MailGunName, r.DisplayName}});
      }
      return recipientVars;
    }

  }
}
