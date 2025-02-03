using BarRaider.SdTools;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DiscordUnfolded.Classes {
    internal static class ImageTools {

        public static Dictionary<string, Bitmap> cachedPictures = new Dictionary<string, Bitmap>();


        public static Bitmap GetResizedBitmapFromUrl(string imageUrl, bool useCachedImages = true) {
            if (string.IsNullOrEmpty(imageUrl))
                return null;

            if(useCachedImages && cachedPictures.ContainsKey(imageUrl)) 
                return cachedPictures[imageUrl].Clone() as Bitmap;


            try {
                using WebClient webClient = new WebClient();
                using var stream = webClient.OpenRead(imageUrl) ?? throw new Exception("Failed to open image stream.");
                using Image originalImage = Image.FromStream(stream);

                Bitmap resizedBitmap = new Bitmap(144, 144);
                using(Graphics g = Graphics.FromImage(resizedBitmap)) {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.DrawImage(originalImage, 0, 0, 144, 144);
                }
                cachedPictures[imageUrl] = resizedBitmap.Clone() as Bitmap;
                return resizedBitmap;
            }
            catch(Exception ex) {
                Logger.Instance.LogMessage(TracingLevel.ERROR, ex.StackTrace);
                return null;
            }
        }

    }
}
