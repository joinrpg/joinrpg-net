using System.Data.Entity;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces.Search;
using LinqKit;

namespace JoinRpg.Services.Impl.Search.Providers;
internal class UserSearchBySocialProvider(IUnitOfWork unitOfWork) : ISearchProvider
{
    public async Task<IReadOnlyCollection<SearchResult>> SearchAsync(int? currentUserId, string searchString)
    {
        searchString = searchString.Trim();

        if (searchString == "")
        {
            return [];
        }

        var predicateBuilder = PredicateBuilder.New<User>();

        predicateBuilder = predicateBuilder.Or(user => user.Extra.Vk == searchString);
        predicateBuilder = predicateBuilder.Or(user => user.Extra.Telegram == searchString);

        var results = await unitOfWork
            .GetDbSet<User>()
            .AsNoTracking()
            .Include(u => u.Extra)
            .Where(predicateBuilder)
            .Take(30)
            .ToListAsync();

        return [.. results.Select(user => user.GetUserResult())];
    }
}

