using JoinRpg.DataModel.Mocks;
using JoinRpg.DataModel.Mocks.Fakes;
using JoinRpg.DomainTypes.Users;
using JoinRpg.Interfaces;
using JoinRpg.Web.Models.Masters;
using JoinRpg.Web.UserProfile;

namespace JoinRpg.WebPortal.Models.Test;

/// <summary>Карточка мастера в списке и на странице правки мастера — из полного профиля.</summary>
public class MasterCardAccessReasonTest
{
    private readonly MockedProject mock = new();

    // В UserInfo мастера ActiveProjects — его действующие мастерства в активных проектах; мок их не заполняет.
    private UserInfo ActiveGameMaster => mock.MasterInfo with { ActiveProjects = [mock.ProjectInfo.ProjectId] };

    // У мастера архивной игры её в ActiveProjects нет.
    private UserInfo ArchivedGameMaster => mock.MasterInfo with { ActiveProjects = [] };

    private MastersListViewModel ListFor(UserInfo master, ICurrentUserAccessor viewer)
        => new([], viewer, mock.ProjectInfo, [], new Dictionary<UserIdentification, UserInfo> { [master.UserId] = master });

    private static ICurrentUserAccessor OtherMaster => new FakeCurrentUserAccessor(new UserIdentification(100500));

    [Fact]
    public void OwnCard_IsMyProfile()
        => ListFor(ActiveGameMaster, new FakeCurrentUserAccessor(mock.MasterInfo.UserId)).Masters.ShouldHaveSingleItem()
            .UserDetails.Reason.ShouldBe(AccessReasonView.ItsMe);

    [Fact]
    public void OtherMasterCard_IsCoMasterWithContacts()
    {
        var card = ListFor(ActiveGameMaster, OtherMaster).Masters.ShouldHaveSingleItem().UserDetails;

        card.Reason.ShouldBe(AccessReasonView.CoMaster);
        card.Email.ShouldBe(mock.MasterInfo.Email);
    }

    /// <summary>Архивная игра не даёт доступа к персональным данным, в том числе со-мастерам.</summary>
    [Fact]
    public void ArchivedGame_GivesNoAccessToCoMasterContacts()
    {
        var card = ListFor(ArchivedGameMaster, OtherMaster).Masters.ShouldHaveSingleItem().UserDetails;

        card.Reason.ShouldBe(AccessReasonView.NoAccess);
        card.Email.ShouldBeNull();
    }
}
