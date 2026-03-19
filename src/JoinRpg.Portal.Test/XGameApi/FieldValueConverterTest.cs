using System.Text.Json;
using JoinRpg.Portal.Controllers.XGameApi;

namespace JoinRpg.Portal.Test.XGameApi;

public class FieldValueConverterTest
{
    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;

    [Fact]
    public void StringPassthrough()
    {
        var input = new Dictionary<int, JsonElement> { [1] = Parse("\"hello\"") };
        var result = FieldValueConverter.ConvertToStringValues(input);
        result[1].ShouldBe("hello");
    }

    [Fact]
    public void IntegerToString()
    {
        var input = new Dictionary<int, JsonElement> { [1] = Parse("42") };
        var result = FieldValueConverter.ConvertToStringValues(input);
        result[1].ShouldBe("42");
    }

    [Fact]
    public void NullToNull()
    {
        var input = new Dictionary<int, JsonElement> { [1] = Parse("null") };
        var result = FieldValueConverter.ConvertToStringValues(input);
        result[1].ShouldBeNull();
    }

    [Fact]
    public void FloatToExactString()
    {
        var input = new Dictionary<int, JsonElement> { [1] = Parse("3.14") };
        var result = FieldValueConverter.ConvertToStringValues(input);
        result[1].ShouldBe("3.14");
    }

    [Fact]
    public void LargeInteger()
    {
        var input = new Dictionary<int, JsonElement> { [1] = Parse("9999999999999") };
        var result = FieldValueConverter.ConvertToStringValues(input);
        result[1].ShouldBe("9999999999999");
    }

    [Fact]
    public void BooleanTrue()
    {
        var input = new Dictionary<int, JsonElement> { [1] = Parse("true") };
        var result = FieldValueConverter.ConvertToStringValues(input);
        result[1].ShouldBe("true");
    }

    [Fact]
    public void BooleanFalse()
    {
        var input = new Dictionary<int, JsonElement> { [1] = Parse("false") };
        var result = FieldValueConverter.ConvertToStringValues(input);
        result[1].ShouldBe("false");
    }

    [Fact]
    public void ArrayOfInts()
    {
        var input = new Dictionary<int, JsonElement> { [1] = Parse("[1,2,3]") };
        var result = FieldValueConverter.ConvertToStringValues(input);
        result[1].ShouldBe("1,2,3");
    }

    [Fact]
    public void ArrayOfStrings()
    {
        var input = new Dictionary<int, JsonElement> { [1] = Parse("[\"a\",\"b\"]") };
        var result = FieldValueConverter.ConvertToStringValues(input);
        result[1].ShouldBe("a,b");
    }

    [Fact]
    public void EmptyArray()
    {
        var input = new Dictionary<int, JsonElement> { [1] = Parse("[]") };
        var result = FieldValueConverter.ConvertToStringValues(input);
        result[1].ShouldBe("");
    }

    [Fact]
    public void MixedTypesInDict()
    {
        var input = new Dictionary<int, JsonElement>
        {
            [1] = Parse("\"text\""),
            [2] = Parse("42"),
            [3] = Parse("null"),
            [4] = Parse("[1,2]"),
        };
        var result = FieldValueConverter.ConvertToStringValues(input);
        result[1].ShouldBe("text");
        result[2].ShouldBe("42");
        result[3].ShouldBeNull();
        result[4].ShouldBe("1,2");
    }

    [Fact]
    public void ObjectValueThrows()
    {
        var input = new Dictionary<int, JsonElement> { [1] = Parse("{\"a\":1}") };
        Should.Throw<ArgumentException>(() => FieldValueConverter.ConvertToStringValues(input));
    }

    [Fact]
    public void ArrayWithObjectThrows()
    {
        var input = new Dictionary<int, JsonElement> { [1] = Parse("[{\"a\":1}]") };
        Should.Throw<ArgumentException>(() => FieldValueConverter.ConvertToStringValues(input));
    }

    [Fact]
    public void EmptyDictionary()
    {
        var input = new Dictionary<int, JsonElement>();
        var result = FieldValueConverter.ConvertToStringValues(input);
        result.ShouldBeEmpty();
    }
}
