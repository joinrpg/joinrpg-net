using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Users;
using JoinRpg.Web.Games.Projects;
using JoinRpg.Web.UserProfile;

namespace JoinRpg.WebPortal.Models.Test;

/// <summary>Публичная страница мастеров: контакты в карточке — по правилам профиля пользователя.</summary>
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
        var card = ProjectMastersViewModel.Build(mock.ProjectInfo, viewer: null, Profiles, Users).Masters.ShouldHaveSingleItem();

        card.Role.ShouldBe("Главный мастер");
        card.Details.User.UserId.ShouldBe(mock.MasterInfo.UserId);
        card.Details.Reason.ShouldBe(AccessReasonView.NoAccess);
        card.Details.Email.ShouldBeNull();
    }

    [Fact]
    public void Admin_SeesContactsLikeInProfile()
    {
        var admin = mock.PlayerInfo with { UserId = new UserIdentification(100500), IsAdmin = true };

        var card = ProjectMastersViewModel.Build(mock.ProjectInfo, admin, Profiles, Users).Masters.ShouldHaveSingleItem();

        card.Details.Reason.ShouldBe(AccessReasonView.Administrator);
        card.Details.Email.ShouldBe(mock.MasterInfo.Email);
    }

    [Fact]
    public void PlayerWithClaimInProject_SeesNoContacts()
    {
        // Заявка игрока в этом проекте доступа к контактам мастера не даёт — как и в профиле мастера.
        var card = ProjectMastersViewModel.Build(mock.ProjectInfo, mock.PlayerInfo, Profiles, Users).Masters.ShouldHaveSingleItem();

        card.Details.Reason.ShouldBe(AccessReasonView.NoAccess);
        card.Details.Email.ShouldBeNull();
    }

    [Fact]
    public void CoMasterOfAnotherProject_SeesContacts()
    {
        var otherProject = new ProjectIdentification(100500);
        var master = mock.MasterInfo with { ActiveProjects = [mock.ProjectInfo.ProjectId, otherProject] };
        var coMaster = mock.PlayerInfo with { UserId = new UserIdentification(100501), ActiveProjects = [otherProject] };

        var card = ProjectMastersViewModel.Build(mock.ProjectInfo, coMaster, Profiles, new Dictionary<UserIdentification, UserInfo> { [master.UserId] = master })
            .Masters.ShouldHaveSingleItem();

        card.Details.Reason.ShouldBe(AccessReasonView.CoMaster);
        card.Details.Email.ShouldBe(master.Email);
    }

    [Fact]
    public void MasterWithoutLoadedUser_IsSkipped()
        => ProjectMastersViewModel.Build(mock.ProjectInfo, viewer: null, Profiles, users: new Dictionary<UserIdentification, UserInfo>())
            .Masters.ShouldBeEmpty();
}
