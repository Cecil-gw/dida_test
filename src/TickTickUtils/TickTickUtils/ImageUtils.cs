using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TickTickUtils;

public class ImageUtils
{
	public static Size GetSmallRect(double maxWidth, double maxHeight, double bitmapWidth, double bitmapHeight)
	{
		double num = Math.Min(bitmapHeight, maxHeight);
		double num2 = ((Math.Abs(num - bitmapHeight) < 0.01) ? bitmapWidth : (bitmapWidth * num / bitmapHeight));
		if (num2 > maxWidth)
		{
			return GetNormalRect(maxWidth, maxHeight, bitmapWidth, bitmapHeight);
		}
		num2 = Math.Max(0.0, num2);
		num = Math.Max(0.0, num);
		return new Size(num2, num);
	}

	public static Size GetNormalRect(double maxWidth, double maxHeight, double bitmapWidth, double bitmapHeight)
	{
		double num = bitmapWidth * bitmapHeight;
		if (num > 6000000.0)
		{
			double num2 = Math.Sqrt(num / 6000000.0);
			bitmapWidth /= num2;
			bitmapHeight /= num2;
		}
		double num3 = Math.Min(maxWidth, bitmapWidth);
		double val = ((Math.Abs(num3 - bitmapWidth) < 0.01) ? bitmapHeight : (bitmapHeight * num3 / bitmapWidth));
		bool flag = bitmapWidth > maxWidth;
		if (bitmapHeight >= 500.0)
		{
			double num4 = bitmapWidth / bitmapHeight;
			if (num4 >= 0.33 && num4 <= 1.0)
			{
				num3 = bitmapWidth * 500.0 / bitmapHeight;
				val = 500.0;
			}
		}
		else if (flag)
		{
			num3 = maxWidth;
			val = bitmapHeight * maxWidth / bitmapWidth;
		}
		num3 = Math.Max(0.0, num3);
		val = Math.Max(0.0, val);
		if (val > maxHeight)
		{
			num3 = num3 * maxHeight / val;
			val = maxHeight;
		}
		return new Size(num3, val);
	}

	public static BitmapImage GetImageByUrl(string path, int width, BitmapCacheOption option = BitmapCacheOption.Default)
	{
		if (File.Exists(path) && new FileInfo(path).Length > 0)
		{
			BitmapImage bitmapImage = new BitmapImage();
			bitmapImage.BeginInit();
			bitmapImage.CacheOption = option;
			if (width > 0)
			{
				bitmapImage.DecodePixelWidth = width;
			}
			bitmapImage.UriSource = new Uri(path);
			try
			{
				bitmapImage.EndInit();
				bitmapImage.Freeze();
			}
			catch (Exception)
			{
				bitmapImage = new BitmapImage();
				bitmapImage.BeginInit();
				if (width > 0)
				{
					bitmapImage.DecodePixelWidth = width;
				}
				bitmapImage.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
				bitmapImage.UriSource = new Uri(path);
				bitmapImage.EndInit();
				bitmapImage.Freeze();
			}
			return bitmapImage;
		}
		return null;
	}

	public static BitmapImage GetResourceImage(string path, int width = -1)
	{
		BitmapImage bitmapImage = new BitmapImage();
		bitmapImage.BeginInit();
		if (width > 0)
		{
			bitmapImage.DecodePixelWidth = width;
		}
		Uri uriSource;
		try
		{
			uriSource = new Uri(path);
		}
		catch (Exception)
		{
			return null;
		}
		bitmapImage.UriSource = uriSource;
		try
		{
			bitmapImage.EndInit();
			bitmapImage.Freeze();
			return bitmapImage;
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static string SavePasteImage(BitmapSource image, string name)
	{
		string text = AppPaths.ImageDir + name;
		using FileStream stream = new FileStream(text, FileMode.Create);
		BmpBitmapEncoder bmpBitmapEncoder = new BmpBitmapEncoder();
		bmpBitmapEncoder.Frames.Add(BitmapFrame.Create(image));
		bmpBitmapEncoder.Save(stream);
		return text;
	}

	public static ImageSource ConvertBase64ToImageSource(string base64String)
	{
		if (string.IsNullOrEmpty(base64String))
		{
			return null;
		}
		MemoryStream value = new MemoryStream(Convert.FromBase64String(base64String));
		return (ImageSource)new ImageSourceConverter().ConvertFrom(value);
	}
}
