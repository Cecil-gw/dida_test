using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ticktick_WPF.Converter;

public class ImageKeyConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		Rect viewbox = new Rect(new Point(0.0, 0.23), new Point(1.0, 0.58));
		BitmapImage bitmapImage;
		if (values.Length != 0 && values.Length == 3)
		{
			string text = values[0] as string;
			string text2 = values[1] as string;
			if (values[2] is Rect)
			{
				Rect valueOrDefault = (values[2] as Rect?).GetValueOrDefault();
				if (valueOrDefault.Width > 0.0)
				{
					viewbox = valueOrDefault;
				}
			}
			if (string.IsNullOrEmpty(text2))
			{
				bitmapImage = (string.IsNullOrEmpty(text) ? new BitmapImage(new Uri("pack://application:,,,/Assets/CountDown/preset_countdown.jpg")) : new BitmapImage(new Uri("pack://application:,,,/Assets/CountDown/preset_" + text + ".jpg")));
			}
			else if (File.Exists(text2))
			{
				using (FileStream streamSource = new FileStream(text2, FileMode.Open, FileAccess.Read))
				{
					bitmapImage = new BitmapImage();
					bitmapImage.BeginInit();
					bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
					bitmapImage.StreamSource = streamSource;
					bitmapImage.EndInit();
					bitmapImage.Freeze();
				}
				viewbox = new Rect(new Point(0.0, 0.0), new Point(1.0, 1.0));
			}
			else
			{
				bitmapImage = new BitmapImage(new Uri("pack://application:,,,/Assets/CountDown/preset_countdown.jpg"));
			}
		}
		else
		{
			bitmapImage = new BitmapImage(new Uri("pack://application:,,,/Assets/CountDown/preset_countdown.jpg"));
		}
		return new ImageBrush(bitmapImage)
		{
			Stretch = Stretch.UniformToFill,
			Viewbox = viewbox
		};
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
