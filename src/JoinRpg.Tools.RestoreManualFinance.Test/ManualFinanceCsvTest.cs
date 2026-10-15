using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.Services.Drp.ManualFinance;
using JoinRpg.Tools.Drp.Common;
using static JoinRpg.Tools.RestoreManualFinance.Test.PlanFactory;

namespace JoinRpg.Tools.RestoreManualFinance.Test;

public class ManualFinanceCsvTest
{
    private static string Header => string.Join(";", ManualFinanceCsv.Columns);

    [Theory]
    [InlineData("", null, null)]
    [InlineData("  ", null, null)]
    [InlineData("7", 7, null)]
    [InlineData(" 7 ", 7, null)]
    [InlineData("cash:10", null, 10)]
    [InlineData("CASH: 10", null, 10)]
    public void ParsesPaymentTypeChoice(string text, int? paymentTypeId, int? cashFor)
    {
        PaymentTypeChoice.TryParse(text, out var choice).ShouldBeTrue();
        choice?.PaymentTypeId.ShouldBe(paymentTypeId);
        choice?.CashFor?.Value.ShouldBe(cashFor!.Value);
        (choice is null).ShouldBe(paymentTypeId is null && cashFor is null);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-7")]
    [InlineData("cash:")]
    [InlineData("cash:abc")]
    [InlineData("наличные")]
    public void RejectsBadPaymentTypeChoice(string text) => PaymentTypeChoice.TryParse(text, out _).ShouldBeFalse();

    [Fact]
    public void ChoiceFormatsBack()
    {
        PaymentTypeChoice.Existing(7).Format().ShouldBe("7");
        PaymentTypeChoice.Cash(new UserIdentification(10)).Format().ShouldBe("cash:10");
    }

    [Fact]
    public void ReportRoundTripsThroughCsv()
    {
        var plan = Plan(Row(
            [
                Operation("id:500", id: 500, suggestion: Suggested(7)),
                Operation("op:100:1:10", at: At.AddHours(1), suggestion: CreateCash(Master)),
                Operation("id:501", id: 501, status: FinanceRestoreStatus.AlreadyExists),
            ],
            [Moderation("id:502", FinanceModerationLink.ExistingOperation, id: 502, targetId: 400)],
            warnings: [FinanceRestoreWarning.TransfersNotRestored],
            transfers: 2));

        var text = DrpCsv.Format(ReportWriter.Rows([plan], new ApplyResults()));
        var rows = ManualFinanceCsv.Parse(text);

        // Строка заявки пропускается; «Тип оплаты» предзаполнен только у однозначно подобранного типа
        // восстанавливаемой операции — «создать наличные» и уже имеющиеся остаются пустыми.
        rows.Select(r => r.Key).ShouldBe(["id:500", "op:100:1:10", "id:501", "id:502"]);
        rows.ShouldAllBe(r => r.ProjectId == Project);
        rows[0].PaymentType.ShouldBe(PaymentTypeChoice.Existing(7));
        rows.Skip(1).ShouldAllBe(r => r.PaymentType == null);
        rows[0].Line.ShouldBe(3);
    }

    [Fact]
    public void EditedChoicesAreRead()
    {
        var text = $"""
            Ключ;Проект;Тип оплаты;Лишняя
            id:500;1600;cash:10;x
            ;1600;;строка заявки
            op:1:2:3;1600; 8 ;
            """;

        var rows = ManualFinanceCsv.Parse(text);

        rows.Select(r => (r.Key, r.PaymentType)).ShouldBe([
            ("id:500", PaymentTypeChoice.Cash(new UserIdentification(10))),
            ("op:1:2:3", PaymentTypeChoice.Existing(8)),
        ]);
    }

    [Fact]
    public void CollectsAllErrorsAtOnce()
    {
        var text = $"""
            {Header}
            1600;100;id:1;оплата;;;;;;;;;;наличные;;
            abc;100;id:2;оплата;;;;;;;;;;;;
            1600;100;id:3;оплата;;;;;;;;;;7;;
            1600;100;id:3;оплата;;;;;;;;;;7;;
            """;

        var errors = Should.Throw<DrpCsvException>(() => ManualFinanceCsv.Parse(text)).Errors;

        // Повтор ключа (строки 4 и 5) — не ошибка файла: разбирается при сопоставлении с планом.
        errors.Count.ShouldBe(2);
        errors[0].ShouldStartWith("Строка 2:");
        errors[0].ShouldContain("«Тип оплаты»");
        errors[1].ShouldStartWith("Строка 3:");
        errors[1].ShouldContain("Проект");
    }

    [Fact]
    public void RepeatedKeyIsReadNotRejected()
    {
        var text = $"""
            {Header}
            1600;100;id:3;оплата;;;;;;;;;;7;;
            1600;100;id:3;оплата;;;;;;;;;;7;;
            """;

        ManualFinanceCsv.Parse(text).Select(r => r.Line).ShouldBe([2, 3]);
    }

    [Fact]
    public void ReportFlagsRepeatedKey()
    {
        var plan = Plan(Row([Operation("op:dup"), Operation("op:dup")]));

        var rows = ReportWriter.Rows([plan], new ApplyResults()).ToList();

        var flags = ManualFinanceCsv.Columns.ToList().IndexOf(ManualFinanceCsv.Flags);
        rows.Skip(2).ShouldAllBe(r => r[flags] == ApplyBuilder.AmbiguousKey);
    }

    [Fact]
    public void MissingColumnsAreErrors()
    {
        var errors = Should.Throw<DrpCsvException>(() => ManualFinanceCsv.Parse("Проект;Заявка\r\n1600;100\r\n")).Errors;

        errors.ShouldBe(["Нет колонки «Ключ».", "Нет колонки «Тип оплаты»."]);
    }

    [Fact]
    public void EmptyFileIsError() => Should.Throw<DrpCsvException>(() => ManualFinanceCsv.Parse(""));

    [Fact]
    public void ReportHasRowPerClaimOperationAndModerationWithResults()
    {
        var plan = Plan(Row(
            [Operation("id:500", id: 500, money: -300, type: FinanceOperationType.Refund)],
            [Moderation("mod:1", FinanceModerationLink.Ambiguous, approved: false)],
            warnings: [FinanceRestoreWarning.ModerationAmbiguous]));
        var results = new ApplyResults();
        results.Entries[(Project, "id:500")] = new ApplyOutcome("вставлено (id 500)");
        results.Claims[Claim] = new ApplyOutcome("применено");

        var rows = ReportWriter.Rows([plan], results).ToList();

        rows[0].ShouldBe(ManualFinanceCsv.Columns);
        rows.Count.ShouldBe(4);
        string? Cell(int row, string column) => rows[row][ManualFinanceCsv.Columns.ToList().IndexOf(column)];

        Cell(1, ManualFinanceCsv.Kind).ShouldBe(ReportWriter.ClaimKind);
        Cell(1, ManualFinanceCsv.Key).ShouldBe("");
        Cell(1, ManualFinanceCsv.Flags).ShouldBe("решение подходит к нескольким операциям");
        Cell(1, ManualFinanceCsv.Result).ShouldBe("применено");

        Cell(2, ManualFinanceCsv.Kind).ShouldBe(ReportWriter.RefundKind);
        Cell(2, ManualFinanceCsv.Id).ShouldBe("500");
        Cell(2, ManualFinanceCsv.Time).ShouldBe("2026-07-01 10:00:00");
        Cell(2, ManualFinanceCsv.Money).ShouldBe("-300");
        Cell(2, ManualFinanceCsv.MarkedBy).ShouldBe("мастер");
        Cell(2, ManualFinanceCsv.Status).ShouldBe("восстановить");
        Cell(2, ManualFinanceCsv.Suggested).ShouldBe("7 «Наличные мастеру» (владелец — инициатор)");
        Cell(2, ManualFinanceCsv.PaymentType).ShouldBe("7");
        Cell(2, ManualFinanceCsv.Result).ShouldBe("вставлено (id 500)");

        Cell(3, ManualFinanceCsv.Kind).ShouldBe("модерация: отклонена");
        Cell(3, ManualFinanceCsv.Link).ShouldBe("неоднозначно");
        Cell(3, ManualFinanceCsv.PaymentType).ShouldBe("");
    }

    [Fact]
    public void CreateCashIsSuggestedButNotPrefilled()
    {
        var operation = Operation("op:a", suggestion: CreateCash(Master));

        ReportWriter.Prefill(operation).ShouldBeNull();
        ReportWriter.Describe(operation.PaymentType, new Dictionary<int, Data.Interfaces.PaymentTypeForRestore>()).ShouldBe("создать наличные на 10");
    }
}
