using JoinRpg.Dal.Impl.Repositories;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Finances;
using Shouldly;
using Xunit;

namespace JoinRpg.Dal.Impl.Test.Finances;

/// <summary>
/// Тест-страж предиката <see cref="ExpensePredicates.ActualSpendingSince"/>.
/// </summary>
/// <remarks>
/// Предикат — единственный источник условия «фактическая трата за окно» в запросе
/// <c>ExpenseRepositoryImpl.HasActualExpensesSince</c>. Перебираются все значения
/// <see cref="ExpenseState"/>, а не только интересные сегодня: новое значение enum
/// автоматически попадает в перебор. Плюс граничные случаи по факту и дате.
/// </remarks>
public class ExpensePredicatesTest
{
    private static readonly DateTime Cutoff = new DateTime(2026, 11, 20, 0, 0, 0, DateTimeKind.Utc);

    public static TheoryData<ExpenseState, int?, DateTime?, bool> AllScenarios()
    {
        var data = new TheoryData<ExpenseState, int?, DateTime?, bool>();
        foreach (var state in Enum.GetValues<ExpenseState>())
        {
            // Одобренный расход с заполненным фактом и датой внутри окна
            data.Add(state, 1, Cutoff.AddDays(1), state == ExpenseState.Approved);
        }

        // Неодобренные состояния с заполненным фактом — всегда false
        data.Add(ExpenseState.Proposed, 1, Cutoff.AddDays(1), false);
        data.Add(ExpenseState.Rejected, 1, Cutoff.AddDays(1), false);

        // Одобрен, но факт не заполнен
        data.Add(ExpenseState.Approved, null, Cutoff.AddDays(1), false);

        // Одобрен, факт заполнен, дата не указана
        data.Add(ExpenseState.Approved, 1, null, false);

        // Граница окна: дата ровно cutoff — входит
        data.Add(ExpenseState.Approved, 1, Cutoff, true);

        // Дата до окна — не входит
        data.Add(ExpenseState.Approved, 1, Cutoff.AddTicks(-1), false);

        return data;
    }

    [Theory]
    [MemberData(nameof(AllScenarios))]
    public void ActualSpendingSince_ShouldMatchExpected(
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

        var fromPredicate = ExpensePredicates.ActualSpendingSince(Cutoff).Compile()(expense);

        fromPredicate.ShouldBe(expected);
    }
}
