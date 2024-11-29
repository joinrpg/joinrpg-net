using JoinRpg.Domain.Access;
using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.TestHelpers;
using Shouldly;
using Xunit;

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
