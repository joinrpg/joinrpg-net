using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.DomainTypes.Test.Characters;

/// <summary>
/// Поле-ссылка на пользователя (ADR017): хранит идентификаторы пользователей строкой.
/// </summary>
public class UserLinkFieldTest
{
    private static FieldWithValue UserLinkField(string? value)
        => new(ProjectInfoFixture.MakeField(1, ProjectFieldType.UserLink), value);

    [Fact]
    public void ParsesSingleUserId()
        => UserLinkField("123").UserIds.ShouldBe([new UserIdentification(123)]);

    [Fact]
    public void EmptyValueGivesNoUsers() => UserLinkField(null).UserIds.ShouldBeEmpty();

    [Fact]
    public void OtherFieldTypesHaveNoUsers()
        => new FieldWithValue(ProjectInfoFixture.MakeField(1, ProjectFieldType.String), "123")
            .UserIds.ShouldBeEmpty();

    /// <summary>
    /// Сохранённое значение читается терпимо: мусор в базе (например, от старого импорта)
    /// не должен ронять показ страницы персонажа.
    /// </summary>
    [Theory]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("123abc")]
    public void GarbageInStoredValueIsIgnoredOnRead(string value)
        => UserLinkField(value).UserIds.ShouldBeEmpty();

    [Fact]
    public void GarbageIsRejectedOnAssign()
        => Should.Throw<FieldUserValueInvalidException>(
            () => UserLinkField(null).NormalizeValueBeforeAssign("abc"));

    [Fact]
    public void NegativeIdIsRejectedOnAssign()
        => Should.Throw<FieldUserValueInvalidException>(
            () => UserLinkField(null).NormalizeValueBeforeAssign("-5"));

    /// <summary>
    /// Мультивыбор — отдельный тип поля (#4511), у одиночного больше одного значения быть не может.
    /// </summary>
    [Fact]
    public void SecondUserIsRejectedOnAssign()
        => Should.Throw<FieldUserValueInvalidException>(
            () => UserLinkField(null).NormalizeValueBeforeAssign("123,456"));

    [Fact]
    public void AssignNormalizesSpaces()
        => UserLinkField(null).NormalizeValueBeforeAssign(" 123 ").ShouldBe("123");

    [Fact]
    public void EmptyAssignGivesNull()
        => UserLinkField("123").NormalizeValueBeforeAssign("").ShouldBeNull();

    [Fact]
    public void UserLinkFieldHasNoPriceAndNoVariants()
    {
        var field = ProjectInfoFixture.MakeField(1, ProjectFieldType.UserLink);
        field.HasValueList.ShouldBeFalse();
        field.SupportsPricing.ShouldBeFalse();
        field.SupportsMarkdown.ShouldBeFalse();
    }

    /// <summary>
    /// <see cref="FieldWithValue.DisplayString"/> у user-поля намеренно остаётся сырым (ADR017 §2):
    /// резолв имени требует обращения к БД, которого в <c>JoinRpg.DomainTypes</c> нет.
    /// </summary>
    [Fact]
    public void DisplayStringStaysRaw() => UserLinkField("123").DisplayString.ShouldBe("123");
}
