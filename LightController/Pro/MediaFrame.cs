using Colourful;
using LightController.Color;
using ProtoBuf;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace LightController.Pro;

[ProtoContract(UseProtoMembersOnly = true)]
public class MediaFrame
{
    [ProtoMember(1)]
    private ColorRGB[] data;
    [ProtoMember(2)]
    private double time;

    private ColorHSV[] hsvData;
    private ColorRGB[] lerpData;

    public double Time => time;

    public ColorRGB[] Data => data;

    public MediaFrame() { }

    public MediaFrame(ColorRGB[] data, TimeSpan time)
    {
        this.data = data;
        this.time = time.TotalSeconds;
    }

    public static MediaFrame CreateFrame(Stream data, TimeSpan time, CancellationToken cancelToken)
    {
        return new MediaFrame(ReadImage(data, cancelToken), time);
    }

    private static ColorRGB[] ReadImage(Stream data, CancellationToken cancelToken)
    {
        ColorRGB[] result;
        using (Bitmap orig = new Bitmap(data))
        {
            if (orig.PixelFormat == PixelFormat.Format24bppRgb)
            {
                result = GetPixels(orig, orig.Width, cancelToken);
            }
            else
            {
                using (Bitmap bmp = new Bitmap(orig.Width, orig.Height, PixelFormat.Format24bppRgb))
                {
                    using (Graphics gr = Graphics.FromImage(bmp))
                    {
                        Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                        gr.DrawImage(orig, rect);
                        result = GetPixels(bmp, bmp.Width, cancelToken);
                    }
                }
            }
        }
        return result;
    }

    public MediaFrame ResizeData(int size)
    {
        double[,] pixels = new double[size, 4];
        double pixelsPerIndex = (double)size / this.data.Length;

        var fromRgb = new ConverterBuilder().FromRGB().ToXYZ(Illuminants.D65).Build();

        for (int i = 0; i < this.data.Length; i++)
        {
            int pixelIndex = (int)(i * pixelsPerIndex);

            ColorRGB rgb = this.data[i];
            var xyz = fromRgb.Convert(RGBColor.FromRGB8Bit(rgb.Red, rgb.Green, rgb.Blue));

            pixels[pixelIndex, 0] += xyz.X;
            pixels[pixelIndex, 1] += xyz.Y;
            pixels[pixelIndex, 2] += xyz.Z;
            pixels[pixelIndex, 3]++;
        }


        var toRgb = new ConverterBuilder().FromXYZ(Illuminants.D65).ToRGB().Build();

        ColorRGB[] resultPixels = new ColorRGB[size];
        for (int i = 0; i < size; i++)
        {
            int count = (int)pixels[i, 3];
            double x = pixels[i, 0];
            double y = pixels[i, 1];
            double z = pixels[i, 2];
            RGBColor rgb = toRgb.Convert(new XYZColor(x / count, y / count, z / count));
            byte red = (byte)(rgb.R * 255);
            byte green = (byte)(rgb.G * 255);
            byte blue = (byte)(rgb.B * 255);

            resultPixels[i] = new ColorRGB(red, green, blue);
        }

        MediaFrame newFrame = new MediaFrame();
        newFrame.data = resultPixels;
        newFrame.time = time;
        return newFrame;
    }

    private static ColorRGB[] GetPixels(Bitmap bmp, int width, CancellationToken cancelToken)
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

        try
        {
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
                    cancelToken.ThrowIfCancellationRequested();

                    int pixelIndex = (int)(x * pixelsPerIndex);

                    int i = y * bmpData.Stride + x * bytesPerPixel;
                    double red = rgbValues[i + 2] / 255d;
                    double green = rgbValues[i + 1] / 255d;
                    double blue = rgbValues[i] / 255d;

                    var xyz = fromRgb.Convert(new RGBColor(red, green, blue));

                    pixels[pixelIndex, 0] += xyz.X;
                    pixels[pixelIndex, 1] += xyz.Y;
                    pixels[pixelIndex, 2] += xyz.Z;
                    pixels[pixelIndex, 3]++;
                }
            }
        }
        finally
        {
            bmp.UnlockBits(bmpData);
        }

        var toRgb = new ConverterBuilder().FromXYZ(Illuminants.D65).ToRGB().Build();

        ColorRGB[] resultPixels = new ColorRGB[width];
        for (int i = 0; i < width; i++)
        {
            int count = (int)pixels[i, 3];
            double x = pixels[i, 0];
            double y = pixels[i, 1];
            double z = pixels[i, 2];
            RGBColor rgb = toRgb.Convert(new XYZColor(x / count, y / count, z / count));
            byte red = (byte)(rgb.R * 255);
            byte green = (byte)(rgb.G * 255);
            byte blue = (byte)(rgb.B * 255);

            resultPixels[i] = new ColorRGB(red, green, blue);
        }
        return resultPixels;
    }

    public ColorRGB[] Interpolate(MediaFrame nextFrame, double percent)
    {
        if (data.Length != nextFrame.data.Length)
            throw new Exception("Cannot interploate between frames of different sizes");

        PrepForInterpolation();
        nextFrame.PrepForInterpolation();

        for (int i = 0; i < data.Length; i++)
            lerpData[i] = (ColorRGB)ColorHSV.Lerp(hsvData[i], nextFrame.hsvData[i], percent);

        return lerpData;
    }

    private void PrepForInterpolation()
    {
        if(hsvData == null)
        {
            hsvData = data.Select(x => (ColorHSV)x).ToArray();
            lerpData = new ColorRGB[data.Length];
        }
    }
}
