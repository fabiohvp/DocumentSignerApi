
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace DocumentSignerApi.Helpers
{
    public static class ImageHelper
    {
        public static Bitmap CreateBitmapFromText(string textWatermark, Font font, int opacity = 100)
        {
            var bitmap = new Bitmap(1, 1);

            // Create a graphics object to measure the text's width and height.
            var graphics = Graphics.FromImage(bitmap);

            // Create the bmpImage again with the correct size for the text and font.
            bitmap = new Bitmap(
                bitmap,
                new Size(
                    (int)graphics.MeasureString(textWatermark, font).Width,
                    (int)graphics.MeasureString(textWatermark, font).Height
                )
            );

            // Add the colors to the new bitmap.
            graphics = Graphics.FromImage(bitmap);

            // Set Background color
            graphics.Clear(Color.Transparent);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = TextRenderingHint.AntiAlias;

            using (var solidBrush = new SolidBrush(Color.FromArgb(opacity, Color.Black)))
            {
                graphics.DrawString(textWatermark, font, solidBrush, 0, 0);
            }

            graphics.Flush();
            return bitmap;
        }
    }
}
