using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media.Animation;
using Emoji.Wpf;
using TickTickDao;
using TickTickModels;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.MarkDown;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Tag;
using ticktick_WPF.Views.Time;

namespace ticktick_WPF.Views.Pomo.FocusStatistics;

public class AddFocusRecordWindow : Window, IOkCancelWindow, IComponentConnector
{
	private int _focusType;

	private int _pomoNum = 1;

	private DateTime? _startTime;

	private DateTime? _endTime;

	private int? _minutes;

	private PomoTask _focusTask = new PomoTask();

	private int _pomoDuration;

	private PomoFilterControl _pomoFilter;

	internal Grid TaskSelectGrid;

	internal EmjTextBlock TaskTextBox;

	internal EscPopup TaskPopup;

	internal TextBlock StartTextBox;

	internal EscPopup StartPopup;

	internal TextBlock EndTextBox;

	internal EscPopup EndPopup;

	internal TextBlock FocusTypeTextBox;

	internal EscPopup TypePopup;

	internal StackPanel EstimatePanel;

	internal AnimateGroupTitle TypeSelector;

	internal Grid PomoGrid;

	internal NumInputTextBox PomoCount;

	internal Grid TimerGrid;

	internal NumInputTextBox HourText;

	internal NumInputTextBox MinuteText;

	internal EmojiEditor NoteEditor;

	internal TextBlock LengthText;

	internal Button SaveButton;

	internal Border ToastBorder;

	internal TextBlock ToastText;

	private bool _contentLoaded;

	private bool? firstChangeStart { get; set; }

	public AddFocusRecordWindow(string type)
	{
		InitializeComponent();
		base.InputBindings.Add(new KeyBinding(OkCancelWindowCommands.EscCommand, new KeyGesture(Key.Escape))
		{
			CommandParameter = this
		});
		base.InputBindings.Add(new KeyBinding(OkCancelWindowCommands.OkCommand, new KeyGesture(Key.Return))
		{
			CommandParameter = this
		});
		base.Title = Utils.GetString("AddFocusRecord");
		TaskTextBox.Text = Utils.GetString("SetTask");
		StartTextBox.Text = Utils.GetString("SetTime");
		EndTextBox.Text = Utils.GetString("SetTime");
		_pomoDuration = Math.Max(5, LocalSettings.Settings.PomoDuration);
		if (type == "timing")
		{
			TypeSelector.SetSelectedIndex(1);
			_focusType = 1;
			SetTypeValues();
			FocusTypeTextBox.Text = Utils.GetString("Timing") + " : 0 " + Utils.GetString("PublicMinute");
		}
		else
		{
			FocusTypeTextBox.Text = Utils.GetString("PomoTimer2") + " : 0 " + Utils.GetString("EstimatePomoCount");
		}
	}

	public AddFocusRecordWindow(TimerModel timer)
		: this(timer.Type)
	{
		bool flag = timer.ObjType == "habit";
		TaskTextBox.Text = timer.Name;
		TaskTextBox.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity100");
		TaskSelectGrid.IsEnabled = false;
		TaskSelectGrid.Opacity = 0.6;
		_focusTask.TaskId = (flag ? null : timer.ObjId);
		_focusTask.HabitId = (flag ? timer.ObjId : null);
		_focusTask.Title = (string.IsNullOrEmpty(timer.ObjId) ? null : timer.Name);
		_focusTask.TimerSid = timer.Id;
		_focusTask.TimerName = timer.Name;
		if (!flag)
		{
			TaskBaseViewModel taskById = TaskCache.GetTaskById(timer.ObjId);
			_focusTask.ProjectName = taskById?.ProjectName;
			_focusTask.Tags = taskById?.Tags;
			_focusTask.TagString = TagSerializer.ToJsonContent(taskById?.Tags?.ToList());
		}
		_pomoDuration = Math.Max(5, (timer.Type == "pomodoro") ? timer.PomodoroTime : LocalSettings.Settings.PomoDuration);
		if (timer.Type != "pomodoro")
		{
			TypeSelector.SetSelectedIndex(1);
			_focusType = 1;
			SetTypeValues();
		}
	}

