using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DataModel.Users;
using JoinRpg.Domain;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Users;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.WebPortal.Managers.Characters;
using CharacterInfo = JoinRpg.DomainTypes.Characters.CharacterInfo;

namespace JoinRpg.WebPortal.Managers.Test.Characters;

/// <summary>
/// <see cref="CharacterApiViewService"/> — общий слой над x-api и (в будущем) MCP-инструментами
/// (ADR012 §4). В отличие от x-api-контроллера, чей единственный барьер сегодня — атрибут
/// <c>[XGameMasterAuthorize]</c>, сервис обязан проверять права мастера сам: MCP через этот
/// атрибут не проходит вообще.
/// </summary>
public class CharacterApiViewServiceTests
{
    private MockedProject Mock { get; } = new MockedProject();

    private CharacterApiViewService CreateService(int userId) =>
        new(
            new FakeCharacterRepository(Mock),
            new FakeCharacterInfoRepository(),
            new FakeUserRepository(),
            new FakeCharacterService(),
            new FakeProjectMetadataRepository(Mock.ProjectInfo),
            new FakeCurrentUserAccessor { UserIdentification = new UserIdentification(userId) });

    [Fact]
    public async Task GetCharacterHeaders_Master_ReturnsHeaders()
    {
        var service = CreateService(Mock.Master.UserId);

        var headers = await service.GetCharacterHeaders(Mock.ProjectInfo.ProjectId, modifiedSince: null);

        headers.ShouldContain(h => h.CharacterId == Mock.Character.CharacterId);
    }

    [Fact]
    public async Task GetCharacterHeaders_NonMaster_ThrowsNoAccessToProjectException()
    {
        var service = CreateService(userId: 12345);

        await Should.ThrowAsync<NoAccessToProjectException>(
            () => service.GetCharacterHeaders(Mock.ProjectInfo.ProjectId, modifiedSince: null));
    }

    [Fact]
    public async Task GetCharacterInfo_NonMaster_ThrowsBeforeLoadingCharacter()
    {
        // FakeCharacterInfoRepository throws NotImplementedException if actually called —
        // seeing NoAccessToProjectException instead proves the rights check runs first.
        var service = CreateService(userId: 12345);
        var characterId = new CharacterIdentification(Mock.ProjectInfo.ProjectId, Mock.Character.CharacterId);

        await Should.ThrowAsync<NoAccessToProjectException>(() => service.GetCharacterInfo(characterId));
    }

    [Fact]
    public async Task GetCharactersByIds_NonMaster_ThrowsBeforeLoadingCharacters()
    {
        var service = CreateService(userId: 12345);

        await Should.ThrowAsync<NoAccessToProjectException>(
            () => service.GetCharactersByIds(Mock.ProjectInfo.ProjectId, [Mock.Character.CharacterId]));
    }

    [Fact]
    public async Task ListCharactersByGroup_NonMaster_ThrowsBeforeLoadingCharacters()
    {
        var service = CreateService(userId: 12345);
        var groupId = new CharacterGroupIdentification(Mock.ProjectInfo.ProjectId, Mock.Group.CharacterGroupId);

        await Should.ThrowAsync<NoAccessToProjectException>(() => service.ListCharactersByGroup(groupId));
    }

    private sealed class FakeCharacterRepository(MockedProject mock) : ICharacterRepository
    {
        public Task<IReadOnlyCollection<JoinRpg.Data.Interfaces.CharacterHeader>> GetCharacterHeaders(int projectId, DateTime? modifiedSince) =>
            Task.FromResult<IReadOnlyCollection<JoinRpg.Data.Interfaces.CharacterHeader>>(
                [.. mock.Project.Characters.Select(c => new JoinRpg.Data.Interfaces.CharacterHeader { CharacterId = c.CharacterId, UpdatedAt = DateTime.UtcNow, IsActive = c.IsActive })]);

