using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.Web.ProjectMasterTools.Print;
public record class EnvelopeViewModel
    (int FeeDue,
    CharacterIdentification CharacterId,
    UserDisplayName? PlayerDisplayName,
    string CharacterName,
    ProjectMasterInfo ResponsibleMaster,
    IReadOnlyCollection<CharacterGroupLinkSlimViewModel> Groups,
    string? PlayerPhoneNumber,
    string ProjectName,
    string? AccommodationName)
{
}
