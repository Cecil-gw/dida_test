using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ticktick_WPF.Views.CountDown.Infrastructure;

public class IconService
{
	private static readonly Dictionary<string, BitmapImage> _imageCache = new Dictionary<string, BitmapImage>();

	public static UIElement GetIcon(string iconName, int iconSize = 32)
	{
		if (string.IsNullOrEmpty(iconName))
		{
			iconName = "countdown_countdown";
		}
		try
		{
			BitmapImage iconBitmap = GetIconBitmap(iconName);
			return new Image
			{
				Source = iconBitmap,
				Width = iconSize,
				Height = iconSize,
				Stretch = Stretch.Uniform
			};
		}
		catch
		{
			return CreatePlaceholderIcon();
		}
	}

	private static BitmapImage GetIconBitmap(string iconName)
	{
		if (!iconName.StartsWith("countdown_"))
		{
			iconName = "countdown_" + iconName;
		}
		if (_imageCache.TryGetValue(iconName, out var value))
		{
			return value;
		}
		try
		{
			Uri uriSource = new Uri("pack://application:,,,/Assets/CountDown/Icons/" + iconName + ".png", UriKind.Absolute);
			BitmapImage bitmapImage = new BitmapImage();
			bitmapImage.BeginInit();
			bitmapImage.UriSource = uriSource;
			bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
			bitmapImage.EndInit();
			bitmapImage.Freeze();
			_imageCache[iconName] = bitmapImage;
			return bitmapImage;
		}
		catch
		{
			if (iconName != "countdown_countdown")
			{
				return GetIconBitmap("countdown_countdown");
			}
			throw;
		}
	}

	private static UIElement CreatePlaceholderIcon()
	{
		Grid obj = new Grid
		{
			Width = 32.0,
			Height = 32.0
		};
		Ellipse element = new Ellipse
		{
			Width = 20.0,
			Height = 20.0,
			Fill = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		TextBlock element2 = new TextBlock
		{
			Text = "?",
			FontSize = 14.0,
			FontWeight = FontWeights.Bold,
			Foreground = new SolidColorBrush(Colors.White),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		obj.Children.Add(element);
		obj.Children.Add(element2);
		return obj;
	}

	public static UIElement CreateTextIcon(string text, Brush background)
	{
		if (string.IsNullOrEmpty(text))
		{
			text = "?";
		}
		Grid obj = new Grid
		{
			Width = 32.0,
			Height = 32.0
		};
		Ellipse element = new Ellipse
		{
			Width = 20.0,
			Height = 20.0,
			Fill = (background ?? new SolidColorBrush(Colors.LightBlue)),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		TextBlock element2 = new TextBlock
		{
			Text = text.Substring(0, 1),
			FontSize = 12.0,
			FontWeight = FontWeights.Bold,
			Foreground = new SolidColorBrush(Colors.White),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		obj.Children.Add(element);
		obj.Children.Add(element2);
		return obj;
	}
}
