using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Test.ContainterTest
{
    public class ControllerDataSource : SingleArgumentDataSource
    {
        protected override IEnumerable<TypeInfo> GetDataSource()
        {
            return Assembly.GetAssembly(typeof(Startup))
                                       .DefinedTypes
                                       .Where(type => typeof(Controller).IsAssignableFrom(type) && !type.IsAbstract);
        }
    }
}
