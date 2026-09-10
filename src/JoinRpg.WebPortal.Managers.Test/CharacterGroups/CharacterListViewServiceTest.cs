using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.Interfaces;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.WebPortal.Managers.CharacterGroupList;

namespace JoinRpg.WebPortal.Managers.Test.CharacterGroups;

/// <summary>
/// Списки <c>*ForMaster</c> считаются в два шага: грубый SQL-префильтр, поверх которого
/// накладываются доменные правила заявки. Здесь проверяется именно композиция.
/// </summary>
public class CharacterListViewServiceTest
{
    private MockedProject Mock { get; } = new MockedProject();

    private async Task<List<CharacterDto>> GetList(CharacterListType listType, ProjectInfo? projectInfo = null)
    {
        var service = new CharacterListViewService(
            new FakeCharacterRepository(Mock),
            new FakeProjectMetadataRepository(projectInfo ?? Mock.ProjectInfo),
            new FakeCurrentUserAccessor { UserIdentification = new UserIdentification(Mock.Master.UserId) });

        return await service.GetCharacters(Mock.ProjectInfo.ProjectId, listType);
    }

    private async Task<IReadOnlyCollection<string>> GetNames(CharacterListType listType, ProjectInfo? projectInfo = null)
        => [.. (await GetList(listType, projectInfo)).Select(x => x.Name)];

    [Fact]
    public async Task OrdinaryCharacterIsAvailable()
        => (await GetNames(CharacterListType.AvailableForMaster)).ShouldContain(Mock.Character.CharacterName);

    /// <summary>
    /// Главный регресс: SQL-префильтр про лимит слота не знает, и раньше исчерпанный слот
    /// доезжал до выпадашки — мастер выбирал его и получал SlotsExhausted только при отправке.
    /// </summary>
    [Fact]
    public async Task ExhaustedSlotIsNotAvailable()
    {
        var slot = CreateSlot("exhausted", slotLimit: 0);

        (await GetNames(CharacterListType.AvailableForMaster)).ShouldNotContain(slot.CharacterName);
    }

    [Fact]
    public async Task SlotWithFreePlacesIsAvailable()
    {
        var slot = CreateSlot("with places", slotLimit: 3);

        (await GetNames(CharacterListType.AvailableForMaster)).ShouldContain(slot.CharacterName);
    }

    [Fact]
    public async Task UnlimitedSlotIsAvailable()
    {
        var slot = CreateSlot("unlimited", slotLimit: null);

        (await GetNames(CharacterListType.AvailableForMaster)).ShouldContain(slot.CharacterName);
    }

    [Fact]
    public async Task BusyCharacterIsNotAvailable()
    {
        var busy = Mock.CreateCharacter("busy");
        _ = Mock.CreateApprovedClaim(busy, Mock.Player);

        (await GetNames(CharacterListType.AvailableForMaster)).ShouldNotContain(busy.CharacterName);
    }

    /// <summary>
    /// NPC отсекается доменными правилами, даже если легаси-колонка
    /// <c>Character.IsAcceptingClaims</c>, на которую смотрит SQL-префильтр, разъехалась с
    /// <c>CharacterType</c> у старых записей.
    /// </summary>
    [Fact]
    public async Task NpcIsNotAvailableEvenWithStaleLegacyFlag()
    {
        var npc = Mock.CreateCharacter("npc");
        npc.CharacterType = CharacterType.NonPlayer;
        npc.IsAcceptingClaims = true;

        (await GetNames(CharacterListType.AvailableForMaster)).ShouldNotContain(npc.CharacterName);
    }

    [Fact]
    public async Task InactiveCharacterIsNotAvailable()
    {
        var inactive = Mock.CreateCharacter("inactive");
        inactive.IsActive = false;

        (await GetNames(CharacterListType.AvailableForMaster)).ShouldNotContain(inactive.CharacterName);
    }

    /// <summary>
    /// Список считается от лица мастера: закрытый приём заявок его не опустошает. Если бы фильтр
    /// звался с DisplayForPlayer, фатальный ProjectClaimsClosed вычистил бы список целиком и
    /// мастер не смог бы никого пригласить.
    /// </summary>
    [Fact]
    public async Task ClaimsClosedDoesNotEmptyListForMaster()
    {
        Mock.Project.IsAcceptingClaims = false;
        Mock.ReInitProjectInfo();
        Mock.ProjectInfo.ProjectStatus.ShouldBe(ProjectLifecycleStatus.ActiveClaimsClosed);

        (await GetNames(CharacterListType.AvailableForMaster)).ShouldContain(Mock.Character.CharacterName);
    }

    // А вот архив не обходится никем.
    [Fact]
    public async Task ArchivedProjectHasNoAvailableCharacters()
    {
        Mock.Project.Active = false;
        Mock.Project.IsAcceptingClaims = false;
        Mock.ReInitProjectInfo();
        Mock.ProjectInfo.ProjectStatus.ShouldBe(ProjectLifecycleStatus.Archived);

        (await GetList(CharacterListType.AvailableForMaster)).ShouldBeEmpty();
    }

