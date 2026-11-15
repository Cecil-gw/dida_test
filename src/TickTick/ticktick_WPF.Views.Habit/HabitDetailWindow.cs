using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Habit;

public class HabitDetailWindow : EscPopup, IComponentConnector
{
	private bool _inOperate;

	internal HabitDetailControl HabitDetail;

	private bool _contentLoaded;

	public event EventHandler<string> Disappear;

	public HabitDetailWindow()
	{
		InitializeComponent();
		base.Closed += delegate
		{
			this.Disappear?.Invoke(this, null);
			PopupStateManager.OnViewPopupClosed();
			this.Disappear = null;
		};
	}

	private async void OnWindowLoaded(object sender, RoutedEventArgs e)
	{
		if (!PopupStateManager.IsViewPopOpened())
		{
			PopupStateManager.OnViewPopupOpened();
		}
	}

	public async void Show(UIElement target, double targetWidth, double addHeight, bool byMouse, string habitId)
	{
		PopupStateManager.OnViewPopupOpened();
		HabitDetail.Load(habitId, DateTime.Today);
		base.PlacementTarget = target;
		TaskPopupArgs popupLocation = PopupLocationCalculator.GetPopupLocation(target, targetWidth, base.Width, byMouse, addHeight);
		if (!popupLocation.ByMouse)
		{
			base.Placement = (popupLocation.IsRight ? PlacementMode.Right : PlacementMode.Left);
			base.HorizontalOffset = (popupLocation.IsRight ? (-6) : 6);
			base.VerticalOffset = -8.0;
		}
		else
		{
			base.Placement = PlacementMode.Mouse;
		}
		base.IsOpen = true;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/habit/habitdetailwindow.xaml", UriKind.Relative);
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
		if (connectionId == 1)
		{
			HabitDetail = (HabitDetailControl)target;
		}
		else
		{
			_contentLoaded = true;
		}
	}
}
