using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes.Finances;
using JoinRpg.Portal.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

/// <summary>
/// Категории расходов проекта (ADR030): плоский список с родителями — для автодополнения
/// поля пути категории и будущего фильтра грида.
/// </summary>
[Route("/webapi/{projectId}/expense-categories")]
[RequireMaster]
[ApiController]
public class ExpenseCategoriesController(IExpenseRepository expenseRepository) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<ExpenseCategoryInfo>>> List(ProjectIdentification projectId)
        => Ok(await expenseRepository.GetCategories(projectId));
}
