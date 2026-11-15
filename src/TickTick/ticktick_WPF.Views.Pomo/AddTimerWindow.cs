using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using TickTickDao;
using TickTickModels;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Habit;
using ticktick_WPF.Views.MarkDown;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Misc.ColorSelector;

namespace ticktick_WPF.Views.Pomo;

public class AddTimerWindow : Window, IOkCancelWindow, IComponentConnector
{
	private bool _isNew;

	private TimerModel _timer;

	private PomoFilterControl _pomoFilter;

	internal Grid IconGrid;

	internal Image IconImage;

	internal EscPopup SetIconPopup;

	internal SetHabitIconControl SetIconControl;

	internal EmojiEditor TitleText;

	internal HoverIconButton LinkIcon;

	internal Border ClearIcon;

	internal RadioButton PomoRadio;

	internal NumInputTextBox PomoCount;

	internal RadioButton TimingRadio;

	internal Button SaveButton;

	private bool _contentLoaded;

	public AddTimerWindow(TimerModel model = null)
	{
		InitializeComponent();
		if (model == null)
		{
			model = GetDefaultTimer();
			_isNew = true;
		}
		_timer = model;
		base.Title = Utils.GetString(_isNew ? "AddTimer" : "EditTimer");
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	private async Task SetTitleText()
	{
		if (!string.IsNullOrEmpty(_timer.ObjId))
		{
			if (_timer.ObjType == "task")
			{
				TaskModel taskModel = await TaskDao.GetTaskById(_timer.ObjId);
				if (taskModel != null)
				{
					TitleText.Text = taskModel.title;
					return;
				}
			}
			else if (_timer.ObjType == "habit")
			{
				HabitModel habitModel = await HabitDao.GetHabitById(_timer.ObjId);
				if (habitModel != null)
				{
					TitleText.Text = habitModel.Name;
					return;
				}
			}
		}
		TitleText.Text = _timer.Name;
	}

	private void SetIconLocation()
	{
		bool flag = !string.IsNullOrEmpty(_timer.ObjId);
		TitleText.ReadOnly = flag;
		TitleText.TextBorder.IsEnabled = _isNew || !TitleText.ReadOnly;
		if (_isNew)
		{
			LinkIcon.Visibility = (((!string.IsNullOrEmpty(_timer.Name) || !string.IsNullOrEmpty(_timer.ObjId)) && string.IsNullOrEmpty(_timer.ObjId)) ? Visibility.Collapsed : Visibility.Visible);
			ClearIcon.Visibility = (((string.IsNullOrEmpty(_timer.Name) || !string.IsNullOrEmpty(_timer.ObjId)) && string.IsNullOrEmpty(_timer.ObjId)) ? Visibility.Collapsed : Visibility.Visible);
			LinkIcon.IsHitTestVisible = string.IsNullOrEmpty(_timer.ObjId);
			TitleText.Padding = new Thickness(flag ? 34 : 8, 0.0, 30.0, 0.0);
			LinkIcon.HorizontalAlignment = ((!flag) ? HorizontalAlignment.Right : HorizontalAlignment.Left);
		}
		else
		{
			LinkIcon.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
			ClearIcon.Visibility = ((string.IsNullOrEmpty(_timer.Name) || flag) ? Visibility.Collapsed : Visibility.Visible);
			TitleText.Padding = new Thickness(flag ? 34 : 8, 0.0, (!string.IsNullOrEmpty(_timer.Name) && !flag) ? 34 : 8, 0.0);
			LinkIcon.HorizontalAlignment = HorizontalAlignment.Left;
			LinkIcon.IsHitTestVisible = false;
		}
	}

	private void OnSelectIconClick(object sender, MouseButtonEventArgs e)
	{
		SetIconPopup.IsOpen = true;
		SetIconControl.Reset(_timer.Icon, _timer.Color);
		HwndHelper.SetFocus(SetIconPopup);
	}

	private void SetIcon()
	{
		IconImage.Source = HabitService.GetIcon(_timer.Icon, _timer.Color);
	}

	private void OnWindowLoaded(object sender, RoutedEventArgs e)
	{
		SetTitleText();
		PomoCount.Text = _timer.PomodoroTime.ToString();
		PomoRadio.IsChecked = _timer.Type == "pomodoro";
		TimingRadio.IsChecked = _timer.Type != "pomodoro";
		PomoCount.IsEnabled = PomoRadio.IsChecked == true;
		PomoCount.InputText.Opacity = (PomoCount.IsEnabled ? 1.0 : 0.4);
		SetIcon();
	}

	private void OnIconSelected(object sender, EventArgs e)
	{
		if (sender is SetHabitIconControl setHabitIconControl)
		{
			if (setHabitIconControl.IsIcon)
			{
				_timer.Icon = setHabitIconControl.SelectedIcon;
				_timer.Color = setHabitIconControl.IconColor;
			}
			else
			{
				_timer.Icon = "txt_" + setHabitIconControl.IconText;
				string text = setHabitIconControl.TextColor ?? "";
				if (_timer.Color != text)
				{
					_timer.Color = text;
					ColorSelector.TryAddClickEvent(text);
				}
			}
		}
		SetIconPopup.IsOpen = false;
		SetIcon();
	}

	private void HideIconPopup(object sender, EventArgs e)
	{
		SetIconPopup.IsOpen = false;
	}

	private void UpdateSaveButton()
	{
		SaveButton.IsEnabled = !string.IsNullOrEmpty(TitleText.Text) || !string.IsNullOrEmpty(_timer.ObjId);
	}

	public void OnCancel()
	{
		Close();
	}

	public async void Ok()
	{
		if (!SaveButton.IsEnabled)
		{
			return;
		}
		_timer.ModifiedTime = DateTime.Now;
		_timer.Type = ((TimingRadio.IsChecked == true) ? "timing" : "pomodoro");
		if (_timer.Type == "pomodoro" && int.TryParse(PomoCount.Text, out var result))
		{
			_timer.PomodoroTime = result;
		}
		_timer.Name = TitleText.Text;
		if (_isNew)
		{
			TimerModel timer = _timer;
			timer.SortOrder = await TimerService.GetNewTimerSortOrder();
			_timer.CreatedTime = DateTime.Now;
			if (_timer.ObjType == "habit")
			{
				await HabitService.SaveHabitIcon(_timer.ObjId, _timer.Icon, _timer.Color);
			}
			await BaseDao<TimerModel>.InsertAsync(_timer);
			UserActCollectUtils.AddClickEvent("timer", "add_timer", string.IsNullOrEmpty(_timer.ObjId) ? "enter_timer" : "select_task");
			UserActCollectUtils.AddClickEvent("timer", "add_timer", (_timer.Type == "pomodoro") ? "pomo" : "stopwatch");
		}
		else
		{
			_timer.ModifiedTime = DateTime.Now;
			await TimerService.UpdateTimer(_timer);
			if (_timer.ObjType == "habit")
			{
				await HabitService.SaveHabitIcon(_timer.ObjId, _timer.Icon, _timer.Color);
			}
			TickFocusManager.OnTimerChanged(_timer);
		}
		PomoNotifier.NotifyTimerChanged();
		SyncManager.Sync();
		Close();
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void OnSaveClick(object sender, RoutedEventArgs e)
	{
		Ok();
	}

	private static TimerModel GetDefaultTimer()
	{
		return new TimerModel
		{
			Id = Utils.GetGuid(),
			Icon = "habit_daily_check_in",
			Color = HabitUtils.IconInfos["habit_daily_check_in"].IconOutlineColor,
			UserId = LocalSettings.Settings.LoginUserId,
			PomodoroTime = LocalSettings.Settings.PomoDuration
		};
	}

	private void OnTitleChanged(object sender, EventArgs e)
	{
		_timer.Name = TitleText.Text;
		SetIconLocation();
		UpdateSaveButton();
	}

	private void OnClearClick(object sender, MouseButtonEventArgs e)
	{
		_timer.ObjId = null;
		_timer.ObjType = null;
		TitleText.Text = string.Empty;
	}

	private async void OnLinkClick(object sender, MouseButtonEventArgs e)
	{
		List<string> linkedIds = await TimerDao.GetObjIds();
		if (_pomoFilter == null)
		{
			EscPopup popup = new EscPopup
			{
				StaysOpen = false,
				PlacementTarget = TitleText,
				Placement = PlacementMode.Center,
				HorizontalOffset = -3.0,
				VerticalOffset = 208.0
			};
			_pomoFilter = new PomoFilterControl(popup, inFocus: false, showDetail: false, linkedIds, loadTimers: false)
			{
				ShowComplete = true,
				DataContext = new FocusViewModel()
			};
			_pomoFilter.Container.Width = 350.0;
			_pomoFilter.ItemSelected += delegate(object o, DisplayItemModel item)
			{
				_timer.ObjId = item.Id;
				_timer.ObjType = (item.IsHabit ? "habit" : "task");
				TitleText.Text = item.Title;
				if (item.IsHabit)
				{
					_timer.Icon = item.Habit.IconRes;
					_timer.Color = item.Habit.Color;
				}
				SetIcon();
			};
		}
		Popup parentPopup = _pomoFilter.GetParentPopup();
		parentPopup.PlacementTarget = TitleText;
		parentPopup.IsOpen = true;
	}

	public static async Task ShowAddTimerWindow(Window owner)
	{
		if (await ProChecker.CheckTimerLimit(owner))
		{
			AddTimerWindow addTimerWindow = new AddTimerWindow();
			addTimerWindow.Owner = owner;
			addTimerWindow.ShowDialog();
		}
	}

	private void OnPomoCheckChanged(object sender, RoutedEventArgs e)
	{
		PomoCount.IsEnabled = PomoRadio.IsChecked == true;
		PomoCount.InputText.Opacity = (PomoCount.IsEnabled ? 1.0 : 0.4);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/pomo/addtimerwindow.xaml", UriKind.Relative);
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
			((AddTimerWindow)target).Loaded += OnWindowLoaded;
			break;
		case 2:
			IconGrid = (Grid)target;
			IconGrid.MouseLeftButtonUp += OnSelectIconClick;
			break;
		case 3:
			IconImage = (Image)target;
			break;
		case 4:
			SetIconPopup = (EscPopup)target;
			break;
		case 5:
			SetIconControl = (SetHabitIconControl)target;
			break;
		case 6:
			TitleText = (EmojiEditor)target;
			break;
		case 7:
			LinkIcon = (HoverIconButton)target;
			break;
		case 8:
			ClearIcon = (Border)target;
			ClearIcon.MouseLeftButtonUp += OnClearClick;
			break;
		case 9:
			PomoRadio = (RadioButton)target;
			PomoRadio.Checked += OnPomoCheckChanged;
			PomoRadio.Unchecked += OnPomoCheckChanged;
			break;
		case 10:
			PomoCount = (NumInputTextBox)target;
			break;
		case 11:
			TimingRadio = (RadioButton)target;
			break;
		case 12:
			((Button)target).Click += OnCancelClick;
			break;
		case 13:
			SaveButton = (Button)target;
			SaveButton.Click += OnSaveClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
