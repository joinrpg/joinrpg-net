using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Web.ProjectCommon.Masters;

namespace JoinRpg.WebPortal.Models.Test;

public class PermissionBadgeTests
{
    [Theory]
    [ClassData(typeof(EnumTheoryDataGenerator<Permission>))]
    public void EveryPermissionShouldHaveVisualization(Permission permission)
    {
        _ = Should.NotThrow(() => new PermissionBadgeViewModel(permission, true));
    }
}
