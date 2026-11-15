using System;
using System.Windows.Media;

namespace TickTickUtils;

public class ColorUtils
{
	public static double GetColorGrayscale(string color, int opacity, int bottomGrayscale)
	{
		if (string.IsNullOrEmpty(color))
		{
			return 0.0;
		}
		if (color.Length > 6)
		{
			try
			{
				string value = color.Substring(color.Length - 6, 2);
				string value2 = color.Substring(color.Length - 4, 2);
				string value3 = color.Substring(color.Length - 2, 2);
				int num = Convert.ToInt32(value, 16);
				int num2 = Convert.ToInt32(value2, 16);
				int num3 = Convert.ToInt32(value3, 16);
				double num4 = (double)opacity / 100.0;
				return num4 * (0.299 * (double)num + 0.587 * (double)num2 + 0.114 * (double)num3) / 255.0 + (1.0 - num4) * (double)bottomGrayscale;
			}
			catch (Exception)
			{
				return 0.0;
			}
		}
		return 0.0;
	}

	public static SolidColorBrush GetBlendColor(string color1, string color2, float alpha1, float alpha2)
	{
		try
		{
			string value = color1.Substring(color1.Length - 6, 2);
			string value2 = color1.Substring(color1.Length - 4, 2);
			string value3 = color1.Substring(color1.Length - 2, 2);
			int num = Convert.ToInt32(value, 16);
			int num2 = Convert.ToInt32(value2, 16);
			int num3 = Convert.ToInt32(value3, 16);
			string value4 = color2.Substring(color2.Length - 6, 2);
			string value5 = color2.Substring(color2.Length - 4, 2);
			string value6 = color2.Substring(color2.Length - 2, 2);
			int num4 = Convert.ToInt32(value4, 16);
			int num5 = Convert.ToInt32(value5, 16);
			int num6 = Convert.ToInt32(value6, 16);
			float num7 = alpha1 + alpha2 - alpha1 * alpha2;
			float c = (float)num / 255f;
			float c2 = (float)num4 / 255f;
			float num8 = CalculateBlend(alpha1, alpha2, c, c2);
			float c3 = (float)num2 / 255f;
			float c4 = (float)num5 / 255f;
			float num9 = CalculateBlend(alpha1, alpha2, c3, c4);
			float c5 = (float)num3 / 255f;
			float c6 = (float)num6 / 255f;
			float num10 = CalculateBlend(alpha1, alpha2, c5, c6);
			return new SolidColorBrush(new Color
			{
				A = (byte)(num7 * 255f),
				R = (byte)(num8 * 255f),
				G = (byte)(num9 * 255f),
				B = (byte)(num10 * 255f)
			});
		}
		catch (Exception)
		{
			return new BrushConverter().ConvertFromString(color1) as SolidColorBrush;
		}
	}

	private static float CalculateBlend(float a1, float a2, float c1, float c2)
	{
		return (c1 * a1 * (1f - a2) + c2 * a2) / (a1 + a2 - a1 * a2);
	}

