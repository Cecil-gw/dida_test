using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Calendar;

public class CalendarProView : UserControl, IComponentConnector
{
	internal ScrollableGroupTitle Titles;

	private bool _contentLoaded;

	public event EventHandler<string> ItemSelected;

	public CalendarProView(string selected = null, bool autoSwitch = true)
	{
		InitializeComponent();
		Titles.InitCalendarProTitles(selected, autoSwitch);
		UserActCollectUtils.SendUpgradePromptData("calendar_view_preview");
	}

	private void OnItemSelected(object sender, string e)
	{
		this.ItemSelected?.Invoke(sender, e);
	}

	private void ProClick(object sender, MouseButtonEventArgs e)
	{
		Utils.StartUpgrade("calendar_view_preview");
		Titles.StopTimer();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/calendar/calendarproview.xaml", UriKind.Relative);
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
			Titles = (ScrollableGroupTitle)target;
			break;
		case 2:
			((Border)target).MouseLeftButtonUp += ProClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
