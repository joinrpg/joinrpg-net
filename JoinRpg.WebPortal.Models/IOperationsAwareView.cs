using System.Collections.Generic;
using JetBrains.Annotations;

namespace JoinRpg.Web.Models
{
    public interface IOperationsAwareView
    {
        int? ProjectId { get; }

        [NotNull]
        IReadOnlyCollection<int> ClaimIds { get; }

        [NotNull]
        IReadOnlyCollection<int> CharacterIds { get; }
    }
}
