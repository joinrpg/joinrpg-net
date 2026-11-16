using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JoinRpg.IdPortal.Test.Scenarios;

/// <summary>
/// Регрессия #6234: рантайм-загрузчик .NET по умолчанию фетчит boot-ресурсы WASM
/// с cache:"no-cache", из-за чего браузер ревалидирует весь _framework при каждом буте
/// в обход immutable-кеша. Старт через Blazor.start с disableNoCacheFetch возвращает
/// обычные правила HTTP-кеша. Сам эффект кеширования проверяется в браузере вручную,
/// здесь — что разметку старта не «почистили» обратно.
/// </summary>
[Collection("IdPortal")]
public class WasmBootCachingScenario(IdPortalApplicationFactory factory)
{
    [Fact]
    public async Task HostPage_StartsBlazorWithDisableNoCacheFetch()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

        var page = await client.GetAsync("/Account/Login");
        page.StatusCode.ShouldBe(HttpStatusCode.OK);

        var html = await page.Content.ReadAsStringAsync();

        html.ShouldContain("autostart=\"false\"");
        html.ShouldContain("Blazor.start");
        html.ShouldContain("disableNoCacheFetch");
    }
}
