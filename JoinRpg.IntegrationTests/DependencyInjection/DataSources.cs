using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JoinRpg.Portal;
using JoinRpg.Portal.Test.ContainerTest;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.IntegrationTests.DependencyInjection
{
    public class FindDerivedClassesDataSourceBase<TClass> : SingleArgumentDataSource
    {
        public override IEnumerable<TypeInfo> GetDataSource()
        {
            return Assembly.GetAssembly(typeof(Startup))
                .DefinedTypes
                .Where(type => typeof(TClass).IsAssignableFrom(type) && !type.IsAbstract);
        }
    }

    public class ControllerDataSource : FindDerivedClassesDataSourceBase<Controller>
    {
    }

    public class ViewComponentsDataSource : FindDerivedClassesDataSourceBase<ViewComponent>
    {
    }
}
