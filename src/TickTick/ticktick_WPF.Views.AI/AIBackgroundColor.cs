using System;
using System.Linq;
using System.Windows.Media;

namespace ticktick_WPF.Views.AI;

internal static class AIBackgroundColor
{
	internal static Color Resolve(Brush foreground, Brush background)
	{
		if (!TryGetRepresentativeColor(foreground, out var color, out var alpha))
		{
			return Colors.White;
		}
		if (alpha >= 1.0)
		{
			return Color.FromRgb(color.R, color.G, color.B);
		}
		if (!TryGetRepresentativeColor(background, out var color2, out var alpha2))
		{
			return Colors.White;
		}
		Color background2 = Blend(color2, Colors.White, alpha2);
		return Blend(color, background2, alpha);
	}

	private static bool TryGetRepresentativeColor(Brush brush, out Color color, out double alpha)
	{
		color = Colors.Transparent;
		alpha = 0.0;
		if (brush == null || brush.Opacity <= 0.0)
		{
			return true;
		}
		if (brush is SolidColorBrush solidColorBrush)
		{
			color = solidColorBrush.Color;
		}
		else
		{
			if (!(brush is GradientBrush gradientBrush) || gradientBrush.GradientStops.Count <= 0)
			{
				return false;
			}
			color = GetGradientMidpoint(gradientBrush);
		}
		alpha = (double)(int)color.A / 255.0 * brush.Opacity;
		return true;
	}

	private static Color GetGradientMidpoint(GradientBrush gradient)
	{
		GradientStop[] array = gradient.GradientStops.OrderBy((GradientStop stop) => stop.Offset).ToArray();
		if (array[0].Offset > 0.5)
		{
			return array[0].Color;
		}
		GradientStop gradientStop = array[0];
		GradientStop[] array2 = array;
		foreach (GradientStop gradientStop2 in array2)
		{
			if (gradientStop2.Offset <= 0.5)
			{
				gradientStop = gradientStop2;
				continue;
			}
			double num2 = (0.5 - gradientStop.Offset) / (gradientStop2.Offset - gradientStop.Offset);
			if (gradient.ColorInterpolationMode == ColorInterpolationMode.ScRgbLinearInterpolation)
			{
				return Color.FromScRgb((float)((double)gradientStop.Color.ScA + (double)(gradientStop2.Color.ScA - gradientStop.Color.ScA) * num2), (float)((double)gradientStop.Color.ScR + (double)(gradientStop2.Color.ScR - gradientStop.Color.ScR) * num2), (float)((double)gradientStop.Color.ScG + (double)(gradientStop2.Color.ScG - gradientStop.Color.ScG) * num2), (float)((double)gradientStop.Color.ScB + (double)(gradientStop2.Color.ScB - gradientStop.Color.ScB) * num2));
			}
			return Color.FromArgb(Mix(gradientStop2.Color.A, gradientStop.Color.A, num2), Mix(gradientStop2.Color.R, gradientStop.Color.R, num2), Mix(gradientStop2.Color.G, gradientStop.Color.G, num2), Mix(gradientStop2.Color.B, gradientStop.Color.B, num2));
		}
		return gradientStop.Color;
	}

	private static Color Blend(Color foreground, Color background, double alpha)
	{
		return Color.FromRgb(Mix(foreground.R, background.R, alpha), Mix(foreground.G, background.G, alpha), Mix(foreground.B, background.B, alpha));
	}

	private static byte Mix(byte foreground, byte background, double alpha)
	{
		return (byte)Math.Round((double)(int)foreground * alpha + (double)(int)background * (1.0 - alpha), MidpointRounding.AwayFromZero);
	}
}
