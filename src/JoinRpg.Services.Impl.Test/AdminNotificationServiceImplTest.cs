using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.AdminTools;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Projects;
using JoinRpg.DataModel.Users;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.DomainTypes.Users;
using JoinRpg.Services.Impl.Test.Projects;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.Services.Impl.Test;

public class AdminNotificationServiceImplTest
{
    private static readonly UserInfoHeader admin = new(new UserIdentification(1), new UserDisplayName("Admin", null));
    private readonly FakeNotificationService fakeNotificationService = new();
    private readonly FakeUserRepository fakeUserRepository = new([admin]);
    private readonly FakeKogdaIgraRepository fakeKogdaIgraRepository = new();

    private AdminNotificationServiceImpl CreateService()
        => new(
            fakeNotificationService,
            new FakeCurrentUserAccessor(userId: 2, isAdmin: true),
            fakeUserRepository,
            fakeKogdaIgraRepository);

    private string QueuedText()
    {
        var notificationEvent = fakeNotificationService.Queued.Single();
        return $"{notificationEvent.Header}\n\n{notificationEvent.TemplateText.TemplateContents}";
    }

    [Fact]
    public async Task LinkedWithGame()
    {
        fakeKogdaIgraRepository.Games.Add(new KogdaIgraGameData(
            new KogdaIgraIdentification(42), "Пример игры на КогдаИгре",
            new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 5), "Регион", "МГ", SiteUri: null, IsActive: true));

        await CreateService().NotifyAboutNewProjectKogdaIgraStatus(
            new ProjectIdentification(1), new ProjectName("Тестовый проект"), KogdaIgraLinkChoiceDto.Linked,
            new KogdaIgraIdentification(42), message: null);

        await Verify(QueuedText());
    }

    [Fact]
    public async Task LinkedWithoutGame()
    {
        await CreateService().NotifyAboutNewProjectKogdaIgraStatus(
            new ProjectIdentification(1), new ProjectName("Тестовый проект"), KogdaIgraLinkChoiceDto.Linked,
            gameId: null, message: null);

        await Verify(QueuedText());
    }

    [Fact]
    public async Task NotOnKogdaIgra()
    {
        await CreateService().NotifyAboutNewProjectKogdaIgraStatus(
            new ProjectIdentification(1), new ProjectName("Тестовый проект"), KogdaIgraLinkChoiceDto.NotOnKogdaIgra,
            gameId: null, "Игра только для своих");

        await Verify(QueuedText());
    }
}

internal sealed class FakeUserRepository(IReadOnlyCollection<UserInfoHeader> admins) : IUserRepository
{
    public Task<IReadOnlyCollection<UserInfoHeader>> GetAdminUserInfoHeaders() => Task.FromResult(admins);

    public Task<User> GetById(int id) => throw new NotSupportedException();
    public Task<User> WithProfile(int userId) => throw new NotSupportedException();
    public Task<User> GetWithSubscribe(int currentUserId) => throw new NotSupportedException();
    public Task<UserAvatar> LoadAvatar(AvatarIdentification userAvatarId) => throw new NotSupportedException();
    public Task<UserInfo?> GetUserInfo(UserIdentification userId) => throw new NotSupportedException();
    public Task<IReadOnlyCollection<UserInfo>> GetUserInfos(IReadOnlyCollection<UserIdentification> userIds) => throw new NotSupportedException();
    public Task<IReadOnlyCollection<UserInfoHeader>> GetUserInfoHeaders(IReadOnlyCollection<UserIdentification> userIds) => throw new NotSupportedException();
    public Task<UserIdentification?> FindByVk(string vkId) => throw new NotSupportedException();
    public Task<UserIdentification?> FindByTelegram(string telegramUsername) => throw new NotSupportedException();
    public Task<UserIdentification?> FindByEmail(string email) => throw new NotSupportedException();
}

internal sealed class FakeKogdaIgraRepository : IKogdaIgraRepository
{
    public List<KogdaIgraGameData> Games { get; } = [];

    public Task<IReadOnlyCollection<KogdaIgraGameData>> GetDataByIds(IReadOnlyCollection<KogdaIgraIdentification> kogdaIgraIdentifications)
        => Task.FromResult<IReadOnlyCollection<KogdaIgraGameData>>(
            [.. Games.Where(g => kogdaIgraIdentifications.Contains(g.Id))]);

    public Task<KogdaIgraListItem[]> GetActive() => throw new NotSupportedException();
    public Task<KogdaIgraListItem[]> GetActiveFuture() => throw new NotSupportedException();
    public Task<ICollection<KogdaIgraGame>> GetByIds(IReadOnlyCollection<KogdaIgraIdentification> kogdaIgraIdentifications) => throw new NotSupportedException();
    public Task<KogdaIgraListItem[]> GetNotUpdated() => throw new NotSupportedException();
    public Task<int> GetNotUpdatedCount() => throw new NotSupportedException();
    public Task<KogdaIgraGame[]> GetNotUpdatedObjects() => throw new NotSupportedException();
}
