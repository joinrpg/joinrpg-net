using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Problems;
using JoinRpg.Helpers;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Web.Models.CharacterGroups;

namespace JoinRpg.Web.Models.Characters;

public class CharacterListByGroupViewModel(int currentUserId,
    IReadOnlyCollection<Character> characters,
    CharacterGroup group,
    ProjectInfo projectInfo,
    IProblemValidator<Character> problemValidator) :

    CharacterListViewModel(currentUserId, $"Персонажи — {group.CharacterGroupName}", characters, group.Project, projectInfo, problemValidator), IOperationsAwareView
{
    public CharacterGroupDetailsViewModel GroupModel { get; } =
            new CharacterGroupDetailsViewModel(group,
                currentUserId,
                GroupNavigationPage.Characters);

    int? IOperationsAwareView.CharacterGroupId => GroupModel.CharacterGroupId;
    //Не вливаем заголовок в строку с кнопочками, она внутри контрола управления группами.
    string? IOperationsAwareView.InlineTitle => null;
}

public class CharacterListViewModel(
    int currentUserId,
    string title,
    IReadOnlyCollection<Character> characters,
    Project project,
    ProjectInfo projectInfo,
    IProblemValidator<Character> problemValidator) : IOperationsAwareView
{
    public IEnumerable<CharacterListItemViewModel> Items { get; } = characters.Select(
            character =>
                new CharacterListItemViewModel(character,
                    currentUserId,
                    projectInfo, problemValidator)).ToArray();
    public int? ProjectId { get; } = projectInfo.ProjectId.Value;
    public IReadOnlyCollection<int> ClaimIds { get; } = characters.Select(c => c.ApprovedClaim?.ClaimId).WhereNotNull().ToArray();
    public IReadOnlyCollection<int> CharacterIds { get; } = characters.Select(c => c.CharacterId).ToArray();
    public string ProjectName { get; } = projectInfo.ProjectName;
    public string Title { get; } = title;

    public bool HasEditAccess { get; } = project.HasEditRolesAccess(currentUserId);

    public IReadOnlyCollection<ProjectFieldInfo> Fields { get; } = projectInfo.SortedActiveFields.Where(f => !f.IsName && !f.IsMultiLine).ToArray();

    public string? CountString => CountHelper.DisplayCount(Items.Count(), "персонаж", "персонажа", "персонажей");

    string? IOperationsAwareView.InlineTitle => Title;
}

public class CharacterListItemViewModel : ILinkable
{
    [Display(Name = "Занят?")]
    public CharacterBusyStatusView BusyStatus { get; }

    public int? SlotCount { get; }

    [Display(Name = "Персонаж")]
    public string Name { get; set; }

    public int CharacterId { get; }

    [ReadOnly(true)]
    public IReadOnlyCollection<FieldWithValue> Fields { get; }

    public int? ApprovedClaimId { get; }

    [Display(Name = "Игрок")]
    public User? Player { get; set; }

    [ReadOnly(true), DisplayName("Входит в группы")]
    public CharacterParentGroupsViewModel Groups { get; }

    [Display(Name = "Ответственный мастер")]
    public User? Responsible { get; }

    public CharacterListItemViewModel(
        Character character,
        int currentUserId,
        ProjectInfo projectInfo,
        IProblemValidator<Character> problemValidator)
    {
        if (character == null)
        {
            throw new ArgumentNullException(nameof(character));
        }

        BusyStatus = character.GetBusyStatus();

        if (character.ApprovedClaim != null)
        {
            ApprovedClaimId = character.ApprovedClaim.ClaimId;
            Player = character.ApprovedClaim.Player;
        }
        else if (character.CharacterType == CharacterType.Slot)
        {
            SlotCount = character.CharacterSlotLimit;
        }

        Name = character.CharacterName;
        CharacterId = character.CharacterId;
        ProjectId = character.ProjectId;
        Fields = character.GetFields(projectInfo);
        Problems = problemValidator.Validate(character, projectInfo).Select(p => new ProblemViewModel(p)).ToList();

        Groups = new CharacterParentGroupsViewModel(character,
            character.HasMasterAccess(currentUserId));

        Responsible = character.GetResponsibleMasterOrDefault();
    }

    [Display(Name = "Проблемы")]
    public ICollection<ProblemViewModel> Problems { get; set; }

    #region Implementation of ILinkable

    public LinkType LinkType => LinkType.ResultCharacter;
    public string Identification => CharacterId.ToString();
    public int? ProjectId { get; }

    #endregion
}
