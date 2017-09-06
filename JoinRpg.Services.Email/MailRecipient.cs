using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.DataModel;

namespace JoinRpg.Services.Email
{ 
  class MailRecipient
  {
    [NotNull]
    public User User;
    /// <summary>
    /// A dictionary of named recipient-specific values, where key is value name
    /// </summary>
    [NotNull]
    public Dictionary<string, string> RecipientSpecificValues;

    public MailRecipient([NotNull] User user,
      Dictionary<string, string> recipientSpecificValues = null)
    {
      if (user == null) throw new ArgumentNullException(nameof(user));

      User = user;
      RecipientSpecificValues = recipientSpecificValues ?? new Dictionary<string, string>();
    }
  }
}
