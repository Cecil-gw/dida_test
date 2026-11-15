using System;
using System.Windows;
using System.Windows.Media;

namespace ticktick_WPF.Views.AI;

internal sealed class AIDesktopWidgetDragState
{
	private Point _pointerStart;

	public bool IsPressed { get; private set; }

	public bool HasDragged { get; private set; }

	public void Begin(Point pointer)
	{
		_pointerStart = pointer;
		IsPressed = true;
		HasDragged = false;
	}

	public bool TryStartDrag(Point pointer, Matrix deviceToLogical, double horizontalThreshold, double verticalThreshold)
	{
		if (!IsPressed)
		{
			return false;
		}
		Vector vector = deviceToLogical.Transform(pointer - _pointerStart);
		if (!HasDragged && Math.Abs(vector.X) < horizontalThreshold && Math.Abs(vector.Y) < verticalThreshold)
		{
			return false;
		}
		HasDragged = true;
		return true;
	}

	public void Cancel()
	{
		IsPressed = false;
	}
}
