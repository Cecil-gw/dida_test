using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using Microsoft.Win32;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Network;
using ticktick_WPF.Util.Twitter;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Habit;

public class HabitListControl : UserControl, IComponentConnector, IStyleConnector
{
	private static DateTime? _mainHabitSelectedDate;

	private List<HabitDayCheckModel> _daysFilterModels;

	public static readonly DependencyProperty IsArchiveProperty = DependencyProperty.Register("IsArchive", typeof(bool), typeof(HabitListControl), new PropertyMetadata(false));

	private Point _startDragPoint;

	private HabitItemBaseViewModel _dragItem;

	private int _currentDragIndex;

	private bool _isUpdating;

	private readonly object _refreshLock = new object();

	private bool _refreshLoopRunning;

	private bool _refreshRequested;

	private bool _pendingSetFilterStatus;

	private Task _refreshTask = Task.CompletedTask;

	private bool _currentDragIsOpen;

	private DateTime? _selectedDate;

	private double _dragItemWidth;

	private Point _dragStartPoint;

	private Popup _recordPopup;

	internal HabitListControl Root;

	internal StackPanel TitlePanel;

	internal TextBlock Title;

	internal HoverIconButton ListModeSwitch;

	internal HoverIconButton AddButton;

	internal HoverIconButton MoreButton;

	internal EscPopup MorePopup;

	internal ItemsControl DayFilter;

	internal StackPanel FilterPanel;

	internal TextBlock SelectedDateText;

	internal Grid ItemsGrid;

	internal ListView HabitItems;

	internal Popup DragPopup;

	internal HabitListItem PopupItem;

	internal Border PopupSectionBorder;

	internal HabitListSectionItem PopupSection;

	internal Grid EmptyGrid;

	internal EmptyWithTextView EmptyView;

	private bool _contentLoaded;

	private static ConcurrentDictionary<string, Dictionary<DateTime, HabitCheckInStatus>> HabitWeekCheckInStatus => HabitCheckinCache.CheckInStatusCache;

	private CustomMenuList moreMenu { get; set; }

	public bool IsArchive
	{
		get
		{
			return (bool)GetValue(IsArchiveProperty);
		}
		set
		{
			SetValue(IsArchiveProperty, value);
		}
	}

	public ObservableCollection<HabitItemBaseViewModel> Items => (HabitItems.ItemsSource as ObservableCollection<HabitItemBaseViewModel>) ?? new ObservableCollection<HabitItemBaseViewModel>();

	public bool IsUpdating => _isUpdating;

	public event EventHandler<string> HabitSelected;

	public event EventHandler<int> ItemsCountChanged;

	private void InitFilterModels()
	{
		_daysFilterModels = new List<HabitDayCheckModel>
		{
			new HabitDayCheckModel(new HabitCheckInModel
			{
				Goal = 1.0
			}, DateTime.Today.AddDays(-6.0), new HabitModel(), 0)
			{
				IsBooleanHabit = false
			},
			new HabitDayCheckModel(new HabitCheckInModel
			{
				Goal = 1.0
			}, DateTime.Today.AddDays(-5.0), new HabitModel(), 1)
			{
				IsBooleanHabit = false
			},
			new HabitDayCheckModel(new HabitCheckInModel
			{
				Goal = 1.0
			}, DateTime.Today.AddDays(-4.0), new HabitModel(), 2)
			{
				IsBooleanHabit = false
			},
			new HabitDayCheckModel(new HabitCheckInModel
			{
				Goal = 1.0
			}, DateTime.Today.AddDays(-3.0), new HabitModel(), 3)
			{
				IsBooleanHabit = false
			},
			new HabitDayCheckModel(new HabitCheckInModel
			{
				Goal = 1.0
			}, DateTime.Today.AddDays(-2.0), new HabitModel(), 4)
			{
				IsBooleanHabit = false
			},
			new HabitDayCheckModel(new HabitCheckInModel
			{
				Goal = 1.0
			}, DateTime.Today.AddDays(-1.0), new HabitModel(), 5)
			{
				IsBooleanHabit = false
			},
			new HabitDayCheckModel(new HabitCheckInModel
			{
				Goal = 1.0
			}, DateTime.Today, new HabitModel(), 6)
			{
				IsBooleanHabit = false
			}
		};
		DayFilter.ItemsSource = _daysFilterModels;
		foreach (HabitDayCheckModel daysFilterModel in _daysFilterModels)
		{
			DateTime date = daysFilterModel.Date;
			DateTime? selectedDate = _selectedDate;
			daysFilterModel.Selected = date == selectedDate;
		}
	}

	public HabitListControl(bool isMainHabit)
	{
		InitializeComponent();
		if (isMainHabit)
		{
			_selectedDate = _mainHabitSelectedDate ?? (LocalSettings.Settings.ExtraSettings.HabitDateSelected ? new DateTime?(DateTime.Today) : ((DateTime?)null));
		}
		else
		{
			_selectedDate = (LocalSettings.Settings.ExtraSettings.HabitDateSelected ? new DateTime?(DateTime.Today) : ((DateTime?)null));
		}
		UserActCollectUtils.AddClickEvent("habit", "view_show", LocalSettings.Settings.ExtraSettings.HabitDateSelected ? "with_date" : "no_date");
		UserActCollectUtils.AddClickEvent("habit", "view_style", (LocalSettings.Settings.HabitItemDisplayMode == 0) ? "list" : "card");
		InitFilterModels();
		HabitItems.ItemsSource = new ObservableCollection<HabitItemBaseViewModel>();
		base.Loaded += delegate
		{
			BindEvents();
		};
		base.Unloaded += delegate
		{
			UnbindEvents();
		};
		base.SizeChanged += delegate(object sender, SizeChangedEventArgs args)
		{
			EmptyGrid.Margin = new Thickness(0.0, args.NewSize.Height * 0.28, 0.0, 0.0);
		};
	}

	private void UnbindEvents()
	{
		DataChangedNotifier.HabitsChanged -= OnHabitsChanged;
		DataChangedNotifier.HabitSkip -= OnHabitCheckInChanged;
		DataChangedNotifier.HabitSectionChanged -= OnHabitsChanged;
		DataChangedNotifier.HabitCyclesChanged -= OnHabitCyclesChanged;
		TimeChangeNotifier.DayChanged -= OnDayChanged;
		DataChangedNotifier.ThemeModeChanged -= OnThemeChanged;
		HabitCheckinCache.HabitCheckInChanged -= OnHabitCheckInChanged;
		HabitCheckinCache.HabitsCheckInChanged -= OnHabitsChanged;
	}

