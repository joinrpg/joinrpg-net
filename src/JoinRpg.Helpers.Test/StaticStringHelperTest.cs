namespace JoinRpg.Helpers.Test;

public class StaticStringHelperTest
{
    [Fact]
    public void UnprefixId() => "id123".TryUnprefixNumber("id").ShouldBe(123);

    [Fact]
    public void UnprefixIncorrectId() => "idxxx123".TryUnprefixNumber("id").ShouldBeNull();

    [Fact]
    public void UnprefixNoPrefix() => "xidxxx123".TryUnprefixNumber("id").ShouldBeNull();

    [Fact]
    public void RemoveTokensFromString() => "foobarbazzoo"
        .RemoveFromString(["bar", "zoo"])
        .ShouldBe("foobaz");

    [Fact]
    public void RemoveTokenFromString() => "foobarbaz"
        .RemoveFromString("bar")
        .ShouldBe("foobaz");

    [Fact]
    public void KeepOnlyLettersNumbersAndLegals() => "foo123*&^_{}\"bar\"()-baz"
        .KeepOnlyLettersNumbersAndLegalSymbols(['_', '-', '"'])
        .ShouldBe("foo123_\"bar\"-baz");
}
