using System.Text;
using JoinRpg.Services.Export.BackEnds;
using JoinRpg.Services.Export.Internal;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Export.Test;

/// <summary>
/// CSV-файл начинается с преамбулы UTF-8 (EF BB BF), и дальше идёт сам текст.
/// </summary>
public class CsvBackendTests
{
    private static readonly byte[] Utf8Preamble = [0xEF, 0xBB, 0xBF];

    [Fact]
    public void Csv_StartsWithUtf8Preamble()
    {
        var backend = new CsvBackend();
        backend.WriteHeaders([new TestColumn("Назначение"), new TestColumn("Сумма")]);
        backend.WriteRow([new Cell("Зимняя игра"), new Cell(1500)]);
        backend.WriteRow([new Cell("Летняя игра"), new Cell(2000)]);

        var bytes = backend.Generate();

        bytes.Take(3).ShouldBe(Utf8Preamble);
        // Точное сравнение текста заодно доказывает, что преамбула написана один раз на файл,
        // а не повторяется перед каждой строкой: лишняя попала бы в текст как \uFEFF.
        DecodeText(bytes).ShouldBe(
            Line("Назначение", "Сумма")
            + Line("Зимняя игра", "1500")
            + Line("Летняя игра", "2000"));
    }

    /// <summary>
    /// Пустая выгрузка — только заголовок — тоже должна открываться в Excel как UTF-8.
    /// </summary>
    [Fact]
    public void EmptyCsv_StillStartsWithUtf8Preamble()
    {
        var backend = new CsvBackend();
        backend.WriteHeaders([new TestColumn("Назначение")]);

        var bytes = backend.Generate();

        bytes.Take(3).ShouldBe(Utf8Preamble);
        DecodeText(bytes).ShouldBe(Line("Назначение"));
    }

    /// <summary>
    /// Контроллеры отдают результат <see cref="TableGenerator{TRow}"/>, поэтому преамбула
    /// проверяется и на всём пути генерации, а не только на бэкенде.
    /// </summary>
    [Fact]
    public void TableGenerator_StartsWithUtf8Preamble()
    {
        var generator = new TableGenerator<TestRow>(
            [new TestRow("Зимняя игра", 1500)],
            new CsvBackend(),
            new TestFrontend(new TestColumn("Назначение", r => r.Purpose), new TestColumn("Сумма", r => r.Amount)));

        var bytes = generator.Generate();

        bytes.Take(3).ShouldBe(Utf8Preamble);
        DecodeText(bytes).ShouldBe(Line("Назначение", "Сумма") + Line("Зимняя игра", "1500"));
    }

    private static string DecodeText(byte[] bytes) => Encoding.UTF8.GetString(bytes, Utf8Preamble.Length, bytes.Length - Utf8Preamble.Length);

    /// <summary>
    /// Бэкенд кавычит каждое непустое значение, числа в том числе, и заканчивает строку
    /// переводом строки текущей платформы.
    /// </summary>
    private static string Line(params string[] cells)
        => string.Join(",", cells.Select(c => $"\"{c}\"")) + Environment.NewLine;

    private sealed record TestRow(string Purpose, int Amount);

    private sealed class TestFrontend(params ITableColumn[] columns) : IGeneratorFrontend<TestRow>
    {
        public IEnumerable<ITableColumn> ParseColumns() => columns;
    }

    private sealed class TestColumn(string? name, Func<TestRow, object?>? extractValue = null) : ITableColumn
    {
        public string? Name => name;

        public object? ExtractValue(object row)
            => extractValue is null
                ? throw new InvalidOperationException($"Колонка «{name}» не читает значение строки")
                : extractValue((TestRow)row);
    }
}
