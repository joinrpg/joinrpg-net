using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes.Users;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Телефоны пачкой — <c>IUserRepository.GetPhoneNumbers</c>.
/// </summary>
/// <remarks>
/// Нужна БД, а не мок: вся суть метода в LINQ-проекции. Телефон лежит в отдельной таблице
/// <c>UserExtra</c> и у только что зарегистрированного пользователя пуст — проверяется, что такой
/// пользователь не попадает в словарь и при этом не мешает остальным.
/// </remarks>
public class UserPhoneNumbersScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    private const string Phone = "+7 900 000-00-01";

    [Fact]
    public async Task GetPhoneNumbers_SkipsUsersWithoutPhoneAndReturnsTheRest()
    {
        var withPhone = await CreateUserAsync();
        await FillPhoneAsync(withPhone, Phone);

        // Профиль не заполнялся ни разу: ряд UserExtra регистрация создаёт, но телефон в нём null.
        var withoutPhone = await CreateUserAsync();

        var phones = await GetPhoneNumbersAsync([withPhone, withoutPhone]);

        phones.Keys.ShouldBe([withPhone]);
        phones[withPhone].ShouldBe(new PhoneNumber(Phone));
    }

    [Fact]
    public async Task GetPhoneNumbers_EmptyPhone_IsNotReturned()
    {
        var userId = await CreateUserAsync();
        await FillPhoneAsync(userId, phoneNumber: "");

        var phones = await GetPhoneNumbersAsync([userId]);

        phones.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPhoneNumbers_EmptyRequest_ReturnsEmpty()
    {
        var phones = await GetPhoneNumbersAsync([]);

        phones.ShouldBeEmpty();
    }

    private async Task<IReadOnlyDictionary<UserIdentification, PhoneNumber>> GetPhoneNumbersAsync(
        IReadOnlyCollection<UserIdentification> userIds)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IUserRepository>().GetPhoneNumbers(userIds);
    }

    private async Task<UserIdentification> CreateUserAsync()
    {
        using var scope = factory.Services.CreateScope();
        return await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
    }

    private Task FillPhoneAsync(UserIdentification userId, string phoneNumber)
        => factory.Services.RunAsAsync(
            userId,
            // UpdateProfile — тот самый путь, которым телефон сохраняет страница профиля.
            sp => sp.GetRequiredService<IUserService>().UpdateProfile(
                userId.Value,
                new UserFullName(new PrefferedName("Тестовый Игрок"), null, null, null),
                Gender.Unknown,
                phoneNumber,
                nicknames: "",
                groupNames: "",
                livejournal: "",
                ContactsAccessType.OnlyForMasters,
                passportData: "",
                registrationAddress: "",
                birthDate: new DateOnly(1985, 6, 12)));
}
