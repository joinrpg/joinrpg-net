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
    {
        characterInfoRepository = new FakeCharacterInfoRepository(Mock);
        IUnifiedGridClient service = new UnifiedGridViewService(
            new FakeCurrentUserAccessor(userId ?? Mock.Master.UserId),
            captainRules ?? new FakeCaptainRulesRepository(Mock),
            new FakeProjectMetadataRepository(Mock),
            characterInfoRepository);

        return await service.GetGrid(Mock.ProjectInfo.ProjectId, scope, new UgFilterView(filter));
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
