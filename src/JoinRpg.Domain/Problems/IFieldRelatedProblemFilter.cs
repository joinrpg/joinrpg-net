using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Domain.Problems;

/// <summary>
/// Правило поиска проблем в значении одного поля.
/// </summary>
/// <remarks>
/// <typeparamref name="TObject"/> нужен только для того, чтобы DI собрал отдельный набор правил
/// для персонажа (<see cref="CharacterInfo"/>) и для заявки
/// (<see cref="DomainTypes.Characters.Claims.CharacterClaimInfo"/>): сам метод от него не зависит
/// и работает поверх <see cref="IFieldAvailabilityTarget"/>. Ограничения <c>IFieldContainter</c>
/// здесь сознательно нет — этот интерфейс живёт в <c>JoinRpg.DataModel</c>, и его требование не
/// пустило бы в <typeparamref name="TObject"/> доменные типы (ADR013).
///
/// Цена такого «маркерного» параметра типа: опечатка в аргументе при регистрации не ловится ни
/// компилятором, ни контейнером, поэтому оба валидатора проверяют непустоту своего набора правил
/// в конструкторе.
/// </remarks>
public interface IFieldRelatedProblemFilter<in TObject>
{
    IEnumerable<FieldRelatedProblem> CheckField(IFieldAvailabilityTarget target, FieldWithValue fieldWithValue);
}
