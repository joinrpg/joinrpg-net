using JoinRpg.Dal.Impl.Repositories;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Finances;
using Shouldly;
using Xunit;

namespace JoinRpg.Dal.Impl.Test.Finances;

/// <summary>
/// Тест-страж предиката <see cref="ExpensePredicates.ActualSpendingInPeriod"/>.
/// </summary>
/// <remarks>
/// Предикат — единственный источник условия «фактическая трата за период» в запросе
/// <c>ExpenseRepositoryImpl.HasActualExpensesInPeriod</c>. Перебираются все значения
/// <see cref="ExpenseState"/>, а не только интересные сегодня: новое значение enum
/// автоматически попадает в перебор. Плюс граничные случаи по факту и обеим
/// границам полуинтервала (нижняя включается, верхняя — нет).
/// </remarks>
public class ExpensePredicatesTest
{
    private static readonly DateTime From = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime To = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);

    public static TheoryData<ExpenseState, int?, DateTime?, bool> AllScenarios()
    {
        var data = new TheoryData<ExpenseState, int?, DateTime?, bool>();
        foreach (var state in Enum.GetValues<ExpenseState>())
        {
            // Одобренный расход с заполненным фактом и датой внутри периода
            data.Add(state, 1, From.AddDays(1), state == ExpenseState.Approved);
        }

        // Одобрен, но факт не заполнен
        data.Add(ExpenseState.Approved, null, From.AddDays(1), false);

        // Одобрен, факт заполнен, дата не указана
        data.Add(ExpenseState.Approved, 1, null, false);

        // Нижняя граница: дата ровно From — входит
        data.Add(ExpenseState.Approved, 1, From, true);

        // Дата до периода — не входит
        data.Add(ExpenseState.Approved, 1, From.AddTicks(-1), false);

        // Верхняя граница: дата ровно To — не входит (полуинтервал)
        data.Add(ExpenseState.Approved, 1, To, false);

        // Дата в последний момент периода — входит
        data.Add(ExpenseState.Approved, 1, To.AddTicks(-1), true);

        return data;
    }

    [Theory]
    [MemberData(nameof(AllScenarios))]
    public void ActualSpendingInPeriod_ShouldMatchExpected(
        ExpenseState state,
        int? actualQuantity,
        DateTime? spentAt,
        bool expected)
    {
        var expense = new Expense
        {
            ProjectId = 1,
            State = state,
            ActualQuantity = actualQuantity,
            SpentAt = spentAt,
        };

        var fromPredicate = ExpensePredicates.ActualSpendingInPeriod(From, To).Compile()(expense);

        fromPredicate.ShouldBe(expected);
    }
}