	private void BindEvents()
	{
		DataChangedNotifier.HabitsChanged += OnHabitsChanged;
		DataChangedNotifier.HabitSkip += OnHabitCheckInChanged;
		DataChangedNotifier.HabitSectionChanged += OnHabitsChanged;
		DataChangedNotifier.HabitCyclesChanged += OnHabitCyclesChanged;
		TimeChangeNotifier.DayChanged += OnDayChanged;
		HabitCheckinCache.HabitCheckInChanged += OnHabitCheckInChanged;
		DataChangedNotifier.ThemeModeChanged += OnThemeChanged;
		HabitCheckinCache.HabitsCheckInChanged -= OnHabitsChanged;
	}

	private void OnThemeChanged(object sender, EventArgs e)
	{
		GetHabitModels();
	}

	private async void OnHabitCheckInChanged(object sender, string e)
	{
		base.Dispatcher.Invoke((Func<Task>)async delegate
		{
			HabitModel habitModel = await HabitDao.GetHabitById(e);
			if (habitModel != null)
			{
				HabitWeekCheckInStatus.TryRemove(habitModel.Id, out var _);
				await GetCheckInStatus(habitModel, _daysFilterModels[0].Date);
				if (Items?.FirstOrDefault((HabitItemBaseViewModel habit) => habit.Id == e) is HabitItemViewModel habitItemViewModel)
				{
					HabitWeekCheckInStatus.TryGetValue(e, out var value2);
					List<DateTime> checkIns = (from c in value2?.Values
						where !c.NeedCheck
						select c.Date).ToList();
					habitItemViewModel.SetCheckIns(checkIns);
					habitItemViewModel.UpdateTotalCheckDaysAndCurrentStreak();
				}
				DelayActionHandlerCenter.TryDoAction("habitloaddatefilter", async delegate
				{
					List<HabitModel> habits = await HabitService.GetHabitsbyStatus(0);
					base.Dispatcher.Invoke(delegate
					{
						SetDateFilterStatus(habits);
					});
				}, 30);
			}
		});
	}

	private void OnDayChanged(object sender, EventArgs e)
	{
		GetHabitModels();
	}

	public Task GetHabitModels(bool setFilterStatus = true)
	{
		lock (_refreshLock)
		{
			_refreshRequested = true;
			_pendingSetFilterStatus |= setFilterStatus;
			if (!_refreshLoopRunning)
			{
				_refreshLoopRunning = true;
				_refreshTask = RefreshHabitModels();
			}
			return _refreshTask;
		}
	}

	private async Task RefreshHabitModels()
	{
		while (true)
		{
			bool pendingSetFilterStatus;
			lock (_refreshLock)
			{
				pendingSetFilterStatus = _pendingSetFilterStatus;
				_pendingSetFilterStatus = false;
				_refreshRequested = false;
			}
			await ReloadHabitModels(pendingSetFilterStatus);
			lock (_refreshLock)
			{
				if (!_refreshRequested)
				{
					_refreshLoopRunning = false;
					break;
				}
			}
		}
	}

	private async Task ReloadHabitModels(bool setFilterStatus)
	{
		while (_isUpdating)
		{
			await Task.Delay(50);
		}
		_isUpdating = true;
		try
		{
			UpdateView();
			List<HabitSectionModel> list = ((!IsArchive) ? (await HabitService.GetHabitSections()) : new List<HabitSectionModel>());
			List<HabitSectionModel> source = list;
			List<HabitSectionModel> sortedSections = source.OrderBy((HabitSectionModel s) => s.SortOrder).ToList();
			List<HabitModel> habits = await HabitService.GetHabitsbyStatus(IsArchive ? 1 : 0);
			List<HabitModel> displayHabits = habits.ToList();
			await HabitCheckinCache.Init();
			if (setFilterStatus && !IsArchive)
			{
				await SetDateFilterStatus(habits);
			}
			if (_selectedDate.HasValue && !IsArchive)
			{
				displayHabits = await GetSelectDateHabits(displayHabits);
			}
			List<HabitItemBaseViewModel> list2 = new List<HabitItemBaseViewModel>();
			string text = Utils.FindParent<HabitContainer>(this)?.GetSelectedHabitId();
			foreach (HabitSectionModel section in sortedSections)
			{
				List<HabitModel> list3 = (from s in displayHabits
					where s.SectionId == section.Id
					orderby s.SortOrder
					select s).ToList();
				if (list3.Count <= 0)
				{
					continue;
				}
				HabitSectionListViewModel habitSectionListViewModel = new HabitSectionListViewModel(section)
				{
					Num = 0,
					Archived = IsArchive
				};
				list2.Add(habitSectionListViewModel);
				foreach (HabitModel item in list3)
				{
					habitSectionListViewModel.Num++;
					HabitItemViewModel habitItemViewModel = new HabitItemViewModel(item, _selectedDate)
					{
						Selected = (item.Id == text)
					};
					if (!habitItemViewModel.Archived)
					{
						HabitWeekCheckInStatus.TryGetValue(item.Id, out var value);
						List<DateTime> checkIns = (from c in value?.Values
							where !c.NeedCheck
							select c.Date).ToList();
						habitItemViewModel.SetCheckIns(checkIns);
					}
					habitSectionListViewModel.Children.Add(habitItemViewModel);
					if (section.IsOpen)
					{
						list2.Add(habitItemViewModel);
					}
				}
				displayHabits.RemoveAll((HabitModel s) => s.SectionId == section.Id);
			}
			HabitSectionListViewModel defaultViewModel = new HabitSectionListViewModel(HabitSectionModel.GetDefault())
			{
				Num = displayHabits.Count,
				Archived = IsArchive,
				IsOther = true
			};
			List<HabitSectionListViewModel> source2 = list2.OfType<HabitSectionListViewModel>().ToList();
			HabitSectionListViewModel habitSectionListViewModel2 = source2.LastOrDefault((HabitSectionListViewModel sectionView) => sectionView.SortOrder < defaultViewModel.SortOrder);
			int num = 0;
			if (habitSectionListViewModel2 != null && habitSectionListViewModel2.IsOpen)
			{
				num = habitSectionListViewModel2.Num;
			}
			int num2 = list2.IndexOf(habitSectionListViewModel2) + 1 + num;
			List<HabitItemBaseViewModel> list4 = new List<HabitItemBaseViewModel>();
			if (num2 >= 0 && source2.Any() && displayHabits.Any())
			{
				num2 = Math.Max(num2, 0);
				list4.Add(defaultViewModel);
			}
			else
			{
				num2 = 0;
				HabitSectionModel.GetDefault().IsOpen = true;
			}
			bool isOpen = HabitSectionModel.GetDefault().IsOpen;
			foreach (HabitModel item2 in displayHabits.OrderBy((HabitModel h) => h.SortOrder))
			{
				item2.SectionId = HabitSectionModel.GetDefault().Id;
				HabitItemViewModel habitItemViewModel2 = new HabitItemViewModel(item2, _selectedDate)
				{
					Selected = (item2.Id == text)
				};
				if (!habitItemViewModel2.Archived)
				{
					HabitWeekCheckInStatus.TryGetValue(item2.Id, out var value2);
					List<DateTime> checkIns2 = (from c in value2?.Values
						where !c.NeedCheck
						select c.Date).ToList();
					habitItemViewModel2.SetCheckIns(checkIns2);
				}
				if (isOpen)
				{
					list4.Add(habitItemViewModel2);
				}
				defaultViewModel.Children.Add(habitItemViewModel2);
			}
			list2.InsertRange(num2, list4);
			if (list2.Count > 0)
			{
				list2[0].IsFirstOne = true;
			}
			SetItems(list2);
			EmptyGrid.Visibility = ((list2.Count > 0) ? Visibility.Collapsed : Visibility.Visible);
			SetEmptyContent();
			this.ItemsCountChanged?.Invoke(this, list2.Count);
		}
		catch (Exception)
		{
		}
		finally
		{
			_isUpdating = false;
		}
	}

