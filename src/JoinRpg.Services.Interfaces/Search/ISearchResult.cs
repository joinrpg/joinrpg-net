using JoinRpg.DataModel;

namespace JoinRpg.Services.Interfaces.Search;

public record class SearchResult : ILinkable
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
