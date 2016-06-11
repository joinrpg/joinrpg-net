using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace JoinRpg.Helpers.Web
{
  public static class ImageHelpers
  {
    public static string ToEmbeddedImageTag(this Bitmap qrCodeImage)
    {
      byte[] imgbytes;
      using (var stream = new MemoryStream())
      {
        qrCodeImage.Save(stream, ImageFormat.Png);
        imgbytes = stream.ToArray();
      }
      return $"<img src='data:image/png;base64,{Convert.ToBase64String(imgbytes)}' />";
    }
  }
}
