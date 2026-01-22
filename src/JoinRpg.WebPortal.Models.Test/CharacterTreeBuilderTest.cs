using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Web.Models.Characters;

namespace JoinRpg.WebPortal.Models.Test;

public class CharacterTreeBuilderTest
{
    public MockedProject Mock = new MockedProject();
    [Fact]
    public void IsFirstCopyCorrectlyAssigned()
    {
        var root = new CharacterGroup()
        {
            Project = new Project()
            {
                Characters = [],
                CharacterGroups = [],
            }
        };
        var builder = new CharacterTreeBuilder(root, currentUserId: null, projectInfo: Mock.ProjectInfo);
        var result = builder.Generate();
        result.First().FirstCopy.ShouldBeTrue();
    }
}
