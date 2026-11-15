using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using TickTickDao;
using TickTickModels;
using TickTickUtils;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Pomo;

public class TaskPomoSetDialog : UserControl, ITabControl, IComponentConnector
{
	private int _originalEstimatedCount;

	private string _taskId;

	private PomodoroSummaryModel _taskPomo;

	private static int _defaultIndex;

	private string _eventType;

	private string _eventCtype;

	private int _tabIndex;

	internal TaskPomoSetDialog Root;

	internal ContentControl Container;

	internal TextBox EmptyBox;

	internal StackPanel TaskPomoPanel;

	internal OptionCheckBox StartPomoButton;

	internal OptionCheckBox StartTimingButton;

	internal OptionCheckBox EstimateButton;

	internal StackPanel EstimatePanel;

	internal CustomComboBox EstimateTypeComboBox;

	internal Grid EstimatePomoGrid;

	internal TextBox PomoCount;

	internal Grid EstimateDurationGrid;

	internal TextBox HourCount;

	internal TextBox MinuteCount;

	internal Grid SaveButtonPanel;

	internal Button SaveButton;

	internal Button CancelButton;

	private bool _contentLoaded;

	public event EventHandler<bool> Closed;

	public TaskPomoSetDialog()
	{
		InitializeComponent();
		EstimateTypeComboBox.Init(new ObservableCollection<ComboBoxViewModel>
		{
			new ComboBoxViewModel(0, Utils.GetString("EstimatedPomo"), 32.0)
			{
				Selected = true
			},
			new ComboBoxViewModel(1, Utils.GetString("EstimatedDurationPro"), 32.0)
		}, null);
	}

	public TaskPomoSetDialog(bool inSticky)
	{
		InitializeComponent();
		EstimateTypeComboBox.Init(new ObservableCollection<ComboBoxViewModel>
		{
			new ComboBoxViewModel(0, Utils.GetString("EstimatedPomo"), 32.0)
			{
				Selected = true
			},
			new ComboBoxViewModel(1, Utils.GetString("EstimatedDurationPro"), 32.0)
		}, null);
		if (inSticky)
		{
			Container.Style = null;
			base.FontSize = 12.0;
			StartPomoButton.Height = 30.0;
			StartTimingButton.Height = 30.0;
			EstimateButton.Height = 30.0;
			EstimatePomoGrid.Height = 30.0;
		}
	}

	public async void InitData(string taskId, bool force = false, string eventType = null, string eventCtype = null, bool onEnter = false)
	{
		TaskPomoPanel.Visibility = Visibility.Visible;
		EstimatePanel.Visibility = Visibility.Collapsed;
		_eventType = eventType;
		_eventCtype = eventCtype;
		if (onEnter)
		{
			StartPomoButton.HoverSelected = true;
			EstimateButton.HoverSelected = false;
			StartTimingButton.HoverSelected = false;
		}
		if (!(_taskId != taskId || force))
		{
			return;
		}
		_taskId = taskId;
		_taskPomo = await PomoService.GetAdjustPomoSummary(_taskId);
		if (_taskPomo != null)
		{
			if (_taskPomo.estimatedPomo > 0)
			{
				PomoCount.Text = _taskPomo.estimatedPomo.ToString() ?? "";
				EstimateTypeComboBox.SetSelected(0);
			}
			else if (_taskPomo.EstimatedDuration > 0)
			{
				long num = _taskPomo.EstimatedDuration / 60;
				HourCount.Text = (num / 60).ToString() ?? "";
				MinuteCount.Text = (num % 60).ToString() ?? "";
				EstimateTypeComboBox.SetSelected(1);
			}
			else
			{
				HourCount.Text = "0";
				MinuteCount.Text = "0";
				PomoCount.Text = "0";
				EstimateTypeComboBox.SetSelected(0);
			}
		}
		else
		{
			HourCount.Text = "0";
			MinuteCount.Text = "0";
			PomoCount.Text = "0";
			EstimateTypeComboBox.SetSelected(_defaultIndex);
			_taskPomo = new PomodoroSummaryModel
			{
				id = Utils.GetGuid(),
				taskId = taskId,
				userId = LocalSettings.Settings.LoginUserId
			};
		}
		StartPomoButton.Text = Utils.GetString("StartPomo");
		StartTimingButton.Text = Utils.GetString("StartTiming");
		if (TickFocusManager.Working)
		{
			StartPomoButton.IsEnabled = LocalSettings.Settings.PomoType == FocusConstance.Focus;
			StartTimingButton.IsEnabled = !StartPomoButton.IsEnabled;
		}
		else
		{
			StartPomoButton.IsEnabled = true;
			StartTimingButton.IsEnabled = true;
		}
		StartPomoButton.Opacity = (StartPomoButton.IsEnabled ? 1.0 : 0.56);
		StartTimingButton.Opacity = (StartTimingButton.IsEnabled ? 1.0 : 0.56);
		if (EstimateTypeComboBox.SelectedIndex == 1)
		{
			EstimatePomoGrid.Visibility = Visibility.Collapsed;
			EstimateDurationGrid.Visibility = Visibility.Visible;
		}
		else
		{
			EstimatePomoGrid.Visibility = Visibility.Visible;
			EstimateDurationGrid.Visibility = Visibility.Collapsed;
		}
	}