	private void SetEmptyContent()
	{
		EmptyView.SetData(Utils.GetString(IsArchive ? "HabitArchiveListEmptyText1" : ((!_selectedDate.HasValue) ? "HabitListEmptyText1" : "NoHabitInDate")), Utils.GetString(IsArchive ? "HabitArchiveListEmptyText2" : "NoHabitDesc"), new EmptyIconsSourceModel(IsArchive ? "HabitListArchiveEmpty" : "HabitListEmpty", IsArchive ? new List<int> { 4, 5 } : new List<int> { 1, 2, 3, 4 }), !IsArchive);
	}

	private void UpdateView()
	{
		Title.Text = Utils.GetString(IsArchive ? "ArchivedHabit" : "Habit");
		FilterPanel.Visibility = ((!_selectedDate.HasValue || IsArchive) ? Visibility.Collapsed : Visibility.Visible);
		DayFilter.Visibility = (IsArchive ? Visibility.Collapsed : Visibility.Visible);
		ListModeSwitch.Visibility = (IsArchive ? Visibility.Collapsed : Visibility.Visible);
		AddButton.Visibility = (IsArchive ? Visibility.Collapsed : Visibility.Visible);
		if (_selectedDate.HasValue && !IsArchive)
		{
			SelectedDateText.Text = ticktick_WPF.Util.DateUtils.FormatMonthDay(_selectedDate.Value, DateFormatLen.Middle);
		}
	}

	private async Task<List<HabitModel>> GetSelectDateHabits(List<HabitModel> habits)
	{
		List<HabitModel> displayHabits = habits.ToList();
		if (_selectedDate.HasValue)
		{
			foreach (HabitModel habit in habits)
			{
				if (habit.IsSkip(_selectedDate.Value))
				{
					displayHabits.Remove(habit);
					continue;
				}
				await GetCheckInStatus(habit, _daysFilterModels[0].Date);
				if (HabitWeekCheckInStatus.TryGetValue(habit.Id, out var value) && value.TryGetValue(_selectedDate.Value, out var value2))
				{
					if (!value2.NeedCheck && !value2.HaveCheckIn)
					{
						displayHabits.Remove(habit);
					}
				}
				else
				{
					displayHabits.Remove(habit);
				}
			}
		}
		return displayHabits;
	}

	private async Task SetDateFilterStatus(List<HabitModel> habits)
	{
		if (IsArchive)
		{
			return;
		}
		if (_daysFilterModels?.LastOrDefault()?.Date != DateTime.Today)
		{
			InitFilterModels();
			HabitWeekCheckInStatus.Clear();
		}
		foreach (HabitModel habit in habits)
		{
			await GetCheckInStatus(habit, _daysFilterModels[0].Date);
		}
		for (int i = 0; i < 7; i++)
		{
			DateTime key = DateTime.Today.AddDays(-6 + i);
			HabitDayCheckModel habitDayCheckModel = _daysFilterModels[i];
			int num = 0;
			double num2 = 0.0;
			int num3 = 0;
			foreach (Dictionary<DateTime, HabitCheckInStatus> item in HabitWeekCheckInStatus.Values.ToList())
			{
				if (!item.TryGetValue(key, out var value) || value.Skip)
				{
					continue;
				}
				if (value.NeedCheck || value.Status == 2 || (value.Status != 1 && value.Percent > 0.0) || value.Status == 1)
				{
					num++;
				}
				switch (value.Status)
				{
				case 2:
					num2 += 1.0;
					continue;
				case 1:
					if (value.NeedCheck)
					{
						num3++;
						continue;
					}
					break;
				}
				num2 += Math.Min(1.0, value.Percent);
			}
			habitDayCheckModel.NeedCheck = num > 0;
			if (num > 0)
			{
				habitDayCheckModel.CheckInModel.Goal = num;
				habitDayCheckModel.CheckInModel.Value = num2;
				habitDayCheckModel.Percent = Math.Min((int)(num2 / (double)num * 100.0), 100);
				habitDayCheckModel.Status = ((Math.Abs(num2 - (double)num) < 0.01) ? 2 : ((num3 == num) ? 1 : 0));
			}
			else
			{
				habitDayCheckModel.CheckInModel.Value = 0.0;
				habitDayCheckModel.Percent = 0.0;
				habitDayCheckModel.Status = 0;
			}
		}
	}

