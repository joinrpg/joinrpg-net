using JoinRpg.DataModel;

namespace JoinRpg.Web.Models.ClaimList;

public class ClaimListForExportViewModel
{
    public IEnumerable<ClaimListItemForExportViewModel> Items { get; }

    public ClaimListForExportViewModel(int currentUserId, IReadOnlyCollection<Claim> claims)
    {
        Items = claims
          .Select(c => new ClaimListItemForExportViewModel(c, currentUserId))
          .ToList();
    }
}
