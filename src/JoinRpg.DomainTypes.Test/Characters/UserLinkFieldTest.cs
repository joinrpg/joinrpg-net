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

    private static FieldWithValue MultiUserLinkField(string? value)
        => new(ProjectInfoFixture.MakeField(1, ProjectFieldType.MultiUserLink), value);

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

    #region Мультивыбор (#4511)

    [Fact]
    public void MultiParsesSeveralUserIds()
        => MultiUserLinkField("123,456").UserIds
            .ShouldBe([new UserIdentification(123), new UserIdentification(456)]);

    [Fact]
    public void MultiEmptyValueGivesNoUsers() => MultiUserLinkField(null).UserIds.ShouldBeEmpty();

    [Fact]
    public void MultiSeveralUsersAreAcceptedOnAssign()
        => MultiUserLinkField(null).NormalizeValueBeforeAssign("123,456").ShouldBe("123,456");

    /// <summary>Порядок, в котором мастер перечислил пользователей, — значимый.</summary>
    [Fact]
    public void MultiAssignKeepsOrder()
        => MultiUserLinkField(null).NormalizeValueBeforeAssign("456,123").ShouldBe("456,123");

    [Fact]
    public void MultiAssignNormalizesSpacesAndEmptyParts()
        => MultiUserLinkField(null).NormalizeValueBeforeAssign(" 123 , 456 ,").ShouldBe("123,456");

    /// <summary>Дубликаты схлопываются, остаётся первое вхождение.</summary>
    [Fact]
    public void MultiAssignDeduplicates()
        => MultiUserLinkField(null).NormalizeValueBeforeAssign("123,456,123").ShouldBe("123,456");

    [Theory]
    [InlineData("123,abc")]
    [InlineData("123,0")]
    [InlineData("123,-5")]
    [InlineData("abc,123")]
    public void MultiGarbageAmongValidIdsIsRejectedOnAssign(string value)
        => Should.Throw<FieldUserValueInvalidException>(
            () => MultiUserLinkField(null).NormalizeValueBeforeAssign(value));

    /// <summary>Мусор в уже сохранённом значении не роняет показ и у мультивыбора.</summary>
    [Fact]
    public void MultiGarbageInStoredValueIsIgnoredOnRead()
        => MultiUserLinkField("123,abc,456").UserIds
            .ShouldBe([new UserIdentification(123), new UserIdentification(456)]);

    [Fact]
    public void MultiEmptyAssignGivesNull()
        => MultiUserLinkField("123,456").NormalizeValueBeforeAssign("").ShouldBeNull();

    [Fact]
    public void MultiUserLinkFieldHasNoPriceAndNoVariants()
    {
        var field = ProjectInfoFixture.MakeField(1, ProjectFieldType.MultiUserLink);
        field.HasValueList.ShouldBeFalse();
        field.SupportsPricing.ShouldBeFalse();
        field.SupportsMarkdown.ShouldBeFalse();
    }

    [Fact]
    public void MultiUserLinkIsUserLink()
    {
        ProjectFieldType.MultiUserLink.IsUserLink().ShouldBeTrue();
        ProjectFieldType.MultiUserLink.IsMultiUserLink().ShouldBeTrue();
        ProjectFieldType.UserLink.IsMultiUserLink().ShouldBeFalse();
    }

    #endregion
}
