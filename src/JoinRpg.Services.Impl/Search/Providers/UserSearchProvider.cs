using System.Data.Entity;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces.Search;

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
        (var idToFind, var matchByIdIsPerfect) = SearchKeywordsResolver.TryGetId(searchString, keysForPerfectMath);

        var results =
          await
            unitOfWork.GetDbSet<User>()
              .Where(user =>
                //TODO Convert to PredicateBuilder
                user.UserId == idToFind
                || user.Email.Contains(searchString)
                || user.FatherName!.Contains(searchString)
                || user.BornName!.Contains(searchString)
                || user.SurName!.Contains(searchString)
                || user.PrefferedName!.Contains(searchString)
                || (user.Extra != null && user.Extra.Nicknames != null && user.Extra.Nicknames.Contains(searchString))
                || (idToFind != null && user.Extra != null && user.Extra.Vk! == "id" + idToFind)
                || (idToFind != null && user.ExternalLogins.Any(el => el.Key == idToFind.ToString()))
              )
              .ToListAsync();

        return results.Select(user =>
        {
            var wasfoundById = user.UserId == idToFind;
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
