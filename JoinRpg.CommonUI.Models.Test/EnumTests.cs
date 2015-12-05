using JoinRpg.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JoinRpg.CommonUI.Models.Test
{
  [TestClass]
  public class EnumTests
  {
    [TestMethod]
    public void ProblemEnum()
    {
      
      EnumerationTestHelper.CheckEnums<DataModel.CommentExtraAction, CommentExtraAction>();
    }
  }
}
