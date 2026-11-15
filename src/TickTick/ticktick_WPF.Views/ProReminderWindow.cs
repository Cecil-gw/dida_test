using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views;

public class ProReminderWindow : MyWindow, IComponentConnector
{
	internal StackPanel LimitPanel;

	internal StackPanel ProjectLimitPanel;

	internal TextBlock ProjectLimitText;

	internal StackPanel TaskLimitPanel;

	internal TextBlock TaskLimitText;

	internal StackPanel ShareLimitPanel;

	internal TextBlock ShareLimitText;

	internal TextBlock ReminderText;

	internal CheckBox DontShowCheckBox;

	private bool _contentLoaded;

	public ProReminderWindow(ListOverLimitsModel model)
	{
		InitializeComponent();
		InitHintText(model);
		TrySetOwner();
		base.InputBindings.Add(new KeyBinding(WindowCommands.EscCommand, new KeyGesture(Key.Escape))
		{
			CommandParameter = this
		});
	}

	private void InitHintText(ListOverLimitsModel model)
	{
		if (model.ProjectNum > 9)
		{
			ProjectLimitPanel.Visibility = Visibility.Visible;
			ProjectLimitText.Text = Utils.GetString("ProjectOverLimitText");
		}
		if (model.TaskNum > 0)
		{
			TaskLimitPanel.Visibility = Visibility.Visible;
			TaskLimitText.Text = string.Format(Utils.GetString("TaskOverLimitText"), model.TaskNum);
		}
		if (model.ShareNum > 0)
		{
			ShareLimitPanel.Visibility = Visibility.Visible;
			ShareLimitText.Text = string.Format(Utils.GetString("ShareOverLimitText"), model.ShareNum);
		}
		if (model.ProjectNum > 9 || model.TaskNum > 0 || model.ShareNum > 0)
		{
			LimitPanel.Visibility = Visibility.Visible;
		}
		string format = Utils.GetString("UpdateReminder");
		ReminderText.Text = string.Format(format, Utils.GetAppName());
	}

	private void CloseWindow(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private async void StartUpdate(object sender, RoutedEventArgs e)
	{
		await Utils.StartUpgrade("pro_expired_downgrade");
		Close();
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		if (DontShowCheckBox.IsChecked == true)
		{
			LocalSettings.Settings.DontShowProWindow = true;
		}
		base.OnClosing(e);
	}

	private void TrySetOwner()
	{
		try
		{
			base.Owner = App.Window;
		}
		catch (Exception)
		{
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/proreminderwindow.xaml", UriKind.Relative);
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
			((Grid)target).MouseLeftButtonUp += CloseWindow;
			break;
		case 2:
			LimitPanel = (StackPanel)target;
			break;
		case 3:
			ProjectLimitPanel = (StackPanel)target;
			break;
		case 4:
			ProjectLimitText = (TextBlock)target;
			break;
		case 5:
			TaskLimitPanel = (StackPanel)target;
			break;
		case 6:
			TaskLimitText = (TextBlock)target;
			break;
		case 7:
			ShareLimitPanel = (StackPanel)target;
			break;
		case 8:
			ShareLimitText = (TextBlock)target;
			break;
		case 9:
			ReminderText = (TextBlock)target;
			break;
		case 10:
			DontShowCheckBox = (CheckBox)target;
			break;
		case 11:
			((Button)target).Click += StartUpdate;
			break;
		case 12:
			((Button)target).Click += CloseWindow;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
