using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Web.Models;

public interface IOperationsAwareView
{
    int? ProjectId { get; }

    int? CharacterGroupId => null;

    bool ShowCharacterCreateButton => ProjectId is not null;

    IReadOnlyCollection<ClaimIdentification> ClaimIds { get; }

    IReadOnlyCollection<CharacterIdentification> CharacterIds { get; }

    string? InlineTitle { get; }

    string? CountString { get; }

    string ClaimIdCompressed() => new CompressedIntList(ClaimIds).ToString();

    string CharacterIdCompressed() => new CompressedIntList(CharacterIds).ToString();
}
