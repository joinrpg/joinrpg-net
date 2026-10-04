using JoinRpg.DataModel;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Services.Impl;

internal class FieldDefaultValueGenerator : IFieldDefaultValueGenerator
{
    public string? CreateDefaultValue(Claim? claim, FieldWithValue field)
    {
        if (field.Field.IsScheduleAuthor && claim is { IsApproved: true })
        {
            return claim.PlayerUserId.ToString();
        }
        return null;
    }

    public string? CreateDefaultValue(Character? character, FieldWithValue field)
    {
        if (field.Field.IsScheduleAuthor && character?.ApprovedClaim is { } approvedClaim)
        {
            // Пустой ведущий и так показывается как игрок утверждённой заявки, а записанное
            // значение мастеру видно и его можно поправить
            return approvedClaim.PlayerUserId.ToString();
        }


        if (field.Field.IsName && character != null)
        {
            return character.CharacterName;
            // It helps battle akward situations where names was re-bound to some new field
            // and empty values start overwriting names
        }

        if (field.Field.Type == ProjectFieldType.PinCode)
        {
            return Random.Shared.Next(9999).ToString("D4");
        }
        return null;
    }
}
