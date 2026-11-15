using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Habit;

public class HabitStatisticsView : UserControl, IComponentConnector
{
	private HabitType _type;

	private bool _showCurrentStreak = true;

	private bool _useDefaultThemeGrayBackground;

	private bool _isInLightSlideOutHost;

	internal Grid Container;

	internal ColumnDefinition ThirdColumn;

	internal RowDefinition ThirdRow;

	internal Border MonthCheckCount;

	internal Border TotalCheckCount;

	internal Border MonthCount;

	internal TextBlock MonthCountText;

	internal TextBlock UnitText;

	internal Border TotalCount;

	internal Border MonthRatio;

	internal Border CurrentStreak;

	internal TextBlock StreakNum;

	internal TextBlock StreakUnit;

	internal TextBlock StreakText;

	private bool _contentLoaded;

	internal void SetSlideOutHost(bool isSlideOut)
	{
		_isInLightSlideOutHost = isSlideOut;
		DataChangedNotifier_InterfaceStyleChanged(this, EventArgs.Empty);
	}

	public HabitStatisticsView()
	{
		InitializeComponent();
	}

	public void Load(HabitModel habit, DateTime startDate, DateTime endDate, List<HabitCheckInModel> checkIns, List<HabitRecordModel> records, bool useCardStyle = false, bool useDefaultThemeGrayBackground = false)
	{
		_useDefaultThemeGrayBackground = useDefaultThemeGrayBackground;
		_type = (habit.IsBooleanType() ? HabitType.Boolean : HabitType.Real);
		SetHabitType(_type);
		if (habit.GetStartDate() > startDate)
		{
			DateTime firstCheckDay = habit.GetStartDate();
			foreach (HabitCheckInModel item in checkIns.TakeWhile((HabitCheckInModel checkIn) => firstCheckDay > startDate))
			{
				if ((item.CheckStatus != 0 || item.Value > 0.0) && DateTime.TryParseExact(item.CheckinStamp, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result) && result < firstCheckDay)
				{
					firstCheckDay = result;
				}
			}
			if (records != null)
			{
				foreach (HabitRecordModel item2 in records.TakeWhile((HabitRecordModel record) => firstCheckDay > startDate))
				{
					if (DateTime.TryParseExact(item2.Stamp.ToString(), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result2) && result2.Date < firstCheckDay)
					{
						firstCheckDay = result2.Date;
					}
				}
			}
			if (startDate < firstCheckDay)
			{
				startDate = firstCheckDay;
			}
		}
		if (habit.Status == 1 && endDate > habit.ModifiedTime.Date)
		{
			endDate = habit.ModifiedTime.Date;
		}
		MonthPlanInfo planDaysInMonth = HabitStatisticsUtils.GetPlanDaysInMonth(habit.RepeatRule, startDate, endDate, checkIns, habit.ExDates);
		int startStamp = int.Parse(startDate.Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
		int endStamp = int.Parse(endDate.Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
		List<HabitCheckInModel> source = (from checkIn in checkIns
			where int.Parse(checkIn.CheckinStamp) >= startStamp && int.Parse(checkIn.CheckinStamp) <= endStamp
			orderby int.Parse(checkIn.CheckinStamp)
			select checkIn).ToList();
		int num = source.Count((HabitCheckInModel item) => item.Value >= item.Goal && item.CheckStatus != 1);
		int monthCheckinRate = (int)Math.Round((planDaysInMonth.Count <= 0) ? ((float)((num > 0) ? 100 : 0)) : ((float)num / (float)planDaysInMonth.Count * 100f), 0, MidpointRounding.AwayFromZero);
		HabitStatisticsModel habitStatisticsModel = HabitStatisticsUtils.CalculateHabitStatInfo(checkIns, habit.RepeatRule);
		HabitStatisticsViewModel habitStatisticsViewModel = new HabitStatisticsViewModel
		{
			MonthCheckinCount = num,
			TotalCheckinCount = habitStatisticsModel.TotalCheckIns,
			MonthCheckinRate = monthCheckinRate,
			BestStreak = habitStatisticsModel.MaxStreak,
			CurrentStreak = habitStatisticsModel.CurrentStreak,
			Unit = habit.Unit,
			UseCardStyle = useCardStyle,
			IsShowGrayBack = ShouldShowGrayBackground()
		};
		if (_type == HabitType.Real)
		{
			double value = source.Sum((HabitCheckInModel checkIn) => checkIn.Value);
			habitStatisticsViewModel.MonthCompletion = Math.Round(value, 2);
			double value2 = checkIns.Sum((HabitCheckInModel checkIn) => checkIn.Value);
			habitStatisticsViewModel.TotalCompletion = Math.Round(value2, 2);
		}
		base.DataContext = habitStatisticsViewModel;
	}

	private void SetHabitType(HabitType type)
	{
		_type = type;
		if (_type == HabitType.Boolean)
		{
			Container.Height = 160.0;
			MonthCount.Visibility = Visibility.Collapsed;
			TotalCount.Visibility = Visibility.Collapsed;
			ThirdRow.Height = new GridLength(0.0);
		}
		else
		{
			ThirdRow.Height = new GridLength(1.0, GridUnitType.Star);
			Container.Height = 240.0;
			MonthCount.Visibility = Visibility.Visible;
			TotalCount.Visibility = Visibility.Visible;
		}
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs e)
	{
	}

	private void SwitchStreakDisplay(object sender, MouseButtonEventArgs e)
	{
		_showCurrentStreak = !_showCurrentStreak;
		if (_showCurrentStreak)
		{
			StreakNum.SetBinding(TextBlock.TextProperty, "CurrentStreak");
			StreakText.SetResourceReference(TextBlock.TextProperty, "CurrentStreak");
			StreakUnit.SetBinding(TextBlock.TextProperty, "CurrentStreakUnit");
		}
		else
		{
			StreakNum.SetBinding(TextBlock.TextProperty, "BestStreak");
			StreakText.SetResourceReference(TextBlock.TextProperty, "BestStreak");
			StreakUnit.SetBinding(TextBlock.TextProperty, "BestStreakUnit");
		}
	}

	private void UserControl_Loaded(object sender, RoutedEventArgs e)
	{
		DataChangedNotifier.InterfaceStyleChanged += DataChangedNotifier_InterfaceStyleChanged;
		DataChangedNotifier.ThemeModeChanged += DataChangedNotifier_InterfaceStyleChanged;
	}

	private void UserControl_Unloaded(object sender, RoutedEventArgs e)
	{
		DataChangedNotifier.InterfaceStyleChanged -= DataChangedNotifier_InterfaceStyleChanged;
		DataChangedNotifier.ThemeModeChanged -= DataChangedNotifier_InterfaceStyleChanged;
	}

	private void DataChangedNotifier_InterfaceStyleChanged(object sender, EventArgs e)
	{
		if (base.DataContext is HabitStatisticsViewModel habitStatisticsViewModel)
		{
			habitStatisticsViewModel.IsShowGrayBack = ShouldShowGrayBackground();
		}
	}

	private bool ShouldShowGrayBackground()
	{
		if (!_isInLightSlideOutHost || ThemeUtil.IsDark())
		{
			if (LocalSettings.Settings.ThemeId == "White")
			{
				if (!_useDefaultThemeGrayBackground)
				{
					return LocalSettings.Settings.ExtraSettings.InterfaceStyle == InterfaceStyle.Card;
				}
				return true;
			}
			return false;
		}
		return true;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/habit/habitstatisticsview.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			((HabitStatisticsView)target).SizeChanged += OnSizeChanged;
			((HabitStatisticsView)target).Loaded += UserControl_Loaded;
			((HabitStatisticsView)target).Unloaded += UserControl_Unloaded;
			break;
		case 2:
			Container = (Grid)target;
			break;
		case 3:
			ThirdColumn = (ColumnDefinition)target;
			break;
		case 4:
			ThirdRow = (RowDefinition)target;
			break;
		case 5:
			MonthCheckCount = (Border)target;
			break;
		case 6:
			TotalCheckCount = (Border)target;
			break;
		case 7:
			MonthCount = (Border)target;
			break;
		case 8:
			MonthCountText = (TextBlock)target;
			break;
		case 9:
			UnitText = (TextBlock)target;
			break;
		case 10:
			TotalCount = (Border)target;
			break;
		case 11:
			MonthRatio = (Border)target;
			break;
		case 12:
			CurrentStreak = (Border)target;
			break;
		case 13:
			StreakNum = (TextBlock)target;
			break;
		case 14:
			StreakUnit = (TextBlock)target;
			break;
		case 15:
			((Image)target).MouseLeftButtonUp += SwitchStreakDisplay;
			break;
		case 16:
			StreakText = (TextBlock)target;
			StreakText.MouseLeftButtonUp += SwitchStreakDisplay;
			break;
		case 17:
			((Border)target).MouseLeftButtonUp += SwitchStreakDisplay;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
