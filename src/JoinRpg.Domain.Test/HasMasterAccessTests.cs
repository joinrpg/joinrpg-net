#pragma warning disable CS0618 // тестируем устаревший метод, который содержал баг
using JoinRpg.DataModel.Mocks;

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

    // ADR019: снятый мастер не даёт доступа ни через ProjectInfo, ни через EF-сущности.
    // Права у строки оставлены (CreateMaster даёт все) — проверяется именно статус.
    private UserIdentification CreateRemovedMaster()
    {
        var user = Mock.CreateMaster();
        Mock.Project.ProjectAcls.Single(acl => acl.UserId == user.UserId).Status = ProjectAclStatus.Removed;
        Mock.ReInitProjectInfo();
        return new UserIdentification(user.UserId);
    }

    [Theory]
    [InlineData(Permission.None)]
    [InlineData(Permission.CanEditRoles)]
    public void ProjectInfo_HasMasterAccess_RemovedMaster_ReturnsFalse(Permission permission)
    {
        var removed = CreateRemovedMaster();

        Mock.ProjectInfo.HasMasterAccess(removed, permission).ShouldBeFalse();
    }

    [Theory]
    [InlineData(Permission.None)]
    [InlineData(Permission.CanEditRoles)]
    public void HasMasterAccess_RemovedMaster_ReturnsFalse(Permission permission)
    {
        var removed = CreateRemovedMaster();

        Mock.Character.HasMasterAccess(removed, permission).ShouldBeFalse();
    }

    [Fact]
    public void ProjectInfo_RemovedMaster_IsFormerNotCurrent()
    {
        var removed = CreateRemovedMaster();

        Mock.ProjectInfo.Masters.ShouldNotContain(m => m.UserId == removed);
        Mock.ProjectInfo.FormerMasters.ShouldContain(m => m.UserId == removed && m.Status == ProjectAclStatus.Removed);
        Mock.ProjectInfo.Masters.ShouldContain(m => m.UserId == MasterUser);
    }

    [Fact]
    public void ProjectInfo_WithMethods_KeepFormerMasters()
    {
        var removed = CreateRemovedMaster();

        _ = Mock.CreateField("Поле после снятия мастера"); // пересобирает ProjectInfo через WithAddedField

        Mock.ProjectInfo.FormerMasters.ShouldContain(m => m.UserId == removed);
        Mock.ProjectInfo.Masters.ShouldNotContain(m => m.UserId == removed);
    }
}