	public static async Task GetCheckInStatus(HabitModel habit, DateTime beginDate, bool moveStartBefore = false)
	{
		if (HabitWeekCheckInStatus.ContainsKey(habit.Id))
		{
			return;
		}
		HabitRepeatInfo repeatInfo = HabitUtils.BuildHabitRepeatInfo(habit.RepeatRule, habit.ExDates);
		DateTime firstCheckDay = habit.GetStartDate().Date;
		List<HabitCheckInModel> list = await HabitCheckinCache.GetHabitCheckInsById(habit.Id);
		List<(DateTime, bool?)> list2 = new List<(DateTime, bool?)>();
		Dictionary<DateTime, HabitCheckInStatus> dictionary = new Dictionary<DateTime, HabitCheckInStatus>();
		if (list != null)
		{
			foreach (HabitCheckInModel item in list)
			{
				if (!DateTime.TryParseExact(item.CheckinStamp, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
				{
					continue;
				}
				if (item.IsComplete() || item.IsUnComplete() || item.Value > 0.0)
				{
					if (moveStartBefore && result.Date < firstCheckDay)
					{
						firstCheckDay = result.Date;
					}
					list2.Add((result, item.IsComplete() ? new bool?(true) : (item.IsUnComplete() ? new bool?(false) : ((bool?)null))));
				}
				if (result >= DateTime.Today.AddDays(-6.0))
				{
					dictionary[result] = new HabitCheckInStatus
					{
						Date = result,
						HaveCheckIn = (item.Value > 0.0 || item.IsComplete() || item.IsUnComplete()),
						Status = (item.IsComplete() ? 2 : (item.IsUnComplete() ? 1 : 0)),
						Percent = ((item.Goal > 0.0) ? (item.GetDisplayValue() / item.Goal) : 0.0)
					};
				}
			}
		}
		for (int i = 0; i < 7; i++)
		{
			DateTime dateTime = beginDate.AddDays(i);
			if (!dictionary.TryGetValue(dateTime, out var value))
			{
				HabitCheckInStatus obj = new HabitCheckInStatus
				{
					Date = dateTime,
					HaveCheckIn = false,
					Status = 0
				};
				value = (dictionary[dateTime] = obj);
			}
			bool needCheck = dateTime.Date >= firstCheckDay && HabitUtils.InitShowHint(repeatInfo, dateTime.Date, firstCheckDay, list2, checkCurrentWeek: true);
			value.NeedCheck = needCheck;
			if (habit.IsSkip(dateTime))
			{
				value.Skip = true;
			}
		}
		HabitWeekCheckInStatus[habit.Id] = dictionary;
	}

	public async void CheckInHabit(string habitId)
	{
		while (_isUpdating)
		{
			await Task.Delay(10);
		}
		_isUpdating = true;
		try
		{
			if (Items?.FirstOrDefault((HabitItemBaseViewModel item) => item.Id == habitId) is HabitItemViewModel habitItemViewModel)
			{
				HabitDayCheckModel dayCheck = habitItemViewModel.WeekCheckIns.FirstOrDefault((HabitDayCheckModel item) => item.Date == DateTime.Today);
				ManuallyRecordCheckIn(dayCheck, null);
			}
		}
		catch (Exception)
		{
		}
		finally
		{
			_isUpdating = false;
		}
	}

	public async Task ManuallyRecordCheckIn(HabitDayCheckModel dayCheck, UIElement element)
	{
		if (dayCheck == null)
		{
			return;
		}
		dayCheck.Selected = true;
		HabitModel habit = await HabitDao.GetHabitById(dayCheck.HabitId);
		if (habit == null)
		{
			return;
		}
		EscPopup popup = new EscPopup
		{
			PlacementTarget = element,
			Placement = PlacementMode.Top,
			StaysOpen = false,
			HorizontalOffset = -60.0,
			VerticalOffset = 0.0,
			PopupAnimation = PopupAnimation.Fade
		};
		ManualRecordCheckinControl manual = new ManualRecordCheckinControl();
		popup.Child = manual;
		manual.Init(habit.Unit);
		popup.Closed += delegate
		{
			dayCheck.Selected = false;
		};
		manual.Save += async delegate(object sender, double step)
		{
			if (step > 0.0)
			{
				await CheckInRealHabit(dayCheck, habit, step, element, HabitAction.ManualRecord);
			}
		};
		manual.Save += delegate
		{
			popup.IsOpen = false;
		};
		manual.Cancel += delegate
		{
			popup.IsOpen = false;
		};
		await Task.Delay(10);
		popup.IsOpen = true;
		Utils.Focus(manual.CheckInText);
		Keyboard.Focus(manual.CheckInText);
		HwndHelper.SetFocus(popup);
	}

	public async Task CheckInRealHabit(HabitDayCheckModel dayCheck, HabitModel habit, double step, UIElement placement, HabitAction action)
	{
		HabitCheckInModel habitCheckInModel = await HabitService.CheckInHabit(habit.Id, dayCheck.Date, step, increment: true, checkShowRecord: true, 0, null, this, action);
		if (habitCheckInModel == null)
		{
			return;
		}
		Utils.PlayCompletionSound();
		dayCheck.CheckInModel.Goal = habitCheckInModel.Goal;
		dayCheck.CheckInModel.Value = Math.Min(habitCheckInModel.GetDisplayValue(), habitCheckInModel.Goal);
		dayCheck.Status = habitCheckInModel.CheckStatus;
		dayCheck.Percent = Math.Min((int)(dayCheck.CheckInModel.Value / dayCheck.CheckInModel.Goal * 100.0), 100);
		if (dayCheck.Percent >= 100.0)
		{
			dayCheck.Status = 2;
		}
		if (step > 0.0)
		{
			string toastText = "+" + step + " " + HabitUtils.GetUnitText(habit.Unit);
			if (placement != null && dayCheck.CheckInModel.Value >= dayCheck.CheckInModel.Goal)
			{
				toastText = Utils.GetString("Done");
			}
			ShowAddStepPop(placement, toastText);
		}
		else if (Math.Abs(step) <= 0.0 && placement != null)
		{
			ShowAddStepPop(placement, Utils.GetString("Done"));
		}
		await Task.Delay(150);
	}

	public void StartDragSection(HabitSectionListViewModel model)
	{
		model.Dragging = true;
		if (model.IsOpen)
		{
			List<HabitItemBaseViewModel> items = Items.Where((HabitItemBaseViewModel h) => !(h is HabitItemViewModel habitItemViewModel) || !(habitItemViewModel.Habit.SectionId == model.Id)).ToList();
			SetItems(items);
			_currentDragIsOpen = model.IsOpen;
			model.IsOpen = false;
		}
	}

	private void SetItems(List<HabitItemBaseViewModel> models)
	{
		ItemsSourceHelper.SetItemsSource(HabitItems, models);
	}

	private void OnHabitsChanged(object sender, EventArgs e)
	{
		base.Dispatcher.Invoke(delegate
		{
			HabitWeekCheckInStatus.Clear();
			if (base.Visibility == Visibility.Visible)
			{
				GetHabitModels();
			}
		});
	}

	private async void OnAddHabitClick(object sender, MouseButtonEventArgs e)
	{
		if (!(await HabitUtils.CheckHabitLimit()))
		{
			return;
		}
		try
		{
			AddOrEditHabitDialog addOrEditHabitDialog = new AddOrEditHabitDialog();
			addOrEditHabitDialog.Owner = Window.GetWindow(this);
			addOrEditHabitDialog.ShowDialog();
		}
		catch (Exception ex)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
		}
	}

	private void OnMoreClick(object sender, MouseButtonEventArgs e)
	{
		if (moreMenu == null)
		{
			List<CustomMenuItemViewModel> types = new List<CustomMenuItemViewModel>
			{
				new CustomMenuItemViewModel("setting", Utils.GetString("HabitSetting"), Utils.GetGeometry("Ic.Tabbar.SettingLinear")),
				new CustomMenuItemViewModel("export", Utils.GetString("Export"), Utils.GetGeometry("Ic.Habit.Share"))
			};
			moreMenu = new CustomMenuList(types, MorePopup);
			moreMenu.Operated += OnMoreItemSelected;
			moreMenu.Closed += MoreMenu_Closed;
			moreMenu.Show();
		}
	}

	private void MoreMenu_Closed(object sender, EventArgs e)
	{
		moreMenu.Operated -= OnMoreItemSelected;
		moreMenu.Closed -= MoreMenu_Closed;
		moreMenu = null;
	}

	public void Toast(string text)
	{
		Utils.FindParent<IToastShowWindow>(this)?.TryToastString(null, text);
	}

	private async void OnMoreItemSelected(object sender, object e)
	{
		string text = e as string;
		if (!(text == "setting"))
		{
			if (!(text == "export"))
			{
				return;
			}
			try
			{
				UserActCollectUtils.AddClickEvent("habit", "export", "habit_export");
				List<string> exportTimes = LocalSettings.Settings.ExtraSettings.EpT?.Split(',').ToList() ?? new List<string>();
				string today = ticktick_WPF.Util.DateUtils.GetDateNum(DateTime.Now.ToUniversalTime()).ToString();
				if (exportTimes.Count((string t) => t == today) >= 3)
				{
					Toast(Utils.GetString("ExportTooManyTimes"));
					return;
				}
				await Task.Delay(250);
				SaveFileDialog saveFileDialog = new SaveFileDialog();
				saveFileDialog.Title = "ExportHabit";
				saveFileDialog.RestoreDirectory = true;
				saveFileDialog.FileName = Utils.GetString("Habits") + "_" + ticktick_WPF.Util.DateUtils.GetDateNum(DateTime.Today);
				saveFileDialog.Filter = "Excel files (*.xlsx)|*.xlsx";
				if (saveFileDialog.ShowDialog() != true)
				{
					return;
				}
				string text2 = await ApiClient.GetFile(BaseUrl.GetApiDomain() + "/api/v2/data/export/habits", saveFileDialog.FileName);
				if (!string.IsNullOrEmpty(text2))
				{
					if (text2.Contains("export_too_many_times"))
					{
						Toast(Utils.GetString("ExportTooManyTimes"));
						LocalSettings.Settings.ExtraSettings.EpT = today + "," + today + "," + today;
						LocalSettings.Settings.Save(force: true);
					}
					else
					{
						Toast(Utils.GetString("ExportFailed"));
					}
				}
				else
				{
					if (exportTimes.Count >= 3)
					{
						exportTimes.RemoveAt(0);
					}
					exportTimes.Add(today);
					LocalSettings.Settings.ExtraSettings.EpT = exportTimes.Join(",");
					LocalSettings.Settings.Save(force: true);
					Toast(Utils.GetString("ExportSuccess"));
				}
				return;
			}
			catch (Exception)
			{
				Toast(Utils.GetString("ExportFailed"));
				return;
			}
		}
		HabitSettingWindow habitSettingWindow = new HabitSettingWindow();
		habitSettingWindow.Owner = Window.GetWindow(this);
		habitSettingWindow.ShowDialog();
	}

	private async void SwitchArchiveHabit(bool archive)
	{
		if (archive)
		{
			EmptyGrid.Visibility = Visibility.Collapsed;
			IsArchive = true;
			this.HabitSelected?.Invoke(this, null);
			GetHabitModels();
		}
		else
		{
			EmptyGrid.Visibility = Visibility.Collapsed;
			IsArchive = false;
			this.HabitSelected?.Invoke(this, null);
			GetHabitModels();
		}
	}

	public async void ShowAddStepPop(UIElement sender, string toastText)
	{
		if (_recordPopup != null)
		{
			_recordPopup.BeginAnimation(Popup.VerticalOffsetProperty, null);
			_recordPopup.IsOpen = false;
			_recordPopup = null;
		}
		if (sender != null)
		{
			Popup popup = new Popup
			{
				Placement = PlacementMode.Center,
				VerticalOffset = -30.0,
				StaysOpen = true,
				AllowsTransparency = true,
				Opacity = 0.0
			};
			popup.PlacementTarget = sender;
			StackPanel stackPanel = new StackPanel();
			Border border = new Border
			{
				Height = 30.0,
				CornerRadius = new CornerRadius(4.0),
				Effect = new DropShadowEffect
				{
					BlurRadius = 10.0,
					Direction = 300.0,
					ShadowDepth = 2.0,
					Opacity = 0.2
				}
			};
			border.SetResourceReference(Control.BackgroundProperty, "PrimaryColor");
			border.Child = new TextBlock
			{
				FontSize = 13.0,
				Foreground = Brushes.White,
				Opacity = 0.8,
				VerticalAlignment = VerticalAlignment.Center,
				TextTrimming = TextTrimming.CharacterEllipsis,
				HorizontalAlignment = HorizontalAlignment.Center,
				Margin = new Thickness(8.0, 0.0, 8.0, 0.0),
				Text = toastText
			};
			stackPanel.Children.Add(border);
			Path path = UiUtils.CreatePath("IcHabitAddStep", "PrimaryColor", "");
			path.Stretch = Stretch.None;
			path.HorizontalAlignment = HorizontalAlignment.Center;
			path.Margin = new Thickness(0.0, -2.0, 0.0, 0.0);
			stackPanel.Children.Add(path);
			popup.Child = stackPanel;
			popup.IsOpen = true;
			_recordPopup = popup;
			BeginRecordAnimation(popup);
		}
	}

	private async void BeginRecordAnimation(Popup popup)
	{
		Storyboard storyboard = new Storyboard();
		popup.BeginAnimation(UIElement.OpacityProperty, AnimationUtils.GetDoubleAnimation(0.0, 1.0, 100));
		DoubleAnimation doubleAnimation = AnimationUtils.GetDoubleAnimation(1.0, 0.0, 800);
		doubleAnimation.BeginTime = TimeSpan.FromMilliseconds(600.0);
		DoubleAnimation doubleAnimation2 = AnimationUtils.GetDoubleAnimation(popup.VerticalOffset, popup.VerticalOffset - 50.0, 1000);
		doubleAnimation2.BeginTime = TimeSpan.FromMilliseconds(300.0);
		storyboard.Children.Add(doubleAnimation);
		storyboard.Children.Add(doubleAnimation2);
		Storyboard.SetTarget(doubleAnimation, popup.Child);
		Storyboard.SetTarget(doubleAnimation2, popup);
		Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath(UIElement.OpacityProperty));
		Storyboard.SetTargetProperty(doubleAnimation2, new PropertyPath(Popup.VerticalOffsetProperty));
		storyboard.Completed += delegate
		{
			popup.IsOpen = false;
			_recordPopup = null;
		};
		storyboard.Begin();
	}

