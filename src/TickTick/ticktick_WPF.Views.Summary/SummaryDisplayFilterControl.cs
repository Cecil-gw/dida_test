using System;
using System.CodeDom.Compiler;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Shapes;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Summary;

public class SummaryDisplayFilterControl : UserControl, IComponentConnector, IStyleConnector
{
	private SummaryFilterViewModel _model;

	private Point _draggingStartPoint;

	private int _startDragIdx;

	internal Path SortByProgressImage;

	internal Path SortByListImage;

	internal Path SortByCompletedDateImage;

	internal Path SortByTaskDateImage;

	internal Path SortByPriorityImage;

	internal Path SortByAssigneeImage;

	internal Path SortByTagImage;

	internal EscPopup SortPopup;

	internal OptionCheckBox SortByProgressButton;

	internal OptionCheckBox SortByListButton;

	internal OptionCheckBox SortByCompletedDateButton;

	internal OptionCheckBox SortByTaskDateButton;

	internal OptionCheckBox SortByTagButton;

	internal OptionCheckBox SortByPriorityButton;

	internal OptionCheckBox SortByAssigneeButton;

	internal Path DisplayFilterImage;

	internal EscPopup FilterPopup;

	internal ItemsControl DisplayItems;

	internal TextBlock ProText;

	internal EscPopup DragItemPopup;

	internal Grid DragContent;

	internal EscPopup ItemStylePopup;

	internal ItemsControl ItemStyleItems;

	private bool _contentLoaded;

	public event EventHandler FilterChanged;

	public SummaryDisplayFilterControl()
	{
		InitializeComponent();
		ItemStylePopup.Closed += delegate
		{
			FilterPopup.LostFocus -= FilterPopup_OnLostFocus;
			FilterPopup.LostFocus += FilterPopup_OnLostFocus;
		};
	}

	private void TryShowProDialog()
	{
		if (!string.IsNullOrEmpty(_model.ProItemsText))
		{
			ProChecker.ShowUpgradeDialog(ProType.SummaryStyle);
			_model.RemoveProItems();
			_model.SaveTemplate();
		}
	}

	private void OnDisplayItemMouseMove(object sender, MouseEventArgs e)
	{
		if (e.LeftButton == MouseButtonState.Pressed && sender is FrameworkElement { DataContext: SummaryDisplayItemViewModel dataContext } frameworkElement && DisplayItems.Items.IndexOf(dataContext) == _startDragIdx)
		{
			Point position = Mouse.GetPosition(frameworkElement);
			if ((Math.Abs(_draggingStartPoint.X - position.X) > 2.0 || Math.Abs(_draggingStartPoint.Y - position.Y) > 2.0) && dataContext.Draggable && !dataContext.IsDragging)
			{
				dataContext.IsDragging = true;
				DragDrop.DoDragDrop(frameworkElement, dataContext, DragDropEffects.Move);
			}
		}
	}

