using System.ComponentModel;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Access;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.Plot;
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

public class CharacterDetailsViewModel : ICreatedUpdatedTracked
{
    [ReadOnly(true), DisplayName("Входит в группы")]
    public CharacterParentGroupsViewModel ParentGroups { get; }

    public UserLinkViewModel? PlayerLink { get; }

    public PlotDisplayViewModel Plot { get; }

    public CustomFieldsViewModel Fields { get; }

    public CharacterNavigationViewModel Navigation { get; }
    public bool HasMasterAccess { get; }

    public CharacterDetailsViewModel(
        ICurrentUserAccessor currentUserId,
        Character character,
        IReadOnlyCollection<PlotElement> plots,
        IUriService uriService,
        ProjectInfo projectInfo)
    {

        PlayerLink = character.GetCharacterPlayerLinkViewModel(currentUserId.UserIdOrDefault);

        var accessArguments = AccessArgumentsFactory.Create(character, currentUserId.UserIdOrDefault) with { EditAllowed = false };

        ParentGroups = new CharacterParentGroupsViewModel(character, accessArguments.MasterAccess);
        Navigation =
          CharacterNavigationViewModel.FromCharacter(character, CharacterNavigationPage.Character,
            currentUserId.UserIdOrDefault);

        Fields = new CustomFieldsViewModel(
            character,
            projectInfo,
            accessArguments
            );
        Plot = PlotDisplayViewModel.Published(plots, currentUserId, character, uriService, projectInfo);

        HasMasterAccess = accessArguments.MasterAccess;
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
