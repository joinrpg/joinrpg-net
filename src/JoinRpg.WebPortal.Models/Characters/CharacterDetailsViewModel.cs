using System.ComponentModel;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.Plot;
using JoinRpg.Web.Models.UserProfile;
using JoinRpg.WebComponents;

namespace JoinRpg.Web.Models.Characters;

public class CharacterParentGroupsViewModel
{
    public bool HasMasterAccess { get; }
    public bool HasAnyGroups { get; }

    [ReadOnly(true), DisplayName("Входит в группы")]
    public IReadOnlyCollection<CharacterGroupLinkViewModel> ParentGroups { get; }

    public CharacterParentGroupsViewModel(Character character, bool hasMasterAccess)
    {
        ArgumentNullException.ThrowIfNull(character);

        HasMasterAccess = hasMasterAccess;
        ParentGroups = character
          .GetParentGroupsToTop()
          .Where(group => !group.IsRoot && !group.IsSpecial)
          .Select(g => new CharacterGroupLinkViewModel(g)).ToList();
        HasAnyGroups = ParentGroups.Count > 0;
    }
}

//TODO merge everything into UserLinkViewModel and remove this interface
public interface ICharacterWithPlayerViewModel
{
    UserLinkViewModel? PlayerLink { get; }
    bool HidePlayer { get; }
    bool HasAccess { get; }
}

public class CharacterDetailsViewModel : ICharacterWithPlayerViewModel, ICreatedUpdatedTracked
{
    [ReadOnly(true), DisplayName("Входит в группы")]
    public CharacterParentGroupsViewModel ParentGroups { get; }

    public UserLinkViewModel? PlayerLink { get; }

    public PlotDisplayViewModel Plot { get; }

    public bool HidePlayer { get; }
    public bool HasAccess { get; }

    public CustomFieldsViewModel Fields { get; }

    public CharacterNavigationViewModel Navigation { get; }
    public bool HasMasterAccess { get; }

    public CharacterDetailsViewModel(
        int? currentUserIdOrDefault,
        Character character,
        IReadOnlyCollection<PlotElement> plots,
        IUriService uriService,
        ProjectInfo projectInfo)
    {
        PlayerLink = UserLinks.Create(character.ApprovedClaim?.Player);
        HasAccess = character.HasAnyAccess(currentUserIdOrDefault);
        ParentGroups = new CharacterParentGroupsViewModel(character, character.HasMasterAccess(currentUserIdOrDefault));
        HidePlayer = character.HidePlayerForCharacter;
        Navigation =
          CharacterNavigationViewModel.FromCharacter(character, CharacterNavigationPage.Character,
            currentUserIdOrDefault);
        Fields = new CustomFieldsViewModel(currentUserIdOrDefault, character, projectInfo, disableEdit: true);
        Plot = PlotDisplayViewModel.Published(plots, currentUserIdOrDefault, character, uriService);

        HasMasterAccess = character.HasMasterAccess(currentUserIdOrDefault);
        CreatedAt = character.CreatedAt;
        UpdatedAt = character.UpdatedAt;
        CreatedBy = character.CreatedBy;
        UpdatedBy = character.UpdatedBy;
    }

    public DateTime CreatedAt { get; }
    public User CreatedBy { get; }
    public DateTime UpdatedAt { get; }
    public User UpdatedBy { get; }
}