    [Fact]
    public async Task NonSlotsListHasNoSlots()
    {
        var slot = CreateSlot("with places", slotLimit: 3);

        var names = await GetNames(CharacterListType.AvailableNonSlotsForMaster);

        names.ShouldNotContain(slot.CharacterName);
        names.ShouldContain(Mock.Character.CharacterName);
    }

    [Fact]
    public async Task TemplatesListHasOnlySlots()
    {
        var slot = CreateSlot("with places", slotLimit: 3);

        var names = await GetNames(CharacterListType.AvailableTemplatesForMaster);

        names.ShouldBe([slot.CharacterName]);
    }

    [Fact]
    public async Task ExhaustedSlotIsNotInTemplatesList()
    {
        var slot = CreateSlot("exhausted", slotLimit: 0);

        (await GetNames(CharacterListType.AvailableTemplatesForMaster)).ShouldNotContain(slot.CharacterName);
    }

    // Списки без "ForMaster" доменными правилами не фильтруются — они не про доступность.
    [Fact]
    public async Task AllListIsNotFilteredByAvailability()
    {
        var busy = Mock.CreateCharacter("busy");
        _ = Mock.CreateApprovedClaim(busy, Mock.Player);

        (await GetNames(CharacterListType.All)).ShouldContain(busy.CharacterName);
    }

    private Character CreateSlot(string name, int? slotLimit)
    {
        var slot = Mock.CreateCharacter(name);
        slot.CharacterType = CharacterType.Slot;
        slot.CharacterSlotLimit = slotLimit;
        return slot;
    }

    /// <summary>
    /// Повторяет в памяти SQL-предикат <c>CharacterPredicates.IsAvailable</c> — тот самый грубый
    /// префильтр, поверх которого сервис накладывает доменные правила.
    /// </summary>
    private sealed class FakeCharacterRepository(MockedProject mock) : ICharacterRepository
    {
        private IEnumerable<Character> Available => mock.Project.Characters
            .Where(character => character.IsAcceptingClaims
                && character.IsActive
                && !mock.Project.Claims.Any(claim => claim.IsApproved && claim.CharacterId == character.CharacterId));

        public Task<IEnumerable<Character>> GetAvailableCharacters(ProjectIdentification projectId)
            => Task.FromResult(Available);

        public Task<IEnumerable<Character>> GetAvailableNonSlotCharacters(ProjectIdentification projectId)
            => Task.FromResult(Available.Where(c => c.CharacterType != CharacterType.Slot));

        public Task<IEnumerable<Character>> GetAvailableTemplateCharacters(ProjectIdentification projectId)
            => Task.FromResult(Available.Where(c => c.CharacterType == CharacterType.Slot));

        public Task<IEnumerable<Character>> GetAllCharacters(int projectId)
            => Task.FromResult<IEnumerable<Character>>(mock.Project.Characters);

        public Task<IEnumerable<Character>> GetActiveTemplateCharacters(int projectId)
            => Task.FromResult(mock.Project.Characters.Where(c => c.IsActive && c.CharacterType == CharacterType.Slot));

        public void Dispose() { }

        public Task<IReadOnlyCollection<CharacterHeader>> GetCharacterHeaders(int projectId, DateTime? modifiedSince) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<Character>> GetCharacters(IReadOnlyCollection<CharacterIdentification> characterIds) => throw new NotImplementedException();
        [Obsolete]
        public Task<Character> GetCharacterAsync(int projectId, int characterId) => throw new NotImplementedException();
        public Task<Character> GetCharacterAsync(CharacterIdentification characterId) => throw new NotImplementedException();
        public Task<Character> GetCharacterWithGroups(int projectId, int characterId) => throw new NotImplementedException();
        public Task<Character> GetCharacterWithDetails(int projectId, int characterId) => throw new NotImplementedException();
        public Task<CharacterView> GetCharacterViewAsync(int projectId, int characterId) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<Character>> LoadCharactersWithGroups(IReadOnlyCollection<CharacterIdentification> characterIds) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<Character>> LoadCharactersWithGroups(ProjectIdentification projectId) => throw new NotImplementedException();
    }

    private sealed class FakeProjectMetadataRepository(ProjectInfo projectInfo) : IProjectMetadataRepository
    {
        public Task<ProjectInfo> GetProjectMetadata(ProjectIdentification projectId, bool ignoreCache = false)
            => Task.FromResult(projectInfo);

        public Task<DomainTypes.ProjectMetadata.ProjectDetails> GetProjectDetails(ProjectIdentification projectId)
            => Task.FromResult(new DomainTypes.ProjectMetadata.ProjectDetails(new MarkdownString(""), [], false));

        public void PrimeCache(ProjectInfo projectInfo) { }
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
