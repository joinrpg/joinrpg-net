using System.Text.Json;
using JoinRpg.Blazor.Client.ApiClients;
using JoinRpg.Services.Interfaces;
using Shouldly;
using Xunit;

namespace JoinRpg.Blazor.Client.Test;

/// <summary>
/// Контракт фактической траты: клиентский <see cref="ExpenseActualDto"/> сериализуется в JSON,
/// который серверный контроллер десериализует в сервисный <c>ExpenseActualRequest</c>.
/// Копейки должны доезжать до сервиса без потерь.
/// </summary>
public class ExpenseActualDtoContractTest
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void WithKopecks_RoundTripsToServiceRequest()
    {
        var dto = new ExpenseActualDto(1, 100.10m, new DateOnly(2026, 2, 15));

        var json = JsonSerializer.Serialize(dto, WebOptions);
        var request = JsonSerializer.Deserialize<ExpenseActualRequest>(json, WebOptions);

        request.ShouldBe(new ExpenseActualRequest(1, 100.10m, new DateOnly(2026, 2, 15)));
    }
}
