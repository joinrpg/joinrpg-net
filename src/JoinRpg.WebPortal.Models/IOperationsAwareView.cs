namespace JoinRpg.Web.Models;

public interface IOperationsAwareView
{
    int? ProjectId { get; }

    int? CharacterGroupId => null;

    bool ShowCharacterCreateButton => ProjectId is not null;

    IReadOnlyCollection<int> ClaimIds { get; }

    IReadOnlyCollection<int> CharacterIds { get; }

    string? InlineTitle { get; }

    string? CountString { get; }
}
