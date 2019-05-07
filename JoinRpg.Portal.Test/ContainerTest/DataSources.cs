using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Test.ContainerTest
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
