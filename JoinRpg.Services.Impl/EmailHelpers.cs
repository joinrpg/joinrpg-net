using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Impl
{
    internal static class EmailHelpers
    {
        public static FieldsChangedEmail CreateFieldsEmail([NotNull]
            Claim claim,
            Func<UserSubscription, bool> subscribePredicate,
            User initiator,
            [NotNull]
            IReadOnlyCollection<FieldWithPreviousAndNewValue> updatedFields,
            IReadOnlyDictionary<string, PreviousAndNewValue> otherChangedAttributes = null)
        {
            if (claim == null) throw new ArgumentNullException(nameof(claim));
            var subscriptions = claim
                .GetSubscriptions(subscribePredicate)
                .ToList();

            return new FieldsChangedEmail(claim,
                initiator,
                subscriptions,
                updatedFields,
                otherChangedAttributes);
        }

        public static FieldsChangedEmail CreateFieldsEmail(
            [NotNull]
            Character character,
            Func<UserSubscription, bool> subscribePredicate,
            User initiator,
            IReadOnlyCollection<FieldWithPreviousAndNewValue> updatedFields,
            IReadOnlyDictionary<string, PreviousAndNewValue> otherChangedAttributes = null)
        {
            if (character == null) throw new ArgumentNullException(nameof(character));

            var subscriptions = character
                .GetSubscriptions(subscribePredicate)
                .ToList();

            return new FieldsChangedEmail(character,
                initiator,
                subscriptions,
                updatedFields,
                otherChangedAttributes);
        }
    }
}
