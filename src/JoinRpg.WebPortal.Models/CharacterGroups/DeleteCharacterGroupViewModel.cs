using System.Diagnostics.CodeAnalysis;
using JoinRpg.Markdown;
using Microsoft.AspNetCore.Components;

namespace JoinRpg.Web.Models.CharacterGroups;

[method: SetsRequiredMembers]
public class DeleteCharacterGroupViewModel(CharacterGroupFullInfo info)
{
    public int CharacterGroupId { get; init; } = info.Id.CharacterGroupId;
    public int ProjectId { get; init; } = info.Id.ProjectId.Value;
    public required string Name { get; init; } = info.Name;
    public MarkupString? Description { get; init; } = info.Description.ToHtmlString();
    public int ChildGroupsCount { get; init; } = info.DirectChildGroupIds.Count;
    public int CharactersCount { get; init; } = info.DirectChildCharactersCount;
}
