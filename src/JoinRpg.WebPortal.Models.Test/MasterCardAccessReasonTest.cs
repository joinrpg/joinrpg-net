using JoinRpg.Common.PrimitiveTypes.Users;

namespace JoinRpg.WebPortal.Models.Test;

/// <summary>Причина доступа на карточке мастера в списке и на странице правки мастера.</summary>
public class MasterCardAccessReasonTest
{
    private static readonly UserInfoHeader master = new(new UserIdentification(1), new UserDisplayName("Master", null));

    [Fact]
    public void OwnCard_IsMyProfile()
        => new UserProfileDetailsViewModel(master, new UserIdentification(1)).Reason.ShouldBe(AccessReasonView.ItsMe);

    [Fact]
    public void OtherMasterCard_IsCoMaster()
        => new UserProfileDetailsViewModel(master, new UserIdentification(2)).Reason.ShouldBe(AccessReasonView.CoMaster);

    [Fact]
    public void AdminNotInProject_SeesCoMaster()
        => new UserProfileDetailsViewModel(master, currentUserId: null).Reason.ShouldBe(AccessReasonView.CoMaster);
}
