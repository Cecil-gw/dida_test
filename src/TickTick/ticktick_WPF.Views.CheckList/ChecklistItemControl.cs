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
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.DragAndDrop;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.MainListView.TaskList;
using ticktick_WPF.Views.QuickAdd;
using ticktick_WPF.Views.Time;

namespace ticktick_WPF.Views.CheckList;

public class ChecklistItemControl : UserControl, IComponentConnector
{
	public delegate void DragDelegate(CheckItemViewModel model, MouseEventArgs arg);

	public delegate void SetTimeDelegate(string id, TimeData timeData);

	private bool _clickFlag;

	private ChecklistControl _parent;

	private bool _raiseSaveTitleEvent = true;

	private CheckItemViewModel _model;

	private bool _inSticky;

	private bool _spliting;

	internal ChecklistItemControl ItemControl;

	internal Grid Container;

	internal Grid DragGrid;

	internal Grid CheckGrid;

	internal TextBlock HintText;

	internal DetailTextBox TitleTextBox;

	internal Grid Time;

	internal Border ReminderButton;

	internal TextBlock TimeText;

	internal Image DeleteButton;

	internal Grid BottomLineGrid;

	internal Line BottomLine;

	internal Line HighlightBottomLine;

	internal Grid BlinkBackground;

	private bool _contentLoaded;

	public string Id => Model?.Id;

	public CheckItemViewModel Model => base.DataContext as CheckItemViewModel;

	public event EventHandler<ItemTextChange> Split;

	public event EventHandler<ItemTextChange> Merge;

	public event EventHandler<string> MoveUp;

	public event EventHandler<string> MoveDown;

	public event EventHandler<string> Check;

	public event EventHandler<string> UnCheck;

	public event EventHandler<ItemTextChange> TitleChanged;

	public event EventHandler<string> TagSelected;

	public event EventHandler<string> ToastString;

	public event EventHandler<QuickSetModel> QuickItemSelected;

	public event EventHandler<double> CaretVerticalOffsetChanged;

	public event EventHandler QuickAddTask;

	public event EventHandler<bool> PopOpened;

	public event EventHandler<bool> PopClosed;

	public ChecklistItemControl()
		: this(null, inSticky: false)
	{
	}

	public ChecklistItemControl(ChecklistControl parent, bool inSticky, bool isDark = false)
	{
		InitializeComponent();
		_parent = parent;
		base.Loaded += delegate
		{
			BindEvents();
		};
		base.Unloaded += delegate
		{
			UnbindEvents();
		};
		TitleTextBox.SetWordWrap(wrap: true);
		if (inSticky)
		{
			_inSticky = true;
			DragGrid.Visibility = Visibility.Hidden;
			BottomLineGrid.Visibility = Visibility.Collapsed;
			ThemeUtil.SetTheme(isDark ? "dark" : "light", this);
			TitleTextBox.SetBaseColor("StickyContentTextColor", "StickyContentTextColor", "StickyCompletedTextColor", "StickyTextColor20");
			TitleTextBox.SetLightTheme();
			TitleTextBox.Padding = new Thickness(6.0, 5.0, 0.0, 2.0);
			TitleTextBox.LineSpacing = 3.0;
			CheckGrid.Margin = new Thickness(3.0, 6.0, 0.0, 0.0);
			TitleTextBox.SetResourceReference(Control.FontSizeProperty, "StickyFont13");
			TimeText.SetResourceReference(Control.FontSizeProperty, "StickyFont12");
			TimeText.Margin = new Thickness(0.0, 7.0, 0.0, 0.0);
			CheckGrid.SetResourceReference(FrameworkElement.MarginProperty, "StickyCheckItemIconMargin");
			Container.SetResourceReference(FrameworkElement.MinHeightProperty, "StickyHeight30");
			ReminderButton.Width = 0.0;
			DeleteButton.Width = 0.0;
			DeleteButton.Margin = new Thickness(0.0);
			Time.Visibility = Visibility.Collapsed;
		}
	}

	private ChecklistControl GetParent()
	{
		return _parent ?? (_parent = FindParent(this));
	}

