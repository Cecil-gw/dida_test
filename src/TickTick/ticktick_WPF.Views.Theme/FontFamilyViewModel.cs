using System.Collections.Generic;
using System.Windows.Media;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Theme;

public class FontFamilyViewModel : BaseViewModel
{
	private bool _loading;

	private bool _showCopyRight;

	private bool _selected;

	public string Title { get; set; }

	public string Id { get; set; }

	public string FontImageUri { get; set; }

	public FontFamily Font { get; set; }

	public bool ShowText { get; set; }

	public bool NeedPro { get; set; }

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

	public bool Loading
	{
		get
		{
			return _loading;
		}
		set
		{
			_loading = value;
			OnPropertyChanged("Loading");
		}
	}

	public bool ShowCopyRight
	{
		get
		{
			return _showCopyRight;
		}
		set
		{
			_showCopyRight = value;
			OnPropertyChanged("ShowCopyRight");
		}
	}

	public FontFamilyViewModel(string id, bool isPro = true, bool showText = true)
	{
		Id = id;
		Title = FontFamilyUtils.GetFontName(id);
		ShowText = showText;
		NeedPro = isPro;
		if (showText)
		{
			Font = FontFamilyUtils.GetFontFamilyByKey(id);
		}
		FontImageUri = (showText ? string.Empty : ("../../Assets/Theme/FontFamily/" + (ThemeUtil.IsDark() ? "dark" : "light") + "/" + id + ".png"));
		Selected = LocalSettings.Settings.ExtraSettings.AppFontFamily == id;
	}

	public static List<FontFamilyViewModel> GetChineseFontViewModel()
	{
		List<FontFamilyViewModel> list = new List<FontFamilyViewModel>
		{
			new FontFamilyViewModel("Default_CN", isPro: false)
			{
				Selected = (LocalSettings.Settings.ExtraSettings.AppFontFamily == "Default_CN" || string.IsNullOrEmpty(LocalSettings.Settings.ExtraSettings.AppFontFamily))
			},
			new FontFamilyViewModel("HarmonyOS", isPro: false, showText: false),
			new FontFamilyViewModel("LXGW_CN", isPro: true, showText: false),
			new FontFamilyViewModel("Yozai_CN", isPro: true, showText: false),
			new FontFamilyViewModel("975Maru_CN", isPro: true, showText: false)
		};
		if (!Utils.IsWindows7())
		{
			list.Insert(2, new FontFamilyViewModel("SourceHansansSC_CN", isPro: false, showText: false));
		}
		return list;
	}

	public static List<FontFamilyViewModel> GetEnFontViewModel()
	{
		return new List<FontFamilyViewModel>
		{
			new FontFamilyViewModel("Default_EN", isPro: false)
			{
				Selected = (LocalSettings.Settings.ExtraSettings.AppFontFamily == "Default_EN" || string.IsNullOrEmpty(LocalSettings.Settings.ExtraSettings.AppFontFamily))
			},
			new FontFamilyViewModel("Roboto", isPro: false),
			new FontFamilyViewModel("Arial", isPro: false),
			new FontFamilyViewModel("Inter"),
			new FontFamilyViewModel("Poppins"),
			new FontFamilyViewModel("Nunito")
		};
	}

	public void ChangeImage()
	{
		if (ThemeUtil.IsDark())
		{
			FontImageUri = FontImageUri?.Replace("light", "dark");
		}
		else
		{
			FontImageUri = FontImageUri?.Replace("dark", "light");
		}
		OnPropertyChanged("FontImageUri");
	}

	public bool ShowAuthor()
	{
		if (Id != "Roboto")
		{
			return Id != "HarmonyOS";
		}
		return false;
	}
}
