using System;
using System.Security.Cryptography;
using JetBrains.Annotations;

namespace JoinRpg.Helpers
{
  public static class CryptFuncs
  {
    private static readonly byte[] Iv =
    {
      1, 2, 3, 4, 5, 6, 7, 8, 1, 2, 3, 4, 5, 6, 7, 8
    };

    public static byte[] EncryptAes([NotNull] this byte[] input, string key)
    {
      if (input == null) throw new ArgumentNullException(nameof(input));
      var algoritm = Aes.Create();

      if (algoritm == null)
      {
        throw new NotImplementedException("Can't create instance of AES alrorithm");
      }

      var transform = algoritm.CreateEncryptor(key.FromHexString(), Iv);
      return transform.TransformFinalBlock(input, 0, input.Length);
    }

    [NotNull]
    public static byte[] DecryptAes([NotNull] this byte[] encryptedBytes, string key)
    {
      if (encryptedBytes == null) throw new ArgumentNullException(nameof(encryptedBytes));
      var algoritm = Aes.Create();

      if (algoritm == null)
      {
        throw new NotImplementedException("Can't create instance of AES alrorithm");
      }

      var transform = algoritm.CreateDecryptor(key.FromHexString(), Iv);
      return transform.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
    }
  }
}
