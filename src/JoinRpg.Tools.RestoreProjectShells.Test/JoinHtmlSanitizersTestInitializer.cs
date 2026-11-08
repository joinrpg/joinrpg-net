using System.Runtime.CompilerServices;
using JoinRpg.Common.WebInfrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Tools.RestoreProjectShells.Test;

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
