using JoinRpg.DomainTypes.Interfaces;

namespace JoinRpg.DomainTypes.Test;

public class IProjectEntityIdExtensionsTest
{
    [Fact]
    public void EnsureProject_AllFromSameProject_ReturnsSameList()
    {
        var projectId = new ProjectIdentification(1);
        var ids = new List<CharacterIdentification>
        {
            new(projectId, 1),
            new(projectId, 2),
        };

        ids.EnsureProject(projectId).ShouldBe(ids);
    }

    [Fact]
    public void EnsureProject_EmptyList_DoesNotThrow()
    {
        var ids = new List<CharacterIdentification>();

        ids.EnsureProject(new ProjectIdentification(1)).ShouldBeEmpty();
    }

    [Fact]
    public void EnsureProject_ForeignProjectId_Throws()
    {
        var ids = new List<CharacterIdentification>
        {
            new(new ProjectIdentification(1), 1),
            new(new ProjectIdentification(2), 2),
        };

        Should.Throw<ArgumentException>(() => ids.EnsureProject(new ProjectIdentification(1)));
    }
}
