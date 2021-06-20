using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Domain;

namespace JoinRpg.Web.Models.ClaimList
{
    public class ClaimListForExportViewModel
    {
        public IEnumerable<ClaimListItemForExportViewModel> Items { get; }

        public ClaimListForExportViewModel(int currentUserId, IReadOnlyCollection<Claim> claims)
        {
            Items = claims
              .Select(c => new ClaimListItemForExportViewModel(c, currentUserId).AddProblems(c.GetProblems()))
              .ToList();
        }
    }
}
