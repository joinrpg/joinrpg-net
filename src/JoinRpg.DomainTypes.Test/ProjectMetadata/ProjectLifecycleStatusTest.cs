using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.DomainTypes.Test.ProjectMetadata;

/// <summary>
/// «Не в архиве» и «можно менять» — разные вопросы (ADR023). Каждый новый статус обязан
/// ответить на оба явно: тест падает на статусе, которого нет в таблице.
/// </summary>
public class ProjectLifecycleStatusTest
{
    public static TheoryData<ProjectLifecycleStatus, bool, bool> Expected => new()
    {
        // статус, IsArchived, AllowsChanges
        { ProjectLifecycleStatus.ActiveClaimsOpen, false, true },
        { ProjectLifecycleStatus.ActiveClaimsClosed, false, true },
        { ProjectLifecycleStatus.Archived, true, false },
    };

    [Theory]
    [MemberData(nameof(Expected))]
    public void Status_AnswersBothQuestions(ProjectLifecycleStatus status, bool isArchived, bool allowsChanges)
    {
        status.IsArchived().ShouldBe(isArchived);
        status.AllowsChanges().ShouldBe(allowsChanges);
    }

    [Fact]
    public void EveryStatus_IsCovered()
        => Expected.Select(row => (ProjectLifecycleStatus)row[0]!)
            .ShouldBe(Enum.GetValues<ProjectLifecycleStatus>(), ignoreOrder: true);
}
