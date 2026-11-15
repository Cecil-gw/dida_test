using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Filter;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Project;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.Views.Search;

public class SearchFilterControl : UserControl, IComponentConnector
{
	private enum SearchFilterItemKind
	{
		TaskType,
		List,
		Tag,
		Date,
		Priority,
		Assign,
		Status,
		More,
		TaskContentType
	}

	private sealed class FilterPanelItem
	{
		public SearchFilterItemKind Kind { get; }

		public UIElement Button { get; }

		public UIElement Popup { get; }

		public int OriginalIndex { get; }

		public FilterPanelItem(SearchFilterItemKind kind, UIElement button, UIElement popup, int originalIndex)
		{
			Kind = kind;
			Button = button;
			Popup = popup;
			OriginalIndex = originalIndex;
		}
	}

	public static readonly DependencyProperty ShowCategoryFilterProperty = DependencyProperty.Register("ShowCategoryFilter", typeof(bool), typeof(SearchFilterControl), new PropertyMetadata(false));

	public static readonly DependencyProperty ShowTaskContentTypeFilterButtonProperty = DependencyProperty.Register("ShowTaskContentTypeFilterButton", typeof(bool), typeof(SearchFilterControl), new PropertyMetadata(false));

	private bool _isEditingTime;

	private AssigneeEditDialog _assignEditDialog;

	private PriorityEditDialog _priorityEditDialog;

	private bool _compactMode;

	private FrameworkElement _filterButtonMouseUpToSuppress;

	private bool _listeningForFilterPopupMouseDown;

	private Point _filterScrollStartPoint;

	private bool _isFilterScrollDragging;

	private bool _filterScrollDragMoved;

	private bool _syncGlobalSearchState = true;

	private readonly List<SearchFilterItemKind> _activeFilterOrder = new List<SearchFilterItemKind>();

	private List<FilterPanelItem> _filterPanelItems;

	internal Grid RootGrid;

	internal RowDefinition HeaderRow;

	internal Grid CloseSearchButton;

	internal Grid TopContainer;

	internal Button SaveAsFilterButton;

	internal Grid FilterPanelHost;

	internal ScrollViewer FilterScrollViewer;

	internal StackPanel FilterPanel;

	internal PopupPlacementBorder TaskTypeFilter;

	internal EscPopup TaskTypeFilterPopup;

	internal PopupPlacementBorder ProjectOrGroupFilterText;

	internal EscPopup ListFilterPopup;

	internal PopupPlacementBorder TagFilterText;

	internal EscPopup TagFilterPopup;

	internal PopupPlacementBorder DateFilterText;

	internal EscPopup DateSelectPopup;

	internal PopupPlacementBorder PriorityFilter;

	internal EscPopup PriorityFilterPopup;

	internal PopupPlacementBorder AssignFilter;

	internal EscPopup AssignFilterPopup;

	internal PopupPlacementBorder StatusFilterText;

	internal EscPopup StatusFilterPopup;

	internal PopupPlacementBorder TaskContentTypeFilter;

	internal EscPopup TaskContentTypeFilterPopup;

	internal PopupPlacementBorder MoreFilter;

	internal EscPopup MoreFilterPopup;

	internal Grid FilterScrollLeftGrid;

	internal Grid FilterScrollRightGrid;

	private bool _contentLoaded;

	private SearchFilterViewModel ViewModel { get; set; }

	public bool PopopOpened => GetFilterPopups().Any((Popup popup) => popup.IsOpen);

	public bool ShowCategoryFilter
	{
		get
		{
			return (bool)GetValue(ShowCategoryFilterProperty);
		}
		set
		{
			SetValue(ShowCategoryFilterProperty, value);
		}
	}

	public bool ShowTaskContentTypeFilterButton
	{
		get
		{
			return (bool)GetValue(ShowTaskContentTypeFilterButtonProperty);
		}
		set
		{
			SetValue(ShowTaskContentTypeFilterButtonProperty, value);
		}
	}

	public event EventHandler FilterChanged;

	public SearchFilterControl()
	{
		InitializeComponent();
		ViewModel = SearchHelper.SearchFilter;
		RegisterFilterPopupClosedHandlers();
		base.Loaded += SearchFilterControl_Loaded;
		base.Unloaded += SearchFilterControl_Unloaded;
		ViewModel.AssignFilterEnable = CacheManager.GetProjects().Any((ProjectModel p) => p.IsShareList());
		ViewModel.TaskTypeFilterEnable = LocalSettings.Settings.IsNoteEnabled;
		ApplyCompactVisibility();
		base.DataContext = ViewModel;
		InitializeFilterPanelItems();
		RefreshFilterPanelOrder();
	}

	private void RegisterFilterPopupClosedHandlers()
	{
		foreach (Popup filterPopup in GetFilterPopups())
		{
			filterPopup.Closed += FilterPopupClosed;
		}
	}

	private void FilterPopupClosed(object sender, EventArgs e)
	{
		if (Mouse.LeftButton == MouseButtonState.Pressed)
		{
			FrameworkElement filterButtonForPopup = GetFilterButtonForPopup(sender as Popup);
			if (IsMouseOverElement(filterButtonForPopup))
			{
				IgnoreCurrentFilterButtonMouseUp(filterButtonForPopup);
			}
		}
	}

	private void SearchFilterControl_Loaded(object sender, RoutedEventArgs e)
	{
		if (!_listeningForFilterPopupMouseDown)
		{
			InputManager.Current.PreProcessInput += OnPreProcessFilterPopupInput;
			_listeningForFilterPopupMouseDown = true;
		}
	}

	private void SearchFilterControl_Unloaded(object sender, RoutedEventArgs e)
	{
		ClearIgnoredFilterMouseUp();
		if (_listeningForFilterPopupMouseDown)
		{
			InputManager.Current.PreProcessInput -= OnPreProcessFilterPopupInput;
			_listeningForFilterPopupMouseDown = false;
		}
	}

	public void UseInSearchPopup(SearchFilterViewModel viewModel)
	{
		ViewModel = viewModel ?? new SearchFilterViewModel();
		_syncGlobalSearchState = false;
		_compactMode = true;
		RootGrid.Margin = new Thickness(20.0, 0.0, 20.0, 0.0);
		HeaderRow.Height = new GridLength(0.0);
		HeaderRow.MinHeight = 0.0;
		CloseSearchButton.Visibility = Visibility.Collapsed;
		TopContainer.Visibility = Visibility.Collapsed;
		SaveAsFilterButton.Visibility = Visibility.Collapsed;
		FilterPanelHost.Margin = new Thickness(0.0, 14.0, 0.0, 0.0);
		FilterPanel.Margin = new Thickness(0.0, 0.0, -8.0, 0.0);
		Reset();
	}

