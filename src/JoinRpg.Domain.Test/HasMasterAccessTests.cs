#pragma warning disable CS0618 // тестируем устаревший метод, который содержал баг
using JoinRpg.DataModel.Mocks;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Access;

namespace JoinRpg.Domain.Test;

public class HasMasterAccessTests
{
    private MockedProject Mock { get; } = new MockedProject();

    private static UserIdentification MasterUser => new UserIdentification(2);
    private static UserIdentification NonMasterUser => new UserIdentification(99);

    [Fact]
    public void HasMasterAccess_AnonymousUser_ReturnsFalse()
    {
        // Регрессионный тест: implicit operator int в UserIdentification вызывал NullReferenceException
        // при сравнении int (DataModel.ProjectAcl.UserId) с UserIdentification? == null
        Mock.Character.HasMasterAccess((UserIdentification?)null).ShouldBeFalse();
    }

    [Fact]
    public void HasMasterAccess_MasterUser_ReturnsTrue()
    {
        Mock.Character.HasMasterAccess(MasterUser).ShouldBeTrue();
    }

    [Fact]
    public void HasMasterAccess_NonMasterUser_ReturnsFalse()
    {
        Mock.Character.HasMasterAccess(NonMasterUser).ShouldBeFalse();
    }

    [Fact]
    public void ProjectInfo_HasMasterAccess_AnonymousUser_ReturnsFalse()
    {
        Mock.ProjectInfo.HasMasterAccess((UserIdentification?)null).ShouldBeFalse();
    }

    [Fact]
    public void ProjectInfo_HasMasterAccess_MasterUser_ReturnsTrue()
    {
        Mock.ProjectInfo.HasMasterAccess(MasterUser).ShouldBeTrue();
    }

    [Fact]
    public void ProjectInfo_HasMasterAccess_NonMasterUser_ReturnsFalse()
    {
        Mock.ProjectInfo.HasMasterAccess(NonMasterUser).ShouldBeFalse();
    }

    [Theory]
    [InlineData(Permission.None)]
    [InlineData(Permission.CanEditRoles)]
    public void HasMasterAccess_AnonymousUserWithPermission_ReturnsFalse(Permission permission)
    {
        Mock.Character.HasMasterAccess((UserIdentification?)null, permission).ShouldBeFalse();
    }
}
