using System.Data.Entity;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces.Search;
using LinqKit;

namespace JoinRpg.Services.Impl.Search;

internal class UserSearchProvider : ISearchProvider
{
    //keep longer strings first to please Regexp
    private static readonly string[] keysForPerfectMath =
    {
  "%контакты",
  "контакты",
  "%игрок",
  "игрок",
};
    private readonly IUnitOfWork unitOfWork;

    public UserSearchProvider(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyCollection<ISearchResult>> SearchAsync(int? currentUserId, string searchString)
    {
        searchString = searchString.Trim();

        if (searchString == "")
        {
            return Array.Empty<ISearchResult>();
        }

        (var idToFind, var matchByIdIsPerfect) = SearchKeywordsResolver.TryGetId(searchString, keysForPerfectMath);

        var predicateBuilder = PredicateBuilder.New<User>();

        predicateBuilder = predicateBuilder.Or(
                user => user.Email.Contains(searchString)
                    || user.FatherName!.Contains(searchString)
                    || user.BornName!.Contains(searchString)
                    || user.SurName!.Contains(searchString)
                    || user.PrefferedName!.Contains(searchString)
                    || (user.Extra != null && user.Extra.Nicknames != null && user.Extra.Nicknames.Contains(searchString)));

        if (idToFind is int intId)
        {
            predicateBuilder = predicateBuilder.Or(user => user.UserId == intId);
            predicateBuilder = predicateBuilder.Or(user => user.Extra != null && user.Extra.Vk! == "id" + intId);
            predicateBuilder = predicateBuilder.Or(user => user.ExternalLogins.Any(el => el.Key == intId.ToString()));
        }

        if (searchString.UnprefixNumber("id") is int vkIntId)
        {
            predicateBuilder = predicateBuilder.Or(user => user.Extra != null && user.Extra.Vk! == "id" + vkIntId);
            matchByIdIsPerfect = true;
        }

        var results = await unitOfWork
            .GetDbSet<User>()
            .AsNoTracking()
            .Include(u => u.Extra)
            .Where(predicateBuilder)
            .Take(30)
            .ToListAsync();

        return results.Select(user =>
        {
            var wasfoundById = user.UserId == idToFind || user.Extra?.Vk == idToFind.ToString();
            var description = new MarkdownString(wasfoundById
        ? WorldObjectProviderBase.GetFoundByIdDescription(user.UserId)
        : "");

            return new SearchResultImpl
            {
                LinkType = LinkType.ResultUser,
                Name = user.GetDisplayName(),
                Description = description,
                Identification = user.UserId.ToString(),
                ProjectId = null, //Users not associated with any project
                IsPublic = true,
                IsActive = true,
                IsPerfectMatch = wasfoundById && matchByIdIsPerfect,
            };
        }).ToList();
    }
}
