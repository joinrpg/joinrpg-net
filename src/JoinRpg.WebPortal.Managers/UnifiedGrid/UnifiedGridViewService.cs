using System.Globalization;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.Domain.Problems;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.UnifiedGrid;
using JoinRpg.Interfaces;
using JoinRpg.Web.Claims.UnifiedGrid;

namespace JoinRpg.WebPortal.Managers.UnifiedGrid;

internal class UnifiedGridViewService(
    ICurrentUserAccessor currentUserAccessor,
    ICaptainRulesRepository captainRulesRepository,
    IProjectMetadataRepository projectMetadataRepository,
    ICharacterInfoRepository characterInfoRepository,
    IUserRepository userRepository,
    IClaimProblemValidator problemValidator) : IUnifiedGridClient
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

        var characters = await LoadScope(projectId, scope, status.GetCharacterStatusSpec(), filter.Group);

        // Отбор в памяти, поэтому скоуп загружен целиком, и страница режется уже от готового списка
        // (ADR027 §3: пагинация сокращает ответ, а не нагрузку на БД).
        IReadOnlyList<GridRow> rows =
            [.. characters
                .Where(character => status.Matches(character))
                .Select(character => new GridRow(character, status.SelectClaims(character)))];

        if (scope == UgScopeView.Master && filter.Responsible is { } responsible)
        {
            rows = KeepClaims(rows, claim => claim.ResponsibleMasterId == responsible);
        }

        // Проблемы скоупа целиком нужны только фильтру. Без него их считаем для одной страницы:
        // сортировка от проблем не зависит, а профили игроков и валидация — самое дорогое в запросе.
        IReadOnlyDictionary<ClaimIdentification, IReadOnlyCollection<ClaimProblem>>? problems = null;
        if (scope == UgScopeView.Master && filter.OnlyWithProblems)
        {
            problems = await Validate(rows);
            rows = KeepClaims(rows, claim => HasWarnings(problems[claim.ClaimId]));
        }

        var sorted = Sort(
            rows
                .Select(row => (Row: row, Item: ItemBuilder.BuildItem(row.Character, row.Claims, currentUserAccessor, scope)))
                .Where(x => x.Item is not null),
            x => x.Item!);

        var totalPages = (sorted.Count + PageSize - 1) / PageSize;
        var actualPage = Math.Clamp(page, 1, Math.Max(totalPages, 1));
        var pageRows = sorted.Skip((actualPage - 1) * PageSize).Take(PageSize).ToList();

        IReadOnlyList<UgItemViewModel> items;
        if (scope == UgScopeView.Master)
        {
            problems ??= await Validate([.. pageRows.Select(x => x.Row)]);
            items = [.. pageRows.Select(x => ItemBuilder.BuildItem(x.Row.Character, x.Row.Claims, currentUserAccessor, scope, problems)!)];
        }
        else
        {
            items = [.. pageRows.Select(x => x.Item!)];
        }

        return new UgPageViewModel(items, actualPage, totalPages, sorted.Count);
    }

    // Явно, а не по текущей культуре: порядок страниц не должен зависеть от настроек потока.
    private static readonly StringComparer NameComparer = StringComparer.Create(CultureInfo.GetCultureInfo("ru-RU"), ignoreCase: true);

    /// <summary>
    /// Сначала персонажи с самыми свежими заявками (по дате обновления, как в старом гриде мастера),
    /// потом без показанных заявок — по имени. Последний ключ — id, чтобы порядок был однозначным и
    /// страницы не теряли и не дублировали строки.
    /// </summary>
    private static IReadOnlyList<T> Sort<T>(IEnumerable<T> rows, Func<T, UgItemViewModel> item)
        => [.. rows
            .OrderByDescending(row => item(row).Claims.Max(claim => claim.UpdateDate))
            .ThenBy(row => item(row).Character.Name.Name, NameComparer)
            .ThenBy(row => item(row).Character.Name.CharacterId.CharacterId)];

    /// <summary>
    /// Персонажи скоупа: мастеру — весь проект (или выбранная группа с подгруппами), капитану —
    /// поддеревья групп из его правил. Без прав на запрошенный скоуп грид пуст.
    /// </summary>
    private async Task<IReadOnlyCollection<CharacterInfo>> LoadScope(
        ProjectIdentification projectId,
        UgScopeView scope,
        CharacterStatusSpec spec,
        CharacterGroupIdentification? group)
    {
        switch (scope)
        {
            case UgScopeView.Master:
                var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
                if (!projectInfo.HasMasterAccess(currentUserAccessor.UserIdentificationOrDefault))
                {
                    return [];
                }
                if (group is null)
                {
                    return await characterInfoRepository.GetAllCharacterInfos(projectId, spec);
                }
                // Группы нет в проекте — пустой грид, а не чужие персонажи. Удалённая группа остаётся в
                // дереве и показывает своих бывших персонажей, как старый список заявок группы.
                if (group.ProjectId != projectId || projectInfo.GroupTree.GetGroupByIdOrDefault(group) is null)
                {
                    return [];
                }
                return await characterInfoRepository.GetCharacterInfosByGroups(
                    projectId, [.. projectInfo.GroupTree.GetChildGroupIdsIncludingThis(group)], spec);

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

    /// <summary>Персонаж грида и его показанные заявки.</summary>
    private sealed record GridRow(CharacterInfo Character, IReadOnlyCollection<CharacterClaimInfo> Claims);

    /// <summary>
    /// Отбор по заявкам: остаются подходящие заявки, а персонажи без них (в том числе вакансии)
    /// пропадают.
    /// </summary>
    private static IReadOnlyList<GridRow> KeepClaims(IEnumerable<GridRow> rows, Func<CharacterClaimInfo, bool> predicate)
        => [.. rows
            .Select(row => row with { Claims = [.. row.Claims.Where(predicate)] })
            .Where(row => row.Claims.Count > 0)];

    /// <summary>
    /// Проблемы показанных заявок — один раз на заявку, с порогом <see cref="ProblemSeverity.Hint"/>, как
    /// в колонке старого грида. Валидатору нужен профиль игрока (контакты, поля профиля), поэтому
    /// профили всех игроков грузятся одним запросом.
    /// </summary>
    private async Task<IReadOnlyDictionary<ClaimIdentification, IReadOnlyCollection<ClaimProblem>>> Validate(
        IReadOnlyCollection<GridRow> rows)
    {
        var claims = rows.SelectMany(row => row.Claims.Select(claim => new ClaimInCharacter(row.Character, claim))).ToList();
        if (claims.Count == 0)
        {
            return new Dictionary<ClaimIdentification, IReadOnlyCollection<ClaimProblem>>();
        }

        var players = (await userRepository.GetRequiredUserInfos([.. claims.Select(c => c.Claim.PlayerId).Distinct()]))
            .ToDictionary(user => user.UserId);

        return claims.ToDictionary(
            c => c.Claim.ClaimId,
            c => (IReadOnlyCollection<ClaimProblem>)[.. problemValidator.Validate(new ClaimInfo(c, players[c.Claim.PlayerId]))]);
    }

    /// <summary>Порог фильтра «есть проблемы» — как у старой страницы «Проблемные».</summary>
    private static bool HasWarnings(IReadOnlyCollection<ClaimProblem> problems)
        => problems.Any(problem => problem.Severity >= ProblemSeverity.Warning);

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
