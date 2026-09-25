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
        var allGroups = projectInfo.GetChildGroupIdsIncludingThis([.. access.Select(x => x.CharacterGroup)]);

        var characters = await characterInfoRepository.GetCharacterInfosByGroups(
            projectId, [.. allGroups], StatusSpecFor(filter));

        return
        [
            .. characters
                .Where(character => MatchesFilter(character, filter))
                .Select(character => ItemBuilder.BuildItemForCaptain(character, SelectClaims(character, filter), currentUserAccessor, projectInfo))
                .WhereNotNull()
        ];
    }

    /// <summary>
    /// Что просить у репозитория: архив — это удалённые персонажи, все остальные виды — живые.
    /// Отбор внутри этого набора всё равно делается в памяти (см. <see cref="MatchesFilter"/>),
    /// но тащить агрегат на заведомо ненужных персонажей незачем.
    /// </summary>
    private static CharacterStatusSpec StatusSpecFor(UgStatusFilterView filter)
        => filter == UgStatusFilterView.Archive ? CharacterStatusSpec.Deleted : CharacterStatusSpec.Active;

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
            UgStatusFilterView.Archive => !character.IsActive && character.Claims.Any(claim => !claim.IsActive),
            _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, null),
        };

    /// <summary>
    /// Какие заявки персонажа показывать: в архиве — неактивные, во всех остальных видах — активные.
    /// Зеркало бывшего <c>ClaimPredicates.ByUgStatus</c>.
    /// </summary>
    private static IReadOnlyCollection<CharacterClaimInfo> SelectClaims(CharacterInfo character, UgStatusFilterView filter)
        => [.. filter == UgStatusFilterView.Archive
            ? character.Claims.Where(claim => !claim.IsActive)
            : character.Claims.Where(claim => claim.IsActive)];
}
