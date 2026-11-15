using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using Newtonsoft.Json;
using TickTickUtils.Lang;
using ticktick_WPF.Entity.Calendar;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service.Display;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.Calendar.Year;
using ticktick_WPF.Views.Project;

namespace ticktick_WPF.ViewModels;

internal class YearCalendarViewModel : INotifyPropertyChanged, IDisposable
{
	internal List<CalendarDisplayModel> _tasks;

	private bool _showWeekNums;

	private int _monthRowsCount = 3;

	private double _eachMonthRowHeight;

	private double _cellWidth;

	private double _cellHeight;

	private double _cellMinLength;

	private double _yearFontSize = 24.0;

	private double _yearLineHeight = 34.0;

	private double _monthFontSize;

	private double _monthLineHeight;

	private double _dayFontSize;

	private double _dayLineHeight;

	private double _yearBottomSpacing;

	private double _monthTitleBottomSpacing;

	private double _monthLeftSpacing;

	private double _dayOfWeekBottomSpacing;

	private string _themeId;

	private bool _showHeatMap;

	private SineEase ease = new SineEase
	{
		EasingMode = EasingMode.EaseOut
	};

	public Action<DateTime> OnDateCountChange { get; set; }

	public Action OnForceRedraw { get; set; }

	public ObservableCollection<YearViewItem> Datas { get; set; } = new ObservableCollection<YearViewItem>();

	public bool ShowWeekNums
	{
		get
		{
			return _showWeekNums;
		}
		set
		{
			_showWeekNums = value;
			NotifyPropertyChanged("ShowWeekNums");
		}
	}

	public int MonthRowsCount
	{
		get
		{
			return _monthRowsCount;
		}
		set
		{
			_monthRowsCount = value;
			NotifyPropertyChanged("MonthRowsCount");
		}
	}

	public double EachMonthRowHeight
	{
		get
		{
			return Math.Max(10.0, _eachMonthRowHeight);
		}
		set
		{
			_eachMonthRowHeight = value;
			NotifyPropertyChanged("EachMonthRowHeight");
		}
	}

	public double CellWidth
	{
		get
		{
			return Math.Max(2.0, _cellWidth);
		}
		set
		{
			_cellWidth = value;
			NotifyPropertyChanged("CellWidth");
		}
	}

	public double CellHeight
	{
		get
		{
			return Math.Max(10.0, _cellHeight);
		}
		set
		{
			_cellHeight = value;
			NotifyPropertyChanged("CellHeight");
		}
	}

	public double CellMinLength
	{
		get
		{
			return Math.Max(10.0, _cellMinLength);
		}
		set
		{
			_cellMinLength = value;
			NotifyPropertyChanged("CellMinLength");
		}
	}

	public double YearFontSize
	{
		get
		{
			return Math.Max(1.0, _yearFontSize);
		}
		set
		{
			_yearFontSize = value;
			NotifyPropertyChanged("YearFontSize");
		}
	}

	public double YearLineHeight
	{
		get
		{
			return Math.Max(1.0, _yearLineHeight);
		}
		set
		{
			_yearLineHeight = value;
			NotifyPropertyChanged("YearLineHeight");
		}
	}

	public double MonthFontSize
	{
		get
		{
			return Math.Max(1.0, _monthFontSize);
		}
		set
		{
			_monthFontSize = value;
			NotifyPropertyChanged("MonthFontSize");
		}
	}

	public double MonthLineHeight
	{
		get
		{
			return Math.Max(1.0, _monthLineHeight);
		}
		set
		{
			_monthLineHeight = value;
			NotifyPropertyChanged("MonthLineHeight");
		}
	}

	public double DayFontSize
	{
		get
		{
			return Math.Max(1.0, _dayFontSize);
		}
		set
		{
			_dayFontSize = value;
			NotifyPropertyChanged("DayFontSize");
		}
	}

	public double DayLineHeight
	{
		get
		{
			return Math.Max(1.0, _dayLineHeight);
		}
		set
		{
			_dayLineHeight = value;
			NotifyPropertyChanged("DayLineHeight");
		}
	}

	public double YearBottomSpacing
	{
		get
		{
			return _yearBottomSpacing;
		}
		set
		{
			_yearBottomSpacing = value;
			NotifyPropertyChanged("YearBottomSpacing");
		}
	}

	public double MonthTitleBottomSpacing
	{
		get
		{
			return _monthTitleBottomSpacing;
		}
		set
		{
			_monthTitleBottomSpacing = value;
			NotifyPropertyChanged("MonthTitleBottomSpacing");
		}
	}

	public double MonthLeftSpacing
	{
		get
		{
			return Math.Max(0.0, _monthLeftSpacing);
		}
		set
		{
			_monthLeftSpacing = value;
			NotifyPropertyChanged("MonthLeftSpacing");
		}
	}

