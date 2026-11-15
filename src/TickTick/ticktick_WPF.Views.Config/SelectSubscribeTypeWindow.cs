using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Cache;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Calendar;

namespace ticktick_WPF.Views.Config;

public class SelectSubscribeTypeWindow : Window, IComponentConnector, IStyleConnector
{
	private readonly SubscribeCalendar _parent;

	internal ItemsControl Items;

	private bool _contentLoaded;

	public SelectSubscribeTypeWindow(SubscribeCalendar subscribeCalendar)
	{
		InitializeComponent();
		Items.ItemsSource = SubscribeTypeItemViewModel.GetSubscribeItems();
		_parent = subscribeCalendar;
		base.Closing += delegate
		{
			base.Owner?.Activate();
		};
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	private void OnBindCalendarClick(object sender, MouseButtonEventArgs e)
	{
		if (!(sender is FrameworkElement { DataContext: SubscribeTypeItemViewModel dataContext }) || (dataContext.Name != "Google" && !ProChecker.CheckPro(ProType.SubscribeCalendar)))
		{
			return;
		}
		switch (dataContext.Name)
		{
		case "Google":
		{
			Close();
			LinkGoogleCalendarWindow linkGoogleCalendarWindow = new LinkGoogleCalendarWindow();
			linkGoogleCalendarWindow.Owner = Window.GetWindow(_parent);
			linkGoogleCalendarWindow.ShowDialog();
			break;
		}
		case "Outlook":
			if (LimitCache.CheckCalendarCount("Outlook"))
			{
				Close();
				BindOutlookWindow bindOutlookWindow = new BindOutlookWindow(_parent);
				bindOutlookWindow.Owner = Window.GetWindow(_parent);
				bindOutlookWindow.ShowDialog();
			}
			break;
		case "Exchange":
			if (LimitCache.CheckCalendarCount("Exchange"))
			{
				BindAccountWindow bindAccountWindow = new BindAccountWindow(_parent, Constants.BindAccountType.Exchange);
				bindAccountWindow.Owner = Window.GetWindow(_parent);
				bindAccountWindow.Show();
				Close();
			}
			break;
		case "iCloud":
			if (LimitCache.CheckCalendarCount("iCloud"))
			{
				BindICloudWindow bindICloudWindow = new BindICloudWindow(_parent);
				bindICloudWindow.Owner = Window.GetWindow(_parent);
				bindICloudWindow.Topmost = false;
				bindICloudWindow.Show();
				Close();
			}
			break;
		case "CalDAV":
			if (LimitCache.CheckCalendarCount("CalDAV"))
			{
				BindAccountWindow bindAccountWindow2 = new BindAccountWindow(_parent, Constants.BindAccountType.CalDAV);
				bindAccountWindow2.Owner = Window.GetWindow(_parent);
				bindAccountWindow2.Show();
				Close();
			}
			break;
		case "URL":
			if (LimitCache.CheckCalendarCount("URL"))
			{
				AddUrlCalendarWindow addUrlCalendarWindow = new AddUrlCalendarWindow(_parent);
				addUrlCalendarWindow.Owner = Window.GetWindow(_parent);
				addUrlCalendarWindow.Show();
				Close();
			}
			break;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/config/selectsubscribetypewindow.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 1)
		{
			Items = (ItemsControl)target;
		}
		else
		{
			_contentLoaded = true;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IStyleConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 2)
		{
			((Border)target).MouseLeftButtonUp += OnBindCalendarClick;
		}
	}
}
