using System.Collections.Generic;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.ClaimList;

namespace JoinRpg.Web.Models.Exporters
{
    public class MyClaimListItemViewModelExporter : CustomExporter<ClaimListItemForExportViewModel>
    {
        public MyClaimListItemViewModelExporter(IUriService uriService) : base(
            uriService)
        { }

        public override IEnumerable<ITableColumn> ParseColumns()
        {
            yield return StringColumn(x => x.Name);
            yield return UriColumn(x => x);
            yield return EnumColumn(x => x.ClaimFullStatusView.ClaimStatus);
            yield return DateTimeColumn(x => x.UpdateDate);
            yield return DateTimeColumn(x => x.CreateDate);
            yield return IntColumn(x => x.TotalFee);
            yield return IntColumn(x => x.FeeDue);
            yield return IntColumn(x => x.FeePaid);
            yield return BoolColumn(x => x.PreferentialFeeUser);
            yield return StringColumn(x => x.AccomodationType);
            yield return StringColumn(x => x.RoomName);

            yield return ShortUserColumn(x => x.LastModifiedBy);
            yield return ShortUserColumn(x => x.Responsible);
        }
    }
}