	private void OnDisplayItemDown(object sender, RoutedEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: SummaryDisplayItemViewModel dataContext } frameworkElement)
		{
			_draggingStartPoint = Mouse.GetPosition(frameworkElement);
			if (!dataContext.Draggable)
			{
				_startDragIdx = -1;
			}
			else
			{
				_startDragIdx = DisplayItems.Items.IndexOf(dataContext);
			}
		}
	}

	private void OnDisplayItemClick(object sender, RoutedEventArgs e)
	{
		if (!(sender is FrameworkElement frameworkElement))
		{
			return;
		}
		object dataContext = frameworkElement.DataContext;
		SummaryDisplayItemViewModel model = dataContext as SummaryDisplayItemViewModel;
		if (model == null)
		{
			return;
		}
		if (model.Key == "title")
		{
			Utils.Toast(Utils.GetString("task_name_cannot_be_hidden"));
		}
		if (model.ForceEnabled)
		{
			model.Enabled = true;
			return;
		}
		model.Enabled = !model.Enabled;
		if (_model?.DisplayItems != null)
		{
			_model.SaveSelectedTemplate(delegate(SummaryTemplate it)
			{
				SummaryDisplayItem summaryDisplayItem = it.displayItems?.FirstOrDefault((SummaryDisplayItem i) => i.key == model.Key);
				if (summaryDisplayItem != null)
				{
					summaryDisplayItem.enabled = model.Enabled;
				}
			});
		}
		this.FilterChanged?.Invoke(null, null);
	}

	private void OnSortByClick(object sender, EventArgs e)
	{
		if (sender is FrameworkElement frameworkElement)
		{
			SummarySortType sortBy = _model.SortBy;
			if (Enum.TryParse<SummarySortType>((string)frameworkElement.Tag, out var type))
			{
				_model.SortBy = type;
				SetDisplaySortIcon(sortBy, Visibility.Collapsed);
				SetDisplaySortIcon(_model.SortBy, Visibility.Visible);
				_model.SaveSelectedTemplate(delegate(SummaryTemplate it)
				{
					it.sortType = type.ToString();
				});
				CollectSortByAct(type);
			}
		}
		this.FilterChanged?.Invoke(sender, e);
		SortPopup.IsOpen = false;
	}

	private static void CollectSortByAct(SummarySortType sortBy)
	{
		switch (sortBy)
		{
		case SummarySortType.progress:
			UserActCollectUtils.AddClickEvent("summary", "sort", "completion_status");
			break;
		case SummarySortType.project:
			UserActCollectUtils.AddClickEvent("summary", "sort", "list");
			break;
		case SummarySortType.completedTime:
			UserActCollectUtils.AddClickEvent("summary", "sort", "completed_date");
			break;
		case SummarySortType.dueDate:
			UserActCollectUtils.AddClickEvent("summary", "sort", "task_date");
			break;
		case SummarySortType.priority:
			UserActCollectUtils.AddClickEvent("summary", "sort", "priority");
			break;
		case SummarySortType.assignee:
			UserActCollectUtils.AddClickEvent("summary", "sort", "assignee");
			break;
		case SummarySortType.tag:
			UserActCollectUtils.AddClickEvent("summary", "sort", "tag");
			break;
		}
	}

	private void OnSaveClick(object sender, RoutedEventArgs e)
	{
		CollectDisplayAct();
		this.FilterChanged?.Invoke(null, null);
		FilterPopup.IsOpen = false;
	}

	private void CollectDisplayAct()
	{
		if (_model.ShowCompleteDate)
		{
			UserActCollectUtils.AddClickEvent("summary", "view_options", "completed_time_default");
		}
		if (_model.ShowStatus)
		{
			UserActCollectUtils.AddClickEvent("summary", "view_options", "status_prem");
		}
		if (_model.ShowTaskDate)
		{
			UserActCollectUtils.AddClickEvent("summary", "view_options", "task_time_prem");
		}
		if (_model.ShowProgress)
		{
			UserActCollectUtils.AddClickEvent("summary", "view_options", "progress_default");
		}
		if (_model.ShowDetail)
		{
			UserActCollectUtils.AddClickEvent("summary", "view_options", "detail");
		}
		if (_model.ShowPomo)
		{
			UserActCollectUtils.AddClickEvent("summary", "view_options", "focus_data");
		}
		if (_model.ShowProject)
		{
			UserActCollectUtils.AddClickEvent("summary", "view_options", "list");
		}
		UserActCollectUtils.AddClickEvent("summary", "view_options", "task_title_default");
		if (_model.ShowParent)
		{
			UserActCollectUtils.AddClickEvent("summary", "view_options", "parent_task_prem");
		}
		if (_model.ShowTag)
		{
			UserActCollectUtils.AddClickEvent("summary", "view_options", "tag_prem");
		}
	}

	private void ShowSortPopupClick(object sender, MouseButtonEventArgs e)
	{
		SortByProgressButton.Selected = _model.SortBy == SummarySortType.progress;
		SortByCompletedDateButton.Selected = _model.SortBy == SummarySortType.completedTime;
		SortByListButton.Selected = _model.SortBy == SummarySortType.project;
		SortByPriorityButton.Selected = _model.SortBy == SummarySortType.priority;
		SortByTaskDateButton.Selected = _model.SortBy == SummarySortType.dueDate;
		SortByAssigneeButton.Selected = _model.SortBy == SummarySortType.assignee;
		SortByTagButton.Selected = _model.SortBy == SummarySortType.tag;
		SortPopup.IsOpen = true;
	}

	private void ShowFilterPopupClick(object sender, MouseButtonEventArgs e)
	{
		FilterPopup.IsOpen = true;
	}

	public void InitModel(SummaryFilterViewModel model)
	{
		_model = model;
		SortByProgressImage.Visibility = Visibility.Collapsed;
		SortByListImage.Visibility = Visibility.Collapsed;
		SortByCompletedDateImage.Visibility = Visibility.Collapsed;
		SortByTaskDateImage.Visibility = Visibility.Collapsed;
		SortByPriorityImage.Visibility = Visibility.Collapsed;
		SortByAssigneeImage.Visibility = Visibility.Collapsed;
		SortByTagImage.Visibility = Visibility.Collapsed;
		SetDisplaySortIcon(_model.SortBy, Visibility.Visible);
		base.DataContext = _model;
		if (model.DisplayItems == null || model.DisplayItems.Count == 0)
		{
			model.SetupDefaultItems();
		}
		DisplayItems.ItemsSource = model.DisplayItems;
	}

	private void SetDisplaySortIcon(SummarySortType sortBy, Visibility visible)
	{
		switch (sortBy)
		{
		case SummarySortType.progress:
			SortByProgressImage.Visibility = visible;
			break;
		case SummarySortType.project:
			SortByListImage.Visibility = visible;
			break;
		case SummarySortType.completedTime:
			SortByCompletedDateImage.Visibility = visible;
			break;
		case SummarySortType.dueDate:
			SortByTaskDateImage.Visibility = visible;
			break;
		case SummarySortType.priority:
			SortByPriorityImage.Visibility = visible;
			break;
		case SummarySortType.assignee:
			SortByAssigneeImage.Visibility = visible;
			break;
		case SummarySortType.tag:
			SortByTagImage.Visibility = visible;
			break;
		}
	}

	private void OnFilterPopupClosed(object sender, EventArgs e)
	{
		OnSaveClick(sender, null);
	}

	private void OnMoreItemClick(object sender, MouseButtonEventArgs e)
	{
		if (!(sender is FrameworkElement frameworkElement))
		{
			return;
		}
		object dataContext = frameworkElement.DataContext;
		SummaryDisplayItemViewModel model = dataContext as SummaryDisplayItemViewModel;
		if (model != null && model.SupportedStyles != null && model.SupportedStyles.Count > 0)
		{
			if (model.Style == null)
			{
				model.Style = model.SupportedStyles.FirstOrDefault();
			}
			if (!model.Enabled)
			{
				model.Style = null;
			}
			ItemStyleItems.ItemsSource = new ObservableCollection<SummaryDisplayStyleItemModel>(model.SupportedStyles.Select((string it) => new SummaryDisplayStyleItemModel
			{
				Key = it,
				Name = SummaryDisplayItemViewModel.GetStyleName(it),
				Selected = (model.Style == it),
				Parent = model
			}));
			ItemStylePopup.PlacementTarget = frameworkElement;
			ItemStylePopup.IsOpen = true;
			FilterPopup.LostFocus -= FilterPopup_OnLostFocus;
		}
	}

	private void ClearDraggingOver()
	{
		((DisplayItems.ItemsSource as ObservableCollection<SummaryDisplayItemViewModel>) ?? new ObservableCollection<SummaryDisplayItemViewModel>()).ToList().ForEach(delegate(SummaryDisplayItemViewModel it)
		{
			it.IsDraggingOver = false;
		});
	}

	private void OnDragOver(object sender, DragEventArgs e)
	{
		if ((sender as FrameworkElement)?.DataContext is SummaryDisplayItemViewModel summaryDisplayItemViewModel && DisplayItems.Items.IndexOf(summaryDisplayItemViewModel) < DisplayItems.Items.Count - 1)
		{
			ClearDraggingOver();
			summaryDisplayItemViewModel.IsDraggingOver = true;
		}
	}

	private void OnDrop(object sender, DragEventArgs e)
	{
		SummaryDisplayItemViewModel item = e.Data.GetData(typeof(SummaryDisplayItemViewModel)) as SummaryDisplayItemViewModel;
		SummaryDisplayItemViewModel item2 = (sender as FrameworkElement)?.DataContext as SummaryDisplayItemViewModel;
		int num = DisplayItems.Items.IndexOf(item);
		int num2 = DisplayItems.Items.IndexOf(item2);
		ResetDragStatus();
		if (num == num2 || num2 == DisplayItems.Items.Count - 1)
		{
			return;
		}
		ObservableCollection<SummaryDisplayItemViewModel> observableCollection = DisplayItems.ItemsSource as ObservableCollection<SummaryDisplayItemViewModel>;
		if (num < num2)
		{
			observableCollection.RemoveAt(num);
			observableCollection.Insert(num2, item);
		}
		else
		{
			observableCollection.RemoveAt(num);
			observableCollection.Insert(num2 + 1, item);
		}
		foreach (SummaryDisplayItemViewModel item3 in observableCollection)
		{
			item3.SortOrder = observableCollection.IndexOf(item3);
		}
		_model.DisplayItems = observableCollection;
		_model.SaveTemplate();
		this.FilterChanged?.Invoke(null, null);
	}

	private void ResetDragStatus()
	{
		((DisplayItems.ItemsSource as ObservableCollection<SummaryDisplayItemViewModel>) ?? new ObservableCollection<SummaryDisplayItemViewModel>()).ToList().ForEach(delegate(SummaryDisplayItemViewModel it)
		{
			it.IsDragging = false;
			it.IsDraggingOver = false;
		});
	}

	private void FilterPopup_OnLostFocus(object sender, RoutedEventArgs e)
	{
		FilterPopup.IsOpen = false;
		((DisplayItems.ItemsSource as ObservableCollection<SummaryDisplayItemViewModel>) ?? new ObservableCollection<SummaryDisplayItemViewModel>()).ToList().ForEach(delegate(SummaryDisplayItemViewModel it)
		{
			it.IsDragging = false;
			it.IsDraggingOver = false;
		});
		TryShowProDialog();
	}

	private void OnStyleClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: SummaryDisplayStyleItemModel dataContext })
		{
			dataContext.Selected = true;
			dataContext.Parent.Style = dataContext.Key;
			dataContext.Parent.Enabled = true;
			ItemStylePopup.IsOpen = false;
			_model.SaveTemplate();
			this.FilterChanged?.Invoke(null, null);
		}
		e.Handled = true;
	}

	private void OnDragLeave(object sender, DragEventArgs e)
	{
		ClearDraggingOver();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/summary/summarydisplayfiltercontrol.xaml", UriKind.Relative);
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
			((Grid)target).MouseLeftButtonUp += ShowSortPopupClick;
			break;
		case 2:
			SortByProgressImage = (Path)target;
			break;
		case 3:
			SortByListImage = (Path)target;
			break;
		case 4:
			SortByCompletedDateImage = (Path)target;
			break;
		case 5:
			SortByTaskDateImage = (Path)target;
			break;
		case 6:
			SortByPriorityImage = (Path)target;
			break;
		case 7:
			SortByAssigneeImage = (Path)target;
			break;
		case 8:
			SortByTagImage = (Path)target;
			break;
		case 9:
			SortPopup = (EscPopup)target;
			break;
		case 10:
			SortByProgressButton = (OptionCheckBox)target;
			break;
		case 11:
			SortByListButton = (OptionCheckBox)target;
			break;
		case 12:
			SortByCompletedDateButton = (OptionCheckBox)target;
			break;
		case 13:
			SortByTaskDateButton = (OptionCheckBox)target;
			break;
		case 14:
			SortByTagButton = (OptionCheckBox)target;
			break;
		case 15:
			SortByPriorityButton = (OptionCheckBox)target;
			break;
		case 16:
			SortByAssigneeButton = (OptionCheckBox)target;
			break;
		case 17:
			((Grid)target).MouseLeftButtonUp += ShowFilterPopupClick;
			break;
		case 18:
			DisplayFilterImage = (Path)target;
			break;
		case 19:
			FilterPopup = (EscPopup)target;
			break;
		case 20:
			DisplayItems = (ItemsControl)target;
			break;
		case 22:
			ProText = (TextBlock)target;
			break;
		case 23:
			DragItemPopup = (EscPopup)target;
			break;
		case 24:
			DragContent = (Grid)target;
			break;
		case 25:
			ItemStylePopup = (EscPopup)target;
			break;
		case 26:
			ItemStyleItems = (ItemsControl)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IStyleConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 21)
		{
			((Border)target).MouseLeftButtonUp += OnDisplayItemClick;
			((Border)target).PreviewMouseLeftButtonDown += OnDisplayItemDown;
			((Border)target).MouseMove += OnDisplayItemMouseMove;
			((Border)target).Drop += OnDrop;
			((Border)target).DragOver += OnDragOver;
			((Border)target).DragLeave += OnDragLeave;
		}
	}
}
