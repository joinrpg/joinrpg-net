using System.Reflection;

namespace JoinRpg.TestHelpers;

/// <summary>
/// Select from assembly every class that derived for some class. Can be used i.e. for found every controller
/// </summary>
/// <typeparam name="TBaseClass"></typeparam>
/// <typeparam name="TAssemblyClass"></typeparam>
public class FindDerivedClassesDataSourceBase<TBaseClass, TAssemblyClass> : SingleArgumentDataSource
{
    public override IEnumerable<TypeInfo> GetDataSource()
    {
        return Assembly.GetAssembly(typeof(TAssemblyClass))!
            .DefinedTypes
            .Where(type => typeof(TBaseClass).IsAssignableFrom(type) && !type.IsAbstract);
    }
}
