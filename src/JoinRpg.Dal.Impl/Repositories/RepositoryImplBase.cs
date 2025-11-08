namespace JoinRpg.Dal.Impl.Repositories;

[Obsolete]
public abstract class RepositoryImplBase : IDisposable
{
    protected MyDbContext Ctx { get; }

    protected RepositoryImplBase(MyDbContext ctx) => Ctx = ctx;

    public void Dispose() => Ctx?.Dispose();
}
