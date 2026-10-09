namespace JoinRpg.Common.WebComponents.Test;

/// <summary>
/// Храповик переезда с JS-плагинов Bootstrap 3 (dropdown, collapse, modal) на компоненты
/// <c>JoinRpg.Common.WebComponents</c>. Новые места на этих плагинах добавлять нельзя, а список
/// исключений только сокращается: когда файл переведён, его надо убрать из исключений —
/// иначе тест тоже упадёт.
/// </summary>
public class BootstrapJsPluginsAreNotUsedTest
{
    private const string ThisTest =
        "src/Common/WebComponents/JoinRpg.Common.WebComponents.Test/BootstrapJsPluginsAreNotUsedTest.cs";

    /// <summary>
    /// Запрещённая строка и файлы, которые ещё не переведены.
    /// </summary>
    /// <param name="Marker">Строка, выдающая использование плагина Bootstrap.</param>
    /// <param name="Explanation">Что делать, если тест упал на этом маркере.</param>
    /// <param name="AllowedFiles">Файлы-исключения, ждущие переезда.</param>
    private sealed record ForbiddenMarker(string Marker, string Explanation, params string[] AllowedFiles);

    private const string Dropdown =
        "Выпадающее меню на JS Bootstrap — не используйте его в новом коде.";

    private const string Collapse =
        "Раскрывающийся блок на JS Bootstrap — используйте JoinCollapsePanel / JoinCollapsePanelGroup.";

    private const string Modal =
        "Модальное окно Bootstrap — используйте JoinDialog / JoinMessageDialog / JoinFormDialog.";

    private static readonly ForbiddenMarker[] ForbiddenMarkers =
    [
        new("data-toggle=\"dropdown\"", Dropdown,
            "src/Common/WebComponents/JoinRpg.Common.WebComponents/JoinDotsMenu.razor",
            "src/Common/WebComponents/JoinRpg.Common.WebComponents/TypedDropdownButton.razor",
            "src/JoinRpg.Portal/Views/Shared/CharacterNavigation.cshtml",
            "src/JoinRpg.Portal/Views/Shared/Components/MainMenu/MainMenu.cshtml",
            "src/JoinRpg.Portal/Views/Shared/Components/ProjectMenu/MasterMenu.cshtml",
            "src/JoinRpg.Portal/Views/Shared/Components/ProjectMenu/PlayerMenu.cshtml",
            "src/JoinRpg.Portal/Views/Shared/_LoginPartial.cshtml"),
        new(".dropdown(", Dropdown,
            ThisTest),
        new(".bs.dropdown", Dropdown,
            ThisTest),
        new("data-toggle=\"collapse\"", Collapse,
            "src/JoinRpg.Portal/Views/Claim/Edit.cshtml",
            "src/JoinRpg.Portal/Views/Finances/Setup.cshtml",
            "src/JoinRpg.Portal/Views/Shared/Components/Comment/Comment.cshtml",
            "src/JoinRpg.Portal/Views/Shared/Components/MainMenu/MainMenu.cshtml",
            "src/JoinRpg.Portal/Views/Shared/Components/ProjectMenu/MasterMenu.cshtml",
            "src/JoinRpg.Portal/Views/Shared/Components/ProjectMenu/PlayerMenu.cshtml"),
        new(".collapse(", Collapse,
            ThisTest),
        new(".bs.collapse", Collapse,
            ThisTest),
        new("data-toggle=\"modal\"", Modal,
            "src/JoinRpg.Portal/Views/Finances/ClaimFinanceOperations/ClaimFinanceOperations.cshtml",
            "src/JoinRpg.Portal/Views/Finances/ClaimFinanceOperations/_AdminFunctionsPartial.cshtml",
            "src/JoinRpg.Portal/Views/Finances/ClaimFinanceOperations/_FinanceOperationRowPartial.cshtml",
            "src/JoinRpg.Portal/Views/Finances/ClaimFinanceOperations/_RecurrentFunctionsPartial.cshtml",
            "src/JoinRpg.Portal/Views/Finances/Setup.cshtml",
            "src/JoinRpg.Portal/Views/Finances/_PaymentTypesPartial.cshtml"),
        new("data-dismiss=\"modal\"", Modal,
            "src/JoinRpg.Portal/Views/Claim/_RequestPreferentialFeeDialog.cshtml",
            "src/JoinRpg.Portal/Views/Finances/ClaimFinanceOperations/ClaimFinanceOperations.cshtml",
            "src/JoinRpg.Portal/Views/Finances/Setup.cshtml",
            "src/JoinRpg.Portal/Views/Finances/_AddPaymentTypeDialog.cshtml",
            "src/JoinRpg.Portal/Views/Finances/_CancelRecurrentPaymentDialog.cshtml",
            "src/JoinRpg.Portal/Views/Finances/_ForceRecurrentPaymentDialog.cshtml",
            "src/JoinRpg.Portal/Views/Finances/_PayOnlineDialog.cshtml",
            "src/JoinRpg.Portal/Views/Finances/_RefundPaymentDialog.cshtml",
            "src/JoinRpg.Portal/Views/Finances/_SubmitPaymentDialog.cshtml",
            "src/JoinRpg.Portal/Views/Finances/_SubscribeOnlineDialog.cshtml",
            "src/JoinRpg.Portal/Views/Finances/_TogglePaymentTypeDialog.cshtml"),
        new(".modal(", Modal,
            ThisTest),
        new(".bs.modal", Modal,
            ThisTest,
            "src/JoinRpg.Portal/Views/Finances/Setup.cshtml",
            "src/JoinRpg.Portal/Views/Finances/_TogglePaymentTypeDialog.cshtml"),
    ];

