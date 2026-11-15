using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Habit;

namespace ticktick_WPF.Views.Widget;

public class HabitListWiget : WidgetBaseUserControl, IComponentConnector, IStyleConnector
{
	internal HabitListWiget Root;

	internal Border WidgetBackground;

	internal SmoothScrollViewer MainScrollViewer;

	internal ItemsControl MainList;

	internal Grid EmptyView;

	internal ItemsControl EmptyList;

	internal Grid DragGrid;

	internal Grid LockedOptionGrid;

	internal Button UnlockWidgetButton;

	internal Grid MoreGrid;

	internal Button MorePath;

	private bool _contentLoaded;

	private DateTime lastUpdateSizeTime { get; set; }

	private double lastItemHeight { get; set; } = 60.0;

	private bool loaded { get; set; }

	private ObservableCollection<HabitListItem> Items { get; set; } = new ObservableCollection<HabitListItem>();

	private ObservableCollection<object> EmptyItems { get; set; } = new ObservableCollection<object>();

	private List<HabitModel> habits { get; set; } = new List<HabitModel>();

	public HabitListWiget(WidgetViewModel model)
		: base(model)
	{
		InitializeComponent();
		base.DataContext = (base.Model = model);
		MainList.ItemsSource = Items;
		EmptyList.ItemsSource = EmptyItems;
	}

	private async void UserControl_Loaded(object sender, RoutedEventArgs e)
	{
		loaded = true;
		await TryReloadItems();
		LockWidget(base.Model.IsLocked);
		DataChangedNotifier.HabitCheckInChanged += OnCheckInChanged;
		DataChangedNotifier.HabitsChanged += OnHabitsChanged;
		TimeChangeNotifier.DayChanged += TimeChangeNotifier_DayChanged;
	}

	private void Root_Unloaded(object sender, RoutedEventArgs e)
	{
		DataChangedNotifier.HabitCheckInChanged -= OnCheckInChanged;
		DataChangedNotifier.HabitsChanged -= OnHabitsChanged;
		TimeChangeNotifier.DayChanged -= TimeChangeNotifier_DayChanged;
	}

	private async void TimeChangeNotifier_DayChanged(object sender, EventArgs e)
	{
		await TryReloadItems();
	}

