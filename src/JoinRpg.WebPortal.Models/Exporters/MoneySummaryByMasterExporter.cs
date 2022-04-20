using JoinRpg.Services.Interfaces;

namespace JoinRpg.Web.Models.Exporters
{
    public class MoneySummaryByMasterExporter : CustomExporter<MasterBalanceViewModel>
    {
        public MoneySummaryByMasterExporter(IUriService uriService) : base(uriService)
        {
        }

        public override IEnumerable<ITableColumn> ParseColumns()
        {
            yield return ShortUserColumn(x => x.Master, "Мастер");
            yield return ComplexElementMemberColumn(x => x.Master, x => x.Email);
            yield return ComplexElementMemberColumn(x => x.Master,
                x => x.Extra,
                x => x.PhoneNumber,
                "Телефон");
            yield return ComplexElementMemberColumn(x => x.Master,
                x => x.Extra,
                x => x.Skype,
                "Skype");
            yield return IntColumn(x => x.Total);
            yield return IntColumn(x => x.ReceiveBalance);
            yield return IntColumn(x => x.SendBalance);
            yield return IntColumn(x => x.FeeBalance);
            yield return IntColumn(x => x.ModerationBalance);
            yield return IntColumn(x => x.ExpensesBalance);
        }
    }
}
