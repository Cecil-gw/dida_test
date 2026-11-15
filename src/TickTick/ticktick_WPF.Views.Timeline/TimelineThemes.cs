using System.Windows.Media;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Timeline;

public class TimelineThemes : BaseViewModel
{
	public static SolidColorBrush GridBrush => ThemeUtil.GetColor("BaseColorOpacity5");

	public static SolidColorBrush WeekendBrush => ThemeUtil.GetColor("BaseColorOpacity2");

	public static SolidColorBrush BackgroundBrush { get; private set; }

	public static Color DayLineColor => ThemeUtil.GetColorValue("TimelineDayLineColor");

	public static SolidColorBrush WeekendDayBrush => ThemeUtil.GetColor("BaseColorOpacity40_20");

	public static SolidColorBrush NormalDayBrush => ThemeUtil.GetColor("BaseColorOpacity80");

	public static SolidColorBrush PrimaryColorBrush => ThemeUtil.GetColor("PrimaryColor");

	public static SolidColorBrush PrimaryColor10Brush => ThemeUtil.GetColor("PrimaryColor10");

	public static SolidColorBrush ContainerBackgroundBrush => ThemeUtil.GetColor("TimelineContainerBackground");

	static TimelineThemes()
	{
		BackgroundBrush = new SolidColorBrush(Colors.White)
		{
			Opacity = 0.1
		};
	}
}
