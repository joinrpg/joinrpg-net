using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces.Search;

namespace JoinRpg.Services.Impl.Search;
internal static class SearchUtils
{
    public static SearchResult GetUserResult(this User user)
    {
        return new SearchResult
        {
            LinkType = LinkType.ResultUser,
            Name = user.GetDisplayName(),
            Description = new MarkdownString(),
            Identification = user.UserId.ToString(),
            ProjectId = null, //Users not associated with any project
            IsPublic = true,
            IsActive = true,
            IsPerfectMatch = false,
        };
    }

    public static MarkdownString GetFoundByIdDescription(int idToFind) => new($"ID: {idToFind}");
}
