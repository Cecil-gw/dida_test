using System.Collections.Generic;
using System.Windows;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Views.Widget;

public static class WidgetConstants
{
	public const double ShadowPadding = 10.0;

	public const string ThemeLight = "light";

	public const string ThemeDark = "dark";

	public const string ThemeAuto = "auto";

	private static Dictionary<WidgetType, Size> DefaultSizes { get; } = new Dictionary<WidgetType, Size>
	{
		{
			WidgetType.List,
			new Size(340.0, 546.0)
		},
		{
			WidgetType.Calendar,
			new Size(847.0, 622.0)
		},
		{
			WidgetType.Matrix,
			new Size(847.0, 622.0)
		},
		{
			WidgetType.CountdownItem,
			new Size(164.0, 164.0)
		},
		{
			WidgetType.CountdownList,
			new Size(340.0, 159.0)
		},
		{
			WidgetType.HabitItem,
			new Size(164.0, 164.0)
		},
		{
			WidgetType.HabitList,
			new Size(320.0, 226.0)
		},
		{
			WidgetType.HabitWeekItem,
			new Size(340.0, 164.0)
		},
		{
			WidgetType.AI,
			new Size(474.0, 510.0)
		}
	};

	private static Dictionary<WidgetType, Size> MinSizes { get; } = new Dictionary<WidgetType, Size>
	{
		{
			WidgetType.List,
			new Size(256.0, 292.0)
		},
		{
			WidgetType.Calendar,
			new Size(256.0, 400.0)
		},
		{
			WidgetType.Matrix,
			new Size(500.0, 400.0)
		},
		{
			WidgetType.CountdownItem,
			new Size(164.0, 164.0)
		},
		{
			WidgetType.CountdownList,
			new Size(340.0, 159.0)
		},
		{
			WidgetType.HabitItem,
			new Size(164.0, 164.0)
		},
		{
			WidgetType.HabitList,
			new Size(320.0, 164.0)
		},
		{
			WidgetType.HabitWeekItem,
			new Size(340.0, 164.0)
		}
	};

	private static Dictionary<WidgetType, Size> MaxSizes { get; } = new Dictionary<WidgetType, Size>
	{
		{
			WidgetType.CountdownItem,
			new Size(310.0, 206.0)
		},
		{
			WidgetType.HabitList,
			new Size(600.0, double.PositiveInfinity)
		}
	};

	public static Size? GetDefaultSize(WidgetType type, bool showShadow = false)
	{
		double num = (showShadow ? 20.0 : 0.0);
		if (DefaultSizes.TryGetValue(type, out var value))
		{
			return new Size(value.Width + num, value.Height + num);
		}
		return null;
	}

	public static Size? GetMinSize(WidgetType type, bool showShadow = false)
	{
		double num = (showShadow ? 20.0 : 0.0);
		if (MinSizes.TryGetValue(type, out var value))
		{
			return new Size(value.Width + num, value.Height + num);
		}
		return null;
	}

	public static Size? GetMaxSize(WidgetType type, bool showShadow = false)
	{
		double num = (showShadow ? 20.0 : 0.0);
		if (MaxSizes.TryGetValue(type, out var value))
		{
			return new Size(value.Width + num, value.Height + num);
		}
		return null;
	}
}
