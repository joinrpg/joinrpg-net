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
        Claim? approvedClaim = character.ApprovedClaim;
        return new EnvelopeViewModel(
            FeeDue: approvedClaim?.ClaimFeeDue(projectInfo) ?? character.Project.ProjectFeeInfo()?.Fee ?? 0,
            AccommodationEnabled: projectInfo.AccomodationEnabled,
            AccommodationTypeName: approvedClaim?.AccommodationRequest?.AccommodationType?.Name,
            AccommodationName: approvedClaim?.AccommodationRequest?.Accommodation?.Name,
            CharacterId: character.GetId(),
            PlayerDisplayName: approvedClaim?.Player?.ExtractDisplayName(),
            CharacterName: character.CharacterName,
            ResponsibleMaster: projectInfo.Masters.First(m => m.UserId == respMasterId).ToUserLinkViewModel(),
            Groups: [.. character.GetIntrestingGroupsForDisplayToTop()
            .Where(g => g.IsPublic)
            .Select(g => g.ToCharacterGroupLinkSlimViewModel())],
            PlayerPhoneNumber: approvedClaim?.Player.Extra?.PhoneNumber,
            ProjectName: projectInfo.ProjectName
            );
    }

    public static CharacterGroupLinkSlimViewModel ToCharacterGroupLinkSlimViewModel(this CharacterGroup g) => new(g.GetId(), g.CharacterGroupName, g.IsPublic, g.IsActive);
}
