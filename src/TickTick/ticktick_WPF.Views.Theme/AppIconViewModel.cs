using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using TickTickUtils;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Theme;

public class AppIconViewModel : BaseViewModel
{
	private bool _selected;

	public string Key { get; set; }

	public List<AppIconViewModel> Parent { get; set; }

	public bool IsDefault { get; set; }

	public bool IsPro { get; set; }

	public double Width => IsDefault ? 44 : 53;

	public BitmapImage Image { get; set; }

	public Visibility BorderVisible { get; set; }

	public double BorderRadius { get; set; }

	public Visibility DarkBorderVisible { get; set; }

	public bool Selected
	{
		get
		{
			return _selected;
		}
		set
		{
			_selected = value;
			OnPropertyChanged("Selected");
		}
	}

	public AppIconViewModel()
	{
	}

	public AppIconViewModel(AppIconKey key, List<AppIconViewModel> parent)
	{
		Key = key.ToString();
		Parent = parent;
		IsPro = IsProIcon(key);
		IsDefault = key == AppIconKey.Default;
		string uriString = AppPaths.AppIconDir + "SetIcons\\" + key.ToString() + ".png";
		BitmapImage bitmapImage = new BitmapImage();
		bitmapImage.BeginInit();
		bitmapImage.DecodePixelWidth = 128;
		bitmapImage.UriSource = new Uri(uriString);
		bitmapImage.EndInit();
		bitmapImage.Freeze();
		BorderVisible = ((key < AppIconKey.CClassic || key > AppIconKey.SBlackPro) ? Visibility.Collapsed : Visibility.Visible);
		DarkBorderVisible = ((key != AppIconKey.Ai && key != AppIconKey.Spaceship && key != AppIconKey.Mechanical && key != AppIconKey.Neon) ? Visibility.Collapsed : Visibility.Visible);
		BorderRadius = ((key == AppIconKey.CClassic) ? 25 : 10);
		Image = bitmapImage;
		Selected = LocalSettings.Settings.Common.AppIconKey == Key || (string.IsNullOrEmpty(LocalSettings.Settings.Common.AppIconKey) && key == AppIconKey.Default);
	}

	private bool IsProIcon(AppIconKey key)
	{
		if (Key.EndsWith("Pro"))
		{
			return true;
		}
		if ((uint)(key - 18) <= 7u || key == AppIconKey.Dark_Linear)
		{
			return true;
		}
		return false;
	}

	public void ClickCommand()
	{
		if (!_selected && (!IsPro || ProChecker.CheckPro(ProType.AppIcon)))
		{
			LocalSettings.Settings.Common.AppIconKey = Key;
			AppIconUtils.SetAppIcons(Key);
			Parent.Where((AppIconViewModel item) => item.Selected && item.Key != Key).ToList().ForEach(delegate(AppIconViewModel item)
			{
				item.Selected = false;
			});
			Selected = true;
		}
	}

	public static string GetEventLabel(string key)
	{
		switch (key)
		{
		default:
			return "default_transparent";
		case "CClassic":
			return "default_round";
		case "SClassic":
			return "default";
		case "SClassicPro":
			return "line_3d";
		case "SGreenPro":
			return "line_green";
		case "SOrangePro":
			return "line_red";
		case "SPinkPro":
			return "line_pink";
		case "SYellowPro":
			return "line_orange";
		case "SBlackPro":
			return "line_black";
		case "WGreenPro":
			return "fashion_watercolor";
		case "PlanetPro":
			return "style_moon";
		case "GYellowPro":
			return "gradient_yellow";
		case "GRedPro":
			return "gradient_red";
		case "GSiriPro":
			return "gradient_siri";
		case "GBluePro":
			return "gradient_blue";
		case "LRedPro":
			return "fashion_red";
		case "LYellowPro":
			return "fashion_yellow";
		case "LGreenPro":
			return "fashion_green";
		case "ScratchBluePro":
			return "fashion_draft";
		case "Christmas":
			return "festival_christmas2";
		case "SpringFestival":
			return "festival_new_year";
		case "Spaceship":
		case "Macintosh":
		case "Mechanical":
		case "Plump3d":
		case "Emotion":
		case "Ai":
		case "Neon":
		case "Planet":
			return "style_" + key.ToLower();
		case "Dark_Linear":
			return "dark_linear";
		}
	}
}
