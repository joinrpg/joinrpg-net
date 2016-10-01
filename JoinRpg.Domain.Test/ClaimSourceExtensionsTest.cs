using JoinRpg.DataModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JoinRpg.Domain.Test
{
  [TestClass]
  public class ClaimSourceExtensionsTest
  {
    [TestMethod]
    public void CharacterGroupIsNeverNpc()
    {
      Assert.IsFalse(new CharacterGroup().IsNpc());
    }
  }
}
