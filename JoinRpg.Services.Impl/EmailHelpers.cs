using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl
{
  internal static class EmailHelpers
  {
    public static TEmail CreateClaimEmail<TEmail>(
      [NotNull] Claim claim,
      [NotNull] string commentText,
      Func<UserSubscription, bool> subscribePredicate,
      CommentExtraAction? commentExtraAction,
      User initiator,
      bool mastersOnly = false,
      IEnumerable<User> extraRecipients = null)
        where TEmail : ClaimEmailModel, new()
    {
      if (claim == null) throw new ArgumentNullException(nameof(claim));
      if (commentText == null) throw new ArgumentNullException(nameof(commentText));
      var subscriptions =
        claim.GetSubscriptions(subscribePredicate, extraRecipients ?? Enumerable.Empty<User>(),
          mastersOnly).ToList();
      return new TEmail()
      {
        Claim = claim,
        ProjectName = claim.Project.ProjectName,
        Initiator = initiator,
        InitiatorType = initiator.UserId == claim.PlayerUserId ? ParcipantType.Player : ParcipantType.Master,
        Recipients = subscriptions.ToList(),
        Text = new MarkdownString(commentText),
        CommentExtraAction = commentExtraAction
      };
    }

    public static FieldsChangedEmail CreateFieldsEmail(
      [NotNull] Claim claim,
      Func<UserSubscription, bool> subscribePredicate,
      User initiator,
      IReadOnlyCollection<FieldWithPreviousAndNewValue> updatedFields)
    {
      if (claim == null) throw new ArgumentNullException(nameof(claim));
      var subscriptions = claim
        .GetSubscriptions(subscribePredicate)
        .ToList();

      return new FieldsChangedEmail(claim, initiator, subscriptions, updatedFields);
    }

    public static FieldsChangedEmail CreateFieldsEmail(
      [NotNull] Character character,
      Func<UserSubscription, bool> subscribePredicate,
      User initiator,
      IReadOnlyCollection<FieldWithPreviousAndNewValue> updatedFields,
      Dictionary<string, PreviousAndNewValue> otherChangedAttributes)
    {
      if (character == null) throw new ArgumentNullException(nameof(character));

      var subscriptions = character
        .GetSubscriptions(subscribePredicate)
        .ToList();

      return new FieldsChangedEmail(character, initiator, subscriptions, updatedFields, otherChangedAttributes);
    }
  }
}
