using System.Data.Entity;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;

namespace JoinRpg.Data.Write.Interfaces
{
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
        IClaimsRepository GetClaimsRepository();
        IPlotRepository GetPlotRepository();
        IForumRepository GetForumRepository();
        ICharacterRepository GetCharactersRepository();
        IAccommodationRepository GetAccomodationRepository();
    }
}
