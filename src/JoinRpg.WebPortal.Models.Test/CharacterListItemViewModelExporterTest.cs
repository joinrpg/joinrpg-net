using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Interfaces;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.Exporters;

namespace JoinRpg.WebPortal.Models.Test;

/// <summary>
/// Страж заголовков выгрузки списка персонажей.
/// </summary>
/// <remarks>
/// <para>
/// Заголовки — часть контракта выгрузки: мастера разбирают эти таблицы по именам колонок, и
/// переименование колонки ломает их процессы молча, без ошибки в сборке и без падения тестов.
/// </para>
/// <para>
/// Ловушка, из-за которой страж и появился: имя колонки у <c>ComplexElementMemberColumn</c>
/// собирается через <c>ExpressionHelpers.AsPropertyAccess</c>, а тот распознаёт только обращение
/// к свойству. У EF-версий вложенное выражение было вызовом метода (<c>u => u.GetDisplayName()</c>),
/// то есть <c>null</c>, и заголовок оставался одним префиксом. Стоит при переходе на доменные типы
/// достать то же имя обращением к свойству (<c>m => m.Name.DisplayName</c>) — и к заголовку
/// допишется «.DisplayName». Поэтому ожидаемые заголовки выписаны здесь литералами, а не
/// вычисляются тем же кодом, который их производит.
/// </para>
/// </remarks>
public class CharacterListItemViewModelExporterTest
{
    private sealed class StubUriService : IUriService
    {
        public string Get(ILinkable link) => "/stub";
        public Uri GetUri(ILinkable link) => new("/stub", UriKind.Relative);
    }

    private const string FieldName = "Поле проекта";

    /// <summary>
    /// Проект с ровно одним полем: так в ожидаемом списке видно и колонку поля
    /// (<c>FieldColumn</c>), и то, где она стоит относительно колонок игрока.
    /// </summary>
    private static ProjectInfo MakeProjectInfo(bool archived)
    {
        var mock = new MockedProject();

        // Именно AddField: поле должно быть сущностью проекта, иначе оно исчезнет при пересборке
        // метаданных ниже. Попутно AddField вытесняет поля, которые мок кладёт только в ProjectInfo,
        // так что поле в проекте остаётся одно.
        _ = mock.AddField(field =>
        {
            field.FieldName = FieldName;
            field.FieldType = ProjectFieldType.String;
            field.FieldBoundTo = FieldBoundTo.Character;
        });

        if (archived)
        {
            mock.Project.Active = false;
            mock.Project.IsAcceptingClaims = false;
            mock.ReInitProjectInfo();
        }

        return mock.ProjectInfo;
    }

    private static string?[] ColumnNames(bool archived)
        => [.. new CharacterListItemViewModelExporter(MakeProjectInfo(archived), new StubUriService())
            .ParseColumns()
            .Select(column => column.Name)];

    [Fact]
    public void ColumnHeadersAreStable()
    {
        ColumnNames(archived: false).ShouldBe(
        [
            "Персонаж",
            "Персонаж",
            "Занят?",
            "Группы",
            "Входит в группы",
            "Ответственный мастер",
            FieldName,
            "Игрок",
            "Игрок.SurName",
            "Игрок.FatherName",
            "Игрок.BornName",
            "Игрок.Email",
            "ВК",
            "Телеграм",
            "Игрок.Extra.Livejournal",
            "Игрок.Extra.PhoneNumber",
        ]);
    }

    /// <summary>
    /// В архивном проекте контакты игрока не выгружаются — остаётся одна короткая колонка,
    /// и заголовок у неё тот же, что у полной.
    /// </summary>
    [Fact]
    public void ArchivedProjectExportsPlayerAsSingleShortColumn()
    {
        ColumnNames(archived: true).ShouldBe(
        [
            "Персонаж",
            "Персонаж",
            "Занят?",
            "Группы",
            "Входит в группы",
            "Ответственный мастер",
            FieldName,
            "Игрок",
        ]);
    }
}
