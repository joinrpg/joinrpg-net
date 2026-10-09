using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Common.WebComponents;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Access;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.Interfaces;
using JoinRpg.Markdown;
using JoinRpg.Web.Models.Plot;
using JoinRpg.Web.ProjectCommon;

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

    /// <summary>
    /// Версия поверх доменного агрегата персонажа (ADR013).
    /// </summary>
    public CharacterParentGroupsViewModel(CharacterInfo character, bool hasMasterAccess)
    {
        ArgumentNullException.ThrowIfNull(character);

        HasMasterAccess = hasMasterAccess;
        ParentGroups = [.. character
          .DirectGroups
          .Where(group => !group.IsRoot && !group.IsSpecial)
          .Select(g => new CharacterGroupLinkViewModel(g))];
        HasAnyGroups = ParentGroups.Count > 0;
    }
}

public class CharacterDetailsViewModel
{
    [ReadOnly(true), DisplayName("Входит в группы")]
    public CharacterParentGroupsViewModel ParentGroups { get; }

    public UserLinkViewModel? PlayerLink { get; }

    public PlotDisplayViewModel Plot { get; }

    public CustomFieldsViewModel Fields { get; }

    public CharacterNavigationViewModel Navigation { get; }
    public bool HasMasterAccess { get; }

    /// <param name="linkRenderer">
    /// Разворачивает директивы в тексте вводных (<c>%персонаж</c>, <c>%список</c>). Собирается
    /// вызывающим через <c>JoinrpgMarkdownLinkRendererFactory</c>.
    /// </param>
    public CharacterDetailsViewModel(
        ICurrentUserAccessor currentUserId,
        Character character,
        CharacterInfo characterInfo,
        IReadOnlyCollection<PlotTextDto> plots,
        ILinkRenderer linkRenderer,
        ProjectInfo projectInfo,
        IReadOnlyDictionary<UserIdentification, UserInfoHeader> fieldUsers,
        CreateUpdateMarksInfo? marks)
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
            accessArguments,
            fieldUsers
            );
        Plot = new PlotDisplayViewModel(plots, currentUserId, characterInfo, linkRenderer);

        HasMasterAccess = accessArguments.MasterAccess;
        Marks = marks?.ToViewModel();
    }

    /// <summary>
    /// Кто и когда создал и последним менял персонажа; показывается только мастерам, поэтому
    /// остальным не загружается.
    /// </summary>
    public CreateUpdateMarksViewModel? Marks { get; }
}
