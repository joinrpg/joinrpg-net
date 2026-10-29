using JoinRpg.Helpers;

namespace JoinRpg.DomainTypes;

/// <summary>
/// Универсальное «доменная валидация не прошла»: операцию выполнить нельзя, потому что
/// переданные данные не удовлетворяют правилам домена.
/// </summary>
/// <remarks>
/// Заменяет исторически использовавшийся для этого EF6-шный
/// <c>System.Data.Entity.Validation.DbEntityValidationException</c> (см. ADR016, задача P4).
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

public class FieldRequiredException(string fieldName, string message) : JoinRpgBaseException(message)
{
    public FieldRequiredException(string fieldName) : this(fieldName, $"Field {fieldName} is required")
    {

    }

    public string FieldName { get; } = fieldName;
}

public class JoinRpgEntityNotFoundException : JoinRpgBaseException
{
    public JoinRpgEntityNotFoundException(IEnumerable<int> ids, string typeName) : base($"Can't found entities of type {typeName} by ids {string.Join(", ", ids)}")
    {
    }

    public JoinRpgEntityNotFoundException(int id, string typeName) : base($"Can't found entity of type {typeName} by id {id}")
    {
    }
}
