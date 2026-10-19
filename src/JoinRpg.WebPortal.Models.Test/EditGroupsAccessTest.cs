using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.WebPortal.Models.Test;

/// <summary>
/// Флаги <see cref="ProjectInfo.HasEditGroupsAccess"/> и <see cref="ProjectInfo.HasEditRolesAccess"/>, на которых
/// стоят кнопки страницы группы (ADR023): настройка групп в заблокированном проекте разрешена — группы
/// заводят заново при восстановлении, а добавление персонажа и удаление группы (оно перевешивает
/// персонажей) — нет.
/// </summary>
public class EditGroupsAccessTest
{
    public enum TestProjectState { Active, Blocked, Archived }

    [Theory]
    [InlineData(TestProjectState.Active, true, true)]
    [InlineData(TestProjectState.Blocked, true, false)]
    [InlineData(TestProjectState.Archived, false, false)]
    public void EditAccess_DependsOnProjectState(TestProjectState state, bool canEditGroups, bool canEditRoles)
    {
        var mock = new MockedProject();
        switch (state)
        {
            case TestProjectState.Blocked:
                mock.Project.IsBlocked = true;
                break;
            case TestProjectState.Archived:
                mock.Project.Active = false;
                mock.Project.IsAcceptingClaims = false;
                break;
        }
        mock.ReInitProjectInfo();
        var master = new UserIdentification(mock.Master.UserId);

        mock.ProjectInfo.HasEditGroupsAccess(master).ShouldBe(canEditGroups);
        mock.ProjectInfo.HasEditRolesAccess(master).ShouldBe(canEditRoles);
    }

    [Fact]
    public void EditGroupsAccess_RequiresEditRolesPermission()
    {
        var mock = new MockedProject();
        mock.Project.IsBlocked = true;
        mock.ReInitProjectInfo();

        mock.ProjectInfo.HasEditGroupsAccess(new UserIdentification(mock.Player.UserId)).ShouldBeFalse();
    }
}
