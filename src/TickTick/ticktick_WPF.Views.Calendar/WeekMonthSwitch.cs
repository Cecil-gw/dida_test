using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Shapes;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Calendar;

public class WeekMonthSwitch : UserControl, IComponentConnector
{
	public delegate void SwitchModeDelegate(string from, string to, bool closePopup);

	internal WeekMonthSwitch Root;

	internal Rectangle SwitchBorder;

	internal TextBlock DisplayText;

	internal Path Arrow;

	internal EscPopup ModePopup;

	private bool _contentLoaded;

	private bool IsOpenPopup { get; set; }

	public event SwitchModeDelegate SwitchMode;

	public WeekMonthSwitch()
	{
		InitializeComponent();
	}

	public void NavigateMode(string mode)
	{
		((WeekMonthViewModel)base.DataContext).Mode = mode;
	}

	public void QuickSwitch(Key key)
	{
		string mode = ((WeekMonthViewModel)base.DataContext).Mode;
		string text = "0";
		string label = "";
		switch (key)
		{
		case Key.D1:
		case Key.D:
			text = "2";
			label = "day_view";
			break;
		case Key.D3:
		case Key.M:
			text = "0";
			label = "month_view";
			break;
		case Key.D2:
		case Key.W:
			text = "1";
			label = "week_view";
			break;
		case Key.D4:
		case Key.Y:
			text = "10";
			label = "year_view";
			break;
		case Key.D5:
		case Key.A:
			text = "8";
			label = "agenda_view";
			break;
		}
		if (mode != text)
		{
			UserActCollectUtils.AddShortCutEvent("switch_views", label);
			((WeekMonthViewModel)base.DataContext).Mode = text;
			this.SwitchMode?.Invoke(mode, text, closePopup: false);
		}
	}

	private void ShowSwitchPopup(object sender, MouseButtonEventArgs e)
	{
		if (IsOpenPopup)
		{
			return;
		}
		IsOpenPopup = true;
		WeekMonthSwitchContent weekMonthSwitchContent = new WeekMonthSwitchContent(moreMode: false);
		string mode = ((WeekMonthViewModel)base.DataContext).Mode;
		weekMonthSwitchContent.InitSelectMode(mode);
		weekMonthSwitchContent.SwitchMode += delegate(string from, string to, bool closePopup)
		{
			if (closePopup)
			{
				ModePopup.IsOpen = false;
			}
			((WeekMonthViewModel)base.DataContext).Mode = to;
			this.SwitchMode?.Invoke(from, to, closePopup: false);
		};
		ModePopup.Child = weekMonthSwitchContent;
		ModePopup.IsOpen = true;
	}

	public void SetParallelShow(bool parallel)
	{
		SwitchBorder.SetResourceReference(FrameworkElement.StyleProperty, parallel ? "ParallelBorderStyle" : "NormalBorderStyle");
		DisplayText.SetResourceReference(FrameworkElement.StyleProperty, parallel ? "ParallelTextStyle" : "NormalTextStyle");
		Arrow.SetResourceReference(FrameworkElement.StyleProperty, parallel ? "ParallelPathStyle" : "NormalPathStyle");
	}

	private async void ModePopup_Closed(object sender, EventArgs e)
	{
		await Task.Delay(100);
		IsOpenPopup = false;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/calendar/weekmonthswitch.xaml", UriKind.Relative);
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
			Root = (WeekMonthSwitch)target;
			break;
		case 2:
			SwitchBorder = (Rectangle)target;
			SwitchBorder.PreviewMouseLeftButtonUp += ShowSwitchPopup;
			break;
		case 3:
			DisplayText = (TextBlock)target;
			break;
		case 4:
			Arrow = (Path)target;
			break;
		case 5:
			ModePopup = (EscPopup)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
