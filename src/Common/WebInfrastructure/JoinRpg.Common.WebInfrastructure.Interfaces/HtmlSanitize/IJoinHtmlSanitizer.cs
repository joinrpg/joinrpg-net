namespace JoinRpg.Common.WebInfrastructure.HtmlSanitize;

/// <summary>
/// Одноразовый HTML-санитайзер. После использования обязателен <see cref="IDisposable.Dispose"/>:
/// он возвращает внутренний объект в пул для повторного использования.
/// </summary>
public interface IJoinHtmlSanitizer : IDisposable
{
    /// <summary>Очистить HTML от небезопасного содержимого.</summary>
    string Sanitize(string html);
}

/// <summary>Базовый «мягкий» санитайзер: пропускает безопасное HTML-форматирование (списки, таблицы, ссылки и т.п.).</summary>
public interface IJoinSimpleHtmlSanitizer : IJoinHtmlSanitizer;

/// <summary>Санитайзер, выкидывающий всю разметку (для вывода plain text поверх HTML-канала).</summary>
public interface IJoinRemoveAllHtmlSanitizer : IJoinHtmlSanitizer;

/// <summary>Санитайзер для Telegram: только inline-разметка, блочные теги выбрасываются.</summary>
public interface IJoinTelegramHtmlSanitizer : IJoinHtmlSanitizer;
