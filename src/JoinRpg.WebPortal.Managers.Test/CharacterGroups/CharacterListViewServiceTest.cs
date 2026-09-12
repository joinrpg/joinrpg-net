using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Extensions;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Interfaces;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.WebPortal.Managers.CharacterGroupList;

namespace JoinRpg.WebPortal.Managers.Test.CharacterGroups;

/// <summary>
/// Списки <c>*ForMaster</c> фильтруются доменными правилами заявки — отдельного SQL-предиката
/// «доступен» больше нет, репозиторий отдаёт всех персонажей проекта лёгкой проекцией.
/// </summary>
public class CharacterListViewServiceTest
{
    private MockedProject Mock { get; } = new MockedProject();

    private async Task<List<CharacterDto>> GetList(CharacterListType listType, ProjectInfo? projectInfo = null)
    {
        var service = new CharacterListViewService(
            new FakeCharacterInfoRepository(Mock),
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
    /// Главный регресс: когда доступность считал SQL-предикат, он про лимит слота не знал, и
    /// исчерпанный слот доезжал до выпадашки — мастер выбирал его и получал SlotsExhausted
    /// только при отправке.
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
    /// NPC отсекается доменными правилами по <c>CharacterType</c>, независимо от легаси-колонки
    /// <c>Character.IsAcceptingClaims</c>, которая у старых записей могла с ним разъехаться.
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
    /// Отдаёт лёгкую проекцию по персонажам мока — ровно то, что делает настоящий репозиторий.
    /// Фильтрации по доступности тут нет: её считает сам сервис доменными правилами.
    /// </summary>
    private sealed class FakeCharacterInfoRepository(MockedProject mock) : ICharacterInfoRepository
    {
        public Task<IReadOnlyCollection<CharacterListEntry>> GetCharactersForList(ProjectIdentification projectId)
            => Task.FromResult<IReadOnlyCollection<CharacterListEntry>>(
                [.. mock.Project.Characters.Select(character => new CharacterListEntry(
                    character.GetId(),
                    character.CharacterName,
                    character.Description?.Contents ?? "",
                    character.IsPublic,
                    character.IsActive,
                    character.ToCharacterTypeInfo(),
                    character.GetApprovedClaimIdOrDefault(),
                    [.. character.Claims.Where(claim => claim.ClaimStatus.IsActive()).Select(claim => claim.GetPlayerId())]))]);

        public Task<CharacterInfo?> GetCharacterInfoOrDefault(CharacterIdentification characterId) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<CharacterInfo>> GetCharacterInfos(IReadOnlyCollection<CharacterIdentification> characterIds) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<CharacterInfo>> GetCharacterInfosByGroups(ProjectIdentification projectId, IReadOnlyCollection<CharacterGroupIdentification> groupIds) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<CharacterInfo>> GetAllCharacterInfos(ProjectIdentification projectId) => throw new NotImplementedException();
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
