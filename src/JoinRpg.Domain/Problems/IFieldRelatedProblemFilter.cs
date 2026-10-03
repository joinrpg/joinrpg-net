using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Domain.Problems;

/// <summary>
/// Правило поиска проблем в значении одного поля.
/// </summary>
/// <remarks>
/// <typeparamref name="TObject"/> нужен только для того, чтобы DI собрал отдельный набор правил
/// для персонажа и для заявки: сам метод от него не зависит и работает поверх
/// <see cref="IFieldAvailabilityTarget"/>. Ограничения <c>IFieldContainter</c> здесь сознательно
/// нет — этот интерфейс живёт в <c>JoinRpg.DataModel</c>, и его требование не пустило бы в
/// <typeparamref name="TObject"/> доменный агрегат <see cref="CharacterInfo"/> (ADR013).
/// </remarks>
public interface IFieldRelatedProblemFilter<in TObject>
{
    IEnumerable<FieldRelatedProblem> CheckField(IFieldAvailabilityTarget target, FieldWithValue fieldWithValue);
}
