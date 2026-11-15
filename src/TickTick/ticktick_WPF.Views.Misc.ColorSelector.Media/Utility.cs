using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace ticktick_WPF.Views.Misc.ColorSelector.Media;

internal class Utility
{
	internal static HsbaColor RgbaToHsba(RgbaColor rgba)
	{
		int[] obj = new int[3] { rgba.R, rgba.G, rgba.B };
		Array.Sort(obj);
		int num = obj[2];
		int num2 = obj[0];
		double b = (double)num / 255.0;
		double s = ((num == 0) ? 0.0 : ((double)(num - num2) / (double)num));
		double h = 0.0;
		if (rgba.R != rgba.G || rgba.R != rgba.B)
		{
			if (num == rgba.R && rgba.G >= rgba.B)
			{
				h = (double)(rgba.G - rgba.B) * 60.0 / (double)(num - num2) + 0.0;
			}
			else if (num == rgba.R && rgba.G < rgba.B)
			{
				h = (double)(rgba.G - rgba.B) * 60.0 / (double)(num - num2) + 360.0;
			}
			else if (num == rgba.G)
			{
				h = (double)(rgba.B - rgba.R) * 60.0 / (double)(num - num2) + 120.0;
			}
			else if (num == rgba.B)
			{
				h = (double)(rgba.R - rgba.G) * 60.0 / (double)(num - num2) + 240.0;
			}
		}
		return new HsbaColor(h, s, b, (double)rgba.A / 255.0);
	}

	internal static RgbaColor HsbaToRgba(HsbaColor hsba)
	{
		double num = 0.0;
		double num2 = 0.0;
		double num3 = 0.0;
		int num4 = (int)(hsba.H / 60.0 % 6.0);
		double num5 = hsba.H / 60.0 - (double)num4;
		double num6 = hsba.B * (1.0 - hsba.S);
		double num7 = hsba.B * (1.0 - num5 * hsba.S);
		double num8 = hsba.B * (1.0 - (1.0 - num5) * hsba.S);
		switch (num4)
		{
		case 0:
			num = hsba.B;
			num2 = num8;
			num3 = num6;
			break;
		case 1:
			num = num7;
			num2 = hsba.B;
			num3 = num6;
			break;
		case 2:
			num = num6;
			num2 = hsba.B;
			num3 = num8;
			break;
		case 3:
			num = num6;
			num2 = num7;
			num3 = hsba.B;
			break;
		case 4:
			num = num8;
			num2 = num6;
			num3 = hsba.B;
			break;
		case 5:
			num = hsba.B;
			num2 = num6;
			num3 = num7;
			break;
		}
		return new RgbaColor((int)(255.0 * num), (int)(255.0 * num2), (int)(255.0 * num3), (int)(255.0 * hsba.A));
	}

	internal static BitmapSource LoadImg(string path)
	{
		try
		{
			BitmapImage bitmapImage = new BitmapImage();
			bitmapImage.BeginInit();
			bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
			bitmapImage.StreamSource = new MemoryStream(File.ReadAllBytes(path));
			bitmapImage.EndInit();
			return bitmapImage;
		}
		catch
		{
			return null;
		}
	}

	internal static Bitmap BitmapImageToBitmap(BitmapImage bitmapImage)
	{
		PixelFormat format = PixelFormat.Format32bppArgb;
		Bitmap bitmap = new Bitmap(bitmapImage.PixelWidth, bitmapImage.PixelHeight, format);
		BitmapData bitmapData = bitmap.LockBits(new Rectangle(System.Drawing.Point.Empty, bitmap.Size), ImageLockMode.WriteOnly, format);
		bitmapImage.CopyPixels(Int32Rect.Empty, bitmapData.Scan0, bitmapData.Height * bitmapData.Stride, bitmapData.Stride);
		bitmap.UnlockBits(bitmapData);
		return bitmap;
	}

	internal static BitmapSource BitmapToBitmapSource(Bitmap bitmap)
	{
		return Imaging.CreateBitmapSourceFromHBitmap(bitmap.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
	}

	internal static int GetBrightness(int r, int g, int b)
	{
		return (int)((0.2126 * (double)r + 0.7152 * (double)g + 0.0722 * (double)b) / 2.55);
	}
}
