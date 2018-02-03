using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Email;

namespace JoinRpg.Services.Impl
{
  internal static class EmailHelpers
  {
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
