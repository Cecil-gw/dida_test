using System;
using System.Windows.Media;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Theme;

public struct PixelColor
{
	public byte Blue;

	public byte Green;

	public byte Red;

	public byte Alpha;

	public bool IsTooLight()
	{
		return Blue + Green + Red > 480;
	}

	public SolidColorBrush ToColorBrush()
	{
		string text = Convert.ToString(Red, 16).ToUpper();
		if (text.Length == 1)
		{
			text = "0" + text;
		}
		string text2 = Convert.ToString(Green, 16).ToUpper();
		if (text2.Length == 1)
		{
			text2 = "0" + text2;
		}
		string text3 = Convert.ToString(Blue, 16).ToUpper();
		if (text3.Length == 1)
		{
			text3 = "0" + text3;
		}
		return ThemeUtil.GetColorInString("#" + text + text2 + text3);
	}
}
