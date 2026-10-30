using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Web.Claims.UnifiedGrid;
using JoinRpg.WebPortal.Managers.UnifiedGrid;

namespace JoinRpg.WebPortal.Managers.Test.UnifiedGrid;

/// <summary>
/// Объединённый грид (ADR027) работает поверх доменного агрегата (ADR013): отбор персонажей и заявок
/// под выбранный фильтр считается в памяти. Сами правила статусов — в <c>UnifiedGridStatusTest</c>,
/// здесь — скоуп и сборка строк.
/// </summary>
public class UnifiedGridViewServiceTest
{
    private MockedProject Mock { get; } = new MockedProject();

    /// <summary>Последний фейк репозитория — по нему видно, о чём сервис его попросил.</summary>
    private FakeCharacterInfoRepository characterInfoRepository = null!;

    private async Task<IReadOnlyCollection<UgItemViewModel>> GetItems(
        UgStatusFilterView filter,
        UgScopeView scope = UgScopeView.Captain,
        int? userId = null,
        FakeCaptainRulesRepository? captainRules = null)
        => (await GetPage(filter, scope, userId, captainRules)).Items;

    private async Task<UgPageViewModel> GetPage(
        UgStatusFilterView filter,
        UgScopeView scope = UgScopeView.Captain,
        int? userId = null,
        FakeCaptainRulesRepository? captainRules = null,
        int page = 1)
    {
        characterInfoRepository = new FakeCharacterInfoRepository(Mock);
        IUnifiedGridClient service = new UnifiedGridViewService(
            new FakeCurrentUserAccessor(userId ?? Mock.Master.UserId),
            captainRules ?? new FakeCaptainRulesRepository(Mock),
            new FakeProjectMetadataRepository(Mock),
            characterInfoRepository);

        return await service.GetGrid(Mock.ProjectInfo.ProjectId, scope, new UgFilterView(filter), page);
    }

    /// <summary>
    /// Архив — единственный вид, которому нужны удалённые персонажи (а заодно и живые — ради их
    /// отклонённых заявок); остальным удалённых не надо даже загружать.
    /// </summary>
    [Theory]
    [InlineData(UgScopeView.Captain, UgStatusFilterView.Active, CharacterStatusSpec.Active)]
    [InlineData(UgScopeView.Captain, UgStatusFilterView.Vacant, CharacterStatusSpec.Active)]
    [InlineData(UgScopeView.Captain, UgStatusFilterView.Discussion, CharacterStatusSpec.Active)]
    [InlineData(UgScopeView.Captain, UgStatusFilterView.Archive, CharacterStatusSpec.Any)]
    [InlineData(UgScopeView.Master, UgStatusFilterView.Active, CharacterStatusSpec.Active)]
    [InlineData(UgScopeView.Master, UgStatusFilterView.Vacant, CharacterStatusSpec.Active)]
    [InlineData(UgScopeView.Master, UgStatusFilterView.Discussion, CharacterStatusSpec.Active)]
    [InlineData(UgScopeView.Master, UgStatusFilterView.Archive, CharacterStatusSpec.Any)]
    public async Task RepositoryIsAskedForRightCharacters(UgScopeView scope, UgStatusFilterView filter, CharacterStatusSpec expected)
    {
        _ = await GetItems(filter, scope);

        characterInfoRepository.RequestedSpec.ShouldBe(expected);
    }

    private async Task<IReadOnlyCollection<string>> GetNames(UgStatusFilterView filter)
        => [.. (await GetItems(filter)).Select(item => item.Character.Name.Name)];

    [Fact]
    public async Task ActiveShowsActiveCharacter()
        => (await GetNames(UgStatusFilterView.Active)).ShouldContain(Mock.Character.CharacterName);

    [Fact]
    public async Task ActiveHidesDeletedCharacter()
    {
        var deleted = Mock.CreateCharacter("deleted");
        deleted.IsActive = false;

        (await GetNames(UgStatusFilterView.Active)).ShouldNotContain(deleted.CharacterName);
    }

    [Fact]
    public async Task VacantHidesNpcAndBusyCharacter()
    {
        var npc = Mock.CreateCharacter("npc");
        npc.CharacterType = CharacterType.NonPlayer;
        var busy = Mock.CreateCharacter("busy");
        _ = Mock.CreateApprovedClaim(busy, Mock.Player);

        var names = await GetNames(UgStatusFilterView.Vacant);

        names.ShouldNotContain(npc.CharacterName);
        names.ShouldNotContain(busy.CharacterName);
        names.ShouldContain(Mock.Character.CharacterName);
    }

