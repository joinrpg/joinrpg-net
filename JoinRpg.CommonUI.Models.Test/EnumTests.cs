using JoinRpg.TestHelpers;
using Xunit;

namespace JoinRpg.CommonUI.Models.Test
{
  public class EnumTests
  {
    [Fact]
    public void ProblemEnum()
    {
      
      EnumerationTestHelper.CheckEnums<DataModel.CommentExtraAction, CommentExtraAction>();
    }
  }
}
