using System.Globalization;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Domain.Problems;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.UnifiedGrid;
using JoinRpg.DomainTypes.Users;
using JoinRpg.Interfaces;
using JoinRpg.Web.Claims.UnifiedGrid;

namespace JoinRpg.WebPortal.Managers.UnifiedGrid;

/// <summary>
/// Выгрузке грида заявок (ADR027 §5) нужен тот же конвейер, что и странице: <see cref="ClaimInfo"/>
/// всех заявок результата фильтра — из уже загруженных агрегатов, без второго похода в репозиторий.
/// Отдельная зависимость выгрузки, а не <c>IUnifiedGridClient</c>: у того есть реализация в WASM-клиенте,
/// которому экспорт не нужен.
/// </summary>
public interface IUnifiedGridExportSource
{
    Task<IReadOnlyList<ClaimInfo>> GetClaimInfosForExport(ProjectIdentification projectId, UgFilterView filter);
}

internal class UnifiedGridViewService(
    ICurrentUserAccessor currentUserAccessor,
    ICaptainRulesRepository captainRulesRepository,
    IProjectMetadataRepository projectMetadataRepository,
    ICharacterInfoRepository characterInfoRepository,
    IUserRepository userRepository,
    IClaimProblemValidator problemValidator,
    IClaimsRepository claimsRepository) : IUnifiedGridClient, IUnifiedGridExportSource
{
    /// <summary>Сколько персонажей на странице.</summary>
    internal const int PageSize = 100;

    async Task<UgPageViewModel> IUnifiedGridClient.GetGrid(
        ProjectIdentification projectId,
        UgScopeView scope,
        UgFilterView filter,
        int page)
    {
        var filtered = await FilterGrid(projectId, scope, filter);
        var rows = filtered.Rows;

        // Идентификаторы для выгрузки и массовых операций — весь результат фильтра, без пейджинга
        // и ещё до сортировки (ADR027 §5). У капитана операции недоступны — коллекции пустые.
        IReadOnlyCollection<ClaimIdentification> operationClaimIds = [];
        IReadOnlyCollection<CharacterIdentification> operationCharacterIds = [];
        if (scope == UgScopeView.Master)
        {
            operationClaimIds = [.. rows.SelectMany(row => row.Claims.Select(claim => claim.ClaimId))];
            operationCharacterIds = [.. rows.Where(row => row.Claims.Count > 0).Select(row => row.Character.Id)];
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
            // Без фильтра по проблемам они считаются только для страницы: сортировка от проблем не
            // зависит, а профили игроков и валидация — самое дорогое в запросе. С фильтром проблемы
            // всего результата уже посчитаны конвейером.
            IReadOnlyCollection<GridRow> pageCharacters = [.. pageRows.Select(x => x.Row)];
            Dictionary<ClaimIdentification, IReadOnlyCollection<ClaimProblem>>? problems = filtered.Problems ?? Validate(pageCharacters, await LoadPlayers(pageCharacters));
            // Бейдж непрочитанных — по заявкам страницы (ADR027 §4): дальше первой сотни читатель
            // всё равно не уходит, а считать непрочитанное всего проекта дорого.
            var unread = await claimsRepository.GetUnreadDiscussionsForClaims(
                [.. pageRows.SelectMany(x => x.Row.Claims.Select(claim => claim.ClaimId))],
                currentUserAccessor.UserIdentification,
                hasMasterAccess: true);
            items = [.. pageRows.Select(x => ItemBuilder.BuildItem(x.Row.Character, x.Row.Claims, currentUserAccessor, scope, problems, unread)!)];
        }
        else
        {
            items = [.. pageRows.Select(x => x.Item!)];
        }

        return new UgPageViewModel(items, actualPage, totalPages, sorted.Count, operationClaimIds, operationCharacterIds);
    }

    /// <summary>
    /// <see cref="ClaimInfo"/> всех заявок результата фильтра — выгрузка грида в Excel (ADR027 §5).
    /// Гонит тот же конвейер, что и страница, но без пейджинга: заявки собираются из уже загруженных
    /// агрегатов и профилей игроков, без второго похода в репозиторий. Порядок — как у
    /// <see cref="UgPageViewModel.OperationClaimIds"/>: персонажи скоупа, внутри каждого — его показанные
    /// заявки. Скоуп мастерский, права проверяются как в <c>GetGrid</c>: без них список пуст.
    /// </summary>
    async Task<IReadOnlyList<ClaimInfo>> IUnifiedGridExportSource.GetClaimInfosForExport(ProjectIdentification projectId, UgFilterView filter)
    {
        var filtered = await FilterGrid(projectId, UgScopeView.Master, filter);

        // Профили нужны каждому ClaimInfo. При фильтре по проблемам конвейер уже загрузил их для
        // всего результата, иначе грузим одним запросом здесь.
        var players = filtered.Players.Count > 0 ? filtered.Players : await LoadPlayers(filtered.Rows);

        return [.. filtered.Rows
            .SelectMany(row => row.Claims.Select(claim => new ClaimInfo(new ClaimInCharacter(row.Character, claim), players[claim.PlayerId])))];
    }

    /// <summary>Результат общих шагов конвейера грида (ревью #6025): загрузка и фильтрация — до пейджинга.</summary>
    private sealed record FilteredGrid(
        IReadOnlyList<GridRow> Rows,

        /// <summary>Проблемы всех заявок результата — посчитаны, если фильтру они были нужны; иначе <c>null</c>.</summary>
        Dictionary<ClaimIdentification, IReadOnlyCollection<ClaimProblem>>? Problems,

        /// <summary>Профили игроков результата — загружены, если их уже требовал фильтр по проблемам.</summary>
        IReadOnlyDictionary<UserIdentification, UserInfo> Players);

    /// <summary>
    /// Общие шаги конвейера (ADR027 §3, §5, ревью #6025): загрузка скоупа и фильтрация. Дальше ветки
    /// расходятся: страница режет результат по <see cref="PageSize"/> и строит viewModel, выгрузка
    /// берёт всё целиком. Отбор в памяти, поэтому скоуп загружен целиком, и страница режется уже от
    /// готового списка (ADR027 §3: пагинация сокращает ответ, а не нагрузку на БД).
    /// </summary>
    private async Task<FilteredGrid> FilterGrid(ProjectIdentification projectId, UgScopeView scope, UgFilterView filter)
    {
        var status = ToDomain(filter.Status);

        var characters = await LoadScope(projectId, scope, status.GetCharacterStatusSpec(), filter.Group);

        IReadOnlyList<GridRow> rows =
            [.. characters
                .Where(character => status.Matches(character))
                .Select(character => new GridRow(character, status.SelectClaims(character)))];

        if (scope == UgScopeView.Master && filter.Responsible is { } responsible)
        {
            rows = KeepClaims(rows, claim => claim.ResponsibleMasterId == responsible);
        }

        Dictionary<ClaimIdentification, IReadOnlyCollection<ClaimProblem>>? problems = null;
        var players = NoPlayers;
        if (scope == UgScopeView.Master && filter.OnlyWithProblems)
        {
            // Фильтру проблемы нужны по всему результату, а валидатору — профили игроков. Профили
            // выгрузке тоже пригодятся (ClaimInfo), поэтому запоминаем их для повторного использования.
            players = await LoadPlayers(rows);
            problems = Validate(rows, players);
            rows = KeepClaims(rows, claim => HasWarnings(problems[claim.ClaimId]));
        }

        return new FilteredGrid(rows, problems, players);
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

    private static readonly IReadOnlyDictionary<UserIdentification, UserInfo> NoPlayers = new Dictionary<UserIdentification, UserInfo>();

    /// <summary>
    /// Профили игроков показанных заявок одним запросом — они нужны и валидатору проблем, и
    /// <see cref="ClaimInfo"/> выгрузки.
    /// </summary>
    private async Task<IReadOnlyDictionary<UserIdentification, UserInfo>> LoadPlayers(IReadOnlyCollection<GridRow> rows)
    {
        IReadOnlyCollection<UserIdentification> playerIds = [.. rows.SelectMany(row => row.Claims.Select(claim => claim.PlayerId)).Distinct()];
        if (playerIds.Count == 0)
        {
            return NoPlayers;
        }

        return (await userRepository.GetRequiredUserInfos(playerIds))
            .ToDictionary(user => user.UserId);
    }

    /// <summary>
    /// Проблемы показанных заявок — один раз на заявку, с порогом <see cref="ProblemSeverity.Hint"/>, как
    /// в колонке старого грида. Профили игроков загружает <see cref="LoadPlayers"/> — валидатору нужен
    /// профиль (контакты, поля профиля).
    /// </summary>
    private Dictionary<ClaimIdentification, IReadOnlyCollection<ClaimProblem>> Validate(
        IReadOnlyCollection<GridRow> rows,
        IReadOnlyDictionary<UserIdentification, UserInfo> players)
    {
        var claims = rows.SelectMany(row => row.Claims.Select(claim => new ClaimInCharacter(row.Character, claim))).ToList();
        if (claims.Count == 0)
        {
            return [];
        }

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
            UgStatusFilterView.Approved => UnifiedGridStatus.Approved,
            UgStatusFilterView.OnHold => UnifiedGridStatus.OnHold,
            UgStatusFilterView.Archive => UnifiedGridStatus.Archive,
            UgStatusFilterView.All => UnifiedGridStatus.All,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
        };
}
