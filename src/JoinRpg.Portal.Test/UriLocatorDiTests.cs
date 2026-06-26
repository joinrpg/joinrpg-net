using System.Collections;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Portal.Test;

public class UriLocatorDiTests(IntegrationTestPortalFactory factory)
    : IClassFixture<IntegrationTestPortalFactory>
{
    [Theory]
    [ClassData(typeof(ProjectEntityIdDataSource))]
    public void IUriLocator_Resolves_From_DI(Type entityType)
    {
        var serviceType = typeof(IUriLocator<>).MakeGenericType(entityType);
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetService(serviceType);
        service.ShouldNotBeNull($"IUriLocator<{entityType.Name}> should resolve from DI container");
    }

    private class ProjectEntityIdDataSource : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            var interfaceType = typeof(IProjectEntityId);
            var types = interfaceType.Assembly.ExportedTypes
                .Where(t => t.IsAssignableTo(interfaceType))
                .Where(t => !t.IsInterface)
                .Select(t => new object[] { t });
            return types.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