	public double DayOfWeekBottomSpacing
	{
		get
		{
			return _dayOfWeekBottomSpacing;
		}
		set
		{
			_dayOfWeekBottomSpacing = value;
			NotifyPropertyChanged("DayOfWeekBottomSpacing");
		}
	}

	public string ThemeId
	{
		get
		{
			return _themeId;
		}
		set
		{
			_themeId = value;
			NotifyPropertyChanged("ThemeId");
		}
	}

	public bool ShowHeatMap
	{
		get
		{
			return _showHeatMap;
		}
		set
		{
			_showHeatMap = value;
			NotifyPropertyChanged("ShowHeatMap");
		}
	}

	private int eachRowMonthCount => 12 / MonthRowsCount;

	private double yearTitleRowHeight => YearLineHeight + YearBottomSpacing;

	private GregorianCalendar gregorianCalendar { get; set; } = new GregorianCalendar();

	private Size ContainerSize { get; set; }

	private Size lastProcessedSize { get; set; }

	private DateTime lastUpdateSizeTime { get; set; }

	private Debouncer scrollFinishDebouncer { get; set; }

	private Debouncer gcDebouncer { get; set; }

	private Debouncer scrollDataCollectDebouncer { get; set; }

	private Random random { get; set; } = new Random();

	public bool IsDisableUpdateRect { get; set; }

	public DateTime SelectedDate { get; set; } = DateTime.Today;

	private CancellationTokenSource _currentAnimationCts { get; set; }

	private Task _scrollTask { get; set; }

	private CancellationTokenSource _loadCountCts { get; set; }

	private YearCalendarControl control { get; }

	private List<YearViewSizeSolution> sizeSolutions { get; set; } = new List<YearViewSizeSolution>
	{
		new YearViewSizeSolution
		{
			XY = 22.0,
			YearFontSize = 18.0,
			YearLineHeight = 25.0,
			MonthFontSize = 13.0,
			MonthLineHeight = 16.0,
			DayFontSize = 9.0,
			DayLineHeight = 13.0,
			MonthTitleBottomSpacing = 8.0,
			DayOfWeekBottomSpacing = 4.0
		},
		new YearViewSizeSolution
		{
			XY = 26.0,
			YearFontSize = 21.0,
			YearLineHeight = 29.0,
			MonthFontSize = 16.0,
			MonthLineHeight = 19.0,
			DayFontSize = 11.0,
			DayLineHeight = 15.0,
			MonthTitleBottomSpacing = 12.0,
			DayOfWeekBottomSpacing = 6.0
		},
		new YearViewSizeSolution
		{
			XY = 32.0,
			YearFontSize = 24.0,
			YearLineHeight = 34.0,
			MonthFontSize = 19.0,
			MonthLineHeight = 23.0,
			DayFontSize = 13.0,
			DayLineHeight = 18.0,
			MonthTitleBottomSpacing = 16.0,
			DayOfWeekBottomSpacing = 8.0
		},
		new YearViewSizeSolution
		{
			XY = 40.0,
			YearFontSize = 28.0,
			YearLineHeight = 39.0,
			MonthFontSize = 22.0,
			MonthLineHeight = 26.0,
			DayFontSize = 15.0,
			DayLineHeight = 21.0,
			MonthTitleBottomSpacing = 20.0,
			DayOfWeekBottomSpacing = 10.0
		},
		new YearViewSizeSolution
		{
			XY = double.MaxValue,
			YearFontSize = 30.0,
			YearLineHeight = 42.0,
			MonthFontSize = 25.0,
			MonthLineHeight = 30.0,
			DayFontSize = 17.0,
			DayLineHeight = 24.0,
			MonthTitleBottomSpacing = 24.0,
			DayOfWeekBottomSpacing = 12.0
		}
	};

	public DateTime Today { get; set; } = DateTime.Today;

	public bool isVirtual { get; set; }

	private object touchScollLocker { get; } = new object();

	public bool IsNewSwitchView { get; set; }

	public event PropertyChangedEventHandler PropertyChanged;

	public event EventHandler<(DateTime, DateTime)> DateRangeChanged;

	public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public YearCalendarViewModel(YearCalendarControl yearCalendarControl)
	{
		control = yearCalendarControl;
		InitFromConfig();
		PropertyChangedEventManager.AddHandler(LocalSettings.Settings, OnShowWeekChange, "ShowWeek");
		DataChangedNotifier.ThemeModeChanged += OnThemeChanged;
		DataChangedNotifier.CalendarShowYearHeatMapChanged += OnShowYearHeatMapChanged;
		DataChangedNotifier.WeekStartFromChanged += OnWeekStartFromChanged;
		TimeChangeNotifier.DayChanged += TimeChangeNotifier_DayChanged;
		LocalSettings settings = LocalSettings.Settings;
		settings.OnPreferenceChanged = (Action)Delegate.Combine(settings.OnPreferenceChanged, new Action(InitFromConfig));
		scrollDataCollectDebouncer = new Debouncer(3000, OnScrollDataCollect);
		scrollFinishDebouncer = new Debouncer(380, OnFinishScroll);
		gcDebouncer = new Debouncer(2000, GC.Collect);
	}