	public void OnItemSelected(string habitId)
	{
		foreach (HabitItemBaseViewModel item in Items)
		{
			item.Selected = item.Id == habitId;
		}
		this.HabitSelected?.Invoke(this, habitId);
	}

	public void TryDragItem(object sender, MouseEventArgs e)
	{
		if (IsArchive)
		{
			return;
		}
		_startDragPoint = e.GetPosition(ItemsGrid);
		_dragItem = null;
		_currentDragIndex = -1;
		if (sender is HabitListSectionItem habitListSectionItem)
		{
			if (habitListSectionItem.DataContext is HabitSectionListViewModel habitSectionListViewModel)
			{
				_dragItem = habitSectionListViewModel;
				_currentDragIndex = Items.IndexOf(habitSectionListViewModel);
				_dragStartPoint = new Point(10.0, 10.0);
			}
			return;
		}
		Point position = e.GetPosition(HabitItems);
		ListViewItem mousePositionItem = GetMousePositionItem(position);
		if (mousePositionItem?.DataContext is HabitItemViewModel habitItemViewModel)
		{
			_dragItem = habitItemViewModel;
			_dragItemWidth = mousePositionItem.ActualWidth;
			_dragStartPoint = e.GetPosition(mousePositionItem);
			_currentDragIndex = Items.IndexOf(habitItemViewModel);
		}
	}

