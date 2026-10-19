using System.Text.Json;
using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.DomainTypes.Test.Characters;

public class UserLinkFieldRewriterTest
{
    private static readonly UserIdentification From = new(5);
    private static readonly UserIdentification To = new(7);
    private static readonly HashSet<int> UserFields = [10, 11];

    [Fact]
    public void SingleValue_Replaced()
        => Rewrite(new() { [10] = "5" }).ShouldBe(new() { [10] = "7" });

    [Fact]
    public void MultiValue_ReplacedInPlace()
        => Rewrite(new() { [11] = "3,5,9" }).ShouldBe(new() { [11] = "3,7,9" });

    [Fact]
    public void MultiValue_BothUsers_Deduplicated()
        => Rewrite(new() { [11] = "7, 5" }).ShouldBe(new() { [11] = "7" });

    [Fact]
    public void OtherFieldsWithSameNumber_Untouched()
        => Rewrite(new() { [10] = "5", [20] = "5" }).ShouldBe(new() { [10] = "7", [20] = "5" });

    [Fact]
    public void IdThatOnlyContainsDigits_Untouched()
        => UserLinkFieldRewriter.Replace(Json(new() { [11] = "15,55" }), UserFields, From, To).ShouldBeNull();

    [Fact]
    public void OtherValues_PreservedOnRewrite()
        => Rewrite(new() { [10] = "5", [20] = "Кириллица \"в кавычках\"", [21] = null })
            .ShouldBe(new() { [10] = "7", [20] = "Кириллица \"в кавычках\"", [21] = null });

    [Fact]
    public void NonCanonicalValue_Replaced()
        => Rewrite(new() { [10] = " 05 " }).ShouldBe(new() { [10] = "7" });

    [Fact]
    public void FieldKeyEqualToUserId_Untouched()
        => UserLinkFieldRewriter.Replace(Json(new() { [5] = "3" }), new HashSet<int> { 5 }, From, To).ShouldBeNull();

    [Fact]
    public void NothingToReplace_Null()
    {
        UserLinkFieldRewriter.Replace(Json(new() { [10] = "3", [11] = null }), UserFields, From, To).ShouldBeNull();
        UserLinkFieldRewriter.Replace(null, UserFields, From, To).ShouldBeNull();
        UserLinkFieldRewriter.Replace("", UserFields, From, To).ShouldBeNull();
    }

    private static Dictionary<int, string?> Rewrite(Dictionary<int, string?> values)
    {
        var result = UserLinkFieldRewriter.Replace(Json(values), UserFields, From, To).ShouldNotBeNull();
        return JsonSerializer.Deserialize<Dictionary<int, string?>>(result)!;
    }

    private static string Json(Dictionary<int, string?> values) => JsonSerializer.Serialize(values);
}
