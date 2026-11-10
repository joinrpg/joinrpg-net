using JoinRpg.Markdown;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Web.Models.Exporters;

/// <summary>
/// Экспорт расходов проекта (ADR030) в Excel/CSV: колонки повторяют грид страницы «Расходы»,
/// план и факт — отдельными колонками (количество и цена), как в форме расхода.
/// </summary>
public class ExpenseExporter(IUriService uriService) : CustomExporter<ExpenseExportItem>(uriService)
{
    public override IEnumerable<ITableColumn> ParseColumns()
    {
        yield return IntColumn(x => x.Id);
        yield return StringColumn(x => x.CategoryPath);
        yield return StringColumn(x => x.Name);
        yield return StringColumn(x => x.MasterName);
        yield return IntColumn(x => x.PlannedQuantity);
        yield return IntColumn(x => x.PlannedPrice);
        yield return NullableIntColumn(x => x.ActualQuantity);
        yield return NullableIntColumn(x => x.ActualPrice);
        yield return StringColumn(x => x.State);
        yield return DateOnlyColumn(x => x.SpentAt);
        yield return ComplexElementMemberColumn(x => x.Description, (MarkdownString description) => description.ToPlainTextWithoutHtmlEscape(), "Описание");
    }
}

/// <summary>Строка выгрузки расхода: плоская DTO — то, что видно в гриде, плюс id и описание.</summary>
public record ExpenseExportItem(
    [property: Display(Name = "ID")] int Id,
    [property: Display(Name = "Категория")] string CategoryPath,
    [property: Display(Name = "Название")] string Name,
    [property: Display(Name = "Мастер")] string MasterName,
    [property: Display(Name = "Количество (план)")] int PlannedQuantity,
    [property: Display(Name = "Цена (план)")] int PlannedPrice,
    [property: Display(Name = "Количество (факт)")] int? ActualQuantity,
    [property: Display(Name = "Цена (факт)")] int? ActualPrice,
    [property: Display(Name = "Состояние")] string State,
    [property: Display(Name = "Дата траты")] DateOnly? SpentAt,
    [property: Display(Name = "Описание")] MarkdownString? Description);
