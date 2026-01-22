using JoinRpg.DataModel.Extensions;
using JoinRpg.PrimitiveTypes.Access;

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
