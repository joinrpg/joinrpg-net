using System.Collections.Generic;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Web.Models.Exporters
{
  public class MoneySummaryByMasterExporter : CustomExporter<MoneySummaryByMasterListItemViewModel>
  {
    public MoneySummaryByMasterExporter(IUriService uriService) : base(uriService)
    {
    }

    public override IEnumerable<ITableColumn> ParseColumns()
    {
      yield return ShortUserColumn(x => x.Master, "Мастер");
      yield return ComplexElementMemberColumn(x => x.Master, x => x.Email);
      yield return ComplexElementMemberColumn(x => x.Master, x => x.Extra, x => x.PhoneNumber, "Телефон");
      yield return ComplexElementMemberColumn(x => x.Master, x => x.Extra, x => x.Skype, "Skype");
      yield return IntColumn(x => x.Total, "Общая сумма взносов");
    }
  }
}