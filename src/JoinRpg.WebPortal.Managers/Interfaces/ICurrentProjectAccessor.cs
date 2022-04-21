using JoinRpg.PrimitiveTypes;

namespace JoinRpg.WebPortal.Managers.Interfaces;

/// <summary>
/// Allows manager to read current project id. 
/// </summary>
public interface ICurrentProjectAccessor
{
    /// <summary>
    /// Project that current page corresponds to
    /// </summary>
    ProjectIdentification ProjectId { get; }
}
