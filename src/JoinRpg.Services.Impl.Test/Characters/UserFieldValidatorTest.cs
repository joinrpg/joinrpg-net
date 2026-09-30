using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DataModel.Users;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.DomainTypes.Users;

namespace JoinRpg.Services.Impl.Test.Characters;

/// <summary>
/// Проверка существования пользователя в полях-ссылках (ADR017 §7). До сохранения, потому что
/// <c>FieldSaveHelper</c> синхронный и репозиториев не видит.
/// </summary>
public class UserFieldValidatorTest : Claims.ClaimServiceTestBase
{
    private CharacterServiceImpl CreateService()
        => new(CreatePropsService(), CreateUserFieldValidator());

    /// <remarks>
    /// Именно <c>AddField</c>, а не <c>CreateField</c>: поле должно быть настоящей сущностью
    /// проекта, иначе оно исчезнет при пересборке метаданных внутри сохранения.
    /// </remarks>
    private ProjectFieldInfo CreateUserField(
        string name = "Ответственный мастер",
        ProjectFieldType fieldType = ProjectFieldType.UserLink)
        => mock.AddField(f =>
        {
            f.FieldName = name;
            f.FieldType = fieldType;
            f.FieldBoundTo = FieldBoundTo.Character;
            f.CanPlayerView = true;
            f.CanPlayerEdit = false;
            f.ValidForNpc = true;
        });

    private FieldLayerContainer Layer(ProjectFieldInfo field, string? value)
        => new(mock.ProjectInfo, new Dictionary<int, string?> { [field.Id.ProjectFieldId] = value });

    [Fact]
    public async Task UnknownUser_Throws_AndDoesNotSave()
    {
        var field = CreateUserField();

        var exception = await Should.ThrowAsync<FieldUserNotFoundException>(
            () => CreateService().SetFields(mock.Character.GetId(), Layer(field, "999999")));

        exception.UserId.ShouldBe(new UserIdentification(999999));
        exception.FieldId.ShouldBe(field.Id);
        SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>
    /// Не найден хотя бы один — не сохраняем ничего: частично записанное значение поля
    /// хуже отказа.
    /// </summary>
    [Fact]
    public async Task ExistingUserTogetherWithUnknown_Throws_AndDoesNotSave()
    {
        var field = CreateUserField();

        _ = await Should.ThrowAsync<FieldUserNotFoundException>(
            () => CreateService().SetFields(mock.Character.GetId(), Layer(field, $"{mock.Master.UserId},999999")));

        SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task ExistingUser_IsSaved()
    {
        var field = CreateUserField();

        await CreateService().SetFields(mock.Character.GetId(), Layer(field, mock.Master.UserId.ToString()));

        SaveChangesCallCount.ShouldBe(1);
        mock.GetCharacterInfo(mock.Character).CharacterFields
            .GetValue(field)
            .ShouldBe(mock.Master.UserId.ToString());
    }

    /// <summary>
    /// Мультивыбор (#4511): несколько существующих пользователей сохраняются целиком,
    /// в том порядке, в каком их указали.
    /// </summary>
    [Fact]
    public async Task MultiUserField_SeveralExistingUsers_AreSaved()
    {
        var field = CreateUserField("Кураторы роли", ProjectFieldType.MultiUserLink);

        await CreateService().SetFields(
            mock.Character.GetId(),
            Layer(field, $"{mock.Player.UserId},{mock.Master.UserId}"));

        SaveChangesCallCount.ShouldBe(1);
        mock.GetCharacterInfo(mock.Character).CharacterFields
            .GetValue(field)
            .ShouldBe($"{mock.Player.UserId},{mock.Master.UserId}");
    }

    /// <summary>Снятие ссылки — не ошибка.</summary>
    [Fact]
    public async Task EmptyValue_IsAllowed()
    {
        var field = CreateUserField();
        MockedProject.AssignFieldValues(
            mock.Character,
            new FieldWithValue(field, mock.Master.UserId.ToString()));

        await CreateService().SetFields(mock.Character.GetId(), Layer(field, ""));

        SaveChangesCallCount.ShouldBe(1);
        mock.GetCharacterInfo(mock.Character).CharacterFields.GetValue(field).ShouldBeNull();
    }

    /// <summary>
    /// Число в обычном поле — это число, а не пользователь: ходить за ним в базу незачем.
    /// </summary>
    [Fact]
    public async Task NonUserFieldWithNumericValue_DoesNotHitDatabase()
    {
        var field = mock.AddField(f =>
        {
            f.FieldName = "Возраст";
            f.FieldType = ProjectFieldType.Number;
        });
        var repository = new CountingUserRepository(mock);

        await new UserFieldValidator(repository).ValidateUserFields(Layer(field, "42"));

        repository.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task UserField_QueriesDatabaseOnceForAllValues()
    {
        var first = CreateUserField();
        var second = CreateUserField("Второй игрок");
        var repository = new CountingUserRepository(mock);

        await new UserFieldValidator(repository).ValidateUserFields(
            new FieldLayerContainer(mock.ProjectInfo, new Dictionary<int, string?>
            {
                [first.Id.ProjectFieldId] = mock.Master.UserId.ToString(),
                [second.Id.ProjectFieldId] = mock.Player.UserId.ToString(),
            }));

        repository.CallCount.ShouldBe(1);
    }

    private sealed class CountingUserRepository(MockedProject mock) : IUserRepository
    {
        private readonly FakeUserRepository inner = new(mock);

        public int CallCount { get; private set; }

        public Task<IReadOnlyCollection<UserInfoHeader>> GetUserInfoHeaders(IReadOnlyCollection<UserIdentification> userIds)
        {
            CallCount++;
            return inner.GetUserInfoHeaders(userIds);
        }

        public Task<UserInfo?> GetUserInfo(UserIdentification userId) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<UserInfo>> GetUserInfos(IReadOnlyCollection<UserIdentification> userIds) => throw new NotSupportedException();
        public Task<User> GetById(int id) => throw new NotSupportedException();
        public Task<User> WithProfile(int userId) => throw new NotSupportedException();
        public Task<User> GetWithSubscribe(int currentUserId) => throw new NotSupportedException();
        public Task<UserAvatar> LoadAvatar(AvatarIdentification userAvatarId) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<UserInfoHeader>> GetAdminUserInfoHeaders() => throw new NotSupportedException();
        public Task<UserIdentification?> FindByVk(string vkId) => throw new NotSupportedException();
        public Task<UserIdentification?> FindByTelegram(string telegramUsername) => throw new NotSupportedException();
        public Task<UserIdentification?> FindByEmail(string email) => throw new NotSupportedException();
    }
}
