using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Access;
using JoinRpg.Helpers;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.Plot;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.Web.Models.Print;

public class PrintCharacterViewModel
{
    public string ProjectName { get; }
    public string CharacterName { get; }
    public int FeeDue { get; }
    public IReadOnlyCollection<CharacterGroupLinkSlimViewModel> Groups { get; }
    public User ResponsibleMaster { get; }
    public string? PlayerDisplayName { get; }
    public string? PlayerFullName { get; }
    public PlotDisplayViewModel Plots { get; }
    public IReadOnlyCollection<HandoutListItemViewModel> Handouts { get; }
    public string? PlayerPhoneNumber { get; }
    public CustomFieldsViewModel Fields { get; }
    public bool RegistrationOnHold => FeeDue > 0 || HasUnready;

    public bool HasUnready { get; }

    public PrintCharacterViewModel
      (ICurrentUserAccessor currentUser, Character character, IReadOnlyCollection<PlotTextDto> plots, IUriService uriService, ProjectInfo projectInfo, IReadOnlyCollection<PlotTextDto> handouts)
    {
        ArgumentNullException.ThrowIfNull(character);

        CharacterName = character.CharacterName;
        FeeDue = character.ApprovedClaim?.ClaimFeeDue(projectInfo) ?? character.Project.ProjectFeeInfo()?.Fee ?? 0;
        ProjectName = character.Project.ProjectName;

        Groups =
          [.. character.GetIntrestingGroupsForDisplayToTop()
            .Where(g => g.IsPublic)
            .Select(g => g.ToCharacterGroupLinkSlimViewModel())];
        ResponsibleMaster = character.GetResponsibleMaster();
        PlayerDisplayName = character.ApprovedClaim?.Player.GetDisplayName();
        PlayerFullName = character.ApprovedClaim?.Player.FullName;

        var plotElements = plots;
        HasUnready = !plotElements.All(x => x.Completed) || !handouts.All(x => x.Completed);
        Plots = new PlotDisplayViewModel(plotElements, currentUser, character, projectInfo);

        Handouts = [.. handouts.Select(e => new HandoutListItemViewModel(e))];

        PlayerPhoneNumber = character.ApprovedClaim?.Player.Extra?.PhoneNumber;
        Fields = new CustomFieldsViewModel(
            character,
            projectInfo,
            AccessArgumentsFactory.CreateForPrint(character, currentUser.UserIdOrDefault) with { EditAllowed = false },
            wherePrintEnabled: true);
    }
}
