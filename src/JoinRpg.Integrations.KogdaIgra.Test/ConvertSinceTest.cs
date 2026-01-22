namespace JoinRpg.Integrations.KogdaIgra.Test;

public class ConvertSinceTest
{
    [Fact]
    public void EnsureCorrectConversion()
    {
        var time = new DateTimeOffset(2025, 10, 28, 19, 58, 33, TimeSpan.FromHours(3));

        KogdaIgraApiClient.ConvertToKogdaIgraTimeStamp(time).ShouldBe(1761670713);
    }
}
