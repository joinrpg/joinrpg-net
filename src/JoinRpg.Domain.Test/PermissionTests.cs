using JoinRpg.DataModel.Extensions;

namespace JoinRpg.Domain.Test;

public class PermissionTests
{
    [Theory]
    [ClassData(typeof(EnumTheoryDataGenerator<Permission>))]
    public void EveryPermissionIsTranslated(Permission permission)
    {
        permission.GetPermssionExpression().ShouldNotBeNull();
    }
}
