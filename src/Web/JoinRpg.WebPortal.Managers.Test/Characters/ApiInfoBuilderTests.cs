using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.WebPortal.Managers.Characters;
using JoinRpg.WebPortal.Managers.Test.CharacterGroups;

namespace JoinRpg.WebPortal.Managers.Test.Characters;

public class ApiInfoBuilderTests
{
    private readonly MockedProject mock = new();

    /// <summary>
    /// Числовой Key привязанного telegram-логина — подтверждённый chat id, на него бот может писать напрямую.
    /// </summary>
    [Fact]
    public void ToPlayerContacts_TelegramLogin_FillsTelegramChatId()
    {
        mock.Player.ExternalLogins.Add(new UserExternalLogin
        {
            Provider = UserExternalLogin.TelegramProvider,
            Key = "12345",
        });

        var contacts = ApiInfoBuilder.ToPlayerContacts(mock.Player.GetUserInfo());

        contacts.TelegramChatId.ShouldBe(12345L);
    }

    /// <summary>
    /// PrettyName из профиля без привязанного логина — неверифицированный контакт: Telegram отдаём как раньше,
    /// но chat id не заполняем.
    /// </summary>
    [Fact]
    public void ToPlayerContacts_TelegramPrettyNameOnly_KeepsTelegramWithoutChatId()
    {
        mock.Player.Extra = new UserExtra { Telegram = "vasya_tg" };

        var contacts = ApiInfoBuilder.ToPlayerContacts(mock.Player.GetUserInfo());

        contacts.Telegram.ShouldBe("vasya_tg");
        contacts.TelegramChatId.ShouldBeNull();
    }

    [Fact]
    public void ToPlayerContacts_NoTelegram_ReturnsNullTelegramChatId()
    {
        var contacts = ApiInfoBuilder.ToPlayerContacts(mock.Player.GetUserInfo());

        contacts.Telegram.ShouldBeNull();
        contacts.TelegramChatId.ShouldBeNull();
    }
}
