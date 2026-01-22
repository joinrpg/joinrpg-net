using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.Web.Models.Masters;

namespace JoinRpg.WebPortal.Models.Test;

public class PermissionBadgeTests
{
    [Theory]
    [ClassData(typeof(EnumTheoryDataGenerator<Permission>))]
    public void EveryPermissionShouldHaveVisualization(Permission permission)
    {
        _ = Should.NotThrow(() => new PermissionBadgeViewModel(permission, true));
    }

    [Theory]
    [ClassData(typeof(EnumTheoryDataGenerator<Permission>))]
    public void EveryPermissionShouldMapToChangeAclViewModelField(Permission permission)
    {
        if (permission == Permission.None) // Нам не нужно иметь галочку для этого случая
        {
            typeof(ChangeAclViewModel).GetProperty(permission.ToString()).ShouldBeNull();
        }
        else
        {
            typeof(ChangeAclViewModel).GetProperty(permission.ToString()).ShouldNotBeNull();
        }
    }
}
