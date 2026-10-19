namespace JoinRpg.Services.Drp.MasterExports;

/// <summary>Ячейка как она лежит в файле, до разбора по смыслу колонки.</summary>
/// <param name="Text">Текст ячейки; у обрезанной — без пометки «(обрезано) … ...».</param>
/// <param name="Date">Нативная дата xlsx.</param>
/// <param name="Number">Нативное число xlsx.</param>
/// <param name="Hyperlink">Адрес гиперссылки xlsx-ячейки, если она есть.</param>
internal record RawCell(string Text, bool Truncated = false, DateTime? Date = null, double? Number = null, string? Hyperlink = null)
{
    public static RawCell Empty { get; } = new("");

    public bool IsEmpty => string.IsNullOrWhiteSpace(Text) && Date is null && Number is null;
}

/// <param name="RowNumber">Номер строки в файле, строка заголовков — первая.</param>
internal record RawRow(int RowNumber, IReadOnlyList<RawCell> Cells)
{
    public RawCell this[int column] => column < Cells.Count ? Cells[column] : RawCell.Empty;

    public bool IsEmpty => Cells.All(c => c.IsEmpty);
}

/// <summary>Таблица выгрузки: заголовки и строки. Пустые строки уже выброшены.</summary>
internal record RawTable(
    IReadOnlyList<string> Headers,
    IReadOnlyList<RawRow> Rows,
    DateTimeOffset? SuggestedDate);
