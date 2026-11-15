using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace ticktick_WPF.Views.Team;

public class TeamNotification : UserControl, IComponentConnector
{
	internal TeamNotification Root;

	internal CheckBox DoOrUndo;

	internal CheckBox Create;

	internal CheckBox Delete;

	private bool _contentLoaded;

	public event EventHandler<bool> DoCheck;

	public event EventHandler<bool> CreateCheck;

	public event EventHandler<bool> DelCheck;

	public TeamNotification()
	{
		InitializeComponent();
	}

	private void OnDoChecked(object sender, RoutedEventArgs e)
	{
		this.DoCheck?.Invoke(null, e: true);
	}

	private void OnDoUnChecked(object sender, RoutedEventArgs e)
	{
		this.DoCheck?.Invoke(null, e: false);
	}

	private void OnCreateChecked(object sender, RoutedEventArgs e)
	{
		this.CreateCheck?.Invoke(null, e: true);
	}

	private void OnCreateUnChecked(object sender, RoutedEventArgs e)
	{
		this.CreateCheck?.Invoke(null, e: false);
	}

	private void OnDelChecked(object sender, RoutedEventArgs e)
	{
		this.DelCheck?.Invoke(null, e: true);
	}

	private void OnDelUnChecked(object sender, RoutedEventArgs e)
	{
		this.DelCheck?.Invoke(null, e: false);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/team/teamnotification.xaml", UriKind.Relative);
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
			Root = (TeamNotification)target;
			break;
		case 2:
			DoOrUndo = (CheckBox)target;
			DoOrUndo.Checked += OnDoChecked;
			DoOrUndo.Unchecked += OnDoUnChecked;
			break;
		case 3:
			Create = (CheckBox)target;
			Create.Checked += OnCreateChecked;
			Create.Unchecked += OnCreateUnChecked;
			break;
		case 4:
			Delete = (CheckBox)target;
			Delete.Checked += OnDelChecked;
			Delete.Unchecked += OnDelUnChecked;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
