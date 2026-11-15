using System.Windows.Media;

namespace ticktick_WPF.Views.Misc.ColorSelector.Media;

public class RgbaColor
{
	private int r;

	private int g;

	private int b;

	private int a;

	public int R
	{
		get
		{
			return r;
		}
		set
		{
			r = ((value >= 0) ? ((value > 255) ? 255 : value) : 0);
		}
	}

	public int G
	{
		get
		{
			return g;
		}
		set
		{
			g = ((value >= 0) ? ((value > 255) ? 255 : value) : 0);
		}
	}

	public int B
	{
		get
		{
			return b;
		}
		set
		{
			b = ((value >= 0) ? ((value > 255) ? 255 : value) : 0);
		}
	}

	public int A
	{
		get
		{
			return a;
		}
		set
		{
			a = ((value >= 0) ? ((value > 255) ? 255 : value) : 0);
		}
	}

	public int Y => Utility.GetBrightness(R, G, B);

	public Color Color => Color.FromArgb((byte)A, (byte)R, (byte)G, (byte)B);

	public Color OpaqueColor => Color.FromArgb(byte.MaxValue, (byte)R, (byte)G, (byte)B);

	public SolidColorBrush SolidColorBrush => new SolidColorBrush(Color);

	public SolidColorBrush OpaqueSolidColorBrush => new SolidColorBrush(OpaqueColor);

	public string HexString => Color.ToString();

	public string RgbaString => R + "," + G + "," + B + "," + A;

	public HsbaColor HsbaColor => Utility.RgbaToHsba(this);

	public RgbaColor()
	{
		R = 255;
		G = 255;
		B = 255;
		A = 255;
	}

	public RgbaColor(int r, int g, int b, int a = 255)
	{
		R = r;
		G = g;
		B = b;
		A = a;
	}

	public RgbaColor(Brush brush)
	{
		if (brush != null)
		{
			R = ((SolidColorBrush)brush).Color.R;
			G = ((SolidColorBrush)brush).Color.G;
			B = ((SolidColorBrush)brush).Color.B;
			A = ((SolidColorBrush)brush).Color.A;
		}
		else
		{
			R = (G = (B = (A = 255)));
		}
	}

	public RgbaColor(double h, double s, double b, double a = 1.0)
	{
		RgbaColor rgbaColor = Utility.HsbaToRgba(new HsbaColor(h, s, b, a));
		R = rgbaColor.R;
		G = rgbaColor.G;
		B = rgbaColor.B;
		A = rgbaColor.A;
	}

	public RgbaColor(string hexColor)
	{
		try
		{
			Color color = ((!(hexColor.Substring(0, 1) == "#")) ? ((Color)ColorConverter.ConvertFromString("#" + hexColor)) : ((Color)ColorConverter.ConvertFromString(hexColor)));
			R = color.R;
			G = color.G;
			B = color.B;
			A = color.A;
		}
		catch
		{
			R = 0;
			G = 0;
			B = 0;
			A = 255;
		}
	}
}
