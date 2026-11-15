using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Shapes;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Habit;

namespace ticktick_WPF.Views.Widget;

public class HabitWeekWidget : WidgetBaseUserControl, IComponentConnector, IStyleConnector
{
	internal HabitWeekWidget Root;

	internal Border WidgetBackground;

	internal Grid View;

	internal Grid EmptyView;

	internal Grid DragGrid;

	internal Grid LockedOptionGrid;

	internal Button UnlockWidgetButton;

	internal Grid MoreGrid;

	internal Button MorePath;

	private bool _contentLoaded;

	private bool isReloading { get; set; }

	private HabitWeekWidgetViewModel viewModel { get; set; }

	public HabitWeekWidget(WidgetViewModel model)
		: base(model)
	{
		InitializeComponent();
		View.DataContext = viewModel;
	}

	private void UserControl_Loaded(object sender, RoutedEventArgs e)
	{
		LoadData();
		LockWidget(base.Model.IsLocked);
		DataChangedNotifier.HabitsChanged += OnHabitsChanged;
		DataChangedNotifier.HabitCheckInChanged += OnCheckInChanged;
		DataChangedNotifier.HabitSkip += OnHabitSkip;
		DataChangedNotifier.WeekStartFromChanged += DataChangedNotifier_WeekStartFromChanged;
		TimeChangeNotifier.DayChanged += TimeChangeNotifier_DayChanged;
	}

	private void Root_Unloaded(object sender, RoutedEventArgs e)
	{
		DataChangedNotifier.HabitsChanged -= OnHabitsChanged;
		DataChangedNotifier.HabitCheckInChanged -= OnCheckInChanged;
		DataChangedNotifier.HabitSkip -= OnHabitSkip;
		DataChangedNotifier.WeekStartFromChanged -= DataChangedNotifier_WeekStartFromChanged;
		TimeChangeNotifier.DayChanged -= TimeChangeNotifier_DayChanged;
	}

	private void TimeChangeNotifier_DayChanged(object sender, EventArgs e)
	{
		LoadData();
	}

	private void DataChangedNotifier_WeekStartFromChanged(object sender, EventArgs e)
	{
		LoadData();
	}

	internal override void OnThemeChange(string themeKey)
	{
		if (viewModel != null)
		{
			viewModel.IsDark = base.Model.ViewThemeId == "dark";
		}
	}

	internal override void OnOpacityChange(float opacity)
	{
		if (viewModel == null)
		{
			return;
		}
		foreach (HabitDayCheckModel weekCheckIn in viewModel.WeekCheckIns)
		{
			UpdateBtnOpacity(weekCheckIn);
		}
	}

	private void OnHabitsChanged(object sender, EventArgs e)
	{
		LoadData();
	}

	private void OnHabitSkip(object sender, string e)
	{
		if (viewModel == null || !(e != viewModel.Id))
		{
			LoadData();
		}
	}

	private void OnCheckInChanged(object sender, HabitCheckInModel e)
	{
		if (viewModel == null || !(e.HabitId != viewModel.Id))
		{
			LoadData();
		}
	}

