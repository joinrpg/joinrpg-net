using System.Globalization;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.UnifiedGrid;
using JoinRpg.Interfaces;
using JoinRpg.Web.Claims.UnifiedGrid;

namespace JoinRpg.WebPortal.Managers.UnifiedGrid;

internal class UnifiedGridViewService(
    ICurrentUserAccessor currentUserAccessor,
    ICaptainRulesRepository captainRulesRepository,
    IProjectMetadataRepository projectMetadataRepository,
    ICharacterInfoRepository characterInfoRepository) : IUnifiedGridClient
{
    /// <summary>Сколько персонажей на странице.</summary>
    internal const int PageSize = 100;

    async Task<UgPageViewModel> IUnifiedGridClient.GetGrid(
        ProjectIdentification projectId,
        UgScopeView scope,
        UgFilterView filter,
        int page)
    {
        var status = ToDomain(filter.Status);

        var characters = await LoadScope(projectId, scope, status.GetCharacterStatusSpec());

        // Отбор в памяти, поэтому скоуп загружен целиком, и страница режется уже от готового списка
        // (ADR027 §3: пагинация сокращает ответ, а не нагрузку на БД).
        var items = Sort(
            characters
                .Where(character => status.Matches(character))
                .Select(character => ItemBuilder.BuildItem(character, status.SelectClaims(character), currentUserAccessor, scope))
                .WhereNotNull());

        var totalPages = (items.Count + PageSize - 1) / PageSize;
        var actualPage = Math.Clamp(page, 1, Math.Max(totalPages, 1));

        return new UgPageViewModel(
            [.. items.Skip((actualPage - 1) * PageSize).Take(PageSize)],
            actualPage,
            totalPages,
            items.Count);
    }

    /// <summary>
    /// Сначала персонажи с самыми свежими заявками (по дате обновления, как в старом гриде мастера),
    /// потом без показанных заявок — по имени. Последний ключ — id, чтобы порядок был однозначным и
    /// страницы не теряли и не дублировали строки.
    /// </summary>
    // Явно, а не по текущей культуре: порядок страниц не должен зависеть от настроек потока.
    private static readonly StringComparer NameComparer = StringComparer.Create(CultureInfo.GetCultureInfo("ru-RU"), ignoreCase: true);

    private static IReadOnlyList<UgItemViewModel> Sort(IEnumerable<UgItemViewModel> items)
        => [.. items
            .OrderByDescending(item => item.Claims.Max(claim => claim.UpdateDate))
            .ThenBy(item => item.Character.Name.Name, NameComparer)
            .ThenBy(item => item.Character.Name.CharacterId.CharacterId)];

    /// <summary>
    /// Персонажи скоупа: мастеру — весь проект, капитану — поддеревья групп из его правил.
    /// Без прав на запрошенный скоуп грид пуст.
    /// </summary>
    private async Task<IReadOnlyCollection<CharacterInfo>> LoadScope(
        ProjectIdentification projectId,
        UgScopeView scope,
        CharacterStatusSpec spec)
    {
        switch (scope)
        {
            case UgScopeView.Master:
                var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
                if (!projectInfo.HasMasterAccess(currentUserAccessor.UserIdentificationOrDefault))
                {
                    return [];
                }
                return await characterInfoRepository.GetAllCharacterInfos(projectId, spec);

            case UgScopeView.Captain:
                var access = await captainRulesRepository.GetCaptainRules(projectId, currentUserAccessor.UserIdentification);
                if (access.Count == 0)
                {
                    return [];
                }
                var groups = (await projectMetadataRepository.GetProjectMetadata(projectId))
                    .GroupTree.GetChildGroupIdsIncludingThis([.. access.Select(x => x.CharacterGroup)]);
                return await characterInfoRepository.GetCharacterInfosByGroups(projectId, [.. groups], spec);

            default:
                throw new ArgumentOutOfRangeException(nameof(scope), scope, null);
        }
    }

    private static UnifiedGridStatus ToDomain(UgStatusFilterView status)
        => status switch
        {
            UgStatusFilterView.Active => UnifiedGridStatus.Active,
            UgStatusFilterView.Vacant => UnifiedGridStatus.Vacant,
            UgStatusFilterView.Discussion => UnifiedGridStatus.Discussion,
            UgStatusFilterView.Archive => UnifiedGridStatus.Archive,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
        };
}
