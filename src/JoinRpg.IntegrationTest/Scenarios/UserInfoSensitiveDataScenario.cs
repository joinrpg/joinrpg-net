using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes.Users;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Паспорт и адрес регистрации должны доезжать из базы в <see cref="UserInfo"/>.
/// </summary>
/// <remarks>
/// Проекция <c>UserInfoRepository.GetUserInfosByPredicate</c> эти две колонки не выбирала,
/// хотя <see cref="UserInfo"/> их объявляет, а <see cref="UserInfo.GetMissingItems"/> по ним
/// считает незаполненность профиля. В итоге у любого репозиторного <see cref="UserInfo"/>
/// паспорт и адрес были всегда <c>null</c> и вечно считались незаполненными — скрытый баг,
/// который до сих пор не проявлялся только потому, что единственный потребитель
/// (<c>ClaimValidator.ValidateContacts</c>) зовёт калькулятор с
/// <c>sensitiveDataAccessAllowed: false</c> и эти два элемента отбрасывает.
/// Проверять это юнит-тестом нельзя — колонки теряются именно в LINQ-проекции, нужна БД.
/// </remarks>
public class UserInfoSensitiveDataScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    private const string PassportData = "4505 123456, выдан 01.01.2010";
    private const string RegistrationAddress = "г. Москва, ул. Тестовая, д. 1, кв. 2";

    [Fact]
    public async Task GetUserInfo_ReturnsPassportAndRegistrationAddress()
    {
        // 1. Пользователь и заполненный штатным путём профиль: UpdateProfile — тот самый метод,
        // которым паспорт сохраняет страница настроек профиля (ManageController).
        var userId = await CreateUserAsync();
        await factory.Services.RunAsAsync(
            userId,
            sp => FillProfileAsync(sp, userId, PassportData, RegistrationAddress));

        // 2. Оба способа загрузки UserInfo из репозитория должны привезти чувствительные данные.
        using var scope = factory.Services.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var userInfo = await userRepository.GetUserInfo(userId);
        userInfo.ShouldNotBeNull();
        userInfo.PassportData.ShouldBe(PassportData);
        userInfo.RegistrationAddress.ShouldBe(RegistrationAddress);

        var batch = await userRepository.GetUserInfos([userId]);
        var fromBatch = batch.ShouldHaveSingleItem();
        fromBatch.PassportData.ShouldBe(PassportData);
        fromBatch.RegistrationAddress.ShouldBe(RegistrationAddress);

        // 3. И GetMissingItems() больше не считает их незаполненными.
        var missingItems = userInfo.GetMissingItems();
        missingItems.ShouldNotContain(UserProfileItemType.Passport);
        missingItems.ShouldNotContain(UserProfileItemType.RegistrationAddress);
    }

    /// <summary>
    /// У только что зарегистрированного пользователя <c>UserExtra</c> вообще нет, и проекция
    /// обязана это переживать: опциональная навигация даёт LEFT JOIN, поля приезжают null.
    /// </summary>
    [Fact]
    public async Task GetUserInfo_WithoutUserExtra_ReturnsNullsAndCountsItemsAsMissing()
    {
        var userId = await CreateUserAsync();

        using var scope = factory.Services.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var userInfo = await userRepository.GetUserInfo(userId);
        userInfo.ShouldNotBeNull();
        userInfo.PassportData.ShouldBeNull();
        userInfo.RegistrationAddress.ShouldBeNull();

        var missingItems = userInfo.GetMissingItems();
        missingItems.ShouldContain(UserProfileItemType.Passport);
        missingItems.ShouldContain(UserProfileItemType.RegistrationAddress);
    }

    /// <summary>
    /// Пустая строка — это не заполненный паспорт: <c>GetMissingItems</c> считает заполненным
    /// значение не короче <see cref="UserProfileItemsCalculator.MinContactLength"/>. Тест сторожит,
    /// что исправленная проекция не начала путать «нет значения» с «значение есть».
    /// </summary>
    [Fact]
    public async Task GetUserInfo_WithEmptyPassport_CountsItemsAsMissing()
    {
        var userId = await CreateUserAsync();
        await factory.Services.RunAsAsync(
            userId,
            sp => FillProfileAsync(sp, userId, passportData: "", registrationAddress: ""));

        using var scope = factory.Services.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var userInfo = await userRepository.GetUserInfo(userId);
        userInfo.ShouldNotBeNull();

        var missingItems = userInfo.GetMissingItems();
        missingItems.ShouldContain(UserProfileItemType.Passport);
        missingItems.ShouldContain(UserProfileItemType.RegistrationAddress);
    }

    private async Task<UserIdentification> CreateUserAsync()
    {
        using var scope = factory.Services.CreateScope();
        return await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
    }

    private static Task FillProfileAsync(
        IServiceProvider sp,
        UserIdentification userId,
        string passportData,
        string registrationAddress)
        => sp.GetRequiredService<IUserService>().UpdateProfile(
            userId.Value,
            new UserFullName(
                new PrefferedName("Тестовый Игрок"),
                new BornName("Игроков Игрок Игрокович"),
                null,
                null),
            Gender.Unknown,
            phoneNumber: "+7 900 000-00-00",
            nicknames: "",
            groupNames: "",
            livejournal: "",
            ContactsAccessType.OnlyForMasters,
            passportData,
            registrationAddress,
            birthDate: new DateOnly(1985, 6, 12));
}
