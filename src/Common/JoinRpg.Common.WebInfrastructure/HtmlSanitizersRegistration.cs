using JoinRpg.Common.WebInfrastructure.HtmlSanitize;

namespace JoinRpg.Common.WebInfrastructure;

/// <summary>
/// Регистрация HTML-санитайзеров: transient-обёртки над пулами (<see cref="Microsoft.Extensions.ObjectPool"/>).
/// Экземпляр берётся из пула при создании и возвращается в пул при Dispose.
/// Заодно инициализирует <see cref="JoinHtmlSanitizerLocator"/> для статических вызовов.
/// </summary>
public static class HtmlSanitizersRegistration
{
    /// <summary>Регистрирует три санитайзера (<see cref="IJoinSimpleHtmlSanitizer"/>, <see cref="IJoinRemoveAllHtmlSanitizer"/>,
    /// <see cref="IJoinTelegramHtmlSanitizer"/>) как transient поверх общих пулов и инициализирует <see cref="JoinHtmlSanitizerLocator"/>.</summary>
    public static IServiceCollection AddJoinHtmlSanitizers(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        _ = services
            .AddTransient<IJoinSimpleHtmlSanitizer>(_ => new JoinSimpleHtmlSanitizer(HtmlSanitizers.SimpleSanitizers))
            .AddTransient<IJoinRemoveAllHtmlSanitizer>(_ => new JoinRemoveAllHtmlSanitizer(HtmlSanitizers.RemoveAllSanitizers))
            .AddTransient<IJoinTelegramHtmlSanitizer>(_ => new JoinTelegramHtmlSanitizer(HtmlSanitizers.TelegramSanitizers));

#pragma warning disable CS0618 // JoinHtmlSanitizerLocator — временный механизм связи, инициализируется здесь централизованно
        JoinHtmlSanitizerLocator.Initialize(
            static () => new JoinSimpleHtmlSanitizer(HtmlSanitizers.SimpleSanitizers),
            static () => new JoinRemoveAllHtmlSanitizer(HtmlSanitizers.RemoveAllSanitizers),
            static () => new JoinTelegramHtmlSanitizer(HtmlSanitizers.TelegramSanitizers));
#pragma warning restore CS0618

        return services;
    }
}
