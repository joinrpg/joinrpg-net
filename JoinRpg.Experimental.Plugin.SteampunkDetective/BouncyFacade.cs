using System;
using System.Text;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;

namespace JoinRpg.Experimental.Plugin.SteampunkDetective
{
  public static class BouncyFacade
  {
    public static string Encrypt(string key, byte[] input)
    {
      byte[] result = BouncyCastleCrypto(true, input, key);
      return Convert.ToBase64String(result);
    }

    public static string Decrypt(string cipher, string key)
    {
      byte[] result = BouncyCastleCrypto(false, Convert.FromBase64String(cipher), key);
      return Encoding.UTF8.GetString(result, 0, result.Length);
    }

    private static byte[] BouncyCastleCrypto(bool forEncrypt, byte[] input, string key)
    {
      var cipher = new PaddedBufferedBlockCipher(new AesEngine());
      byte[] keyByte = Encoding.UTF8.GetBytes(key);
      cipher.Init(forEncrypt, new KeyParameter(keyByte));
      return cipher.DoFinal(input);
    }
  }
}