using ClosedXML.Excel;
using JoinRpg.Services.Export.BackEnds;
using JoinRpg.Services.Export.Internal;

namespace JoinRpg.Services.Export.Test;

public class ClosedXmlExcelBackendTests
{
    private static string ReadFirstCellValue(byte[] excelBytes)
    {
        using var stream = new MemoryStream(excelBytes);
        using var workbook = new XLWorkbook(stream);
        return workbook.Worksheets.First().Cell(2, 1).GetString();
    }

    private static byte[] GenerateExcelWithValue(string value)
    {
        var backend = new ClosedXmlExcelBackend();
        backend.WriteRow([new Cell("Заголовок")]);
        backend.WriteRow([new Cell(value)]);
        return backend.Generate();
    }

    [Fact]
    public void ShortString_IsNotTruncated()
    {
        var value = new string('а', 100);
        var bytes = GenerateExcelWithValue(value);
        var result = ReadFirstCellValue(bytes);
        result.ShouldBe(value);
    }

    [Fact]
    public void StringAtExactLimit_IsNotTruncated()
    {
        var value = new string('а', ClosedXmlExcelBackend.ExcelMaxCellLength);
        var bytes = GenerateExcelWithValue(value);
        var result = ReadFirstCellValue(bytes);
        result.ShouldBe(value);
    }

    [Fact]
    public void StringOverLimit_IsTruncatedToExactLimit()
    {
        var value = new string('а', ClosedXmlExcelBackend.ExcelMaxCellLength + 1000);
        var bytes = GenerateExcelWithValue(value);
        var result = ReadFirstCellValue(bytes);
        result.Length.ShouldBe(ClosedXmlExcelBackend.ExcelMaxCellLength);
        result.ShouldStartWith("(обрезано)");
        result.ShouldEndWith("...");
    }

    [Fact]
    public void DateOnly_IsWrittenAsExcelDateWithoutTime()
    {
        var backend = new ClosedXmlExcelBackend();
        backend.WriteRow([new Cell("Заголовок")]);
        backend.WriteRow([new Cell(new DateOnly(2000, 2, 1))]);

        using var stream = new MemoryStream(backend.Generate());
        using var workbook = new XLWorkbook(stream);
        var cell = workbook.Worksheets.First().Cell(2, 1);
        cell.DataType.ShouldBe(XLDataType.DateTime);
        cell.GetDateTime().ShouldBe(new DateTime(2000, 2, 1));
        cell.GetFormattedString().ShouldBe("01.02.2000");
    }

    [Fact]
    public void ExcelFileWithLongValue_CanBeCreatedWithoutException()
    {
        var value = new string('х', 100_000);
        Should.NotThrow(() => GenerateExcelWithValue(value));
    }
}
