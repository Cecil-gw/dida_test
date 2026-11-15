using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Shapes;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.Views.Config;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Kanban;
using ticktick_WPF.Views.MainListView;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Project;
using ticktick_WPF.Views.TaskList;

namespace ticktick_WPF.Views.Timeline;

public class TimelineNavBar : UserControl, IComponentConnector
{
	private bool _titleBorderMouseDown;

	internal Grid Container;

	internal Border FoldGrid;

	internal Path FoldImage;

	internal Grid TitleGrid;

	internal Border TitleBorder;

	internal EmojiTitleEditor Title;

	internal HoverIconButton ShareGrid;

	internal HoverIconButton SelectOrderBtn;

	internal HoverIconButton MoreOptBtn;

	internal EscPopup ChooseSortTypePopup;

	internal EscPopup MoreOptPopup;

	private bool _contentLoaded;

	public TimelineNavBar()
	{
		InitializeComponent();
		Title.SetCheckFunc(CheckTitleValid);
		base.SizeChanged += OnSizeChanged;
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs e)
	{
		Title.SetMaxWidth(base.ActualWidth - 150.0);
	}

	private string CheckTitleValid(string text)
	{
		if (base.DataContext is TimelineViewModel timelineViewModel)
		{
			return timelineViewModel.ProjectIdentity?.CheckTitleValid(text);
		}
		return null;
	}

	private void OnTitleTextChanged(object sender, string e)
	{
		if (base.DataContext is TimelineViewModel timelineViewModel)
		{
			timelineViewModel.ProjectIdentity?.SaveTitle(e);
		}
	}

