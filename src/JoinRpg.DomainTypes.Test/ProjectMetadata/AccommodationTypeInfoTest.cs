using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.DomainTypes.ProjectMetadata.Accommodation;

namespace JoinRpg.DomainTypes.Test.ProjectMetadata;

/// <summary>
/// Нереализованные флаги типа проживания (ADR015): «бесконечное поселение» и «автозаполнение»
/// объявлены в доменной модели, но функциональности за ними нет — значение всегда <c>false</c>.
/// </summary>
/// <remarks>
/// Тест стоит не ради самой константы (её держит подпись: свойства без сеттеров), а ради того,
/// чтобы «реализация» мимо настоящего механизма — например, превращение свойства в параметр
/// записи, читаемый из колонки БД, — не прошла молча: писать флаги по-прежнему нечем, а
/// потребители метаданных уже считают их заведомо снятыми.
/// </remarks>
public class AccommodationTypeInfoTest
{
    [Fact]
    public void UnimplementedFlagsAreAlwaysFalse()
    {
        var typeInfo = ProjectInfoFixture.MakeAccommodationType(typeId: 1, capacity: 4);

        typeInfo.IsInfinite.ShouldBeFalse();
        typeInfo.IsAutoFilledAccommodation.ShouldBeFalse();
    }

    /// <summary>
    /// Даже у типа, собранного «как попало» — нулевая вместимость, отрицательная цена, закрытый
    /// для игроков, — флаги остаются снятыми: они ни от чего не зависят.
    /// </summary>
    [Fact]
    public void UnimplementedFlagsAreFalseForAnyType()
    {
        var projectId = ProjectInfoFixture.ProjectId;
        var typeInfo = new AccommodationTypeInfo(
            new AccommodationTypeIdentification(projectId, 42),
            new RoomCategoryIdentification(projectId, 42),
            Name: "",
            Description: new MarkdownString(""),
            Cost: -100,
            Capacity: 0,
            IsPlayerSelectable: false);

        typeInfo.IsInfinite.ShouldBeFalse();
        typeInfo.IsAutoFilledAccommodation.ShouldBeFalse();
    }

    /// <summary>
    /// Копия через <c>with</c> тоже не даёт выставить флаги: они не параметры записи, а
    /// вычисляемые свойства.
    /// </summary>
    [Fact]
    public void UnimplementedFlagsSurviveWithCopy()
    {
        var original = ProjectInfoFixture.MakeAccommodationType(typeId: 7, capacity: 2);
        var typeInfo = original with { Capacity = int.MaxValue, IsPlayerSelectable = true };

        typeInfo.IsInfinite.ShouldBeFalse();
        typeInfo.IsAutoFilledAccommodation.ShouldBeFalse();
    }
}
