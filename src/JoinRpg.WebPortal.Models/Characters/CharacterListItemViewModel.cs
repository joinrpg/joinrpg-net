using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Common.WebComponents;
using JoinRpg.Domain.Problems;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Interfaces;
using JoinRpg.Helpers;
using JoinRpg.Web.Claims;
using JoinRpg.Web.Models.CharacterGroups;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.Web.Models.Characters;

public class CharacterListByGroupViewModel(UserIdentification currentUserId,
    IReadOnlyCollection<CharacterInfo> characters,
    IReadOnlyDictionary<CharacterIdentification, ClaimInfo> approvedClaims,
    IReadOnlyDictionary<UserIdentification, UserInfoHeader> fieldUsers,
    CharacterGroupFullInfo group,
    ProjectInfo projectInfo,
    ICharacterProblemValidator problemValidator) :

    CharacterListViewModel(currentUserId, $"Персонажи — {group.Name}", characters, approvedClaims, fieldUsers, projectInfo, problemValidator), IOperationsAwareView
{
    public CharacterGroupDetailsViewModel GroupModel { get; } =
            new CharacterGroupDetailsViewModel(group,
                projectInfo,
                currentUserId,
                GroupNavigationPage.Characters);

    int? IOperationsAwareView.CharacterGroupId => GroupModel.CharacterGroupId;
    //Не вливаем заголовок в строку с кнопочками, она внутри контрола управления группами.
    string? IOperationsAwareView.InlineTitle => null;
}

/// <param name="approvedClaims">
/// Утверждённые заявки вместе с игроками, загруженные пачкой (ADR021). Агрегат персонажа несёт
/// только id игрока (ADR013), а грузить профили по одному — это N+1 запрос на каждой странице списка.
/// </param>
/// <param name="fieldUsers">
/// Пользователи из полей-ссылок на пользователя (ADR017) — тоже пачкой на весь список.
/// </param>
public class CharacterListViewModel(
    UserIdentification currentUserId,
    string title,
    IReadOnlyCollection<CharacterInfo> characters,
    IReadOnlyDictionary<CharacterIdentification, ClaimInfo> approvedClaims,
    IReadOnlyDictionary<UserIdentification, UserInfoHeader> fieldUsers,
    ProjectInfo projectInfo,
    ICharacterProblemValidator problemValidator) : IOperationsAwareView
{
    public IEnumerable<CharacterListItemViewModel> Items { get; } = characters.Select(
            character =>
                new CharacterListItemViewModel(character,
                    currentUserId,
                    approvedClaims,
                    projectInfo, problemValidator)).ToArray();
    public int? ProjectId { get; } = projectInfo.ProjectId.Value;
    public IReadOnlyCollection<ClaimIdentification> ClaimIds { get; } = characters.Select(c => c.ApprovedClaimId).WhereNotNull().ToArray();
    public IReadOnlyCollection<CharacterIdentification> CharacterIds { get; } = characters.Select(c => c.Id).ToArray();
    public string ProjectName { get; } = projectInfo.ProjectName;
    public string Title { get; } = title;

    public bool HasEditAccess { get; } = projectInfo.HasEditRolesAccess(currentUserId);

    public IReadOnlyCollection<ProjectFieldInfo> Fields { get; } = projectInfo.SortedActiveFields.Where(f => !f.IsName && !f.IsMultiLine).ToArray();

    /// <summary>
    /// Значение поля-ссылки на пользователя: сам <see cref="FieldWithValue.DisplayString"/> отдаёт сырые id.
    /// </summary>
    public IReadOnlyList<UserLinkViewModel> GetUserLinks(FieldWithValue field) => fieldUsers.GetUserLinks(field);

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
    public UserInfo? Player { get; set; }

    [ReadOnly(true), DisplayName("Входит в группы")]
    public CharacterParentGroupsViewModel Groups { get; }

    [Display(Name = "Ответственный мастер")]
    public ProjectMasterInfo Responsible { get; }

    public CharacterListItemViewModel(
        CharacterInfo character,
        UserIdentification currentUserId,
        IReadOnlyDictionary<CharacterIdentification, ClaimInfo> approvedClaims,
        ProjectInfo projectInfo,
        ICharacterProblemValidator problemValidator)
    {
        ArgumentNullException.ThrowIfNull(character);
        ArgumentNullException.ThrowIfNull(approvedClaims);

        BusyStatus = character.GetBusyStatus();

        if (character.ApprovedClaim is { } approvedClaim)
        {
            ApprovedClaimId = approvedClaim.ClaimId.ClaimId;
            Player = approvedClaims.GetValueOrDefault(character.Id)?.Player;
        }
        else if (character.CharacterType == CharacterType.Slot)
        {
            SlotCount = character.CharacterTypeInfo.SlotLimit;
        }

        Name = character.CharacterName;
        CharacterId = character.Id.CharacterId;
        ProjectId = character.Id.ProjectId.Value;
        Fields = character.GetAllFields();
        Problems = problemValidator.Validate(character).Select(p => new ProblemViewModel(p)).ToList();

        Groups = new CharacterParentGroupsViewModel(character, projectInfo.HasMasterAccess(currentUserId));

        Responsible = character.ResponsibleMaster;
    }

    [Display(Name = "Проблемы")]
    public ICollection<ProblemViewModel> Problems { get; set; }

    #region Implementation of ILinkable

    public LinkType LinkType => LinkType.ResultCharacter;
    public string Identification => CharacterId.ToString();
    public int? ProjectId { get; }

    #endregion
}
