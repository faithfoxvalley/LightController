using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace LightController.Color
{
    public static class BitmapProcessing
    {

        public static ColorRGB[] ReadImage(byte[] data, int width, double range)
        {
            ColorRGB[] result;
            using (var ms = new MemoryStream(data))
            using (Bitmap orig = new Bitmap(ms))
            {
                if(orig.PixelFormat == PixelFormat.Format24bppRgb)
                {
                    result = GetPixels(orig, width, range);
                }
                else
                {
                    using (Bitmap bmp = new Bitmap(orig.Width, orig.Height, PixelFormat.Format24bppRgb))
                    {
                        using (Graphics gr = Graphics.FromImage(bmp))
                        {
                            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                            gr.DrawImage(orig, rect);
                            result = GetPixels(bmp, width, range);
                        }
                    }
                }
            }
            return result;
        }

        private static ColorRGB[] GetPixels(Bitmap bmp, int width, double range)
        {
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
                throw new ArgumentException("Bitmap must be of format 24bpp RGB.");
            if (bmp.Height < 2 || bmp.Width < width)
                throw new ArgumentException("Bitmap is too small.");

            int bytesPerPixel = 3;
            int[,] pixels = new int[width, 4];
            double pixelsPerIndex = width / (double)bmp.Width;
            range *= bmp.Height;
            int pixelStartY = (bmp.Height / 2) - (int)(range / 2);
            int pixelEndY = (bmp.Height / 2) + (int)(range / 2);

            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, bmp.PixelFormat);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            byte[] rgbValues = new byte[bmp.Height * bmpData.Stride];

            // Copy the RGB values into the array.
            Marshal.Copy(ptr, rgbValues, 0, rgbValues.Length);

            // Set every red value to 255.
            for (int x = 0; x < bmp.Width; x++)
            {
                for (int y = 0; y < bmp.Height; y++)
                {
                    int pixelIndex = (int)(x * pixelsPerIndex);

                    int i = y * bmpData.Stride + x * bytesPerPixel;
                    int red = rgbValues[i + 2];
                    int green = rgbValues[i + 1];
                    int blue = rgbValues[i];

                    pixels[pixelIndex, 0] += red * red;
                    pixels[pixelIndex, 1] += green * green;
                    pixels[pixelIndex, 2] += blue * blue;
                    pixels[pixelIndex, 3]++;
                }
            }

            bmp.UnlockBits(bmpData);

            ColorRGB[] resultPixels = new ColorRGB[width];
            for(int i = 0; i < width; i++)
            {
                int count = pixels[i, 3];
                byte red = (byte)Math.Round(Math.Sqrt(pixels[i, 0] / count));
                byte green = (byte)Math.Round(Math.Sqrt(pixels[i, 1] / count));
                byte blue = (byte)Math.Round(Math.Sqrt(pixels[i, 2] / count));
                resultPixels[i] = new ColorRGB(red, green, blue);
            }
            return resultPixels;
        }
    }
}
