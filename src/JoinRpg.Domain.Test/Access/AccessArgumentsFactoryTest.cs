using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain.Access;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Domain.Test.Access;

public class AccessArgumentsFactoryTest
{
    private MockedProject Mock { get; } = new MockedProject();

    private static UserIdentification MasterUser => new UserIdentification(2);
    private static UserIdentification PlayerUser => new UserIdentification(1);

    [Fact]
    public void HiddenCharacter_AnonymousUser_CannotView()
    {
        // Персонаж не публичный, пользователь не авторизован
        Mock.Character.IsPublic = false;
        var args = AccessArgumentsFactory.Create(Mock.Character, (UserIdentification?)null, Mock.ProjectInfo);
        args.CanViewCharacterAtAll.ShouldBeFalse();
    }

    [Fact]
    public void HiddenCharacter_MasterUser_CanView()
    {
        // Персонаж не публичный, но пользователь — мастер проекта
        Mock.Character.IsPublic = false;
        var args = AccessArgumentsFactory.Create(Mock.Character, MasterUser, Mock.ProjectInfo);
        args.CanViewCharacterAtAll.ShouldBeTrue();
    }

    [Fact]
    public void HiddenCharacter_ApprovedPlayer_CanView()
    {
        // Персонаж не публичный, но пользователь — утверждённый игрок
        Mock.Character.IsPublic = false;
        _ = Mock.CreateApprovedClaim(Mock.Character, Mock.Player);
        var args = AccessArgumentsFactory.Create(Mock.Character, PlayerUser, Mock.ProjectInfo);
        args.CanViewCharacterAtAll.ShouldBeTrue();
    }

    [Fact]
    public void PublicCharacter_AnonymousUser_CanView()
    {
        // Персонаж публичный — виден всем
        Mock.Character.IsPublic = true;
        var args = AccessArgumentsFactory.Create(Mock.Character, (UserIdentification?)null, Mock.ProjectInfo);
        args.CanViewCharacterAtAll.ShouldBeTrue();
    }

    [Fact]
    public void HiddenCharacter_PublishedProject_AnonCanView()
    {
        // Опубликованные вводные открывают все персонажи для чтения
        Mock.Character.IsPublic = false;
        Mock.Project.Details.PublishPlot = true;
        Mock.ReInitProjectInfo();
        var args = AccessArgumentsFactory.Create(Mock.Character, (UserIdentification?)null, Mock.ProjectInfo);
        args.CanViewCharacterAtAll.ShouldBeTrue();
    }

    [Fact]
    public void HiddenCharacter_OtherPlayer_CannotView()
    {
        // Чужой игрок не должен видеть скрытого персонажа другого игрока
        Mock.Character.IsPublic = false;
        _ = Mock.CreateApprovedClaim(Mock.Character, Mock.Player);
        var otherPlayer = new UserIdentification(99);
        var args = AccessArgumentsFactory.Create(Mock.Character, otherPlayer, Mock.ProjectInfo);
        args.CanViewCharacterAtAll.ShouldBeFalse();
    }
}
