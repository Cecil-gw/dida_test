using System.Windows.Media;

namespace ticktick_WPF.Views.Misc.ColorSelector.Media;

public class HsbaColor
{
	private double h;

	private double s;

	private double b;

	private double a;

	public double H
	{
		get
		{
			return h;
		}
		set
		{
			h = ((value < 0.0) ? 0.0 : ((value >= 360.0) ? 0.0 : value));
		}
	}

	public double S
	{
		get
		{
			return s;
		}
		set
		{
			s = ((value < 0.0) ? 0.0 : ((value > 1.0) ? 1.0 : value));
		}
	}

	public double B
	{
		get
		{
			return b;
		}
		set
		{
			b = ((value < 0.0) ? 0.0 : ((value > 1.0) ? 1.0 : value));
		}
	}

	public double A
	{
		get
		{
			return a;
		}
		set
		{
			a = ((value < 0.0) ? 0.0 : ((value > 1.0) ? 1.0 : value));
		}
	}

	public int Y => RgbaColor.Y;

	public Color Color => RgbaColor.Color;

	public Color OpaqueColor => RgbaColor.OpaqueColor;

	public SolidColorBrush SolidColorBrush => RgbaColor.SolidColorBrush;

	public SolidColorBrush OpaqueSolidColorBrush => RgbaColor.OpaqueSolidColorBrush;

	public string HexString => Color.ToString();

	public string RgbaString => RgbaColor.RgbaString;

	public RgbaColor RgbaColor => Utility.HsbaToRgba(this);

	public HsbaColor()
	{
		H = 0.0;
		S = 0.0;
		B = 1.0;
		A = 1.0;
	}

	public HsbaColor(double h, double s, double b, double a = 1.0)
	{
		H = h;
		S = s;
		B = b;
		A = a;
	}

	public HsbaColor(int r, int g, int b, int a = 255)
	{
		HsbaColor hsbaColor = Utility.RgbaToHsba(new RgbaColor(r, g, b, a));
		H = hsbaColor.H;
		S = hsbaColor.S;
		B = hsbaColor.B;
		A = hsbaColor.A;
	}

	public HsbaColor(Brush brush)
	{
		HsbaColor hsbaColor = Utility.RgbaToHsba(new RgbaColor(brush));
		H = hsbaColor.H;
		S = hsbaColor.S;
		B = hsbaColor.B;
		A = hsbaColor.A;
	}

	public HsbaColor(string hexColor)
	{
		HsbaColor hsbaColor = Utility.RgbaToHsba(new RgbaColor(hexColor));
		H = hsbaColor.H;
		S = hsbaColor.S;
		B = hsbaColor.B;
		A = hsbaColor.A;
	}
}