	private void RefreshViewModelState(bool syncCategoryFromGlobal)
	{
		ViewModel.AssignFilterEnable = CacheManager.GetProjects().Any((ProjectModel p) => p.IsShareList());
		ViewModel.TaskTypeFilterEnable = LocalSettings.Settings.IsNoteEnabled;
		if (syncCategoryFromGlobal)
		{
			ViewModel.Category = SearchHelper.SearchCategory;
		}
		ViewModel.SyncSearchTypeFromCategory();
		ViewModel.RefreshCategoryState();
		ApplyCompactVisibility();
	}

	private void ApplyCompactVisibility()
	{
		RefreshLocalFilterVisibility();
		if (!_compactMode)
		{
			ViewModel.ShowTaskTypeFilter = false;
			ViewModel.ShowTaskContentTypeFilter = false;
			return;
		}
		bool flag = ViewModel.Category == SearchCategory.None || ViewModel.Category == SearchCategory.Task;
		ViewModel.ShowTaskTypeFilter = false;
		bool flag2 = ViewModel.Category == SearchCategory.Task && ViewModel.TaskTypeFilterEnable;
		if (!flag2 && ViewModel.SelectedType != TaskType.TaskAndNote)
		{
			ViewModel.SelectedType = TaskType.TaskAndNote;
		}
		ViewModel.ShowTaskContentTypeFilter = flag2;
		ViewModel.ShowDateFilter = flag || ViewModel.Category == SearchCategory.CalendarEvent;
		ViewModel.ShowListFilter = flag || ViewModel.Category == SearchCategory.CalendarEvent;
		ViewModel.ShowTagFilter = flag;
		ViewModel.ShowPriorityFilter = flag;
		ViewModel.ShowAssignFilter = flag && ViewModel.AssignFilterEnable;
		ViewModel.ShowStatusFilter = false;
	}

	private void RefreshLocalFilterVisibility()
	{
		ShowCategoryFilter = _compactMode;
		ShowTaskContentTypeFilterButton = ViewModel.TaskTypeFilterEnable && (ViewModel.Category == SearchCategory.None || ViewModel.Category == SearchCategory.Task);
	}

	private void InitializeFilterPanelItems()
	{
		_filterPanelItems = new List<FilterPanelItem>
		{
			new FilterPanelItem(SearchFilterItemKind.TaskType, TaskTypeFilter, TaskTypeFilterPopup, 0),
			new FilterPanelItem(SearchFilterItemKind.List, ProjectOrGroupFilterText, ListFilterPopup, 1),
			new FilterPanelItem(SearchFilterItemKind.Tag, TagFilterText, TagFilterPopup, 2),
			new FilterPanelItem(SearchFilterItemKind.Date, DateFilterText, DateSelectPopup, 3),
			new FilterPanelItem(SearchFilterItemKind.Priority, PriorityFilter, PriorityFilterPopup, 4),
			new FilterPanelItem(SearchFilterItemKind.Assign, AssignFilter, AssignFilterPopup, 5),
			new FilterPanelItem(SearchFilterItemKind.Status, StatusFilterText, StatusFilterPopup, 6),
			new FilterPanelItem(SearchFilterItemKind.TaskContentType, TaskContentTypeFilter, TaskContentTypeFilterPopup, 7),
			new FilterPanelItem(SearchFilterItemKind.More, MoreFilter, MoreFilterPopup, 8)
		};
	}

	private void RefreshFilterPanelOrder()
	{
		if (_filterPanelItems == null)
		{
			return;
		}
		SyncActiveFilterOrder();
		IEnumerable<FilterPanelItem> first = from kind in _activeFilterOrder
			select _filterPanelItems.FirstOrDefault((FilterPanelItem item) => item.Kind == kind) into item
			where item != null
			select item;
		IOrderedEnumerable<FilterPanelItem> second = from item in _filterPanelItems
			where !_activeFilterOrder.Contains(item.Kind)
			orderby item.OriginalIndex
			select item;
		FilterPanel.Children.Clear();
		foreach (FilterPanelItem item in first.Concat(second))
		{
			FilterPanel.Children.Add(item.Button);
			FilterPanel.Children.Add(item.Popup);
		}
	}

	private void SyncActiveFilterOrder()
	{
		List<SearchFilterItemKind> activeKinds = (from item in _filterPanelItems
			where IsFilterItemActive(item.Kind)
			orderby item.OriginalIndex
			select item.Kind).ToList();
		_activeFilterOrder.RemoveAll((SearchFilterItemKind kind) => !activeKinds.Contains(kind));
		foreach (SearchFilterItemKind item in activeKinds)
		{
			if (!_activeFilterOrder.Contains(item))
			{
				_activeFilterOrder.Add(item);
			}
		}
	}

	private bool IsFilterItemActive(SearchFilterItemKind kind)
	{
		return kind switch
		{
			SearchFilterItemKind.TaskType => ViewModel.HasTypeFilter, 
			SearchFilterItemKind.TaskContentType => ViewModel.HasTaskContentTypeFilter, 
			SearchFilterItemKind.List => ViewModel.HasProjectOrGroupFilter, 
			SearchFilterItemKind.Tag => ViewModel.HasTagFilter, 
			SearchFilterItemKind.Date => ViewModel.HasDateFilter, 
			SearchFilterItemKind.Priority => ViewModel.HasPriorityFilter, 
			SearchFilterItemKind.Assign => ViewModel.HasAssignFilter, 
			SearchFilterItemKind.Status => ViewModel.HasStatusFilter, 
			_ => false, 
		};
	}

	private static bool IsClearFilterSource(MouseButtonEventArgs e)
	{
		for (DependencyObject dependencyObject = e.OriginalSource as DependencyObject; dependencyObject != null; dependencyObject = GetParent(dependencyObject))
		{
			if (dependencyObject is FrameworkElement frameworkElement && object.Equals(frameworkElement.Tag, "FilterClear"))
			{
				return true;
			}
		}
		return false;
	}

	private static DependencyObject GetParent(DependencyObject current)
	{
		if (current is Visual || current is Visual3D)
		{
			return VisualTreeHelper.GetParent(current);
		}
		if (current is FrameworkContentElement frameworkContentElement)
		{
			return frameworkContentElement.Parent;
		}
		if (!(current is ContentElement reference))
		{
			return null;
		}
		return ContentOperations.GetParent(reference);
	}

	private static bool IgnoreSelectedFilterClick(MouseButtonEventArgs e, bool selected)
	{
		if (!selected || IsClearFilterSource(e))
		{
			return false;
		}
		e.Handled = true;
		return true;
	}

