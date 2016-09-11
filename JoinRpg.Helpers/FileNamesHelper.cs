using System.IO;

namespace JoinRpg.Helpers
{
  public static class FileNamesHelper
  {
    /// <summary>
    /// Change all incorrect symbols in filename to _
    /// See http://stackoverflow.com/a/12800424/408666
    /// </summary>
    /// <param name="filename"></param>
    /// <returns></returns>
    public static string ToSafeFileName(this string filename) => string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
  }
}
