using System.Drawing;

namespace ticktick_WPF.Views.Widget;

public class WidgetLocationModel
{
	public double Left { get; set; }

	public double Top { get; set; }

	public double Width { get; set; }

	public double Height { get; set; }

	public double ScreenX { get; set; }

	public double ScreenY { get; set; }

	public double ScreenWidth { get; set; }

	public HideType HideType { get; set; }

	public bool ShowShadow { get; set; }

	public double Padding
	{
		get
		{
			if (!ShowShadow)
			{
				return 0.0;
			}
			return 10.0;
		}
	}

	public double HideLeftIn => ScreenX - Width + Padding;

	public double HideRightIn => ScreenX + ScreenWidth - Padding;

	public double HideTopIn => ScreenY - Height + Padding;

	public double LeftOut => ScreenX - Padding;

	public double RightOut => ScreenX + ScreenWidth - Width + Padding;

	public double TopOut => ScreenY - Padding;

	public void SetScreen(Rectangle rect, double factorX, double factorY)
	{
		ScreenWidth = (double)rect.Width * factorX;
		ScreenX = (double)rect.X * factorX;
		ScreenY = (double)rect.Y * factorY;
	}
}