	public async Task LoadData()
	{
		if (string.IsNullOrEmpty(base.Model.HabitId))
		{
			EmptyView.Visibility = Visibility.Visible;
			View.Visibility = Visibility.Collapsed;
			return;
		}
		HabitModel habit = await HabitDao.GetHabitById(base.Model.HabitId);
		if (habit == null || habit.Status != 0)
		{
			habit = await HabitDao.GetFirstHabit();
			if (habit == null)
			{
				EmptyView.Visibility = Visibility.Visible;
				View.Visibility = Visibility.Collapsed;
				return;
			}
		}
		if (isReloading)
		{
			return;
		}
		try
		{
			isReloading = true;
			View.Visibility = Visibility.Visible;
			EmptyView.Visibility = Visibility.Collapsed;
			viewModel = new HabitWeekWidgetViewModel
			{
				Id = habit.Id,
				Name = habit.Name,
				Habit = habit,
				IsDark = (base.Model.ViewThemeId == "dark")
			};
			viewModel.Icon = HabitService.GetIcon(habit.IconRes, habit.Color);
			string iconBackColor = HabitUtils.GetIconBackColor(habit.IconRes, habit.Color);
			viewModel.Background = ThemeUtil.GetColorInString(iconBackColor);
			int num = await HabitService.GetCurrentDays(habit);
			if (habit.TargetDays > 0)
			{
				viewModel.Days = $"{num}/{habit.TargetDays}";
			}
			else
			{
				viewModel.Days = num.ToString();
			}
			HabitWeekWidgetViewModel habitWeekWidgetViewModel = viewModel;
			habitWeekWidgetViewModel.Days = habitWeekWidgetViewModel.Days + (Utils.IsCn() ? "" : " ") + Utils.GetString((habit.TotalCheckIns > 1) ? "PublicUpDays" : "PublicUpDay");
			HabitCheckInModel habitCheckInModel = await HabitCheckInDao.GetHabitCheckInByHabitIdAndStamp(habit.Id, DateTime.Today.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
			if (habit.Goal > 1.0 && habitCheckInModel != null)
			{
				viewModel.Describe = $"{habitCheckInModel.Value}/{habit.Goal} {habit.Unit}";
			}
			HabitCheckinCache.CheckInStatusCache.TryRemove(habit.Id, out var _);
			await HabitListControl.GetCheckInStatus(habit, DateTime.Today.AddDays(-6.0));
			HabitCheckinCache.CheckInStatusCache.TryGetValue(habit.Id, out var value2);
			List<DateTime> noNeedCheckDates = (from c in value2?.Values
				where !c.NeedCheck
				select c.Date).ToList();
			List<HabitCheckInModel> source = (await HabitCheckinCache.GetHabitCheckInsById(habit?.Id)) ?? new List<HabitCheckInModel>();
			DateTime weekStart = Utils.GetWeekStart(DateTime.Today);
			for (int num2 = 0; num2 < 7; num2++)
			{
				DateTime dateTime = weekStart.AddDays(num2);
				string stamp = dateTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
				HabitCheckInModel checkIn = source.FirstOrDefault((HabitCheckInModel c) => c.CheckinStamp == stamp);
				bool needCheck = noNeedCheckDates == null || !noNeedCheckDates.Contains(dateTime);
				HabitDayCheckModel habitDayCheckModel = new HabitDayCheckModel(checkIn, dateTime, habit, 6 + num2)
				{
					NeedCheck = needCheck
				};
				UpdateBtnOpacity(habitDayCheckModel);
				viewModel.WeekCheckIns.Add(habitDayCheckModel);
			}
			View.DataContext = viewModel;
		}
		catch (Exception ex)
		{
			UtilLog.Error("HabitWeekWidget LoadData Error:" + ex);
		}
		finally
		{
			isReloading = false;
		}
	}

	private void UpdateBtnOpacity(HabitDayCheckModel checkModel)
	{
		checkModel.BtnOpacity = 0.1;
		if ((double)base.Model.Opacity >= 0.85)
		{
			if (!checkModel.IsFuture)
			{
				checkModel.BtnOpacity = 0.1;
			}
			else
			{
				checkModel.BtnOpacity = (viewModel.IsDark ? 0.05 : 0.03);
			}
		}
		else if ((double)base.Model.Opacity >= 0.6)
		{
			checkModel.BtnOpacity = ((!checkModel.IsFuture) ? 0.3 : 0.1);
		}
		else
		{
			checkModel.BtnOpacity = ((!checkModel.IsFuture) ? 0.44 : 0.16);
		}
	}

	private void OnMoreClick(object sender, RoutedEventArgs e)
	{
		OnMoreClick(MorePath);
	}

	internal override void LockWidget(bool isLocked)
	{
		base.LockWidget(isLocked);
		DragGrid.IsHitTestVisible = !isLocked;
		LockedOptionGrid.Visibility = ((!isLocked) ? Visibility.Collapsed : Visibility.Visible);
	}

	internal override void CloseWidget()
	{
		HabitWidgetHelper.CloseHabitWeekItemWidget(base.Model.Id);
		GetParentWindow()?.CloseWidget();
	}

	private void HabitItemCheckItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: HabitDayCheckModel dataContext } frameworkElement)
		{
			HabitEventsHelper.CheckDayCheck(viewModel.Habit, dataContext, frameworkElement);
		}
	}

	private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (viewModel != null)
		{
			HabitDayCheckModel habitDayCheckModel = viewModel.WeekCheckIns.FirstOrDefault((HabitDayCheckModel x) => x.Date == DateTime.Today);
			if (habitDayCheckModel != null)
			{
				HabitEventsHelper.CheckDayCheck(viewModel.Habit, habitDayCheckModel, sender as FrameworkElement);
			}
		}
	}

	private void StackPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (viewModel != null)
		{
			App.NavigateHabit(viewModel.Id);
		}
	}

	private void JumpToHabitPage(object sender, MouseButtonEventArgs e)
	{
		App.NavigateHabit(null);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/widget/habitweekwidget.xaml", UriKind.Relative);
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
			Root = (HabitWeekWidget)target;
			break;
		case 2:
			WidgetBackground = (Border)target;
			break;
		case 3:
			View = (Grid)target;
			break;
		case 4:
			((StackPanel)target).MouseLeftButtonUp += StackPanel_MouseLeftButtonDown;
			break;
		case 5:
			((Image)target).MouseLeftButtonUp += Image_MouseLeftButtonDown;
			break;
		case 7:
			EmptyView = (Grid)target;
			break;
		case 8:
			((Border)target).MouseLeftButtonUp += JumpToHabitPage;
			break;
		case 9:
			((Ellipse)target).MouseLeftButtonUp += JumpToHabitPage;
			break;
		case 10:
			DragGrid = (Grid)target;
			DragGrid.PreviewMouseLeftButtonDown += base.OnDragBarMouseDown;
			DragGrid.MouseMove += base.OnDragMove;
			break;
		case 11:
			LockedOptionGrid = (Grid)target;
			break;
		case 12:
			UnlockWidgetButton = (Button)target;
			UnlockWidgetButton.Click += base.OnUnlockWidgetClick;
			break;
		case 13:
			MoreGrid = (Grid)target;
			break;
		case 14:
			MorePath = (Button)target;
			MorePath.Click += OnMoreClick;
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
		if (connectionId == 6)
		{
			((Grid)target).MouseLeftButtonUp += HabitItemCheckItem_MouseLeftButtonUp;
		}
	}
}
