using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using Microsoft.IdentityModel.Tokens;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Project;

public class ProjectOrGroupPopup : UserControl, ITabControl, IComponentConnector
{
	private readonly bool _batchMode;

	private readonly Popup _popup;

	private ItemSelection _projectItems;

	internal Grid SearchGrid;

	internal TextBox SearchProjectTextBox;

	internal Grid OperationPanel;

	internal Grid ItemsContainer;

	internal Grid EmptyGrid;

	private bool _contentLoaded;

	public event EventHandler<SelectableItemViewModel> ItemSelect;

	public event EventHandler Closed;

	public event EventHandler<ProjectExtra> Save;

	public event EventHandler NoSearchResult;

	public ProjectOrGroupPopup(Popup popup, ProjectExtra data, ProjectSelectorExtra extra)
	{
		_popup = popup;
		_batchMode = extra.batchMode;
		InitializeComponent();
		data = data ?? new ProjectExtra();
		if (!extra.ShowInbox.HasValue)
		{
			extra.ShowInbox = data.ProjectIds.IsNullOrEmpty() || data.ProjectIds.Contains(LocalSettings.Settings.InServerBoxId) || LocalSettings.Settings.SmartListInbox != 1;
		}
		if (!extra.isCalendarMode)
		{
			InitProjectItems(data, extra);
			if (!extra.IgnoreMiniHeight)
			{
				_popup.MinHeight = 240.0;
			}
		}
		else
		{
			InitCalendarItems(data, extra.accountId);
			_popup.MinHeight = 0.0;
		}
		if (!_batchMode)
		{
			OperationPanel.Visibility = Visibility.Collapsed;
		}
		if (extra.CanSearch)
		{
			SearchGrid.Visibility = Visibility.Visible;
		}
		_popup.Width = extra.popupWidth;
		_popup.Closed -= OnPopupClosed;
		_popup.Closed += OnPopupClosed;
		base.Loaded += delegate
		{
			DelayFocus();
		};
	}

	public async void DelayFocus()
	{
		if (SearchGrid.Visibility == Visibility.Visible)
		{
			await Task.Delay(250);
			if (!SearchProjectTextBox.IsFocused)
			{
				SearchProjectTextBox.Focus();
			}
		}
	}

	private void InitCalendarItems(ProjectExtra data, string accountId)
	{
		_projectItems = new ItemSelection(_popup)
		{
			AccountId = accountId,
			OriginalData = data,
			CalendarCreateMode = true
		};
		_projectItems.Container.MinHeight = 0.0;
		_projectItems.ItemSelect += OnItemSelect;
		ItemsContainer.Children.Add(_projectItems);
	}

	private void OnPopupClosed(object sender, EventArgs e)
	{
		this.Closed?.Invoke(this, null);
	}

	private void InitProjectItems(ProjectExtra data, ProjectSelectorExtra extra)
	{
		_projectItems = new ItemSelection(_popup, extra.canSelectGroup)
		{
			ShowAll = extra.showAll,
			ShowFilters = extra.showFilters,
			ShowTags = extra.showTags,
			ShowSmartProjects = extra.showSmartProjects,
			ShowCalendars = extra.showCalendars,
			BatchMode = extra.batchMode,
			OriginalData = data,
			CanSelectGroup = extra.canSelectGroup,
			OnlyShowPermission = extra.onlyShowPermission,
			ShowSmartAll = extra.showSmartAll,
			ShowFilterGroup = extra.showFilterGroup,
			ShowNoteProject = extra.showNoteProject,
			ShowSharedProject = extra.showSharedProject,
			ShowListGroup = extra.showListGroup,
			ShowFilterCalendar = extra.showCalendarCategory,
			ShowHabitCategory = extra.showHabitCategory,
			UseCircleCheckForAll = extra.useCircleCheckForAll,
			UseCircleCheckForSectionGroups = extra.useCircleCheckForSectionGroups,
			ShowColumns = extra.ShowColumn,
			SelectedColumn = extra.ColumnId,
			ShowAllProjectsCategory = extra.showAllProjectCategory,
			ShowProjects = extra.showProjects,
			ShowInbox = (extra.ShowInbox ?? true),
			LinkedProjectIds = extra.LinkedProjectIds,
			CanExpandAllList = extra.canExpandAllList,
			CanExpandAllCalendars = extra.canExpandAllCalendars,
			HighlightAllListChildren = extra.highlightAllListChildren
		};
		_projectItems.ItemSelect += OnItemSelect;
		ItemsContainer.Children.Add(_projectItems);
	}

