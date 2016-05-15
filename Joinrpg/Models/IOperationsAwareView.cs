using System.Collections.Generic;
using JetBrains.Annotations;

namespace JoinRpg.Web.Models
{
  public interface IOperationsAwareView 
  {
    int? ProjectId { get; }
    [CanBeNull]
    IReadOnlyCollection<int> ClaimIds { get; }
  }
}
