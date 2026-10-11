using JoinRpg.Dal.Impl.Repositories;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes.ProjectMetadata;
using Shouldly;
using Xunit;

namespace JoinRpg.Dal.Impl.Test;

/// <summary>
/// Статус проекта из трёх флагов и обратно — в SQL-предикат (ADR023). Предикат и <see cref="ProjectLoaderCommon.CreateStatus"/>
/// обязаны совпадать: по <c>Status(ActiveClaimsOpen)</c> отбираются проекты для рекламы и горячих ролей.
/// </summary>
public class ProjectStatusMappingTest
{
    public static TheoryData<bool, bool, bool, ProjectLifecycleStatus> ValidFlags => new()
    {
        // Active, IsAcceptingClaims, IsBlocked
        { true, false, false, ProjectLifecycleStatus.ActiveClaimsClosed },
        { true, true, false, ProjectLifecycleStatus.ActiveClaimsOpen },
        { false, false, false, ProjectLifecycleStatus.Archived },
        { true, false, true, ProjectLifecycleStatus.Blocked },
        { true, true, true, ProjectLifecycleStatus.Blocked },
    };

    [Theory]
    [MemberData(nameof(ValidFlags))]
    public void CreateStatus_MapsFlags(bool active, bool isAcceptingClaims, bool isBlocked, ProjectLifecycleStatus expected)
        => ProjectLoaderCommon.CreateStatus(active, isAcceptingClaims, isBlocked).ShouldBe(expected);

    [Theory]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    public void CreateStatus_InvalidFlags_Throws(bool active, bool isAcceptingClaims, bool isBlocked)
        => Should.Throw<InvalidOperationException>(() => ProjectLoaderCommon.CreateStatus(active, isAcceptingClaims, isBlocked));

    [Theory]
    [MemberData(nameof(ValidFlags))]
    public void StatusPredicate_MatchesOnlyItsOwnStatus(bool active, bool isAcceptingClaims, bool isBlocked, ProjectLifecycleStatus expected)
    {
        var project = new Project { Active = active, IsAcceptingClaims = isAcceptingClaims, IsBlocked = isBlocked };

        foreach (var status in Enum.GetValues<ProjectLifecycleStatus>())
        {
            ProjectPredicates.Status(status).Compile()(project).ShouldBe(status == expected, $"Status({status})");
        }
    }
}