    [Fact]
    public async Task DiscussionShowsOnlyCharactersWithActiveClaims()
    {
        var discussed = Mock.CreateCharacter("discussed");
        _ = Mock.CreateClaim(discussed, Mock.Player);

        var names = await GetNames(UgStatusFilterView.Discussion);

        names.ShouldBe([discussed.CharacterName]);
    }

    [Fact]
    public async Task DiscussionHidesCharacterWithApprovedClaim()
    {
        var busy = Mock.CreateCharacter("busy");
        _ = Mock.CreateApprovedClaim(busy, Mock.Player);

        (await GetNames(UgStatusFilterView.Discussion)).ShouldNotContain(busy.CharacterName);
    }

    [Fact]
    public async Task ArchiveShowsDeletedCharacterWithDeclinedClaim()
    {
        var deleted = Mock.CreateCharacter("deleted");
        deleted.IsActive = false;
        var claim = Mock.CreateClaim(deleted, Mock.Player);
        claim.ClaimStatus = ClaimStatus.DeclinedByMaster;

        (await GetNames(UgStatusFilterView.Archive)).ShouldBe([deleted.CharacterName]);
    }

    [Fact]
    public async Task ArchiveShowsDeletedCharacterWithoutClaims()
    {
        var deleted = Mock.CreateCharacter("deleted");
        deleted.IsActive = false;

        (await GetNames(UgStatusFilterView.Archive)).ShouldBe([deleted.CharacterName]);
    }

    /// <summary>
    /// Отклонённая заявка на живого персонажа — тоже архив: раньше архив грузил только удалённых
    /// персонажей, и такие заявки капитан не видел нигде.
    /// </summary>
    [Fact]
    public async Task ArchiveShowsDeclinedClaimOfActiveCharacter()
    {
        var character = Mock.CreateCharacter("active with declined");
        _ = Mock.CreateClaim(character, Mock.Player);
        var declined = Mock.CreateClaim(character, Mock.Master);
        declined.ClaimStatus = ClaimStatus.DeclinedByMaster;

        var item = (await GetItems(UgStatusFilterView.Archive)).ShouldHaveSingleItem();

        item.Character.Name.Name.ShouldBe(character.CharacterName);
        item.Claims.Select(c => c.ClaimId).ShouldBe([declined.GetId()]);
    }

    [Fact]
    public async Task ArchiveHidesActiveCharacterWithoutInactiveClaims()
    {
        var character = Mock.CreateCharacter("active");
        _ = Mock.CreateClaim(character, Mock.Player);

        (await GetNames(UgStatusFilterView.Archive)).ShouldBeEmpty();
    }

    /// <summary>
    /// Агрегат несёт все заявки персонажа, поэтому в грид попадают только отобранные фильтром:
    /// в обычных видах — активные, в архиве — отклонённые и отозванные (а у удалённого персонажа
    /// — все).
    /// </summary>
    [Fact]
    public async Task OnlyActiveClaimsAreShown()
    {
        var character = Mock.CreateCharacter("with claims");
        var active = Mock.CreateClaim(character, Mock.Player);
        var declined = Mock.CreateClaim(character, Mock.Master);
        declined.ClaimStatus = ClaimStatus.DeclinedByUser;

        var item = (await GetItems(UgStatusFilterView.Active))
            .Single(x => x.Character.Name.Name == character.CharacterName);

        item.Claims.Select(c => c.ClaimId).ShouldBe([active.GetId()]);
    }

    [Fact]
    public async Task ArchiveShowsAllClaimsOfDeletedCharacter()
    {
        var character = Mock.CreateCharacter("archived");
        character.IsActive = false;
        var onHold = Mock.CreateClaim(character, Mock.Player);
        onHold.ClaimStatus = ClaimStatus.OnHold;
        var declined = Mock.CreateClaim(character, Mock.Master);
        declined.ClaimStatus = ClaimStatus.DeclinedByUser;

        var item = (await GetItems(UgStatusFilterView.Archive))
            .Single(x => x.Character.Name.Name == character.CharacterName);

        item.Claims.Select(c => c.ClaimId).ShouldBe([onHold.GetId(), declined.GetId()], ignoreOrder: true);
    }

    /// <summary>
    /// Лист ожидания живого персонажа — не архив, хотя заявка там и неактивна.
    /// </summary>
    [Fact]
    public async Task ArchiveHidesOnHoldClaimOfActiveCharacter()
    {
        var character = Mock.CreateCharacter("active with waiting list");
        _ = Mock.CreateApprovedClaim(character, Mock.Player);
        var onHold = Mock.CreateClaim(character, Mock.Master);
        onHold.ClaimStatus = ClaimStatus.OnHold;

        (await GetNames(UgStatusFilterView.Archive)).ShouldBeEmpty();
    }

