using System.Windows;

namespace ticktick_WPF.Views.Detail;

public class TaskWindowDisplayArgs
{
	public UIElement Target;

	public double TargetWidth;

	public Point Point;

	public bool FocusTitle;

	public double AddHeight;

	public int QuadrantLevel;

	public bool UseInWidget;

	public TitleDateParseMode TitleParseMode;

	public TaskWindowDisplayArgs(UIElement target, double targetWidth, Point point, bool focus = false, double height = 0.0, int qLevel = 0, bool useInWidget = false)
	{
		Target = target;
		TargetWidth = targetWidth;
		Point = point;
		FocusTitle = focus;
		AddHeight = height;
		QuadrantLevel = qLevel;
		UseInWidget = useInWidget;
	}

	public TaskWindowDisplayArgs(UIElement target, double height)
	{
		Target = target;
		AddHeight = height;
	}

	public TaskWindowDisplayArgs(UIElement target, double targetWidth, Point point, double height = 0.0)
	{
		Target = target;
		TargetWidth = targetWidth;
		Point = point;
		AddHeight = height;
	}

	public TaskWindowDisplayArgs(UIElement target, double targetWidth, bool focus, int qLevel)
	{
		Target = target;
		TargetWidth = targetWidth;
		FocusTitle = focus;
		QuadrantLevel = qLevel;
	}
}
