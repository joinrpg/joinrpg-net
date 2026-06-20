using JoinRpg.Common.PrimitiveTypes;

namespace JoinRpg.WebComponents.Test;

public class ContactComponentsTest
{
    [Fact]
    public void EmailLink_Null_RendersNothing()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<EmailLink>(p => p.Add(x => x.Contact, null));
        cut.Markup.Trim().ShouldBeEmpty();
    }

    [Fact]
    public void EmailLink_WithValue_RendersMailtoLink()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<EmailLink>(p => p.Add(x => x.Contact, new Email("test@example.com")));
        cut.Markup.ShouldContain("href=\"mailto:test@example.com\"");
        cut.Markup.ShouldContain("test@example.com");
        cut.Markup.ShouldContain("glyphicon-envelope");
        cut.Markup.ShouldContain("white-space: nowrap");
    }

    [Fact]
    public void PhoneLink_Null_RendersNothing()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<PhoneLink>(p => p.Add(x => x.Contact, null));
        cut.Markup.Trim().ShouldBeEmpty();
    }

    [Fact]
    public void PhoneLink_WithValue_RendersPhoneNumber()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<PhoneLink>(p => p.Add(x => x.Contact, new PhoneNumber("+79161234567")));
        cut.Markup.ShouldContain("+79161234567");
        cut.Markup.ShouldContain("glyphicon-phone");
        cut.Markup.ShouldContain("white-space: nowrap");
    }

    [Fact]
    public void TelegramLink_Null_RendersNothing()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<TelegramLink>(p => p.Add(x => x.Contact, null));
        cut.Markup.Trim().ShouldBeEmpty();
    }

    [Fact]
    public void TelegramLink_WithoutUserName_RendersNothing()
    {
        using var ctx = new BunitContext();
        var contact = new TelegramId(123456, null);
        var cut = ctx.Render<TelegramLink>(p => p.Add(x => x.Contact, contact));
        cut.Markup.Trim().ShouldBeEmpty();
    }

    [Fact]
    public void TelegramLink_WithUserName_RendersLink()
    {
        using var ctx = new BunitContext();
        var contact = new TelegramId(123456, new PrefferedName("joinrpg"));
        var cut = ctx.Render<TelegramLink>(p => p.Add(x => x.Contact, contact));
        cut.Markup.ShouldContain("https://t.me/joinrpg");
        cut.Markup.ShouldContain("glyphicon-send");
        cut.Markup.ShouldContain("white-space: nowrap");
    }

    [Fact]
    public void LiveJournalLink_Null_RendersNothing()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<LiveJournalLink>(p => p.Add(x => x.Contact, null));
        cut.Markup.Trim().ShouldBeEmpty();
    }

    [Fact]
    public void LiveJournalLink_WithValue_RendersLjLink()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<LiveJournalLink>(p => p.Add(x => x.Contact, new LiveJournalId("someuser")));
        cut.Markup.ShouldContain("https://someuser.lj.ru");
        cut.Markup.ShouldContain("ЖЖ:");
        cut.Markup.ShouldContain("white-space: nowrap");
    }

    [Fact]
    public void VkLink_Null_RendersNothing()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<VkLink>(p => p.Add(x => x.Contact, null));
        cut.Markup.Trim().ShouldBeEmpty();
    }

    [Fact]
    public void VkLink_WithValue_RendersVkLink()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<VkLink>(p => p.Add(x => x.Contact, new VkId("id123")));
        cut.Markup.ShouldContain("https://vk.com/id123");
        cut.Markup.ShouldContain("ВК:");
        cut.Markup.ShouldContain("white-space: nowrap");
    }
}
