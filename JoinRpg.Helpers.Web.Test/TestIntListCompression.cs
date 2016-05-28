using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JoinRpg.Helpers.Web.Test
{
    [TestClass]
    public class TestIntListCompression
    {
      [TestMethod]
      public void Test123()
      {
        var list = new[] {1, 2, 3};
        var compressed = list.CompressIdList();
        var result = compressed.UnCompressIdList().ToArray();
        Assert.AreEqual(list.Length, result.Length);
        for (var i = 0; i < list.Length; i++)
        {
          Assert.AreEqual(list[i], result[i]);
        }
      }
    }
}
