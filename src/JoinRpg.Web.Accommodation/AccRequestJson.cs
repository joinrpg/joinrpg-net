using System.Text.Json;

namespace JoinRpg.Web.Models.Accommodation;

/// <summary>
/// Сериализация групп проживающих в разметку страницы типа проживания.
/// </summary>
/// <remarks>
/// Форма JSON — часть контракта с <c>wwwroot/Scripts/rooms.js</c>: скрипт читает свойства по
/// именам в PascalCase (<c>Id</c>, <c>RoomId</c>, <c>Persons</c>, <c>PersonsList</c>,
/// <c>FeeToPay</c>, <c>PaymentStatusCssClass</c>, <c>PaymentStatusTitle</c>) и дописывает в объект
/// своё поле <c>Instance</c>. Поэтому опции собраны здесь один раз, а не задаются в каждой
/// Razor-разметке, и накрыты тестом (AccRequestJsonTest).
/// </remarks>
public static class AccRequestJson
{
    /// <remarks>
    /// <see cref="JsonSerializerOptions.PropertyNamingPolicy"/> = <c>null</c> — это и так значение
    /// по умолчанию, но прописано явно: camelCase сломал бы rooms.js. Веб-умолчания ASP.NET Core
    /// (camelCase) тут не действуют — они живут в опциях MVC, а не в <see cref="JsonSerializer"/>.
    /// Экранирование оставлено стандартным (<c>JavaScriptEncoder.Default</c>): значения уезжают и в
    /// HTML-атрибут, и внутрь <c>&lt;script&gt;</c> через <c>Html.Raw</c>, а стандартный
    /// энкодер экранирует <c>&lt;</c>, <c>&gt;</c>, <c>&amp;</c> — то есть имя игрока с
    /// <c>&lt;/script&gt;</c> из разметки не вырвется (Newtonsoft их не экранировал).
    /// </remarks>
    private static readonly JsonSerializerOptions options = new() { PropertyNamingPolicy = null };

    /// <summary>
    /// Сериализует группы проживающих для rooms.js.
    /// </summary>
    public static string Serialize(IReadOnlyList<AccRequestViewModel> requests)
        => JsonSerializer.Serialize(requests, options);
}
