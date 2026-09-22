using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Interfaces;
using JoinRpg.Web.Claims.UnifiedGrid;
using JoinRpg.WebPortal.Managers.UnifiedGrid;

namespace JoinRpg.WebPortal.Managers.Test.UnifiedGrid;

/// <summary>
/// Кабинет капитана работает поверх доменного агрегата (ADR013): отбор персонажей и заявок под
/// выбранный фильтр считается в памяти, а не SQL-предикатами (<c>ByUgStatus</c>), которых больше нет.
/// </summary>
public class UnifiedGridViewServiceTest
{
    private MockedProject Mock { get; } = new MockedProject();

    private async Task<IReadOnlyCollection<UgItemForCaptainViewModel>> GetItems(UgStatusFilterView filter)
    {
        IUnifiedGridClient service = new UnifiedGridViewService(
            new FakeCurrentUserAccessor { UserIdentification = new UserIdentification(Mock.Master.UserId) },
            new FakeCaptainRulesRepository(Mock),
            new FakeProjectMetadataRepository(Mock),
            new FakeCharacterInfoRepository(Mock));

        return await service.GetForCaptain(Mock.ProjectInfo.ProjectId, filter);
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

    /// <summary>
    /// Агрегат несёт все заявки персонажа, поэтому в грид попадают только отобранные фильтром:
    /// в обычных видах — активные, в архиве — отклонённые и отозванные.
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
    public async Task ArchiveShowsInactiveClaimsOnly()
    {
        var character = Mock.CreateCharacter("archived");
        character.IsActive = false;
        _ = Mock.CreateClaim(character, Mock.Player);
        var declined = Mock.CreateClaim(character, Mock.Master);
        declined.ClaimStatus = ClaimStatus.DeclinedByUser;

        var item = (await GetItems(UgStatusFilterView.Archive))
            .Single(x => x.Character.Name.Name == character.CharacterName);

        item.Claims.Select(c => c.ClaimId).ShouldBe([declined.GetId()]);
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
    {
        IUnifiedGridClient service = new UnifiedGridViewService(
            new FakeCurrentUserAccessor { UserIdentification = new UserIdentification(Mock.Player.UserId) },
            new FakeCaptainRulesRepository(Mock, hasRules: false),
            new FakeProjectMetadataRepository(Mock),
            new FakeCharacterInfoRepository(Mock));

        (await service.GetForCaptain(Mock.ProjectInfo.ProjectId, UgStatusFilterView.Active)).ShouldBeEmpty();
    }

    private sealed class FakeCaptainRulesRepository(MockedProject mock, bool hasRules = true) : ICaptainRulesRepository
    {
        public Task<IReadOnlyCollection<CaptainAccessRule>> GetCaptainRules(ProjectIdentification projectIdentification)
            => throw new NotImplementedException();

        public Task<IReadOnlyCollection<CaptainAccessRule>> GetCaptainRules(ProjectIdentification projectIdentification, UserIdentification userId)
            => Task.FromResult<IReadOnlyCollection<CaptainAccessRule>>(
                hasRules
                    ? [new CaptainAccessRule(
                        new CharacterGroupIdentification(projectIdentification, mock.Project.CharacterGroups.Single(g => g.IsRoot).CharacterGroupId),
                        userId,
                        CanApprove: true)]
                    : []);
    }

    /// <summary>
    /// Отдаёт агрегаты по всем персонажам мока: в моке они все лежат в корневой группе, а отбор
    /// по фильтру — задача самого сервиса.
    /// </summary>
    private sealed class FakeCharacterInfoRepository(MockedProject mock) : ICharacterInfoRepository
    {
        public Task<IReadOnlyCollection<CharacterInfo>> GetCharacterInfosByGroups(ProjectIdentification projectId, IReadOnlyCollection<CharacterGroupIdentification> groupIds)
            => Task.FromResult<IReadOnlyCollection<CharacterInfo>>([.. mock.Project.Characters.Select(mock.GetCharacterInfo)]);

        public Task<CharacterInfo?> GetCharacterInfoOrDefault(CharacterIdentification characterId) => throw new NotImplementedException();
        public Task<CharacterInfo?> GetCharacterInfoOrDefault(CharacterIdentification characterId, ProjectInfo projectInfo) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<CharacterInfo>> GetCharacterInfos(IReadOnlyCollection<CharacterIdentification> characterIds) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<CharacterInfo>> GetAllCharacterInfos(ProjectIdentification projectId) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<CharacterListEntry>> GetCharactersForList(ProjectIdentification projectId) => throw new NotImplementedException();
    }

    private sealed class FakeCurrentUserAccessor : ICurrentUserAccessor
    {
        public UserIdentification UserIdentification { get; set; } = new UserIdentification(0);
        public int? UserIdOrDefault => UserIdentification.Value;
        public UserDisplayName DisplayName => new UserDisplayName("Test Master", null);
        public bool IsAdmin => false;
        public AvatarIdentification? Avatar => null;
    }
}
