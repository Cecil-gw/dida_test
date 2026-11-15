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
using System.Windows.Shapes;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.Calendar.Agenda;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.Kanban.Item;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Pomo;
using ticktick_WPF.Views.Project;
using ticktick_WPF.Views.Tag;
using ticktick_WPF.Views.Time;

namespace ticktick_WPF.Views;

public class TaskOperationDialog : UserControl, ITabControl, IComponentConnector
{
	public static readonly DependencyProperty SelectedIndexProperty = DependencyProperty.Register("SelectedIndex", typeof(int), typeof(TaskOperationDialog), new PropertyMetadata(-1, null));

	public static readonly DependencyProperty SelectedDateProperty = DependencyProperty.Register("SelectedDate", typeof(string), typeof(TaskOperationDialog), new PropertyMetadata(string.Empty, null));

	public static readonly DependencyProperty SelectedPriorityProperty = DependencyProperty.Register("SelectedPriority", typeof(int), typeof(TaskOperationDialog), new PropertyMetadata(0, null));

	private const string Today = "today";

	private const string Tomorrow = "tomorrow";

	private const string NextWeek = "nextweek";

	private const string Custom = "custom";

	private const string Clear = "clear";

	private const string Delay = "delay";

	private readonly Popup _popup;

	private SelectDateDialog _dueDateDialog;

	private DateTime? _completeTime;

	private static Popup _lastOne;

	private bool _pomoPopupShow;

	private bool _changeDatePopupShow;

	private SetAssigneeDialog _assignDialog;

	private bool _assignPopupShow;

	private bool _setProjectPopupShow;

	private bool _tagPopupShow;

	private ProjectOrGroupPopup _projectOrGroupPopup;

	private BatchSetTagControl _batchSetTagCtrl;

	private readonly OperationExtra _extra;

	private Point _position;

	private readonly PopupLocationInfo _projectPopupTracker = new PopupLocationInfo();

	private readonly PopupLocationInfo _tagPopupTracker = new PopupLocationInfo();

	private readonly PopupLocationInfo _pomoPopupTracker = new PopupLocationInfo();

	private readonly PopupLocationInfo _assignPopupTracker = new PopupLocationInfo();

	private readonly List<PopupLocationInfo> _popupTrackers = new List<PopupLocationInfo>();

	private UIElement _uiElement;

	internal TaskOperationDialog Root;

	internal StackPanel Container;

	internal StackPanel DateStack;

	internal Grid DatePanel;

	internal ColumnDefinition ThirdColumn;

	internal ColumnDefinition FourthColumn;

	internal ColumnDefinition SixthColumn;

	internal ColumnDefinition SeventhColumn;

	internal Border TodayGrid;

	internal Border TomorrowGrid;

	internal Border NextWeekGrid;

	internal Border SkipGrid;

	internal Border SetDateBorder;

	internal Border DelayOrAdvance;

	internal Border ClearDateBorder;

	internal Grid PriorityGrid;

	internal Border PriorityHighGrid;

	internal Border PriorityMediumGrid;

	internal Border PriorityLowGrid;

	internal Border PriorityNoGrid;

	internal Line PriorityLine;

	internal OptionCheckBox CreateSubTaskButton;

	internal OptionCheckBox LinkParentTaskButton;

	internal OptionCheckBox PinButton;

	internal OptionCheckBox AbandonedButton;

	internal OptionCheckBox BatchAbandonButton;

	internal OptionCheckBox ReopenButton;

	internal ContentControl ProjectPlacement;

	internal Grid MoveButton;

	internal OptionCheckBox ProjectItem;

	internal EscPopup SetProjectPopup;

	internal Grid AssignBtn;

	internal OptionCheckBox AssignItem;

	internal EscPopup SetAssigneePopup;

	internal ContentControl TagPlacement;

	internal Grid TagGrid;

	internal OptionCheckBox TagItem;

	internal EscPopup SetTagPopup;

	internal OptionCheckBox BatchLinkParentTaskButton;

	internal Line Line1;

	internal StackPanel PomoPanel;

	internal Grid PomoGrid;

	internal OptionCheckBox PomoItem;

	internal EscPopup TaskPomoPopup;

	internal TaskPomoSetDialog TaskPomoSetDialog;

	internal OptionCheckBox CopyButton;

	internal OptionCheckBox CopyLinkGrid;

	internal OptionCheckBox CopyTextGrid;

	internal OptionCheckBox MergeBtn;

	internal Grid ChangeCompleteDate;

	internal OptionCheckBox CompleteItem;

	internal EscPopup ChangeDatePopup;

	internal Line DivideLine;

	internal OptionCheckBox SwitchButton;

	internal CommonDeleteIcon DeleteButton;

	private bool _contentLoaded;

	public int SelectedIndex
	{
		get
		{
			return (int)GetValue(SelectedIndexProperty);
		}
		set
		{
			SetValue(SelectedIndexProperty, value);
			SetItemTabSelected();
		}
	}

	public string SelectedDate
	{
		get
		{
			return (string)GetValue(SelectedDateProperty);
		}
		set
		{
			SetValue(SelectedDateProperty, value);
		}
	}

	public int SelectedPriority
	{
		get
		{
			return (int)GetValue(SelectedPriorityProperty);
		}
		set
		{
			SetValue(SelectedPriorityProperty, value);
		}
	}

	public event EventHandler CreateSubTask;

	public event EventHandler Copied;

	public event EventHandler LinkCopied;

	public event EventHandler Deleted;

	public event EventHandler AbandonOrReopen;

	public event EventHandler BatchAbandoned;

	public event EventHandler TextCopied;

	public event EventHandler<int> PrioritySelect;

	public event EventHandler<AvatarInfo> AssigneeSelect;

	public event EventHandler<SelectableItemViewModel> ProjectSelect;

	public event EventHandler<TimeData> TimeSelect;

	public event EventHandler<DateTime> QuickDateSelect;

	public event EventHandler<TagSelectData> TagsSelect;

	public event EventHandler CustomDateSelect;

	public event EventHandler TimeClear;

	public event EventHandler SkipCurrentRecurrence;

	public event EventHandler Merge;

	public event EventHandler StartPomo;

	public event EventHandler Closed;

	public event EventHandler<string> Toast;

	public event EventHandler<DateTime> CompleteDateChanged;

	public event EventHandler<bool> Disappear;

	public event EventHandler SwitchTaskOrNote;

	public event EventHandler OpenSticky;

	public event EventHandler AIComplete;

	public event EventHandler<long> DelayOrAdvanced;

	public event EventHandler<bool> Starred;

