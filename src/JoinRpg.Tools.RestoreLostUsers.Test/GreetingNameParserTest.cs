namespace JoinRpg.Tools.RestoreLostUsers.Test;

public class GreetingNameParserTest
{
    private static readonly DateTimeOffset Early = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Late = new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("Добрый день, Иван!", "Иван")]
    [InlineData("Добрый день, Иван", "Иван")] // шаблон заявок — без восклицательного знака
    [InlineData("Добрый день, #Скракан ", "#Скракан")]
    [InlineData("Добрый день, Мария Ивановна Петрова!", "Мария Ивановна Петрова")]
    public void ExtractsName(string line, string expected) => GreetingNameParser.TryExtract(line).ShouldBe(expected);

    [Theory]
    [InlineData("Привет, Иван!")]
    [InlineData("Добрый день, !")]
    [InlineData("Добрый день, %recepient.name%!")]
    [InlineData("Добрый день, Иван %recepient.claim%!")]
    [InlineData("")]
    public void NotAGreeting(string line) => GreetingNameParser.TryExtract(line).ShouldBeNull();

    [Fact]
    public void TooLongNameIsRejected()
    {
        GreetingNameParser.TryExtract(GreetingNameParser.GreetingPrefix + new string('я', 100) + "!").ShouldNotBeNull();
        GreetingNameParser.TryExtract(GreetingNameParser.GreetingPrefix + new string('я', 101) + "!").ShouldBeNull();
    }

    [Fact]
    public void RegistrationPlaceholderIsNotAName()
        => GreetingNameParser.ChooseName([new GreetingLine("Добрый день, user1234!", 1, Late)], 1234, "a@example.com").ShouldBeNull();

    [Fact]
    public void EmailUserPartIsNotAName()
        => GreetingNameParser.ChooseName([new GreetingLine("Добрый день, Ivan.Petrov!", 1, Late)], 1, "ivan.petrov@example.com").ShouldBeNull();

    [Fact]
    public void MostFrequentNameWins()
        => GreetingNameParser.ChooseName(
            [
                new GreetingLine("Добрый день, Иван!", 5, Early),
                new GreetingLine("Добрый день, коллеги!", 1, Late),
            ],
            1,
            "a@example.com").ShouldBe("Иван");

    [Fact]
    public void SameNameWithAndWithoutExclamationIsCountedTogether()
        => GreetingNameParser.ChooseName(
            [
                new GreetingLine("Добрый день, Иван!", 2, Early),
                new GreetingLine("Добрый день, Иван", 2, Early),
                new GreetingLine("Добрый день, Пётр!", 3, Late),
            ],
            1,
            "a@example.com").ShouldBe("Иван");

    [Fact]
    public void OnTieLatestWins()
        => GreetingNameParser.ChooseName(
            [
                new GreetingLine("Добрый день, Иван!", 2, Early),
                new GreetingLine("Добрый день, Пётр!", 2, Late),
            ],
            1,
            "a@example.com").ShouldBe("Пётр");
}
