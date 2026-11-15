using System;
using System.Windows.Media.TextFormatting;

namespace ticktick_WPF.Views.MarkDown;

internal static class EmbeddedObjectMetricsHelper
{
	internal static double ResolveTextBaseline(TextRunProperties properties)
	{
		double fontSize = properties?.FontRenderingEmSize ?? 14.0;
		double baselineRatio = properties?.Typeface?.FontFamily?.Baseline ?? 0.8;
		return ResolveTextBaseline(fontSize, baselineRatio);
	}

	internal static double ResolveTextBaseline(double fontSize, double baselineRatio)
	{
		double num = ((fontSize > 0.0 && !double.IsNaN(fontSize) && !double.IsInfinity(fontSize)) ? fontSize : 14.0);
		double num2 = ((baselineRatio > 0.0 && !double.IsNaN(baselineRatio) && !double.IsInfinity(baselineRatio)) ? baselineRatio : 0.8);
		return Math.Max(1.0, num * num2);
	}
}