	public TaskOperationDialog(OperationExtra extra, UIElement element = null, EscPopup popup = null)
	{
		_popup = popup ?? new EscPopup
		{
			StaysOpen = false,
			Placement = PlacementMode.Mouse,
			VerticalOffset = 0.0,
			HorizontalOffset = 5.0,
			PlacementTarget = element
		};
		if (_lastOne != null)
		{
			_lastOne.IsOpen = false;
		}
		_lastOne = _popup;
		_extra = extra;
		_uiElement = element;
		TimeData timeModel = extra.TimeModel;
		if (timeModel != null && timeModel.StartDate.HasValue)
		{
			extra.TimeModel.IsDefault = false;
		}
		InitializeComponent();
		if (!extra.ShowCopy)
		{
			CopyButton.Visibility = Visibility.Collapsed;
		}
		if (!extra.ShowCopyLink)
		{
			CopyLinkGrid.Visibility = Visibility.Collapsed;
		}
		if (!extra.ShowCreateSubTask || extra.TaskType != TaskType.Task)
		{
			CreateSubTaskButton.Visibility = Visibility.Collapsed;
		}
		if (extra.TaskType != TaskType.Task)
		{
			PriorityGrid.Visibility = Visibility.Collapsed;
		}
		if (extra.TaskType == TaskType.Note)
		{
			SwitchButton.Text = Utils.GetString("ConvertToTask");
			SwitchButton.SetResourceReference(OptionCheckBox.IconProperty, "Ic.TaskOM.ConvertToTask");
		}
		if (!extra.CanSwitch)
		{
			SwitchButton.Visibility = ((!extra.ShowSwitch) ? Visibility.Collapsed : Visibility.Visible);
		}
		if (!extra.ShowMerge || extra.TaskType != TaskType.Task)
		{
			MergeBtn.Visibility = Visibility.Collapsed;
		}
		if (!extra.ShowPomo || !LocalSettings.Settings.EnableFocus)
		{
			PomoPanel.Visibility = Visibility.Collapsed;
		}
		if (!extra.ShowDate)
		{
			DateStack.Visibility = Visibility.Collapsed;
		}
		if (extra.UsedInBatchControl)
		{
			PriorityLine.Visibility = Visibility.Collapsed;
			DateStack.Visibility = Visibility.Collapsed;
			PriorityGrid.Visibility = Visibility.Collapsed;
			MoveButton.Visibility = Visibility.Collapsed;
			TagGrid.Visibility = Visibility.Collapsed;
			DeleteButton.Visibility = Visibility.Collapsed;
			DivideLine.Visibility = Visibility.Collapsed;
			Line1.Visibility = Visibility.Collapsed;
		}
		if (extra.BatchTaskIds != null)
		{
			BatchLinkParentTaskButton.Visibility = Visibility.Visible;
			LinkParentTaskButton.Visibility = Visibility.Collapsed;
		}
		else
		{
			BatchLinkParentTaskButton.Visibility = Visibility.Collapsed;
			LinkParentTaskButton.Visibility = Visibility.Visible;
		}
		if (!extra.ShowLinkParent)
		{
			LinkParentTaskButton.Visibility = Visibility.Collapsed;
			BatchLinkParentTaskButton.Visibility = Visibility.Collapsed;
		}
		NextWeekGrid.Visibility = (extra.ShowSkip ? Visibility.Collapsed : Visibility.Visible);
		ThirdColumn.Width = ((!extra.ShowSkip) ? new GridLength(1.0, GridUnitType.Star) : new GridLength(0.0));
		SkipGrid.Visibility = ((!extra.ShowSkip) ? Visibility.Collapsed : Visibility.Visible);
		FourthColumn.Width = (extra.ShowSkip ? new GridLength(1.0, GridUnitType.Star) : new GridLength(0.0));
		if (extra.InBatch && extra.CanPostpone)
		{
			SixthColumn.Width = new GridLength(1.0, GridUnitType.Star);
			DelayOrAdvance.Visibility = Visibility.Visible;
		}
		else
		{
			SixthColumn.Width = new GridLength(0.0);
			DelayOrAdvance.Visibility = Visibility.Collapsed;
		}
		if (!Utils.IsEmptyDate(extra.TimeModel.BatchData?.StartDate) || !Utils.IsEmptyDate(extra.TimeModel.StartDate))
		{
			SeventhColumn.Width = new GridLength(1.0, GridUnitType.Star);
			ClearDateBorder.Visibility = Visibility.Visible;
		}
		else
		{
			SeventhColumn.Width = new GridLength(0.0);
			ClearDateBorder.Visibility = Visibility.Collapsed;
		}
		if (extra.ShowAssignTo)
		{
			AssignBtn.Visibility = Visibility.Visible;
		}
		if (extra.CompleteTime.HasValue && extra.TaskType == TaskType.Task)
		{
			_completeTime = extra.CompleteTime;
			ChangeCompleteDate.Visibility = Visibility.Visible;
		}
		if (!extra.ShowCopy && !extra.CompleteTime.HasValue && (!extra.ShowMerge || extra.TaskType != TaskType.Task))
		{
			DivideLine.Visibility = Visibility.Collapsed;
		}
		if (extra.IsPinned.HasValue)
		{
			PinButton.Visibility = Visibility.Visible;
			PinButton.Text = Utils.GetString(extra.IsPinned.Value ? "Unpin" : "Pin");
			PinButton.Tag = !extra.IsPinned.Value;
			PinButton.SetResourceReference(OptionCheckBox.IconProperty, extra.IsPinned.Value ? "Ic.BatchEdit.CancelTopmost" : "Ic.BatchEdit.Topmost");
		}
		bool? isAbandoned = extra.IsAbandoned;
		if (isAbandoned.HasValue)
		{
			bool valueOrDefault = isAbandoned == true;
			AbandonedButton.Visibility = (valueOrDefault ? Visibility.Collapsed : Visibility.Visible);
			ReopenButton.Visibility = ((!valueOrDefault) ? Visibility.Collapsed : Visibility.Visible);
		}
		if (extra.InBatch)
		{
			ReopenButton.Visibility = Visibility.Collapsed;
			bool flag = extra.TaskType != TaskType.Note;
			bool flag2 = extra.HasCompletedTask || extra.HasAbandonedTask;
			AbandonedButton.Visibility = ((!flag || !(extra.HasUncompletedTask || flag2)) ? Visibility.Collapsed : Visibility.Visible);
			if (extra.HasUncompletedTask)
			{
				AbandonedButton.SetResourceReference(OptionCheckBox.IconProperty, "Ic.ProjectList.Completed");
				AbandonedButton.Text = Utils.GetString("Done");
			}
			else
			{
				AbandonedButton.SetResourceReference(OptionCheckBox.IconProperty, "Ic.TaskDetail.Uncompleted");
				AbandonedButton.Text = Utils.GetString((extra.HasAbandonedTask && !extra.HasCompletedTask) ? "TaskReopen" : "TaskUndone");
			}
			BatchAbandonButton.Visibility = ((!flag || !extra.HasUncompletedTask) ? Visibility.Collapsed : Visibility.Visible);
		}
		CopyTextGrid.Visibility = ((!extra.ShowCopyText) ? Visibility.Collapsed : Visibility.Visible);
		_popup.Closed += OnPopupClosed;
		EventHandler<bool> value = async delegate
		{
			await Task.Delay(250);
			this.CreateSubTask = null;
			this.Copied = null;
			this.LinkCopied = null;
			this.Deleted = null;
			this.AbandonOrReopen = null;
			this.BatchAbandoned = null;
			this.PrioritySelect = null;
			this.AssigneeSelect = null;
			this.ProjectSelect = null;
			this.TimeSelect = null;
			this.QuickDateSelect = null;
			this.TagsSelect = null;
			this.CustomDateSelect = null;
			this.TimeClear = null;
			this.SkipCurrentRecurrence = null;
			this.Merge = null;
			this.StartPomo = null;
			this.CompleteDateChanged = null;
			this.SwitchTaskOrNote = null;
			this.Disappear = null;
			this.Starred = null;
			this.Toast = null;
			this.OpenSticky = null;
			this.DelayOrAdvanced = null;
			_popup.PlacementTarget = null;
			await Task.Delay(250);
			this.Closed = null;
		};
		Disappear += value;
		_popupTrackers.Add(_projectPopupTracker);
		_popupTrackers.Add(_pomoPopupTracker);
		_popupTrackers.Add(_tagPopupTracker);
		_popupTrackers.Add(_assignPopupTracker);
	}

