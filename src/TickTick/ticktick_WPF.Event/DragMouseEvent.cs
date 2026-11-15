using System.Windows.Input;

namespace ticktick_WPF.Event;

public class DragMouseEvent
{
	public MouseEventArgs MouseArg { get; set; }

	public object Data { get; set; }

	public double X { get; }

	public double Y { get; }

	public DragMouseEvent(double x, double y)
	{
		X = x;
		Y = y;
	}

	public DragMouseEvent(MouseEventArgs arg)
	{
		MouseArg = arg;
	}
}