	private async void OnSaveClick(object sender, RoutedEventArgs e)
	{
		e.Handled = true;
		await TrySaveAndClose();
	}

	private async Task TrySaveAndClose()
	{
		TaskModel task = await TaskDao.GetThinTaskById(_taskId);
		if (task != null)
		{
			_defaultIndex = EstimateTypeComboBox.SelectedIndex;
			if (EstimatePomoGrid.Visibility == Visibility.Visible)
			{
				int.TryParse(PomoCount.Text, out var result);
				_taskPomo.estimatedPomo = result;
				_taskPomo.EstimatedDuration = 0L;
			}
			else if (EstimateDurationGrid.Visibility == Visibility.Visible)
			{
				int.TryParse(HourCount.Text, out var result2);
				int.TryParse(MinuteCount.Text, out var result3);
				int num = Math.Min(5999999, result2 * 60 + result3) * 60;
				_taskPomo.EstimatedDuration = num;
				_taskPomo.estimatedPomo = 0;
			}
			await PomoSummaryDao.SavePomoSummary(_taskPomo);
			DataChangedNotifier.NotifyPomoChanged(_taskId);
			task.modifiedTime = DateTime.Now;
			await TaskDao.UpdateTask(task);
			await SyncStatusDao.AddModifySyncStatus(task.id);
			SyncManager.Sync();
			this.Closed?.Invoke(this, e: false);
		}
	}

	private async void OnCancelClick(object sender, RoutedEventArgs e)
	{
		e.Handled = true;
		EstimatePanel.Visibility = Visibility.Collapsed;
		TaskPomoPanel.Visibility = Visibility.Visible;
	}

	private void OnEstimatePreviewInput(object sender, TextCompositionEventArgs e)
	{
		if (sender != null && sender is TextBox textBox)
		{
			int result;
			bool flag = int.TryParse(textBox.Text.Insert(textBox.SelectionStart, e.Text), out result);
			e.Handled = !flag;
		}
	}

	private async void OnPomoCountChanged(object sender, TextChangedEventArgs e)
	{
		if (sender is TextBox textBox)
		{
			await Task.Delay(10);
			int.TryParse(textBox.Text, out var result);
			if (result > 60)
			{
				textBox.Text = 60.ToString() ?? "";
				textBox.SelectAll();
			}
			if (result <= 0)
			{
				textBox.Text = "0";
				textBox.SelectAll();
			}
		}
	}

	private async void OnTextChanged(object sender, TextChangedEventArgs e)
	{
		if (sender is TextBox textBox)
		{
			await Task.Delay(10);
			int.TryParse(textBox.Text, out var result);
			if (result > 99999)
			{
				textBox.Text = 99999.ToString() ?? "";
				textBox.SelectAll();
			}
			if (result <= 0)
			{
				textBox.Text = "0";
				textBox.SelectAll();
			}
		}
	}

	private void OnTextKeyDown(object sender, KeyEventArgs e)
	{
		switch (e.Key)
		{
		case Key.Return:
			if (_tabIndex < 0)
			{
				TrySaveAndClose();
			}
			break;
		case Key.Escape:
			this.Closed?.Invoke(this, e: false);
			break;
		}
	}

	public static bool ConfirmSwitch(string title, bool isPomo, Window owner = null)
	{
		List<Inline> list = new List<Inline>();
		string[] array = Utils.GetString(isPomo ? "SwitchTimingTips" : "SwitchPomoTips").Split('0');
		if (array.Length == 2)
		{
			list.Add(new Run(array[0].Substring(0, array[0].Length - 1)));
			list.Add(new Run(title)
			{
				Foreground = ThemeUtil.GetColor("PrimaryColor")
			});
			list.Add(new Run(array[1].Substring(1, array[1].Length - 1)));
		}
		bool? flag = new CustomerDialog("", list, Utils.GetString("Ok"), Utils.GetString("Cancel"), owner).ShowDialog();
		if (flag.HasValue)
		{
			return flag != false;
		}
		return false;
	}

	private async void OnStartFocusClick(object sender, EventArgs e)
	{
		StartFocus(isPomo: true);
	}

