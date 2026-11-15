using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Detail;

public class StickyColorViewModel : BaseViewModel
{
	private static readonly Dictionary<StickyColorKey, string> BackColorDict = new Dictionary<StickyColorKey, string>
	{
		{
			StickyColorKey.Default,
			"#FFF2AA"
		},
		{
			StickyColorKey.Apricot,
			"#FFD4C4"
		},
		{
			StickyColorKey.Watermelon,
			"#FFADAD"
		},
		{
			StickyColorKey.LakeBlue,
			"#CAF1FF"
		},
		{
			StickyColorKey.Blue,
			"#C4D4FF"
		},
		{
			StickyColorKey.Purple,
			"#D4C9FC"
		},
		{
			StickyColorKey.BlueGreen,
			"#AFEBBB"
		},
		{
			StickyColorKey.White,
			"#FFFFFF"
		},
		{
			StickyColorKey.Gray,
			"#EEEEEE"
		},
		{
			StickyColorKey.DarkGray,
			"#3D3F45"
		},
		{
			StickyColorKey.DarkBlue,
			"#152240"
		},
		{
			StickyColorKey.Dark,
			"#1F2126"
		}
	};

	private static readonly Dictionary<StickyColorKey, string> TopColorDict = new Dictionary<StickyColorKey, string>
	{
		{
			StickyColorKey.Default,
			"#FFED92"
		},
		{
			StickyColorKey.Apricot,
			"#FFCEBE"
		},
		{
			StickyColorKey.Watermelon,
			"#FFA5A5"
		},
		{
			StickyColorKey.LakeBlue,
			"#BEEDFF"
		},
		{
			StickyColorKey.Blue,
			"#BBCEFF"
		},
		{
			StickyColorKey.Purple,
			"#D0C3FF"
		},
		{
			StickyColorKey.BlueGreen,
			"#A0E7AF"
		},
		{
			StickyColorKey.White,
			"#F9F9F9"
		},
		{
			StickyColorKey.Gray,
			"#E8E8E8"
		},
		{
			StickyColorKey.DarkGray,
			"#383A3E"
		},
		{
			StickyColorKey.DarkBlue,
			"#131F3B"
		},
		{
			StickyColorKey.Dark,
			"#1A1C20"
		}
	};

	private bool _selected;

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

	public string Key { get; set; }

	public string Tooltip { get; set; }

	public bool NeedPro { get; set; }

	public SolidColorBrush BorderColor { get; set; }

	public SolidColorBrush FillColor { get; set; }

	public Geometry Icon { get; set; }

	public StickyColorViewModel(StickyColorKey key)
	{
		Key = key.ToString();
		NeedPro = key != StickyColorKey.Default;
		FillColor = GetBackColor(key);
		BorderColor = ((key == StickyColorKey.White) ? GetTopColor(StickyColorKey.Gray) : GetTopColor(key));
		if (key == StickyColorKey.Random)
		{
			FillColor = Brushes.Transparent;
			BorderColor = ThemeUtil.GetColor("BaseColorOpacity10");
			Icon = Utils.GetIcon("RandomIcon");
			Tooltip = Utils.GetString("Random");
		}
	}

	public static List<StickyColorViewModel> GetColorModels()
	{
		return (from StickyColorKey e in Enum.GetValues(typeof(StickyColorKey))
			select new StickyColorViewModel(e)).ToList();
	}

	public static SolidColorBrush GetBackColor(StickyColorKey key)
	{
		if (BackColorDict.ContainsKey(key))
		{
			return ThemeUtil.GetColorInString(BackColorDict[key]);
		}
		return ThemeUtil.GetColorInString("#FFF2AA");
	}

	public static SolidColorBrush GetTopColor(StickyColorKey key)
	{
		if (TopColorDict.ContainsKey(key))
		{
			return ThemeUtil.GetColorInString(TopColorDict[key]);
		}
		return ThemeUtil.GetColorInString("#FFF2AA");
	}
}
