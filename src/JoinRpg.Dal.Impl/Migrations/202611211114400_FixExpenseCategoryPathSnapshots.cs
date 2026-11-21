using System.Data.Entity.Migrations;

namespace JoinRpg.Dal.Impl.Migrations;

/// <summary>
/// Бэкфилл снапшотов путей категорий расходов (ParentCategoriesImpl_ListIds). Резолвер выставлял
/// их после вставки, но на транзиентном контексте сервиса, который эти категории не трекал, —
/// колонка осталась пустой, а FK родителя корректным. Снапшоты пересчитываем по живому дереву
/// (ParentCategoryId), а не копируем из снапшотов расходов: категория без расходов тоже должна
/// получить путь. Аналог — миграция FixZeroGroups (2021).
/// </summary>
public partial class FixExpenseCategoryPathSnapshots : DbMigration
{
    public override void Up()
    {
        // UNION ALL: SQL Server не принимает UNION на верхнем уровне рекурсивного CTE. Циклов
        // в дереве быть не может (пишет только резолвер, сверху вниз), а на случай битых данных
        // MAXRECURSION обрывает рекурсию ошибкой, а не молчаливым мусором.
        Sql(@"
WITH CategoryPaths AS
(
    SELECT c.ExpenseCategoryId,
           CAST(N'' AS nvarchar(max)) AS PathIds
    FROM dbo.ExpenseCategories c
    WHERE c.ParentCategoryId IS NULL

    UNION ALL

    SELECT c.ExpenseCategoryId,
           CAST(p.PathIds + CASE WHEN p.PathIds = N'' THEN N'' ELSE N',' END
                + CAST(p.ExpenseCategoryId AS nvarchar(20)) AS nvarchar(max))
    FROM dbo.ExpenseCategories c
    INNER JOIN CategoryPaths p ON c.ParentCategoryId = p.ExpenseCategoryId
)
UPDATE c
SET ParentCategoriesImpl_ListIds = p.PathIds
FROM dbo.ExpenseCategories c
INNER JOIN CategoryPaths p ON c.ExpenseCategoryId = p.ExpenseCategoryId
WHERE ISNULL(c.ParentCategoriesImpl_ListIds, N'') <> p.PathIds
OPTION (MAXRECURSION 100);
");
    }

    public override void Down()
    {
    }
}
