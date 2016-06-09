using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoinRpg.Helpers.Web
{
  public static class ImageHelpers
  {
    public static string ToEmbeddedImageTag(this Bitmap qrCodeImage)
    {
      Bitmap cropped = qrCodeImage;
      byte[] imgbytes;
      using (MemoryStream stream = new MemoryStream())
      {
        cropped.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        imgbytes = stream.ToArray();
      }
      return $"<img src='data:image/png;base64,{Convert.ToBase64String(imgbytes)}' />";
    }
  }
}
