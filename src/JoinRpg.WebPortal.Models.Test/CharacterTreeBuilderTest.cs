using JoinRpg.DataModel;
using JoinRpg.Web.Models.Characters;

namespace JoinRpg.WebPortal.Models.Test;
public class CharacterTreeBuilderTest
{
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
        var builder = new CharacterTreeBuilder(root, currentUserId: null);
        var result = builder.Generate();
        result.First().FirstCopy.ShouldBeTrue();
    }
}
