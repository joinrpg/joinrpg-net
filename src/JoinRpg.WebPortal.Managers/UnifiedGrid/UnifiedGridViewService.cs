using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Interfaces;
using JoinRpg.Web.Claims.UnifiedGrid;

namespace JoinRpg.WebPortal.Managers.UnifiedGrid;

internal class UnifiedGridViewService(
    ICurrentUserAccessor currentUserAccessor,
    ICaptainRulesRepository captainRulesRepository,
    IProjectMetadataRepository projectMetadataRepository,
    ICharacterInfoRepository characterInfoRepository) : IUnifiedGridClient
{
    async Task<IReadOnlyCollection<UgItemForCaptainViewModel>> IUnifiedGridClient.GetForCaptain(ProjectIdentification projectId, UgStatusFilterView filter)
    {
        var access = await captainRulesRepository.GetCaptainRules(projectId, currentUserAccessor.UserIdentification);
        if (access.Count == 0)
        {
            return [];
        }
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        var allGroups = projectInfo.GroupTree.GetChildGroupIdsIncludingThis([.. access.Select(x => x.CharacterGroup)]);

        var characters = await characterInfoRepository.GetCharacterInfosByGroups(
            projectId, [.. allGroups], StatusSpecFor(filter));

        return
        [
            .. characters
                .Where(character => MatchesFilter(character, filter))
                .Select(character => ItemBuilder.BuildItemForCaptain(character, SelectClaims(character, filter), currentUserAccessor))
                .WhereNotNull()
        ];
    }

    /// <summary>
    /// Что просить у репозитория: архиву нужны и удалённые персонажи, и живые (с отклонёнными
    /// заявками), всем остальным видам — только живые.
    /// Отбор внутри этого набора всё равно делается в памяти (см. <see cref="MatchesFilter"/>),
    /// но тащить агрегат на заведомо ненужных персонажей незачем.
    /// </summary>
    private static CharacterStatusSpec StatusSpecFor(UgStatusFilterView filter)
        => filter == UgStatusFilterView.Archive ? CharacterStatusSpec.Any : CharacterStatusSpec.Active;

    /// <summary>
    /// Отбор персонажей под выбранный фильтр.
    /// </summary>
    /// <remarks>
    /// Раньше это был SQL-предикат (<c>CharacterPredicates.ByUgStatus</c>), но агрегат
    /// <see cref="CharacterInfo"/> по построению несёт все заявки персонажа, поэтому отбор считается
    /// в памяти — правила ровно те же.
    /// </remarks>
    private static bool MatchesFilter(CharacterInfo character, UgStatusFilterView filter)
        => filter switch
        {
            UgStatusFilterView.Active => character.IsActive,
            UgStatusFilterView.Vacant => character.IsActive
                && character.ApprovedClaimId is null
                && character.CharacterType != CharacterType.NonPlayer,
            UgStatusFilterView.Discussion => character.IsActive
                && character.ApprovedClaimId is null
                && character.HasActiveClaims,
            UgStatusFilterView.Archive => !character.IsActive || character.Claims.Any(IsDeclined),
            _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, null),
        };

    /// <summary>
    /// Какие заявки персонажа показывать: в архиве — все заявки удалённого персонажа и
    /// отклонённые/отозванные у живого, во всех остальных видах — активные.
    /// </summary>
    /// <remarks>
    /// Лист ожидания у живого персонажа — не архив, хотя заявка там и неактивна
    /// (<see cref="ClaimStatusExtensions.IsActive"/>). У удалённого персонажа активных заявок не бывает
    /// (удаление их не допускает), а оставшиеся — в том числе из листа ожидания — показываем все.
    /// </remarks>
    private static IReadOnlyCollection<CharacterClaimInfo> SelectClaims(CharacterInfo character, UgStatusFilterView filter)
        => [.. filter switch
        {
            UgStatusFilterView.Archive when !character.IsActive => character.Claims,
            UgStatusFilterView.Archive => character.Claims.Where(IsDeclined),
            _ => character.Claims.Where(claim => claim.IsActive),
        }];

    private static bool IsDeclined(CharacterClaimInfo claim)
        => claim.Status is ClaimStatus.DeclinedByMaster or ClaimStatus.DeclinedByUser;
}
