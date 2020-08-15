using System.Collections.Generic;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.Exporters;

namespace JoinRpg.Web.Models.Accommodation
{
    public class AccomodationReportExporter : CustomExporter<AccomodationReportListItemViewModel>
    {
        public AccomodationReportExporter(IUriService uriService) : base(uriService)
        {
        }

        public override IEnumerable<ITableColumn> ParseColumns()
        {
            yield return StringColumn(x => x.DisplayName);
            yield return StringColumn(x => x.FullName);
            yield return UriColumn(x => x);
            yield return StringColumn(x => x.AccomodationType);
            yield return StringColumn(x => x.RoomName);
            yield return StringColumn(x => x.Phone);
        }
    }
}
