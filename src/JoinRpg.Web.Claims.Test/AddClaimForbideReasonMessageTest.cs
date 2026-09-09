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

        var cut = Render(ctx, reason);

        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }
}