	public void InitFromConfig()
	{
		ShowWeekNums = LocalSettings.Settings.ShowWeek;
		ShowHeatMap = LocalSettings.Settings.ShowHeatMap;
	}

	private void TimeChangeNotifier_DayChanged(object sender, EventArgs e)
	{
		Today = DateTime.Today;
		OnForceRedraw?.Invoke();
	}

	private void OnWeekStartFromChanged(object sender, EventArgs e)
	{
		LoadYear(SelectedDate);
	}

	private void OnShowYearHeatMapChanged(object sender, bool e)
	{
		ShowHeatMap = e;
		if (e)
		{
			LoadCount();
		}
	}

	private void OnThemeChanged(object sender, EventArgs e)
	{
		ThemeId = LocalSettings.Settings.ThemeId;
	}

	private void OnShowWeekChange(object sender, PropertyChangedEventArgs e)
	{
		ShowWeekNums = LocalSettings.Settings.ShowWeek;
		UpdateRect();
	}

	public void LoadYear(DateTime date)
	{
		if (!isVirtual)
		{
			Today = DateTime.Today;
		}
		SelectedDate = date;
		Datas.Clear();
		_ = LocalSettings.Settings.WeekStartFromType;
		for (int i = 1; i <= 12; i += eachRowMonthCount)
		{
			DateTime startDate = new DateTime(date.Year, i, 1);
			YearMonthRowItem yearMonthRowItem = LoadMonthRow(startDate);
			yearMonthRowItem.Offset = (double)(i / eachRowMonthCount) * EachMonthRowHeight;
			if (i == 1)
			{
				yearMonthRowItem.IsViewStart = true;
			}
			Datas.Add(yearMonthRowItem);
		}
		LoadPrevRow();
		LoadNextRow();
		this.DateRangeChanged?.Invoke(this, GetDateRange());
		if (!isVirtual)
		{
			LoadCount();
		}
	}

	public void LoadPrevYear()
	{
		LoadYear(SelectedDate.AddYears(-1));
	}

	public void LoadNextYear()
	{
		LoadYear(SelectedDate.AddYears(1));
	}

	public void LoadPrevRow()
	{
		YearMonthRowItem yearMonthRowItem = Datas.FirstOrDefault((YearViewItem x) => x is YearMonthRowItem) as YearMonthRowItem;
		DateTime monthStart = yearMonthRowItem.Items.FirstOrDefault().MonthStart;
		if (monthStart.Month == 1 && Datas.IndexOf(yearMonthRowItem) == 0)
		{
			int year = monthStart.Year;
			Datas.Insert(0, new YearHeaderItem(year)
			{
				Offset = GetBeignOffset() - yearTitleRowHeight
			});
			LoadPrevRow();
		}
		else
		{
			monthStart = monthStart.AddMonths(-eachRowMonthCount);
			YearMonthRowItem yearMonthRowItem2 = LoadMonthRow(monthStart);
			yearMonthRowItem2.Offset = GetBeignOffset() - EachMonthRowHeight;
			Datas.Insert(0, yearMonthRowItem2);
		}
	}

	public void LoadNextRow()
	{
		YearMonthRowItem yearMonthRowItem = Datas.LastOrDefault((YearViewItem x) => x is YearMonthRowItem) as YearMonthRowItem;
		DateTime monthStart = yearMonthRowItem.Items.LastOrDefault().MonthStart;
		double offset = Datas.Sum((YearViewItem x) => GetRowHeight(x)) + GetBeignOffset();
		if (monthStart.Month == 12 && Datas.IndexOf(yearMonthRowItem) == Datas.Count - 1)
		{
			int year = monthStart.AddYears(1).Year;
			Datas.Add(new YearHeaderItem(year)
			{
				Offset = offset
			});
			LoadNextRow();
		}
		else
		{
			monthStart = monthStart.AddMonths(1);
			YearMonthRowItem yearMonthRowItem2 = LoadMonthRow(monthStart);
			yearMonthRowItem2.Offset = offset;
			Datas.Add(yearMonthRowItem2);
		}
	}

	public void RemoveRow(bool fromTop)
	{
		if (fromTop)
		{
			Datas.RemoveAt(0);
		}
		else
		{
			Datas.RemoveAt(Datas.Count - 1);
		}
	}

	private YearMonthRowItem LoadMonthRow(DateTime startDate)
	{
		YearMonthRowItem yearMonthRowItem = new YearMonthRowItem();
		for (int i = 0; i < eachRowMonthCount; i++)
		{
			YearViewMonthData item = LoadMonth(startDate.AddMonths(i));
			yearMonthRowItem.Items.Add(item);
		}
		return yearMonthRowItem;
	}

