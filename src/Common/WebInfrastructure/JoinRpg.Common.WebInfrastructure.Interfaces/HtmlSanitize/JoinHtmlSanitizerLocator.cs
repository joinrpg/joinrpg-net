namespace JoinRpg.Common.WebInfrastructure.HtmlSanitize;

/// <summary>
/// Временный механизм связи для статических вызовов (например, <c>MarkDownRendererFacade</c>):
/// позволяет достать санитайзеры, зарегистрированные через <c>AddJoinHtmlSanitizers()</c>,
/// без доступа к <see cref="IServiceProvider"/>.
/// </summary>
/// <remarks>
/// Будет удалён, когда рендеринг markdown переведут на DI. Новому коду использовать DI-регистрации
/// <see cref="IJoinSimpleHtmlSanitizer"/>, <see cref="IJoinRemoveAllHtmlSanitizer"/> и <see cref="IJoinTelegramHtmlSanitizer"/>.
/// </remarks>
[Obsolete("Временный механизм связи для статических вызовов. Использовать DI-регистрации санитайзеров.")]
public static class JoinHtmlSanitizerLocator
{
    private static Func<IJoinSimpleHtmlSanitizer>? simpleSanitizerFactory;
    private static Func<IJoinRemoveAllHtmlSanitizer>? removeAllSanitizerFactory;
    private static Func<IJoinTelegramHtmlSanitizer>? telegramSanitizerFactory;

    /// <summary>Взять простой санитайзер из пула; вернуть в пул — через <see cref="IDisposable.Dispose"/>.</summary>
    /// <exception cref="InvalidOperationException">Санитайзеры не зарегистрированы (нужен вызов <c>AddJoinHtmlSanitizers()</c>).</exception>
    public static IJoinSimpleHtmlSanitizer GetSimpleSanitizer()
        => simpleSanitizerFactory?.Invoke() ?? throw CreateNotInitializedException();

    /// <summary>Взять «удаляющий всё» санитайзер из пула; вернуть в пул — через <see cref="IDisposable.Dispose"/>.</summary>
    /// <exception cref="InvalidOperationException">Санитайзеры не зарегистрированы (нужен вызов <c>AddJoinHtmlSanitizers()</c>).</exception>
    public static IJoinRemoveAllHtmlSanitizer GetRemoveAllSanitizer()
        => removeAllSanitizerFactory?.Invoke() ?? throw CreateNotInitializedException();

    /// <summary>Взять Telegram-санитайзер из пула; вернуть в пул — через <see cref="IDisposable.Dispose"/>.</summary>
    /// <exception cref="InvalidOperationException">Санитайзеры не зарегистрированы (нужен вызов <c>AddJoinHtmlSanitizers()</c>).</exception>
    public static IJoinTelegramHtmlSanitizer GetTelegramSanitizer()
        => telegramSanitizerFactory?.Invoke() ?? throw CreateNotInitializedException();

    /// <summary>Подключить фабрики санитайзеров. Вызывается из <c>AddJoinHtmlSanitizers()</c>.</summary>
    public static void Initialize(
        Func<IJoinSimpleHtmlSanitizer> simpleSanitizer,
        Func<IJoinRemoveAllHtmlSanitizer> removeAllSanitizer,
        Func<IJoinTelegramHtmlSanitizer> telegramSanitizer)
    {
        ArgumentNullException.ThrowIfNull(simpleSanitizer);
        ArgumentNullException.ThrowIfNull(removeAllSanitizer);
        ArgumentNullException.ThrowIfNull(telegramSanitizer);

        simpleSanitizerFactory = simpleSanitizer;
        removeAllSanitizerFactory = removeAllSanitizer;
        telegramSanitizerFactory = telegramSanitizer;
    }

    private static InvalidOperationException CreateNotInitializedException()
        => new("HTML-санитайзеры не зарегистрированы. Вызовите services.AddJoinHtmlSanitizers() при старте хоста.");
}
