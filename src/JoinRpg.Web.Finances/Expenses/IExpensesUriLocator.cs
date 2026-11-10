using JoinRpg.DomainTypes;

namespace JoinRpg.Web.Finances.Expenses;

/// <summary>Ссылки на страницы расходов проекта.</summary>
public interface IExpensesUriLocator
{
    /// <summary>Страница расходов проекта.</summary>
    Uri GetExpensesUri(ProjectIdentification projectId);
}