	private ListViewItem GetMousePositionItem(Point pos)
	{
		HitTestResult hitTestResult = VisualTreeHelper.HitTest(ItemsGrid, pos);
		if (hitTestResult != null)
		{
			return Utils.FindParent<ListViewItem>(hitTestResult.VisualHit);
		}
		return null;
	}

	private void OnDragMove(object sender, MouseEventArgs e)
	{
		if (_dragItem == null)
		{
			return;
		}
		Point position = e.GetPosition(ItemsGrid);
		if (DragPopup.IsOpen)
		{
			if (_dragItem is HabitSectionListViewModel)
			{
				DragPopup.HorizontalOffset = position.X - _dragStartPoint.X;
				DragPopup.VerticalOffset = position.Y - _dragStartPoint.Y;
			}
			else
			{
				DragPopup.HorizontalOffset = position.X - _dragStartPoint.X;
				DragPopup.VerticalOffset = position.Y - _dragStartPoint.Y;
			}
			ListViewItem mousePositionItem = GetMousePositionItem(position);
			if (mousePositionItem == null && !_dragItem.IsSection && LocalSettings.Settings.HabitItemDisplayMode == 1)
			{
				if (position.X < 200.0)
				{
					return;
				}
				mousePositionItem = GetMousePositionItem(new Point(100.0, position.Y));
				if (mousePositionItem == null)
				{
					return;
				}
				double actualWidth = mousePositionItem.ActualWidth;
				for (double num = (double)(int)(position.X / actualWidth) * actualWidth + 120.0; num > actualWidth; num -= actualWidth)
				{
					ListViewItem mousePositionItem2 = GetMousePositionItem(new Point(num - actualWidth, position.Y));
					if (mousePositionItem2 != null)
					{
						mousePositionItem = mousePositionItem2;
						break;
					}
				}
				if (mousePositionItem.DataContext is HabitItemBaseViewModel habitItemBaseViewModel && !_dragItem.Equals(habitItemBaseViewModel) && !habitItemBaseViewModel.IsSection)
				{
					List<HabitItemBaseViewModel> list = Items.ToList();
					list.Remove(_dragItem);
					int num2 = list.IndexOf(habitItemBaseViewModel);
					list.Insert(num2 + 1, _dragItem);
					try
					{
						SetItems(list);
					}
					catch (Exception)
					{
					}
					_currentDragIndex = num2 + 1;
				}
			}
			else
			{
				if (!(mousePositionItem?.DataContext is HabitItemBaseViewModel habitItemBaseViewModel2) || (!_dragItem.IsSection && habitItemBaseViewModel2.IsSection))
				{
					return;
				}
				int num3 = Items.IndexOf(habitItemBaseViewModel2);
				if (_currentDragIndex < 0 || num3 == _currentDragIndex)
				{
					return;
				}
				int num4 = -1;
				if (_currentDragIndex < num3)
				{
					num4 = num3;
					HabitItemBaseViewModel habitItemBaseViewModel3 = ((num4 + 1 < Items.Count) ? Items[num4 + 1] : null);
					if (_dragItem.IsSection && habitItemBaseViewModel3 != null && !habitItemBaseViewModel3.IsSection)
					{
						return;
					}
				}
				else if (_currentDragIndex > num3)
				{
					num4 = num3;
					if (_dragItem.IsSection && !habitItemBaseViewModel2.IsSection)
					{
						return;
					}
				}
				if (num4 != _currentDragIndex && num4 >= 0 && num4 < Items.Count)
				{
					List<HabitItemBaseViewModel> list2 = Items.ToList();
					list2.Remove(_dragItem);
					list2.Insert(Math.Min(list2.Count, num4), _dragItem);
					try
					{
						SetItems(list2);
					}
					catch (Exception)
					{
					}
					_currentDragIndex = num4;
				}
			}
		}
		else if (_startDragPoint != default(Point) && _dragItem != null)
		{
			if (_dragItem is HabitItemViewModel habitItemViewModel && Math.Abs((position - _startDragPoint).Length) > 5.0)
			{
				habitItemViewModel.Dragging = true;
				ShowDragPop(habitItemViewModel, position);
			}
			if (_dragItem is HabitSectionListViewModel model && Math.Abs((position - _startDragPoint).Length) > 5.0)
			{
				StartDragSection(model);
				ShowDragPop(model, position);
			}
		}
	}

