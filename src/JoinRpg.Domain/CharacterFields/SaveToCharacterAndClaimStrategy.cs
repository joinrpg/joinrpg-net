using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Domain.CharacterFields;

internal class SaveToCharacterAndClaimStrategy(Claim claim,
    Character character,
    UserIdentification currentUserId,
    IFieldDefaultValueGenerator generator,
    ProjectInfo projectInfo) : CharacterExistsStrategyBase(claim,
    character,
    currentUserId,
    generator,
    projectInfo)
{
    protected new Claim Claim => base.Claim!; //Claim should always exists

    protected override FieldLayerContainer? BuildClaimFields(FieldLayerContainer working)
        => Layer(working.LayerData.Values.Where(v => v.Field.BoundTo == FieldBoundTo.Claim));

    protected override string CharacterNameFromPlayer() => Claim.Player.GetDisplayName();

    protected override bool FieldIsMandatory(FieldWithValue field) => field.Field.MandatoryStatus == MandatoryStatus.Required && field.Field.IsAvailableForTarget(Character, ProjectInfo);
}
