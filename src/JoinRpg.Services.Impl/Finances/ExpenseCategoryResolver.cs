using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;

namespace JoinRpg.Services.Impl.Finances;

/// <summary>
/// Разрешает введённый пользователем путь категории («Аренда базы\Жилые корпуса») в существующее
/// дерево категорий расхода (ADR030), создавая отсутствующие уровни (find-or-create).
/// </summary>
internal class ExpenseCategoryResolver(
    IUnitOfWork unitOfWork,
    IExpenseRepository expenseRepository,
    ILogger<ExpenseCategoryResolver> logger)
{
    /// <summary>
    /// Разрешает введённый путь («Аренда базы\Жилые корпуса») в существующее дерево категорий,
    /// создавая отсутствующие уровни. Возвращает лист и полный путь id от корня.
    /// </summary>
    public async Task<ResolvedCategory> ResolveCategoryPath(ProjectIdentification projectId, string categoryPath)
    {
        var segments = categoryPath.Split('\\')
            .Select(segment => segment.Trim())
            .Where(segment => segment.Length > 0)
            .ToArray();
        if (segments.Length == 0)
        {
            throw new JoinValidationException("TODO[Localize]: категория расхода не может быть пустой");
        }

        var existing = await expenseRepository.GetCategories(projectId);
        // Ключ 0 = корневой уровень (ParentCategoryId == null); id категорий положительные.
        var siblingsByParent = existing
            .GroupBy(c => c.ParentCategoryId?.ExpenseCategoryId ?? 0)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Проходим по существующему дереву, пока сегменты находятся (сравнение имён
        // регистронезависимое внутри одного родителя).
        int? parentId = null;
        var pathIds = new List<int>();
        var skip = 0;
        foreach (var segment in segments)
        {
            var siblings = siblingsByParent.TryGetValue(parentId ?? 0, out var list) ? list : [];
            var found = siblings.FirstOrDefault(c => string.Equals(c.Name, segment, StringComparison.InvariantCultureIgnoreCase));
            if (found is null)
            {
                break;
            }
            parentId = found.CategoryId.ExpenseCategoryId;
            pathIds.Add(parentId.Value);
            skip++;
        }

        // Остаток пути создаём: навигация ParentCategory связывает цепочку, FK и снапшоты
        // путей проставляем после SaveChanges — identity id появляются только там.
        var remaining = segments.Skip(skip).ToArray();
        var created = new List<ExpenseCategory>();
        ExpenseCategory? parentEntity = null;
        foreach (var segment in remaining)
        {
            var entity = new ExpenseCategory
            {
                ProjectId = projectId.Value,
                Name = segment,
                IsActive = true,
            };
            if (parentEntity is null)
            {
                // Родитель — существующий узел, его id известен: проставляем FK сразу,
                // чтобы категория не осталась сиротой, если последующее сохранение расхода
                // бросит после нашего промежуточного SaveChanges.
                entity.ParentCategoryId = parentId;
            }
            else
            {
                // id родителя появится только при сохранении — связываем навигацией
                // (EF6 зафиксирует FK в том же SaveChanges) и дочистим после.
                entity.ParentCategory = parentEntity;
            }
            _ = unitOfWork.GetDbSet<ExpenseCategory>().Add(entity);
            created.Add(entity);
            parentEntity = entity;
        }

        if (created.Count > 0)
        {
            await unitOfWork.SaveChangesAsync();
            var ancestorId = parentId;
            var ancestorPath = new List<int>(pathIds);
            foreach (var entity in created)
            {
                entity.ParentCategoryId = ancestorId;
                entity.ParentCategoryIds = [.. ancestorPath];
                ancestorId = entity.ExpenseCategoryId;
                ancestorPath.Add(entity.ExpenseCategoryId);
            }
            pathIds.AddRange(created.Select(c => c.ExpenseCategoryId));
            logger.LogInformation(
                "Создано {createdCount} категорий расходов по пути «{categoryPath}» в проекте {projectId}",
                created.Count,
                categoryPath,
                projectId.Value);
        }
        else
        {
            logger.LogInformation(
                "Путь категорий «{categoryPath}» в проекте {projectId} найден в существующем дереве",
                categoryPath,
                projectId.Value);
        }

        return new ResolvedCategory(pathIds[^1], [.. pathIds]);
    }
}

internal sealed record ResolvedCategory(int LeafId, int[] PathIds);
