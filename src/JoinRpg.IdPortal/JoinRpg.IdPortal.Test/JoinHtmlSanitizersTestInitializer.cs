using System.Runtime.CompilerServices;
using JoinRpg.Common.WebInfrastructure;

namespace JoinRpg.IdPortal.Test;

/// <summary>
/// Статические вызовы (MarkDownRendererFacade) идут через JoinHtmlSanitizerLocator,
/// поэтому санитайзеры надо зарегистрировать один раз на сборку тестов — контейнер даже не нужен,
/// хватает пустого ServiceCollection.
/// </summary>
internal static class JoinHtmlSanitizersTestInitializer
{
    [ModuleInitializer]
    public static void InitializeJoinHtmlSanitizers()
        => _ = new ServiceCollection().AddJoinHtmlSanitizers();
}