	private YearViewMonthData LoadMonth(DateTime monthStart)
	{
		DayOfWeek weekStartFromType = LocalSettings.Settings.WeekStartFromType;
		DateTime monthStart2 = monthStart;
		int year = monthStart2.Year;
		int month = monthStart2.Month;
		int num = (7 + monthStart2.DayOfWeek - weekStartFromType) % 7;
		DateTime dateTime = monthStart2.AddDays(-num);
		YearViewMonthData yearViewMonthData = new YearViewMonthData(this)
		{
			MonthName = monthStart2.ToString("MMMM", App.Ci),
			Year = year,
			Month = month,
			MonthStart = monthStart2
		};
		for (int i = 0; i < 7; i++)
		{
			int num2 = (int)(weekStartFromType + i) % 7;
			ObservableCollection<string> dayOfWeeks = yearViewMonthData.DayOfWeeks;
			DayOfWeek dayOfWeek = (DayOfWeek)num2;
			dayOfWeeks.Add(Utils.GetString(dayOfWeek.ToString().Substring(0, 2)));
		}
		for (int j = 0; j < 6; j++)
		{
			int weekNumOfYear = DateUtils.GetWeekNumOfYear(dateTime.AddDays(j * 7).AddDays(6.0));
			yearViewMonthData.Weeks.Add(weekNumOfYear);
		}
		for (int k = 0; k < 42; k++)
		{
			DateTime dateTime2 = dateTime.AddDays(k);
			YearViewCalendarDay item = new YearViewCalendarDay(this)
			{
				Date = dateTime2,
				IsCurrentMonth = (dateTime2.Month == month),
				IsToday = (dateTime2 == Today && dateTime2.Month == month),
				Year = year,
				Month = month,
				Day = dateTime2.Day
			};
			yearViewMonthData.Days.Add(item);
		}
		return yearViewMonthData;
	}

	public (DateTime, DateTime) GetDateRange()
	{
		YearViewItem yearViewItem = Datas.FirstOrDefault((YearViewItem x) => x.IsViewStart);
		if (yearViewItem == null)
		{
			return (new DateTime(DateTime.Today.Year, 1, 1), new DateTime(DateTime.Today.Year, 12, 31));
		}
		if (yearViewItem is YearMonthRowItem yearMonthRowItem)
		{
			DateTime monthStart = yearMonthRowItem.Items.FirstOrDefault().MonthStart;
			return (monthStart, monthStart.AddYears(1));
		}
		int num = Datas.IndexOf(yearViewItem);
		DateTime monthStart2 = (Datas[num + 1] as YearMonthRowItem).Items.FirstOrDefault().MonthStart;
		return (monthStart2, monthStart2.AddYears(1));
	}

	public async Task LoadCount()
	{
		if (isVirtual || !ShowHeatMap)
		{
			return;
		}
		_loadCountCts = new CancellationTokenSource();
		Task.Run(async delegate
		{
			try
			{
				(DateTime startTime, DateTime endTime) range = GetDateRange();
				ProjectExtra projectFilter = null;
				Application.Current.Dispatcher.Invoke(delegate
				{
					CalendarControl calendarControl = Utils.FindParent<CalendarControl>(control);
					projectFilter = ((calendarControl != null && calendarControl.ViewSourceType == CalendarViewSourceType.WithList) ? LocalSettings.Settings.ExtraSettings.CalendarParallelSelectProject : ProjectExtra.Deserialize(LocalSettings.Settings.CalendarFilterData));
				});
				List<CalendarDisplayModel> tasks = await CalendarDisplayService.GetDisplayModels(range.startTime, range.endTime, withCourses: true, withFocus: false, projectFilter, pullRemote: false, forcePullPomo: true);
				_tasks = tasks;
				if (!_loadCountCts.IsCancellationRequested)
				{
					List<YearMonthRowItem> monthRows = Datas.OfType<YearMonthRowItem>().ToList();
					Application.Current.Dispatcher.Invoke((Func<Task>)async delegate
					{
						foreach (YearMonthRowItem item in monthRows)
						{
							if (_loadCountCts.IsCancellationRequested)
							{
								break;
							}
							foreach (YearViewMonthData item2 in item.Items)
							{
								if (!(item2.MonthStart > range.endTime) && !(item2.MonthStart.AddMonths(1).AddSeconds(-1.0) < range.startTime))
								{
									if (_loadCountCts.IsCancellationRequested)
									{
										return;
									}
									foreach (YearViewCalendarDay day in item2.Days)
									{
										if (_loadCountCts.IsCancellationRequested)
										{
											return;
										}
										day.Count = tasks.Count(delegate(CalendarDisplayModel x)
										{
											if (x.DisplayStartDate.HasValue)
											{
												if (!x.DisplayDueDate.HasValue)
												{
													return day.Date == x.DisplayStartDate.Value.Date;
												}
												if (day.Date.AddDays(1.0) > x.DisplayStartDate.Value)
												{
													return day.Date < x.DisplayDueDate.Value;
												}
												return false;
											}
											return false;
										});
									}
									OnDateCountChange?.Invoke(item2.MonthStart);
								}
							}
						}
					});
				}
			}
			catch (Exception value)
			{
				Console.WriteLine(value);
			}
		});
	}

