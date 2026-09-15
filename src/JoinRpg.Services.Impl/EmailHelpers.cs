using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Impl;

internal static class EmailHelpers
{
    [Obsolete("Письмо не отправляется — отправка закомментирована у всех вызывающих. Восстановить или удалить, см. ADR014")]
    public static FieldsChangedEmail CreateFieldsEmail(
        Claim claim,
        Func<UserSubscription, bool> subscribePredicate,
        User initiator,
        IReadOnlyCollection<FieldWithPreviousAndNewValue> updatedFields)
    {
        ArgumentNullException.ThrowIfNull(claim);

        var subscriptions = claim
            .GetSubscriptions(subscribePredicate)
            .ToList();

        return new FieldsChangedEmail(claim,
            initiator,
            subscriptions,
            updatedFields);
    }

    [Obsolete("Все эти экстраданные должны быть полями персонажа/заявки. Кроме того, письмо не отправляется — отправка закомментирована у всех вызывающих; восстановить или удалить, см. ADR014")]
    public static FieldsChangedEmail CreateFieldsEmailWithExtraData(
        Claim claim,
        Func<UserSubscription, bool> subscribePredicate,
        User initiator,
        IReadOnlyCollection<FieldWithPreviousAndNewValue> updatedFields,
        string extraDataName,
        PreviousAndNewValue extraDataValue)
    {
        ArgumentNullException.ThrowIfNull(claim);

        var subscriptions = claim
            .GetSubscriptions(subscribePredicate)
            .ToList();

        return new FieldsChangedEmail(claim,
            initiator,
            subscriptions,
            updatedFields,
            new Dictionary<string, PreviousAndNewValue>()
            {
                {
                    extraDataName,
                    extraDataValue
                },
            });
    }

    [Obsolete("Письмо не отправляется — отправка закомментирована у всех вызывающих. Восстановить или удалить, см. ADR014")]
    public static FieldsChangedEmail CreateFieldsEmail(
        Character character,
        Func<UserSubscription, bool> subscribePredicate,
        User initiator,
        IReadOnlyCollection<FieldWithPreviousAndNewValue> updatedFields)
    {
        ArgumentNullException.ThrowIfNull(character);

        var subscriptions = character
            .GetSubscriptions(subscribePredicate)
            .ToList();

        return new FieldsChangedEmail(character,
            initiator,
            subscriptions,
            updatedFields);
    }
}
