using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Time;

namespace ticktick_WPF.Views;

public class SelectTimeDropDialog : UserControl, IComponentConnector
{
	private readonly int _hour;

	private readonly int _minute;

	private DateTime _date;

	private TimeZoneSelectControl _tz;

	private DateTime? _beginTime;

	internal ListView ReminderListView;

	internal Grid TimeZoneGrid;

	private bool _contentLoaded;

	public event EventHandler<TimeZoneViewModel> TimeZoneChanged;

	public event EventHandler<DateTime> Select;

	public SelectTimeDropDialog()
	{
		InitializeComponent();
		DateTime dateTime = (_date = DateTime.Now);
		_hour = dateTime.Hour;
		_minute = dateTime.Minute;
		TimeZoneGrid.Visibility = Visibility.Collapsed;
		base.Loaded += delegate
		{
			InitView();
		};
	}

	public SelectTimeDropDialog(DateTime? date, bool showTimeZone, bool enableEditTimeZone, TimeZoneViewModel tzModel = null, DateTime? beginTime = null, bool directSelectTimeZone = false)
	{
		InitializeComponent();
		DateTime dateTime = (_date = date ?? DateTime.Now);
		_beginTime = beginTime;
		_hour = dateTime.Hour;
		_minute = dateTime.Minute;
		InitTimeZoneControl(showTimeZone, tzModel, enableEditTimeZone, directSelectTimeZone);
		base.Loaded += delegate
		{
			InitView();
		};
	}

	private void InitTimeZoneControl(bool showTimeZone, TimeZoneViewModel tzModel, bool enableEditTimeZone, bool directSelectTimeZone)
	{
		if (showTimeZone)
		{
			if (!enableEditTimeZone && tzModel.TimeZone.Equals(TimeZoneInfo.Local) && !tzModel.IsFloat)
			{
				_tz = null;
				TimeZoneGrid.Visibility = Visibility.Collapsed;
			}
			else
			{
				InitTimeZoneOptionControl(tzModel, enableEditTimeZone, directSelectTimeZone);
			}
		}
		else
		{
			_tz = null;
			TimeZoneGrid.Visibility = Visibility.Collapsed;
		}
	}

	private void InitTimeZoneOptionControl(TimeZoneViewModel tzModel, bool enableEditTimeZone, bool directSelectTimeZone)
	{
		_tz = new TimeZoneSelectControl(tzModel)
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			Height = 24.0,
			Margin = new Thickness(16.0, 3.0, 16.0, 3.0),
			VerticalAlignment = VerticalAlignment.Center,
			DirectSelectTimeZone = directSelectTimeZone
		};
		_tz.OnTimeZoneChanged += delegate(object obj, TimeZoneViewModel model)
		{
			this.TimeZoneChanged?.Invoke(obj, model);
		};
		_tz.IsEnabled = enableEditTimeZone;
		_tz.Opacity = (enableEditTimeZone ? 1.0 : 0.56);
		_tz.DropArrow.Visibility = ((!enableEditTimeZone) ? Visibility.Collapsed : Visibility.Visible);
		_tz.SetDropContentWidth(266.0);
		_tz.BackBorder.CornerRadius = new CornerRadius(4.0);
		TimeZoneGrid.Children.Clear();
		TimeZoneGrid.Children.Add(_tz);
	}

	private void InitView()
	{
		ReminderListView.ItemsSource = InitPickerItems(_date, _beginTime);
		ScrollToSelectedItem();
		ReminderListView.Height = 220.0;
	}

	private async void ScrollToSelectedItem()
	{
		DateTime dateTime = new DateTime(_date.Year, _date.Month, _date.Day, _hour, _minute, 0);
		for (int i = 0; i < ReminderListView.Items.Count; i++)
		{
			if (!(ReminderListView.Items[i] is TimeViewModel timeViewModel))
			{
				continue;
			}
			double totalMinutes = (timeViewModel.Value - dateTime).TotalMinutes;
			if (Math.Abs(totalMinutes) <= 0.0)
			{
				timeViewModel.Selected = true;
				timeViewModel.Suggested = false;
				if (ReminderListView.Template.FindName("ScrollViewer", ReminderListView) is ScrollViewer scrollViewer)
				{
					scrollViewer.ScrollToVerticalOffset(i * 32);
					await Task.Delay(300);
				}
				break;
			}
			if (totalMinutes > 0.0 && totalMinutes < 30.0)
			{
				timeViewModel.Suggested = true;
				timeViewModel.Selected = false;
				if (ReminderListView.Template.FindName("ScrollViewer", ReminderListView) is ScrollViewer scrollViewer2)
				{
					scrollViewer2.ScrollToVerticalOffset(i * 32);
				}
				break;
			}
			timeViewModel.Suggested = false;
			timeViewModel.Selected = false;
		}
	}

	private static IEnumerable<TimeViewModel> InitPickerItems(DateTime date, DateTime? beginTime)
	{
		List<TimeViewModel> list = new List<TimeViewModel>();
		DateTime dateTime = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0);
		if (!beginTime.HasValue || beginTime.Value.Date != date.Date)
		{
			for (int i = 0; i < 48; i++)
			{
				DateTime time = dateTime;
				TimeViewModel item = new TimeViewModel
				{
					DisplayText = DateUtils.GetHourMinuteText(time),
					Value = dateTime
				};
				list.Add(item);
				dateTime = dateTime.AddMinutes(30.0);
			}
		}
		else
		{
			DateTime dateTime2 = beginTime.Value;
			for (int j = 0; j < 49; j++)
			{
				dateTime2 = dateTime2.AddMinutes((j < 4) ? 15 : 30);
				int num = (int)(dateTime2 - beginTime.Value).TotalMinutes;
				TimeViewModel item2 = new TimeViewModel
				{
					DisplayText = DateUtils.GetHourMinuteText(dateTime2),
					Value = dateTime2,
					TimeSpanText = ((num >= 60) ? (((num % 60 == 0) ? (num / 60 + " ") : ((float)num / 60f + " ")) + Utils.GetString((num > 60) ? "PublicHours" : "PublicHour")) : (num + " " + Utils.GetString("PublicMinutes"))),
					ShowSpanText = true
				};
				list.Add(item2);
			}
		}
		return list;
	}

	private void OnReminderClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is ListView { SelectedItem: null })
		{
			return;
		}
		foreach (object item in (IEnumerable)ReminderListView.Items)
		{
			if (item is TimeViewModel timeViewModel)
			{
				timeViewModel.Selected = false;
			}
		}
		if (sender is ListView listView2)
		{
			TimeViewModel timeViewModel2 = (TimeViewModel)listView2.SelectedItem;
			timeViewModel2.Selected = true;
			this.Select?.Invoke(this, timeViewModel2.Value);
		}
	}

	public bool TryCloseTimezonePopup()
	{
		return _tz?.ClosePopup() ?? false;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/selecttimedropdialog.xaml", UriKind.Relative);
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
			ReminderListView = (ListView)target;
			ReminderListView.MouseLeftButtonUp += OnReminderClick;
			break;
		case 2:
			TimeZoneGrid = (Grid)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
