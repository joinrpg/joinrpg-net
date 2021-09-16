using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.ClaimList;

namespace JoinRpg.Web.Models.Exporters
{
    public class ClaimListItemViewModelExporter : CustomExporter<ClaimListItemForExportViewModel>
    {
        public ClaimListItemViewModelExporter(Project project, IUriService uriService) : base(
            uriService) => Project = project;

        private Project Project { get; }

        public override IEnumerable<ITableColumn> ParseColumns()
        {
            yield return StringColumn(x => x.Name);
            yield return UriColumn(x => x);
            yield return EnumColumn(x => x.ClaimFullStatusView.ClaimStatus);
            yield return EnumColumn(x => x.ClaimFullStatusView.ClaimDenialStatus);
            yield return DateTimeColumn(x => x.UpdateDate);
            yield return DateTimeColumn(x => x.CreateDate);
            yield return IntColumn(x => x.TotalFee);
            yield return IntColumn(x => x.FeeDue);
            yield return IntColumn(x => x.FeePaid);
            if (Project.Details.PreferentialFeeEnabled)
            {
                yield return BoolColumn(x => x.PreferentialFeeUser);
            }
            if (Project.Details.EnableAccommodation)
            {
                yield return StringColumn(x => x.AccomodationType);
                yield return StringColumn(x => x.RoomName);
            }

            yield return ShortUserColumn(x => x.LastModifiedBy);
            yield return ShortUserColumn(x => x.Responsible);
            foreach (var c in UserColumn(x => x.Player))
            {
                yield return c;
            }

            foreach (var projectField in Project.GetOrderedFields().Where(f => f.CanHaveValue()))
            {
                yield return
                    FieldColumn(projectField, x => x.Fields);
            }
        }
    }
}
