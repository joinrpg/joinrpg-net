using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Users;

namespace JoinRpg.Services.Impl.Test;

public class UserProfileMergerTest
{
    [Fact]
    public void PrimaryValuesWin()
    {
        var primary = CreateUser(1, "Иван", phone: "+7 111", telegram: "ivan");
        var secondary = CreateUser(2, "Пётр", phone: "+7 222", telegram: "petr");

        UserProfileMerger.MergeInto(primary, secondary);

        primary.BornName.ShouldBe("Иван");
        primary.Extra.PhoneNumber.ShouldBe("+7 111");
        primary.Extra.Telegram.ShouldBe("ivan");
    }

    [Fact]
    public void EmptyPrimaryValues_FilledFromSecondary()
    {
        var primary = CreateUser(1, bornName: " ", phone: "", telegram: null);
        var secondary = CreateUser(2, "Пётр", phone: "+7 222", telegram: "petr");
        secondary.SurName = "Петров";
        secondary.Extra.Gender = Gender.Male;
        secondary.Extra.BirthDate = new DateTime(1990, 1, 1);

        var taken = UserProfileMerger.MergeInto(primary, secondary);

        taken.ShouldBe(["Имя", "Фамилия", "Пол", "Телефон", "Telegram", "Дата рождения"], ignoreOrder: true);
        primary.BornName.ShouldBe("Пётр");
        primary.SurName.ShouldBe("Петров");
        primary.Extra.PhoneNumber.ShouldBe("+7 222");
        primary.Extra.Telegram.ShouldBe("petr");
        primary.Extra.Gender.ShouldBe(Gender.Male);
        primary.Extra.BirthDate.ShouldBe(new DateTime(1990, 1, 1));
    }

