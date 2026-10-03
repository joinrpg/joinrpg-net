using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Interfaces;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.Exporters;

namespace JoinRpg.WebPortal.Models.Test;

/// <summary>
/// Страж заголовков выгрузки списка заявок — брат
/// <see cref="CharacterListItemViewModelExporterTest"/>.
/// </summary>
/// <remarks>
/// <para>
/// Заголовки — часть контракта выгрузки: мастера разбирают эти таблицы по именам колонок, и
/// переименование колонки ломает их процессы молча, без ошибки в сборке и без падения тестов.
/// </para>
/// <para>
/// Ловушка, из-за которой стражи и появились: имя колонки у <c>ComplexElementMemberColumn</c>
/// собирается через <c>ExpressionHelpers.AsPropertyAccess</c>, а тот распознаёт только обращение
/// к свойству. У EF-версий вложенное выражение было вызовом метода
/// (<c>u => u.GetDisplayName()</c>), то есть <c>null</c>, и заголовок оставался одним префиксом.
/// Стоит при переходе на доменные типы достать то же имя обращением к свойству — и к заголовку
/// допишется «.DisplayName». Поэтому ожидаемые заголовки выписаны здесь литералами, а не
/// вычисляются тем же кодом, который их производит.
/// </para>
/// <para>
/// Колонки игрока здесь — доменная перегрузка <c>UserColumn(UserInfo?)</c>: заявка отдаёт игрока
/// как <c>UserInfo</c>, а не как EF-сущность. Её заголовки сознательно человеческие
/// («Игрок.Фамилия» вместо «FullPlayer.SurName»), а префикс берётся из Display
/// свойства <c>FullPlayer</c>.
/// </para>
/// </remarks>
public class ClaimListItemViewModelExporterTest
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
            field.FieldBoundTo = FieldBoundTo.Claim;
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
        => [.. new ClaimListItemViewModelExporter(new StubUriService(), MakeProjectInfo(archived))
            .ParseColumns()
            .Select(column => column.Name)];

    /// <remarks>
    /// Две странности в списке зафиксированы как есть, они были и до перехода на доменные типы:
    /// <c>null</c> — это колонка-ссылка на заявку (<c>UriColumn(x => x)</c>), у которой нет ни
    /// обращения к свойству, ни явного имени; а хвосты «.DisplayName» у ответственного и автора
    /// последнего комментария — та самая ловушка <c>CombineName</c> для
    /// <c>ShortUserColumn(UserLinkViewModel?)</c>, до которой руки ещё не дошли.
    /// </remarks>
    [Fact]
    public void ColumnHeadersAreStable()
    {
        ColumnNames(archived: false).ShouldBe(
        [
            "Имя",
            null,
            "Статус",
            "Причина отказа",
            "Обновлена",
            "Создана",
            "Итого взнос",
            "Осталось",
            "Уплачено",
            "LastModifiedBy.DisplayName",
            "Ответственный.DisplayName",
            "Игрок",
            "Игрок.Фамилия",
            "Игрок.Отчество",
            "Игрок.Имя",
            "Игрок.Email",
            "ВК",
            "Телеграм",
            "Игрок.Livejournal",
            "Игрок.Телефон",
            FieldName,
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
            "Имя",
            null,
            "Статус",
            "Причина отказа",
            "Обновлена",
            "Создана",
            "Итого взнос",
            "Осталось",
            "Уплачено",
            "LastModifiedBy.DisplayName",
            "Ответственный.DisplayName",
            "Игрок",
            FieldName,
        ]);
    }
}