	private void ShowDragPop(HabitSectionListViewModel model, Point position)
	{
		PopupSection.IsEnabled = false;
		PopupSection.DataContext = new HabitSectionListViewModel(model.Section)
		{
			Num = model.Num
		};
		PopupItem.Visibility = Visibility.Collapsed;
		PopupSectionBorder.Visibility = Visibility.Visible;
		DragPopup.HorizontalOffset = position.X - 1.0 * ((ItemsGrid.ActualWidth > 400.0) ? 200.0 : (ItemsGrid.ActualWidth / 2.0));
		DragPopup.VerticalOffset = position.Y - 22.0;
		DragPopup.IsOpen = true;
	}

	private void ShowDragPop(HabitItemViewModel model, Point position)
	{
		PopupItem.IsEnabled = false;
		PopupItem.DataContext = model.Copy();
		PopupItem.Visibility = Visibility.Visible;
		PopupItem.Width = _dragItemWidth;
		PopupSectionBorder.Visibility = Visibility.Collapsed;
		DragPopup.HorizontalOffset = position.X - 1.0 * ((ItemsGrid.ActualWidth > 400.0) ? 200.0 : (ItemsGrid.ActualWidth / 2.0));
		DragPopup.VerticalOffset = position.Y - 22.0;
		DragPopup.IsOpen = true;
	}

	private void OnDrop(object sender, MouseButtonEventArgs e)
	{
		if (!DragPopup.IsOpen && _dragItem != null)
		{
			_dragItem.Dragging = false;
			_dragItem = null;
		}
		ItemsGrid.ReleaseMouseCapture();
		if (_dragItem is HabitItemViewModel { SortOrder: var num } habitItemViewModel)
		{
			HabitItemBaseViewModel obj = ((_currentDragIndex > 0) ? Items[_currentDragIndex - 1] : null);
			HabitItemBaseViewModel habitItemBaseViewModel = ((_currentDragIndex < Items.Count() - 1) ? Items[_currentDragIndex + 1] : null);
			HabitItemViewModel habitItemViewModel2 = obj as HabitItemViewModel;
			HabitSectionListViewModel habitSectionListViewModel = obj as HabitSectionListViewModel;
			long? num2 = habitItemViewModel2?.SortOrder;
			long? num3 = ((habitItemBaseViewModel is HabitItemViewModel habitItemViewModel3) ? new long?(habitItemViewModel3.SortOrder) : ((long?)null));
			if (num2.HasValue || num3.HasValue)
			{
				num = ((!num2.HasValue) ? (num3.Value - 268435456) : (num3.HasValue ? (num2.Value + (num3.Value - num2.Value) / 2) : (num2.Value + 268435456)));
			}
			string text = habitItemViewModel.Habit.SectionId;
			if (habitItemViewModel2 != null)
			{
				text = habitItemViewModel2.Habit.SectionId;
			}
			else if (habitSectionListViewModel != null)
			{
				text = habitSectionListViewModel.Id;
			}
			if (num != habitItemViewModel.SortOrder || text != habitItemViewModel.Habit.SectionId)
			{
				habitItemViewModel.Habit.SectionId = text;
				habitItemViewModel.Habit.SortOrder = num;
				HabitDao.SaveSortOrderAndSection(habitItemViewModel.Habit.Id, num, text);
			}
		}
		else if (_dragItem is HabitSectionListViewModel habitSectionListViewModel2)
		{
			List<HabitSectionListViewModel> list = Items.OfType<HabitSectionListViewModel>().ToList();
			int num4 = list.IndexOf(habitSectionListViewModel2);
			long? front = ((num4 > 0) ? new long?(list[num4 - 1].SortOrder) : ((long?)null));
			long? next = ((num4 < list.Count - 1) ? new long?(list[num4 + 1].SortOrder) : ((long?)null));
			long num5 = GetSortOrderBetween(front, next) ?? habitSectionListViewModel2.SortOrder;
			habitSectionListViewModel2.IsOpen = _currentDragIsOpen;
			if (num5 != habitSectionListViewModel2.SortOrder)
			{
				habitSectionListViewModel2.Section.SortOrder = num5;
				habitSectionListViewModel2.SortOrder = num5;
				HabitService.UpdateSectionSortOrderById(habitSectionListViewModel2.Id, num5);
			}
			else
			{
				GetHabitModels();
			}
		}
		if (_dragItem != null)
		{
			_dragItem.Dragging = false;
			_dragItem = null;
		}
		_startDragPoint = default(Point);
		_currentDragIndex = -1;
		DragPopup.IsOpen = false;
	}

	private static long? GetSortOrderBetween(long? front, long? next)
	{
		if (front.HasValue || next.HasValue)
		{
			if (!front.HasValue)
			{
				return next.Value - 268435456;
			}
			if (!next.HasValue)
			{
				return front.Value + 268435456;
			}
			return front.Value + (next.Value - front.Value) / 2;
		}
		return null;
	}

	public void RemoveItem(string habitId)
	{
		HabitItemBaseViewModel habitItemBaseViewModel = Items?.FirstOrDefault((HabitItemBaseViewModel it) => it.Id == habitId);
		if (habitItemBaseViewModel != null)
		{
			Items?.Remove(habitItemBaseViewModel);
			this.HabitSelected?.Invoke(this, null);
		}
	}

