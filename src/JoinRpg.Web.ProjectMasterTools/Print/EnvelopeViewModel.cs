using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.Web.ProjectMasterTools.Print;
public record class EnvelopeViewModel
    (int FeeDue,
    CharacterIdentification CharacterId,
    UserDisplayName? PlayerDisplayName,
    string CharacterName,
    UserLinkViewModel ResponsibleMaster,
    IReadOnlyCollection<CharacterGroupLinkSlimViewModel> Groups,
    string? PlayerPhoneNumber,
    string ProjectName,
    bool AccommodationEnabled,
    string? AccommodationTypeName,
    string? AccommodationName)
{
}
