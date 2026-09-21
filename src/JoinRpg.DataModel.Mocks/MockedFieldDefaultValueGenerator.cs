using JoinRpg.Domain.CharacterFields;
using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.DataModel.Mocks;

/// <summary>
/// Генератор значений по умолчанию, который ничего не генерирует. Нужен тестам, которые проверяют
/// сохранение полей, а не подстановку умолчаний.
/// </summary>
public class MockedFieldDefaultValueGenerator : IFieldDefaultValueGenerator
{
    public string? CreateDefaultValue(Claim? claim, FieldWithValue field) => null;

    public string? CreateDefaultValue(Character? character, FieldWithValue field) => null;
}