	private void BindEvents()
	{
		ChecklistControl parent = GetParent();
		if (parent != null)
		{
			parent.Register(this);
			TitleTextBox.MergeText += OnMergeText;
			TitleTextBox.SplitText += OnSplitItem;
			TitleTextBox.MoveUp += delegate
			{
				this.MoveUp?.Invoke(this, Id);
			};
			TitleTextBox.MoveDown += delegate
			{
				this.MoveDown?.Invoke(this, Id);
			};
			TitleTextBox.CaretVerticalOffsetChanged += OnItemCaretVOffsetChanged;
			TitleTextBox.EscKeyUp += OnEscKeyUp;
			InitShortCut();
		}
	}

	private void UnbindEvents()
	{
		ChecklistControl parent = GetParent();
		if (parent != null)
		{
			parent.Unregister(this);
			TitleTextBox.MergeText -= OnMergeText;
			TitleTextBox.SplitText -= OnSplitItem;
			TitleTextBox.CaretVerticalOffsetChanged -= OnItemCaretVOffsetChanged;
			TitleTextBox.EscKeyUp -= OnEscKeyUp;
			if (_model != null)
			{
				PropertyChangedEventManager.RemoveHandler(_model, OnItemTitleChanged, "Title");
				PropertyChangedEventManager.RemoveHandler(_model, OnSortOrderChanged, "SortOrder");
				PropertyChangedEventManager.RemoveHandler(_model, OnDateChanged, "DisplayStartDate");
				PropertyChangedEventManager.RemoveHandler(_model, OnStatusChanged, "Status");
				_model = null;
			}
			RemoveKeyBinding();
		}
	}

	private void OnEscKeyUp(object sender, EventArgs e)
	{
		Keyboard.ClearFocus();
		Window window = Window.GetWindow(this);
		if (window != null && window.IsActive)
		{
			FocusManager.SetFocusedElement(window, window);
			Keyboard.Focus(window);
		}
	}

	private void InitShortCut()
	{
		if (base.InputBindings.Count < 6)
		{
			return;
		}
		if (_inSticky)
		{
			KeyBinding keyBinding = base.InputBindings[5] as KeyBinding;
			base.InputBindings.Clear();
			if (keyBinding != null)
			{
				base.InputBindings.Add(keyBinding);
			}
			KeyBindingManager.TryAddKeyBinding("CompleteTask", base.InputBindings[0] as KeyBinding);
		}
		else
		{
			KeyBindingManager.TryAddKeyBinding("ClearDate", base.InputBindings[0] as KeyBinding);
			KeyBindingManager.TryAddKeyBinding("SetToday", base.InputBindings[1] as KeyBinding);
			KeyBindingManager.TryAddKeyBinding("SetTomorrow", base.InputBindings[2] as KeyBinding);
			KeyBindingManager.TryAddKeyBinding("SetNextWeek", base.InputBindings[3] as KeyBinding);
			KeyBindingManager.TryAddKeyBinding("SetDate", base.InputBindings[4] as KeyBinding);
			KeyBindingManager.TryAddKeyBinding("CompleteTask", base.InputBindings[5] as KeyBinding);
		}
	}

	private void RemoveKeyBinding()
	{
		if (base.InputBindings.Count >= 6)
		{
			if (_inSticky)
			{
				KeyBindingManager.RemoveKeyBinding("CompleteTask", base.InputBindings[0] as KeyBinding);
				return;
			}
			KeyBindingManager.RemoveKeyBinding("ClearDate", base.InputBindings[0] as KeyBinding);
			KeyBindingManager.RemoveKeyBinding("SetToday", base.InputBindings[1] as KeyBinding);
			KeyBindingManager.RemoveKeyBinding("SetTomorrow", base.InputBindings[2] as KeyBinding);
			KeyBindingManager.RemoveKeyBinding("SetNextWeek", base.InputBindings[3] as KeyBinding);
			KeyBindingManager.RemoveKeyBinding("SetDate", base.InputBindings[4] as KeyBinding);
			KeyBindingManager.RemoveKeyBinding("CompleteTask", base.InputBindings[5] as KeyBinding);
		}
	}

