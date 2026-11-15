using System.Drawing;
using System.Windows;
using System.Windows.Media;

namespace ticktick_WPF.Views.MarkDown;

public static class ExtentionMethods
{
	private static System.Windows.Point ToWpf(this System.Drawing.Point p)
	{
		return new System.Windows.Point(p.X, p.Y);
	}

	private static System.Windows.Size ToWpf(this System.Drawing.Size s)
	{
		return new System.Windows.Size(s.Width, s.Height);
	}

	public static Rect ToWpf(this Rectangle rect)
	{
		return new Rect(rect.Location.ToWpf(), rect.Size.ToWpf());
	}

	public static System.Drawing.Point ToSystemDrawing(this System.Windows.Point p)
	{
		return new System.Drawing.Point((int)p.X, (int)p.Y);
	}

	public static System.Windows.Size TransformToDevice(this System.Windows.Size size, Visual visual)
	{
		Matrix? matrix = PresentationSource.FromVisual(visual)?.CompositionTarget?.TransformToDevice;
		return new System.Windows.Size(size.Width * (matrix?.M11 ?? 1.0), size.Height * (matrix?.M22 ?? 1.0));
	}

	public static Rect TransformFromDevice(this Rect rect, Visual visual)
	{
		Matrix? matrix = PresentationSource.FromVisual(visual)?.CompositionTarget?.TransformFromDevice;
		if (matrix.HasValue)
		{
			return Rect.Transform(rect, matrix.Value);
		}
		return rect;
	}
}
