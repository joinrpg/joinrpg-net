using JoinRpg.WebPortal.Managers.Accommodation;

namespace JoinRpg.WebPortal.Managers.Test.Accommodation;

/// <summary>
/// Разбор списка комнат (ADR018, PR 4). До переноса в web-слой эта логика жила в
/// <c>AccommodationServiceImpl.AddRooms</c> и не была покрыта тестами вовсе.
/// </summary>
public class RoomNamesParserTests
{
    [Fact]
    public void Parse_SingleName_ReturnsIt()
        => RoomNamesParser.Parse("101").ShouldBe(["101"]);

    [Fact]
    public void Parse_CommaSeparatedNames_TrimsSpaces()
        => RoomNamesParser.Parse("101, 102 ,103").ShouldBe(["101", "102", "103"]);

    [Fact]
    public void Parse_NumericRange_IsExpanded()
        => RoomNamesParser.Parse("5-8").ShouldBe(["5", "6", "7", "8"]);

    [Fact]
    public void Parse_RangeWithSpaces_IsExpanded()
        => RoomNamesParser.Parse(" 5 - 7 ").ShouldBe(["5", "6", "7"]);

    [Fact]
    public void Parse_MixOfNamesAndRanges_KeepsInputOrder()
        => RoomNamesParser.Parse("1,2,5-8").ShouldBe(["1", "2", "5", "6", "7", "8"]);

    /// <summary>Границы диапазона включаются обе, одиночный «диапазон» из одного числа не схлопывается.</summary>
    [Fact]
    public void Parse_RangeOfTwo_ReturnsBothBounds()
        => RoomNamesParser.Parse("10-11").ShouldBe(["10", "11"]);

    /// <summary>Имя комнаты вполне может содержать дефис — оно не диапазон и остаётся как есть.</summary>
    [Theory]
    [InlineData("A-1")]
    [InlineData("1-A")]
    [InlineData("север-юг")]
    public void Parse_NonNumericBounds_KeepsNameAsIs(string name)
        => RoomNamesParser.Parse(name).ShouldBe([name]);

    /// <summary>Перевёрнутый диапазон — не диапазон.</summary>
    [Fact]
    public void Parse_ReversedRange_KeepsNameAsIs()
        => RoomNamesParser.Parse("8-5").ShouldBe(["8-5"]);

    /// <summary>Вырожденный диапазон «5-5» не диапазон: границы равны, а не возрастают.</summary>
    [Fact]
    public void Parse_EqualBounds_KeepsNameAsIs()
        => RoomNamesParser.Parse("5-5").ShouldBe(["5-5"]);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(",")]
    [InlineData(" , , ")]
    [InlineData(null)]
    public void Parse_NoNames_ReturnsEmpty(string? rooms)
        => RoomNamesParser.Parse(rooms).ShouldBeEmpty();

    /// <summary>Пустые элементы списка отбрасываются, а не превращаются в комнату без имени.</summary>
    [Fact]
    public void Parse_TrailingComma_DoesNotProduceEmptyName()
        => RoomNamesParser.Parse("101,").ShouldBe(["101"]);
}
