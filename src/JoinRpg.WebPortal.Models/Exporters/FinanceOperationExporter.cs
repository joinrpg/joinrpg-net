using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Web.Models.Exporters;

public class FinanceOperationExporter(IUriService uriService, ProjectInfo projectInfo) : CustomExporter<FinOperationListItemViewModel>(uriService)
{
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
        foreach (var c in UserColumn(x => x.Player, projectInfo))
        {
            yield return c;
        }
    }
}
