using System.Reflection;
using JoinRpg.Dal.Impl.Repositories;

namespace JoinRpg.Dal.Impl;

public static class RepositoriesRegistraton
{
    public static IEnumerable<Type> GetTypes()
    {
        foreach (var type in Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsAssignableTo(typeof(RepositoryImplBase)) && !t.IsAbstract))
        {
            yield return type;
        }
        yield return typeof(UserInfoRepository);
    }
}
