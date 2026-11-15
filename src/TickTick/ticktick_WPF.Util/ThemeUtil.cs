using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util.Files;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Misc.ColorSelector;

namespace ticktick_WPF.Util;

public static class ThemeUtil
{
	public struct Dpi
	{
		public double X { get; set; }

		public double Y { get; set; }

		public Dpi(double x, double y)
		{
			X = x;
			Y = y;
		}
	}

	public static ResourceDictionary DarkResource;

	public static ResourceDictionary LightResource;

	private static readonly Dictionary<string, (BitmapImage, DateTime, DateTime)> LoadedImages;

	private static readonly Dictionary<string, BitmapImage> LargeImages;

	private static readonly BitmapImage ImageLoading;

	private static readonly BitmapImage ImageLoadFailed;

	private static readonly BitmapImage ImageLoadingDark;

	private static readonly BitmapImage ImageLoadFailedDark;

	private static readonly HashSet<string> Themes;

	private static Dictionary<string, SolidColorBrush> _brushes;

	static ThemeUtil()
	{
		DarkResource = new ResourceDictionary
		{
			Source = new Uri("Resource/Themes/Dark.xaml", UriKind.Relative)
		};
		LightResource = new ResourceDictionary
		{
			Source = new Uri("Resource/Themes/Light.xaml", UriKind.Relative)
		};
		LoadedImages = new Dictionary<string, (BitmapImage, DateTime, DateTime)>();
		LargeImages = new Dictionary<string, BitmapImage>();
		ImageLoading = new BitmapImage(new Uri("pack://application:,,,/Assets/Attachment_Icon/ImageLoading.png"));
		ImageLoadFailed = new BitmapImage(new Uri("pack://application:,,,/Assets/Attachment_Icon/ImageLoadFailed.png"));
		ImageLoadingDark = new BitmapImage(new Uri("pack://application:,,,/Assets/Attachment_Icon/ImageLoadingDark.png"));
		ImageLoadFailedDark = new BitmapImage(new Uri("pack://application:,,,/Assets/Attachment_Icon/ImageLoadFailedDark.png"));
		Themes = new HashSet<string>
		{
			"white", "blue", "pink", "black", "green", "mint", "gray", "yellow", "dark", "newdark",
			"olddark", "darkblue", "darkcyan", "darkgreen", "darkmint", "darkyellow", "darkpink", "darkpurple", "darkcustom", "seablizz",
			"matcha", "peach", "lilac", "sunshine", "ebony", "navy", "spring", "summer", "autumn", "winter",
			"beijing", "hangzhou", "london", "moscow", "sanfrancisco", "seoul", "shanghai", "sydney", "tokyo", "newyork",
			"desert", "structure", "blacksea", "leaves", "birds", "guangzhou", "shenzhen", "cairo", "losangeles", "custom",
			"dawn", "blossom", "frozen", "meadow", "silence"
		};
		_brushes = new Dictionary<string, SolidColorBrush>();
		InitPlaceholder();
	}

	public static SolidColorBrush GetPrimaryColor(double opacity)
	{
		object obj = Application.Current?.FindResource("ColorPrimary");
		if (obj != null && obj is Color color)
		{
			return new SolidColorBrush(color)
			{
				Opacity = opacity
			};
		}
		return new SolidColorBrush(Colors.Transparent);
	}

	public static Size GetImageSize(string path)
	{
		double width = 1080.0;
		double height = 608.0;
		if (FileUtils.FileEmptyOrNotExists(path))
		{
			return new Size(width, height);
		}
		try
		{
			if (LoadedImages.ContainsKey(path))
			{
				BitmapImage item = LoadedImages[path].Item1;
				width = item.Width;
				height = item.Height;
			}
			else
			{
				using FileStream bitmapStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				BitmapDecoder bitmapDecoder = BitmapDecoder.Create(bitmapStream, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.Default);
				height = bitmapDecoder.Frames[0].Height;
				width = bitmapDecoder.Frames[0].Width;
			}
		}
		catch (Exception arg)
		{
			UtilLog.Warn($"ThemeUtil.GetImageSize Exception {arg}");
		}
		return new Size(width, height);
	}

