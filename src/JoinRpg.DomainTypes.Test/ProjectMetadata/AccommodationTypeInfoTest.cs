namespace JoinRpg.DomainTypes.Test.ProjectMetadata;

/// <summary>
/// Флаги типа проживания (ADR015): «бесконечное поселение» читается из колонки БД как параметр
/// записи, «автозаполнение» остаётся нереализованным — значение всегда <c>false</c>.
/// </summary>
public class AccommodationTypeInfoTest
{
    [Fact]
    public void IsInfiniteIsFalseByDefault()
    {
        var typeInfo = ProjectInfoFixture.MakeAccommodationType(typeId: 1, capacity: 4);

        typeInfo.IsInfinite.ShouldBeFalse();
        typeInfo.IsAutoFilledAccommodation.ShouldBeFalse();
    }

    [Fact]
    public void IsInfiniteIsReadFromParameter()
    {
        var typeInfo = ProjectInfoFixture.MakeAccommodationType(typeId: 2, capacity: 1, isInfinite: true);

        typeInfo.IsInfinite.ShouldBeTrue();
        typeInfo.IsAutoFilledAccommodation.ShouldBeFalse();
    }

    /// <summary>
    /// Копия через <c>with</c> сохраняет параметр записи: флаг часть состояния типа, а не
    /// вычисляемое свойство.
    /// </summary>
    [Fact]
    public void IsInfiniteSurvivesWithCopy()
    {
        var original = ProjectInfoFixture.MakeAccommodationType(typeId: 7, capacity: 2, isInfinite: true);
        var typeInfo = original with { Capacity = int.MaxValue, IsPlayerSelectable = true };

        typeInfo.IsInfinite.ShouldBeTrue();
    }

    /// <summary>
    /// Нереализованный флаг «автозаполнение» остаётся вычисляемым свойством: копия через
    /// <c>with</c> не даёт его выставить.
    /// </summary>
    [Fact]
    public void IsAutoFilledAccommodationSurvivesWithCopy()
    {
        var original = ProjectInfoFixture.MakeAccommodationType(typeId: 8, capacity: 2);
        var typeInfo = original with { Capacity = int.MaxValue, IsPlayerSelectable = true };

        typeInfo.IsAutoFilledAccommodation.ShouldBeFalse();
    }
}
