using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Shapes;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Pomo.MiniFocus;

public class MiniFocusStatisticsView : UserControl, IComponentConnector
{
	public static string MiniStatisticsTodayPomo = Utils.GetString("TodayPomo");

	public static string MiniStatisticsTodayDuration = Utils.GetString("DailyFocused");

	public static string MiniStatisticsWeekPomo = Utils.GetString("WeeklyPomo");

	public static string MiniStatisticsWeekDuration = Utils.GetString("WeeklyFocused");

	private List<string> _typeNames;

	internal Grid Container;

	internal Border LeftBorder;

	internal StackPanel StatisticsTitle1;

	internal TextBlock Text1;

	internal Path StatisticsPath1;

	internal Border ValBorder1;

	internal TextBlock FirstVal;

	internal Run FirstRun1;

	internal Run FirstRun2;

	internal Run FirstRun3;

	internal Run FirstRun4;

	internal Border RightBorder;

	internal StackPanel StatisticsTitle2;

	internal TextBlock Text2;

	internal Border ValBorder2;

	internal Run SecondRun1;

	internal Run SecondRun2;

	internal Run SecondRun3;

	internal Run SecondRun4;

	private bool _contentLoaded;

	public bool TitleMouseOver
	{
		get
		{
			if (!StatisticsTitle1.IsMouseOver)
			{
				return StatisticsTitle2.IsMouseOver;
			}
			return true;
		}
	}

	public bool PopupOpened { get; set; }

	public MiniFocusStatisticsView()
	{
		InitializeComponent();
		_typeNames = new List<string> { MiniStatisticsTodayDuration, MiniStatisticsTodayPomo, MiniStatisticsWeekDuration, MiniStatisticsWeekPomo };
		SetData();
		SetTitleEnable();
		PropertyChangedEventManager.AddHandler(LocalSettings.Settings, OnWeekStartFromChanged, "WeekStartFrom");
	}

	private void OnWeekStartFromChanged(object sender, PropertyChangedEventArgs e)
	{
		TickFocusManager.ReloadStatistics();
	}

	public void SetTitleEnable()
	{
		StatisticsTitle2.IsHitTestVisible = TickFocusManager.IsPomo;
		StatisticsTitle1.IsHitTestVisible = TickFocusManager.IsPomo;
	}

	public void SetData()
	{
		base.Dispatcher.Invoke(delegate
		{
			int[] statisticsTypesSafely = TickFocusManager.GetStatisticsTypesSafely();
			int num = ((statisticsTypesSafely[0] >= 0 && statisticsTypesSafely[0] <= 3) ? statisticsTypesSafely[0] : 0);
			int num2 = ((statisticsTypesSafely[1] >= 0 && statisticsTypesSafely[1] <= 3) ? statisticsTypesSafely[1] : 2);
			Text1.Text = _typeNames[num];
			Text2.Text = _typeNames[num2];
			PomoStat pomoStat = TickFocusManager.PomoStatistics;
			if (pomoStat != null)
			{
				SetStatistics(FirstRun1, FirstRun2, FirstRun3, FirstRun4, num);
				SetStatistics(SecondRun1, SecondRun2, SecondRun3, SecondRun4, num2);
			}
			void SetStatistics(Run run1, Run run2, Run run3, Run run4, int type)
			{
				switch (type)
				{
				case 0:
					SetDurationStatistics(run1, run2, run3, run4, pomoStat.TodayDuration);
					break;
				case 1:
					SetPomoStatistics(run1, run2, run3, run4, pomoStat.TodayPomos);
					break;
				case 2:
					SetDurationStatistics(run1, run2, run3, run4, pomoStat.WeeklyDuration);
					break;
				case 3:
					SetPomoStatistics(run1, run2, run3, run4, pomoStat.WeeklyPomos);
					break;
				}
			}
		});
		static void SetDurationStatistics(Run run1, Run run2, Run run3, Run run4, long duration)
		{
			long num = duration / 60 + ((duration % 60 >= 30) ? 1 : 0);
			long num2 = num / 60;
			num %= 60;
			run1.Text = ((num2 > 0) ? (num2.ToString() ?? "") : "");
			run2.Text = ((num2 > 0) ? "h " : "");
			run3.Text = ((num2 > 0 && num == 0L) ? "" : (num.ToString() ?? ""));
			run4.Text = ((num2 > 0 && num == 0L) ? "" : "m");
		}
		static void SetPomoStatistics(Run run1, Run run2, Run run3, Run run4, int pomos)
		{
			run1.Text = pomos.ToString();
			run2.Text = "";
			run3.Text = "";
			run4.Text = "";
		}
	}

