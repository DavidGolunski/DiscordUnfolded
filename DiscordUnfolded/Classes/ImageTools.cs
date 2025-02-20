using BarRaider.SdTools;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
                Logger.Instance.LogMessage(TracingLevel.ERROR, "Error while retrieving Bitmap with URL: " +  imageUrl + " ErrorMessage: " + ex.StackTrace);
                return null;
            }
        }


        public static Bitmap GetBitmapFromFilePath(string filePath) {
            if(!File.Exists(filePath)) {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"File not found: {filePath}");
                return null;
            }

            return new Bitmap(filePath);
        }


        // takes two bitmaps. Draws the second bitmap on top of the first Bitmap
        public static Bitmap MergeBitmaps(Bitmap bottomBitmap, Bitmap topBitmap) {
            // Create a new bitmap with the size of the bottom bitmap
            Bitmap mergedBitmap = new Bitmap(bottomBitmap.Width, bottomBitmap.Height);

            using(Graphics g = Graphics.FromImage(mergedBitmap)) {
                // Draw the bottom bitmap first
                g.DrawImage(bottomBitmap, 0, 0);

                // Draw the top bitmap on top (it will be placed at (0,0) by default)
                g.DrawImage(topBitmap, 0, 0);
            }

            return mergedBitmap;
        }


        // adds text to a 144x144 bitmap image. The number of characters in each "line" of the string must not exceed 8
        public static Bitmap AddTextToBitmap(Bitmap bitmap, string input, Color textColor) {
            List<string> lines = input.Split('\n').ToList();


            using(Graphics graphics = Graphics.FromImage(bitmap))
            using(Font font = new Font("Arial", 20, FontStyle.Bold))
            using(Brush brush = new SolidBrush(textColor)) {
                // Enable anti-aliasing for smoother text
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                int imageWidth = bitmap.Width;
                int imageHeight = bitmap.Height;
                int padding = 16; // Space from edges

                // Calculate total text height
                int lineHeight = 25; // Estimated line height
                int totalTextHeight = lines.Count * lineHeight;

                // Determine starting Y position to vertically center the text
                int startY = (imageHeight - totalTextHeight) / 2;

                // Ensure text doesn't go beyond padding limits
                startY = Math.Max(startY, padding);

                foreach(string line in lines) {
                    // Measure text width
                    SizeF textSize = graphics.MeasureString(line, font);
                    int textWidth = (int) textSize.Width;

                    // Calculate X position to center text horizontally
                    int x = (imageWidth - textWidth) / 2;

                    // Ensure the text stays within padding limits
                    x = Math.Max(x, padding);

                    graphics.DrawString(line, font, brush, new PointF(x, startY));
                    startY += lineHeight;
                }
            }

            return bitmap; // Return the modified bitmap instead of saving it
        }


        // splits a string into "maxLength" segments. Will try to split at spaces " " if possible
        public static string SplitString(string input, int maxLength) {
            if(string.IsNullOrEmpty(input) || maxLength < 1)
                return string.Empty;

            StringBuilder result = new StringBuilder();
            List<string> words = input.Split(' ').ToList();  // Split by spaces
            string currentLine = "";

            // if a word is longer than maxLength, then split the word into multple words
            List<string> wordsSplitByMaxLength = new List<string>();
            for(int i = 0; i < words.Count; i++) {
                if(words[i].Length > maxLength) {
                    for(int j = 0; j < words[i].Length; j += maxLength) {
                        // Take a substring of maxLength or the remaining part of the word
                        string part = words[i].Substring(j, Math.Min(maxLength, words[i].Length - j));
                        wordsSplitByMaxLength.Add(part);
                    }
                }
                else {
                    wordsSplitByMaxLength.Add(words[i]);
                }
            }

            foreach(var word in wordsSplitByMaxLength) {
                if(currentLine.Length == 0) {
                    currentLine = word;
                }
                else if(currentLine.Length + word.Length + 1 <= maxLength) {
                    // Add to the current line
                    if(currentLine.Length > 0)
                        currentLine += " ";
                    currentLine += word;
                }
                else {
                    result.Append(currentLine.Trim());
                    result.Append("\n");
                    currentLine = word;
                }
            }

            // Append the last line if not empty
            if(!string.IsNullOrEmpty(currentLine)) {
                result.Append(currentLine.Trim());
            }

            return result.ToString().Trim();
            ;
        }
    }
}
