using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Services.Impl.Search;
internal static class SearchUtils
{
    public static SearchResultImpl GetUserResult(this User user)
    {
        return new SearchResultImpl
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