	private bool ConsumeIgnoredFilterMouseUp(FrameworkElement element, MouseButtonEventArgs e)
	{
		if (_filterButtonMouseUpToSuppress == null)
		{
			return false;
		}
		e.Handled = true;
		ClearIgnoredFilterMouseUp();
		return true;
	}

	private void IgnoreCurrentFilterButtonMouseUp(FrameworkElement element)
	{
		_filterButtonMouseUpToSuppress = element;
	}

	private void ClearIgnoredFilterMouseUp()
	{
		_filterButtonMouseUpToSuppress = null;
		if (base.IsMouseCaptured)
		{
			ReleaseMouseCapture();
		}
	}

	private void FilterButtonPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (sender is FrameworkElement element)
		{
			HandleFilterButtonPreviewMouseLeftButtonDown(element, e);
		}
	}

	private void HandleFilterButtonPreviewMouseLeftButtonDown(FrameworkElement element, MouseButtonEventArgs e)
	{
		if (!HandleClearFilterMouseDown(element, e))
		{
			e.Handled = true;
		}
	}

	private bool HandleClearFilterMouseDown(FrameworkElement element, MouseButtonEventArgs e)
	{
		if (!IsFilterSelected(element))
		{
			return false;
		}
		if (!(e.GetPosition(element).X >= element.ActualWidth - 26.0) && !IsClearFilterSource(e))
		{
			return false;
		}
		if (ClearFilterByElement(element))
		{
			CloseFilterPopup(element);
			MarkFilterButtonMouseDownHandled(element, e);
		}
		return true;
	}

	private void MarkFilterButtonMouseDownHandled(FrameworkElement element, MouseButtonEventArgs e)
	{
		IgnoreCurrentFilterButtonMouseUp(element);
		e.Handled = true;
	}

	private bool TryHandleFilterButtonMouseUp(FrameworkElement element, MouseButtonEventArgs e, bool ignoreSelectedBodyClick = false)
	{
		if (ConsumeIgnoredFilterMouseUp(element, e))
		{
			return true;
		}
		if (ignoreSelectedBodyClick && IgnoreSelectedFilterClick(e, IsFilterSelected(element)))
		{
			return true;
		}
		if (IsClearFilterSource(e))
		{
			return true;
		}
		if (element == null)
		{
			return false;
		}
		if (TryCloseOpenFilterPopup(element))
		{
			e.Handled = true;
			return true;
		}
		CloseOtherFilterPopups(GetFilterPopup(element));
		return false;
	}

	private void OnPreProcessFilterPopupInput(object sender, PreProcessInputEventArgs e)
	{
		if (e.StagingItem.Input is MouseButtonEventArgs { ChangedButton: MouseButton.Left } e2)
		{
			if (e2.ButtonState == MouseButtonState.Released)
			{
				HandleFilterPopupMouseUpPreProcess(e2);
			}
			else if (e2.ButtonState == MouseButtonState.Pressed && PopopOpened && (GetFilterButtonFromEventSource(e2) ?? GetFilterButtonUnderMouse()) == null && !IsFilterPopupEventSource(e2) && !IsMouseOverAnyFilterPopup())
			{
				CloseAllFilterPopups();
			}
		}
	}

	private void HandleFilterPopupMouseUpPreProcess(MouseButtonEventArgs e)
	{
		if (_filterButtonMouseUpToSuppress != null)
		{
			if ((GetFilterButtonFromEventSource(e) ?? GetFilterButtonUnderMouse()) == _filterButtonMouseUpToSuppress || IsMouseOverElement(_filterButtonMouseUpToSuppress))
			{
				e.Handled = true;
			}
			base.Dispatcher.BeginInvoke(new Action(ClearIgnoredFilterMouseUp));
		}
	}

	private FrameworkElement GetFilterButtonUnderMouse()
	{
		return GetFilterButtons().FirstOrDefault(IsMouseOverElement);
	}

	private bool IsFilterPopupEventSource(MouseButtonEventArgs e)
	{
		DependencyObject source = e.OriginalSource as DependencyObject;
		if (source != null)
		{
			return GetFilterPopups().Any((Popup popup) => IsDescendantOf(source, popup.Child));
		}
		return false;
	}

	private bool IsMouseOverAnyFilterPopup()
	{
		if (!(from popup in GetFilterPopups()
			select popup.Child as FrameworkElement).Any(IsMouseOverElement))
		{
			return IsMouseOverNestedFilterPopup();
		}
		return true;
	}

	private bool IsMouseOverNestedFilterPopup()
	{
		if (DateSelectPopup.Child is TimeFilterDialog timeFilterDialog)
		{
			return timeFilterDialog.IsMouseOverNestedPopup();
		}
		return false;
	}

	private IEnumerable<FrameworkElement> GetFilterButtons()
	{
		yield return TaskTypeFilter;
		yield return ProjectOrGroupFilterText;
		yield return TagFilterText;
		yield return DateFilterText;
		yield return PriorityFilter;
		yield return AssignFilter;
		yield return StatusFilterText;
		yield return MoreFilter;
		yield return TaskContentTypeFilter;
	}

	private static bool IsMouseOverElement(FrameworkElement element)
	{
		if (element == null || !element.IsVisible || element.ActualWidth <= 0.0 || element.ActualHeight <= 0.0)
		{
			return false;
		}
		Point position = Mouse.GetPosition(element);
		if (position.X >= 0.0 && position.Y >= 0.0 && position.X <= element.ActualWidth)
		{
			return position.Y <= element.ActualHeight;
		}
		return false;
	}

	private static bool IsDescendantOf(DependencyObject source, DependencyObject ancestor)
	{
		for (DependencyObject dependencyObject = source; dependencyObject != null; dependencyObject = GetParent(dependencyObject))
		{
			if (dependencyObject == ancestor)
			{
				return true;
			}
		}
		return false;
	}

	private bool TryCloseOpenFilterPopup(FrameworkElement element)
	{
		Popup filterPopup = GetFilterPopup(element);
		if (filterPopup == null || !filterPopup.IsOpen)
		{
			return false;
		}
		filterPopup.IsOpen = false;
		return true;
	}

	private void CloseFilterPopup(FrameworkElement element)
	{
		Popup filterPopup = GetFilterPopup(element);
		if (filterPopup != null)
		{
			filterPopup.IsOpen = false;
		}
	}

	private void CloseOtherFilterPopups(Popup popupToKeep)
	{
		foreach (Popup filterPopup in GetFilterPopups())
		{
			if (filterPopup != popupToKeep)
			{
				filterPopup.IsOpen = false;
			}
		}
	}

	private void CloseAllFilterPopups()
	{
		CloseOtherFilterPopups(null);
	}

	private IEnumerable<Popup> GetFilterPopups()
	{
		yield return TaskTypeFilterPopup;
		yield return ListFilterPopup;
		yield return TagFilterPopup;
		yield return DateSelectPopup;
		yield return PriorityFilterPopup;
		yield return AssignFilterPopup;
		yield return StatusFilterPopup;
		yield return MoreFilterPopup;
		yield return TaskContentTypeFilterPopup;
	}

	private FrameworkElement GetFilterButtonFromEventSource(MouseButtonEventArgs e)
	{
		for (DependencyObject dependencyObject = e.OriginalSource as DependencyObject; dependencyObject != null; dependencyObject = GetParent(dependencyObject))
		{
			if (dependencyObject is FrameworkElement frameworkElement && GetFilterPopup(frameworkElement) != null)
			{
				return frameworkElement;
			}
		}
		return null;
	}

	private void OpenFilterPopup(FrameworkElement element, MouseButtonEventArgs e)
	{
		if (element == TaskTypeFilter)
		{
			TaskTypeFilterClick(element, e);
		}
		else if (element == ProjectOrGroupFilterText)
		{
			ProjectOrGroupFilterClick(element, e);
		}
		else if (element == TagFilterText)
		{
			TagFilterClick(element, e);
		}
		else if (element == DateFilterText)
		{
			DateFilterClick(element, e);
		}
		else if (element == StatusFilterText)
		{
			StatusFilterClick(element, e);
		}
		else if (element == PriorityFilter)
		{
			PriorityFilterClick(element, e);
		}
		else if (element == AssignFilter)
		{
			AssignFilterClick(element, e);
		}
		else if (element == MoreFilter)
		{
			MoreFilterClick(element, e);
		}
		else if (element == TaskContentTypeFilter)
		{
			TaskContentTypeFilterClick(element, e);
		}
	}

	private Popup GetFilterPopup(FrameworkElement element)
	{
		if (element == TaskTypeFilter)
		{
			return TaskTypeFilterPopup;
		}
		if (element == ProjectOrGroupFilterText)
		{
			return ListFilterPopup;
		}
		if (element == TagFilterText)
		{
			return TagFilterPopup;
		}
		if (element == DateFilterText)
		{
			return DateSelectPopup;
		}
		if (element == StatusFilterText)
		{
			return StatusFilterPopup;
		}
		if (element == PriorityFilter)
		{
			return PriorityFilterPopup;
		}
		if (element == AssignFilter)
		{
			return AssignFilterPopup;
		}
		if (element == MoreFilter)
		{
			return MoreFilterPopup;
		}
		if (element == TaskContentTypeFilter)
		{
			return TaskContentTypeFilterPopup;
		}
		return null;
	}

	private FrameworkElement GetFilterButtonForPopup(Popup popup)
	{
		if (popup == null)
		{
			return null;
		}
		return GetFilterButtons().FirstOrDefault((FrameworkElement button) => GetFilterPopup(button) == popup);
	}

	private bool IsFilterSelected(FrameworkElement element)
	{
		if (element == TaskTypeFilter)
		{
			return ViewModel.HasTypeFilter;
		}
		if (element == ProjectOrGroupFilterText)
		{
			return ViewModel.HasProjectOrGroupFilter;
		}
		if (element == TagFilterText)
		{
			return ViewModel.HasTagFilter;
		}
		if (element == DateFilterText)
		{
			return ViewModel.HasDateFilter;
		}
		if (element == StatusFilterText)
		{
			return ViewModel.HasStatusFilter;
		}
		if (element == PriorityFilter)
		{
			return ViewModel.HasPriorityFilter;
		}
		if (element == AssignFilter)
		{
			return ViewModel.HasAssignFilter;
		}
		if (element == TaskContentTypeFilter)
		{
			return ViewModel.HasTaskContentTypeFilter;
		}
		return false;
	}

	private bool ClearFilterByElement(FrameworkElement element)
	{
		if (element == TaskTypeFilter)
		{
			ResetAllFilterConditions();
			return true;
		}
		if (element == ProjectOrGroupFilterText)
		{
			ViewModel.SelectedProjectIds = new List<string>();
			ViewModel.SelectedProjectGroupIds = new List<string>();
			ViewModel.SelectedCalendarAccountIds = new List<string>();
			ViewModel.SelectedCalendarIds = new List<string>();
			ViewModel.SelectedProjectDisplayText = ViewModel.GetDefaultProjectDisplayText();
			ViewModel.ProjectOrGroupFilter = ProjectOrGroupFilter.All;
			NotifyFilterChanged();
			return true;
		}
		if (element == TagFilterText)
		{
			ViewModel.SelectedTags = new List<string>();
			ViewModel.SelectedTagDisplayText = Utils.GetString("AllTags");
			NotifyFilterChanged();
			return true;
		}
		if (element == DateFilterText)
		{
			ViewModel.DateFilter = DateFilter.All;
			ViewModel.StartDate = null;
			ViewModel.EndDate = null;
			NotifyFilterChanged();
			return true;
		}
		if (element == StatusFilterText)
		{
			ViewModel.StatusFilter = StatusFilter.All;
			NotifyFilterChanged();
			return true;
		}
		if (element == PriorityFilter)
		{
			ViewModel.SelectedPriorities = new List<int>();
			NotifyFilterChanged();
			return true;
		}
		if (element == AssignFilter)
		{
			ViewModel.SelectedAssignees = new List<string>();
			NotifyFilterChanged();
			return true;
		}
		if (element == TaskContentTypeFilter)
		{
			ViewModel.SelectedType = TaskType.TaskAndNote;
			NotifyFilterChanged();
			return true;
		}
		return false;
	}

	private void NotifyFilterChanged(string type = null)
	{
		if (_syncGlobalSearchState)
		{
			SearchHelper.SearchCategory = ViewModel.Category;
		}
		SearchHelper.ClearTaskSearchModels();
		ViewModel.Searched = false;
		ApplyCompactVisibility();
		ViewModel.NotifyFilterStateChanged();
		RefreshFilterPanelOrder();
		ScheduleFilterScrollButtonUpdate();
		this.FilterChanged?.Invoke(this, null);
		if (!string.IsNullOrEmpty(type))
		{
			UserActCollectUtils.AddClickEvent("search", "filter", type);
		}
	}

	private void FilterScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
	{
		UpdateFilterScrollButtons();
	}

	private void FilterScrollViewerSizeChanged(object sender, SizeChangedEventArgs e)
	{
		ScheduleFilterScrollButtonUpdate();
	}

	private void FilterPanelSizeChanged(object sender, SizeChangedEventArgs e)
	{
		ScheduleFilterScrollButtonUpdate();
	}

	private void FilterScrollViewerPreviewMouseWheel(object sender, MouseWheelEventArgs e)
	{
		if (!IsFilterPopupWheelEventSource(e) && !(FilterScrollViewer.ScrollableWidth <= 0.5))
		{
			ScrollFiltersBy((double)(-e.Delta) * 0.5);
			e.Handled = true;
		}
	}

	private bool IsFilterPopupWheelEventSource(MouseWheelEventArgs e)
	{
		DependencyObject source = e.OriginalSource as DependencyObject;
		if (source != null)
		{
			return (from popup in GetFilterPopups()
				where popup.IsOpen
				select popup).Any((Popup popup) => IsDescendantOf(source, popup.Child));
		}
		return false;
	}

	private void FilterScrollViewerPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		_filterScrollStartPoint = e.GetPosition(FilterScrollViewer);
		_isFilterScrollDragging = true;
		_filterScrollDragMoved = false;
	}

	private void FilterScrollViewerPreviewMouseMove(object sender, MouseEventArgs e)
	{
		if (_isFilterScrollDragging && e.LeftButton == MouseButtonState.Pressed && !(FilterScrollViewer.ScrollableWidth <= 0.5))
		{
			Point position = e.GetPosition(FilterScrollViewer);
			double num = _filterScrollStartPoint.X - position.X;
			if (!(Math.Abs(num) < 2.0))
			{
				_filterScrollDragMoved = true;
				ScrollFiltersBy(num);
				_filterScrollStartPoint = position;
				e.Handled = true;
			}
		}
	}

	private void FilterScrollViewerPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		if (_filterScrollDragMoved)
		{
			Mouse.Capture(null);
			e.Handled = true;
		}
		_isFilterScrollDragging = false;
		_filterScrollDragMoved = false;
	}

	private void FilterScrollViewerMouseLeave(object sender, MouseEventArgs e)
	{
		_isFilterScrollDragging = false;
		_filterScrollDragMoved = false;
	}

	private void ScrollFiltersLeftClick(object sender, MouseButtonEventArgs e)
	{
		ScrollFiltersBy(0.0 - GetFilterScrollPageWidth());
		e.Handled = true;
	}

	private void ScrollFiltersRightClick(object sender, MouseButtonEventArgs e)
	{
		ScrollFiltersBy(GetFilterScrollPageWidth());
		e.Handled = true;
	}

	private double GetFilterScrollPageWidth()
	{
		return Math.Max(120.0, FilterScrollViewer.ViewportWidth * 0.75);
	}

	private void ScrollFiltersBy(double offset)
	{
		double val = FilterScrollViewer.HorizontalOffset + offset;
		val = Math.Max(0.0, Math.Min(FilterScrollViewer.ScrollableWidth, val));
		FilterScrollViewer.ScrollToHorizontalOffset(val);
		UpdateFilterScrollButtons();
	}

	private void ResetFilterScrollPosition()
	{
		FilterScrollViewer.ScrollToHorizontalOffset(0.0);
		ScheduleFilterScrollButtonUpdate();
	}

	private void ScheduleFilterScrollButtonUpdate()
	{
		base.Dispatcher.BeginInvoke(new Action(UpdateFilterScrollButtons));
	}

	private void UpdateFilterScrollButtons()
	{
		if (FilterScrollViewer != null && FilterScrollLeftGrid != null && FilterScrollRightGrid != null)
		{
			bool flag = FilterScrollViewer.ScrollableWidth > 0.5;
			FilterScrollLeftGrid.Visibility = ((!flag || !(FilterScrollViewer.HorizontalOffset > 0.5)) ? Visibility.Collapsed : Visibility.Visible);
			FilterScrollRightGrid.Visibility = ((!flag || !(FilterScrollViewer.HorizontalOffset < FilterScrollViewer.ScrollableWidth - 0.5)) ? Visibility.Collapsed : Visibility.Visible);
		}
	}

	private void StatusFilterClick(object sender, MouseButtonEventArgs e)
	{
		if (TryHandleFilterButtonMouseUp(sender as FrameworkElement, e))
		{
			return;
		}
		DropdownDialog dropdownDialog = new DropdownDialog
		{
			ItemsSource = new List<ListItemData>
			{
				new ListItemData(StatusFilter.All, Utils.GetString("AllStatus")),
				new ListItemData(StatusFilter.Uncompleted, Utils.GetString("Uncompleted")),
				new ListItemData(StatusFilter.Completed, Utils.GetString("Completed")),
				new ListItemData(StatusFilter.Abandoned, Utils.GetString("Abandoned"))
			}
		};
		foreach (ListItemData item in dropdownDialog.ItemsSource)
		{
			if ((StatusFilter)item.Key == ViewModel.StatusFilter)
			{
				item.Selected = true;
			}
		}
		dropdownDialog.OnItemSelected += delegate(object obj, ListItemData item)
		{
			ViewModel.StatusFilter = (StatusFilter)item.Key;
			StatusFilterPopup.IsOpen = false;
			NotifyFilterChanged("status");
		};
		StatusFilterPopup.Child = dropdownDialog;
		StatusFilterPopup.IsOpen = true;
	}

	private void DateFilterClick(object sender, MouseButtonEventArgs e)
	{
		if (TryHandleFilterButtonMouseUp(sender as FrameworkElement, e))
		{
			return;
		}
		TimeFilterDialog timeFilterDialog = new TimeFilterDialog
		{
			ItemsSource = new List<ListItemData>
			{
				new ListItemData(DateFilter.All, SearchDateTextConverter.GetDateFilterText(DateFilter.All)),
				new ListItemData(DateFilter.ThisWeek, SearchDateTextConverter.GetDateFilterText(DateFilter.ThisWeek)),
				new ListItemData(DateFilter.NextWeek, SearchDateTextConverter.GetDateFilterText(DateFilter.NextWeek)),
				new ListItemData(DateFilter.LastWeek, SearchDateTextConverter.GetDateFilterText(DateFilter.LastWeek)),
				new ListItemData(DateFilter.ThisMonth, SearchDateTextConverter.GetDateFilterText(DateFilter.ThisMonth)),
				new ListItemData(DateFilter.LastMonth, SearchDateTextConverter.GetDateFilterText(DateFilter.LastMonth)),
				new ListItemData(DateFilter.Custom, Utils.GetString("Custom"))
			}
		};
		timeFilterDialog.SetStartDate(ViewModel.StartDate);
		timeFilterDialog.SetEndDate(ViewModel.EndDate);
		foreach (ListItemData item in timeFilterDialog.ItemsSource)
		{
			if ((DateFilter)item.Key == ViewModel.DateFilter)
			{
				item.Selected = true;
			}
		}
		timeFilterDialog.OnFilterSelect += delegate(object obj, DateFilterData item)
		{
			ViewModel.DateFilter = item.Type;
			if (item.Type == DateFilter.Custom)
			{
				if (item.StartDate.HasValue)
				{
					ViewModel.StartDate = item.StartDate.Value;
				}
				if (item.EndDate.HasValue)
				{
					ViewModel.EndDate = item.EndDate.Value;
				}
			}
			else
			{
				ViewModel.StartDate = null;
				ViewModel.EndDate = null;
			}
			DateSelectPopup.IsOpen = false;
			NotifyFilterChanged("date");
		};
		timeFilterDialog.OnEndEditDate += async delegate
		{
			_isEditingTime = true;
		};
		timeFilterDialog.OnCancel += delegate
		{
			DateSelectPopup.IsOpen = false;
		};
		DateSelectPopup.Child = timeFilterDialog;
		DateSelectPopup.IsOpen = true;
		DateSelectPopup.Closed += async delegate
		{
			if (_isEditingTime)
			{
				await Task.Delay(1);
				_isEditingTime = false;
			}
		};
	}

	private async void ProjectOrGroupFilterClick(object sender, MouseButtonEventArgs e)
	{
		if (!TryHandleFilterButtonMouseUp(sender as FrameworkElement, e))
		{
			ProjectExtra data = new ProjectExtra
			{
				ProjectIds = ViewModel.SelectedProjectIds.ToList(),
				GroupIds = ViewModel.SelectedProjectGroupIds.ToList(),
				BindAccounts = ViewModel.SelectedCalendarAccountIds.ToList(),
				SubscribeCalendars = ViewModel.SelectedCalendarIds.ToList()
			};
			ProjectOrGroupPopup projectOrGroupPopup = new ProjectOrGroupPopup(ListFilterPopup, data, GetProjectSelectorExtra());
			projectOrGroupPopup.Save += OnProjectSelect;
			projectOrGroupPopup.Show();
		}
	}

	private ProjectSelectorExtra GetProjectSelectorExtra()
	{
		ProjectSelectorExtra projectSelectorExtra = new ProjectSelectorExtra
		{
			showAll = false,
			showListGroup = true,
			showCalendars = (ViewModel.Category != SearchCategory.Task),
			showProjects = (ViewModel.Category != SearchCategory.CalendarEvent),
			popupWidth = 240.0
		};
		projectSelectorExtra.canExpandAllList = false;
		projectSelectorExtra.canExpandAllCalendars = false;
		projectSelectorExtra.highlightAllListChildren = false;
		if (ViewModel.Category == SearchCategory.CalendarEvent)
		{
			projectSelectorExtra.canSelectGroup = false;
		}
		else if (ViewModel.Category == SearchCategory.List)
		{
			projectSelectorExtra.showCalendars = false;
		}
		return projectSelectorExtra;
	}

	private async void OnProjectSelect(object sender, ProjectExtra data)
	{
		if (!data.IsAll)
		{
			ViewModel.SelectedProjectIds = data.ProjectIds.ToList();
			ViewModel.SelectedProjectGroupIds = data.GroupIds.ToList();
			ViewModel.SelectedCalendarAccountIds = data.BindAccounts.ToList();
			ViewModel.SelectedCalendarIds = data.SubscribeCalendars.ToList();
			ViewModel.SelectedProjectDisplayText = ProjectExtra.FormatDisplayText(ProjectExtra.Serialize(data));
			ViewModel.ProjectOrGroupFilter = ProjectOrGroupFilter.Custom;
			UserActCollectUtils.AddClickEvent("search", "filter", "list");
		}
		else
		{
			ViewModel.SelectedProjectIds = new List<string>();
			ViewModel.SelectedProjectGroupIds = new List<string>();
			ViewModel.SelectedCalendarAccountIds = new List<string>();
			ViewModel.SelectedCalendarIds = new List<string>();
			ViewModel.SelectedProjectDisplayText = ViewModel.GetDefaultProjectDisplayText();
			ViewModel.ProjectOrGroupFilter = ProjectOrGroupFilter.All;
		}
		NotifyFilterChanged();
	}

	public void Reset()
	{
		ClearIgnoredFilterMouseUp();
		RefreshViewModelState(_syncGlobalSearchState);
		base.DataContext = ViewModel;
		RefreshFilterPanelOrder();
		if (_compactMode)
		{
			ResetFilterScrollPosition();
		}
		else
		{
			ScheduleFilterScrollButtonUpdate();
		}
	}

	private void TagFilterClick(object sender, MouseButtonEventArgs e)
	{
		if (!TryHandleFilterButtonMouseUp(sender as FrameworkElement, e))
		{
			TagSearchFilterControl tagSearchFilterControl = new TagSearchFilterControl(ViewModel.SelectedTags);
			tagSearchFilterControl.Cancel -= OnTagFilterCancel;
			tagSearchFilterControl.Cancel += OnTagFilterCancel;
			tagSearchFilterControl.Save -= OnTagFilterSelected;
			tagSearchFilterControl.Save += OnTagFilterSelected;
			TagFilterPopup.Child = tagSearchFilterControl;
			TagFilterPopup.IsOpen = true;
		}
	}

	private void OnTagFilterSelected(object sender, List<string> tags)
	{
		TagFilterPopup.IsOpen = false;
		ViewModel.SelectedTags = tags.Select((string t) => TagDataHelper.GetTagDisplayName(t)).ToList();
		ViewModel.SelectedTagDisplayText = TagCardViewModel.ToNormalDisplayText(tags);
		NotifyFilterChanged("tag");
	}

	private void OnTagFilterCancel(object sender, EventArgs e)
	{
		TagFilterPopup.IsOpen = false;
	}

	private void EditSearchClick(object sender, MouseButtonEventArgs e)
	{
		App.Window.StartSearch(reopen: true);
	}

	private void CloseSearch(object sender, MouseButtonEventArgs e)
	{
		App.Window.StopSearch();
	}

	private void PriorityFilterClick(object sender, MouseButtonEventArgs e)
	{
		if (!TryHandleFilterButtonMouseUp(sender as FrameworkElement, e))
		{
			_priorityEditDialog = new PriorityEditDialog(ViewModel.SelectedPriorities ?? new List<int>(), showAll: true);
			_priorityEditDialog.OnSelectedPriorityChanged += delegate(object s, List<int> priorities)
			{
				ViewModel.SelectedPriorities = priorities;
			};
			PriorityFilterPopup.Closed -= PriorityPopup_Closed;
			PriorityFilterPopup.Closed += PriorityPopup_Closed;
			PriorityFilterPopup.Child = _priorityEditDialog;
			_priorityEditDialog.OnCancel += delegate
			{
				PriorityFilterPopup.IsOpen = false;
			};
			_priorityEditDialog.OnSave += delegate
			{
				PriorityFilterPopup.IsOpen = false;
				NotifyFilterChanged("priority");
			};
			PriorityFilterPopup.IsOpen = true;
		}
	}

	private void PriorityPopup_Closed(object sender, EventArgs e)
	{
		if (!_priorityEditDialog.IsSave)
		{
			_priorityEditDialog.Restore();
		}
		_priorityEditDialog.IsSave = false;
	}

	private void AssignFilterClick(object sender, MouseButtonEventArgs e)
	{
		if (!TryHandleFilterButtonMouseUp(sender as FrameworkElement, e))
		{
			_assignEditDialog = new AssigneeEditDialog(ViewModel.SelectedAssignees ?? new List<string>(), showAll: true);
			_assignEditDialog.OnSelectedAssigneeChanged += delegate(object s, List<string> assignees)
			{
				ViewModel.SelectedAssignees = assignees;
			};
			AssignFilterPopup.Closed -= AssigneePopup_Closed;
			AssignFilterPopup.Closed += AssigneePopup_Closed;
			AssignFilterPopup.Child = _assignEditDialog;
			_assignEditDialog.OnCancel += delegate
			{
				AssignFilterPopup.IsOpen = false;
			};
			_assignEditDialog.OnSave += delegate
			{
				AssignFilterPopup.IsOpen = false;
				NotifyFilterChanged("assignee");
			};
			AssignFilterPopup.IsOpen = true;
		}
	}

	private void AssigneePopup_Closed(object sender, EventArgs e)
	{
		if (!_assignEditDialog.IsSave)
		{
			_assignEditDialog.Restore();
		}
		_assignEditDialog.IsSave = false;
	}

	private void TaskTypeFilterClick(object sender, MouseButtonEventArgs e)
	{
		if (TryHandleFilterButtonMouseUp(sender as FrameworkElement, e, ignoreSelectedBodyClick: true))
		{
			return;
		}
		List<ListItemData> list = new List<ListItemData>
		{
			new ListItemData(SearchTypeFilter.All, Utils.GetString("All")),
			new ListItemData(SearchTypeFilter.Task, Utils.GetString("Task"))
		};
		List<SearchTagAndProjectModel> modelsMatched = SearchProjectHelper.GetModelsMatched(string.Empty, onlyProject: false, withInbox: true);
		if (modelsMatched.Any((SearchTagAndProjectModel model) => model.IsTag))
		{
			list.Add(new ListItemData(SearchTypeFilter.Tag, Utils.GetString("Tags")));
		}
		if (modelsMatched.Any((SearchTagAndProjectModel model) => model.IsProject))
		{
			list.Add(new ListItemData(SearchTypeFilter.List, Utils.GetString("List")));
		}
		if (modelsMatched.Any((SearchTagAndProjectModel model) => model.IsFilter))
		{
			list.Add(new ListItemData(SearchTypeFilter.Filter, Utils.GetString("Filter")));
		}
		if (HasVisibleCalendar())
		{
			list.Add(new ListItemData(SearchTypeFilter.CalendarEvent, Utils.GetString("Event", Utils.GetString("Calendar"))));
		}
		DropdownDialog dropdownDialog = new DropdownDialog
		{
			ItemsSource = list
		};
		foreach (ListItemData item in dropdownDialog.ItemsSource)
		{
			if ((SearchTypeFilter)item.Key == ViewModel.SelectedSearchType)
			{
				item.Selected = true;
			}
		}
		dropdownDialog.OnItemSelected += delegate(object obj, ListItemData item)
		{
			ViewModel.ApplySearchType((SearchTypeFilter)item.Key);
			TaskTypeFilterPopup.IsOpen = false;
			ApplyCompactVisibility();
			NotifyFilterChanged("type");
		};
		TaskTypeFilterPopup.Child = dropdownDialog;
		TaskTypeFilterPopup.IsOpen = true;
	}

	private void TaskContentTypeFilterClick(object sender, MouseButtonEventArgs e)
	{
		if (TryHandleFilterButtonMouseUp(sender as FrameworkElement, e))
		{
			return;
		}
		DropdownDialog dropdownDialog = new DropdownDialog
		{
			ItemsSource = new List<ListItemData>
			{
				new ListItemData(TaskType.TaskAndNote, Utils.GetString("All")),
				new ListItemData(TaskType.Task, Utils.GetString("Task")),
				new ListItemData(TaskType.Note, Utils.GetString("Notes"))
			}
		};
		foreach (ListItemData item in dropdownDialog.ItemsSource)
		{
			if ((TaskType)item.Key == ViewModel.SelectedType)
			{
				item.Selected = true;
			}
		}
		dropdownDialog.OnItemSelected += delegate(object obj, ListItemData item)
		{
			ViewModel.SelectedType = (TaskType)item.Key;
			TaskContentTypeFilterPopup.IsOpen = false;
			NotifyFilterChanged("task_type");
		};
		TaskContentTypeFilterPopup.Child = dropdownDialog;
		TaskContentTypeFilterPopup.IsOpen = true;
	}

	private static bool HasVisibleCalendar()
	{
		if (!CacheManager.GetSubscribeCalendars().Any((CalendarSubscribeProfileModel calendar) => calendar.Show != "hidden"))
		{
			return CacheManager.GetBindCalendars().Any((BindCalendarModel calendar) => calendar.Show != "hidden");
		}
		return true;
	}

	private void ShowFilterClick(object sender, object e)
	{
		switch (e as string)
		{
		case "priority":
			ViewModel.ShowPriorityFilter = true;
			break;
		case "assign":
			ViewModel.ShowAssignFilter = true;
			break;
		}
		ScheduleFilterScrollButtonUpdate();
	}

	private void MoreFilterClick(object sender, MouseButtonEventArgs e)
	{
		if (!TryHandleFilterButtonMouseUp(sender as FrameworkElement, e))
		{
			List<CustomMenuItemViewModel> list = new List<CustomMenuItemViewModel>();
			if (!ViewModel.ShowPriorityFilter)
			{
				list.Add(new CustomMenuItemViewModel((object)"priority", Utils.GetString("priority"), (Geometry)null, "")
				{
					ImageWidth = 0.0
				});
			}
			if (ViewModel.AssignFilterEnable && !ViewModel.ShowAssignFilter)
			{
				list.Add(new CustomMenuItemViewModel((object)"assign", Utils.GetString("assignee"), (Geometry)null, "")
				{
					ImageWidth = 0.0
				});
			}
			CustomMenuList customMenuList = new CustomMenuList(list, MoreFilterPopup);
			customMenuList.Operated += ShowFilterClick;
			customMenuList.Show();
		}
	}

	private void ResetTaskTypeFilterClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		ResetAllFilterConditions();
	}

	private void ResetTaskContentTypeFilterClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		ViewModel.SelectedType = TaskType.TaskAndNote;
		NotifyFilterChanged();
	}

	private void ResetAllFilterConditions()
	{
		ViewModel.ResetAllFilterConditions();
		ApplyCompactVisibility();
		NotifyFilterChanged();
	}

	private void ResetProjectFilterClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		ViewModel.SelectedProjectIds = new List<string>();
		ViewModel.SelectedProjectGroupIds = new List<string>();
		ViewModel.SelectedCalendarAccountIds = new List<string>();
		ViewModel.SelectedCalendarIds = new List<string>();
		ViewModel.SelectedProjectDisplayText = ViewModel.GetDefaultProjectDisplayText();
		ViewModel.ProjectOrGroupFilter = ProjectOrGroupFilter.All;
		NotifyFilterChanged();
	}

	private void ResetTagFilterClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		ViewModel.SelectedTags = new List<string>();
		ViewModel.SelectedTagDisplayText = Utils.GetString("AllTags");
		NotifyFilterChanged();
	}

	private void ResetDateFilterClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		ViewModel.DateFilter = DateFilter.All;
		ViewModel.StartDate = null;
		ViewModel.EndDate = null;
		NotifyFilterChanged();
	}

	private void ResetStatusFilterClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		ViewModel.StatusFilter = StatusFilter.All;
		NotifyFilterChanged();
	}

	private void ResetPriorityFilterClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		ViewModel.SelectedPriorities = new List<int>();
		NotifyFilterChanged();
	}

	private void ResetAssignFilterClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		ViewModel.SelectedAssignees = new List<string>();
		NotifyFilterChanged();
	}

	private void SaveAsFilterClick(object sender, RoutedEventArgs e)
	{
		if (ProChecker.CheckPro(ProType.Filter))
		{
			FilterEditDialog filterEditDialog = new FilterEditDialog(ViewModel.ToSearchFilter());
			filterEditDialog.Owner = Window.GetWindow(this);
			filterEditDialog.ShowDialog();
			UserActCollectUtils.AddClickEvent("search", "action", "save_as_filter");
			if (filterEditDialog.Saved)
			{
				Utils.Toast(Utils.GetString("Saved"));
				SyncManager.Sync();
			}
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/search/searchfiltercontrol.xaml", UriKind.Relative);
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
			RootGrid = (Grid)target;
			break;
		case 2:
			HeaderRow = (RowDefinition)target;
			break;
		case 3:
			CloseSearchButton = (Grid)target;
			CloseSearchButton.MouseLeftButtonUp += CloseSearch;
			break;
		case 4:
			TopContainer = (Grid)target;
			break;
		case 5:
			((Border)target).MouseLeftButtonUp += EditSearchClick;
			break;
		case 6:
			SaveAsFilterButton = (Button)target;
			SaveAsFilterButton.Click += SaveAsFilterClick;
			break;
		case 7:
			FilterPanelHost = (Grid)target;
			break;
		case 8:
			FilterScrollViewer = (ScrollViewer)target;
			FilterScrollViewer.PreviewMouseWheel += FilterScrollViewerPreviewMouseWheel;
			FilterScrollViewer.PreviewMouseLeftButtonDown += FilterScrollViewerPreviewMouseLeftButtonDown;
			FilterScrollViewer.PreviewMouseMove += FilterScrollViewerPreviewMouseMove;
			FilterScrollViewer.PreviewMouseLeftButtonUp += FilterScrollViewerPreviewMouseLeftButtonUp;
			FilterScrollViewer.MouseLeave += FilterScrollViewerMouseLeave;
			FilterScrollViewer.ScrollChanged += FilterScrollViewerScrollChanged;
			FilterScrollViewer.SizeChanged += FilterScrollViewerSizeChanged;
			break;
		case 9:
			FilterPanel = (StackPanel)target;
			FilterPanel.SizeChanged += FilterPanelSizeChanged;
			break;
		case 10:
			TaskTypeFilter = (PopupPlacementBorder)target;
			break;
		case 11:
			((Border)target).PreviewMouseLeftButtonUp += ResetTaskTypeFilterClick;
			break;
		case 12:
			TaskTypeFilterPopup = (EscPopup)target;
			break;
		case 13:
			ProjectOrGroupFilterText = (PopupPlacementBorder)target;
			break;
		case 14:
			((Border)target).PreviewMouseLeftButtonUp += ResetProjectFilterClick;
			break;
		case 15:
			ListFilterPopup = (EscPopup)target;
			break;
		case 16:
			TagFilterText = (PopupPlacementBorder)target;
			break;
		case 17:
			((Border)target).PreviewMouseLeftButtonUp += ResetTagFilterClick;
			break;
		case 18:
			TagFilterPopup = (EscPopup)target;
			break;
		case 19:
			DateFilterText = (PopupPlacementBorder)target;
			break;
		case 20:
			((Border)target).PreviewMouseLeftButtonUp += ResetDateFilterClick;
			break;
		case 21:
			DateSelectPopup = (EscPopup)target;
			break;
		case 22:
			PriorityFilter = (PopupPlacementBorder)target;
			break;
		case 23:
			((Border)target).PreviewMouseLeftButtonUp += ResetPriorityFilterClick;
			break;
		case 24:
			PriorityFilterPopup = (EscPopup)target;
			break;
		case 25:
			AssignFilter = (PopupPlacementBorder)target;
			break;
		case 26:
			((Border)target).PreviewMouseLeftButtonUp += ResetAssignFilterClick;
			break;
		case 27:
			AssignFilterPopup = (EscPopup)target;
			break;
		case 28:
			StatusFilterText = (PopupPlacementBorder)target;
			break;
		case 29:
			((Border)target).PreviewMouseLeftButtonUp += ResetStatusFilterClick;
			break;
		case 30:
			StatusFilterPopup = (EscPopup)target;
			break;
		case 31:
			TaskContentTypeFilter = (PopupPlacementBorder)target;
			break;
		case 32:
			((Border)target).PreviewMouseLeftButtonUp += ResetTaskContentTypeFilterClick;
			break;
		case 33:
			TaskContentTypeFilterPopup = (EscPopup)target;
			break;
		case 34:
			MoreFilter = (PopupPlacementBorder)target;
			break;
		case 35:
			MoreFilterPopup = (EscPopup)target;
			break;
		case 36:
			FilterScrollLeftGrid = (Grid)target;
			break;
		case 37:
			((Border)target).MouseLeftButtonUp += ScrollFiltersLeftClick;
			break;
		case 38:
			FilterScrollRightGrid = (Grid)target;
			break;
		case 39:
			((Border)target).MouseLeftButtonUp += ScrollFiltersRightClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
