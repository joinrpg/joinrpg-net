using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces.Search;

namespace JoinRpg.Services.Impl.Search;

internal class SearchResultImpl : ISearchResult
{
    public required LinkType LinkType { get; set; }
    public required string Name { get; set; }
    public required MarkdownString Description { get; set; }

    public required bool IsPublic { get; set; }

    public required bool IsActive { get; set; }
    public bool IsPerfectMatch { get; set; } = false;

    public required string Identification { get; set; }
    public required int? ProjectId { get; set; }
}
