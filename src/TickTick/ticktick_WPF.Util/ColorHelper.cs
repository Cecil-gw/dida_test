using System;
using System.Windows.Media;

namespace ticktick_WPF.Util;

internal class ColorHelper
{
	public static ColorConverter ColorConverter { get; set; } = new ColorConverter();

	public static Color BlendWithAlpha(Color sourceColor, Color destinationColor)
	{
		float num = (float)(int)sourceColor.A / 255f;
		float num2 = (float)(int)destinationColor.A / 255f;
		float num3 = num + num2 * (1f - num);
		if (num3 == 0f)
		{
			return Color.FromArgb(0, 0, 0, 0);
		}
		float num4 = 1f / num3;
		byte val = (byte)(((float)(int)sourceColor.R * num + (float)(int)destinationColor.R * num2 * (1f - num)) * num4);
		byte val2 = (byte)(((float)(int)sourceColor.G * num + (float)(int)destinationColor.G * num2 * (1f - num)) * num4);
		byte val3 = (byte)(((float)(int)sourceColor.B * num + (float)(int)destinationColor.B * num2 * (1f - num)) * num4);
		val = Math.Max((byte)0, Math.Min(val, byte.MaxValue));
		val2 = Math.Max((byte)0, Math.Min(val2, byte.MaxValue));
		val3 = Math.Max((byte)0, Math.Min(val3, byte.MaxValue));
		return Color.FromArgb((byte)Math.Max(0f, Math.Min(num3 * 255f, 255f)), val, val2, val3);
	}
}
