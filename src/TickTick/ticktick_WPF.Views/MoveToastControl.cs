using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using Emoji.Wpf;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views;

public class MoveToastControl : UserControl, IComponentConnector
{
	private readonly string _projectId;

	private readonly INavigateProject _navigate;

	private readonly bool _isToday;

	private readonly bool _isTomorrow;

	private readonly Action _action;

	internal EmjTextBlock TitleText;

	internal EmjTextBlock ProjectTitle;

	private bool _contentLoaded;

	public MoveToastControl(string projectId, INavigateProject navigate, string taskName, MoveToastType moveType)
	{
		taskName = "";
		InitializeComponent();
		ProjectModel projectById = CacheManager.GetProjectById(projectId);
		_projectId = projectById?.id;
		_navigate = navigate;
		TitleText.Text = taskName;
		TitleText.Visibility = (string.IsNullOrEmpty(taskName) ? Visibility.Collapsed : Visibility.Visible);
		string text = projectById?.name ?? Utils.GetString("Inbox");
		text = (EmojiHelper.StartWithEmoji(text) ? text : ("\"" + text + "\""));
		switch (moveType)
		{
		case MoveToastType.Add:
			ProjectTitle.Text = Utils.GetString("AddTo") + " " + text;
			break;
		case MoveToastType.Restore:
			ProjectTitle.Text = string.Format(Utils.GetString("TaskHasBeenRestoreToProject"), text);
			break;
		default:
			ProjectTitle.Text = string.Format(Utils.GetString("MovedTo"), text);
			break;
		}
	}

	public MoveToastControl(bool isToday, INavigateProject navigate, string taskName)
	{
		InitializeComponent();
		_navigate = navigate;
		TitleText.Text = taskName;
		TitleText.Visibility = (string.IsNullOrEmpty(taskName) ? Visibility.Collapsed : Visibility.Visible);
		_isToday = isToday;
		_isTomorrow = !isToday;
		ProjectTitle.Text = string.Format(Utils.GetString("MovedTo"), Utils.GetString(isToday ? "Today" : "Tomorrow"));
	}

	private void OnMouseUp(object sender, MouseButtonEventArgs e)
	{
		if (_action != null)
		{
			_action();
			base.Visibility = Visibility.Collapsed;
			return;
		}
		if (!string.IsNullOrEmpty(_projectId))
		{
			_navigate.NavigateProjectById(_projectId);
		}
		else if (_isToday)
		{
			_navigate.NavigateTodayProject();
		}
		else if (_isTomorrow)
		{
			_navigate.NavigateTomorrowProject();
		}
		base.Visibility = Visibility.Collapsed;
	}

	public MoveToastControl(string text, Action action)
	{
		InitializeComponent();
		TitleText.Visibility = Visibility.Collapsed;
		ProjectTitle.Text = text;
		_action = action;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/movetoastcontrol.xaml", UriKind.Relative);
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
			((Border)target).MouseLeftButtonUp += OnMouseUp;
			break;
		case 2:
			TitleText = (EmjTextBlock)target;
			break;
		case 3:
			ProjectTitle = (EmjTextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
