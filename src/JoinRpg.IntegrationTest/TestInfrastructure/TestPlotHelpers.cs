using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Plots;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.IntegrationTest.TestInfrastructure;

/// <summary>
/// Помощники для наполнения проекта сюжетами (папка сюжета + вводные с таргетами).
/// </summary>
public static class TestPlotHelpers
{
    /// <summary>
    /// Создаёт в проекте персонажа, группу персонажей, папку сюжета и <paramref name="elementCount"/> вводных.
    /// Каждая вводная привязана и к персонажу, и к группе, имеет уникальный текст и опубликована.
    /// </summary>
    /// <remarks>
    /// Вызывать в скоупе, где уже включена impersonation мастера проекта
    /// (см. <see cref="ImpersonationHelpers.RunAsAsync{T}"/>): доменные сервисы проверяют доступ текущего пользователя.
    /// У каждой вводной две версии: 0 — черновик, 1 — итоговый уникальный текст (он и попадает в
    /// <see cref="PlotSeedResult.ElementContents"/>). Это позволяет проверять страницу просмотра конкретной версии.
    /// </remarks>
    /// <param name="serviceProvider">Scoped service provider с включённой impersonation мастера.</param>
    /// <param name="projectId">Проект, в котором создаётся сюжет.</param>
    /// <param name="elementCount">Сколько вводных создать в папке.</param>
    public static async Task<PlotSeedResult> SeedPlotFolderAsync(
        IServiceProvider serviceProvider,
        ProjectIdentification projectId,
        int elementCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(elementCount);

        var metadataRepository = serviceProvider.GetRequiredService<IProjectMetadataRepository>();
        var characterService = serviceProvider.GetRequiredService<ICharacterService>();
        var characterGroupService = serviceProvider.GetRequiredService<ICharacterGroupService>();
        var plotService = serviceProvider.GetRequiredService<IPlotService>();

        var projectInfo = await metadataRepository.GetProjectMetadata(projectId);
        var rootGroupId = projectInfo.GroupTree.RootGroupId;
        var nameFieldId = (projectInfo.CharacterNameField
                ?? throw new InvalidOperationException("В проекте нет поля имени персонажа"))
            .Id.ProjectFieldId;

        // Уникальный суффикс, чтобы тексты и имена гарантированно не пересекались с другими тестами.
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var characterName = $"Таргетперсонаж {suffix}";
        var targetCharacterId = await characterService.AddCharacter(
            new AddCharacterRequest(
                projectId,
                ParentCharacterGroupIds: [rootGroupId],
                new CharacterTypeInfo(
                    CharacterType.Player,
                    IsHot: false,
                    SlotLimit: null,
                    SlotName: null,
                    CharacterVisibility.Public),
                FieldValues: new FieldLayerContainer(
                    projectInfo,
                    new Dictionary<int, string?> { [nameFieldId] = characterName })));

        var targetGroupId = await characterGroupService.AddCharacterGroup(
            projectId,
            $"Таргетгруппа {suffix}",
            isPublic: true,
            parentCharacterGroupIds: [rootGroupId],
            description: "");

        var plotFolderId = await plotService.CreatePlotFolder(
            projectId,
            $"Тестовый сюжет {suffix}",
            todo: "");

        var elementIds = new List<PlotElementIdentification>(elementCount);
        var elementContents = new List<string>(elementCount);

        for (var i = 1; i <= elementCount; i++)
        {
            var content = $"Уникальный текст вводной {i} {suffix}";

            // Версия 0 — черновик.
            var draftVersionId = await plotService.CreatePlotElement(
                plotFolderId,
                content: $"Черновик вводной {i} {suffix}",
                todoField: "",
                targetGroups: [targetGroupId],
                targetChars: [targetCharacterId],
                elementType: PlotElementType.RegularPlot,
                isMasterOnly: false);

            // Версия 1 — итоговый текст.
            await plotService.EditPlotElementText(draftVersionId.PlotElementId, content, todoField: "");

            await plotService.PublishElementVersion(
                draftVersionId.Next(),
                sendNotification: false,
                commentText: null);

            elementIds.Add(draftVersionId.PlotElementId);
            elementContents.Add(content);
        }

        return new PlotSeedResult(
            plotFolderId,
            elementIds,
            targetCharacterId,
            characterName,
            targetGroupId,
            elementContents);
    }

