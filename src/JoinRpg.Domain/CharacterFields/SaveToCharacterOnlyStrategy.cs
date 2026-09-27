using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Domain.CharacterFields;

internal class SaveToCharacterOnlyStrategy(
    Character character,
    UserIdentification currentUserId,
    IFieldDefaultValueGenerator generator,
    ProjectInfo projectInfo,
    IReadOnlyCollection<CharacterGroupIdentification> regularGroupIds) : CharacterExistsStrategyBase(claim: null,
    character,
    currentUserId,
    generator,
    projectInfo,
    regularGroupIds)
{
    protected override string CharacterNameFromPlayer()
    {
        //TODO: we don't have player yet, but have to set player name from it.
        //M.b. Disallow create characters in this scenarios?
        return Character.CharacterName ?? "PLAYER_NAME";
    }

    /// <summary>Заявки нет — писать поля заявки некуда.</summary>
    protected override FieldLayerContainer? BuildClaimFields(FieldLayerContainer working) => null;

    protected override bool FieldIsMandatory(FieldWithValue field) =>
        field.Field.MandatoryStatus == MandatoryStatus.Required
        && field.Field.BoundTo == FieldBoundTo.Character // Игнорируем пустые поля заявок в данном случае
        && field.Field.IsAvailableForTarget(AvailabilityTarget);
}
