using System.Globalization;

namespace JoinRpg.IntegrationTest.TestInfrastructure;

/// <summary>
/// Значения параметров URL, выведенные из сид-проекта: чем смоук заполняет маршруты (#4956).
/// </summary>
/// <remarks>
/// Ключ — имя параметра, а не эндпоинт: смысл generic-смоука в том, чтобы не писать сценарий под
/// каждую страницу. Один и тот же <c>characterGroupId</c> подходит и списку заявок группы, и
/// её сюжетам, и форуму — разница только в форме записи, и её выбирает <see cref="SmokeParameterValue"/>.
///
/// Имена сравниваются без учёта регистра: в приложении один и тот же параметр пишется и
/// <c>projectId</c>, и <c>projectid</c>, и <c>ProjectId</c>.
/// </remarks>
internal sealed class SmokeParameterValues
{
    private readonly Dictionary<string, SmokeParameterValue> values = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Регистрирует значение параметра.
    /// </summary>
    /// <param name="name">Имя параметра, как оно написано в маршруте или в сигнатуре экшена.</param>
    /// <param name="routeValue">
    /// Значение для подстановки в маршрут — всегда «плоское» (<c>12</c>), как в реальных ссылках сайта.
    /// </param>
    /// <param name="typedValue">
    /// Типизированный id, если параметр может биндиться из query как составной
    /// (<c>PlotElement(1-2-3)</c>). Если тип параметра в экшене не совпадёт — подставится
    /// <paramref name="routeValue"/>.
    /// </param>
    public SmokeParameterValues Add(string name, object routeValue, object? typedValue = null)
    {
        values[name] = new SmokeParameterValue(
            Convert.ToString(routeValue, CultureInfo.InvariantCulture)
                ?? throw new ArgumentNullException(nameof(routeValue)),
            typedValue);
        return this;
    }

    public bool TryGetValue(string name, out SmokeParameterValue value) => values.TryGetValue(name, out value!);
}

/// <summary>Значение одного параметра в двух видах: для маршрута и для query-string.</summary>
internal sealed record SmokeParameterValue(string RouteValue, object? TypedValue)
{
    /// <summary>
    /// Как записать значение в query-string для параметра типа <paramref name="parameterType"/>.
    /// </summary>
    /// <remarks>
    /// Типизированные id (<c>PlotElementIdentification</c>, <c>CompressedIntList</c>) знают свой
    /// формат сами и умеют себя разобрать назад, поэтому для них берём <c>ToString()</c>.
    /// Если экшен объявил параметр обычным <c>int</c> — подставляем плоское значение. Один и тот же
    /// <c>characterGroupId</c> встречается в обоих видах, поэтому решает именно тип параметра.
    /// </remarks>
    public string ForQuery(Type parameterType)
    {
        var type = Nullable.GetUnderlyingType(parameterType) ?? parameterType;
        return TypedValue is not null && type.IsInstanceOfType(TypedValue)
            ? TypedValue.ToString() ?? RouteValue
            : RouteValue;
    }
}