	private void OnDataBind(object sender, DependencyPropertyChangedEventArgs e)
	{
		CheckItemViewModel checkItemViewModel = e.NewValue as CheckItemViewModel;
		if (e.OldValue is CheckItemViewModel source)
		{
			PropertyChangedEventManager.RemoveHandler(source, OnItemTitleChanged, "Title");
			PropertyChangedEventManager.RemoveHandler(source, OnSortOrderChanged, "SortOrder");
			PropertyChangedEventManager.RemoveHandler(source, OnStatusChanged, "Status");
			PropertyChangedEventManager.RemoveHandler(source, OnDateChanged, "DisplayStartDate");
		}
		ChecklistControl parent = GetParent();
		if (parent != null && parent.InMainDetail)
		{
			TitleTextBox.SetupSearchRender(isTitle: false, inSearchDetail: true);
		}
		if (checkItemViewModel != null)
		{
			TitleTextBox.SetTextOffset(Model.Title, restoreOffset: true, clearUndo: true);
			_model = checkItemViewModel;
			PropertyChangedEventManager.AddHandler(checkItemViewModel, OnItemTitleChanged, "Title");
			PropertyChangedEventManager.AddHandler(checkItemViewModel, OnSortOrderChanged, "SortOrder");
			PropertyChangedEventManager.AddHandler(checkItemViewModel, OnStatusChanged, "Status");
			PropertyChangedEventManager.AddHandler(checkItemViewModel, OnDateChanged, "DisplayStartDate");
			SetDateTextColor();
			ReminderButton.Visibility = ((checkItemViewModel.DisplayStartDate.HasValue || !checkItemViewModel.Enable || !base.IsMouseOver) ? ((!checkItemViewModel.DisplayStartDate.HasValue) ? Visibility.Hidden : Visibility.Collapsed) : Visibility.Visible);
			DeleteButton.Visibility = ((!checkItemViewModel.Enable || !base.IsMouseOver) ? ((!checkItemViewModel.DisplayStartDate.HasValue) ? Visibility.Hidden : Visibility.Collapsed) : Visibility.Visible);
		}
		if (parent != null && parent.InQuickAddWindow)
		{
			DragGrid.Visibility = Visibility.Collapsed;
			BottomLineGrid.Margin = new Thickness(25.0, 0.0, 0.0, 0.0);
		}
	}

	private void OnSortOrderChanged(object sender, PropertyChangedEventArgs e)
	{
		GetParent()?.SortChecklistItems();
	}

	private void OnStatusChanged(object sender, PropertyChangedEventArgs e)
	{
		GetParent()?.SortChecklistItems();
		SetDateTextColor();
	}

	private void SetDateTextColor()
	{
		int num = _model?.Status ?? 0;
		DateTime? dateTime = _model?.DisplayStartDate;
		if (num != 0)
		{
			TimeText.SetResourceReference(Control.ForegroundProperty, "BaseColorOpacity20");
		}
		else if (dateTime.HasValue && dateTime.Value < DateTime.Today.Date)
		{
			TimeText.SetResourceReference(Control.ForegroundProperty, "OutDateColor");
		}
		else
		{
			TimeText.SetResourceReference(Control.ForegroundProperty, "DateColorPrimary");
		}
	}

	private void OnDateChanged(object sender, PropertyChangedEventArgs e)
	{
		SetDateTextColor();
	}

	private void OnItemTitleChanged(object sender, PropertyChangedEventArgs e)
	{
		if (!TitleTextBox.KeyboardFocused && TitleTextBox.Text != Model.Title)
		{
			TitleTextBox.SetTextOffset(Model.Title, restoreOffset: false, clearUndo: true);
		}
	}

	private void OnItemCaretVOffsetChanged(object sender, double e)
	{
		this.CaretVerticalOffsetChanged?.Invoke(this, e);
	}

	private void OnPopClosed(object sender, EventArgs e)
	{
		this.PopClosed?.Invoke(this, e: false);
	}

	private void OnPopOpened(object sender, EventArgs e)
	{
		this.PopOpened?.Invoke(this, e: false);
	}

	private void OnLinkPopClosed(object sender, EventArgs e)
	{
		this.PopClosed?.Invoke(this, e: true);
	}

	private void OnLinkPopOpened(object sender, EventArgs e)
	{
		this.PopOpened?.Invoke(this, e: true);
	}

	private async void OnHandleDown(object sender, MouseButtonEventArgs e)
	{
		ChecklistControl parent = GetParent();
		this.PopOpened?.Invoke(this, e: true);
		TaskDragHelpModel.DragHelp.IsDragging = true;
		await TickDragHelper.StartDragItem(_parent, Model, null, base.ActualWidth, e.GetPosition(this));
		TaskDragHelpModel.DragHelp.IsDragging = false;
		parent?.OnPopupClosed(b: true);
	}