	public TaskOperationDialog()
	{
		InitializeComponent();
	}

	private void OnPopupClosed(object sender, EventArgs e)
	{
		SetDateDialog.GetDialog()?.TryClose();
		this.Closed?.Invoke(this, e);
		this.Disappear?.Invoke(this, e: true);
		PopupStateManager.OnViewPopupClosed();
	}

	public void Show()
	{
		_popup.Child = this;
		_popup.IsOpen = true;
		PopupStateManager.OnViewPopupOpened();
	}

	public void Dismiss()
	{
		_popup.IsOpen = false;
	}

	private void OnInitialized(object sender, EventArgs e)
	{
		InitPriority();
		InitDate();
	}

	private void InitDate()
	{
		if (_extra.TimeModel == null)
		{
			_extra.TimeModel = new TimeData();
		}
		if (_extra.TimeModel.HasTime && _extra.TimeModel.StartDate.HasValue)
		{
			DateTime value = _extra.TimeModel.StartDate.Value;
			if (value.Date == DateTime.Now.Date)
			{
				SelectedDate = "today";
			}
			else if (value.Date == DateTime.Now.Date.AddDays(1.0))
			{
				SelectedDate = "tomorrow";
			}
			else if (value.Date == DateTime.Now.Date.AddDays(7.0))
			{
				SelectedDate = "nextweek";
			}
		}
	}

	private static void SetGridHighlight(Border grid)
	{
		grid.SetResourceReference(Control.BackgroundProperty, "BaseColorOpacity5");
	}

	private void InitPriority()
	{
		SelectedPriority = _extra.Priority;
	}

	private void OnCopyClick(object sender, MouseButtonEventArgs e)
	{
		CloseParentPopup();
		this.Copied?.Invoke(this, null);
		UserActCollectUtils.AddClickEvent("tasklist", _extra.InBatch ? "cm_batch_task" : ((_extra.TaskType == TaskType.Task) ? "cm_single_task" : "cm_single_note"), "duplicate");
	}

	private void OnCopyLinkClick(object sender, MouseButtonEventArgs e)
	{
		CloseParentPopup();
		this.LinkCopied?.Invoke(this, null);
		UserActCollectUtils.AddClickEvent("tasklist", _extra.InBatch ? "cm_batch_task" : "cm_single_task", "copy_link");
	}

	private void OnDeleteClick(object sender, MouseButtonEventArgs e)
	{
		CloseParentPopup();
		this.Deleted?.Invoke(this, null);
		UserActCollectUtils.AddClickEvent("tasklist", _extra.InBatch ? "cm_batch_task" : ((_extra.TaskType == TaskType.Task) ? "cm_single_task" : "cm_single_note"), "delete");
	}

	private void PriorityGridClick(object sender, MouseButtonEventArgs e)
	{
		CloseParentPopup();
		if (sender is Border border)
		{
			int priority = int.Parse(border.Tag.ToString());
			OnPriorityClick(priority);
		}
	}

	private void OnPriorityClick(int priority)
	{
		this.PrioritySelect?.Invoke(this, priority);
		string data = null;
		switch (priority)
		{
		case 0:
			data = "priority_none";
			break;
		case 1:
			data = "priority_low";
			break;
		case 3:
			data = "priority_medium";
			break;
		case 5:
			data = "priority_high";
			break;
		}
		UserActCollectUtils.AddClickEvent("tasklist", _extra.InBatch ? "cm_batch_task" : "cm_single_task", data);
	}

	private async void OnProjectSelect(object sender, SelectableItemViewModel e)
	{
		UserActCollectUtils.AddClickEvent("tasklist", _extra.InBatch ? "cm_batch_task" : ((_extra.TaskType == TaskType.Task) ? "cm_single_task" : "cm_single_note"), "move_project");
		this.ProjectSelect?.Invoke(this, e);
		await Task.Delay(250);
		CloseParentPopup();
	}

	private async void OnAssigneeSelect(object sender, AvatarInfo assignee)
	{
		await Task.Delay(250);
		CloseParentPopup();
		this.AssigneeSelect?.Invoke(this, assignee);
	}

