using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Misc;

public class TopmostPopup : Popup
{
	private class PopupAdorner : Adorner
	{
		private readonly UIElement _content;

		private Point _position;

		protected override int VisualChildrenCount => 1;

		public PopupAdorner(UIElement adornedElement, UIElement content)
			: base(adornedElement)
		{
			_content = content;
			base.IsHitTestVisible = true;
		}

		protected override Visual GetVisualChild(int index)
		{
			return _content;
		}

		protected override Size MeasureOverride(Size constraint)
		{
			_content.Measure(constraint);
			return _content.DesiredSize;
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			if (base.Parent is Popup popup)
			{
				_position = ((popup.HorizontalOffset != 0.0 || popup.VerticalOffset != 0.0) ? new Point(popup.HorizontalOffset, popup.VerticalOffset) : new Point(0.0, 0.0));
			}
			_content.Arrange(new Rect(_position, _content.DesiredSize));
			return finalSize;
		}

		protected override Geometry GetLayoutClip(Size layoutSlotSize)
		{
			return new RectangleGeometry(new Rect(_position, _content.DesiredSize));
		}
	}

	private AdornerLayer _adornerLayer;

	private UIElement _content;

	private bool _isOpen;

	public bool NeedFocus = true;

	public bool RestoreFocus { get; set; } = true;

	public TopmostPopup()
	{
		base.Opened += TopmostPopup_Opened;
		base.Closed += TopmostPopup_Closed;
		base.PreviewKeyDown += OnKeyDown;
		base.KeyUp += OnKeyUp;
		AppLockWindowManager.TrackPopup(this);
	}

	private void OnKeyUp(object sender, KeyEventArgs e)
	{
		switch (e.Key)
		{
		case Key.Escape:
		{
			ITabControl tabControl2 = GetTabControl();
			if (tabControl2 != null && tabControl2.HandleEsc())
			{
				e.Handled = true;
				break;
			}
			base.IsOpen = false;
			e.Handled = true;
			break;
		}
		case Key.Return:
		{
			ITabControl tabControl = GetTabControl();
			if (tabControl != null)
			{
				e.Handled = tabControl.HandleEnter();
			}
			break;
		}
		}
	}

	private void OnKeyDown(object sender, KeyEventArgs e)
	{
		switch (e.Key)
		{
		case Key.Escape:
			e.Handled = true;
			break;
		case Key.Up:
		case Key.Down:
		{
			ITabControl tabControl = GetTabControl();
			if (tabControl != null)
			{
				e.Handled = tabControl.UpDownSelect(e.Key == Key.Up);
			}
			break;
		}
		case Key.Left:
		case Key.Right:
		{
			ITabControl tabControl2 = GetTabControl();
			if (tabControl2 != null)
			{
				e.Handled = tabControl2.LeftRightSelect(e.Key == Key.Left);
			}
			break;
		}
		case Key.Tab:
			GetTabControl()?.HandleTab(Utils.IfShiftPressed());
			e.Handled = true;
			break;
		}
	}

	private ITabControl GetTabControl()
	{
		if (base.Child is ITabControl result)
		{
			return result;
		}
		if (base.Child is FrameworkElement frameworkElement)
		{
			return frameworkElement.DataContext as ITabControl;
		}
		return null;
	}

	private void TopmostPopup_Opened(object sender, EventArgs e)
	{
		if (base.PlacementTarget == null)
		{
			return;
		}
		_adornerLayer = AdornerLayer.GetAdornerLayer(base.PlacementTarget);
		if (_adornerLayer == null)
		{
			return;
		}
		_content = base.Child;
		if (_content != null)
		{
			PopupAdorner adorner = new PopupAdorner(base.PlacementTarget, _content);
			_adornerLayer.Add(adorner);
			_isOpen = true;
			if (NeedFocus)
			{
				HwndHelper.SetFocus(this, !base.StaysOpen);
			}
		}
	}

	private void TopmostPopup_Closed(object sender, EventArgs e)
	{
		if (_adornerLayer != null && _isOpen)
		{
			Adorner[] adorners = _adornerLayer.GetAdorners(base.PlacementTarget);
			if (adorners != null)
			{
				Adorner[] array = adorners;
				foreach (Adorner adorner in array)
				{
					if (adorner is PopupAdorner)
					{
						_adornerLayer.Remove(adorner);
					}
				}
			}
			_isOpen = false;
		}
		if (base.PlacementTarget != null && RestoreFocus && !AppLockWindowManager.IsLocked)
		{
			HwndHelper.SetFocus(base.PlacementTarget);
		}
	}
}
