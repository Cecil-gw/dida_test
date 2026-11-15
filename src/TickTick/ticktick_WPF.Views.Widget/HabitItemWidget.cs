using System;
using System.CodeDom.Compiler;
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
using Emoji.Wpf;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Habit;

namespace ticktick_WPF.Views.Widget;

public class HabitItemWidget : WidgetBaseUserControl, IComponentConnector
{
	internal HabitItemWidget Root;

	internal Border View;

	internal Border EmptyView;

	internal Grid DragGrid;

	internal Grid LockedOptionGrid;

	internal Button UnlockWidgetButton;

	internal Grid MoreGrid;

	internal Button MorePath;

	internal Path MorePathIcon;

	private bool _contentLoaded;

	private HabitItemWidgetViewModel viewModel { get; set; }

	public HabitItemWidget(WidgetViewModel model)
		: base(model)
	{
		InitializeComponent();
		MoreGrid.DataContext = model;
		View.DataContext = viewModel;
	}

	private void UserControl_Loaded(object sender, RoutedEventArgs e)
	{
		LockWidget(base.Model.IsLocked);
		LoadData();
		DataChangedNotifier.HabitCheckInChanged += OnCheckInChanged;
		DataChangedNotifier.HabitsChanged += DataChangedNotifier_HabitsChanged;
		TimeChangeNotifier.DayChanged += TimeChangeNotifier_DayChanged;
	}

	private void Root_Unloaded(object sender, RoutedEventArgs e)
	{
		DataChangedNotifier.HabitCheckInChanged -= OnCheckInChanged;
		DataChangedNotifier.HabitsChanged -= DataChangedNotifier_HabitsChanged;
		TimeChangeNotifier.DayChanged -= TimeChangeNotifier_DayChanged;
	}

	private void TimeChangeNotifier_DayChanged(object sender, EventArgs e)
	{
		LoadData();
	}

	private void OnCheckInChanged(object sender, HabitCheckInModel e)
	{
		if (viewModel == null || !(e.HabitId != viewModel.Id))
		{
			LoadData();
		}
	}

	private void DataChangedNotifier_HabitsChanged(object sender, EventArgs e)
	{
		LoadData();
	}

	public async Task LoadData()
	{
		if (string.IsNullOrEmpty(base.Model.HabitId))
		{
			EmptyView.Visibility = Visibility.Visible;
			return;
		}
		HabitModel habit = await HabitDao.GetHabitById(base.Model.HabitId);
		if (habit == null || habit.Status != 0)
		{
			habit = await HabitDao.GetFirstHabit();
			if (habit == null)
			{
				EmptyView.Visibility = Visibility.Visible;
				return;
			}
		}
		EmptyView.Visibility = Visibility.Collapsed;
		viewModel = new HabitItemWidgetViewModel
		{
			Id = habit.Id,
			Name = habit.Name,
			Habit = habit
		};
		int num = await HabitService.GetCurrentDays(habit);
		if (habit.TargetDays > 0)
		{
			viewModel.Days = $"{num}/{habit.TargetDays}";
		}
		else
		{
			viewModel.Days = num.ToString();
		}
		viewModel.IsTextIcon = !string.IsNullOrEmpty(habit.IconRes) && habit.IconRes.StartsWith("txt_");
		if (!viewModel.IsTextIcon)
		{
			viewModel.Image = HabitService.GetWidgetCornerPic(habit.IconRes);
		}
		else
		{
			viewModel.IconText = habit.IconRes.Split('_').LastOrDefault();
		}
		string iconBackColor = HabitUtils.GetIconBackColor(habit.IconRes, habit.Color);
		viewModel.Background = ThemeUtil.GetColorInString(iconBackColor);
		string widgetTextColor = HabitUtils.GetWidgetTextColor(habit.IconRes, habit.Color);
		viewModel.Foreground = ThemeUtil.GetColorInString(widgetTextColor);
		DateTime today = DateTime.Today;
		HabitCheckInModel habitCheckInModel = await HabitCheckInDao.GetHabitCheckInByHabitIdAndStamp(habit.Id, today.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
		HabitDayCheckModel habitDayCheckModel = new HabitDayCheckModel(habitCheckInModel, today, habit, 0);
		viewModel.DayCheckModel = habitDayCheckModel;
		viewModel.ShowProgress = habitCheckInModel != null && !habitDayCheckModel.Completed && !habitDayCheckModel.Uncompleted && habitCheckInModel.Value > 0.0;
		if (habit.Goal > 1.0 && habitCheckInModel != null)
		{
			viewModel.HasGoal = true;
			viewModel.GoalDescribe = $"{habitCheckInModel.Value}/{habit.Goal} {habit.Unit}";
		}
		View.DataContext = viewModel;
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
		HabitWidgetHelper.CloseHabitItemWidget(base.Model.Id);
		GetParentWindow()?.CloseWidget();
	}

	private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (viewModel != null)
		{
			HabitEventsHelper.CheckDayCheck(viewModel.Habit, viewModel.DayCheckModel, sender as FrameworkElement);
		}
	}

	private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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
			Uri resourceLocator = new Uri("/TickTick;component/views/widget/habititemwidget.xaml", UriKind.Relative);
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
			Root = (HabitItemWidget)target;
			break;
		case 2:
			View = (Border)target;
			break;
		case 3:
			((Grid)target).MouseLeftButtonUp += Grid_MouseLeftButtonDown;
			break;
		case 4:
			((EmjTextBlock)target).MouseLeftButtonUp += TextBlock_MouseLeftButtonDown;
			break;
		case 5:
			EmptyView = (Border)target;
			break;
		case 6:
			((Border)target).MouseLeftButtonUp += JumpToHabitPage;
			break;
		case 7:
			((Border)target).MouseLeftButtonUp += JumpToHabitPage;
			break;
		case 8:
			DragGrid = (Grid)target;
			DragGrid.PreviewMouseLeftButtonDown += base.OnDragBarMouseDown;
			DragGrid.MouseMove += base.OnDragMove;
			break;
		case 9:
			LockedOptionGrid = (Grid)target;
			break;
		case 10:
			UnlockWidgetButton = (Button)target;
			UnlockWidgetButton.Click += base.OnUnlockWidgetClick;
			break;
		case 11:
			MoreGrid = (Grid)target;
			break;
		case 12:
			MorePath = (Button)target;
			MorePath.Click += OnMoreClick;
			break;
		case 13:
			MorePathIcon = (Path)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
