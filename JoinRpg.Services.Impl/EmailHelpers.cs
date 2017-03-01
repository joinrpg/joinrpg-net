using System;
using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl
{
  internal static class EmailHelpers
  {
    public static TEmail CreateClaimEmail<TEmail>(Claim claim, int currentUserId, string commentText, Func<UserSubscription, bool> subscribePredicate, bool isVisibleToPlayer, CommentExtraAction? commentExtraAction, User initiator, IEnumerable<User> extraRecepients = null)
      where TEmail : ClaimEmailModel, new()
    {
      var subscriptions =
        claim.GetSubscriptions(subscribePredicate, extraRecepients ?? Enumerable.Empty<User>(),
          isVisibleToPlayer).ToList();
      return new TEmail()
      {
        Claim = claim,
        ProjectName = claim.Project.ProjectName,
        Initiator = initiator,
        InitiatorType = currentUserId == claim.PlayerUserId ? ParcipantType.Player : ParcipantType.Master,
        Recepients = subscriptions.ToList(),
        Text = new MarkdownString(commentText),
        CommentExtraAction = commentExtraAction
      };
    }
  }
}
