using JoinRpg.Tools.Drp.Common;

namespace JoinRpg.Tools.RestoreProjectShells.Test;

public class ProjectShellPlannerTest
{
    private const int BackupMax = 1500;
    private const long ShiftedSeed = 20000;
    private const int Creator = 77;

    private static LostProjectsCsvRow Row(int projectId, bool create = true, string? name = "Игра", int? creator = Creator)
        => new(Line: projectId, projectId, create, name, creator);

    private static MainDatabaseState State(
        IReadOnlyDictionary<int, string>? existingProjects = null,
        IReadOnlySet<int>? users = null,
        long? shiftedSeed = ShiftedSeed)
        => new(existingProjects ?? new Dictionary<int, string>(), users ?? new HashSet<int> { Creator }, shiftedSeed);

    private static ShellDecision PlanOne(LostProjectsCsvRow row, MainDatabaseState? state = null)
        => new ProjectShellPlanner(BackupMax, state ?? State()).Plan([row]).ShouldHaveSingleItem();

    [Fact]
    public void ValidRowIsCreated()
    {
        var decision = PlanOne(Row(1600));
        decision.Kind.ShouldBe(ShellDecisionKind.Create);
        decision.Name.ShouldBe("Игра");
        decision.CreatorUserId.ShouldBe(Creator);
    }

    [Fact]
    public void RowMarkedNoIsNotCreated()
        => PlanOne(Row(1600, create: false)).Kind.ShouldBe(ShellDecisionKind.NotRequested);

    [Fact]
    public void RowMarkedNoIsNotCheckedFurther()
        => PlanOne(Row(1600, create: false, name: null, creator: null), State(shiftedSeed: null))
            .Kind.ShouldBe(ShellDecisionKind.NotRequested);

    [Theory]
    [InlineData(1500)]
    [InlineData(10)]
    public void IdNotAboveBackupMaxIsRejected(int projectId)
        => PlanOne(Row(projectId)).Kind.ShouldBe(ShellDecisionKind.NotAboveBackupMax);

    [Fact]
    public void MayProjectWithSameIdIsNotReportedAsAlreadyCreated()
        => PlanOne(Row(1400), State(existingProjects: new Dictionary<int, string> { [1400] = "Майская" }))
            .Kind.ShouldBe(ShellDecisionKind.NotAboveBackupMax);

    [Fact]
    public void ExistingProjectIsAlreadyCreated()
    {
        var decision = PlanOne(Row(1600), State(existingProjects: new Dictionary<int, string> { [1600] = "Игра" }));
        decision.Kind.ShouldBe(ShellDecisionKind.AlreadyExists);
        decision.MssqlName.ShouldBe("Игра");
    }

    [Fact]
    public void RerunAfterCounterRestoreIsAlreadyCreated()
        => PlanOne(Row(1600), State(existingProjects: new Dictionary<int, string> { [1600] = "Игра" }, shiftedSeed: null))
            .Kind.ShouldBe(ShellDecisionKind.AlreadyExists);

    [Fact]
    public void DuplicateIdsAreBothRejected()
    {
        var decisions = new ProjectShellPlanner(BackupMax, State()).Plan([Row(1600), Row(1600, name: "Другая")]);
        decisions.Count.ShouldBe(2);
        decisions.ShouldAllBe(d => d.Kind == ShellDecisionKind.DuplicateId);
    }

    [Fact]
    public void DuplicateMarkedNoDoesNotBlockTheOther()
    {
        var decisions = new ProjectShellPlanner(BackupMax, State()).Plan([Row(1600), Row(1600, create: false)]);
        decisions.ShouldContain(d => d.Kind == ShellDecisionKind.DuplicateId);
        decisions.ShouldContain(d => d.Kind == ShellDecisionKind.NotRequested);
    }

    [Fact]
    public void RowWithoutNameIsRejected()
        => PlanOne(Row(1600, name: null)).Kind.ShouldBe(ShellDecisionKind.NoName);

    [Fact]
    public void RowWithoutCreatorIsRejected()
        => PlanOne(Row(1600, creator: null)).Kind.ShouldBe(ShellDecisionKind.NoCreator);

    [Fact]
    public void CreatorMissingInUsersIsRejected()
        => PlanOne(Row(1600, creator: 999)).Kind.ShouldBe(ShellDecisionKind.CreatorNotFound);

    [Fact]
    public void WithoutShiftedCounterNothingIsCreated()
        => PlanOne(Row(1600), State(shiftedSeed: null)).Kind.ShouldBe(ShellDecisionKind.CounterNotShifted);

    [Theory]
    [InlineData(20000)]
    [InlineData(25000)]
    public void IdNotBelowShiftedCounterIsRejected(int projectId)
        => PlanOne(Row(projectId)).Kind.ShouldBe(ShellDecisionKind.NotBelowCounter);

    [Fact]
    public void DecisionsAreOrderedById()
        => new ProjectShellPlanner(BackupMax, State()).Plan([Row(1700), Row(1600), Row(1650)])
            .Select(d => d.ProjectId).ShouldBe([1600, 1650, 1700]);

    [Fact]
    public void OnlyRequestedCreatorsAreLookedUp()
        => ProjectShellPlanner.CreatorIdsToLookUp([Row(1600, creator: 5), Row(1601, creator: 5), Row(1602, create: false, creator: 6), Row(1603, creator: null)])
            .ShouldBe([5]);
}
