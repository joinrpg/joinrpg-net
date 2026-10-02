using JoinRpg.DomainTypes.Users;

namespace JoinRpg.DomainTypes.Test;

/// <summary>
/// Тест-страж: текстовое представление <see cref="UserInfo"/> не должно содержать персональных
/// данных. У record'а <c>ToString()</c> генерируется автоматически и печатает все свойства, то
/// есть и паспорт с адресом регистрации — достаточно один раз подставить объект в сообщение лога
/// или исключения, чтобы они уехали в логи. Защищает это только ручной override, и если его
/// уберут (или добавят в тип новое чувствительное поле), упадёт этот тест.
/// </summary>
public class UserInfoToStringTest
{
    private const string Passport = "4509 123456 выдан ОВД Тестового района";
    private const string RegistrationAddress = "г. Тестовый, ул. Проверочная, д. 1, кв. 2";

    private static UserInfo BuildUserInfo()
        => new UserInfo(
            UserId: new UserIdentification(1),
            Social: new UserSocialNetworks(null, null, null, null, ContactsAccessType.OnlyForMasters),
            ActiveClaims: [],
            ActiveProjects: [],
            AllProjects: [],
            IsAdmin: false,
            SelectedAvatarId: null,
            Email: new Email("user@example.com"),
            EmailConfirmed: true,
            UserFullName: new UserFullName(null, BornName.FromOptional("Иван"), SurName.FromOptional("Иванов"), null),
            VerifiedProfileFlag: false,
            PhoneNumber: "+79991234567",
            HasPassword: false,
            PassportData: Passport,
            RegistrationAddress: RegistrationAddress);

    [Fact]
    public void ToString_DoesNotContainPassportData()
        => BuildUserInfo().ToString().ShouldNotContain(Passport);

    [Fact]
    public void ToString_DoesNotContainRegistrationAddress()
        => BuildUserInfo().ToString().ShouldNotContain(RegistrationAddress);

    [Fact]
    public void ToString_StillIdentifiesUser()
    {
        var text = BuildUserInfo().ToString();

        text.ShouldContain("1");
        text.ShouldContain("user@example.com");
    }
}
