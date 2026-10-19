using JoinRpg.Tools.Drp.Common;

namespace JoinRpg.Tools.RestoreProjectShells.Test;

public class ReportWriterTest
{
    [Fact]
    public void ReportHasDecisionAndResult()
    {
        var rows = ReportWriter.Rows(
            [new ShellDecision(1600, ShellDecisionKind.Create, "=Игра", 77)],
            new Dictionary<int, string> { [1600] = "создан" }).ToList();

        rows[0].ShouldBe(ReportWriter.Header);
        rows[1].ShouldBe(["1600", "Create", "будет создан", "создан", "'=Игра", "77", null]);
    }

    [Fact]
    public void ReportRoundTripsThroughCsv()
    {
        var text = DrpCsv.Format(ReportWriter.Rows(
            [new ShellDecision(1600, ShellDecisionKind.AlreadyExists, "Игра; вторая", 77, "Игра; вторая")],
            new Dictionary<int, string>()));

        DrpCsv.Parse(text)[1].Values.ShouldBe(["1600", "AlreadyExists", "уже создан", "", "Игра; вторая", "77", "Игра; вторая"]);
    }
}
