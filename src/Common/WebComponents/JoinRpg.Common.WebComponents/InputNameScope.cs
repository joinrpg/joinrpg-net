namespace JoinRpg.Common.WebComponents;

/// <summary>
/// Область имён полей формы: каскадируется формой (<see cref="JoinEditForm"/>), поля ввода
/// квалифицируют своё имя префиксом, чтобы браузерное автодополнение не принимало служебные
/// поля (название метода оплаты, расхода и т.п.) за персональные данные. Явный параметр <c>Name</c>
/// у поля побеждает над областью.
/// </summary>
internal sealed class InputNameScope
{
    public string Prefix { get; }

    public InputNameScope(string prefix) => Prefix = prefix;

    public string Qualify(string fieldName) => $"{Prefix}.{fieldName}";
}
