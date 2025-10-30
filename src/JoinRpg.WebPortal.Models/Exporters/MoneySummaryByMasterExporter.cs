using JoinRpg.Services.Interfaces;

namespace JoinRpg.Web.Models.Exporters;

public class MoneySummaryByMasterExporter(IUriService uriService) : CustomExporter<MasterBalanceViewModel>(uriService)
{
    public override IEnumerable<ITableColumn> ParseColumns()
    {
        // Порядок полей тут важен, так как гуглдоки у людей опираются скорее всего на порядок колонок
        yield return ShortUserColumn(x => x.Master, "Мастер");
        yield return ComplexElementMemberColumn(x => x.Master, x => x.Email);
        yield return ComplexElementMemberColumn(x => x.Master,
            x => x.Extra,
            x => x.PhoneNumber,
            "Телефон");
        yield return ComplexElementMemberColumn(x => x.Master, x => x.Extra, x => x.Telegram, "Telegram");
        // Выше должно быть ровно 4 поля с какими-то данными профиля мастера
        yield return IntColumn(x => x.Total);
        yield return IntColumn(x => x.ReceiveBalance);
        yield return IntColumn(x => x.SendBalance);
        yield return IntColumn(x => x.FeeBalance);
        yield return IntColumn(x => x.ModerationBalance);
        yield return IntColumn(x => x.ExpensesBalance);
        // выше должно быть 6 полей с цифирками ровно в таком порядке
    }
}
