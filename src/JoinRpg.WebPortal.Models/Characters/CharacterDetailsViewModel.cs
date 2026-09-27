using JoinRpg.Common.WebComponents;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Access;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.Interfaces;
using JoinRpg.Web.Models.Plot;

namespace JoinRpg.Web.Models.Characters;

public class CharacterParentGroupsViewModel
{
    public bool HasMasterAccess { get; }
    public bool HasAnyGroups { get; }

    [ReadOnly(true), DisplayName("Входит в группы")]
    public IReadOnlyCollection<CharacterGroupLinkViewModel> ParentGroups { get; }

    public CharacterParentGroupsViewModel(Character character, bool hasMasterAccess, ProjectInfo projectInfo)
    {
        ArgumentNullException.ThrowIfNull(character);

        HasMasterAccess = hasMasterAccess;
        ParentGroups = character
          .GetDirectGroups(projectInfo)
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

    /// <param name="projectForRendering">
    /// EF-граф проекта для рендеринга директив в тексте вводных (<c>%персонаж</c>, <c>%список</c>) —
    /// см. <c>IProjectRepository.GetProjectForMarkdownRendering</c>.
    /// </param>
    public CharacterDetailsViewModel(
        ICurrentUserAccessor currentUserId,
        Character character,
        CharacterInfo characterInfo,
        IReadOnlyCollection<PlotTextDto> plots,
        Project projectForRendering,
        ProjectInfo projectInfo)
    {
        // Ссылка на игрока строится поверх агрегата (ADR013): у варианта поверх EF-сущности внутри
        // лежит character.Project.Details.PublishPlot — ленивая загрузка на каждый заход (#4992).
        PlayerLink = characterInfo.GetCharacterPlayerLinkViewModel(currentUserId.UserIdentificationOrDefault);

        var accessArguments = AccessArgumentsFactory.Create(character, currentUserId, projectInfo) with { EditAllowed = false };

        ParentGroups = new CharacterParentGroupsViewModel(character, accessArguments.MasterAccess, projectInfo);
        Navigation =
          CharacterNavigationViewModel.FromCharacter(characterInfo, CharacterNavigationPage.Character,
            currentUserId.UserIdentificationOrDefault);

        Fields = new CustomFieldsViewModel(
            character,
            projectInfo,
            accessArguments
            );
        Plot = new PlotDisplayViewModel(plots, currentUserId, character, projectForRendering, projectInfo);

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
