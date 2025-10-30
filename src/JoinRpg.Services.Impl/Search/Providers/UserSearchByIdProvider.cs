using System.Data.Entity;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Helpers;
using JoinRpg.Services.Interfaces.Search;
using LinqKit;

namespace JoinRpg.Services.Impl.Search.Providers;
internal class UserSearchByIdProvider(IUnitOfWork unitOfWork) : ISearchProvider
{
    //keep longer strings first to please Regexp
    private static readonly string[] keysForPerfectMath = ["%контакты", "контакты", "%игрок", "игрок",];

    public async Task<IReadOnlyCollection<SearchResult>> SearchAsync(int? currentUserId, string searchString)
    {
        searchString = searchString.Trim();

        (var idToFind, var matchByIdIsPerfect) = SearchKeywordsResolver.TryGetId(searchString, keysForPerfectMath);

        if (idToFind is null && searchString.TryUnprefixNumber("id") is int vkIntId)
        {
            idToFind = vkIntId;
        }

        if (idToFind is null)
        {
            return [];
        }

        var predicateBuilder = PredicateBuilder.New<User>();

        predicateBuilder = predicateBuilder.Or(user => user.UserId == idToFind);

        if (!matchByIdIsPerfect)
        {
            predicateBuilder = predicateBuilder.Or(user => user.Extra != null && user.Extra!.Vk == "id" + idToFind);
            predicateBuilder = predicateBuilder.Or(user => user.ExternalLogins.Any(el => el.Key == idToFind.ToString()));
            predicateBuilder = predicateBuilder.Or(user => user.Extra != null && user.Extra.Vk! == "id" + idToFind);
        }

        var results = await unitOfWork
            .GetDbSet<User>()
            .AsNoTracking()
            .Include(u => u.Extra)
            .Where(predicateBuilder)
            .Take(30)
            .ToListAsync();

        return [.. results.Select(
            user => {
                var result = user.GetUserResult();
                result.IsPerfectMatch = matchByIdIsPerfect;
                result.Description = SearchUtils.GetFoundByIdDescription(idToFind.Value);
                return result;
            })];
    }
}