	public static void TryClearImageCached(bool force = false)
	{
		List<string> list = new List<string>();
		DateTime dateTime = DateTime.Now.AddMinutes((!force) ? (-2) : 0);
		foreach (KeyValuePair<string, (BitmapImage, DateTime, DateTime)> loadedImage in LoadedImages)
		{
			if (loadedImage.Value.Item3 < dateTime)
			{
				list.Add(loadedImage.Key);
			}
		}
		list.ForEach(delegate(string x)
		{
			LoadedImages.Remove(x);
		});
		LargeImages.Clear();
	}

	public static BitmapImage GetImage(string path, int width = -1, BitmapCacheOption option = BitmapCacheOption.Default)
	{
		if (FileUtils.FileEmptyOrNotExists(path))
		{
			return GetPlaceholderAttachmentImage();
		}
		DateTime dateTime = FileUtils.FileModifyTime(path);
		if (LargeImages.TryGetValue(path, out var value))
		{
			return value;
		}
		string key = path + width;
		if (LoadedImages.ContainsKey(key))
		{
			var (bitmapImage, dateTime2, _) = LoadedImages[key];
			if (dateTime2 == dateTime)
			{
				LoadedImages[key] = (bitmapImage, dateTime2, DateTime.Now);
				return bitmapImage;
			}
		}
		BitmapImage bitmapImage2 = ImageUtils.GetImageByUrl(path, width, option);
		double num = bitmapImage2.Width * bitmapImage2.Height;
		if (num > 8000000.0)
		{
			double num2 = Math.Sqrt(num / 8000000.0);
			width = (int)(bitmapImage2.Width / num2);
			bitmapImage2 = new BitmapImage();
			bitmapImage2.CacheOption = BitmapCacheOption.OnLoad;
			bitmapImage2.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
			bitmapImage2.BeginInit();
			bitmapImage2.UriSource = new Uri(path);
			bitmapImage2.DecodePixelWidth = width;
			bitmapImage2.EndInit();
			bitmapImage2.Freeze();
			LargeImages[path] = bitmapImage2;
		}
		else
		{
			LoadedImages[key] = (bitmapImage2, dateTime, DateTime.Now);
		}
		return bitmapImage2;
	}

	private static void InitPlaceholder()
	{
		if (ImageLoading.CanFreeze)
		{
			ImageLoading.Freeze();
		}
		if (ImageLoadFailed.CanFreeze)
		{
			ImageLoadFailed.Freeze();
		}
		if (ImageLoadingDark.CanFreeze)
		{
			ImageLoadingDark.Freeze();
		}
		if (ImageLoadFailed.CanFreeze)
		{
			ImageLoadFailedDark.Freeze();
		}
	}

	public static BitmapImage GetPlaceholderAttachmentImage(bool isFailed = false)
	{
		if (string.Compare(LocalSettings.Settings.ThemeId, "dark", StringComparison.OrdinalIgnoreCase) == 0)
		{
			return isFailed ? ImageLoadFailedDark : ImageLoadingDark;
		}
		return isFailed ? ImageLoadFailed : ImageLoading;
	}

	public static string GetRandomColor()
	{
		int count = ColorItemSelector.MoreColorList.Count;
		int val = new Random().Next(0, count - 1);
		val = Math.Min(Math.Max(val, 0), count - 1);
		return ColorItemSelector.MoreColorList[val];
	}

	public static Color GetColorValue(string key)
	{
		object obj = Application.Current?.TryFindResource(key);
		if (obj != null)
		{
			return (Color)obj;
		}
		return Colors.Transparent;
	}

