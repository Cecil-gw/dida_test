using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace TickTickUtils;

public class AnimationUtils
{
	public static DoubleAnimation GetDoubleAnimation(double? from, double to, int duration)
	{
		return new DoubleAnimation
		{
			From = from,
			To = to,
			Duration = new Duration(TimeSpan.FromMilliseconds(duration))
		};
	}
}
