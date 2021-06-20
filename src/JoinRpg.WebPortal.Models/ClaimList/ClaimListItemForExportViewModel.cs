using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Domain;

namespace JoinRpg.Web.Models.ClaimList
{
    public class ClaimListItemForExportViewModel : ClaimListItemViewModel
    {
        public IReadOnlyCollection<FieldWithValue> Fields { get; }

        public ClaimListItemForExportViewModel(Claim claim, int currentUserId) : base(claim, currentUserId)
        {
            Fields = claim.GetFields();
        }

        public new ClaimListItemForExportViewModel AddProblems(IEnumerable<ClaimProblem> problem)
        {
            Problems =
                problem.Select(p => new ProblemViewModel(p)).ToList();
            return this;
        }
    }
}
