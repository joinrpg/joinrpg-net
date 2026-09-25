using JoinRpg.Dal.Impl.Repositories;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Characters;
using Shouldly;
using Xunit;

namespace JoinRpg.Dal.Impl.Test;

public class CharacterPredicatesTest
{
    private static bool Matches(CharacterStatusSpec spec, bool isActive)
        => CharacterPredicates.ByStatus(spec).Compile()(new Character { IsActive = isActive });

    [Theory]
    [InlineData(CharacterStatusSpec.Any, true)]
    [InlineData(CharacterStatusSpec.Any, false)]
    [InlineData(CharacterStatusSpec.Active, true)]
    [InlineData(CharacterStatusSpec.Deleted, false)]
    public void SpecMatchesCharacter(CharacterStatusSpec spec, bool isActive)
        => Matches(spec, isActive).ShouldBeTrue();

    [Theory]
    [InlineData(CharacterStatusSpec.Active, false)]
    [InlineData(CharacterStatusSpec.Deleted, true)]
    public void SpecDoesNotMatchCharacter(CharacterStatusSpec spec, bool isActive)
        => Matches(spec, isActive).ShouldBeFalse();

    /// <summary>
    /// ТЕСТ-СТРАЖ: отбор в запросе и отбор в памяти должны отвечать одинаково — иначе фейки в
    /// тестах и настоящий репозиторий разъедутся, и разойдутся молча.
    /// </summary>
    [Theory]
    [InlineData(CharacterStatusSpec.Any)]
    [InlineData(CharacterStatusSpec.Active)]
    [InlineData(CharacterStatusSpec.Deleted)]
    public void PredicateAgreesWithInMemoryVersion(CharacterStatusSpec spec)
    {
        foreach (var isActive in new[] { true, false })
        {
            Matches(spec, isActive).ShouldBe(spec.Matches(isActive));
        }
    }
}
