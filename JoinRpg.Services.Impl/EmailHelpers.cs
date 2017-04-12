using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl
{
  internal static class EmailHelpers
  {
    public static TEmail CreateClaimEmail<TEmail>([NotNull] Claim claim, [NotNull] string commentText, Func<UserSubscription, bool> subscribePredicate, bool isVisibleToPlayer, CommentExtraAction? commentExtraAction, User initiator, IEnumerable<User> extraRecepients = null)
      where TEmail : ClaimEmailModel, new()
    {
      if (claim == null) throw new ArgumentNullException(nameof(claim));
      if (commentText == null) throw new ArgumentNullException(nameof(commentText));
      var subscriptions =
        claim.GetSubscriptions(subscribePredicate, extraRecepients ?? Enumerable.Empty<User>(),
          isVisibleToPlayer).ToList();
      return new TEmail()
      {
        Claim = claim,
        ProjectName = claim.Project.ProjectName,
        Initiator = initiator,
        InitiatorType = initiator.UserId == claim.PlayerUserId ? ParcipantType.Player : ParcipantType.Master,
        Recepients = subscriptions.ToList(),
        Text = new MarkdownString(commentText),
        CommentExtraAction = commentExtraAction
      };
    }
  }
}