    [Fact]
    public async Task ClaimCarriesPlayerAndResponsibleMaster()
    {
        var character = Mock.CreateCharacter("with claim");
        _ = Mock.CreateClaim(character, Mock.Player);

        var claim = (await GetItems(UgStatusFilterView.Active))
            .Single(x => x.Character.Name.Name == character.CharacterName)
            .Claims
            .Single();

        claim.Player!.UserId.ShouldBe(new UserIdentification(Mock.Player.UserId));
        claim.Responsible!.UserId.ShouldBe(new UserIdentification(Mock.Master.UserId));
    }

    /// <summary>
    /// Доступность в кабинете капитана считают те же правила, что и в сетке ролей: у свободной
    /// роли кнопка «Заявиться» есть, а в проекте с закрытым приёмом заявок — нет (issue #4766).
    /// </summary>
    [Fact]
    public async Task FreeCharacterIsAvailableToApply()
    {
        var item = (await GetItems(UgStatusFilterView.Active))
            .Single(x => x.Character.Name.Name == Mock.Character.CharacterName);

        item.Character.ApplyStatus.IsAvailable.ShouldBeTrue();
    }

    [Fact]
    public async Task ClaimsClosedMakesCharacterUnavailableToApply()
    {
        Mock.Project.IsAcceptingClaims = false;
        Mock.ReInitProjectInfo();
        Mock.ProjectInfo.ProjectStatus.ShouldBe(ProjectLifecycleStatus.ActiveClaimsClosed);

        var item = (await GetItems(UgStatusFilterView.Active))
            .Single(x => x.Character.Name.Name == Mock.Character.CharacterName);

        item.Character.ApplyStatus.IsAvailable.ShouldBeFalse();
    }

    [Fact]
    public async Task NoCaptainRulesMeansEmptyGrid()
        => (await GetItems(
            UgStatusFilterView.Active,
            userId: Mock.Player.UserId,
            captainRules: new FakeCaptainRulesRepository(Mock, hasRules: false)))
            .ShouldBeEmpty();

    /// <summary>
    /// Капитан видит только поддеревья групп из своих правил.
    /// </summary>
    [Fact]
    public async Task CaptainSeesOnlyRuleGroups()
    {
        var inGroup = Mock.CreateCharacter("in group");
        inGroup.ParentCharacterGroupIds = [Mock.Group.CharacterGroupId];

        var names = (await GetItems(
            UgStatusFilterView.Active,
            captainRules: new FakeCaptainRulesRepository(Mock, groupId: Mock.Group.CharacterGroupId)))
            .Select(item => item.Character.Name.Name);

        names.ShouldBe([inGroup.CharacterName]);
    }

    /// <summary>
    /// Капитан, который не мастер, видит и непубличных персонажей своей команды: права ему даёт
    /// правило капитана, а не доступ мастера.
    /// </summary>
    [Fact]
    public async Task CaptainWhoIsNotMasterSeesHiddenCharacter()
    {
        var hidden = Mock.CreateCharacter("hidden");
        hidden.IsPublic = false;

        (await GetItems(UgStatusFilterView.Active, userId: Mock.Player.UserId))
            .Select(item => item.Character.Name.Name)
            .ShouldContain(hidden.CharacterName);
    }

    /// <summary>
    /// Мастеру правила капитана не нужны: он видит весь проект.
    /// </summary>
    [Fact]
    public async Task MasterSeesWholeProject()
        => (await GetItems(
            UgStatusFilterView.Active,
            UgScopeView.Master,
            captainRules: new FakeCaptainRulesRepository(Mock, hasRules: false)))
            .Select(item => item.Character.Name.Name)
            .ShouldContain(Mock.Character.CharacterName);

    /// <summary>
    /// Мастерский скоуп просит страница, но права проверяет сервер: капитан, не мастер, через него
    /// весь проект не увидит.
    /// </summary>
    [Fact]
    public async Task MasterScopeRequiresMasterAccess()
        => (await GetItems(UgStatusFilterView.Active, UgScopeView.Master, userId: Mock.Player.UserId))
            .ShouldBeEmpty();

