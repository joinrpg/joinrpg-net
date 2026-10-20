using JoinRpg.DataModel;
using JoinRpg.DataModel.Users;
using JoinRpg.DomainTypes.Users;

namespace JoinRpg.Domain.Test;

public class UserAvatarExtensionsTest
{
    [Fact]
    public void AddGravatar_NewUser_SelectsGravatarByEmail()
    {
        var user = new User { Email = "Somebody@Example.com" };

        var avatar = user.AddGravatar();

        user.SelectedAvatar.ShouldBe(avatar);
        user.Avatars.ShouldBe([avatar]);
        avatar.AvatarSource.ShouldBe(AvatarSource.GrAvatar);
        avatar.IsActive.ShouldBeTrue();
        avatar.CachedUri.ShouldBeNull();
        // sha256("somebody@example.com") — хеш от email в нижнем регистре, как требует Gravatar.
        avatar.OriginalUri.ShouldBe("https://www.gravatar.com/avatar/787d5b7c06ca0a21c96436cc7c8117e6fe046d0fd7dedcae8c93bfc14b8e5df7?d=robohash&s=64");
    }

    [Fact]
    public void AddGravatar_HasSelectedAvatar_KeepsSelection()
    {
        var social = new UserAvatar { AvatarSource = AvatarSource.SocialNetwork, IsActive = true, OriginalUri = "https://example.com/vk.jpg" };
        var user = new User { Email = "somebody@example.com", SelectedAvatar = social, SelectedAvatarId = 1 };
        user.Avatars.Add(social);

        _ = user.AddGravatar();

        user.SelectedAvatar.ShouldBe(social);
        user.Avatars.Count.ShouldBe(2);
    }

    [Fact]
    public void AddSocialAvatar_DefaultGravatar_SelectsSocial()
    {
        var user = new User { Email = "somebody@example.com" };
        _ = user.AddGravatar();
        var social = CreateSocialAvatar();

        user.AddSocialAvatar(social);

        user.SelectedAvatar.ShouldBe(social);
    }

    [Fact]
    public void AddSocialAvatar_NoAvatars_SelectsSocial()
    {
        var user = new User { Email = "somebody@example.com" };
        var social = CreateSocialAvatar();

        user.AddSocialAvatar(social);

        user.SelectedAvatar.ShouldBe(social);
        user.Avatars.ShouldBe([social]);
    }

    [Fact]
    public void AddSocialAvatar_GravatarChosenOverSocial_KeepsGravatar()
    {
        // Пользователь уже имел аватарку из соцсети и сам выбрал Gravatar — новая фотография из соцсети его выбор не меняет.
        var user = new User { Email = "somebody@example.com" };
        var gravatar = user.AddGravatar();
        user.Avatars.Add(CreateSocialAvatar());

        user.AddSocialAvatar(CreateSocialAvatar());

        user.SelectedAvatar.ShouldBe(gravatar);
        user.Avatars.Count.ShouldBe(3);
    }

    [Fact]
    public void AddSocialAvatar_SocialSelected_KeepsSelection()
    {
        var user = new User { Email = "somebody@example.com" };
        var first = CreateSocialAvatar();
        user.AddSocialAvatar(first);

        user.AddSocialAvatar(CreateSocialAvatar());

        user.SelectedAvatar.ShouldBe(first);
    }

    [Fact]
    public void AddSocialAvatar_SelectedDeactivated_SelectsNew()
    {
        // Соцсеть сменила фотографию: прежняя аватарка выключена до вызова, выбранной должна стать новая.
        var user = new User { Email = "somebody@example.com" };
        _ = user.AddGravatar();
        var old = CreateSocialAvatar();
        user.AddSocialAvatar(old);
        old.IsActive = false;
        var fresh = CreateSocialAvatar();

        user.AddSocialAvatar(fresh);

        user.SelectedAvatar.ShouldBe(fresh);
    }

    private static UserAvatar CreateSocialAvatar() => new()
    {
        AvatarSource = AvatarSource.SocialNetwork,
        IsActive = true,
        ProviderId = "Vkontakte",
        OriginalUri = "https://example.com/vk.jpg",
    };
}