    private static readonly string[] ScannedExtensions = [".razor", ".cshtml", ".cs", ".js", ".html"];

    /// <summary>Папки, за которые мы не отвечаем: вендорные библиотеки и результаты сборки.</summary>
    private static readonly string[] SkippedFolders =
        ["/wwwroot/lib/", "/wwwroot/twitter-bootstrap/", "/obj/", "/bin/"];

    private static readonly IReadOnlyList<(string Path, string Text)> Sources = ReadSources();

    [Fact]
    public void NoNewBootstrapJsPluginUsages()
    {
        var offenders = ForbiddenMarkers
            .SelectMany(marker => Sources
                .Where(source => !marker.AllowedFiles.Contains(source.Path, StringComparer.OrdinalIgnoreCase))
                .Where(source => source.Text.Contains(marker.Marker, StringComparison.Ordinal))
                .Select(source => $"{source.Path}: «{marker.Marker}» — {marker.Explanation}"))
            .Order()
            .ToList();

        offenders.ShouldBeEmpty(
            $"Новые использования JS-плагинов Bootstrap 3. Нарушители:{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    [Fact]
    public void AllowedFilesStillNeedTheException()
    {
        var stale = ForbiddenMarkers
            .SelectMany(marker => marker.AllowedFiles
                .Where(file => file != ThisTest)
                .Where(file => !Sources.Any(source =>
                    string.Equals(source.Path, file, StringComparison.OrdinalIgnoreCase)
                    && source.Text.Contains(marker.Marker, StringComparison.Ordinal)))
                .Select(file => $"{file}: «{marker.Marker}»"))
            .Order()
            .ToList();

        stale.ShouldBeEmpty(
            $"Эти файлы уже переведены (или удалены) — уберите их из исключений:{Environment.NewLine}{string.Join(Environment.NewLine, stale)}");
    }

    private static List<(string Path, string Text)> ReadSources()
    {
        var repositoryRoot = RepositoryLocator.FindRoot();

        return Directory
            .EnumerateFiles(Path.Combine(repositoryRoot, "src"), "*", SearchOption.AllDirectories)
            .Where(path => ScannedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .Select(path => Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/'))
            .Where(path => !SkippedFolders.Any(folder => path.Contains(folder, StringComparison.OrdinalIgnoreCase)))
            .Select(path => (Path: path, Text: File.ReadAllText(Path.Combine(repositoryRoot, path))))
            .ToList();
    }
}
