using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Plots;
using JoinRpg.Web.Plots.Folders;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Web.Plots.Test;

/// <summary>
/// Список <see cref="PlotFolderSelector"/> переезжает из пререндера в WebAssembly через JSON
/// <see cref="PersistentComponentState"/> (#4119) — <see cref="PlotFolderDto"/> должен пережить этот путь.
/// Остальные элементы селекторов проверяются в JoinRpg.Web.ProjectCommon.Test.
/// </summary>
public class PlotFolderDtoPrerenderTransferTest
{
    [Fact]
    public async Task PlotFolderDtoSurvivesTransfer()
    {
        var item = new PlotFolderDto(new PlotFolderIdentification(new ProjectIdentification(1), 2), "Сюжет");
        var persistKey = $"SelectorListCache:{typeof(PlotFolderDto).FullName}:list";

        using var server = new BunitContext();
        var serverState = server.AddBunitPersistentComponentState();
        _ = await CreateCache(server).GetOrLoad<PlotFolderDto>("list", () => Task.FromResult<IEnumerable<PlotFolderDto>>([item]));
        serverState.TriggerOnPersisting();
        serverState.TryTake<PlotFolderDto[]>(persistKey, out var persisted).ShouldBeTrue();

        using var client = new BunitContext();
        client.AddBunitPersistentComponentState().Persist(persistKey, persisted);
        var restored = await CreateCache(client).GetOrLoad<PlotFolderDto>("list",
            () => throw new InvalidOperationException("Список должен прийти с пререндера"));

        restored.ShouldBe([item]);
    }

    private static SelectorListCache CreateCache(BunitContext ctx)
        => new(
            ctx.Services.GetRequiredService<NavigationManager>(),
            ctx.Services.GetRequiredService<PersistentComponentState>(),
            NullLogger<SelectorListCache>.Instance);
}
