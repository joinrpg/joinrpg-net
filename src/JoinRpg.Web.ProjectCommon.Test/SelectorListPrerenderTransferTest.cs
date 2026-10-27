using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Web.ProjectCommon.Claims;
using JoinRpg.Web.ProjectCommon.Fields;
using JoinRpg.Web.ProjectCommon.KogdaIgra;
using JoinRpg.Web.ProjectCommon.Projects;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Web.ProjectCommon.Test;

/// <summary>
/// Списки селекторов переезжают из пререндера в WebAssembly через JSON <see cref="PersistentComponentState"/> (#4119).
/// Элементы каждой обёртки должны пережить этот путь без потерь: иначе остров упадёт при старте
/// (второй публичный конструктор у record без <c>[JsonConstructor]</c>, идентификатор без конвертера).
/// </summary>
public class SelectorListPrerenderTransferTest
{
    private static readonly ProjectIdentification ProjectId = new(1);

    [Fact]
    public Task CharacterDto() => ShouldSurviveTransfer(
        new CharacterDto(new CharacterIdentification(ProjectId, 10), "Персонаж", "Описание", IsPublic: true));

    [Fact]
    public Task CharacterGroupDto() => ShouldSurviveTransfer(
        new CharacterGroupDto(new CharacterGroupIdentification(ProjectId, 20), "Группа", ["Корень", "Группа"], IsPublic: false, IsSpecial: true));

    [Fact]
    public Task ClaimLinkViewModel() => ShouldSurviveTransfer(
        new ClaimLinkViewModel(new ClaimIdentification(ProjectId, 30), new UserDisplayName("Игрок", "Иван Иванов"), "Персонаж", "ник", new UserIdentification(40)));

    [Fact]
    public Task UserInfoHeader() => ShouldSurviveTransfer(
        new UserInfoHeader(new UserIdentification(40), new UserDisplayName("Мастер", null)));

    [Fact]
    public Task ProjectLinkViewModel() => ShouldSurviveTransfer(new ProjectLinkViewModel(ProjectId, "Проект"));

    [Fact]
    public Task ProjectFieldDto() => ShouldSurviveTransfer(
        new ProjectFieldDto(new ProjectFieldIdentification(ProjectId, 50), "Поле", ProjectFieldType.Dropdown, FieldBoundTo.Claim));

    [Fact]
    public Task KogdaIgraShortViewModel() => ShouldSurviveTransfer(
        new KogdaIgraShortViewModel(new KogdaIgraIdentification(60), "Игра", new Uri("https://kogda-igra.ru/game/60"), Year: 2026));

    /// <summary>
    /// Сервер грузит список при пререндере и сохраняет его, клиент получает сохранённое и не грузит сам.
    /// Посередине — те же байты, что едут в странице.
    /// </summary>
    private static async Task ShouldSurviveTransfer<TItem>(TItem item)
    {
        var persistKey = $"SelectorListCache:{typeof(TItem).FullName}:list";

        using var server = new BunitContext();
        var serverState = server.AddBunitPersistentComponentState();
        var serverCache = CreateCache(server);
        _ = await serverCache.GetOrLoad<TItem>("list", () => Task.FromResult<IEnumerable<TItem>>([item]));
        serverState.TriggerOnPersisting();
        serverState.TryTake<TItem[]>(persistKey, out var persisted).ShouldBeTrue();

        using var client = new BunitContext();
        var clientState = client.AddBunitPersistentComponentState();
        clientState.Persist(persistKey, persisted);
        var clientCache = CreateCache(client);
        var restored = await clientCache.GetOrLoad<TItem>("list", () => throw new InvalidOperationException("Список должен прийти с пререндера"));

        restored.Count.ShouldBe(1);
        restored[0].ShouldBeEquivalentTo(item);
    }

    private static SelectorListCache CreateCache(BunitContext ctx)
        => new(
            ctx.Services.GetRequiredService<NavigationManager>(),
            ctx.Services.GetRequiredService<PersistentComponentState>(),
            NullLogger<SelectorListCache>.Instance);
}