	private async void OnSplitItem(object sender, int index)
	{
		if (_spliting)
		{
			return;
		}
		_spliting = true;
		try
		{
			if (_parent != null && _parent.InQuickAddWindow && Utils.IfCtrlPressed())
			{
				this.QuickAddTask?.Invoke(this, null);
				return;
			}
			if (Model.Enable)
			{
				Model.ShowAddHint = false;
				_raiseSaveTitleEvent = false;
				index = Math.Min(index, TitleTextBox.Text.Length);
				Keyboard.Focus(null);
				GetParent()?.SplitNewItem(new ItemTextChange
				{
					Id = Id,
					Text = TitleTextBox.Text.Substring(0, index),
					Extra = TitleTextBox.Text.Substring(index)
				});
			}
		}
		finally
		{
			await Task.Delay(100);
			_spliting = false;
		}
	}

	private void OnMergeText(object sender, string text)
	{
		_raiseSaveTitleEvent = false;
		this.Merge?.Invoke(this, new ItemTextChange
		{
			Id = Id,
			Text = text
		});
	}

	private async void SelectTimeClick(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is CheckItemViewModel checkItemViewModel)
		{
			if (!checkItemViewModel.IsAgendaOwner && Utils.IsDida())
			{
				this.ToastString?.Invoke(null, Utils.GetString("AttendeeSetDate"));
			}
			if (!checkItemViewModel.Enable)
			{
				GetParent()?.TryToastUnableText();
				return;
			}
		}
		await Task.Delay(10);
		ShowSelectTime();
	}

	private void ShowSelectTime()
	{
		if (ProChecker.CheckPro(ProType.ReminderForSubTasks) && Model != null)
		{
			ChecklistControl parent = GetParent();
			if (parent != null && !parent.IsNewAddTask)
			{
				Model.Save(checkCount: false);
			}
			TimeData timeData = new TimeData
			{
				IsAllDay = Model.IsAllDay,
				StartDate = Model.DisplayStartDate,
				TimeZone = new TimeZoneViewModel(Model.IsFloating, Model.TimeZoneName),
				IsDefault = (!Model.IsAllDay.HasValue && !Model.StartDate.HasValue)
			};
			ShowSetTimeDialog(timeData);
		}
	}

	public void ShowSetTimeDialog(TimeData timeData)
	{
		CheckItemViewModel model = Model;
		if (model == null)
		{
			return;
		}
		_ = model.Id;
		SetDateDialog dialog = SetDateDialog.GetDialog();
		dialog.ClearEventHandle();
		dialog.Save += async delegate(object o, TimeData data)
		{
			double repeatDiff = model.RepeatDiff;
			if (repeatDiff > 0.0)
			{
				data.StartDate = data.StartDate?.AddDays(0.0 - repeatDiff);
			}
			model.SourceViewModel.IsAllDay = data.IsAllDay;
			model.SourceViewModel.StartDate = data.StartDate;
			model.SourceViewModel.RemindTime = null;
			if (!(GetParent()?.IsNewAddTask ?? false))
			{
				await TaskService.SetCheckItemDate(model.TaskServerId, model.Id, new TimeDataModel
				{
					StartDate = model.StartDate,
					IsAllDay = model.IsAllDay
				});
				TaskModel taskModel = await TaskDao.GetThinTaskById(model.TaskServerId ?? "");
				if (taskModel != null && (!data.IsAllDay.HasValue || !data.IsAllDay.Value) && !Utils.IsEmptyDate(data.StartDate) && (taskModel.isAllDay ?? Utils.IsEmptyDate(taskModel.startDate)) && taskModel.timeZone != TimeZoneData.LocalTimeZoneModel?.TimeZoneName)
				{
					taskModel.timeZone = TimeZoneData.LocalTimeZoneModel?.TimeZoneName;
					await TaskService.UpdateTaskOnTimeChanged(taskModel);
				}
				GetParent()?.NotifyTaskChanged(CheckItemModifyType.SetTime);
			}
		};
		dialog.Clear += async delegate
		{
			model.SourceViewModel.IsAllDay = null;
			model.SourceViewModel.StartDate = null;
			model.SourceViewModel.RemindTime = null;
			if (!(GetParent()?.IsNewAddTask ?? false))
			{
				await TaskService.SetCheckItemDate(model.TaskServerId, model.Id, new TimeDataModel
				{
					StartDate = null,
					IsAllDay = null
				});
				GetParent()?.NotifyTaskChanged(CheckItemModifyType.SetTime);
			}
		};
		dialog.Hided += delegate
		{
			GetParent()?.OnDatePopupOpenChange(isOpen: false);
		};
		Point point = TranslatePoint(new Point(base.ActualWidth - 180.0, 30.0), BottomLineGrid);
		dialog.Show(timeData, new SetDateDialogArgs(calendarMode: false, isNote: false, itemMode: true, BottomLineGrid, point.X, point.Y, PlacementMode.Relative, showQuickDate: false));
		GetParent()?.OnDatePopupOpenChange(isOpen: true);
	}

	private static ChecklistControl FindParent(DependencyObject child)
	{
		if (child == null)
		{
			return null;
		}
		DependencyObject parent = VisualTreeHelper.GetParent(child);
		if (parent == null)
		{
			return null;
		}
		if (parent is ChecklistControl result)
		{
			return result;
		}
		return FindParent(parent);
	}

	public void Blink()
	{
		object obj = Application.Current?.FindResource("ColorPrimary");
		if (obj != null && obj is Color toValue)
		{
			toValue.A = 26;
			ColorAnimation colorAnimation = new ColorAnimation(toValue, new Duration(TimeSpan.FromSeconds(0.7)))
			{
				AutoReverse = true,
				BeginTime = TimeSpan.FromSeconds(0.1)
			};
			colorAnimation.Completed += delegate
			{
				BlinkBackground.Visibility = Visibility.Collapsed;
			};
			BlinkBackground.Visibility = Visibility.Visible;
			BlinkBackground.Background = new SolidColorBrush(Colors.Transparent);
			BlinkBackground.Background.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
		}
	}

	public void FocusTitle(int caretIndex = -1, bool scroll = false)
	{
		TitleTextBox.FocusText(caretIndex);
		if (scroll)
		{
			this.CaretVerticalOffsetChanged?.Invoke(this, 21.0);
		}
	}

	public async void FocusEnd()
	{
		TitleTextBox.FocusEnd();
	}

	private void DeleteClick(object sender, MouseButtonEventArgs e)
	{
		if (Model != null)
		{
			GetParent()?.RemoveItemById(Model.Id);
		}
	}

	private void OnGotFocus(object sender, RoutedEventArgs e)
	{
		HighlightBottomLine.Visibility = Visibility.Visible;
	}

	private void ItemChecked(object sender, RoutedEventArgs e)
	{
		ChangeItemStatus(isChecked: true);
	}

	private void ItemUnChecked(object sender, RoutedEventArgs e)
	{
		ChangeItemStatus(isChecked: false);
	}

	private void ChangeItemStatus(bool isChecked)
	{
		if (_clickFlag)
		{
			ToggleCompleted(isChecked);
			_clickFlag = false;
		}
	}

	public void ToggleTaskCompleted()
	{
		if (Model != null)
		{
			bool isChecked = Model.Status == 0;
			ToggleCompleted(isChecked);
		}
	}

	private void ToggleCompleted(bool isChecked)
	{
		if (isChecked)
		{
			this.Check?.Invoke(this, Id);
		}
		else
		{
			this.UnCheck?.Invoke(this, Id);
		}
	}

	private async void CheckBoxClick(object sender, MouseButtonEventArgs e)
	{
		_clickFlag = true;
	}

	private async void OnLostFocus(object sender, EventArgs e)
	{
		HighlightBottomLine.Visibility = Visibility.Collapsed;
		if (Model != null && Model.Title != TitleTextBox.Text)
		{
			Model.SourceViewModel.Title = TitleTextBox.Text;
			await TrySaveItem(Model);
		}
		TaskUtils.TryGetTaskLinkTitle(Model?.Id, Model?.Title, isCheckItem: true);
	}

	private async Task TrySaveItem(CheckItemViewModel model)
	{
		await SaveItemTitle(model);
		if ((_parent == null || (!_parent.InQuickAddWindow && !_parent.IsNewAddTask)) && model != null)
		{
			await SyncStatusDao.AddSyncStatus(model.TaskServerId, 0);
			SyncManager.TryDelaySync();
		}
	}

	private async Task SaveItemTitle(CheckItemViewModel model)
	{
		if (model != null)
		{
			await model.Save(checkCount: false);
		}
	}

	private void OnMouseEnter(object sender, MouseEventArgs e)
	{
		if (base.DataContext is CheckItemViewModel checkItemViewModel)
		{
			Time.Cursor = (checkItemViewModel.Enable ? Cursors.Hand : Cursors.No);
			TimeText.Cursor = (checkItemViewModel.Enable ? Cursors.Hand : Cursors.No);
			ReminderButton.Visibility = ((checkItemViewModel.DisplayStartDate.HasValue || !checkItemViewModel.Enable) ? ((!checkItemViewModel.DisplayStartDate.HasValue) ? Visibility.Hidden : Visibility.Collapsed) : Visibility.Visible);
			DeleteButton.Visibility = ((!checkItemViewModel.Enable) ? ((!checkItemViewModel.DisplayStartDate.HasValue) ? Visibility.Hidden : Visibility.Collapsed) : Visibility.Visible);
		}
	}

	private void OnMouseLeave(object sender, MouseEventArgs e)
	{
		HideOperationButton();
	}

	private void HideOperationButton()
	{
		if (base.DataContext is CheckItemViewModel checkItemViewModel)
		{
			ReminderButton.Visibility = ((!checkItemViewModel.DisplayStartDate.HasValue) ? Visibility.Hidden : Visibility.Collapsed);
			DeleteButton.Visibility = ((!checkItemViewModel.DisplayStartDate.HasValue) ? Visibility.Hidden : Visibility.Collapsed);
		}
	}

	private async void OnTitleChanged(object sender, EventArgs e)
	{
		string text = TitleTextBox?.Text;
		CheckItemViewModel model = Model;
		if (model != null)
		{
			if (text != null)
			{
				model.SourceViewModel.Title = text;
			}
			if (model.IsValid && !(GetParent()?.IsNewAddTask ?? false))
			{
				await TrySaveItem(model);
				this.TitleChanged?.Invoke(this, new ItemTextChange
				{
					Id = model.Id,
					Text = text
				});
			}
		}
	}

	public void SetDate(string key)
	{
		if (!ProChecker.CheckPro(ProType.ReminderForSubTasks))
		{
			return;
		}
		CheckItemViewModel model = Model;
		if (model != null && model.Enable)
		{
			double repeatDiff = model.RepeatDiff;
			switch (key)
			{
			case "today":
				model.SourceViewModel.StartDate = DateTime.Today.AddDays(0.0 - repeatDiff);
				model.SourceViewModel.IsAllDay = true;
				break;
			case "tomorrow":
				model.SourceViewModel.StartDate = DateTime.Today.AddDays(1.0).AddDays(0.0 - repeatDiff);
				model.SourceViewModel.IsAllDay = true;
				break;
			case "nextweek":
				model.SourceViewModel.StartDate = DateTime.Today.AddDays(7.0).AddDays(0.0 - repeatDiff);
				model.SourceViewModel.IsAllDay = true;
				break;
			}
			ReminderButton.Visibility = Visibility.Hidden;
			model.SourceViewModel.RemindTime = null;
			ChecklistControl parent = GetParent();
			if ((parent == null || !parent.IsNewAddTask) && model.StartDate.HasValue)
			{
				TaskService.SetCheckItemDate(model.TaskServerId, model.Id, new TimeDataModel
				{
					IsAllDay = true,
					StartDate = model.StartDate
				});
				SyncManager.TryDelaySync();
			}
		}
	}

	private void SelectDate(object sender, EventArgs e)
	{
		if (!_inSticky)
		{
			ShowSelectTime();
		}
	}

	public void SelectDate()
	{
		ShowSelectTime();
	}

	public async void ClearDate()
	{
		Model.SourceViewModel.StartDate = null;
		Model.SourceViewModel.RemindTime = null;
		ReminderButton.Visibility = ((Model.DisplayStartDate.HasValue || !Model.Enable) ? Visibility.Hidden : Visibility.Visible);
		await TaskService.SetCheckItemDate(Model.TaskServerId, Model.Id, new TimeDataModel
		{
			StartDate = null,
			IsAllDay = null
		});
		SyncManager.TryDelaySync();
	}

	private void CheckEditable(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext is CheckItemViewModel { IsAgendaOwner: false } && Utils.IsDida())
		{
			this.ToastString?.Invoke(null, Utils.GetString("AttendeeModifyContent"));
		}
	}

	private void CheckChckBoxEditable(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext is CheckItemViewModel checkItemViewModel)
		{
			if (!checkItemViewModel.IsAgendaOwner && Utils.IsDida())
			{
				this.ToastString?.Invoke(null, Utils.GetString("AttendeeCompleteSubtask"));
			}
			if (!checkItemViewModel.Enable)
			{
				GetParent()?.TryToastUnableText();
			}
		}
	}

	public void RemoveUndoCache()
	{
	}

	private void OnItemSelected(object sender, QuickSetModel e)
	{
		this.QuickItemSelected?.Invoke(sender, e);
	}

	private void EditorOnRequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
	{
		e.Handled = true;
	}

	public double GetSearchOffset()
	{
		double firstSearchIndex = TitleTextBox.GetFirstSearchIndex();
		if (firstSearchIndex >= 0.0)
		{
			this.CaretVerticalOffsetChanged?.Invoke(this, firstSearchIndex);
		}
		return firstSearchIndex;
	}

	public void SetStickyTheme(bool isDark)
	{
		ThemeUtil.SetTheme(isDark ? "dark" : "light", this);
		TitleTextBox.SetLightTheme();
	}

	public void UnbindEvent()
	{
		TitleTextBox.MergeText -= OnMergeText;
		TitleTextBox.SplitText -= OnSplitItem;
		TitleTextBox.CaretVerticalOffsetChanged -= OnItemCaretVOffsetChanged;
		if (_model != null)
		{
			PropertyChangedEventManager.RemoveHandler(_model, OnItemTitleChanged, "Title");
			PropertyChangedEventManager.RemoveHandler(_model, OnSortOrderChanged, "SortOrder");
			PropertyChangedEventManager.RemoveHandler(_model, OnDateChanged, "DisplayStartDate");
			PropertyChangedEventManager.RemoveHandler(_model, OnStatusChanged, "Status");
			_model = null;
		}
		RemoveKeyBinding();
	}

	public async void FocusOnLoaded()
	{
		int times = 300;
		while (!base.IsLoaded && times > 0)
		{
			times--;
			await Task.Delay(1);
		}
		FocusTitle(-1, scroll: true);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/checklist/checklistitemcontrol.xaml", UriKind.Relative);
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
			ItemControl = (ChecklistItemControl)target;
			ItemControl.DataContextChanged += OnDataBind;
			ItemControl.MouseEnter += OnMouseEnter;
			ItemControl.MouseLeave += OnMouseLeave;
			break;
		case 2:
			Container = (Grid)target;
			break;
		case 3:
			DragGrid = (Grid)target;
			break;
		case 4:
			((Grid)target).MouseLeftButtonDown += OnHandleDown;
			break;
		case 5:
			CheckGrid = (Grid)target;
			break;
		case 6:
			((Grid)target).MouseLeftButtonUp += CheckChckBoxEditable;
			break;
		case 7:
			((CheckBox)target).PreviewMouseLeftButtonUp += CheckBoxClick;
			((CheckBox)target).Checked += ItemChecked;
			((CheckBox)target).Unchecked += ItemUnChecked;
			break;
		case 8:
			HintText = (TextBlock)target;
			break;
		case 9:
			TitleTextBox = (DetailTextBox)target;
			break;
		case 10:
			Time = (Grid)target;
			Time.PreviewMouseLeftButtonUp += SelectTimeClick;
			break;
		case 11:
			ReminderButton = (Border)target;
			ReminderButton.PreviewMouseLeftButtonUp += SelectTimeClick;
			break;
		case 12:
			TimeText = (TextBlock)target;
			TimeText.PreviewMouseLeftButtonUp += SelectTimeClick;
			break;
		case 13:
			DeleteButton = (Image)target;
			DeleteButton.MouseLeftButtonUp += DeleteClick;
			break;
		case 14:
			BottomLineGrid = (Grid)target;
			break;
		case 15:
			BottomLine = (Line)target;
			break;
		case 16:
			HighlightBottomLine = (Line)target;
			break;
		case 17:
			BlinkBackground = (Grid)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
