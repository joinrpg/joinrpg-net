using JoinRpg.Data.Interfaces;
using JoinRpg.Domain.Access;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.Interfaces;

namespace JoinRpg.Domain.CharacterFields;

public static class CharacterFieldLayersBuilder
{
    public static CharacterFieldLayers FromCharacter(ProjectInfo projectInfo, Character character, ICurrentUserAccessor user)
        => new(
            ClaimLayer: FieldLayerContainer.DeserializeFieldLayer(projectInfo, character.ApprovedClaim?.JsonData),
            CharacterLayer: FieldLayerContainer.DeserializeFieldLayer(projectInfo, character.JsonData),
            AccessArguments: AccessArgumentsFactory.Create(character, user, projectInfo));

    public static CharacterFieldLayers FromCharacterView(ProjectInfo projectInfo, CharacterView character, ICurrentUserAccessor user)
        => new(
            ClaimLayer: FieldLayerContainer.DeserializeFieldLayer(projectInfo, character.ApprovedClaim?.JsonData),
            CharacterLayer: FieldLayerContainer.DeserializeFieldLayer(projectInfo, character.JsonData),
            AccessArguments: AccessArgumentsFactory.Create(character, user.UserIdentificationOrDefault, projectInfo));

    public static CharacterFieldLayers FromClaim(ProjectInfo projectInfo, Claim claim, ICurrentUserAccessor user)
        => new(
            ClaimLayer: FieldLayerContainer.DeserializeFieldLayer(projectInfo, claim.JsonData),
            CharacterLayer: FieldLayerContainer.DeserializeFieldLayer(projectInfo, claim.Character.JsonData),
            AccessArguments: AccessArgumentsFactory.Create(claim, user, projectInfo));
}
