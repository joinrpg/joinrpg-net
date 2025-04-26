using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.Web.ProjectMasterTools.Print;

namespace JoinRpg.Web.Models.Print;
public static class ModelBuilders
{
    public static EnvelopeViewModel ToEnvelopeViewModel(this Character character, ProjectInfo projectInfo)
    {
        var respMasterId = character.GetResponsibleMaster().UserId;
        return new EnvelopeViewModel(
            FeeDue: character.ApprovedClaim?.ClaimFeeDue(projectInfo) ?? character.Project.ProjectFeeInfo()?.Fee ?? 0,
            AccommodationName:
                projectInfo.AccomodationEnabled ? character.ApprovedClaim?.AccommodationRequest?.Accommodation?.Name ?? "нет"
                : null,
            CharacterId: character.GetId(),
            PlayerDisplayName: character.ApprovedClaim?.Player?.ExtractDisplayName(),
            CharacterName: character.CharacterName,
            ResponsibleMaster: projectInfo.Masters.First(m => m.UserId == respMasterId),
            Groups: [.. character.GetIntrestingGroupsForDisplayToTop()
            .Where(g => g.IsPublic)
            .Select(g => g.ToCharacterGroupLinkSlimViewModel())],
            PlayerPhoneNumber: character.ApprovedClaim?.Player.Extra?.PhoneNumber,
            ProjectName: projectInfo.ProjectName
            );
    }

    public static CharacterGroupLinkSlimViewModel ToCharacterGroupLinkSlimViewModel(this CharacterGroup g) => new(g.GetId(), g.CharacterGroupName, g.IsPublic, g.IsActive);
}
