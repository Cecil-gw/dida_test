using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.CountDown.ViewModels;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.CountDown.Views;

public class CountdownDetailWindow : EscPopup, IComponentConnector
{
	private UIElement _target;

	public static bool PopupShowing;

	internal CountDownCard Card;

	private bool _contentLoaded;

	public event EventHandler<string> Disappear;

	public CountdownDetailWindow()
	{
		InitializeComponent();
	}

	private void OnWindowKeyDown(object sender, KeyEventArgs e)
	{
		if (object.Equals(FocusManager.GetFocusedElement(this), this))
		{
			Key key = e.Key;
			if ((key == Key.Up || key == Key.Down) && _target is IShowTaskDetailWindow showTaskDetailWindow)
			{
				showTaskDetailWindow.OnDetailKeyUp(e.Key);
			}
		}
	}

	protected override void OnClosed(EventArgs e)
	{
		this.Disappear?.Invoke(this, null);
		base.OnClosed(e);
	}

	public async void Show(UIElement target, double targetWidth, double addHeight, bool byMouse, CountDownViewModel model, Point point = default(Point))
	{
		PopupStateManager.LastTarget = target;
		_target = target;
		base.PlacementTarget = target;
		model.ShowActionButton = false;
		Card.DataContext = model;
		TaskPopupArgs popupLocation = PopupLocationCalculator.GetPopupLocation(_target, targetWidth, 330.0, byMouse, addHeight);
		if (point != default(Point))
		{
			base.Placement = PlacementMode.RelativePoint;
			base.HorizontalOffset = point.X;
			base.VerticalOffset = point.Y;
		}
		else if (!popupLocation.ByMouse)
		{
			base.Placement = (popupLocation.IsRight ? PlacementMode.Right : PlacementMode.Left);
			base.HorizontalOffset = (popupLocation.IsRight ? (-4) : 6);
			base.VerticalOffset = -10.0;
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

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/countdown/views/countdowndetailwindow.xaml", UriKind.Relative);
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
			Card = (CountDownCard)target;
		}
		else
		{
			_contentLoaded = true;
		}
	}
}
