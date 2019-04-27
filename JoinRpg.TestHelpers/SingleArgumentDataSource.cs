using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace JoinRpg.Portal.Test.ContainerTest
{
    /// <summary>
    /// Helper for create XUnit data sources
    /// </summary>
    public abstract class SingleArgumentDataSource : IEnumerable<object[]>
    {
        public abstract IEnumerable<TypeInfo> GetDataSource();

        public IEnumerator<object[]> GetEnumerator() => GetDataSource().Select(type => (new[] { type })).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
