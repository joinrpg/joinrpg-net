using JoinRpg.DomainTypes.ProjectMetadata;
using static JoinRpg.DomainTypes.Test.ProjectInfoFixture;

namespace JoinRpg.DomainTypes.Test.ProjectMetadata;

/// <summary>
/// Порядок и видимость мастеров в <see cref="ProjectInfo"/> (ADR019, §3 и §5).
/// </summary>
public class ProjectInfoMastersTest
{
    private static readonly UserIdentification First = new(1);
    private static readonly UserIdentification Second = new(2);
    private static readonly UserIdentification Hidden = new(3);
    private static readonly UserIdentification Stranger = new(99);

    private static ProjectInfo Build(string? mastersOrdering = null) => ProjectInfoFixture.Build(
        masters:
        [
            MakeMaster(Second),
            MakeMaster(First, isOwner: true),
            MakeMaster(Hidden, isPublic: false),
        ],
        mastersOrdering: mastersOrdering);

    [Fact]
    public void Masters_WithoutOrdering_AreInCreationOrder()
        => Build().Masters.Select(m => m.UserId).ShouldBe([First, Second, Hidden]);

    [Fact]
    public void Masters_FollowStoredOrdering()
        => Build("3,2,1").Masters.Select(m => m.UserId).ShouldBe([Hidden, Second, First]);

    [Fact]
    public void Masters_NotInOrderingGoToEnd()
        => Build("2").Masters.Select(m => m.UserId).ShouldBe([Second, First, Hidden]);

    [Fact]
    public void WithMethods_KeepMastersOrdering()
        => Build("3,2,1").WithChangedStatus(ProjectLifecycleStatus.Archived)
            .Masters.Select(m => m.UserId).ShouldBe([Hidden, Second, First]);

    [Fact]
    public void MasterOfProject_SeesNonPublicMasters()
        => Build().GetMastersVisibleTo(First).Select(m => m.UserId).ShouldBe([First, Second, Hidden]);

    [Fact]
    public void Stranger_SeesOnlyPublicMasters_InSameOrder()
        => Build("3,2,1").GetMastersVisibleTo(Stranger).Select(m => m.UserId).ShouldBe([Second, First]);

    [Fact]
    public void Anonymous_SeesOnlyPublicMasters()
        => Build().GetMastersVisibleTo(null).Select(m => m.UserId).ShouldBe([First, Second]);

    [Fact]
    public void NonPublicMaster_KeepsAccess()
        => Build().HasMasterAccess(Hidden).ShouldBeTrue();

    [Fact]
    public void RemovedMaster_IsNotVisibleEvenToMasters()
    {
        var project = ProjectInfoFixture.Build(masters:
        [
            MakeMaster(First, isOwner: true),
            MakeMaster(Second) with { Status = ProjectAclStatus.Removed },
        ]);

        project.GetMastersVisibleTo(First).Select(m => m.UserId).ShouldBe([First]);
    }
}
