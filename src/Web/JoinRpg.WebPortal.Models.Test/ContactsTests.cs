using JoinRpg.DataModel;
using JoinRpg.Web.Models.Helpers;

namespace JoinRpg.WebPortal.Models.Test;

public class ContactsTests
{
    [Fact]
    public void TestContact()
    {
        var user = new User { Email = "player@joinrpg.ru", PrefferedName = "Player", Extra = new UserExtra { Telegram = "player1", Vk = "player1" } };
        JoinrpgMarkdownLinkRenderer.GetPlayerContacts(user).ShouldBe(
            "Player (Email: <a href=\"mailto:player@joinrpg.ru\">player@joinrpg.ru</a>, ВК: <a href=\"https://vk.com/player1\">vk.com/player1</a>, "
            + "Телеграм: <a href=\"https://t.me/player1\">t.me/player1</a>)");
    }
}
