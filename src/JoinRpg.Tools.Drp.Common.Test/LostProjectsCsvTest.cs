using System.Text;

namespace JoinRpg.Tools.Drp.Common.Test;

public class LostProjectsCsvTest
{
    private const string Header = "ProjectId;Создавать;Название;ВариантыНазвания;СоздательId;Пометки";

    [Fact]
    public void ColumnNamesAreTheContract()
        => LostProjectsCsv.Columns.ShouldBe(
        [
            "ProjectId", "Создавать", "Название", "ВариантыНазвания", "СоздательId", "СоздательEmail", "СпособОпределения",
            "ДругиеКандидаты", "ПерваяАктивность", "ПоследняяАктивность", "Уведомлений", "ЗапросовВЛогах",
            "ПользователиВЛогах", "СоздательВЛогах", "КогдаИгра", "НазваниеВMssql", "Пометки",
        ]);

    [Fact]
    public void ReadsHumanEditedRows()
    {
        // Человек поменял решение, поправил название и создателя; Excel мог убрать кавычки и поменять регистр.
        var rows = LostProjectsCsv.Parse(
            Header + "\r\n"
            + "1001;Да;Дюна: Пробуждение;варианты;2001;\r\n"
            + "\r\n"
            + "1002; нет ;\"Арракис; \"\"пустыня\"\"\";;;только по логам\r\n"
            + "1003;да;'=формула;;2003;");

        rows.ShouldBe(
        [
            new LostProjectsCsvRow(2, 1001, true, "Дюна: Пробуждение", 2001),
            new LostProjectsCsvRow(4, 1002, false, "Арракис; \"пустыня\"", null),
            new LostProjectsCsvRow(5, 1003, true, "=формула", 2003),
        ]);
    }

    [Fact]
    public void ColumnsAreFoundByName()
    {
        var rows = LostProjectsCsv.Parse("СоздательId;Название;Создавать;ProjectId\n2001;Дюна;да;1001\n");

        rows.ShouldHaveSingleItem().ShouldBe(new LostProjectsCsvRow(2, 1001, true, "Дюна", 2001));
    }

    [Fact]
    public void MultilineValueKeepsLineNumbersOfNextRows()
    {
        var rows = LostProjectsCsv.Parse(Header + "\n1001;да;\"две\nстроки\";;2001;\n1002;нет;x;;;\n");

        rows.Select(r => r.Line).ShouldBe([2, 4]);
        rows[0].Name.ShouldBe("две\nстроки");
    }

    [Fact]
    public void ReportsAllErrorsAtOnce()
    {
        var exception = Should.Throw<DrpCsvException>(() => LostProjectsCsv.Parse(
            Header + "\n"
            + "abc;да;Дюна;;2001;\n"
            + "1002;может быть;Дюна;;2002;\n"
            + "1003;да;Дюна;;мастер;\n"));

        exception.Errors.Count.ShouldBe(3);
        exception.Errors[0].ShouldStartWith("Строка 2:");
        exception.Errors[1].ShouldContain("Создавать");
        exception.Errors[2].ShouldContain("СоздательId");
    }

    [Fact]
    public void MissingColumnIsError()
        => Should.Throw<DrpCsvException>(() => LostProjectsCsv.Parse("ProjectId;Название;СоздательId\n1001;Дюна;2001\n"))
            .Errors.ShouldHaveSingleItem().ShouldContain("Создавать");

    [Fact]
    public void UnclosedQuoteIsError()
        => Should.Throw<DrpCsvException>(() => DrpCsv.Parse("a;\"b\nc"));

    [Fact]
    public async Task FileNotInUtf8IsError()
    {
        var path = Path.GetTempFileName();
        try
        {
            // Excel «CSV (разделители — точка с запятой)» сохраняет в cp1251.
            await File.WriteAllBytesAsync(path, [.. "ProjectId;"u8, 0xD1, 0xEE, 0xE7, 0xE4]);

            var exception = await Should.ThrowAsync<DrpCsvException>(() => LostProjectsCsv.Read(path, CancellationToken.None));

            exception.Errors.ShouldHaveSingleItem().ShouldContain("UTF-8");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Utf8WithoutBomIsAccepted()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, Header + "\n1001;да;Дюна;;2001;\n", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            (await LostProjectsCsv.Read(path, CancellationToken.None)).ShouldHaveSingleItem().Name.ShouldBe("Дюна");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData("=1+1")]
    [InlineData("-5")]
    [InlineData("@user")]
    [InlineData("'обычный апостроф")]
    [InlineData("Дюна")]
    public void UserTextRoundTrips(string value) => DrpCsv.FromUserText(DrpCsv.UserText(value)).ShouldBe(value);
}
