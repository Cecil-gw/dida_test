using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Calendar;

public class CourseDetailWindow : EscPopup, IComponentConnector
{
	public static bool PopupShowing;

	internal CourseDetailWindow Root;

	internal ContentControl Control;

	internal ItemsControl CourseItems;

	private bool _contentLoaded;

	public CourseDetailWindow(CourseDetailViewModel viewModel)
	{
		InitializeComponent();
		base.DataContext = viewModel;
		base.Closed += delegate
		{
			PopupStateManager.OnViewPopupClosed();
			PopupShowing = false;
		};
	}

	public async void Show(UIElement target, double targetWidth, bool showByMouse, Point point = default(Point))
	{
		base.PlacementTarget = target;
		TaskPopupArgs popupLocation = PopupLocationCalculator.GetPopupLocation(target, targetWidth, 380.0, showByMouse, 0.0);
		if (point != default(Point))
		{
			base.Placement = PlacementMode.RelativePoint;
			base.HorizontalOffset = point.X;
			base.VerticalOffset = point.Y;
		}
		else if (!popupLocation.ByMouse)
		{
			base.Placement = (popupLocation.IsRight ? PlacementMode.Right : PlacementMode.Left);
			base.HorizontalOffset = ((!popupLocation.IsRight) ? 6 : 0);
			base.VerticalOffset = -4.0;
		}
		else
		{
			base.Placement = PlacementMode.Mouse;
		}
		PopupStateManager.OnViewPopupOpened();
		base.IsOpen = true;
		await Task.Delay(200);
		if (base.IsOpen)
		{
			PopupShowing = true;
			PopupStateManager.OnViewPopupOpened();
		}
	}

	private void OnWindowKeyDown(object sender, KeyEventArgs e)
	{
		Key key = e.Key;
		if ((key == Key.Up || key == Key.Down) && base.PlacementTarget is IShowTaskDetailWindow showTaskDetailWindow)
		{
			showTaskDetailWindow.OnDetailKeyUp(e.Key);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/calendar/coursedetailwindow.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	internal Delegate _CreateDelegate(Type delegateType, string handler)
	{
		return Delegate.CreateDelegate(delegateType, this, handler);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			Root = (CourseDetailWindow)target;
			break;
		case 2:
			Control = (ContentControl)target;
			break;
		case 3:
			CourseItems = (ItemsControl)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