    /// <summary>
    /// Добавляет в папку сюжета одну опубликованную вводную типа «раздатка», привязанную к указанному персонажу.
    /// Возвращает её идентификатор и текст — текст же должен отрисоваться в чек-листе раздатки на печати.
    /// </summary>
    /// <remarks>
    /// Вызывать в скоупе с включённой impersonation мастера проекта, как и <see cref="SeedPlotFolderAsync"/>.
    /// Раздатка попадает на печать только опубликованной (<see cref="PlotVersionFilter.PublishedVersion"/>),
    /// поэтому сразу публикуем нулевую версию.
    /// </remarks>
    public static async Task<HandoutSeedResult> SeedHandoutAsync(
        IServiceProvider serviceProvider,
        PlotFolderIdentification plotFolderId,
        CharacterIdentification targetCharacterId)
    {
        var plotService = serviceProvider.GetRequiredService<IPlotService>();

        var content = $"Уникальная раздатка {Guid.NewGuid().ToString("N")[..8]}";

        var versionId = await plotService.CreatePlotElement(
            plotFolderId,
            content: content,
            todoField: "",
            targetGroups: [],
            targetChars: [targetCharacterId],
            elementType: PlotElementType.Handout,
            isMasterOnly: false);

        await plotService.PublishElementVersion(versionId, sendNotification: false, commentText: null);

        return new(versionId.PlotElementId, content);
    }

    /// <summary>
    /// Создаёт в папке вводную с несколькими версиями текста: у каждой версии свой уникальный текст,
    /// мастерское TODO заполнено только у последней.
    /// </summary>
    /// <remarks>
    /// TODO только у последней версии — не прихоть сида, а проверяемое поведение: страница
    /// редактирования показывает текст запрошенной версии, но TODO всегда от последней
    /// (<c>PlotElementDetailsDto.LastVersionTodoField</c>).
    /// </remarks>
    /// <param name="serviceProvider">Scoped service provider с включённой impersonation мастера.</param>
    /// <param name="plotFolderId">Папка, в которой создаётся вводная.</param>
    /// <param name="targetCharacterId">Персонаж в таргетах вводной.</param>
    /// <param name="versionCount">Сколько версий создать; версии нумеруются с нуля.</param>
    /// <param name="publishVersion">Номер версии для публикации; <c>null</c> — не публиковать вовсе.</param>
    public static async Task<PlotVersionsSeedResult> SeedElementWithVersionsAsync(
        IServiceProvider serviceProvider,
        PlotFolderIdentification plotFolderId,
        CharacterIdentification targetCharacterId,
        int versionCount,
        int? publishVersion = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(versionCount, 1);
        if (publishVersion is int p)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(p, versionCount);
        }

        var plotService = serviceProvider.GetRequiredService<IPlotService>();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var contents = new List<string>(versionCount);

        string ContentOf(int version) => $"Версия {version} вводной {suffix}";
        var lastTodo = $"Доделать к версии {versionCount - 1} {suffix}";

        contents.Add(ContentOf(0));
        var firstVersionId = await plotService.CreatePlotElement(
            plotFolderId,
            content: ContentOf(0),
            todoField: versionCount == 1 ? lastTodo : "",
            targetGroups: [],
            targetChars: [targetCharacterId],
            elementType: PlotElementType.RegularPlot,
            isMasterOnly: false);
        var elementId = firstVersionId.PlotElementId;

        for (var version = 1; version < versionCount; version++)
        {
            contents.Add(ContentOf(version));
            await plotService.EditPlotElementText(
                elementId,
                ContentOf(version),
                todoField: version == versionCount - 1 ? lastTodo : "");
        }

        if (publishVersion is int versionToPublish)
        {
            await plotService.PublishElementVersion(
                new PlotVersionIdentification(elementId, versionToPublish),
                sendNotification: false,
                commentText: null);
        }

        return new PlotVersionsSeedResult(elementId, contents, lastTodo);
    }
}

/// <summary>
/// Результат наполнения проекта тестовой раздаткой.
/// </summary>
/// <param name="ElementId">Созданная раздатка.</param>
/// <param name="Content">Текст раздатки.</param>
public record HandoutSeedResult(
    PlotElementIdentification ElementId,
    string Content);

/// <summary>
/// Вводная с несколькими версиями текста.
/// </summary>
/// <param name="ElementId">Созданная вводная.</param>
/// <param name="VersionContents">Текст каждой версии; индекс в списке равен номеру версии.</param>
/// <param name="LastVersionTodoField">Мастерское TODO последней версии.</param>
public record PlotVersionsSeedResult(
    PlotElementIdentification ElementId,
    IReadOnlyList<string> VersionContents,
    string LastVersionTodoField);

/// <summary>
/// Результат наполнения проекта тестовым сюжетом.
/// </summary>
/// <param name="PlotFolderId">Созданная папка сюжета.</param>
/// <param name="ElementIds">Созданные вводные в порядке создания.</param>
/// <param name="TargetCharacterId">Персонаж, привязанный ко всем вводным.</param>
/// <param name="TargetCharacterName">Имя этого персонажа (как оно должно отображаться на странице).</param>
/// <param name="TargetGroupId">Группа персонажей, привязанная ко всем вводным.</param>
/// <param name="ElementContents">Тексты вводных в том же порядке, что <paramref name="ElementIds"/>.</param>
public record PlotSeedResult(
    PlotFolderIdentification PlotFolderId,
    IReadOnlyList<PlotElementIdentification> ElementIds,
    CharacterIdentification TargetCharacterId,
    string TargetCharacterName,
    CharacterGroupIdentification TargetGroupId,
    IReadOnlyList<string> ElementContents);