	private async void StartFocus(bool isPomo)
	{
		if (!string.IsNullOrEmpty(_taskId))
		{
			if (TickFocusManager.IsFocusing(_taskId))
			{
				Utils.Toast(Utils.GetString("TaskIsFocused"));
				this.Closed?.Invoke(this, e: true);
				return;
			}
			TaskModel taskModel = await TaskDao.GetThinTaskById(_taskId);
			if (taskModel != null)
			{
				if (!TickFocusManager.ConfirmSwitch(taskModel.id, taskModel.title))
				{
					return;
				}
				if (TickFocusManager.Status == PomoStatus.WaitingWork)
				{
					UserActCollectUtils.AddClickEvent("focus", "start_from", (_eventCtype == "cm_single_task") ? "cm_single_task" : ((_eventCtype == "task_detail_pomo") ? "task_detail_pomo" : "task_detail"));
				}
				if (!TickFocusManager.Working)
				{
					TickFocusManager.SetFocusType((!isPomo) ? 1 : 0);
					if (isPomo)
					{
						TimerModel timerModel = await TimerDao.GetTimerByObjId(taskModel.id);
						if (timerModel != null && timerModel.Type == "pomodoro")
						{
							TickFocusManager.Config.SetPomoSeconds(timerModel.PomodoroTime);
						}
					}
				}
				TickFocusManager.StartFocus(_taskId);
				UserActCollectUtils.AddClickEvent(_eventType, _eventCtype, "start_focus");
			}
		}
		this.Closed?.Invoke(this, e: true);
	}

	private async void OnStartTimingClick(object sender, EventArgs e)
	{
		StartFocus(isPomo: false);
	}

	private void OnEstimateClick(object sender, EventArgs e)
	{
		UserActCollectUtils.AddClickEvent(_eventType, _eventCtype, "estimated_pomo");
		EstimatePanel.Visibility = Visibility.Visible;
		TaskPomoPanel.Visibility = Visibility.Collapsed;
		if (EstimatePomoGrid.Visibility == Visibility.Visible)
		{
			PomoCount.SelectAll();
			PomoCount.Focus();
		}
		if (EstimateDurationGrid.Visibility == Visibility.Visible)
		{
			HourCount.SelectAll();
			HourCount.Focus();
		}
	}

	private void OnEstimateTypeChanged(object sender, ComboBoxViewModel e)
	{
		object value = e.Value;
		if (value is int && (int)value == 0)
		{
			EstimatePomoGrid.Visibility = Visibility.Visible;
			EstimateDurationGrid.Visibility = Visibility.Collapsed;
			PomoCount.SelectAll();
			PomoCount.Focus();
		}
		else
		{
			EstimatePomoGrid.Visibility = Visibility.Collapsed;
			EstimateDurationGrid.Visibility = Visibility.Visible;
			HourCount.SelectAll();
			HourCount.Focus();
		}
	}

	public bool HandleTab(bool shift)
	{
		if (EstimatePanel.IsVisible)
		{
			if (EstimateTypeComboBox.IsOpen)
			{
				EstimateTypeComboBox.UpDownSelect(shift);
				return false;
			}
			if (_tabIndex < 0)
			{
				_tabIndex = 0;
				EstimateTypeComboBox.TabSelected = true;
				EmptyBox.Focus();
			}
			else
			{
				_tabIndex += ((!shift) ? 1 : (-1));
				_tabIndex += 5;
				_tabIndex %= 5;
				EstimateTypeComboBox.TabSelected = _tabIndex == 0;
				switch (_tabIndex)
				{
				case 1:
					if (EstimateTypeComboBox.SelectedIndex == 0)
					{
						PomoCount.Focus();
					}
					else
					{
						HourCount.Focus();
					}
					break;
				case 2:
					if (EstimateTypeComboBox.SelectedIndex == 0)
					{
						HandleTab(shift);
					}
					else
					{
						MinuteCount.Focus();
					}
					break;
				default:
					EmptyBox.Focus();
					break;
				}
				UiUtils.SetSaveButtonTabSelected(SaveButton, _tabIndex == 3);
				UiUtils.SetCancelButtonTabSelected(CancelButton, _tabIndex == 4);
			}
		}
		else
		{
			UpDownSelect(shift);
		}
		return true;
	}

	public bool HandleEnter()
	{
		if (TaskPomoPanel.IsVisible)
		{
			if (StartPomoButton.HoverSelected)
			{
				StartFocus(isPomo: true);
			}
			else if (StartTimingButton.HoverSelected)
			{
				StartFocus(isPomo: false);
			}
			else if (EstimateButton.HoverSelected)
			{
				OnEstimateClick(null, null);
				_tabIndex = -1;
				HandleTab(shift: true);
			}
		}
		else if (EstimatePanel.IsVisible)
		{
			switch (_tabIndex)
			{
			case 0:
				EstimateTypeComboBox.HandleEnter();
				break;
			case 3:
				TrySaveAndClose();
				break;
			case 4:
				EstimatePanel.Visibility = Visibility.Collapsed;
				TaskPomoPanel.Visibility = Visibility.Visible;
				break;
			}
		}
		return true;
	}