	private async void OnItemSelect(object sender, SelectableItemViewModel model)
	{
		if (!_batchMode)
		{
			_popup.IsOpen = false;
			await Task.Delay(80);
			this.ItemSelect?.Invoke(this, model);
		}
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		_popup.IsOpen = false;
	}

	private void OnSaveClick(object sender, RoutedEventArgs e)
	{
		this.Save?.Invoke(this, _projectItems.GetSelectedData());
		_popup.IsOpen = false;
	}

	public void Show()
	{
		_popup.Child = this;
		_popup.IsOpen = true;
	}

	public void SetData(ProjectExtra data, string columnId = null)
	{
		_projectItems.OriginalData = data;
		_projectItems.SelectedColumn = columnId;
		_projectItems.LoadData();
	}

	public bool ChildMouseOver()
	{
		if (_projectItems != null)
		{
			return _projectItems.ChildMouseOver();
		}
		return false;
	}

	private void OnTextChanged(object sender, TextChangedEventArgs e)
	{
		_projectItems.LoadData(SearchProjectTextBox.Text.Trim());
	}

	public int PerformSearch(string filterContent)
	{
		_projectItems.LoadData(filterContent);
		EmptyGrid.Visibility = ((_projectItems.SelectableItems.Items.Count != 0) ? Visibility.Collapsed : Visibility.Visible);
		if (_projectItems.SelectableItems.Items.Count == 0)
		{
			this.NoSearchResult?.Invoke(this, null);
		}
		return _projectItems.SelectableItems.Items.Count;
	}

	private void OnKeyDown(object sender, KeyEventArgs e)
	{
	}

	public void SetPopupPosition(Point point)
	{
		_popup.HorizontalOffset = point.X;
		_popup.VerticalOffset = point.Y;
	}

	public bool HandleTab(bool shift)
	{
		return _projectItems?.HandleTab(shift) ?? false;
	}

	public bool HandleEnter()
	{
		return _projectItems?.HandleEnter() ?? false;
	}

	public bool HandleEsc()
	{
		return _projectItems?.HandleEsc() ?? false;
	}

	public bool UpDownSelect(bool isUp)
	{
		return _projectItems?.UpDownSelect(isUp) ?? false;
	}

	public bool LeftRightSelect(bool isLeft)
	{
		return _projectItems?.LeftRightSelect(isLeft) ?? false;
	}

	public void EnterSelect()
	{
		_projectItems?.EnterSelect();
	}

	public bool SpaceSelect()
	{
		return _projectItems?.SpaceSelect() ?? false;
	}

	public async void HoverSelectFirst(int delay = 200)
	{
		if (delay > 0)
		{
			await Task.Delay(delay);
		}
		_projectItems?.HoverSelectFirst();
	}

	public void ClosePopup()
	{
		_projectItems.ClosePopup();
	}

	public Popup GetProjectPopup()
	{
		return _popup;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/project/projectorgrouppopup.xaml", UriKind.Relative);
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
			SearchGrid = (Grid)target;
			break;
		case 2:
			SearchProjectTextBox = (TextBox)target;
			SearchProjectTextBox.KeyDown += OnKeyDown;
			SearchProjectTextBox.TextChanged += OnTextChanged;
			break;
		case 3:
			OperationPanel = (Grid)target;
			break;
		case 4:
			((Button)target).Click += OnSaveClick;
			break;
		case 5:
			((Button)target).Click += OnCancelClick;
			break;
		case 6:
			ItemsContainer = (Grid)target;
			break;
		case 7:
			EmptyGrid = (Grid)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