	public void ClearItem()
	{
		Datas.Clear();
	}

	public void LoadVirtualModels()
	{
		ShowHeatMap = true;
		Today = new DateTime(2023, 9, 6);
		LoadYear(new DateTime(2023, 1, 1));
		Dictionary<string, Dictionary<string, int>> dictionary = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, int>>>(IOUtils.GetFileContentInResourceFile("/Assets/CalendarYearVirtualData.json"));
		foreach (YearMonthRowItem item in Datas.OfType<YearMonthRowItem>().ToList())
		{
			foreach (YearViewMonthData item2 in item.Items)
			{
				if (!dictionary.TryGetValue(item2.Month.ToString(), out var value))
				{
					continue;
				}
				foreach (YearViewCalendarDay day in item2.Days)
				{
					if (value.TryGetValue(day.Day.ToString(), out var value2))
					{
						day.Count = value2;
					}
				}
				OnDateCountChange?.Invoke(item2.MonthStart);
			}
		}
	}

	private double GetRowHeight(YearViewItem item)
	{
		if (item is YearHeaderItem)
		{
			return yearTitleRowHeight;
		}
		return EachMonthRowHeight;
	}

	private double GetBeignOffset()
	{
		if (!Datas.Any())
		{
			return 0.0;
		}
		return Datas.FirstOrDefault().Offset;
	}

	public void OnContainerSizeChanged(Size size)
	{
		_ = ContainerSize == default(Size);
		ContainerSize = size;
		UpdateRect();
	}

	public void UpdateRect()
	{
		if (IsDisableUpdateRect || ContainerSize == default(Size) || ContainerSize == lastProcessedSize)
		{
			return;
		}
		DateTime now = DateTime.Now;
		if ((now - lastUpdateSizeTime).TotalMilliseconds < 30.0)
		{
			return;
		}
		lastUpdateSizeTime = now;
		try
		{
			lastProcessedSize = ContainerSize;
			int num = 3;
			num = ((!(ContainerSize.Width > ContainerSize.Height)) ? 4 : 3);
			if (num != MonthRowsCount)
			{
				Reshape(MonthRowsCount, eachRowMonthCount, num);
				MonthRowsCount = num;
				LoadCount();
				this.DateRangeChanged?.Invoke(this, GetDateRange());
			}
			EachMonthRowHeight = ContainerSize.Height / (double)MonthRowsCount;
			YearBottomSpacing = EachMonthRowHeight * 0.07 - 4.0;
			int num2 = 7 * eachRowMonthCount + (eachRowMonthCount + 1);
			if (ShowWeekNums)
			{
				num2 += eachRowMonthCount;
			}
			CellWidth = ContainerSize.Width / (double)num2;
			foreach (YearViewSizeSolution sizeSolution in sizeSolutions)
			{
				double num3 = (EachMonthRowHeight * 0.85 - sizeSolution.MonthLineHeight - sizeSolution.MonthTitleBottomSpacing - sizeSolution.DayOfWeekBottomSpacing) / 7.0;
				if (CellWidth <= sizeSolution.XY || num3 <= sizeSolution.XY)
				{
					CellHeight = Math.Max(2.0, num3);
					YearFontSize = sizeSolution.YearFontSize;
					YearLineHeight = sizeSolution.YearLineHeight;
					MonthFontSize = sizeSolution.MonthFontSize;
					MonthLineHeight = sizeSolution.MonthLineHeight;
					DayFontSize = sizeSolution.DayFontSize;
					DayLineHeight = sizeSolution.DayLineHeight;
					MonthTitleBottomSpacing = sizeSolution.MonthTitleBottomSpacing;
					DayOfWeekBottomSpacing = sizeSolution.DayOfWeekBottomSpacing;
					if (IsNewSwitchView)
					{
						IsNewSwitchView = false;
						UserActCollectUtils.AddClickEvent("calendar", "font_size_show", DayFontSize.ToString());
					}
					break;
				}
			}
			CellMinLength = Math.Min(CellWidth, CellHeight) - 1.0;
			MonthLeftSpacing = CellWidth / 2.0 - DayFontSize + 0.22 * YearFontSize;
			UpdateOffset(Datas, immediately: true);
		}
		catch (Exception ex)
		{
			Console.WriteLine("UpdateRect:" + ex);
			UtilLog.Error(ex);
		}
	}

