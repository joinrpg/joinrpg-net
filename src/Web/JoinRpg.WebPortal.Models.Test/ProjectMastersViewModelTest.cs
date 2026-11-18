using JoinRpg.DataModel.Mocks;
using JoinRpg.DataModel.Mocks.Fakes;
using JoinRpg.DomainTypes.Users;
using JoinRpg.Web.Games.Projects;
using JoinRpg.Web.UserProfile;

namespace JoinRpg.WebPortal.Models.Test;

/// <summary>
/// Публичная страница мастеров одинакова для всех: в карточке только публичные контакты, даже если смотрящему
/// доступно больше.
/// </summary>
public class ProjectMastersViewModelTest
{
    private readonly MockedProject mock = new();

    private Dictionary<UserIdentification, ProjectMasterProfileViewModel> Profiles => new()
    {
        [mock.MasterInfo.UserId] = new ProjectMasterProfileViewModel("Главный мастер", null),
    };

    private Dictionary<UserIdentification, UserInfo> Users => new()
    {
        [mock.MasterInfo.UserId] = mock.MasterInfo,
    };

    [Fact]
    public void Anonymous_SeesCardWithoutContacts()
    {
        var card = BuildFor(viewerId: null).Masters.ShouldHaveSingleItem();

        card.Role.ShouldBe("Главный мастер");
        card.Details.User.UserId.ShouldBe(mock.MasterInfo.UserId);
        card.Details.Reason.ShouldBe(AccessReasonView.NoAccess);
        card.Details.Email.ShouldBeNull();
    }

    [Fact]
    public void Master_SeesOwnCardWithoutContacts()
    {
        // Даже свою карточку мастер видит так же, как игроки.
        var card = BuildFor(mock.MasterInfo.UserId).Masters.ShouldHaveSingleItem();

        card.Details.Reason.ShouldBe(AccessReasonView.NoAccess);
        card.Details.Email.ShouldBeNull();
    }

    [Fact]
    public void PublicSocialNetworks_AreShown()
    {
        var master = mock.MasterInfo with
        {
            Social = mock.MasterInfo.Social with { SocialNetworksAccess = ContactsAccessType.Public },
        };

        var card = ProjectMastersViewModel.Build(mock.ProjectInfo, viewerId: null, Profiles, new Dictionary<UserIdentification, UserInfo> { [master.UserId] = master }, CardBuilder)
            .Masters.ShouldHaveSingleItem();

        card.Details.HasSocialAccess.ShouldBeTrue();
        card.Details.Email.ShouldBeNull();
    }

    [Fact]
    public void LinkToManagement_OnlyForProjectMaster()
    {
        BuildFor(mock.MasterInfo.UserId).ViewerIsMaster.ShouldBeTrue();
        BuildFor(mock.PlayerInfo.UserId).ViewerIsMaster.ShouldBeFalse();
        BuildFor(viewerId: null).ViewerIsMaster.ShouldBeFalse();
    }

    [Fact]
    public void MasterWithoutLoadedUser_IsSkipped()
        => ProjectMastersViewModel.Build(mock.ProjectInfo, viewerId: null, Profiles, new Dictionary<UserIdentification, UserInfo>(), CardBuilder)
            .Masters.ShouldBeEmpty();

    private static UserProfileDetailsBuilder CardBuilder => new(new FakeAvatarUriResolver());

    private ProjectMastersViewModel BuildFor(UserIdentification? viewerId)
        => ProjectMastersViewModel.Build(mock.ProjectInfo, viewerId, Profiles, Users, CardBuilder);
}