	public static Dpi GetDpiFromVisual(Visual visual)
	{
		PresentationSource presentationSource = PresentationSource.FromVisual(App.Window);
		double x = 96.0;
		double y = 96.0;
		if (presentationSource?.CompositionTarget != null)
		{
			x = 96.0 * presentationSource.CompositionTarget.TransformToDevice.M11;
			y = 96.0 * presentationSource.CompositionTarget.TransformToDevice.M22;
		}
		return new Dpi(x, y);
	}

	public static WriteableBitmap SaveAsWriteableBitmap(FrameworkElement element)
	{
		if (element == null)
		{
			return null;
		}
		Dpi dpiFromVisual = GetDpiFromVisual(element);
		double num = dpiFromVisual.X / 96.0;
		double num2 = dpiFromVisual.X / 96.0;
		Size size = ((double.IsNaN(element.ActualHeight) || double.IsNaN(element.ActualWidth)) ? new Size(element.Width, element.Height) : new Size(element.Width, element.Height));
		element.Measure(size);
		element.Arrange(new Rect(size));
		element.UpdateLayout();
		RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap((int)(size.Width * num), (int)(size.Height * num2), dpiFromVisual.X, dpiFromVisual.Y, PixelFormats.Pbgra32);
		renderTargetBitmap.Render(element);
		return new WriteableBitmap(renderTargetBitmap);
	}

	public static bool SaveBitmapToPng(string filePath, BitmapSource bitmapSource)
	{
		try
		{
			PngBitmapEncoder pngBitmapEncoder = new PngBitmapEncoder();
			pngBitmapEncoder.Frames.Add(BitmapFrame.Create(bitmapSource));
			using FileStream stream = new FileStream(filePath, FileMode.Create);
			pngBitmapEncoder.Save(stream);
		}
		catch (Exception ex)
		{
			UtilLog.Error("ThemeUtil.SaveBitmapToJpg failed" + ex.Message);
			return false;
		}
		return true;
	}

	public static SolidColorBrush GetColorInString(string color)
	{
		try
		{
			if (color == null)
			{
				return Brushes.Transparent;
			}
			if ((color.Length == 6 || color.Length == 8) && !color.StartsWith("#"))
			{
				color = "#" + color;
			}
			return new BrushConverter().ConvertFromString(color) as SolidColorBrush;
		}
		catch (Exception)
		{
			UtilLog.Info("GetColorInString Error + color:" + color);
			return Brushes.Transparent;
		}
	}

	public static SolidColorBrush GetAlphaColor(string color, int percent)
	{
		if (color.StartsWith("#"))
		{
			string text = Convert.ToString(256 * percent / 100, 16).ToUpper();
			if (text.Length == 1)
			{
				text = "0" + text;
			}
			text = "#" + text;
			if (text.Length == 3)
			{
				color = color.Replace("#", "");
				if (color.Length > 6)
				{
					color = color.Substring(color.Length - 6, 6);
				}
				color = text + color;
				return GetColorInString(color);
			}
		}
		return GetColorInString(color);
	}

	public static bool IsEmptyColor(string color)
	{
		if (!string.IsNullOrEmpty(color))
		{
			return color.ToLowerInvariant() == "transparent";
		}
		return true;
	}

	public static Style GetStyle(string key)
	{
		if (Application.Current != null && Application.Current?.TryFindResource(key) is Style result)
		{
			return result;
		}
		return null;
	}

	public static SolidColorBrush GetColor(ResourceDictionary dict, string key)
	{
		if (dict.Contains(key))
		{
			return dict[key] as SolidColorBrush;
		}
		return GetColor(key);
	}