	private void Root_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (!loaded)
		{
			return;
		}
		double num = CalculateHeight();
		if (num <= 0.0)
		{
			return;
		}
		lastItemHeight = num;
		if ((DateTime.Now - lastUpdateSizeTime).TotalMilliseconds < 30.0)
		{
			return;
		}
		if (habits != null && habits.Any())
		{
			foreach (HabitListItem item in Items)
			{
				item.Height = num;
			}
			return;
		}
		UpdateEmptyItems();
	}

	private async void OnHabitsChanged(object sender, EventArgs e)
	{
		await TryReloadItems();
	}

	private async void OnCheckInChanged(object sender, HabitCheckInModel e)
	{
		await TryReloadItems();
	}

	private async Task TryReloadItems()
	{
		await GetAllHabits();
		if (habits != null && habits.Any())
		{
			MainScrollViewer.Visibility = Visibility.Visible;
			EmptyView.Visibility = Visibility.Collapsed;
			LoadItems();
		}
		else
		{
			MainScrollViewer.Visibility = Visibility.Collapsed;
			EmptyView.Visibility = Visibility.Visible;
			UpdateEmptyItems();
		}
	}

	private async Task GetAllHabits()
	{
		habits = await HabitService.GetHabitsbyStatus(0);
		UpdateMaxHeight();
	}

	private void UpdateMaxHeight()
	{
		WidgetWindow parentWindow = GetParentWindow();
		if (habits.Count <= 0)
		{
			Size? maxSize = WidgetConstants.GetMaxSize(WidgetType.HabitList);
			if (maxSize.HasValue)
			{
				parentWindow.MaxHeight = maxSize.Value.Height;
			}
			return;
		}
		int num = (habits.Count + 1) / 2;
		if (parentWindow == null)
		{
			return;
		}
		if (num <= 2)
		{
			Size? minSize = WidgetConstants.GetMinSize(WidgetType.HabitList);
			if (minSize.HasValue)
			{
				parentWindow.MaxHeight = minSize.Value.Height;
			}
			else
			{
				parentWindow.MaxHeight = 164.0;
			}
		}
		else
		{
			parentWindow.MaxHeight = 92 * num + 16;
		}
	}

	private int CalculateHeightRow()
	{
		double num = base.ActualHeight;
		if (double.IsNaN(num))
		{
			Size? defaultSize = WidgetConstants.GetDefaultSize(WidgetType.HabitList);
			num = ((!defaultSize.HasValue) ? 164.0 : defaultSize.Value.Height);
		}
		return (int)((num - 16.0) / 70.0);
	}

	public double CalculateHeight()
	{
		double num = Math.Round(base.ActualHeight);
		if (double.IsNaN(num))
		{
			Size? defaultSize = WidgetConstants.GetDefaultSize(WidgetType.HabitList);
			num = ((!defaultSize.HasValue) ? 164.0 : defaultSize.Value.Height);
		}
		int num2 = (int)((num - 16.0) / 70.0);
		int num3 = habits?.Count ?? 0;
		if (num2 > num3 / 2)
		{
			num2 = (num3 + 1) / 2;
		}
		return (num - 16.0 - (double)(10 * num2)) / (double)num2;
	}

	private async Task LoadItems()
	{
		double itemHeight = CalculateHeight();
		DateTime today = DateTime.Today;
		List<HabitModel> habitsToday = await GetSelectDateHabits(habits, today);
		List<HabitModel> sortedHabits = new List<HabitModel>();
		List<HabitSectionModel> list = (await HabitService.GetHabitSections()).OrderBy((HabitSectionModel s) => s.SortOrder).ToList();
		int num = 0;
		foreach (HabitSectionModel section in list)
		{
			List<HabitModel> list2 = (from s in habitsToday
				where s.SectionId == section.Id
				orderby s.SortOrder
				select s).ToList();
			sortedHabits.AddRange(list2);
			if (section.SortOrder < 0)
			{
				num += list2.Count;
			}
		}
		IOrderedEnumerable<HabitModel> collection = from h in habitsToday
			where !sortedHabits.Contains(h)
			orderby h.SortOrder
			select h;
		sortedHabits.InsertRange(num, collection);
		List<HabitListItem> items = new List<HabitListItem>();
		foreach (HabitModel habit in sortedHabits)
		{
			HabitListItem item = new HabitListItem
			{
				Id = habit.Id,
				Habit = habit,
				Name = habit.Name,
				BitmapSource = HabitService.GetIcon(habit.IconRes, habit.Color),
				Step = habit.Step,
				Type = habit.Type,
				Height = itemHeight
			};
			string iconBackColor = HabitUtils.GetIconBackColor(habit.IconRes, habit.Color);
			item.Background = ThemeUtil.GetColorInString(iconBackColor);
			int num2 = await HabitCheckInDao.CountValidHabitCheckInsByHabitId(habit.Id);
			item.Describe = num2.ToString();
			item.Describe = item.Describe + (Utils.IsCn() ? "" : " ") + Utils.GetString((num2 > 1) ? "PublicUpDays" : "PublicUpDay");
			HabitCheckInModel habitCheckInModel = await HabitCheckInDao.GetHabitCheckInByHabitIdAndStamp(habit.Id, today.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
			item.CheckIn = new HabitDayCheckModel(habitCheckInModel, today, habit, 0);
			item.IsCompleted = item.CheckIn.Completed;
			item.IsAbandon = item.CheckIn.Uncompleted;
			if (habit.Goal > 1.0)
			{
				if (habitCheckInModel != null)
				{
					if (habitCheckInModel.Value < habit.Goal)
					{
						item.Describe = $"{habitCheckInModel.Value}/{habit.Goal} {habit.Unit}";
						item.IsShowProgress = true;
						item.PercentValue = Math.Round(habitCheckInModel.Value / habit.Goal * 100.0);
					}
				}
				else
				{
					item.Describe = $"0/{habit.Goal} {habit.Unit}";
					item.IsShowProgress = true;
					item.PercentValue = 0.0;
				}
			}
			items.Add(item);
		}
		for (int num3 = 0; num3 < items.Count; num3++)
		{
			if (num3 < Items.Count)
			{
				Items[num3] = items[num3];
			}
			else
			{
				Items.Add(items[num3]);
			}
		}
		for (int num4 = Items.Count - 1; num4 >= items.Count; num4--)
		{
			Items.RemoveAt(num4);
		}
	}

	private async Task<List<HabitModel>> GetSelectDateHabits(List<HabitModel> habits, DateTime date)
	{
		List<HabitModel> displayHabits = habits.ToList();
		foreach (HabitModel habit in habits)
		{
			if (habit.IsSkip(date))
			{
				displayHabits.Remove(habit);
				continue;
			}
			await HabitListControl.GetCheckInStatus(habit, DateTime.Today.AddDays(-6.0));
			if (HabitCheckinCache.CheckInStatusCache.TryGetValue(habit.Id, out var value) && value.TryGetValue(date, out var value2))
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
		return displayHabits;
	}

	private void UpdateEmptyItems()
	{
		int num = CalculateHeightRow();
		if (EmptyItems.Count == num * 2)
		{
			return;
		}
		List<object> list = new List<object>();
		for (int i = 0; i < num * 2; i++)
		{
			list.Add(new object());
		}
		for (int j = 0; j < list.Count; j++)
		{
			if (j < EmptyItems.Count)
			{
				EmptyItems[j] = list[j];
			}
			else
			{
				EmptyItems.Add(list[j]);
			}
		}
		for (int num2 = EmptyItems.Count - 1; num2 >= list.Count; num2--)
		{
			EmptyItems.RemoveAt(num2);
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
		HabitWidgetHelper.CloseListWidget();
		GetParentWindow()?.CloseWidget();
	}

	private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: HabitListItem dataContext } frameworkElement)
		{
			HabitEventsHelper.CheckDayCheck(dataContext.Habit, dataContext.CheckIn, frameworkElement);
		}
	}

	private void StackPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: HabitListItem dataContext })
		{
			App.NavigateHabit(dataContext.Id);
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
			Uri resourceLocator = new Uri("/TickTick;component/views/widget/habitlistwiget.xaml", UriKind.Relative);
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
			Root = (HabitListWiget)target;
			break;
		case 2:
			WidgetBackground = (Border)target;
			break;
		case 3:
			MainScrollViewer = (SmoothScrollViewer)target;
			break;
		case 4:
			MainList = (ItemsControl)target;
			break;
		case 7:
			EmptyView = (Grid)target;
			break;
		case 8:
			EmptyList = (ItemsControl)target;
			break;
		case 11:
			DragGrid = (Grid)target;
			DragGrid.PreviewMouseLeftButtonDown += base.OnDragBarMouseDown;
			DragGrid.MouseMove += base.OnDragMove;
			break;
		case 12:
			LockedOptionGrid = (Grid)target;
			break;
		case 13:
			UnlockWidgetButton = (Button)target;
			UnlockWidgetButton.Click += base.OnUnlockWidgetClick;
			break;
		case 14:
			MoreGrid = (Grid)target;
			break;
		case 15:
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
		switch (connectionId)
		{
		case 5:
			((Image)target).MouseLeftButtonUp += Image_MouseLeftButtonDown;
			break;
		case 6:
			((StackPanel)target).MouseLeftButtonUp += StackPanel_MouseLeftButtonDown;
			break;
		case 9:
			((Ellipse)target).MouseLeftButtonUp += JumpToHabitPage;
			break;
		case 10:
			((Border)target).MouseLeftButtonUp += JumpToHabitPage;
			break;
		case 7:
		case 8:
			break;
		}
	}
}