        public Task<IReadOnlyCollection<Character>> GetCharacters(IReadOnlyCollection<CharacterIdentification> characterIds) => throw new NotImplementedException();
        [Obsolete]
        public Task<Character> GetCharacterAsync(int projectId, int characterId) => throw new NotImplementedException();
        public Task<Character> GetCharacterAsync(CharacterIdentification characterId) => throw new NotImplementedException();
        public Task<Character> GetCharacterWithGroups(int projectId, int characterId) => throw new NotImplementedException();
        public Task<Character> GetCharacterWithDetails(int projectId, int characterId) => throw new NotImplementedException();
        public Task<CharacterView> GetCharacterViewAsync(int projectId, int characterId) => throw new NotImplementedException();
        public Task<IEnumerable<Character>> GetAvailableCharacters(ProjectIdentification projectId) => throw new NotImplementedException();
        public Task<IEnumerable<Character>> GetAvailableNonSlotCharacters(ProjectIdentification projectId) => throw new NotImplementedException();
        public Task<IEnumerable<Character>> GetAvailableTemplateCharacters(ProjectIdentification projectId) => throw new NotImplementedException();
        public Task<IEnumerable<Character>> GetAllCharacters(int projectId) => throw new NotImplementedException();
        public Task<IEnumerable<Character>> GetActiveTemplateCharacters(int projectId) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<Character>> LoadCharactersWithGroups(IReadOnlyCollection<CharacterIdentification> characterIds) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<Character>> LoadCharactersWithGroups(ProjectIdentification projectId) => throw new NotImplementedException();
        public void Dispose() { }
    }

    private sealed class FakeCharacterInfoRepository : ICharacterInfoRepository
    {
        public Task<CharacterInfo?> GetCharacterInfoOrDefault(CharacterIdentification characterId) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<CharacterInfo>> GetCharacterInfos(IReadOnlyCollection<CharacterIdentification> characterIds) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<CharacterInfo>> GetCharacterInfosByGroups(ProjectIdentification projectId, IReadOnlyCollection<CharacterGroupIdentification> groupIds) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<CharacterInfo>> GetAllCharacterInfos(ProjectIdentification projectId) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<CharacterListEntry>> GetCharactersForList(ProjectIdentification projectId) => throw new NotImplementedException();
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public Task<User> GetById(int id) => throw new NotImplementedException();
        public Task<User> WithProfile(int userId) => throw new NotImplementedException();
        public Task<User> GetWithSubscribe(int currentUserId) => throw new NotImplementedException();
        public Task<UserAvatar> LoadAvatar(AvatarIdentification userAvatarId) => throw new NotImplementedException();
        public Task<UserInfo?> GetUserInfo(UserIdentification userId) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<UserInfo>> GetUserInfos(IReadOnlyCollection<UserIdentification> userIds) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<UserInfoHeader>> GetUserInfoHeaders(IReadOnlyCollection<UserIdentification> userIds) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<UserInfoHeader>> GetAdminUserInfoHeaders() => throw new NotImplementedException();
        public Task<UserIdentification?> FindByVk(string vkId) => throw new NotImplementedException();
        public Task<UserIdentification?> FindByTelegram(string telegramUsername) => throw new NotImplementedException();
        public Task<UserIdentification?> FindByEmail(string email) => throw new NotImplementedException();
    }

    private sealed class FakeCharacterService : ICharacterService
    {
        public Task<CharacterIdentification> AddCharacter(AddCharacterRequest addCharacterRequest) => throw new NotImplementedException();
        public Task DeleteCharacter(DeleteCharacterRequest deleteCharacterRequest) => throw new NotImplementedException();
        public Task EditCharacter(EditCharacterRequest editCharacterRequest) => throw new NotImplementedException();
        public Task SetFields(CharacterIdentification characterId, FieldLayerContainer fieldsToSet) => throw new NotImplementedException();
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
        public UserDisplayName DisplayName => new UserDisplayName("Test", null);
        public bool IsAdmin => false;
        public AvatarIdentification? Avatar => null;
    }
}