	public void UpdateOffset(ObservableCollection<YearViewItem> items, bool immediately)
	{
		if (items == null || items.Count == 0)
		{
			return;
		}
		int num = -1;
		for (int i = 0; i < items.Count; i++)
		{
			if (items[i].IsViewStart)
			{
				num = i;
				break;
			}
		}
		if (num == -1)
		{
			return;
		}
		double num2 = 0.0;
		for (int num3 = num - 1; num3 >= 0; num3--)
		{
			num2 -= GetRowHeight(items[num3]);
			if (immediately)
			{
				items[num3].Offset = num2;
			}
			else
			{
				items[num3].TargetOffset = num2;
			}
		}
		num2 = 0.0;
		for (int j = num; j < items.Count; j++)
		{
			if (immediately)
			{
				items[j].Offset = num2;
			}
			else
			{
				items[j].TargetOffset = num2;
			}
			num2 += GetRowHeight(items[j]);
		}
	}

	private async Task AnimateOffset(bool moveDown, double milliseconds = 400.0)
	{
		_currentAnimationCts?.Cancel();
		if (_scrollTask != null)
		{
			await _scrollTask;
		}
		_currentAnimationCts?.Dispose();
		_currentAnimationCts = new CancellationTokenSource();
		CancellationToken token = _currentAnimationCts.Token;
		_scrollTask = Task.Run(async delegate
		{
			List<YearViewItem> datas = Datas.ToList();
			try
			{
				DateTime startTime = DateTime.Now;
				TimeSpan duration = TimeSpan.FromMilliseconds(milliseconds);
				List<double> startOffsets = datas.Select((YearViewItem item) => item.Offset).ToList();
				while (true)
				{
					if (token.IsCancellationRequested)
					{
						return;
					}
					double progress = (DateTime.Now - startTime).TotalMilliseconds / duration.TotalMilliseconds;
					progress = Math.Min(Math.Max(progress, 0.0), 1.0);
					Application.Current.Dispatcher.Invoke(delegate
					{
						double num = ease.Ease(progress);
						for (int i = 0; i < datas.Count; i++)
						{
							double num2 = startOffsets[i];
							double targetOffset = datas[i].TargetOffset;
							datas[i].Offset = num2 + (targetOffset - num2) * num;
						}
					});
					if (progress >= 1.0)
					{
						break;
					}
					try
					{
						await Task.Delay(16, token);
					}
					catch (OperationCanceledException) when (token.IsCancellationRequested)
					{
						return;
					}
				}
			}
			catch (OperationCanceledException)
			{
			}
			catch (Exception value)
			{
				Console.WriteLine(value);
			}
			Application.Current.Dispatcher.Invoke(delegate
			{
				foreach (YearViewItem item in datas)
				{
					item.Offset = item.TargetOffset;
				}
				UpdateDatasCount(Datas);
			});
		});
	}

	public void UpdateDatasCount(ObservableCollection<YearViewItem> items)
	{
		if (items == null || items.Count == 0)
		{
			return;
		}
		int num = -1;
		for (int i = 0; i < items.Count; i++)
		{
			if (items[i].IsViewStart)
			{
				num = i;
				break;
			}
		}
		if (num != -1)
		{
			int num2 = Math.Max(0, num - 1);
			int num3 = Math.Min(items.Count - 1, num + 4);
			for (int num4 = items.Count - 1; num4 > num3; num4--)
			{
				items.RemoveAt(num4);
			}
			for (int num5 = num2 - 1; num5 >= 0; num5--)
			{
				items.RemoveAt(num5);
			}
			gcDebouncer?.Trigger();
		}
	}

	public async Task Scroll(bool movedown)
	{
		if (movedown)
		{
			LoadNextRow();
		}
		else
		{
			LoadPrevRow();
		}
		int num = Datas.ToList().FindIndex((YearViewItem item) => item.IsViewStart);
		if (movedown)
		{
			if (num >= 0 && num < Datas.Count - 1)
			{
				Datas[num].IsViewStart = false;
				Datas[num + 1].IsViewStart = true;
				if (Datas[num + 1] is YearHeaderItem && num + 2 < Datas.Count)
				{
					Datas[num + 1].IsViewStart = false;
					Datas[num + 2].IsViewStart = true;
				}
			}
		}
		else if (num > 0)
		{
			Datas[num].IsViewStart = false;
			Datas[num - 1].IsViewStart = true;
			if (Datas[num - 1] is YearHeaderItem && num - 2 >= 0)
			{
				Datas[num - 1].IsViewStart = false;
				Datas[num - 2].IsViewStart = true;
			}
		}
		this.DateRangeChanged?.Invoke(this, GetDateRange());
		UpdateOffset(Datas, immediately: false);
		await AnimateOffset(movedown);
		_loadCountCts?.Cancel();
		scrollFinishDebouncer?.Trigger();
		scrollDataCollectDebouncer?.Trigger();
	}