	public void OnCancel()
	{
		if (!EndPopup.IsOpen && !StartPopup.IsOpen && !TaskPopup.IsOpen && !TypePopup.IsOpen)
		{
			Close();
		}
	}

	public void Ok()
	{
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	private async void OnSaveClick(object sender, RoutedEventArgs e)
	{
		if (_endTime.HasValue && _startTime.HasValue)
		{
			double totalMinutes = (_endTime.Value - _startTime.Value).TotalMinutes;
			int count = ((_focusType != 0) ? 1 : _pomoNum);
			double duration = ((_focusType == 0) ? ((double)_pomoDuration) : totalMinutes);
			if (string.IsNullOrEmpty(_focusTask.TimerSid))
			{
				TimerModel timerModel = await TimerDao.GetTimerByObjId(_focusTask.TaskId ?? _focusTask.HabitId);
				if (timerModel != null)
				{
					_focusTask.TimerSid = timerModel.Id;
					_focusTask.TimerName = _focusTask.Title;
				}
			}
			for (int i = 0; i < count; i++)
			{
				PomodoroModel pomodoroModel = new PomodoroModel
				{
					Id = Utils.GetGuid(),
					StartTime = _startTime.Value.AddMinutes((double)i * duration),
					EndTime = ((i == count - 1) ? _endTime.Value : _startTime.Value.AddMinutes((double)(i + 1) * duration)),
					UserId = LocalSettings.Settings.LoginUserId,
					Added = true,
					Type = _focusType,
					Status = 1,
					Note = ((i == count - 1) ? NoteEditor.Text : string.Empty)
				};
				PomoTask pomoTask = _focusTask.Copy();
				pomoTask.PomoId = pomodoroModel.Id;
				pomoTask.StartTime = pomodoroModel.StartTime;
				pomoTask.EndTime = pomodoroModel.EndTime;
				pomodoroModel.Tasks = new PomoTask[1] { pomoTask };
				LocalSettings.Settings.PomoLocalSetting.AddRecordType = ((_focusType == 0) ? "pomodoro" : "timing");
				LocalSettings.Settings.Save(force: true);
				await PomoService.SaveFocusModel(pomodoroModel);
			}
		}
		UserActCollectUtils.AddClickEvent("focus", "focus_tab", "add_focus_record");
		Close();
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void OnNoteChanged(object sender, EventArgs e)
	{
		if (NoteEditor.Text.Length >= 400)
		{
			LengthText.Visibility = Visibility.Visible;
			LengthText.Text = NoteEditor.Text.Length + "/500";
		}
		else
		{
			LengthText.Visibility = Visibility.Collapsed;
		}
	}

	private void OnTaskClick(object sender, MouseButtonEventArgs e)
	{
		Mouse.Capture(null);
		e.Handled = true;
		if (_pomoFilter == null)
		{
			_pomoFilter = new PomoFilterControl(TaskPopup, inFocus: false)
			{
				ShowComplete = true,
				DataContext = new FocusViewModel()
			};
		}
		_pomoFilter.ClearItemSelectedEvent();
		_pomoFilter.ItemSelected += async delegate(object obj, DisplayItemModel model)
		{
			TimerModel timerModel = await TimerDao.GetTimerByIdOrObjId(model.Id);
			_focusTask.TimerSid = timerModel?.Id;
			_focusTask.TimerName = timerModel?.Name;
			TaskTextBox.Text = (model.IsHabit ? model.Habit.Name : model.Title);
			TaskTextBox.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity100");
			_focusTask.HabitId = (model.IsHabit ? model.Habit.Id : ((timerModel != null && timerModel.ObjType == "habit") ? timerModel.ObjId : null));
			_focusTask.Title = (model.IsHabit ? model.Habit.Name : model.Title);
			TaskBaseViewModel taskBaseViewModel = (model.IsTaskOrNote ? TaskCache.GetTaskById(model.Id) : TaskCache.GetTaskById(timerModel?.ObjId));
			_focusTask.TaskId = taskBaseViewModel?.Id;
			_focusTask.ProjectName = taskBaseViewModel?.ProjectName;
			_focusTask.Tags = taskBaseViewModel?.Tags;
			_focusTask.TagString = taskBaseViewModel?.Tag;
			TaskPopup.IsOpen = false;
		};
		TaskPopup.IsOpen = true;
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		base.Owner?.Activate();
		base.OnClosing(e);
	}

	private void OnStartTimeClick(object sender, MouseButtonEventArgs e)
	{
		Mouse.Capture(null);
		e.Handled = true;
		DateTimeSelector dateTimeSelector = new DateTimeSelector(_startTime ?? _endTime?.AddMinutes(-1 * _pomoDuration) ?? DateTime.Today.AddHours(DateTime.Now.Hour).AddHours(-3.0), DateTime.Today.AddDays(1.0).AddSeconds(-1.0));
		dateTimeSelector.TimeSelected += delegate(object o, DateTime t)
		{
			DateTime? startTime = _startTime;
			_startTime = t;
			if (!_endTime.HasValue && _minutes.HasValue)
			{
				_endTime = t.AddMinutes(_minutes.Value);
			}
			else if (startTime.HasValue && _endTime.HasValue)
			{
				double totalMinutes = (_endTime.Value - startTime.Value).TotalMinutes;
				_endTime = t.AddMinutes(totalMinutes);
			}
			SetStartText();
			SetEndText();
			SetFocusText();
			SetSaveButtonEnable();
			StartPopup.IsOpen = false;
			if (!firstChangeStart.HasValue)
			{
				firstChangeStart = true;
			}
		};
		dateTimeSelector.Cancel += delegate
		{
			StartPopup.IsOpen = false;
		};
		StartPopup.Child = dateTimeSelector;
		StartPopup.IsOpen = true;
	}

	private void SetStartText()
	{
		if (_startTime.HasValue)
		{
			bool flag = _startTime.Value.Year != DateTime.Today.Year || (_endTime.HasValue && _endTime.Value.Year != DateTime.Today.Year);
			StartTextBox.Text = (flag ? DateUtils.FormatYearMonthDay(_startTime.Value, DateFormatLen.Short) : DateUtils.FormatMonthDay(_startTime.Value, DateFormatLen.Middle)) + "  " + DateUtils.GetTimeText(_startTime.Value);
			StartTextBox.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity100");
		}
	}

	private void SetEndText()
	{
		if (!_endTime.HasValue)
		{
			return;
		}
		bool flag = _endTime.Value.Year != DateTime.Today.Year || (_startTime.HasValue && _startTime.Value.Year != DateTime.Today.Year);
		EndTextBox.Text = (flag ? DateUtils.FormatYearMonthDay(_endTime.Value, DateFormatLen.Short) : DateUtils.FormatMonthDay(_endTime.Value, DateFormatLen.Middle)) + "  " + DateUtils.GetTimeText(_endTime.Value);
		EndTextBox.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity100");
		if (_startTime.HasValue)
		{
			int num = (int)(_endTime.Value - _startTime.Value).TotalMinutes;
			int num2 = num / _pomoDuration;
			if (num - num2 * _pomoDuration >= 5)
			{
				num2++;
			}
			_pomoNum = num2;
		}
	}

	private void SetFocusText()
	{
		if (_endTime.HasValue && _startTime.HasValue)
		{
			_minutes = (int)(_endTime.Value - _startTime.Value).TotalMinutes;
		}
		if (_minutes.HasValue)
		{
			int value = _minutes.Value;
			if (_focusType == 0)
			{
				FocusTypeTextBox.Text = Utils.GetString("PomoTimer2") + " : " + _pomoNum + " " + Utils.GetString((_pomoNum > 1) ? "EstimatePomoCounts" : "EstimatePomoCount");
			}
			else
			{
				FocusTypeTextBox.Text = Utils.GetString("Timing") + " : " + Utils.MinutesToHourMinuteText(value);
			}
			FocusTypeTextBox.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity100");
		}
	}

	private void OnEndTimeClick(object sender, MouseButtonEventArgs e)
	{
		Mouse.Capture(null);
		e.Handled = true;
		DateTimeSelector dateTimeSelector = new DateTimeSelector(_endTime ?? _startTime?.AddMinutes(_pomoDuration) ?? DateTime.Today.AddHours(DateTime.Now.Hour), DateTime.Today.AddDays(1.0).AddSeconds(-1.0));
		dateTimeSelector.TimeSelected += delegate(object o, DateTime t)
		{
			if (!_startTime.HasValue && _minutes.HasValue)
			{
				_startTime = t.AddMinutes(-1 * _minutes.Value);
			}
			_endTime = t;
			SetEndText();
			SetStartText();
			SetFocusText();
			SetSaveButtonEnable();
			EndPopup.IsOpen = false;
			if (!firstChangeStart.HasValue)
			{
				firstChangeStart = false;
			}
		};
		dateTimeSelector.Cancel += delegate
		{
			EndPopup.IsOpen = false;
		};
		EndPopup.Child = dateTimeSelector;
		EndPopup.IsOpen = true;
	}

	private void SetSaveButtonEnable()
	{
		if (!_startTime.HasValue || !_endTime.HasValue)
		{
			SaveButton.IsEnabled = false;
		}
		else if (_endTime.Value > DateTime.Now)
		{
			Toast(Utils.GetString("AddFocusBeforeNow"));
			SaveButton.IsEnabled = false;
		}
		else if ((_endTime.Value - _startTime.Value).TotalMinutes < 5.0)
		{
			Toast(Utils.GetString("AddFocusMinMinutes"));
			SaveButton.IsEnabled = false;
		}
		else if (_focusType == 1 && (_endTime.Value - _startTime.Value).TotalMinutes > 720.0)
		{
			Toast(Utils.GetString("AddFocusMaxDuration"));
			SaveButton.IsEnabled = false;
		}
		else if (_focusType == 0 && _pomoNum > 10)
		{
			Toast(Utils.GetString("AddFocusMaxCount"));
			SaveButton.IsEnabled = false;
		}
		else
		{
			SaveButton.IsEnabled = true;
		}
	}

	private void Toast(string text)
	{
		ToastBorder.Visibility = Visibility.Visible;
		ToastText.Text = text;
		Storyboard storyboard = (Storyboard)FindResource("ShowToast");
		ToastBorder.BeginStoryboard(storyboard);
	}

	private void OnToasted(object sender, EventArgs e)
	{
		ToastBorder.Visibility = Visibility.Collapsed;
	}

	private void OnTypeSaveClick(object sender, RoutedEventArgs e)
	{
		_focusType = TypeSelector.SelectedIndex;
		int result3;
		if (_focusType == 1)
		{
			if (int.TryParse(HourText.Text, out var result) && int.TryParse(MinuteText.Text, out var result2))
			{
				int num = result * 60 + result2;
				if (num > 720)
				{
					num = 720;
				}
				if (num < 5)
				{
					num = 5;
				}
				_minutes = num;
			}
		}
		else if (int.TryParse(PomoCount.Text, out result3))
		{
			_pomoNum = result3;
			_minutes = result3 * _pomoDuration;
		}
		if (_minutes.HasValue && (_startTime.HasValue || _endTime.HasValue))
		{
			if (!_startTime.HasValue)
			{
				_startTime = _endTime.Value.AddMinutes(-1 * _minutes.Value);
			}
			else if (!_endTime.HasValue)
			{
				_endTime = _startTime.Value.AddMinutes(_minutes.Value);
			}
			else if (firstChangeStart == false)
			{
				_startTime = _endTime.Value.AddMinutes(-1 * _minutes.Value);
			}
			else
			{
				_endTime = _startTime.Value.AddMinutes(_minutes.Value);
			}
		}
		SetStartText();
		SetEndText();
		SetFocusText();
		SetSaveButtonEnable();
		TypePopup.IsOpen = false;
	}

	private void OnTypeCancelClick(object sender, RoutedEventArgs e)
	{
		TypePopup.IsOpen = false;
	}

	private void OnTypeClick(object sender, MouseButtonEventArgs e)
	{
		Mouse.Capture(null);
		e.Handled = true;
		TypeSelector.SetSelectedIndex(_focusType);
		TypePopup.IsOpen = true;
		SetTypeValues();
	}

	private void SetTypeValues()
	{
		PomoGrid.Visibility = ((_focusType != 0) ? Visibility.Collapsed : Visibility.Visible);
		TimerGrid.Visibility = ((_focusType != 1) ? Visibility.Collapsed : Visibility.Visible);
		if (_focusType == 1)
		{
			double num = ((_startTime.HasValue && _endTime.HasValue) ? (_endTime - _startTime).Value.TotalMinutes : ((double)_minutes.GetValueOrDefault()));
			HourText.Text = (int)num / 60 + string.Empty;
			MinuteText.Text = (int)num % 60 + string.Empty;
		}
		else
		{
			PomoCount.Text = _pomoNum.ToString() ?? "";
		}
	}

	private void OnTypeSelected(object sender, GroupTitleViewModel e)
	{
		_focusType = e.Index;
		SetTypeValues();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/pomo/focusstatistics/addfocusrecordwindow.xaml", UriKind.Relative);
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
			((Storyboard)target).Completed += OnToasted;
			break;
		case 2:
			TaskSelectGrid = (Grid)target;
			TaskSelectGrid.PreviewMouseLeftButtonUp += OnTaskClick;
			break;
		case 3:
			TaskTextBox = (EmjTextBlock)target;
			break;
		case 4:
			TaskPopup = (EscPopup)target;
			break;
		case 5:
			((Grid)target).PreviewMouseLeftButtonUp += OnStartTimeClick;
			break;
		case 6:
			StartTextBox = (TextBlock)target;
			break;
		case 7:
			StartPopup = (EscPopup)target;
			break;
		case 8:
			((Grid)target).PreviewMouseLeftButtonUp += OnEndTimeClick;
			break;
		case 9:
			EndTextBox = (TextBlock)target;
			break;
		case 10:
			EndPopup = (EscPopup)target;
			break;
		case 11:
			((Grid)target).PreviewMouseLeftButtonUp += OnTypeClick;
			break;
		case 12:
			FocusTypeTextBox = (TextBlock)target;
			break;
		case 13:
			TypePopup = (EscPopup)target;
			break;
		case 14:
			EstimatePanel = (StackPanel)target;
			break;
		case 15:
			TypeSelector = (AnimateGroupTitle)target;
			break;
		case 16:
			PomoGrid = (Grid)target;
			break;
		case 17:
			PomoCount = (NumInputTextBox)target;
			break;
		case 18:
			TimerGrid = (Grid)target;
			break;
		case 19:
			HourText = (NumInputTextBox)target;
			break;
		case 20:
			MinuteText = (NumInputTextBox)target;
			break;
		case 21:
			((Button)target).Click += OnTypeSaveClick;
			break;
		case 22:
			((Button)target).Click += OnTypeCancelClick;
			break;
		case 23:
			NoteEditor = (EmojiEditor)target;
			break;
		case 24:
			LengthText = (TextBlock)target;
			break;
		case 25:
			SaveButton = (Button)target;
			SaveButton.Click += OnSaveClick;
			break;
		case 26:
			((Button)target).Click += OnCancelClick;
			break;
		case 27:
			ToastBorder = (Border)target;
			break;
		case 28:
			ToastText = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
