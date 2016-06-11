using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JoinRpg.Helpers.Test
{
  [TestClass]
  public class CryptTest
  {
    [TestMethod]
    public void TestEncryptDecrypt()
    {
      const string str = "Abracadabre asfdasdfsdf sdf sggtrsgrt";
      var arr = Encoding.UTF8.GetBytes(str);
      const string key = "0102030405060708010203040506070801020304050607080102030405060708";
      var enc = arr.EncryptAes(key);
      var dec = enc.DecryptAes(key);
      Assert.AreEqual(str, Encoding.UTF8.GetString(dec));
    }
  }
}