	private void OnFinishScroll()
	{
		LoadCount();
	}

	public void OnTouchScroll(int delta)
	{
		lock (touchScollLocker)
		{
			YearViewItem yearViewItem = Datas.FirstOrDefault((YearViewItem x) => x.IsViewStart);
			foreach (YearViewItem item in Datas.ToList())
			{
				item.Offset += delta;
			}
			if (yearViewItem == null)
			{
				return;
			}
			int num = Datas.IndexOf(yearViewItem);
			if (yearViewItem.Offset < -0.5 * EachMonthRowHeight)
			{
				LoadNextRow();
				if (num + 1 >= Datas.Count)
				{
					return;
				}
				yearViewItem.IsViewStart = false;
				YearViewItem yearViewItem2 = Datas[num + 1];
				if (yearViewItem2 is YearHeaderItem)
				{
					if (num + 2 < Datas.Count)
					{
						Datas[num + 2].IsViewStart = true;
					}
				}
				else
				{
					yearViewItem2.IsViewStart = true;
				}
			}
			else
			{
				if (!(yearViewItem.Offset > 0.5 * EachMonthRowHeight))
				{
					return;
				}
				LoadPrevRow();
				if (num - 1 < 0)
				{
					return;
				}
				yearViewItem.IsViewStart = false;
				YearViewItem yearViewItem3 = Datas[num - 1];
				if (yearViewItem3 is YearHeaderItem)
				{
					if (num - 2 >= 0)
					{
						Datas[num - 2].IsViewStart = true;
					}
				}
				else
				{
					yearViewItem3.IsViewStart = true;
				}
			}
		}
	}

	public async Task OnTouchScrollEnd(bool movedown)
	{
		this.DateRangeChanged?.Invoke(this, GetDateRange());
		UpdateOffset(Datas, immediately: false);
		await AnimateOffset(movedown, 100.0);
		_loadCountCts?.Cancel();
		scrollFinishDebouncer?.Trigger();
		scrollDataCollectDebouncer?.Trigger();
	}

	public void OnScrollDataCollect()
	{
		UserActCollectUtils.AddClickEvent("calendar", "year_action", "swipe_vertical");
	}