	private void SetTimeClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is Border border)
		{
			string cmd = border.Tag.ToString();
			OnTimeClick(cmd);
		}
	}

	private void OnTimeClick(string cmd)
	{
		if (!_extra.TimeModel.StartDate.HasValue && cmd != "custom")
		{
			_extra.TimeModel.StartDate = DateTime.Now.Date;
		}
		string ctype = (_extra.InBatch ? "cm_batch_task" : ((_extra.TaskType == TaskType.Task) ? "cm_single_task" : "cm_single_note"));
		switch (cmd)
		{
		case "today":
			UserActCollectUtils.AddClickEvent("tasklist", ctype, "date_today");
			OnStartDateSelect(DateTime.Today);
			break;
		case "tomorrow":
			UserActCollectUtils.AddClickEvent("tasklist", ctype, "date_tmr");
			OnStartDateSelect(DateTime.Today.AddDays(1.0));
			break;
		case "nextweek":
			UserActCollectUtils.AddClickEvent("tasklist", ctype, "date_n7d");
			OnStartDateSelect(DateTime.Today.AddDays(7.0));
			break;
		case "custom":
			UserActCollectUtils.AddClickEvent("tasklist", ctype, "date_other");
			SelectTimeOrReminder();
			break;
		case "clear":
			UserActCollectUtils.AddClickEvent("tasklist", ctype, "date_clear");
			CloseParentPopup();
			this.TimeClear?.Invoke(this, null);
			break;
		case "delay":
			ShowDelayPopup();
			break;
		}
	}

	private void ShowDelayPopup()
	{
		EscPopup popup = new EscPopup
		{
			PlacementTarget = DatePanel,
			Placement = PlacementMode.Right,
			StaysOpen = false,
			HorizontalOffset = 6.0,
			VerticalOffset = -8.0
		};
		DelayOrAdvanceDialog delayOrAdvanceDialog = new DelayOrAdvanceDialog();
		delayOrAdvanceDialog.DelayOrAdvanced += async delegate(object sender, long l)
		{
			if (!_extra.InCal)
			{
				TaskService.DelayOrAdvancedTasks(_extra.InBatch ? _extra.BatchTaskIds : new List<string> { _extra.TaskId }, l);
				Utils.Toast(Utils.GetString((l > 0) ? "TaskPostponed" : "TaskAdvanced"));
			}
			else
			{
				this.DelayOrAdvanced?.Invoke(this, l);
			}
			await Task.Delay(40);
			Dismiss();
		};
		delayOrAdvanceDialog.ShowDialog(popup, placeTop: false, _extra.CanPostponeTime);
	}

	private void OnStartDateSelect(DateTime date)
	{
		CloseParentPopup();
		this.QuickDateSelect?.Invoke(this, date);
	}

	private async void SelectTimeOrReminder()
	{
		bool isNote = _extra.TaskType == TaskType.Note || (_extra.TaskType == TaskType.TaskAndNote && _extra.InNoteProject);
		SetDateDialog dia = SetDateDialog.GetDialog();
		dia.ClearEventHandle();
		dia.Clear += delegate
		{
			dia.TryClose();
			this.TimeClear?.Invoke(this, null);
		};
		dia.Save += delegate(object sender, TimeData data)
		{
			dia.TryClose();
			this.TimeSelect?.Invoke(this, data);
		};
		dia.Show(_extra.TimeModel, new SetDateDialogArgs(calendarMode: false, isNote, itemMode: false, DateStack, 0.0, -10.0, PlacementMode.Right, showQuickDate: false));
		this.CustomDateSelect?.Invoke(this, null);
	}

	private void CloseParentPopup()
	{
		_popup.IsOpen = false;
	}

	private void OnMergeClick(object sender, MouseButtonEventArgs e)
	{
		CloseParentPopup();
		this.Merge?.Invoke(this, null);
		UserActCollectUtils.AddClickEvent("tasklist", _extra.InBatch ? "cm_batch_task" : "cm_single_task", "merge");
	}

	private void SkipRecurrenceClick(object sender, MouseButtonEventArgs e)
	{
		CloseParentPopup();
		UserActCollectUtils.AddClickEvent("tasklist", _extra.InBatch ? "cm_batch_task" : "cm_single_task", "date_skip");
		this.SkipCurrentRecurrence?.Invoke(this, null);
	}

	private async void ClosePomoPopup(object sender, bool e)
	{
		TaskPomoPopup.IsOpen = false;
		await Task.Delay(250);
		CloseParentPopup();
	}

	private void ShowPopup(object sender, MouseEventArgs e)
	{
		if (_popupTrackers.Any((PopupLocationInfo tracker) => tracker.IsSafeShowing()))
		{
			_popupTrackers.ForEach(delegate(PopupLocationInfo t)
			{
				t.Mark();
			});
			return;
		}
		Point position = e.GetPosition(this);
		if (Math.Abs(_position.Y - position.Y) > 4.0 || Math.Abs(_position.X - position.X) > 4.0)
		{
			_position = position;
			TryShowAssignPopup();
			TryShowSetProjectPopup();
			TryShowChangeDatePopup();
			TryShowPomoSetPopup();
			TryShowTagPopup();
		}
	}

	private void TryShowTagPopup()
	{
		if (SetTagPopup.IsOpen)
		{
			_tagPopupTracker.Mark();
		}
		if (!TagGrid.IsMouseOver && !SetTagPopup.IsMouseOver)
		{
			TryHideTagPopup();
		}
		else if (!_tagPopupShow && !SetTagPopup.IsOpen)
		{
			_tagPopupShow = true;
			DelayShowTagPopup();
		}
	}

	private async Task TryHideTagPopup()
	{
		_tagPopupShow = false;
		if (SetTagPopup.IsOpen)
		{
			await Task.Delay(100);
			bool flag = _tagPopupTracker.IsInSafeArea();
			if (!TagGrid.IsMouseOver && !SetTagPopup.IsMouseOver && !flag)
			{
				SetTagPopup.IsOpen = false;
			}
			else
			{
				_tagPopupShow = SetTagPopup.IsOpen;
			}
		}
	}

	private async Task DelayShowTagPopup(bool wait = true)
	{
		if (SetTagPopup.IsOpen)
		{
			_tagPopupTracker.Mark();
		}
		bool isFirst = _batchSetTagCtrl == null;
		if (isFirst)
		{
			_batchSetTagCtrl = new BatchSetTagControl();
			_batchSetTagCtrl.SetMaxHeight(220.0);
			_batchSetTagCtrl.Close += OnBatchSetTagClosed;
			_batchSetTagCtrl.TagsSelect += async delegate(object s, TagSelectData tags)
			{
				_extra.Tags = tags;
				this.TagsSelect?.Invoke(this, tags);
				UserActCollectUtils.AddClickEvent("tasklist", _extra.InBatch ? "cm_batch_task" : ((_extra.TaskType == TaskType.Task) ? "cm_single_task" : "cm_single_note"), "tag");
				SetTagPopup.IsOpen = false;
				await Task.Delay(250);
				CloseParentPopup();
			};
			SetTagPopup.Child = _batchSetTagCtrl;
		}
		if (wait)
		{
			await Task.Delay(150);
			if (TaskPomoPopup.IsOpen || SetAssigneePopup.IsOpen || SetProjectPopup.IsOpen || ChangeDatePopup.IsOpen)
			{
				_tagPopupShow = false;
				return;
			}
		}
		if (_tagPopupShow)
		{
			_batchSetTagCtrl.Init(_extra.Tags?.Clone() ?? new TagSelectData(), isFirst);
			SetTagPopup.IsOpen = true;
			_tagPopupTracker.Bind(SetTagPopup);
		}
	}

	private void OnBatchSetTagClosed(object sender, EventArgs e)
	{
		SetTagPopup.IsOpen = false;
	}

	private void TryShowSetProjectPopup()
	{
		if (SetProjectPopup.IsOpen)
		{
			_projectPopupTracker.Mark();
		}
		bool flag = _projectOrGroupPopup != null && _projectOrGroupPopup.ChildMouseOver();
		if (!MoveButton.IsMouseOver && !SetProjectPopup.IsMouseOver && !flag)
		{
			TryHideProjectPopup();
		}
		else if (!_setProjectPopupShow && !SetProjectPopup.IsOpen)
		{
			_setProjectPopupShow = true;
			DelayShowSetProjectPopup();
		}
	}

	private async Task TryHideProjectPopup()
	{
		_setProjectPopupShow = false;
		if (SetProjectPopup.IsOpen)
		{
			await Task.Delay(100);
			bool flag = _projectPopupTracker.IsInSafeArea();
			bool flag2 = _projectOrGroupPopup != null && _projectOrGroupPopup.ChildMouseOver();
			if (!MoveButton.IsMouseOver && !SetProjectPopup.IsMouseOver && !flag2 && !flag)
			{
				_projectOrGroupPopup?.ClosePopup();
				SetProjectPopup.IsOpen = false;
			}
			else
			{
				_setProjectPopupShow = SetProjectPopup.IsOpen;
			}
		}
	}

	private async void DelayShowSetProjectPopup()
	{
		await Task.Delay(150);
		if (TaskPomoPopup.IsOpen || SetAssigneePopup.IsOpen || SetTagPopup.IsOpen || ChangeDatePopup.IsOpen)
		{
			_setProjectPopupShow = false;
		}
		else if (_setProjectPopupShow)
		{
			ShowProjectSelector();
		}
	}

	private void ShowProjectSelector()
	{
		if (_projectOrGroupPopup == null)
		{
			_projectOrGroupPopup = new ProjectOrGroupPopup(SetProjectPopup, new ProjectExtra
			{
				ProjectIds = new List<string> { _extra.ProjectId }
			}, new ProjectSelectorExtra
			{
				showAll = false,
				batchMode = false,
				canSelectGroup = false,
				onlyShowPermission = true,
				CanSearch = true,
				ShowColumn = true,
				ColumnId = _extra.ColumnId
			});
			_projectOrGroupPopup.ItemSelect += OnProjectSelect;
		}
		_projectOrGroupPopup.Show();
		_projectOrGroupPopup.DelayFocus();
		_projectPopupTracker.Bind(_projectOrGroupPopup.GetProjectPopup());
	}

	private void TryShowAssignPopup()
	{
		if (!AssignBtn.IsMouseOver && !SetAssigneePopup.IsMouseOver)
		{
			TryHideAssignPopup();
		}
		else if (!_assignPopupShow && !SetAssigneePopup.IsOpen)
		{
			_assignPopupShow = true;
			DelayShowAssignPopup();
		}
	}

	private async Task TryHideAssignPopup()
	{
		_assignPopupShow = false;
		if (SetAssigneePopup.IsOpen)
		{
			await Task.Delay(100);
			if (!AssignBtn.IsMouseOver && !SetAssigneePopup.IsMouseOver)
			{
				SetAssigneePopup.IsOpen = false;
			}
			else
			{
				_assignPopupShow = SetAssigneePopup.IsOpen;
			}
		}
	}

	private async void DelayShowAssignPopup()
	{
		await Task.Delay(150);
		if (TaskPomoPopup.IsOpen || SetProjectPopup.IsOpen || SetTagPopup.IsOpen || ChangeDatePopup.IsOpen)
		{
			_assignPopupShow = false;
		}
		else if (_assignPopupShow)
		{
			ShowAssignPopup();
		}
	}

	private void ShowAssignPopup(bool onEnter = false)
	{
		if (_assignDialog == null)
		{
			_assignDialog = new SetAssigneeDialog(_extra.ProjectId, SetAssigneePopup, _extra.Assignee);
			_assignDialog.AssigneeSelect += OnAssigneeSelect;
			if (onEnter)
			{
				_assignDialog.UpDownSelect(isUp: true);
			}
		}
		_assignDialog.Show();
		_assignPopupTracker.Bind(_assignDialog.GetPopup());
	}

	private void TryShowPomoSetPopup()
	{
		if (TaskPomoPopup.IsOpen)
		{
			_pomoPopupTracker.Mark();
		}
		if (!PomoGrid.IsMouseOver && !TaskPomoPopup.IsMouseOver)
		{
			TryHidePomoPopup();
		}
		else if (!_pomoPopupShow && !TaskPomoPopup.IsOpen)
		{
			_pomoPopupShow = true;
			DelayShowPomoPopup();
		}
	}

	private async Task TryHidePomoPopup()
	{
		_pomoPopupShow = false;
		if (TaskPomoPopup.IsOpen)
		{
			await Task.Delay(100);
			bool flag = _pomoPopupTracker.IsInSafeArea();
			if (!PomoGrid.IsMouseOver && !TaskPomoSetDialog.CheckMouseMove() && !flag)
			{
				TaskPomoPopup.IsOpen = false;
			}
			else
			{
				_pomoPopupShow = TaskPomoPopup.IsOpen;
			}
		}
	}

	private void TryShowChangeDatePopup()
	{
		if (!ChangeCompleteDate.IsMouseOver && !ChangeDatePopup.IsMouseOver)
		{
			TryHideChangeDatePopup();
		}
		else if (!_changeDatePopupShow && !ChangeDatePopup.IsOpen)
		{
			_changeDatePopupShow = true;
			DelayShowChangeDatePopup();
		}
	}

	private async Task TryHideChangeDatePopup()
	{
		_changeDatePopupShow = false;
		if (ChangeDatePopup.IsOpen)
		{
			await Task.Delay(100);
			if (!ChangeCompleteDate.IsMouseOver && !ChangeDatePopup.IsMouseOver)
			{
				ChangeDatePopup.IsOpen = false;
			}
			else
			{
				_changeDatePopupShow = ChangeDatePopup.IsOpen;
			}
		}
		else
		{
			_changeDatePopupShow = false;
		}
	}

	private async void DelayShowChangeDatePopup()
	{
		await Task.Delay(150);
		if (TaskPomoPopup.IsOpen || SetProjectPopup.IsOpen || SetTagPopup.IsOpen || SetAssigneePopup.IsOpen)
		{
			_changeDatePopupShow = false;
		}
		else if (_changeDatePopupShow)
		{
			ShowChangeDatePopup();
		}
	}

	private void ShowChangeDatePopup(bool onEnter = false)
	{
		if (_dueDateDialog == null)
		{
			_dueDateDialog = new SelectDateDialog(ChangeDatePopup, new DurationModel
			{
				SelectedDate = _completeTime,
				SelectionStart = null,
				SelectionEnd = null
			}, isStart: false, DateTime.Today, null, isChooseDate: true);
			_dueDateDialog.SelectDate += SelectDueDate;
		}
		_dueDateDialog.Show();
		_dueDateDialog.SetCurrentTab(onEnter);
	}

	private async void DelayShowPomoPopup()
	{
		await Task.Delay(150);
		if (ChangeDatePopup.IsOpen || SetProjectPopup.IsOpen || SetTagPopup.IsOpen || SetAssigneePopup.IsOpen)
		{
			_pomoPopupShow = false;
		}
		else if (_pomoPopupShow)
		{
			ShowPomoPopup();
		}
	}

	private void ShowPomoPopup(bool onEnter = false)
	{
		TaskPomoPopup.IsOpen = true;
		TaskPomoSetDialog.InitData(_extra.TaskId, force: true, "tasklist", "cm_single_task", onEnter);
		TaskPomoSetDialog.Closed -= ClosePomoPopup;
		TaskPomoSetDialog.Closed += ClosePomoPopup;
		_pomoPopupTracker.Bind(TaskPomoPopup);
	}

	private void SelectDueDate(object sender, DateTime date)
	{
		CloseParentPopup();
		Window window = ((_uiElement == null) ? null : Window.GetWindow(_uiElement));
		if (window != null && !window.IsActive)
		{
			window.Activate();
		}
		this.CompleteDateChanged?.Invoke(null, date);
	}

	private void OnLinkParentTaskClick(object sender, MouseButtonEventArgs e)
	{
		UserActCollectUtils.AddClickEvent("link_parent_task", "link_from", _extra.InBatch ? "batch_om" : "list_om");
		UserActCollectUtils.AddClickEvent("tasklist", _extra.InBatch ? "cm_batch_task" : "cm_single_task", "link_parent_task");
		CloseParentPopup();
		List<string> list = ((_extra?.TaskId != null) ? new List<string> { _extra?.TaskId } : _extra?.BatchTaskIds);
		if (list != null)
		{
			string text = "list";
			if (_uiElement is TaskBar || _uiElement is TaskCell || (_uiElement is Border && !(_uiElement is KanbanItemView)) || _uiElement is CalendarAgendaItem || (_uiElement == null && _extra != null && !_extra.InBatch))
			{
				text = "calendar";
			}
			App.Window.StartLinkSearch(list.Distinct().ToList(), text);
		}
	}

	private void OnCreateSubTaskClick(object sender, MouseButtonEventArgs e)
	{
		CloseParentPopup();
		this.CreateSubTask?.Invoke(this, null);
		UserActCollectUtils.AddClickEvent("tasklist", "cm_single_task", "add_subtask");
	}

	private void OnSwitchClick(object sender, MouseButtonEventArgs e)
	{
		CloseParentPopup();
		if (!_extra.CanSwitch)
		{
			this.Toast?.Invoke(this, _extra.FailedSwitchTips);
			return;
		}
		this.SwitchTaskOrNote?.Invoke(null, null);
		if (_extra.InBatch)
		{
			UserActCollectUtils.AddClickEvent("tasklist", "cm_batch_task", "convert_to_note_or_task");
		}
		else
		{
			UserActCollectUtils.AddClickEvent("tasklist", (_extra.TaskType == TaskType.Task) ? "cm_single_task" : "cm_single_note", (_extra.TaskType == TaskType.Task) ? "convert_to_note" : "convert_to_task");
		}
	}

	public static void TryCloseLastOne()
	{
		if (_lastOne != null)
		{
			_lastOne.IsOpen = false;
		}
	}

	private void OnAICompleteClick(object sender, MouseButtonEventArgs e)
	{
		CloseParentPopup();
		this.AIComplete?.Invoke(this, null);
	}

	private void OnPinClick(object sender, MouseButtonEventArgs e)
	{
		CloseParentPopup();
		bool valueOrDefault = PinButton.Tag as bool? == true;
		UserActCollectUtils.AddClickEvent("tasklist", (_extra.TaskType == TaskType.Task) ? "cm_single_task" : "cm_single_note", valueOrDefault ? "pin" : "unpin");
		this.Starred?.Invoke(this, valueOrDefault);
	}

	private void OnAbandonOrReopenClick(object sender, MouseButtonEventArgs e)
	{
		CloseParentPopup();
		this.AbandonOrReopen?.Invoke(this, null);
	}

	private void OnBatchAbandonClick(object sender, MouseButtonEventArgs e)
	{
		CloseParentPopup();
		this.BatchAbandoned?.Invoke(this, null);
		UserActCollectUtils.AddClickEvent("tasklist", "cm_batch_task", "abandon");
	}

	private void OnCopyTextClick(object sender, MouseButtonEventArgs e)
	{
		CloseParentPopup();
		this.TextCopied?.Invoke(this, null);
	}

	private async void OnStickyClick(object sender, MouseButtonEventArgs e)
	{
		CloseParentPopup();
		UserActCollectUtils.AddClickEvent("tasklist", _extra.InBatch ? "cm_batch_task" : ((_extra.TaskType == TaskType.Task) ? "cm_single_task" : "cm_single_note"), "open_as_sticky_note");
		if (_extra.InBatch)
		{
			this.OpenSticky?.Invoke(this, null);
			return;
		}
		string text = TaskUtils.ToastOnOpenSticky(_extra?.TaskId);
		if (!string.IsNullOrEmpty(text))
		{
			this.Toast?.Invoke(this, text);
			return;
		}
		TaskStickyWindow.ShowTaskSticky(new List<string> { _extra?.TaskId });
	}

	public bool HandleTab(bool shift)
	{
		if (TaskPomoPopup.IsOpen)
		{
			TaskPomoSetDialog.HandleTab(shift);
			return true;
		}
		if (SetTagPopup.IsOpen)
		{
			_batchSetTagCtrl?.HandleTab(shift);
			return true;
		}
		if (ChangeDatePopup.IsOpen)
		{
			_dueDateDialog?.SetCurrentTab(inTab: true);
			return true;
		}
		if (SetProjectPopup.IsOpen)
		{
			_projectOrGroupPopup.UpDownSelect(shift);
			return false;
		}
		if (SetAssigneePopup.IsOpen)
		{
			_assignDialog.UpDownSelect(shift);
			return false;
		}
		if (SelectedIndex < 0)
		{
			SelectedIndex = 0;
			return true;
		}
		SelectedIndex += ((!shift) ? 1 : 28);
		SelectedIndex %= 29;
		if (SelectedIndex >= 0 && SelectedIndex <= 6 && !DatePanel.IsVisible)
		{
			SelectedIndex = (shift ? 28 : (PriorityGrid.IsVisible ? 7 : 11));
		}
		if (SelectedIndex >= 7 && SelectedIndex <= 10 && !PriorityGrid.IsVisible)
		{
			SelectedIndex = ((!shift) ? 11 : (DatePanel.IsVisible ? 6 : 28));
		}
		switch (SelectedIndex)
		{
		case 2:
			if (NextWeekGrid.IsVisible)
			{
				break;
			}
			goto IL_034a;
		case 3:
			if (SkipGrid.IsVisible)
			{
				break;
			}
			goto IL_034a;
		case 5:
			if (DelayOrAdvance.IsVisible)
			{
				break;
			}
			goto IL_034a;
		case 6:
			if (ClearDateBorder.IsVisible)
			{
				break;
			}
			goto IL_034a;
		case 11:
			if (CreateSubTaskButton.IsVisible)
			{
				break;
			}
			goto IL_034a;
		case 12:
			if (LinkParentTaskButton.IsVisible)
			{
				break;
			}
			goto IL_034a;
		case 13:
			if (PinButton.IsVisible)
			{
				break;
			}
			goto IL_034a;
		case 14:
			if (AbandonedButton.IsVisible)
			{
				break;
			}
			goto IL_034a;
		case 15:
			if (BatchAbandonButton.IsVisible)
			{
				break;
			}
			goto IL_034a;
		case 16:
			if (ReopenButton.IsVisible)
			{
				break;
			}
			goto IL_034a;
		case 17:
			if (MoveButton.IsVisible)
			{
				break;
			}
			goto IL_034a;
		case 18:
			if (AssignBtn.IsVisible)
			{
				break;
			}
			goto IL_034a;
		case 19:
			if (TagGrid.IsVisible)
			{
				break;
			}
			goto IL_034a;
		case 20:
			if (PomoPanel.IsVisible)
			{
				break;
			}
			goto IL_034a;
		case 21:
			if (CopyButton.IsVisible)
			{
				break;
			}
			goto IL_034a;
		case 22:
			if (CopyLinkGrid.IsVisible)
			{
				break;
			}
			goto IL_034a;
		case 23:
			if (CopyTextGrid.IsVisible)
			{
				break;
			}
			goto IL_034a;
		case 24:
			if (MergeBtn.IsVisible)
			{
				break;
			}
			goto IL_034a;
		case 25:
			if (ChangeCompleteDate.IsVisible)
			{
				break;
			}
			goto IL_034a;
		case 27:
			if (SwitchButton.IsVisible)
			{
				break;
			}
			goto IL_034a;
		case 28:
			{
				if (DeleteButton.IsVisible)
				{
					break;
				}
				goto IL_034a;
			}
			IL_034a:
			return HandleTab(shift);
		}
		return true;
	}

	public bool HandleEnter()
	{
		if (ChangeDatePopup.IsOpen)
		{
			_dueDateDialog.EnterSelect();
		}
		if (SetProjectPopup.IsOpen || SetAssigneePopup.IsOpen || SetTagPopup.IsOpen || TaskPomoPopup.IsOpen || ChangeDatePopup.IsOpen)
		{
			return false;
		}
		switch (SelectedIndex)
		{
		case 0:
			OnTimeClick("today");
			break;
		case 1:
			OnTimeClick("tomorrow");
			break;
		case 2:
			OnTimeClick("nextweek");
			break;
		case 3:
			SkipRecurrenceClick(null, null);
			break;
		case 4:
			OnTimeClick("custom");
			break;
		case 6:
			OnTimeClick("clear");
			break;
		case 7:
			OnPriorityClick(5);
			CloseParentPopup();
			break;
		case 8:
			OnPriorityClick(3);
			CloseParentPopup();
			break;
		case 9:
			OnPriorityClick(1);
			CloseParentPopup();
			break;
		case 10:
			OnPriorityClick(0);
			CloseParentPopup();
			break;
		case 11:
			OnCreateSubTaskClick(null, null);
			break;
		case 13:
			OnPinClick(null, null);
			break;
		case 12:
			OnLinkParentTaskClick(null, null);
			break;
		case 16:
			OnAbandonOrReopenClick(null, null);
			break;
		case 14:
			OnAbandonOrReopenClick(null, null);
			break;
		case 15:
			OnBatchAbandonClick(null, null);
			break;
		case 17:
			_setProjectPopupShow = true;
			ShowProjectSelector();
			break;
		case 18:
			_assignPopupShow = true;
			ShowAssignPopup(onEnter: true);
			break;
		case 19:
			_tagPopupShow = true;
			DelayShowTagPopup();
			break;
		case 20:
			_pomoPopupShow = true;
			ShowPomoPopup(onEnter: true);
			break;
		case 21:
			OnCopyClick(null, null);
			break;
		case 22:
			OnCopyLinkClick(null, null);
			break;
		case 23:
			OnCopyTextClick(null, null);
			break;
		case 24:
			OnMergeClick(null, null);
			break;
		case 25:
			_changeDatePopupShow = true;
			ShowChangeDatePopup(onEnter: true);
			break;
		case 26:
			OnStickyClick(null, null);
			break;
		case 27:
			OnSwitchClick(null, null);
			break;
		case 28:
			OnDeleteClick(null, null);
			break;
		}
		return true;
	}

	public bool HandleEsc()
	{
		return false;
	}

	public bool UpDownSelect(bool isUp)
	{
		if (ChangeDatePopup.IsOpen)
		{
			_dueDateDialog.HandleUpDown(isUp);
			return true;
		}
		if (SetProjectPopup.IsOpen)
		{
			_projectOrGroupPopup.UpDownSelect(isUp);
			return true;
		}
		if (SetProjectPopup.IsOpen || SetAssigneePopup.IsOpen || SetTagPopup.IsOpen || TaskPomoPopup.IsOpen || ChangeDatePopup.IsOpen)
		{
			return false;
		}
		if (SelectedIndex < 0)
		{
			if (DateStack.IsVisible)
			{
				SelectedIndex = 0;
			}
			else if (PriorityGrid.IsVisible)
			{
				SelectedIndex = 7;
			}
			else
			{
				SelectedIndex = 14;
			}
			return true;
		}
		if (SelectedIndex >= 0 && SelectedIndex <= 6)
		{
			SelectedIndex = (isUp ? (-1) : ((!PriorityGrid.IsVisible) ? 11 : ((SelectedIndex >= 3) ? 10 : (SelectedIndex + 7))));
		}
		else if (SelectedIndex >= 7 && SelectedIndex <= 10)
		{
			SelectedIndex = (isUp ? (SelectedIndex - 5) : 9);
			if (SelectedIndex == 2 && !NextWeekGrid.IsVisible)
			{
				SelectedIndex = 3;
			}
			if ((SelectedIndex == 3 && !SkipGrid.IsVisible) || (SelectedIndex == 5 && !DelayOrAdvance.IsVisible) || (SelectedIndex == 6 && !ClearDateBorder.IsVisible))
			{
				SelectedIndex = 4;
			}
		}
		else if (SelectedIndex == 11)
		{
			SelectedIndex = ((!isUp) ? 13 : (PriorityGrid.IsVisible ? 7 : ((!DateStack.IsVisible) ? 28 : 0)));
		}
		else
		{
			SelectedIndex += ((!isUp) ? 1 : (-1));
		}
		SelectedIndex += 29;
		SelectedIndex %= 29;
		if (SelectedIndex >= 0 && SelectedIndex <= 6 && !DatePanel.IsVisible)
		{
			UpDownSelect(isUp);
		}
		else if (SelectedIndex >= 7 && SelectedIndex <= 10 && !PriorityGrid.IsVisible)
		{
			UpDownSelect(isUp);
		}
		else
		{
			switch (SelectedIndex)
			{
			case 11:
				if (CreateSubTaskButton.IsVisible)
				{
					break;
				}
				goto IL_03d0;
			case 13:
				if (PinButton.IsVisible)
				{
					break;
				}
				goto IL_03d0;
			case 14:
				if (AbandonedButton.IsVisible)
				{
					break;
				}
				goto IL_03d0;
			case 15:
				if (BatchAbandonButton.IsVisible)
				{
					break;
				}
				goto IL_03d0;
			case 16:
				if (ReopenButton.IsVisible)
				{
					break;
				}
				goto IL_03d0;
			case 17:
				if (MoveButton.IsVisible)
				{
					break;
				}
				goto IL_03d0;
			case 18:
				if (AssignBtn.IsVisible)
				{
					break;
				}
				goto IL_03d0;
			case 19:
				if (TagGrid.IsVisible)
				{
					break;
				}
				goto IL_03d0;
			case 20:
				if (PomoPanel.IsVisible)
				{
					break;
				}
				goto IL_03d0;
			case 21:
				if (CopyButton.IsVisible)
				{
					break;
				}
				goto IL_03d0;
			case 22:
				if (CopyLinkGrid.IsVisible)
				{
					break;
				}
				goto IL_03d0;
			case 23:
				if (CopyTextGrid.IsVisible)
				{
					break;
				}
				goto IL_03d0;
			case 24:
				if (MergeBtn.IsVisible)
				{
					break;
				}
				goto IL_03d0;
			case 25:
				if (ChangeCompleteDate.IsVisible)
				{
					break;
				}
				goto IL_03d0;
			case 27:
				if (SwitchButton.IsVisible)
				{
					break;
				}
				goto IL_03d0;
			case 28:
				{
					if (DeleteButton.IsVisible)
					{
						break;
					}
					goto IL_03d0;
				}
				IL_03d0:
				UpDownSelect(isUp);
				break;
			}
		}
		return true;
	}

	public bool LeftRightSelect(bool isLeft)
	{
		if (ChangeDatePopup.IsOpen)
		{
			_dueDateDialog.HandleLeftRight(isLeft);
			return true;
		}
		if (SetProjectPopup.IsOpen)
		{
			return _projectOrGroupPopup.LeftRightSelect(isLeft);
		}
		if (!isLeft)
		{
			if (SetAssigneePopup.IsOpen || SetTagPopup.IsOpen || TaskPomoPopup.IsOpen)
			{
				return false;
			}
			int selectedIndex = SelectedIndex;
			if ((uint)(selectedIndex - 17) <= 3u || selectedIndex == 25)
			{
				HandleEnter();
				return false;
			}
		}
		else
		{
			if (TaskPomoPopup.IsOpen && !TaskPomoSetDialog.IsInputFocus())
			{
				TaskPomoPopup.IsOpen = false;
				return true;
			}
			if (SetAssigneePopup.IsOpen)
			{
				SetAssigneePopup.IsOpen = false;
				return true;
			}
		}
		if (SelectedIndex >= 0 && SelectedIndex <= 6)
		{
			SelectedIndex += ((!isLeft) ? 1 : (-1));
			if (SelectedIndex == 2 && !NextWeekGrid.IsVisible)
			{
				SelectedIndex += ((!isLeft) ? 1 : (-1));
			}
			if (SelectedIndex == 3 && !SkipGrid.IsVisible)
			{
				SelectedIndex += ((!isLeft) ? 1 : (-1));
			}
			if (SelectedIndex == 5 && !DelayOrAdvance.IsVisible)
			{
				SelectedIndex = ((!isLeft) ? 1 : 4);
			}
			if (SelectedIndex == 6 && !ClearDateBorder.IsVisible)
			{
				SelectedIndex = ((!isLeft) ? 1 : 4);
			}
			SelectedIndex += 7;
			SelectedIndex %= 7;
		}
		else if (SelectedIndex >= 7 && SelectedIndex <= 10)
		{
			SelectedIndex += ((!isLeft) ? 1 : (-1));
			if (SelectedIndex > 10)
			{
				SelectedIndex = 7;
			}
			if (SelectedIndex < 7)
			{
				SelectedIndex = 10;
			}
		}
		return false;
	}

	private void OnChildPopupOpened(object sender, EventArgs e)
	{
		if (object.Equals(sender, SetProjectPopup))
		{
			ProjectItem.HoverSelected = true;
		}
		else if (object.Equals(sender, SetAssigneePopup))
		{
			AssignItem.HoverSelected = true;
		}
		else if (object.Equals(sender, SetTagPopup))
		{
			TagItem.HoverSelected = true;
		}
		else if (object.Equals(sender, TaskPomoPopup))
		{
			PomoItem.HoverSelected = true;
		}
		else if (object.Equals(sender, ChangeDatePopup))
		{
			CompleteItem.HoverSelected = true;
		}
	}

	private void OnChildPopupClosed(object sender, EventArgs e)
	{
		if (object.Equals(sender, SetProjectPopup))
		{
			ProjectItem.HoverSelected = SelectedIndex == 17;
		}
		else if (object.Equals(sender, SetAssigneePopup))
		{
			AssignItem.HoverSelected = SelectedIndex == 18;
		}
		else if (object.Equals(sender, SetTagPopup))
		{
			TagItem.HoverSelected = SelectedIndex == 19;
		}
		else if (object.Equals(sender, TaskPomoPopup))
		{
			PomoItem.HoverSelected = SelectedIndex == 20;
		}
		else if (object.Equals(sender, ChangeDatePopup))
		{
			CompleteItem.HoverSelected = SelectedIndex == 25;
		}
	}

	private void SetItemTabSelected()
	{
		ProjectItem.HoverSelected = SelectedIndex == 17 || SetProjectPopup.IsOpen;
		AssignItem.HoverSelected = SelectedIndex == 18 || SetAssigneePopup.IsOpen;
		TagItem.HoverSelected = SelectedIndex == 19 || SetTagPopup.IsOpen;
		PomoItem.HoverSelected = SelectedIndex == 20 || TaskPomoPopup.IsOpen;
		CompleteItem.HoverSelected = SelectedIndex == 25 || ChangeDatePopup.IsOpen;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/taskoperationdialog.xaml", UriKind.Relative);
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
			Root = (TaskOperationDialog)target;
			Root.Initialized += OnInitialized;
			break;
		case 2:
			Container = (StackPanel)target;
			Container.MouseMove += ShowPopup;
			break;
		case 3:
			DateStack = (StackPanel)target;
			break;
		case 4:
			DatePanel = (Grid)target;
			break;
		case 5:
			ThirdColumn = (ColumnDefinition)target;
			break;
		case 6:
			FourthColumn = (ColumnDefinition)target;
			break;
		case 7:
			SixthColumn = (ColumnDefinition)target;
			break;
		case 8:
			SeventhColumn = (ColumnDefinition)target;
			break;
		case 9:
			TodayGrid = (Border)target;
			TodayGrid.MouseLeftButtonUp += SetTimeClick;
			break;
		case 10:
			TomorrowGrid = (Border)target;
			TomorrowGrid.MouseLeftButtonUp += SetTimeClick;
			break;
		case 11:
			NextWeekGrid = (Border)target;
			NextWeekGrid.MouseLeftButtonUp += SetTimeClick;
			break;
		case 12:
			SkipGrid = (Border)target;
			SkipGrid.MouseLeftButtonUp += SkipRecurrenceClick;
			break;
		case 13:
			SetDateBorder = (Border)target;
			SetDateBorder.MouseLeftButtonUp += SetTimeClick;
			break;
		case 14:
			DelayOrAdvance = (Border)target;
			DelayOrAdvance.MouseLeftButtonUp += SetTimeClick;
			break;
		case 15:
			ClearDateBorder = (Border)target;
			ClearDateBorder.MouseLeftButtonUp += SetTimeClick;
			break;
		case 16:
			PriorityGrid = (Grid)target;
			break;
		case 17:
			PriorityHighGrid = (Border)target;
			PriorityHighGrid.MouseLeftButtonUp += PriorityGridClick;
			break;
		case 18:
			PriorityMediumGrid = (Border)target;
			PriorityMediumGrid.MouseLeftButtonUp += PriorityGridClick;
			break;
		case 19:
			PriorityLowGrid = (Border)target;
			PriorityLowGrid.MouseLeftButtonUp += PriorityGridClick;
			break;
		case 20:
			PriorityNoGrid = (Border)target;
			PriorityNoGrid.MouseLeftButtonUp += PriorityGridClick;
			break;
		case 21:
			PriorityLine = (Line)target;
			break;
		case 22:
			CreateSubTaskButton = (OptionCheckBox)target;
			break;
		case 23:
			LinkParentTaskButton = (OptionCheckBox)target;
			break;
		case 24:
			PinButton = (OptionCheckBox)target;
			break;
		case 25:
			AbandonedButton = (OptionCheckBox)target;
			break;
		case 26:
			BatchAbandonButton = (OptionCheckBox)target;
			break;
		case 27:
			ReopenButton = (OptionCheckBox)target;
			break;
		case 28:
			ProjectPlacement = (ContentControl)target;
			break;
		case 29:
			MoveButton = (Grid)target;
			break;
		case 30:
			ProjectItem = (OptionCheckBox)target;
			break;
		case 31:
			SetProjectPopup = (EscPopup)target;
			break;
		case 32:
			AssignBtn = (Grid)target;
			break;
		case 33:
			AssignItem = (OptionCheckBox)target;
			break;
		case 34:
			SetAssigneePopup = (EscPopup)target;
			break;
		case 35:
			TagPlacement = (ContentControl)target;
			break;
		case 36:
			TagGrid = (Grid)target;
			break;
		case 37:
			TagItem = (OptionCheckBox)target;
			break;
		case 38:
			SetTagPopup = (EscPopup)target;
			break;
		case 39:
			BatchLinkParentTaskButton = (OptionCheckBox)target;
			break;
		case 40:
			Line1 = (Line)target;
			break;
		case 41:
			PomoPanel = (StackPanel)target;
			break;
		case 42:
			PomoGrid = (Grid)target;
			break;
		case 43:
			PomoItem = (OptionCheckBox)target;
			break;
		case 44:
			TaskPomoPopup = (EscPopup)target;
			break;
		case 45:
			TaskPomoSetDialog = (TaskPomoSetDialog)target;
			break;
		case 46:
			CopyButton = (OptionCheckBox)target;
			break;
		case 47:
			CopyLinkGrid = (OptionCheckBox)target;
			break;
		case 48:
			CopyTextGrid = (OptionCheckBox)target;
			break;
		case 49:
			MergeBtn = (OptionCheckBox)target;
			break;
		case 50:
			ChangeCompleteDate = (Grid)target;
			break;
		case 51:
			CompleteItem = (OptionCheckBox)target;
			break;
		case 52:
			ChangeDatePopup = (EscPopup)target;
			break;
		case 53:
			DivideLine = (Line)target;
			break;
		case 54:
			SwitchButton = (OptionCheckBox)target;
			break;
		case 55:
			DeleteButton = (CommonDeleteIcon)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
