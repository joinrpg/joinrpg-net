using System.Data.Entity;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.AdminTools;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.Data.Interfaces.Claims;

namespace JoinRpg.Data.Write.Interfaces;

public interface IUnitOfWork : IDisposable
{
    /// <summary>
    ///
    /// </summary>
    /// <remarks>
    /// We must <see cref="DbSet{Entity}"/>, not <see cref="IDbSet{TEntity}"/> because of .Add/RemoveRange() methods</remarks>
    DbSet<T> GetDbSet<T>() where T : class;
    Task SaveChangesAsync();

    IUserRepository GetUsersRepository();
    IProjectRepository GetProjectRepository();
    IProjectMetadataWriteRepository GetProjectMetadataWriteRepository();

    /// <summary>
    /// Репозиторий записи агрегата персонажа (ADR014). Доступен только отсюда, а не из DI:
    /// иначе он получил бы другой <c>DbContext</c>, чем тот, на котором вызывается
    /// <see cref="SaveChangesAsync"/>.
    /// </summary>
    ICharacterAggregateWriteRepository GetCharacterAggregateWriteRepository();
    IClaimsRepository GetClaimsRepository();
    IPlotRepository GetPlotRepository();
    IForumRepository GetForumRepository();
    ICharacterRepository GetCharactersRepository();
    IAccommodationRepository GetAccomodationRepository();

    IKogdaIgraRepository GetKogdaIgraRepository();

    [Obsolete("Временный хак")]
    Task<int> ExecuteSqlCommandAsync(string sql);
}