	public void Reshape(int originRowCount, int originCountEachRow, int newRowCount)
	{
		int num = originRowCount * originCountEachRow;
		int newCountEachRow = num / newRowCount;
		if (newRowCount <= 0 || newCountEachRow <= 0)
		{
			UtilLog.Error($"无效的新行列数: rows={newRowCount}, perRow={newCountEachRow}");
			return;
		}
		if (num % newRowCount != 0)
		{
			UtilLog.Error($"无法将 {num} 个月均匀分配到 {newRowCount} 行");
			return;
		}
		List<YearHeaderItem> headers = Datas.OfType<YearHeaderItem>().ToList();
		List<YearMonthRowItem> list = Datas.OfType<YearMonthRowItem>().ToList();
		List<(YearViewMonthData, bool, DateTime)> list2 = new List<(YearViewMonthData, bool, DateTime)>();
		foreach (YearMonthRowItem item4 in list)
		{
			foreach (YearViewMonthData item5 in item4.Items)
			{
				list2.Add((item5, item4.IsViewStart, item5.MonthStart));
			}
		}
		if (!list2.Any())
		{
			UtilLog.Warn("没有可用的月份数据，跳过重排");
			return;
		}
		List<(YearViewMonthData, bool, DateTime)> source = list2.OrderBy<(YearViewMonthData, bool, DateTime), DateTime>(((YearViewMonthData data, bool isViewStart, DateTime start) x) => x.start).ToList();
		DateTime item = source.First().Item3;
		ILookup<(int, int), (YearViewMonthData, bool)> lookup = source.ToLookup<(YearViewMonthData, bool, DateTime), (int, int), (YearViewMonthData, bool)>(((YearViewMonthData data, bool isViewStart, DateTime start) x) => (Year: x.data.Year, Month: x.data.Month), ((YearViewMonthData data, bool isViewStart, DateTime start) x) => (data: x.data, isViewStart: x.isViewStart));
		HashSet<(int, int)> hashSet = new HashSet<(int, int)>(list.FirstOrDefault((YearMonthRowItem r) => r.IsViewStart)?.Items.Select((YearViewMonthData yearViewMonthData2) => (Year: yearViewMonthData2.Year, Month: yearViewMonthData2.Month))) ?? new HashSet<(int, int)>();
		if (!hashSet.Any())
		{
			YearMonthRowItem yearMonthRowItem = list.FirstOrDefault();
			hashSet = ((yearMonthRowItem == null) ? new HashSet<(int, int)>() : new HashSet<(int, int)>(yearMonthRowItem.Items.Select((YearViewMonthData yearViewMonthData2) => (Year: yearViewMonthData2.Year, Month: yearViewMonthData2.Month))));
		}
		List<YearMonthRowItem> outputRows = new List<YearMonthRowItem>();
		(int year, int month) tuple = GetGroupStart(item);
		int num2 = tuple.year;
		int num3 = tuple.month;
		int? num4 = null;
		int num5 = -1;
		while (true)
		{
			List<YearViewMonthData> list3 = new List<YearViewMonthData>();
			bool flag = false;
			int num6 = 0;
			for (int num7 = 0; num7 < newCountEachRow; num7++)
			{
				int num8 = num2;
				int num9;
				for (num9 = num3 + num7; num9 > 12; num9 -= 12)
				{
					num8++;
				}
				(int, int) key = (num8, num9);
				List<(YearViewMonthData, bool)> source2 = lookup[key].ToList();
				YearViewMonthData yearViewMonthData;
				if (source2.Any())
				{
					yearViewMonthData = source2.First().Item1;
					if (hashSet.Contains((yearViewMonthData.Year, yearViewMonthData.Month)))
					{
						num6++;
					}
					flag = true;
				}
				else
				{
					DateTime monthStart = new DateTime(num8, num9, 1);
					yearViewMonthData = LoadMonth(monthStart);
				}
				list3.Add(yearViewMonthData);
			}
			if (!flag)
			{
				break;
			}
			int count = outputRows.Count;
			YearMonthRowItem orCreateRow = GetOrCreateRow(count, new List<YearMonthRowItem>(list));
			orCreateRow.Items.Clear();
			foreach (YearViewMonthData item6 in list3)
			{
				orCreateRow.Items.Add(item6);
			}
			if (num6 > num5)
			{
				num5 = num6;
				num4 = count;
			}
			outputRows.Add(orCreateRow);
			num3 += newCountEachRow;
			if (num3 > 12)
			{
				num2 += (num3 - 1) / 12;
				num3 = (num3 - 1) % 12 + 1;
			}
		}
		for (int num10 = 0; num10 < outputRows.Count; num10++)
		{
			outputRows[num10].IsViewStart = num10 == num4;
		}
		Application.Current.Dispatcher.Invoke(delegate
		{
			List<YearViewItem> list4 = new List<YearViewItem>();
			double num11 = 0.0;
			Dictionary<int, YearHeaderItem> dictionary = headers.ToDictionary((YearHeaderItem h) => h.Year, (YearHeaderItem h) => h);
			HashSet<int> hashSet2 = new HashSet<int>();
			foreach (YearMonthRowItem item7 in outputRows)
			{
				YearViewMonthData yearViewMonthData2 = item7.Items.FirstOrDefault();
				if (yearViewMonthData2 != null && yearViewMonthData2.Month == 1 && !hashSet2.Contains(yearViewMonthData2.Year))
				{
					YearHeaderItem value;
					YearHeaderItem yearHeaderItem = ((!dictionary.TryGetValue(yearViewMonthData2.Year, out value)) ? new YearHeaderItem(yearViewMonthData2.Year) : value);
					yearHeaderItem.Offset = num11;
					list4.Add(yearHeaderItem);
					num11 += YearLineHeight + YearBottomSpacing;
					hashSet2.Add(yearViewMonthData2.Year);
				}
				item7.Offset = num11;
				list4.Add(item7);
				num11 += EachMonthRowHeight;
			}
			Datas.Clear();
			foreach (YearViewItem item8 in list4)
			{
				Datas.Add(item8);
			}
		});
		(int year, int month) GetGroupStart(DateTime dt)
		{
			int num11 = ((dt.Year - 1) * 12 + dt.Month - 1) / newCountEachRow * newCountEachRow + 1;
			int item2 = (num11 - 1) / 12 + 1;
			int item3 = (num11 - 1) % 12 + 1;
			return (year: item2, month: item3);
		}
	}

	private YearMonthRowItem GetOrCreateRow(int index, List<YearMonthRowItem> sourceRows)
	{
		if (index < sourceRows.Count)
		{
			return sourceRows[index];
		}
		return new YearMonthRowItem();
	}

	public void Dispose()
	{
		PropertyChangedEventManager.RemoveHandler(LocalSettings.Settings, OnShowWeekChange, "ShowWeek");
		DataChangedNotifier.ThemeModeChanged -= OnThemeChanged;
		DataChangedNotifier.CalendarShowYearHeatMapChanged -= OnShowYearHeatMapChanged;
		DataChangedNotifier.WeekStartFromChanged -= OnWeekStartFromChanged;
		TimeChangeNotifier.DayChanged -= TimeChangeNotifier_DayChanged;
		LocalSettings settings = LocalSettings.Settings;
		settings.OnPreferenceChanged = (Action)Delegate.Remove(settings.OnPreferenceChanged, new Action(InitFromConfig));
		scrollDataCollectDebouncer?.Dispose();
		scrollFinishDebouncer?.Dispose();
		gcDebouncer?.Dispose();
	}
}
