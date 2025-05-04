namespace JoinRpg.Helpers.Test;
public class StaticStringHelperTest
{
    [Fact]
    public void UnprefixId() => "id123".TryUnprefixNumber("id").ShouldBe(123);

    [Fact]
    public void UnprefixIncorrectId() => "idxxx123".TryUnprefixNumber("id").ShouldBeNull();

    [Fact]
    public void UnprefixNoPrefix() => "xidxxx123".TryUnprefixNumber("id").ShouldBeNull();
}
