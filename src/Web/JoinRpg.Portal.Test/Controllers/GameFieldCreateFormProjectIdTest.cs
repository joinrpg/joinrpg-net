namespace JoinRpg.Portal.Test.Controllers;

/// <summary>
/// Форма создания поля обязана отправлять ProjectId скрытым полем. Имя поля формы
/// префиксуется («field») — префикс не даёт route-значению ProjectId привязаться к модели,
/// и без скрытого поля в POST приходит ProjectId = 0 (#6236: «Project with Project(0) not found»).
/// </summary>
public class GameFieldCreateFormProjectIdTest
{
    [Fact]
    public void CreateForm_RendersHiddenProjectId()
    {
        var view = Path.Combine(RepoRoot(), "src", "Web", "JoinRpg.Portal", "Views", "GameField", "Create.cshtml");
        var content = File.ReadAllText(view);

        content.Contains("HtmlFieldPrefix = \"field\"")
            .ShouldBeTrue("форма создания поля перестала префиксовать имена полей — проверка HiddenFor ниже утратила смысл, пересмотри тест");
        content.Contains("Html.HiddenFor(model => model.ProjectId)")
            .ShouldBeTrue("форма создания поля не отправляет ProjectId: с префиксом «field» route-значение не привяжется и POST создаст ProjectId = 0 (#6236)");
    }

    /// <summary>
    /// Вьюхи — это контент, в выходной каталог теста они не копируются, поэтому ищем их
    /// в исходниках: от каталога сборки вверх до файла решения.
    /// </summary>
    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Joinrpg.slnx")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        throw new InvalidOperationException("Не найден корень репозитория (Joinrpg.slnx)");
    }
}
