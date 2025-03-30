using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Access;
using JoinRpg.Helpers;
using JoinRpg.Markdown;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.Characters;
using JoinRpg.Web.Models.Plot;

namespace JoinRpg.Web.Models.Print;

public class PrintCharacterViewModelSlim
{
    public string ProjectName { get; }
    public string CharacterName { get; }
    public int FeeDue { get; }
    public IReadOnlyCollection<CharacterGroupWithDescViewModel> Groups { get; }
    public User ResponsibleMaster { get; }
    public string? PlayerDisplayName { get; }
    public string? PlayerFullName { get; }

    public PrintCharacterViewModelSlim(Character character, ProjectInfo projectInfo)
    {
        CharacterName = character.CharacterName;
        FeeDue = character.ApprovedClaim?.ClaimFeeDue(projectInfo) ?? character.Project.ProjectFeeInfo()?.Fee ?? 0;
        ProjectName = character.Project.ProjectName;

        Groups =
          character.GetParentGroupsToTop()
            .Where(g => !g.IsSpecial && g.IsActive && g.IsPublic && !g.IsRoot)
            .Distinct()
            .Select(g => new CharacterGroupWithDescViewModel(g))
            .ToArray();
        ResponsibleMaster = character.GetResponsibleMasterOrDefault() ?? character.Project.GetDefaultResponsibleMaster();
        PlayerDisplayName = character.ApprovedClaim?.Player.GetDisplayName();
        PlayerFullName = character.ApprovedClaim?.Player.FullName;
    }
}

public class PrintCharacterViewModel : PrintCharacterViewModelSlim
{
    public PlotDisplayViewModel Plots { get; }
    public IReadOnlyCollection<HandoutListItemViewModel> Handouts { get; }
    public string? PlayerPhoneNumber { get; }
    public CustomFieldsViewModel Fields { get; }
    public bool RegistrationOnHold => FeeDue > 0 || Plots.HasUnready;

    public PrintCharacterViewModel
      (int currentUserId, Character character, IReadOnlyCollection<PlotElement> plots, IUriService uriService, ProjectInfo projectInfo)
      : base(character, projectInfo)
    {
        ArgumentNullException.ThrowIfNull(character);

        var plotElements = character.GetOrderedPlots(character.SelectPlots(plots)).ToArray();
        Plots = PlotDisplayViewModel.Published(plotElements, currentUserId, character, uriService, projectInfo);

        Handouts =
          plotElements.Where(e => e.ElementType == PlotElementType.Handout && e.IsActive)
            .Select(e => e.PublishedVersion())
            .WhereNotNull()
            .Select(e => new HandoutListItemViewModel(e.Content.ToPlainText(), e.AuthorUser))
            .ToArray();

        PlayerPhoneNumber = character.ApprovedClaim?.Player.Extra?.PhoneNumber;
        Fields = new CustomFieldsViewModel(
            character,
            projectInfo,
            AccessArgumentsFactory.CreateForPrint(character, currentUserId) with { EditAllowed = false },
            wherePrintEnabled: true);
    }
}

public class HandoutListItemViewModel : HandoutViewModelBase
{
    public HandoutListItemViewModel(string text, User master) : base(text, master)
    {
    }
}