	public static (int, int, int) GetRgbColorByHslColor(float h, float s, float l)
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		if (s == 0f)
		{
			num = (int)(l * 255f);
			num2 = (int)(l * 255f);
			num3 = (int)(l * 255f);
		}
		else
		{
			float num4 = ((!((double)l < 0.5)) ? (l + s - s * l) : (l * (1f + s)));
			float f = 2f * l - num4;
			num = (int)(255f * GetRgb(f, num4, h + 1f / 3f));
			num2 = (int)(255f * GetRgb(f, num4, h));
			num3 = (int)(255f * GetRgb(f, num4, h - 1f / 3f));
		}
		return (num, num2, num3);
		static float GetRgb(float num5, float num6, float fh)
		{
			if (fh < 0f)
			{
				fh += 1f;
			}
			if (fh > 1f)
			{
				fh -= 1f;
			}
			if (6f * fh < 1f)
			{
				return num5 + (num6 - num5) * 6f * fh;
			}
			if (2f * fh < 1f)
			{
				return num6;
			}
			if (3f * fh < 2f)
			{
				return num5 + (num6 - num5) * (2f / 3f - fh) * 6f;
			}
			return num5;
		}
	}

	public static (float, float, float) GetHslColorByRgbColor(int r, int g, int b)
	{
		float num = (float)r / 255f;
		float num2 = (float)g / 255f;
		float num3 = (float)b / 255f;
		float num4 = Math.Min(Math.Min(num, num2), num3);
		float num5 = Math.Max(Math.Max(num, num2), num3);
		float num6 = num5 - num4;
		float num7 = (num5 + num4) / 2f;
		float num8 = 0f;
		float num9 = 0f;
		num9 = (((double)Math.Abs(num5 - num4) < 0.0001) ? 0f : ((!(num7 <= 0.5f)) ? ((num5 - num4) / (2f - num5 - num4)) : ((num5 - num4) / (num5 + num4))));
		if (num5 == 0f)
		{
			num8 = 0f;
		}
		else
		{
			float num10 = ((num5 - num) / 6f + num6 / 2f) / num6;
			float num11 = ((num5 - num2) / 6f + num6 / 2f) / num6;
			float num12 = ((num5 - num3) / 6f + num6 / 2f) / num6;
			if ((double)Math.Abs(num - num5) < 0.001)
			{
				num8 = num12 - num11;
			}
			else if ((double)Math.Abs(num2 - num5) < 0.001)
			{
				num8 = 1f / 3f + num10 - num12;
			}
			else if ((double)Math.Abs(num3 - num5) < 0.001)
			{
				num8 = 2f / 3f + num11 - num10;
			}
			if (num8 < 0f)
			{
				num8 += 1f;
			}
			if (num8 > 1f)
			{
				num8 -= 1f;
			}
		}
		return (num8, num9, num7);
	}

	public static (int, int, int) GetColorRgbByString(string color)
	{
		if (color != null && color.Length > 6)
		{
			string value = color.Substring(color.Length - 6, 2);
			string value2 = color.Substring(color.Length - 4, 2);
			string value3 = color.Substring(color.Length - 2, 2);
			int item = Convert.ToInt32(value, 16);
			int item2 = Convert.ToInt32(value2, 16);
			int item3 = Convert.ToInt32(value3, 16);
			return (item, item2, item3);
		}
		return (0, 0, 0);
	}

	public static string GetColorByRgb(int r, int g, int b)
	{
		return "#" + IntTHex(r) + IntTHex(g) + IntTHex(b);
		static string IntTHex(int i)
		{
			if (i < 0 || i > 255)
			{
				return "00";
			}
			string text = i.ToString("x");
			if (text.Length == 1)
			{
				text = "0" + text;
			}
			return text;
		}
	}

	public static bool IsAvailableColor(string color)
	{
		if (color != null && color.Length > 6)
		{
			string value = color.Substring(color.Length - 6, 2);
			string value2 = color.Substring(color.Length - 4, 2);
			string value3 = color.Substring(color.Length - 2, 2);
			try
			{
				int num = Convert.ToInt32(value, 16);
				int num2 = Convert.ToInt32(value2, 16);
				int num3 = Convert.ToInt32(value3, 16);
				return num < 256 && num2 < 256 && num3 < 256;
			}
			catch (Exception)
			{
			}
		}
		return false;
	}

	public static bool NeedsContrastBorder(string color, bool isDark)
	{
		if (!IsAvailableColor(color))
		{
			return false;
		}
		try
		{
			(int, int, int) colorRgbByString = GetColorRgbByString(color);
			int item = colorRgbByString.Item1;
			int item2 = colorRgbByString.Item2;
			int item3 = colorRgbByString.Item3;
			double num = 0.299 * (double)item + 0.587 * (double)item2 + 0.114 * (double)item3;
			return isDark ? (num <= 55.0) : (num >= 245.0);
		}
		catch (Exception)
		{
			return false;
		}
	}
}
