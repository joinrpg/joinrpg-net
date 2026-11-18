using System.Runtime.CompilerServices;
using JoinRpg.Common.WebInfrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Portal.Test;

/// <summary>
/// Часть тестов конструирует контроллеры напрямую, без хоста, а view models в конструкторах
/// рендерят markdown через статический MarkDownRendererFacade — он идёт через JoinHtmlSanitizerLocator.
/// Поэтому санитайзеры надо зарегистрировать один раз на сборку тестов — контейнер даже не нужен,
/// хватает пустого ServiceCollection.
/// </summary>
internal static class JoinHtmlSanitizersTestInitializer
{
    [ModuleInitializer]
    public static void InitializeJoinHtmlSanitizers()
        => _ = new ServiceCollection().AddJoinHtmlSanitizers();
}
