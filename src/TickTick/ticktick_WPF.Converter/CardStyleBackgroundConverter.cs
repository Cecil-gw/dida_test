using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace ticktick_WPF.Converter;

public class CardStyleBackgroundConverter : IValueConverter
{
	private static readonly Dictionary<string, BitmapImage> _imageCache = new Dictionary<string, BitmapImage>();

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		try
		{
			if (value is string[] array && array.Length != 0)
			{
				string text = array[0].Replace("#", "");
				return GetCacheImage("pack://application:,,,/Assets/CountDown/card_style_bg_" + text + ".png");
			}
		}
		catch (Exception)
		{
			return GetCacheImage("pack://application:,,,/Assets/CountDown/card_style_bg_546382.png");
		}
		return GetCacheImage("pack://application:,,,/Assets/CountDown/card_style_bg_546382.png");
	}

	private BitmapImage GetCacheImage(string path)
	{
		if (!_imageCache.TryGetValue(path, out var value))
		{
			BitmapImage bitmapImage = new BitmapImage();
			bitmapImage.BeginInit();
			bitmapImage.UriSource = new Uri(path);
			bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
			bitmapImage.EndInit();
			bitmapImage.Freeze();
			_imageCache[path] = bitmapImage;
			return bitmapImage;
		}
		return value;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
