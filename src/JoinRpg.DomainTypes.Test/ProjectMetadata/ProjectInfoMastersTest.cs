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

    private static ProjectInfo BuildWithRemovedSecond(ProjectGroupTree? groupTree = null) => ProjectInfoFixture.Build(
        groupTree: groupTree,
        masters:
        [
            MakeMaster(First, isOwner: true),
            MakeMaster(Second) with { Status = ProjectAclStatus.Removed },
            MakeMaster(Hidden),
        ]);

    [Fact]
    public void GetMasterById_FindsRemovedMaster()
        => BuildWithRemovedSecond().GetMasterById(Second).UserId.ShouldBe(Second);

    [Fact]
    public void GetMasterById_Unknown_ThrowsWithMasterAndProject()
    {
        var exception = Should.Throw<KeyNotFoundException>(() => Build().GetMasterById(Stranger));

        exception.Message.ShouldContain(Stranger.ToString());
        exception.Message.ShouldContain(ProjectId.ToString());
    }

    [Fact]
    public void GetActiveMasterOrDefault_RemovedMaster_IsNull()
        => BuildWithRemovedSecond().GetActiveMasterOrDefault(Second).ShouldBeNull();

    [Fact]
    public void SelectResponsibleMaster_SkipsRuleOfRemovedMaster()
    {
        // Группа 3 вложена в 2: правило ближайшей группы (снятый мастер) пропускается, берётся правило группы 2.
        var project = BuildWithRemovedSecond(MakeGroupTree(
            new Dictionary<int, int[]> { [2] = [1], [3] = [2] },
            responsibleMasterByGroup: new Dictionary<int, UserIdentification> { [2] = Hidden, [3] = Second }));

        project.SelectResponsibleMaster([GroupId(3), GroupId(2), RootGroupId]).UserId.ShouldBe(Hidden);
    }

    [Fact]
    public void SelectResponsibleMaster_OnlyRuleOfRemovedMaster_FallsBackToDefault()
    {
        var project = BuildWithRemovedSecond(MakeGroupTree(
            new Dictionary<int, int[]> { [2] = [1] },
            responsibleMasterByGroup: new Dictionary<int, UserIdentification> { [2] = Second }));

        project.SelectResponsibleMaster([GroupId(2), RootGroupId]).UserId.ShouldBe(First);
    }
}