	public bool HandleEsc()
	{
		if (EstimateTypeComboBox.IsOpen)
		{
			EstimateTypeComboBox.Close();
			return true;
		}
		return false;
	}

	public bool UpDownSelect(bool isUp)
	{
		if (!TaskPomoPanel.IsVisible)
		{
			return false;
		}
		if (StartPomoButton.HoverSelected)
		{
			StartPomoButton.HoverSelected = false;
			if (isUp && EstimateButton.IsVisible)
			{
				EstimateButton.HoverSelected = true;
			}
			else
			{
				StartTimingButton.HoverSelected = true;
			}
		}
		else if (StartTimingButton.HoverSelected)
		{
			StartTimingButton.HoverSelected = false;
			if (!isUp && EstimateButton.IsVisible)
			{
				EstimateButton.HoverSelected = true;
			}
			else
			{
				StartPomoButton.HoverSelected = true;
			}
		}
		else if (EstimateButton.HoverSelected)
		{
			EstimateButton.HoverSelected = false;
			if (isUp)
			{
				StartTimingButton.HoverSelected = true;
			}
			else
			{
				StartPomoButton.HoverSelected = true;
			}
		}
		else
		{
			StartPomoButton.HoverSelected = true;
		}
		return true;
	}

	public bool LeftRightSelect(bool isLeft)
	{
		return false;
	}

	public bool IsInputFocus()
	{
		if (!HourCount.IsFocused && !MinuteCount.IsFocused)
		{
			return PomoCount.IsFocused;
		}
		return true;
	}

	public bool CheckMouseMove()
	{
		if (EstimateTypeComboBox.IsOpen)
		{
			return true;
		}
		Point position = Mouse.GetPosition(this);
		if (!(position.X < -5.0) && !(position.X > base.ActualWidth + 10.0) && !(position.Y < -5.0))
		{
			return !(position.Y > base.ActualHeight + 10.0);
		}
		return false;
	}

	private void OnTextGotFocus(object sender, RoutedEventArgs e)
	{
		if (object.Equals(sender, PomoCount) || object.Equals(sender, HourCount))
		{
			_tabIndex = 1;
		}
		if (object.Equals(sender, MinuteCount))
		{
			_tabIndex = 2;
		}
		UiUtils.SetSaveButtonTabSelected(SaveButton, tabSelected: false);
		UiUtils.SetCancelButtonTabSelected(CancelButton, tabSelected: false);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/pomo/taskpomosetdialog.xaml", UriKind.Relative);
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
			Root = (TaskPomoSetDialog)target;
			break;
		case 2:
			Container = (ContentControl)target;
			break;
		case 3:
			EmptyBox = (TextBox)target;
			break;
		case 4:
			TaskPomoPanel = (StackPanel)target;
			break;
		case 5:
			StartPomoButton = (OptionCheckBox)target;
			break;
		case 6:
			StartTimingButton = (OptionCheckBox)target;
			break;
		case 7:
			EstimateButton = (OptionCheckBox)target;
			break;
		case 8:
			EstimatePanel = (StackPanel)target;
			break;
		case 9:
			EstimateTypeComboBox = (CustomComboBox)target;
			break;
		case 10:
			EstimatePomoGrid = (Grid)target;
			break;
		case 11:
			PomoCount = (TextBox)target;
			PomoCount.GotFocus += OnTextGotFocus;
			PomoCount.PreviewTextInput += OnEstimatePreviewInput;
			PomoCount.TextChanged += OnPomoCountChanged;
			PomoCount.KeyDown += OnTextKeyDown;
			break;
		case 12:
			EstimateDurationGrid = (Grid)target;
			break;
		case 13:
			HourCount = (TextBox)target;
			HourCount.GotFocus += OnTextGotFocus;
			HourCount.PreviewTextInput += OnEstimatePreviewInput;
			HourCount.TextChanged += OnTextChanged;
			HourCount.KeyDown += OnTextKeyDown;
			break;
		case 14:
			MinuteCount = (TextBox)target;
			MinuteCount.GotFocus += OnTextGotFocus;
			MinuteCount.PreviewTextInput += OnEstimatePreviewInput;
			MinuteCount.TextChanged += OnTextChanged;
			MinuteCount.KeyDown += OnTextKeyDown;
			break;
		case 15:
			SaveButtonPanel = (Grid)target;
			break;
		case 16:
			SaveButton = (Button)target;
			SaveButton.Click += OnSaveClick;
			break;
		case 17:
			CancelButton = (Button)target;
			CancelButton.Click += OnCancelClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