	private void OnStatisticsTitle1Click(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		int[] statisticsTypesSafely = TickFocusManager.GetStatisticsTypesSafely();
		PomoStatisticsSetView pomoStatisticsSetView = new PomoStatisticsSetView(statisticsTypesSafely[0], statisticsTypesSafely[1]);
		pomoStatisticsSetView.TypeChanged += delegate
		{
			SetData();
		};
		pomoStatisticsSetView.PopupClosed += delegate
		{
			PopupOpened = false;
		};
		pomoStatisticsSetView.Show(StatisticsTitle1);
		PopupOpened = true;
	}

	private void OnStatisticsTitle2Click(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		int[] statisticsTypesSafely = TickFocusManager.GetStatisticsTypesSafely();
		PomoStatisticsSetView pomoStatisticsSetView = new PomoStatisticsSetView(statisticsTypesSafely[1], statisticsTypesSafely[0]);
		pomoStatisticsSetView.TypeChanged += delegate
		{
			SetData();
		};
		pomoStatisticsSetView.PopupClosed += delegate
		{
			PopupOpened = false;
		};
		pomoStatisticsSetView.Show(StatisticsTitle2);
		PopupOpened = true;
	}

	private void OnStatisticsClick(object sender, MouseButtonEventArgs e)
	{
		if (!StatisticsTitle2.IsMouseOver && !StatisticsTitle1.IsMouseOver)
		{
			TickFocusManager.OpenStatisticsInWeb();
			UserActCollectUtils.AddClickEvent("focus_mini", "om", "statistics");
		}
	}

	public void SetSize(double rate, double textRate)
	{
		Container.Width = 194.0 * rate;
		LeftBorder.Width = 110.0 * rate;
		LeftBorder.Margin = new Thickness(-7.0 * rate, 0.0, 0.0, 0.0);
		StatisticsTitle1.Margin = new Thickness(14.0 * rate, 8.0 * rate, 0.0, 0.0);
		Text1.MaxWidth = 80.0 * rate;
		Text1.LineHeight = 11.0 * textRate;
		Text1.FontSize = 9.5 * textRate;
		StatisticsPath1.Width = 10.0 * textRate;
		StatisticsPath1.Height = 10.0 * textRate;
		ValBorder1.Width = 93.0 * rate;
		ValBorder1.Margin = new Thickness(4.0 * rate, 0.0, 0.0, 0.0);
		FirstVal.Margin = new Thickness(0.0, 4.0 * rate, 0.0, 8.0 * rate);
		FirstVal.FontSize = 14.0 * textRate;
		FirstRun2.FontSize = 10.0 * textRate;
		RightBorder.Margin = new Thickness(85.0 * rate, 0.0, 0.0, 0.0);
		ValBorder2.Margin = new Thickness(97.0 * rate, 0.0, 0.0, 0.0);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/pomo/minifocus/minifocusstatisticsview.xaml", UriKind.Relative);
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
			Container = (Grid)target;
			break;
		case 2:
			LeftBorder = (Border)target;
			break;
		case 3:
			StatisticsTitle1 = (StackPanel)target;
			StatisticsTitle1.PreviewMouseLeftButtonUp += OnStatisticsTitle1Click;
			break;
		case 4:
			Text1 = (TextBlock)target;
			break;
		case 5:
			StatisticsPath1 = (Path)target;
			break;
		case 6:
			ValBorder1 = (Border)target;
			break;
		case 7:
			FirstVal = (TextBlock)target;
			FirstVal.MouseLeftButtonUp += OnStatisticsClick;
			break;
		case 8:
			FirstRun1 = (Run)target;
			break;
		case 9:
			FirstRun2 = (Run)target;
			break;
		case 10:
			FirstRun3 = (Run)target;
			break;
		case 11:
			FirstRun4 = (Run)target;
			break;
		case 12:
			RightBorder = (Border)target;
			break;
		case 13:
			StatisticsTitle2 = (StackPanel)target;
			StatisticsTitle2.PreviewMouseLeftButtonUp += OnStatisticsTitle2Click;
			break;
		case 14:
			Text2 = (TextBlock)target;
			break;
		case 15:
			ValBorder2 = (Border)target;
			break;
		case 16:
			((TextBlock)target).MouseLeftButtonUp += OnStatisticsClick;
			break;
		case 17:
			SecondRun1 = (Run)target;
			break;
		case 18:
			SecondRun2 = (Run)target;
			break;
		case 19:
			SecondRun3 = (Run)target;
			break;
		case 20:
			SecondRun4 = (Run)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
