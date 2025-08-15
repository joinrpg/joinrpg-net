using System.Data.Entity;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces.Search;
using LinqKit;

namespace JoinRpg.Services.Impl.Search.Providers;

internal class UserSearchProvider(IUnitOfWork unitOfWork) : ISearchProvider
{
    public async Task<IReadOnlyCollection<ISearchResult>> SearchAsync(int? currentUserId, string searchString)
    {
        if (searchString.Length < 3)
        {
            return [];
        }
        var predicateBuilder = PredicateBuilder.New<User>();

        predicateBuilder = predicateBuilder.Or(
                user => user.Email.Contains(searchString)
                    || user.FatherName!.Contains(searchString)
                    || user.BornName!.Contains(searchString)
                    || user.SurName!.Contains(searchString)
                    || user.PrefferedName!.Contains(searchString)
                    || (user.Extra != null && user.Extra.Nicknames != null && user.Extra.Nicknames.Contains(searchString)));

        var results = await unitOfWork
            .GetDbSet<User>()
            .AsNoTracking()
            .Include(u => u.Extra)
            .Where(predicateBuilder)
            .Take(30)
            .ToListAsync();

        return results.Select(user => user.GetUserResult()).ToList();
    }


}
