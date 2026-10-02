using JoinRpg.Dal.Impl.Repositories;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Characters.Claims.Finances;
using Shouldly;
using Xunit;

namespace JoinRpg.Dal.Impl.Test;

/// <summary>
/// Тест-страж предиката <see cref="FinancePredicates.RequireModeration"/>.
/// </summary>
/// <remarks>
/// Предикат — намеренная копия вычисляемого свойства
/// <see cref="FinanceOperation.RequireModeration"/>: свойство нужно домену, дерево выражений —
/// запросу (EF6 обычный геттер в SQL не переводит). Копия легко разойдётся с оригиналом, если
/// в правило добавят новый тип операции или состояние, поэтому здесь перебираются ВСЕ комбинации
/// <see cref="FinanceOperationState"/> и <see cref="FinanceOperationType"/>, а не только
/// интересные сегодня: новое значение enum автоматически попадает в перебор.
/// </remarks>
public class FinancePredicatesTest
{
    public static TheoryData<FinanceOperationState, FinanceOperationType> AllCombinations()
    {
        var data = new TheoryData<FinanceOperationState, FinanceOperationType>();
        foreach (var state in Enum.GetValues<FinanceOperationState>())
        {
            foreach (var operationType in Enum.GetValues<FinanceOperationType>())
            {
                data.Add(state, operationType);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(AllCombinations))]
    public void RequireModerationPredicate_ShouldMatchEntityProperty(
        FinanceOperationState state,
        FinanceOperationType operationType)
    {
        var operation = new FinanceOperation { State = state, OperationType = operationType };

        var fromPredicate = FinancePredicates.RequireModeration().Compile()(operation);

        fromPredicate.ShouldBe(operation.RequireModeration);
    }
}
