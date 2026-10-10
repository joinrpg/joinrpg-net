using System.Text.Json;

namespace JoinRpg.Tools.RestoreLostProjects.Test;

public class LogsSourceTest
{
    private static readonly DateTimeOffset LostAt = new(2026, 10, 7, 22, 30, 49, TimeSpan.Zero);

    private static LogEntry? Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return LogsSource.Parse(document.RootElement);
    }

    [Fact]
    public void ParsesYcRecordWithJsonPayload()
    {
        var entry = Parse("""
            {
              "timestamp": "2026-10-01T10:00:00.123Z",
              "json_payload": {
                "ProjectId": 1535,
                "LoggedUser": "master@example.com",
                "RequestPath": "/1535/character/14311/edit",
                "ActionName": "JoinRpg.Portal.Controllers.CharacterController.Edit (JoinRpg.Portal)"
              }
            }
            """);

        entry.ShouldBe(new LogEntry(
            1535,
            "master@example.com",
            "/1535/character/14311/edit",
            "JoinRpg.Portal.Controllers.CharacterController.Edit (JoinRpg.Portal)",
            new DateTimeOffset(2026, 10, 1, 10, 0, 0, 123, TimeSpan.Zero)));
    }

    [Fact]
    public void ParsesFlatRecordWithSerilogTimestamp()
    {
        var entry = Parse("""{"@timestamp": "2026-10-01T13:00:00+03:00", "ProjectId": "1535", "RequestPath": "/1535/claim/1"}""");

        entry.ShouldNotBeNull();
        entry.ProjectId.ShouldBe(1535);
        entry.Timestamp.ShouldBe(new DateTimeOffset(2026, 10, 1, 10, 0, 0, TimeSpan.Zero));
    }

    [Theory]
    [InlineData("1535", 1535)]
    [InlineData("\"1535\"", 1535)]
    [InlineData("\"Project(158)\"", 158)]
    [InlineData("\"ProjectId(158)\"", 158)]
    [InlineData("\"abc\"", null)]
    [InlineData("0", null)]
    [InlineData("null", null)]
    public void ParsesProjectId(string json, int? expected)
    {
        using var document = JsonDocument.Parse(json);
        LogsSource.ParseProjectId(document.RootElement).ShouldBe(expected);
    }

    [Fact]
    public void RecordWithoutProjectAndPathIsSkipped()
        => Parse("""{"json_payload": {"LoggedUser": "a@example.com"}}""").ShouldBeNull();

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReadsArrayAndLinesSkippingAfterLoss(bool asArray)
    {
        string[] records =
        [
            """{"timestamp": "2026-10-01T10:00:00Z", "json_payload": {"ProjectId": 1001}}""",
            """{"timestamp": "2026-10-08T10:00:00Z", "json_payload": {"ProjectId": 1002}}""",
            """{"json_payload": {"RequestPath": "/1003/claim/1"}}""",
        ];
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, asArray ? "[\n" + string.Join(",\n", records) + "\n]" : string.Join("\n", records) + "\n",
                new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

            var entries = await LogsSource.Read([path], LostAt, CancellationToken.None);

            entries.Select(e => e.ProjectId ?? LostProjectAnalyzer.TryGetProjectIdFromPath(e.RequestPath)).ShouldBe([1001, 1003]);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