    [Fact]
    public void VkVerified_TakenOnlyTogetherWithVk()
    {
        var primary = CreateUser(1);
        primary.Extra.Vk = "id1";
        primary.Extra.VkVerified = false;
        var secondary = CreateUser(2);
        secondary.Extra.Vk = "id2";
        secondary.Extra.VkVerified = true;

        UserProfileMerger.MergeInto(primary, secondary);

        primary.Extra.Vk.ShouldBe("id1");
        primary.Extra.VkVerified.ShouldBeFalse();
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)] // у основного свой ВК-логин — ВК поглощаемого через вход уже не подтвердить
    public void VkFromSecondary_VerifiedOnlyWithoutPrimaryVkLogin(bool primaryHasVkLogin, bool expectedVerified)
    {
        var primary = CreateUser(1);
        if (primaryHasVkLogin)
        {
            primary.ExternalLogins.Add(new UserExternalLogin { UserId = 1, Provider = UserExternalLogin.VkProvider, Key = "1" });
        }
        var secondary = CreateUser(2);
        secondary.Extra.Vk = "id2";
        secondary.Extra.VkVerified = true;

        UserProfileMerger.MergeInto(primary, secondary);

        primary.Extra.Vk.ShouldBe("id2");
        primary.Extra.VkVerified.ShouldBe(expectedVerified);
    }

    [Fact]
    public void MissingPrimaryExtra_CreatedFromSecondary()
    {
        var primary = CreateUser(1);
        primary.Extra = null!;
        var secondary = CreateUser(2, phone: "+7 222");

        UserProfileMerger.MergeInto(primary, secondary);

        primary.Extra.UserId.ShouldBe(1);
        primary.Extra.PhoneNumber.ShouldBe("+7 222");
    }

    [Fact]
    public void ContactsFromSecondary_KeepSecondaryPrivacy()
    {
        var primary = CreateUser(1);
        primary.Extra.SocialNetworksAccess = ContactsAccessType.Public;
        var secondary = CreateUser(2, telegram: "hidden");
        secondary.Extra.SocialNetworksAccess = ContactsAccessType.OnlyForMasters;

        UserProfileMerger.MergeInto(primary, secondary);

        primary.Extra.Telegram.ShouldBe("hidden");
        primary.Extra.SocialNetworksAccess.ShouldBe(ContactsAccessType.OnlyForMasters);
    }

    [Fact]
    public void NoContactsFromSecondary_PrimaryPrivacyKept()
    {
        var primary = CreateUser(1, telegram: "own");
        primary.Extra.SocialNetworksAccess = ContactsAccessType.Public;
        var secondary = CreateUser(2, telegram: "hidden");
        secondary.Extra.SocialNetworksAccess = ContactsAccessType.OnlyForMasters;

        UserProfileMerger.MergeInto(primary, secondary);

        primary.Extra.SocialNetworksAccess.ShouldBe(ContactsAccessType.Public);
    }

    [Fact]
    public void MissingPrimaryExtra_TakesSecondarySettings()
    {
        var primary = CreateUser(1);
        primary.Extra = null!;
        var secondary = CreateUser(2);
        secondary.Extra.SocialNetworksAccess = ContactsAccessType.Public;
        secondary.Extra.EnableTelegramPlayerDigestNotification = false;

        UserProfileMerger.MergeInto(primary, secondary);

        primary.Extra.SocialNetworksAccess.ShouldBe(ContactsAccessType.Public);
        primary.Extra.EnableTelegramPlayerDigestNotification.ShouldBeFalse();
    }

    [Fact]
    public void AuthDates_CoverWholeHistory_SecurityStampChanged()
    {
        var primary = CreateUser(1);
        primary.Auth.RegisterDate = new DateTime(2020, 1, 1);
        primary.Auth.LastLoginDate = new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero);
        primary.Auth.AspNetSecurityStamp = "old";
        var secondary = CreateUser(2);
        secondary.Auth.RegisterDate = new DateTime(2015, 1, 1);
        secondary.Auth.LastLoginDate = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        secondary.Auth.IsAdmin = true;

        UserProfileMerger.MergeInto(primary, secondary);

        primary.Auth.RegisterDate.ShouldBe(new DateTime(2015, 1, 1));
        primary.Auth.LastLoginDate.ShouldBe(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));
        primary.Auth.IsAdmin.ShouldBeFalse();
        primary.Auth.AspNetSecurityStamp.ShouldNotBe("old");
    }

    [Fact]
    public void AllrpgSid_TakenWhenPrimaryHasNone()
    {
        var primary = CreateUser(1);
        var secondary = CreateUser(2);
        secondary.Allrpg = new AllrpgUserDetails { UserId = 2, Sid = 42, JsonProfile = "{}" };

        UserProfileMerger.MergeInto(primary, secondary);

        primary.Allrpg.ShouldNotBeNull().UserId.ShouldBe(1);
        primary.Allrpg.Sid.ShouldBe(42);
    }

    [Fact]
    public void AllrpgSid_PrimaryWins()
    {
        var primary = CreateUser(1);
        primary.Allrpg = new AllrpgUserDetails { UserId = 1, Sid = 7, JsonProfile = "{\"a\":1}" };
        var secondary = CreateUser(2);
        secondary.Allrpg = new AllrpgUserDetails { UserId = 2, Sid = 42, JsonProfile = "{}" };

        UserProfileMerger.MergeInto(primary, secondary);

        primary.Allrpg.Sid.ShouldBe(7);
        primary.Allrpg.JsonProfile.ShouldBe("{\"a\":1}");
    }

    private static User CreateUser(int id, string? bornName = null, string phone = "", string? telegram = null) => new()
    {
        UserId = id,
        BornName = bornName,
        Email = $"user{id}@example.com",
        UserName = $"user{id}@example.com",
        Auth = new UserAuthDetails { UserId = id, RegisterDate = new DateTime(2020, 1, 1), AspNetSecurityStamp = "" },
        Extra = new UserExtra { UserId = id, PhoneNumber = phone, Telegram = telegram, Nicknames = "", GroupNames = "" },
    };
}
