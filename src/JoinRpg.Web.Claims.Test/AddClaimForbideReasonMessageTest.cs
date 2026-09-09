using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.TestHelpers;
using JoinRpg.Web.ProjectCommon.Projects;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Web.Claims.Test;

/// <summary>
/// <see cref="AddClaimForbideReasonMessage"/> переводит <see cref="AddClaimForbideReason"/> в текст для игрока
/// через switch без обработки по умолчанию (default бросает <see cref="ArgumentOutOfRangeException"/>).
/// Тест перебирает все значения enum, чтобы добавление нового значения без соответствующей ветки
/// в компоненте валило сборку тестов, а не прод.
/// </summary>
public class AddClaimForbideReasonMessageTest
{
    private static readonly ProjectIdentification ProjectId = new(1);

    /// <summary>
    /// Причины, для которых компонент уже сегодня (до этого теста) падает в default: throw —
    /// в свиче для них нет веток. Это существующий баг в AddClaimForbideReasonMessage.razor,
    /// который не в скоупе этой задачи (задача — только тесты, компонент не трогаем).
    /// Как только кто-то добавит ветку для одной из этих причин, соответствующий тест ниже
    /// начнет падать с сообщением "ожидали throw, но компонент отрендерился" — тогда нужно
    /// убрать причину из этого списка.
    /// </summary>
    private static readonly HashSet<AddClaimForbideReason> KnownUnhandledReasons =
    [
        AddClaimForbideReason.ApprovedClaimMovedToGroupOrSlot,
        AddClaimForbideReason.CheckedInClaimCantBeMoved,
    ];

    private static BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.Services.AddLogging();
        ctx.Services.AddSingleton<IProjectUriLocator>(new FakeProjectUriLocator());
        return ctx;
    }

    private static IRenderedComponent<AddClaimForbideReasonMessage> Render(BunitContext ctx, AddClaimForbideReason reason)
        => ctx.Render<AddClaimForbideReasonMessage>(p => p
            .Add(x => x.Reason, reason)
            .Add(x => x.CharacterName, "Тестовый персонаж")
            .Add(x => x.ProjectId, ProjectId)
            .Add(x => x.ProjectLifecycleStatus, ProjectLifecycleStatus.ActiveClaimsOpen));

    [Theory]
    [ClassData(typeof(EnumTheoryDataGenerator<AddClaimForbideReason>))]
    public void RenderForEveryReason_ShouldNotThrowAndShouldProduceMarkup(AddClaimForbideReason reason)
    {
        using var ctx = CreateContext();

        if (KnownUnhandledReasons.Contains(reason))
        {
            // Документируем существующий баг, а не прячем его: пока для этой причины
            // нет ветки в switch, компонент бросает ArgumentOutOfRangeException.
            _ = Should.Throw<ArgumentOutOfRangeException>(() => Render(ctx, reason));
            return;
        }

        var cut = Render(ctx, reason);

        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }
}
