using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JoinRpg.Helpers.Test
{
  [TestClass]
  public class HexFuncsTest
  {
    [TestMethod]
    public void TestFromHexString()
    {
      var str = "010203040506070E";
      var result = str.FromHexString();
      CollectionAssert.AreEqual(new byte[] {1,2,3,4,5,6,7, 0x0E}, result);
    }
  }
}
