using System.Net;
using JoinRpg.IntegrationTest.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Выгрузка мастерского грида заявок (ADR027 §5): <c>/{projectId}/claims?export=…</c> — самая
/// тяжёлая по EF ветка страницы (пересчёт фильтра, GetClaims по id, экспортёр), смоук её не
/// видит: он ходит на страницы без query-параметров.
/// </summary>
/// <remarks>
/// Проверяются оба формата: CSV и xlsx идут разными генераторами. Достаточно статуса, типа
/// содержимого и непустого тела — состав колонок фиксирован DRP и не меняется в этом PR.
/// </remarks>
[Collection(SmokeCollection.Name)]
public class ClaimsGridExportScenario(SmokeProjectFixture fixture)
{
    [Theory]
    [InlineData("csv")]
    [InlineData("xlsx")]
    public async Task ExportGrid_ReturnsFile(string exportType)
    {
        var url = $"{fixture.ProjectId.Value}/claims?export={exportType}";

        var response = await fixture.MasterClient.GetAsync(url);

        response.StatusCode.ShouldBe(HttpStatusCode.OK, $"Выгрузка {url} не прошла");
        response.Content.Headers.ContentType.ShouldNotBeNull(
            $"У выгрузки {url} нет типа содержимого — файл не построен");
        (await response.Content.ReadAsByteArrayAsync()).ShouldNotBeEmpty(
            $"Выгрузка {url} отдала пустой файл");
    }
}
