using JetBrains.Annotations;

namespace JoinRpg.Web.Models
{
    public interface IOperationsAwareView
    {
        int? ProjectId { get; }

        int? CharacterGroupId => null;

        bool ShowCharacterCreateButton => ProjectId is not null;

        [NotNull]
        IReadOnlyCollection<int> ClaimIds { get; }

        [NotNull]
        IReadOnlyCollection<int> CharacterIds { get; }
    }
}
