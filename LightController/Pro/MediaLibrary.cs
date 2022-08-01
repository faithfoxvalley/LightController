using LightController.Color;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using OpenCvSharp;
using Colourful;

namespace LightController.Pro
{
    public class MediaLibrary
    {
        public ColorRGB[] GetData(int width)
        {
            return null;
        }


        public static ColorRGB[] ReadImage(byte[] data, int width, double range)
        {
            ColorRGB[] result;
            using (var ms = new MemoryStream(data))
            using (Bitmap orig = new Bitmap(ms))
            {
                if (orig.PixelFormat == PixelFormat.Format24bppRgb)
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
            double[,] pixels = new double[width, 4];
            double pixelsPerIndex = width / (double)bmp.Width;

            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, bmp.PixelFormat);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            byte[] rgbValues = new byte[bmp.Height * bmpData.Stride];

            // Copy the RGB values into the array.
            Marshal.Copy(ptr, rgbValues, 0, rgbValues.Length);

            var fromRgb = new ConverterBuilder().FromRGB().ToXYZ(Illuminants.D65).Build();

            // Set every red value to 255.
            for (int x = 0; x < bmp.Width; x++)
            {
                for (int y = 0; y < bmp.Height; y++)
                {
                    int pixelIndex = (int)(x * pixelsPerIndex);

                    int i = y * bmpData.Stride + x * bytesPerPixel;
                    double red = rgbValues[i + 2] / 255.0;
                    double green = rgbValues[i + 1] / 255.0;
                    double blue = rgbValues[i] / 255.0;

                    var xyz = fromRgb.Convert(new RGBColor(red, green, blue));

                    pixels[pixelIndex, 0] += xyz.X;
                    pixels[pixelIndex, 1] += xyz.Y;
                    pixels[pixelIndex, 2] += xyz.Z;
                    pixels[pixelIndex, 3]++;
                }
            }

            bmp.UnlockBits(bmpData);

            var toRgb = new ConverterBuilder().FromXYZ(Illuminants.D65).ToRGB().Build();

            ColorRGB[] resultPixels = new ColorRGB[width];
            for (int i = 0; i < width; i++)
            {
                int count = (int)pixels[i, 3];
                double x = pixels[i, 0];
                double y = pixels[i, 1];
                double z = pixels[i, 2];
                var rgb = toRgb.Convert(new XYZColor(x / count, y / count, z / count));
                byte red = (byte)(rgb.R * 255);
                byte green = (byte)(rgb.G * 255);
                byte blue = (byte)(rgb.B * 255);

                resultPixels[i] = new ColorRGB(red, green, blue);
            }
            return resultPixels;
        }

        public static void DrawHistogram(string image)
        {
            Mat src = Cv2.ImRead(image, ImreadModes.Color);
            src = src.CvtColor(ColorConversionCodes.BGR2HSV_FULL);

            // Histogram view
            const int Width = 260, Height = 200;
            Mat render = new Mat(new OpenCvSharp.Size(Width, Height), MatType.CV_8UC3, Scalar.All(255));

            // Calculate histogram
            Mat hist = new Mat();
            int[] hdims = { 256 }; // Histogram size for each dimension
            Rangef[] ranges = { new Rangef(0, 256), }; // min/max 
            Cv2.CalcHist(
                new Mat[] { src },
                new int[] { 0 },
                null,
                hist,
                1,
                hdims,
                ranges);

            // Get the max value of histogram
            double minVal, maxVal;
            Cv2.MinMaxLoc(hist, out minVal, out maxVal);

            Scalar color = Scalar.All(100);
            // Scales and draws histogram
            hist = hist * (maxVal != 0 ? Height / maxVal : 0.0);
            for (int j = 0; j < hdims[0]; ++j)
            {
                int binW = (int)((double)Width / hdims[0]);
                render.Rectangle(
                    new OpenCvSharp.Point(j * binW, render.Rows - (int)(hist.Get<float>(j))),
                    new OpenCvSharp.Point((j + 1) * binW, render.Rows),
                    color,
                    -1);
            }

            using (new Window("Image", src, WindowFlags.AutoSize | WindowFlags.FreeRatio))
            using (new Window("Histogram", render, WindowFlags.AutoSize | WindowFlags.FreeRatio))
            {
                Cv2.WaitKey();
            }
        }
    }
}