	private void SelectOrderBtnMouseUp(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext is TimelineViewModel { ProjectEnable: not false, ProjectIdentity: not null } timelineViewModel)
		{
			List<SortTypeViewModel> timelineSortTypes = timelineViewModel.ProjectIdentity.GetTimelineSortTypes();
			SortTypeSelector sortTypeSelector = new SortTypeSelector(timelineViewModel.ProjectIdentity, timelineSortTypes, timelineViewModel.TimelineSortOption.Copy(), ChooseSortTypePopup);
			sortTypeSelector.SortOptionSelect += OnSortTypeSelect;
			sortTypeSelector.Show();
		}
	}

	private void OnSortTypeSelect(object sender, SortOption e)
	{
		if (base.DataContext is TimelineViewModel timelineViewModel)
		{
			string text = e.orderBy;
			switch (text)
			{
			case "sortOrder":
				text = "default";
				break;
			case "dueDate":
				text = "time";
				break;
			case "project":
				text = "list";
				break;
			}
			UserActCollectUtils.AddClickEvent("timeline", "sort", text);
			timelineViewModel.TimelineSortOption = e;
			if (e.groupBy == Constants.SortType.sortOrder.ToString() && !string.IsNullOrEmpty(timelineViewModel.ProjectIdentity?.GetProjectId()))
			{
				ColumnBatchHandler.MergeWithServer(timelineViewModel.ProjectIdentity.GetProjectId());
			}
			SyncManager.TryDelaySync(1000);
		}
	}

	private async void MoreOptBtnMouseUp(object sender, MouseButtonEventArgs e)
	{
		MoreOptPopup.IsOpen = true;
		object dataContext = base.DataContext;
		if (!(dataContext is TimelineViewModel model))
		{
			return;
		}
		List<CustomMenuItemViewModel> menuItems = new List<CustomMenuItemViewModel>
		{
			new CustomMenuItemViewModel("showComplete", Utils.GetString(LocalSettings.Settings.HideComplete ? "ShowCompleted" : "HideCompleted"), Utils.GetGeometry(LocalSettings.Settings.HideComplete ? "Ic.TaskOM.ShowCompleted" : "Ic.TaskOM.HideCompleted")),
			new CustomMenuItemViewModel("DisplaySetting", Utils.GetString("DisplaySetting"), Utils.GetGeometry("Ic.TaskOM.ViewSetting")),
			new CustomMenuItemViewModel("ArrangeTask", Utils.GetString("ArrangeTask"), Utils.GetGeometry("Ic.CalendarOM.CalendarFreeTasks"))
		};
		if (model.GroupByEnum == Constants.SortType.sortOrder && model.ProjectEnable)
		{
			menuItems.Add(new CustomMenuItemViewModel("addSection", Utils.GetString("AddSection"), Utils.GetGeometry("Ic.ProjectList.Add")));
		}
		if (model.ProjectIdentity is NormalProjectIdentity normalProjectIdentity)
		{
			ProjectModel projectById = CacheManager.GetProjectById(normalProjectIdentity.Id);
			if (projectById != null)
			{
				if (!projectById.IsShareList() && !projectById.Isinbox)
				{
					menuItems.Add(new CustomMenuItemViewModel("share", Utils.GetString("Share"), Utils.GetGeometry("Ic.TaskOM.Sharing")));
				}
				if (projectById.IsProjectPermit())
				{
					menuItems.Add(new CustomMenuItemViewModel("listActivities", Utils.GetString("ListActivitiesPro"), Utils.GetGeometry("Ic.TaskOM.ListActivities")));
				}
			}
		}
		List<string> list = model.ProjectIdentity?.GetSwitchViewModes();
		SwitchListViewControl switchView = ((list != null) ? new SwitchListViewControl() : null);
		if (list != null)
		{
			switchView.ViewSelected += OnSwitchView;
			switchView.SetButtonStatus(false, list.Contains("kanban") ? new bool?(false) : ((bool?)null), true);
		}
		if (model.ProjectIdentity is NormalProjectIdentity || model.ProjectIdentity is AllProjectIdentity)
		{
			List<string> list2 = await CalendarConnectService.GetNotionConnectedProjectIds();
			if (list2.Contains(model.ProjectIdentity.Id) || (model.ProjectIdentity is AllProjectIdentity && list2.Contains("all")))
			{
				menuItems.Add(new CustomMenuItemViewModel("SyncNotion", Utils.GetString("SyncWithNotion"), Utils.GetGeometry("Ic.Tabbar.Sync"))
				{
					ShowSelected = false
				});
			}
		}
		if (Utils.FindParent<TaskView>(this)?.ShowInCal ?? false)
		{
			menuItems.Add(new CustomMenuItemViewModel("ExitParallel", Utils.GetString("Close"), Utils.GetGeometry("Ic.Common.Close"))
			{
				ImageWidth = 12.0,
				ImageMargin = new Thickness(14.0, 0.0, 0.0, 0.0)
			});
		}
		CustomMenuList customMenuList = new CustomMenuList(menuItems, MoreOptPopup, switchView);
		customMenuList.Operated += OnMoreItemSelected;
		customMenuList.Show();
	}

	private void OnMoreItemSelected(object sender, object e)
	{
		if (!(e is string text) || text == null)
		{
			return;
		}
		switch (text.Length)
		{
		case 12:
			switch (text[0])
			{
			case 's':
				if (text == "showComplete")
				{
					ShowOrHideCompleteClick();
				}
				break;
			case 'E':
				if (text == "ExitParallel")
				{
					UserActCollectUtils.AddClickEvent("split_view", "calendar+task", "close_from_om");
					MainWindow.ShowTaskInCalendarChanged();
				}
				break;
			}
			break;
		case 14:
			switch (text[0])
			{
			case 'D':
				if (text == "DisplaySetting")
				{
					OnSettingsClick();
				}
				break;
			case 'l':
				if (text == "listActivities")
				{
					OnListActivityClick();
				}
				break;
			}
			break;
		case 10:
			switch (text[0])
			{
			case 'a':
				if (text == "addSection")
				{
					AddSectionClick();
				}
				break;
			case 'S':
				if (text == "SyncNotion")
				{
					Communicator.PostNotionSync();
					MainWindowManager.BeginSyncStory();
				}
				break;
			}
			break;
		case 5:
			if (text == "share")
			{
				ShowShareDiaplog();
			}
			break;
		case 11:
			if (text == "ArrangeTask")
			{
				OnArrangeClick();
			}
			break;
		}
	}

	private void OnArrangeClick()
	{
		if (base.DataContext is TimelineViewModel timelineViewModel)
		{
			timelineViewModel.IsArranging = !timelineViewModel.IsArranging;
			if (timelineViewModel.IsArranging)
			{
				UserActCollectUtils.AddClickEvent("timeline", "action_bar", "arrangement");
			}
		}
	}

	private void OnSettingsClick()
	{
		if (base.DataContext is TimelineViewModel timelineViewModel)
		{
			timelineViewModel.IsSetting = true;
			UserActCollectUtils.AddClickEvent("timeline", "action_bar", "view_options");
			TimelineSettingsWindow timelineSettingsWindow = new TimelineSettingsWindow(timelineViewModel);
			timelineSettingsWindow.Owner = Window.GetWindow(this);
			timelineSettingsWindow.ShowDialog();
		}
	}

	private void MenuFoldGridMouseUp(object sender, MouseButtonEventArgs e)
	{
		Utils.FindParent<ListViewContainer>(this)?.ShowProjectMenu();
	}

	private void ShowOrHideCompleteClick()
	{
		if (base.DataContext is TimelineViewModel timelineViewModel)
		{
			UserActCollectUtils.AddClickEvent("timeline", "om", "show_hide_completed");
			timelineViewModel.HideCompleted = !timelineViewModel.HideCompleted;
		}
	}

	private void AddSectionClick()
	{
		UserActCollectUtils.AddClickEvent("timeline", "om", "add_section");
		if (base.DataContext is TimelineViewModel { GroupByEnum: Constants.SortType.sortOrder } timelineViewModel)
		{
			timelineViewModel.AddNewColumn();
		}
	}

	private void ShareGridClick(object sender, MouseButtonEventArgs e)
	{
		ShowShareDiaplog();
	}

	private async Task ShowShareDiaplog()
	{
		if (base.DataContext is TimelineViewModel timelineViewModel)
		{
			ProjectModel projectModel = await ProjectDao.GetProjectById(timelineViewModel.ProjectIdentity?.GetProjectId());
			if (projectModel != null)
			{
				ShareProjectDialog.TryShowShareDialog(projectModel.id, Window.GetWindow(this));
			}
		}
	}

	private async void OnListActivityClick()
	{
		if (ProChecker.CheckPro(ProType.ListActivities) && base.DataContext is TimelineViewModel timelineViewModel && !string.IsNullOrEmpty(timelineViewModel.ProjectIdentity?.GetProjectId()))
		{
			ProjectActivityWindow window = new ProjectActivityWindow(timelineViewModel.ProjectIdentity.GetProjectId())
			{
				Owner = Window.GetWindow(this)
			};
			window.Closed += delegate
			{
				window.Owner?.Activate();
			};
			window.Show();
		}
	}

	private void OnSwitchView(object sender, string e)
	{
		MoreOptPopup.IsOpen = false;
		if (e == "list" || e == "kanban")
		{
			UserActCollectUtils.AddClickEvent("list_mode", "switch", e);
			if (base.DataContext is TimelineViewModel timelineViewModel)
			{
				timelineViewModel.ProjectIdentity?.SwitchViewMode(e, Utils.FindParent<TaskView>(this)?.ShowInCal ?? false);
			}
		}
	}

	public void SetTitle(string title)
	{
		Title.SetText(title);
	}

	public void SetTitleEnable(bool enable)
	{
		Title.SetEnable(enable);
	}

	public void SetModeColumn(bool isOneColumn)
	{
		if (isOneColumn)
		{
			Title.IsHitTestVisible = false;
			TitleBorder.SetResourceReference(FrameworkElement.StyleProperty, "HoverBorderStyle");
			TitleBorder.PreviewMouseLeftButtonDown -= OnTitleBorderMouseDown;
			TitleBorder.PreviewMouseLeftButtonDown += OnTitleBorderMouseDown;
			TitleBorder.MouseLeftButtonUp -= OnTitleClick;
			TitleBorder.MouseLeftButtonUp += OnTitleClick;
		}
		else
		{
			Title.IsHitTestVisible = true;
			TitleBorder.Style = null;
			TitleBorder.PreviewMouseLeftButtonDown -= OnTitleBorderMouseDown;
		}
	}

	private void OnTitleBorderMouseDown(object sender, MouseButtonEventArgs e)
	{
		_titleBorderMouseDown = true;
	}

	private async void OnTitleClick(object sender, MouseButtonEventArgs e)
	{
		if (_titleBorderMouseDown)
		{
			_titleBorderMouseDown = false;
			e.Handled = true;
			await Task.Delay(1);
			Utils.FindParent<ListViewContainer>(this)?.ShowProjectMenu();
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/timeline/timelinenavbar.xaml", UriKind.Relative);
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
			Container = (Grid)target;
			break;
		case 2:
			FoldGrid = (Border)target;
			FoldGrid.MouseLeftButtonUp += MenuFoldGridMouseUp;
			break;
		case 3:
			FoldImage = (Path)target;
			break;
		case 4:
			TitleGrid = (Grid)target;
			break;
		case 5:
			TitleBorder = (Border)target;
			break;
		case 6:
			Title = (EmojiTitleEditor)target;
			break;
		case 7:
			ShareGrid = (HoverIconButton)target;
			break;
		case 8:
			SelectOrderBtn = (HoverIconButton)target;
			break;
		case 9:
			MoreOptBtn = (HoverIconButton)target;
			break;
		case 10:
			ChooseSortTypePopup = (EscPopup)target;
			break;
		case 11:
			MoreOptPopup = (EscPopup)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
