namespace JoinRpg.Common.PrimitiveTypes.Test;

public class SendingResultTest
{
    [Fact]
    public void Skipped_SetsSkippedStateAndReason()
    {
        var result = SendingResult.Skipped("Telegram не настроен");

        result.IsSkipped.ShouldBeTrue();
        result.Succeeded.ShouldBeFalse();
        result.Message.ShouldBe("Telegram не настроен");
    }

    [Fact]
    public void Skipped_WithoutReason_HasNullMessage()
    {
        var result = SendingResult.Skipped();

        result.IsSkipped.ShouldBeTrue();
        result.Message.ShouldBeNull();
    }

    [Fact]
    public void BuiltInResults_AreNotSkippedAndHaveNoMessage()
    {
        foreach (var result in new[]
        {
            SendingResult.Success(),
            SendingResult.RepeatableFailure(),
            SendingResult.CommonFailure(),
            SendingResult.PermanentUserFailure(),
        })
        {
            result.IsSkipped.ShouldBeFalse();
            result.Message.ShouldBeNull();
        }
    }

    [Fact]
    public void FailureResult_CanCarryMessage()
    {
        var result = SendingResult.PermanentUserFailure() with { Message = "bot was kicked from the channel" };

        result.Message.ShouldBe("bot was kicked from the channel");
        result.IsSkipped.ShouldBeFalse();
        result.Succeeded.ShouldBeFalse();
    }

    [Fact]
    public void Skipped_IsDistinguishableFromPermanentUserFailure()
    {
        // Попытки не было вовсе — не то же самое, что «писали и упали с постоянной ошибкой».
        SendingResult.Skipped().ShouldNotBe(SendingResult.PermanentUserFailure());
        SendingResult.Skipped("причина А").ShouldNotBe(SendingResult.Skipped("причина Б"));
    }
}