    /// <summary>
    /// Свежие заявки — наверх, как в старом гриде мастера; персонажи без показанных заявок — в конце,
    /// по имени.
    /// </summary>
    [Fact]
    public async Task SortedByUpdateDateThenByName()
    {
        // Алфавит идёт против даты — иначе тест прошёл бы и с сортировкой по имени.
        var old = Mock.CreateCharacter("a old");
        Mock.CreateClaim(old, Mock.Player).CreateDate = At(1);
        var fresh = Mock.CreateCharacter("z fresh");
        Mock.CreateClaim(fresh, Mock.Player).CreateDate = At(2);
        var vacantB = Mock.CreateCharacter("b vacant");
        var vacantA = Mock.CreateCharacter("a vacant");

        var names = await GetNames(UgStatusFilterView.Active);

        names.ShouldBe([fresh.CharacterName, old.CharacterName, vacantA.CharacterName, vacantB.CharacterName, Mock.Character.CharacterName]);
    }

    /// <summary>
    /// Дата персонажа — самая свежая из его заявок: одна старая заявка его вниз не тянет.
    /// </summary>
    [Fact]
    public async Task CharacterDateIsItsFreshestClaim()
    {
        var neighbour = Mock.CreateCharacter("a neighbour");
        Mock.CreateClaim(neighbour, Mock.Player).CreateDate = At(2);
        var twoClaims = Mock.CreateCharacter("b two claims");
        Mock.CreateClaim(twoClaims, Mock.Player).CreateDate = At(1);
        Mock.CreateClaim(twoClaims, Mock.Master).CreateDate = At(3);

        (await GetNames(UgStatusFilterView.Discussion)).ShouldBe([twoClaims.CharacterName, neighbour.CharacterName]);
    }

    /// <summary>
    /// При одинаковых имени и дате порядок задаёт id — иначе страницы теряли бы и дублировали строки.
    /// </summary>
    [Fact]
    public async Task SameNamesOrderedById()
    {
        var first = Mock.CreateCharacter("Стражник");
        var second = Mock.CreateCharacter("Стражник");

        var ids = (await GetItems(UgStatusFilterView.Active))
            .Where(item => item.Character.Name.Name == "Стражник")
            .Select(item => item.Character.Name.CharacterId.CharacterId);

        ids.ShouldBe([first.CharacterId, second.CharacterId]);
    }

    private static DateTime At(int month) => new(2026, month, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Страница режется по персонажам, а номер за пределами диапазона прижимается к последней странице.
    /// </summary>
    [Fact]
    public async Task PagesAreCutByCharacters()
    {
        for (var i = 0; i < UnifiedGridViewService.PageSize; i++)
        {
            _ = Mock.CreateCharacter($"character {i:D3}");
        }
        var total = UnifiedGridViewService.PageSize + 1; // плюс Mock.Character

        var first = await GetPage(UgStatusFilterView.Active);
        var beyond = await GetPage(UgStatusFilterView.Active, page: 10);

        first.Items.Count.ShouldBe(UnifiedGridViewService.PageSize);
        first.TotalPages.ShouldBe(2);
        first.TotalCharacters.ShouldBe(total);
        beyond.Page.ShouldBe(2);
        beyond.Items.ShouldHaveSingleItem();
        first.Items.Concat(beyond.Items).Select(item => item.Character.Name.CharacterId).Distinct().Count().ShouldBe(total);
    }

    [Theory]
    [InlineData(-5)]
    [InlineData(0)]
    public async Task PageBeforeFirstIsFirst(int page)
        => (await GetPage(UgStatusFilterView.Active, page: page)).Page.ShouldBe(1);

    [Fact]
    public async Task EmptyGridIsOnePageWithoutItems()
    {
        var page = await GetPage(
            UgStatusFilterView.Active,
            captainRules: new FakeCaptainRulesRepository(Mock, hasRules: false),
            page: 3);

        page.Items.ShouldBeEmpty();
        page.Page.ShouldBe(1);
        page.TotalPages.ShouldBe(0);
        page.TotalCharacters.ShouldBe(0);
    }

    private sealed class FakeCaptainRulesRepository(MockedProject mock, bool hasRules = true, int? groupId = null) : ICaptainRulesRepository
    {
        public Task<IReadOnlyCollection<CaptainAccessRule>> GetCaptainRules(ProjectIdentification projectIdentification)
            => throw new NotImplementedException();

        public Task<IReadOnlyCollection<CaptainAccessRule>> GetCaptainRules(ProjectIdentification projectIdentification, UserIdentification userId)
            => Task.FromResult<IReadOnlyCollection<CaptainAccessRule>>(
                hasRules
                    ? [new CaptainAccessRule(
                        new CharacterGroupIdentification(projectIdentification, groupId ?? mock.Project.CharacterGroups.Single(g => g.IsRoot).CharacterGroupId),
                        userId,
                        CanApprove: true)]
                    : []);
    }
}
