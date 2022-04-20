using System.Collections;
using System.Reflection;

namespace JoinRpg.TestHelpers
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