	private async void OnHabitCyclesChanged(object sender, string e)
	{
		HabitItemBaseViewModel habitItemBaseViewModel = Items?.FirstOrDefault((HabitItemBaseViewModel h) => h.Id == e);
		if (habitItemBaseViewModel is HabitItemViewModel model)
		{
			HabitModel habitModel = await HabitDao.GetHabitById(model.Habit.Id);
			if (habitModel != null)
			{
				model.SetCyclesList(habitModel.CompletedCyclesList);
			}
		}
	}

	public void ClearData()
	{
		HabitItems.ItemsSource = null;
	}

	public void OnSectionOpenChanged(HabitSectionListViewModel section)
	{
		List<HabitItemBaseViewModel> list = Items.ToList();
		if (!section.IsOpen)
		{
			list.RemoveAll((HabitItemBaseViewModel model) => model is HabitItemViewModel habitItemViewModel && habitItemViewModel.Habit.SectionId == section.Id);
		}
		else
		{
			int num = list.IndexOf(section);
			if (num >= 0)
			{
				list.InsertRange(num + 1, section.Children);
			}
		}
		SetItems(list);
	}

	public async Task TrySelectItem(string habitId)
	{
		int time = 0;
		while (Items.Count == 0 && time < 30)
		{
			await Task.Delay(50);
			time++;
		}
		foreach (HabitItemBaseViewModel item in Items.ToList())
		{
			if (item.Selected && item.Id != habitId)
			{
				item.Selected = false;
			}
			else if (!item.Selected && item.Id == habitId)
			{
				item.Selected = true;
				HabitItems.ScrollIntoView(item);
			}
		}
	}

	private void OnArchiveToggleClick(object sender, MouseButtonEventArgs e)
	{
		EscPopup popup = new EscPopup
		{
			StaysOpen = false,
			PlacementTarget = TitlePanel,
			VerticalOffset = -8.0,
			Placement = PlacementMode.Bottom,
			HorizontalOffset = -8.0
		};
		CustomMenuList customMenuList = new CustomMenuList(new List<CustomMenuItemViewModel>
		{
			new CustomMenuItemViewModel((object)"Keeping", Utils.GetString("Keeping"), (Geometry)null, "")
			{
				Selected = !IsArchive
			},
			new CustomMenuItemViewModel((object)"Archived", Utils.GetString("Archived"), (Geometry)null, "")
			{
				Selected = IsArchive
			}
		}, popup);
		customMenuList.Operated += async delegate(object o, object o1)
		{
			SwitchArchiveHabit(o1.ToString() == "Archived");
			GetHabitModels();
		};
		customMenuList.Show();
	}

	private void OnHabitListModeClick(object sender, MouseButtonEventArgs e)
	{
		LocalSettings.Settings.HabitItemDisplayMode = ((LocalSettings.Settings.HabitItemDisplayMode == 0) ? 1 : 0);
		UserActCollectUtils.AddClickEvent("habit", "view_style", (LocalSettings.Settings.HabitItemDisplayMode == 0) ? "list" : "card");
	}

	private void OnDateFilterClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: HabitDayCheckModel dataContext })
		{
			bool flag = !dataContext.Selected;
			_daysFilterModels.ForEach(delegate(HabitDayCheckModel m)
			{
				m.Selected = false;
			});
			dataContext.Selected = flag;
			_selectedDate = (flag ? new DateTime?(dataContext.Date) : ((DateTime?)null));
			GetHabitModels(setFilterStatus: false);
			if (Utils.FindParent<MainWindow>(this) != null)
			{
				_mainHabitSelectedDate = _selectedDate;
			}
			UserActCollectUtils.AddClickEvent("habit", "view_action", _selectedDate.HasValue ? "select_date" : "deselect_date");
			LocalSettings.Settings.ExtraSettings.HabitDateSelected = _selectedDate.HasValue;
			LocalSettings.Settings.Save(force: true);
		}
	}

	private void OnCloseFilterClick(object sender, MouseButtonEventArgs e)
	{
		_daysFilterModels.ForEach(delegate(HabitDayCheckModel m)
		{
			m.Selected = false;
		});
		_selectedDate = null;
		GetHabitModels(setFilterStatus: false);
		if (Utils.FindParent<MainWindow>(this) != null)
		{
			_mainHabitSelectedDate = null;
		}
		UserActCollectUtils.AddClickEvent("habit", "view_action", "clear_date");
		LocalSettings.Settings.ExtraSettings.HabitDateSelected = false;
		LocalSettings.Settings.Save(force: true);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/habit/habitlistcontrol.xaml", UriKind.Relative);
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
			Root = (HabitListControl)target;
			break;
		case 2:
			TitlePanel = (StackPanel)target;
			TitlePanel.MouseLeftButtonUp += OnArchiveToggleClick;
			break;
		case 3:
			Title = (TextBlock)target;
			break;
		case 4:
			ListModeSwitch = (HoverIconButton)target;
			break;
		case 5:
			AddButton = (HoverIconButton)target;
			break;
		case 6:
			MoreButton = (HoverIconButton)target;
			break;
		case 7:
			MorePopup = (EscPopup)target;
			break;
		case 8:
			DayFilter = (ItemsControl)target;
			break;
		case 10:
			FilterPanel = (StackPanel)target;
			break;
		case 11:
			SelectedDateText = (TextBlock)target;
			break;
		case 12:
			((Border)target).MouseLeftButtonUp += OnCloseFilterClick;
			break;
		case 13:
			ItemsGrid = (Grid)target;
			ItemsGrid.PreviewMouseLeftButtonUp += OnDrop;
			ItemsGrid.PreviewMouseLeftButtonDown += TryDragItem;
			ItemsGrid.MouseMove += OnDragMove;
			break;
		case 14:
			HabitItems = (ListView)target;
			break;
		case 15:
			DragPopup = (Popup)target;
			break;
		case 16:
			PopupItem = (HabitListItem)target;
			break;
		case 17:
			PopupSectionBorder = (Border)target;
			break;
		case 18:
			PopupSection = (HabitListSectionItem)target;
			break;
		case 19:
			EmptyGrid = (Grid)target;
			break;
		case 20:
			EmptyView = (EmptyWithTextView)target;
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
		if (connectionId == 9)
		{
			((Border)target).MouseLeftButtonUp += OnDateFilterClick;
		}
	}
}
