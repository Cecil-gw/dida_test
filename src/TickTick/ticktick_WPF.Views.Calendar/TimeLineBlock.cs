using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Calendar;

public class TimeLineBlock : UserControl, IComponentConnector
{
	public static readonly DependencyProperty CheckNowProperty = DependencyProperty.Register("CheckNow", typeof(bool), typeof(TimeLineBlock), new PropertyMetadata(false));

	public static readonly DependencyProperty CheckCnProperty = DependencyProperty.Register("CheckCn", typeof(bool), typeof(TimeLineBlock), new PropertyMetadata(false));

	private bool _bottomPressed;

	private bool _topPressed;

	private bool _handleEnter;

	private bool _mouseDown;

	private TimeLine _timeLine;

	internal TimeLineBlock Root;

	private bool _contentLoaded;

	public bool CheckNow
	{
		get
		{
			return (bool)GetValue(CheckNowProperty);
		}
		set
		{
			if (CheckNow != value)
			{
				SetValue(CheckNowProperty, value);
			}
		}
	}

	public bool CheckCn
	{
		get
		{
			return (bool)GetValue(CheckCnProperty);
		}
		set
		{
			if (CheckCn != value)
			{
				SetValue(CheckCnProperty, value);
			}
		}
	}

	public TimeLineBlock()
	{
		InitializeComponent();
	}

	private void OnHandleMouseLeave(object sender, MouseEventArgs e)
	{
		_handleEnter = false;
	}

	private void OnHandleMouseEnter(object sender, MouseEventArgs e)
	{
		_handleEnter = true;
		NotifyHandleDragged(e);
	}

	private void NotifyHandleDragged(MouseEventArgs e)
	{
		if (e.LeftButton != MouseButtonState.Pressed || base.DataContext == null || !(base.DataContext is TimeOffset timeOffset))
		{
			return;
		}
		TimeLine timeLine = Utils.FindParent<TimeLine>(this);
		if (timeLine == null)
		{
			return;
		}
		if (timeLine.TopHandleDragging)
		{
			int baseOffset = timeOffset.BaseOffset;
			int bottomOffset = timeLine.GetBottomOffset();
			if (baseOffset < bottomOffset)
			{
				timeLine.NotifyHandleDragged(timeOffset.BaseOffset, isTop: true);
			}
		}
		if (timeLine.BottomHandleDragging)
		{
			int baseOffset2 = timeOffset.BaseOffset;
			int topOffset = timeLine.GetTopOffset();
			if (baseOffset2 > topOffset && timeOffset.BaseOffset < 1440)
			{
				timeLine.NotifyHandleDragged(timeOffset.BaseOffset, isTop: false);
			}
		}
	}

	private TimeLine GetParent()
	{
		if (_timeLine == null)
		{
			_timeLine = Utils.FindParent<TimeLine>(this);
		}
		return _timeLine;
	}

	private void OnBottomHandlePressed(object sender, MouseButtonEventArgs e)
	{
		_bottomPressed = true;
		GetParent()?.SetTryDrag(tryDrag: true);
	}

	private void OnStartHandlePressed(object sender, MouseButtonEventArgs e)
	{
		_topPressed = true;
		GetParent()?.SetTryDrag(tryDrag: true);
	}

	private async void OnBlockMouseMove(object sender, MouseEventArgs e)
	{
		if (e.LeftButton == MouseButtonState.Pressed)
		{
			TimeLine parent = GetParent();
			if (parent != null)
			{
				if (_bottomPressed)
				{
					parent.BottomHandleDragging = true;
				}
				if (_topPressed)
				{
					parent.TopHandleDragging = true;
				}
			}
		}
		else
		{
			_mouseDown = false;
			_bottomPressed = false;
			_topPressed = false;
			GetParent()?.SetTryDrag(tryDrag: false);
		}
	}

	private void OnBlockMouseUp(object sender, MouseButtonEventArgs e)
	{
		_bottomPressed = false;
		_topPressed = false;
		GetParent()?.SetTryDrag(tryDrag: false);
		if (base.DataContext != null && base.DataContext is TimeOffset offset)
		{
			Utils.FindParent<TimeLine>(this)?.OnHandleDrop(offset, _mouseDown);
		}
		_mouseDown = false;
	}

	private void OnCollapsedClick(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext != null && base.DataContext is TimeOffset model)
		{
			Utils.FindParent<TimeLine>(this)?.ExpandTimeline(model);
			e.Handled = true;
		}
	}

	private void OnBlockMouseDown(object sender, MouseButtonEventArgs e)
	{
		_mouseDown = true;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/calendar/timelineblock.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			Root = (TimeLineBlock)target;
			break;
		case 2:
			((Grid)target).MouseMove += OnBlockMouseMove;
			((Grid)target).MouseLeftButtonUp += OnBlockMouseUp;
			((Grid)target).MouseLeftButtonDown += OnBlockMouseDown;
			break;
		case 3:
			((Border)target).PreviewMouseLeftButtonUp += OnCollapsedClick;
			break;
		case 4:
			((Border)target).MouseEnter += OnHandleMouseEnter;
			((Border)target).MouseLeave += OnHandleMouseLeave;
			break;
		case 5:
			((Border)target).PreviewMouseLeftButtonDown += OnBottomHandlePressed;
			break;
		case 6:
			((Border)target).PreviewMouseLeftButtonDown += OnStartHandlePressed;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
