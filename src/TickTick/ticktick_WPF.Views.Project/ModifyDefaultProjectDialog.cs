using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using Emoji.Wpf;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Project;

public class ModifyDefaultProjectDialog : Window, IComponentConnector
{
	private string _currentId;

	private TaskDefaultModel _taskDefault;

	internal EmjTextBlock DefaultAddProjectNameText;

	internal EscPopup DefaultAddProjectPopup;

	internal Button OkButton;

	private bool _contentLoaded;

	public ModifyDefaultProjectDialog(bool isDelete = false, bool isArchive = false)
	{
		InitializeComponent();
		base.Loaded += OnLoaded;
		if (isDelete)
		{
			base.Title = Utils.GetString("CannotDeleteDefault");
		}
		else if (isArchive)
		{
			base.Title = Utils.GetString("CannotArchiveDefault");
		}
		else
		{
			base.Title = Utils.GetString("CannotHideDefault");
		}
	}

	private async void OnLoaded(object sender, RoutedEventArgs e)
	{
		_taskDefault = TaskDefaultDao.GetDefaultSafely();
		_currentId = _taskDefault.ProjectId;
		ProjectModel projectById = CacheManager.GetProjectById(_currentId);
		if (_currentId != null)
		{
			DefaultAddProjectNameText.Text = projectById.name;
		}
		OkButton.IsEnabled = false;
	}

	private async void OnSaveClick(object sender, RoutedEventArgs e)
	{
		if (_taskDefault != null && _taskDefault.ProjectId != _currentId)
		{
			_taskDefault.ProjectId = _currentId;
			await TaskDefaultDao.SaveTaskDefault(_taskDefault);
			SettingsHelper.PushLocalSettings();
			DataChangedNotifier.NotifyTaskDefaultChanged();
		}
		Close();
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private async void OnDefaultAddProjectClick(object sender, MouseButtonEventArgs e)
	{
		ProjectOrGroupPopup projectOrGroupPopup = new ProjectOrGroupPopup(DefaultAddProjectPopup, new ProjectExtra
		{
			ProjectIds = new List<string> { _currentId }
		}, new ProjectSelectorExtra
		{
			showAll = false,
			batchMode = false,
			canSelectGroup = false,
			onlyShowPermission = true,
			showNoteProject = false,
			showSharedProject = true,
			CanSearch = true
		});
		projectOrGroupPopup.ItemSelect += OnDefaultProjectSelect;
		projectOrGroupPopup.Show();
	}

	private void OnDefaultProjectSelect(object sender, SelectableItemViewModel e)
	{
		if (e is ProjectGroupViewModel)
		{
			return;
		}
		DefaultAddProjectPopup.IsOpen = false;
		ProjectModel projectById = CacheManager.GetProjectById(e.Id);
		if (projectById != null)
		{
			if (projectById.IsShareList() && new CustomerDialog(Utils.GetString("SetShareProjectAsDefault"), Utils.GetString("SetShareProjectAsDefaultDesc"), Utils.GetString("OK"), Utils.GetString("Cancel"), Window.GetWindow(this)).ShowDialog() != true)
			{
				return;
			}
			DefaultAddProjectNameText.Text = projectById.name;
			_currentId = projectById.id;
		}
		OkButton.IsEnabled = _currentId != _taskDefault?.ProjectId;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/project/modifydefaultprojectdialog.xaml", UriKind.Relative);
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
			((Border)target).MouseLeftButtonUp += OnDefaultAddProjectClick;
			break;
		case 2:
			DefaultAddProjectNameText = (EmjTextBlock)target;
			break;
		case 3:
			DefaultAddProjectPopup = (EscPopup)target;
			break;
		case 4:
			OkButton = (Button)target;
			OkButton.Click += OnSaveClick;
			break;
		case 5:
			((Button)target).Click += OnCancelClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
