using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Web.Models.Exporters
{
    public class FinanceOperationExporter : CustomExporter<FinOperationListItemViewModel>
    {
        public FinanceOperationExporter(Project project, IUriService uriService) : base(
            uriService) => Project = project;

        private Project Project { get; }

        public override IEnumerable<ITableColumn> ParseColumns()
        {
            yield return IntColumn(x => x.FinanceOperationId);
            yield return IntColumn(x => x.Money);
            yield return IntColumn(x => x.FeeChange);
            yield return ShortUserColumn(x => x.PaymentMaster);
            yield return StringColumn(x => x.PaymentTypeName);
            yield return ShortUserColumn(x => x.MarkingMaster);
            yield return DateTimeColumn(x => x.OperationDate);
            yield return StringColumn(x => x.Claim);
            yield return UriColumn(x => x);
            foreach (var c in UserColumn(x => x.Player))
            {
                yield return c;
            }
        }
    }
}
