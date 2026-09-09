namespace JoinRpg.Domain.Test;

public class VkBirthDateParserTest
{
    [Theory]
    [InlineData("15.06.1990", true, "1990-06-15")]
    [InlineData("15.06", false, null)]
    [InlineData("", false, null)]
    [InlineData(null, false, null)]
    [InlineData("not a date", false, null)]
    public void TryParse(string? raw, bool expectedSuccess, string? expectedDate)
    {
        var success = VkBirthDateParser.TryParse(raw, out var birthDate);

        success.ShouldBe(expectedSuccess);
        if (expectedSuccess)
        {
            birthDate.ShouldBe(DateOnly.Parse(expectedDate!));
        }
    }
}
