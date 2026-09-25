using JoinRpg.Helpers;

namespace JoinRpg.DomainTypes;

/// <summary>
/// Универсальное «доменная валидация не прошла»: операцию выполнить нельзя, потому что
/// переданные данные не удовлетворяют правилам домена.
/// </summary>
/// <remarks>
/// Заменяет исторически использовавшийся для этого EF6-шный
/// <c>System.Data.Entity.Validation.DbEntityValidationException</c> (см. ADR015, задача P4).
/// Кидать это исключение стоит только там, где нарушение действительно сводится к «данные
/// невалидны». Если для места есть более точное по смыслу исключение («сущность не найдена»,
/// «нарушено конкретное бизнес-правило») — заводить и использовать его.
/// </remarks>
public class JoinValidationException(string message) : JoinRpgBaseException(message)
{
    public JoinValidationException() : this("Validation failed")
    {
    }
}