	public static SolidColorBrush TryGetColor(string key, SolidColorBrush defaultColor = null)
	{
		try
		{
			if (Application.Current != null && Application.Current?.TryFindResource(key) is SolidColorBrush result)
			{
				return result;
			}
			return defaultColor ?? new SolidColorBrush(Colors.Black);
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static SolidColorBrush GetColor(string key, SolidColorBrush defaultColor = null)
	{
		if (Application.Current != null && Application.Current?.TryFindResource(key) is SolidColorBrush result)
		{
			return result;
		}
		return defaultColor ?? new SolidColorBrush(Colors.Black);
	}

	public static SolidColorBrush GetTodayColorInMenu(FrameworkElement context)
	{
		switch (LocalSettings.Settings.ThemeId)
		{
		case "Yellow":
		case "Pink":
		case "Green":
			return new SolidColorBrush(Colors.White);
		default:
			return GetColor("TextAccentColor");
		}
	}

	public static SolidColorBrush GetDateSelectedColorInMenu(FrameworkElement context)
	{
		switch (LocalSettings.Settings.ThemeId)
		{
		case "Yellow":
		case "Pink":
		case "Green":
			return GetColor("LightBorderColor", context);
		default:
			return GetPrimaryColor(1.0);
		}
	}

	public static SolidColorBrush GetColor(string key, FrameworkElement context, SolidColorBrush defaultColor = null)
	{
		if (context != null)
		{
			if (context.TryFindResource(key) is SolidColorBrush result)
			{
				return result;
			}
		}
		else if (Application.Current != null && Application.Current?.TryFindResource(key) is SolidColorBrush result2)
		{
			return result2;
		}
		return defaultColor ?? new SolidColorBrush(Colors.Black);
	}

	public static SolidColorBrush GetColor(string key, ResourceDictionary dict, SolidColorBrush defaultColor = null)
	{
		if (dict != null && dict.Contains(key) && dict[key] is SolidColorBrush result)
		{
			return result;
		}
		if (Application.Current != null && Application.Current?.TryFindResource(key) is SolidColorBrush result2)
		{
			return result2;
		}
		return defaultColor ?? new SolidColorBrush(Colors.Black);
	}

	public static void SetTheme(string themeId, FrameworkElement context, bool isWhite = false)
	{
		switch (themeId)
		{
		case "light":
			SetThemeLight(context, isWhite);
			break;
		case "dark":
			SetThemeDark(context);
			break;
		case "auto":
			if (IsSystemDark())
			{
				SetThemeDark(context);
			}
			else
			{
				SetThemeLight(context, isWhite);
			}
			break;
		}
	}

	private static void SetThemeLight(FrameworkElement context, bool isWhite = false)
	{
		AddResourceDictionary(context, "Resource/Themes/Light.xaml");
		AddResourceDictionary(context, "Resource/Themes/LightNecessaryColor.xaml");
		if (isWhite)
		{
			AddResourceDictionary(context, "Resource/Themes/White.xaml");
		}
		else if (!string.IsNullOrWhiteSpace(LocalSettings.Settings.ThemeId) && !ThemeKey.IsDarkTheme(LocalSettings.Settings.ThemeId) && LocalSettings.Settings.ThemeId != "Custom")
		{
			string src = "Resource/Themes/" + LocalSettings.Settings.ThemeId + ".xaml";
			AddResourceDictionary(context, src);
		}
		AddResourceDictionary(context, "Resource/icons_light.xaml");
	}

	private static void SetThemeDark(FrameworkElement context)
	{
		AddResourceDictionary(context, "Resource/Themes/Dark.xaml");
		AddResourceDictionary(context, "Resource/icons_dark.xaml");
		string lastDarkTheme = LocalSettings.Settings.ExtraSettings.LastDarkTheme;
		if (!string.IsNullOrEmpty(lastDarkTheme))
		{
			AddResourceDictionary(context, "Resource/Themes/" + lastDarkTheme + ".xaml");
			if (lastDarkTheme == "DarkCustom")
			{
				SetDarkCustomColor(context);
			}
		}
	}

	private static void AddResourceDictionary(FrameworkElement context, string src)
	{
		context.Resources.MergedDictionaries.Add(new ResourceDictionary
		{
			Source = new Uri(src, UriKind.Relative)
		});
	}

	public static Geometry GetTaskIconGeometry(DisplayType type, int status, string kind, int calKind)
	{
		switch (status)
		{
		case -1:
			return Utils.GetGeometry("Ic.TaskType.Abandon");
		default:
			return Utils.GetGeometry("Ic.TaskType.Completed");
		case 0:
			switch (type)
			{
			case DisplayType.Note:
				return Utils.GetGeometry("Ic.TaskType.Note");
			case DisplayType.Event:
				return calKind switch
				{
					1 => Utils.GetGeometry("Ic.TaskType.Google"), 
					5 => Utils.GetGeometry("Ic.TaskType.Caldav"), 
					2 => Utils.GetGeometry("Ic.TaskType.Outlook"), 
					3 => Utils.GetGeometry("Ic.TaskType.Exchange"), 
					4 => Utils.GetGeometry("Ic.TaskType.ICloud"), 
					_ => Utils.GetGeometry("Ic.TaskType.Url"), 
				};
			case DisplayType.Agenda:
				return Utils.GetGeometry("Ic.TaskType.Schedule");
			case DisplayType.Task:
				if (!(kind == "TEXT"))
				{
					if (!(kind == "CHECKLIST"))
					{
						break;
					}
					return Utils.GetGeometry("Ic.TaskType.CheckitemTitle");
				}
				return Utils.GetGeometry("Ic.TaskType.Uncompleted");
			case DisplayType.CheckItem:
				return Utils.GetGeometry("Ic.TaskType.Checkitem");
			case DisplayType.Course:
				return Utils.GetGeometry("Ic.TaskType.Course");
			case DisplayType.Countdown:
				return Utils.GetGeometry("Ic.CalendarType.Countdown");
			}
			return Utils.GetGeometry("Ic.TaskType.Uncompleted");
		}
	}

	public static bool ExistTheme(string themeId)
	{
		return Themes.Contains(themeId.ToLower());
	}

	public static string GetDarkColorString(string key)
	{
		ResourceDictionary resourceDictionary = new ResourceDictionary
		{
			Source = new Uri("Resource/Themes/Light.xaml", UriKind.Relative)
		};
		if (resourceDictionary.Contains(key))
		{
			object obj = resourceDictionary[key];
			if (obj is Color)
			{
				_ = (Color)obj;
			}
		}
		return null;
	}

	public static string GetColorString(string key, bool inDark)
	{
		Color? color = null;
		if (inDark)
		{
			if (IsDark())
			{
				if (Application.Current != null && Application.Current?.TryFindResource(key) is Color value)
				{
					color = value;
				}
			}
			else
			{
				ResourceDictionary darkResource = DarkResource;
				if (darkResource.Contains(key))
				{
					color = darkResource[key] as Color?;
				}
			}
		}
		else if (IsDark())
		{
			ResourceDictionary lightResource = LightResource;
			if (lightResource.Contains(key))
			{
				color = lightResource[key] as Color?;
			}
		}
		if (!color.HasValue && Application.Current != null && Application.Current?.TryFindResource(key) is Color value2)
		{
			color = value2;
		}
		if (color.HasValue)
		{
			string text = color.Value.ToString();
			return "#" + text.Substring(text.Length - 6, 6);
		}
		return null;
	}

	public static SolidColorBrush GetColorInDict(string color, int percent)
	{
		string text = (string.IsNullOrEmpty(color) ? "transparent" : (color + percent));
		if (_brushes.ContainsKey(text))
		{
			return _brushes[text];
		}
		if (color != null && (color.Length == 6 || color.Length == 8) && !color.StartsWith("#"))
		{
			color = "#" + color;
		}
		SolidColorBrush value = ((text == "transparent") ? new SolidColorBrush(Colors.Transparent) : GetAlphaColor(color, percent));
		_brushes[text] = value;
		return _brushes[text];
	}

	public static SolidColorBrush GetBlendColor(string color1, string color2, float alpha1, float alpha2)
	{
		string key = color1 + color2 + alpha1 + alpha2;
		if (_brushes.ContainsKey(key))
		{
			return _brushes[key];
		}
		if (color1.Length > 6 && color2.Length > 6)
		{
			try
			{
				SolidColorBrush blendColor = ColorUtils.GetBlendColor(color1, color2, alpha1, alpha2);
				_brushes[key] = blendColor;
				return blendColor;
			}
			catch (Exception)
			{
			}
		}
		return GetColorInString(color1);
	}

	public static void TrySetSystemTheme(bool onlyDark = false, bool force = false)
	{
		bool flag = IsSystemDark();
		if (!force && IsDark() == flag)
		{
			return;
		}
		if (flag)
		{
			string text = LocalSettings.Settings.ExtraSettings.LastDarkTheme;
			if (string.IsNullOrEmpty(text))
			{
				text = "Dark";
			}
			App.Instance.LoadTheme(LocalSettings.Settings.ThemeId, text);
			LocalSettings.Settings.ThemeId = text;
		}
		else if (!onlyDark)
		{
			string text2 = LocalSettings.Settings.ExtraSettings.AppTheme;
			if (ThemeKey.IsDarkTheme(text2))
			{
				text2 = LocalSettings.Settings.ExtraSettings.SelectTheme;
			}
			if (string.IsNullOrEmpty(text2))
			{
				text2 = "White";
			}
			App.Instance.LoadTargetTheme(text2, fromDark: true);
		}
		DataChangedNotifier.NotifyIsDarkChanged();
	}

	public static bool IsSystemDark()
	{
		object obj = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize")?.GetValue("AppsUseLightTheme");
		if (obj == null)
		{
			return false;
		}
		return (int)obj <= 0;
	}

	public static ResourceDictionary GetImageDictionary(bool isDark)
	{
		return new ResourceDictionary
		{
			Source = new Uri("../Resource/" + (isDark ? "icons_dark" : "icons_light") + ".xaml", UriKind.Relative)
		};
	}

	internal static string GetNoAlphaColorString(string colorString)
	{
		if (string.IsNullOrEmpty(colorString) || colorString.ToLower(CultureInfo.InvariantCulture) == "transparent")
		{
			return colorString;
		}
		if ((colorString.Length == 8 || colorString.Length == 6) && !colorString.StartsWith("#"))
		{
			colorString = "#" + colorString;
		}
		if (colorString.Length == 9 && colorString.StartsWith("#"))
		{
			return "#" + colorString.Substring(3);
		}
		return colorString;
	}

	public static bool IsDark()
	{
		return ThemeKey.IsDarkTheme(LocalSettings.Settings.ThemeId);
	}

	public static string GetThemeKey(string themeId)
	{
		return themeId switch
		{
			"Green" => "Mint", 
			"Yellow" => "Sunshine", 
			"Black" => "Gray", 
			"Pink" => "Peach", 
			_ => themeId, 
		};
	}

	public static string Color2Hex(Color color)
	{
		return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
	}

	public static void SetDarkCustomColor(FrameworkElement context = null)
	{
		string value = LocalSettings.Settings?.ExtraSettings?.CustomDarkThemeColor;
		if (string.IsNullOrEmpty(value))
		{
			value = "#3957B6";
		}
		Color color = (Color)ColorConverter.ConvertFromString(value);
		ResourceDictionary resourceDictionary = ((context != null) ? context.Resources.MergedDictionaries : Application.Current.Resources.MergedDictionaries).LastOrDefault((ResourceDictionary d) => d.Source?.ToString().Contains("DarkCustom.xaml") ?? false);
		if (resourceDictionary != null)
		{
			resourceDictionary["ColorPrimary"] = color;
		}
	}
}
