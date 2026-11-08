using JoinRpg.Common.WebInfrastructure.HtmlSanitize;

namespace JoinRpg.Markdown;

/// <summary>
/// Точка входа к HTML-санитайзерам для статического <see cref="MarkDownRendererFacade"/>.
/// Делегирует в <see cref="JoinHtmlSanitizerLocator"/>; сами санитайзеры живут в JoinRpg.Common.WebInfrastructure.
/// </summary>
internal static class HtmlSanitizers
{
    /// <returns>An instance of <see cref="IJoinSimpleHtmlSanitizer"/> which has to be disposed right after use.</returns>
    public static IJoinSimpleHtmlSanitizer GetSimple()
    {
#pragma warning disable CS0618 // JoinHtmlSanitizerLocator — временный механизм связи для статических вызовов
        return JoinHtmlSanitizerLocator.GetSimpleSanitizer();
#pragma warning restore CS0618
    }

    /// <returns>An instance of <see cref="IJoinRemoveAllHtmlSanitizer"/> which has to be disposed right after use.</returns>
    public static IJoinRemoveAllHtmlSanitizer GetRemoveAll()
    {
#pragma warning disable CS0618 // JoinHtmlSanitizerLocator — временный механизм связи для статических вызовов
        return JoinHtmlSanitizerLocator.GetRemoveAllSanitizer();
#pragma warning restore CS0618
    }

    /// <returns>An instance of <see cref="IJoinTelegramHtmlSanitizer"/> which has to be disposed right after use.</returns>
    public static IJoinTelegramHtmlSanitizer GetTelegram()
    {
#pragma warning disable CS0618 // JoinHtmlSanitizerLocator — временный механизм связи для статических вызовов
        return JoinHtmlSanitizerLocator.GetTelegramSanitizer();
#pragma warning restore CS0618
    }
}
