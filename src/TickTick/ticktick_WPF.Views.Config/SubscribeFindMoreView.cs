using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Config;

public class SubscribeFindMoreView : UserControl, IComponentConnector
{
	private bool _contentLoaded;

	public SubscribeFindMoreView()
	{
		InitializeComponent();
	}

	private void OnFindMoreClick(object sender, MouseButtonEventArgs e)
	{
		IntegrationsAndImportTypeSelectWindow integrationsAndImportTypeSelectWindow = new IntegrationsAndImportTypeSelectWindow(Utils.FindParent<SubscribeCalendar>(this));
		integrationsAndImportTypeSelectWindow.Owner = Window.GetWindow(this);
		integrationsAndImportTypeSelectWindow.ShowDialog();
	}

	private void OnTaskClick(object sender, MouseButtonEventArgs e)
	{
		IntegrationsAndImportTypeSelectWindow integrationsAndImportTypeSelectWindow = new IntegrationsAndImportTypeSelectWindow(Utils.FindParent<SubscribeCalendar>(this), 1000.0);
		integrationsAndImportTypeSelectWindow.Owner = Window.GetWindow(this);
		integrationsAndImportTypeSelectWindow.ShowDialog();
	}

	private void OnCalendarClick(object sender, MouseButtonEventArgs e)
	{
		IntegrationsAndImportTypeSelectWindow integrationsAndImportTypeSelectWindow = new IntegrationsAndImportTypeSelectWindow(Utils.FindParent<SubscribeCalendar>(this), Utils.IsDida() ? 190 : 0);
		integrationsAndImportTypeSelectWindow.Owner = Window.GetWindow(this);
		integrationsAndImportTypeSelectWindow.ShowDialog();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/config/subscribefindmoreview.xaml", UriKind.Relative);
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
			((TextBlock)target).MouseLeftButtonUp += OnFindMoreClick;
			break;
		case 2:
			((Border)target).MouseLeftButtonUp += OnFindMoreClick;
			break;
		case 3:
			((Border)target).MouseLeftButtonUp += OnCalendarClick;
			break;
		case 4:
			((Border)target).MouseLeftButtonUp += OnTaskClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
