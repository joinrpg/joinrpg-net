using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Web.Models.ClaimList;

public class ClaimListForExportViewModel
{
    public IEnumerable<ClaimListItemForExportViewModel> Items { get; }

    public ClaimListForExportViewModel(int currentUserId, IReadOnlyCollection<Claim> claims, ProjectInfo projectInfo)
        : this(currentUserId, claims.Select(c => (c, projectInfo)).ToList())
    {
    }

    /// <summary>
    /// Use this ctor if need to export claims from different projects
    /// </summary>
    /// <param name="currentUserId"></param>
    /// <param name="claimPair"></param>
    public ClaimListForExportViewModel(int currentUserId, IReadOnlyCollection<(Claim Claim, ProjectInfo ProjectInfo)> claimPair)
    {
        Items = claimPair
          .Select(c => new ClaimListItemForExportViewModel(c.Claim, currentUserId, c.ProjectInfo))
          .ToList();
    }
}
