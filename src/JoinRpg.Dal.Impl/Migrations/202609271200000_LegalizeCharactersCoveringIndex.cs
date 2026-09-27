namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

/// <summary>
/// Узаконивает покрывающий индекс на Characters, который до сих пор существовал только
/// на проде — его когда-то создали руками, минуя миграции (ADR016, задача P5).
/// </summary>
/// <remarks>
/// Индекс обслуживает запрос «все персонажи проекта с полями для отображения» (сетка ролей).
/// Измерено на проде: 3463 обращения за 9 часов, все — seeks, при 10 обновлениях за тот же
/// период. То есть он рабочий и нужный, а не остаток инструмента анализа.
///
/// В модели EF6 его выразить нельзя: IndexAnnotation не поддерживает INCLUDE. Поэтому
/// создаётся сырым SQL — ровно так же, как рукотворный покрывающий индекс на Claims
/// в миграции 201703201734113_NewIndexes. Модель и снапшот не меняются.
///
/// Зачем это вообще нужно: пока индекс есть только на проде, любое пересозданное
/// из миграций окружение остаётся без него — незаметно, до первой нагрузки. Имя
/// сохранено как на проде (хеш из рекомендации тюнинг-советника), чтобы не переименовывать
/// живой индекс: тот же подход, что и у индекса на Claims.
///
/// Состав воспроизведён один в один. Рекомендации sys.dm_db_missing_index_details просят
/// расширить его (ключ ProjectId + IsActive и INCLUDE почти из всех колонок), но выполнять
/// их не надо: запросов мало (16 за 9 часов), а индекс и так занимает 264 МБ при таблице
/// 269 МБ — расширение даст ещё одну почти полную копию таблицы. Настоящая причина там
/// в том, что запрос выбирает почти всю строку, и лечится она сужением проекции (ADR011).
/// </remarks>
public partial class LegalizeCharactersCoveringIndex : DbMigration
{
    private const string IndexName = "nci_wi_Characters_D7BC872CA7D3A04CC0F1BA8D736093BB";

    public override void Up()
        // На проде индекс уже есть — там миграция no-op; работу делает на всех остальных окружениях.
        => Sql($"""
            IF NOT EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE name = '{IndexName}' AND object_id = OBJECT_ID('dbo.Characters'))
                CREATE NONCLUSTERED INDEX [{IndexName}]
                ON [dbo].[Characters] ([ProjectId])
                INCLUDE ([CharacterName], [Description_Contents], [HidePlayerForCharacter],
                    [IsAcceptingClaims], [IsActive], [IsHot], [IsPublic], [JsonData],
                    [ParentGroupsImpl_ListIds], [PlotElementOrderData]);
            """);

    public override void Down()
        => Sql($"""
            IF EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE name = '{IndexName}' AND object_id = OBJECT_ID('dbo.Characters'))
                DROP INDEX [{IndexName}] ON [dbo].[Characters];
            """);
}
