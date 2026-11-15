using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Calendar.Week;

namespace ticktick_WPF.Views.Calendar;

public class TimeLine : UserControl, IComponentConnector
{
	public bool TopHandleDragging;

	public bool BottomHandleDragging;

	private Dictionary<string, TimeLineBlock[]> _blocksDict;

	private List<TimeOffset> _items;

	private DateTime? _date;

	public static readonly DependencyProperty TopFoldHoverProperty = DependencyProperty.Register("TopFoldHover", typeof(bool), typeof(TimeLine), new PropertyMetadata(false));

	public static readonly DependencyProperty BottomFoldHoverProperty = DependencyProperty.Register("BottomFoldHover", typeof(bool), typeof(TimeLine), new PropertyMetadata(false));

	internal Grid Containner;

	internal StackPanel ItemsPanel;

	internal Border TopExpandBorder;

	internal Border BottomExpandBorder;

	private bool _contentLoaded;

	public bool TopFoldHover
	{
		get
		{
			return (bool)GetValue(TopFoldHoverProperty);
		}
		set
		{
			SetValue(TopFoldHoverProperty, value);
		}
	}

	public bool BottomFoldHover
	{
		get
		{
			return (bool)GetValue(BottomFoldHoverProperty);
		}
		set
		{
			SetValue(BottomFoldHoverProperty, value);
		}
	}

	public TimeLine()
	{
		InitializeComponent();
		InitItems();
		InitBoard(checkNow: false, _date);
		TimeLineViewModel dataContext = new TimeLineViewModel
		{
			StartDate = DateTime.Today
		};
		base.DataContext = dataContext;
		base.Loaded += delegate
		{
			CalendarGeoHelper.TopFoldedChanged += OnTopFoldedChanged;
		};
		base.Unloaded += delegate
		{
			CalendarGeoHelper.TopFoldedChanged -= OnTopFoldedChanged;
		};
		TopExpandBorder.Visibility = (CalendarGeoHelper.TopFolded ? Visibility.Collapsed : Visibility.Visible);
		BottomExpandBorder.Visibility = (CalendarGeoHelper.TopFolded ? Visibility.Collapsed : Visibility.Visible);
	}

	private void OnTopFoldedChanged(object sender, EventArgs e)
	{
		TopExpandBorder.Visibility = (CalendarGeoHelper.TopFolded ? Visibility.Collapsed : Visibility.Visible);
		BottomExpandBorder.Visibility = (CalendarGeoHelper.TopFolded ? Visibility.Collapsed : Visibility.Visible);
	}

	private void InitItems()
	{
		ItemsPanel.Children.Clear();
		_blocksDict = new Dictionary<string, TimeLineBlock[]>();
		CalTimelineTimeZones calendarDisplayTimeZones = LocalSettings.Settings.CalendarDisplayTimeZones;
		if (LocalSettings.Settings.EnableCalendarTimeZone && calendarDisplayTimeZones != null && calendarDisplayTimeZones.additional?.Count > 0)
		{
			for (int num = calendarDisplayTimeZones.additional.Count - 1; num >= 0; num--)
			{
				CalTimeZone calTimeZone = calendarDisplayTimeZones.additional[num];
				if (TimeZoneUtils.GetTimeZoneInfo(calTimeZone.timeZone) != null)
				{
					List<string> hiddenCalTimeZones = LocalSettings.Settings.ExtraSettings.HiddenCalTimeZones;
					if (hiddenCalTimeZones == null || !hiddenCalTimeZones.Contains(calTimeZone.timeZone))
					{
						TimeLineBlock[] array = new TimeLineBlock[26];
						StackPanel stackPanel = new StackPanel
						{
							VerticalAlignment = VerticalAlignment.Top,
							Width = 56.0
						};
						for (int i = 0; i < 26; i++)
						{
							TimeLineBlock element = (array[i] = new TimeLineBlock());
							stackPanel.Children.Add(element);
						}
						ItemsPanel.Children.Add(stackPanel);
						_blocksDict[calTimeZone.timeZone + num] = array;
					}
				}
			}
		}
		TimeLineBlock[] array2 = new TimeLineBlock[26];
		StackPanel stackPanel2 = new StackPanel
		{
			VerticalAlignment = VerticalAlignment.Top,
			Width = 56.0
		};
		for (int j = 0; j < 26; j++)
		{
			TimeLineBlock element2 = (array2[j] = new TimeLineBlock());
			stackPanel2.Children.Add(element2);
		}
		ItemsPanel.Children.Add(stackPanel2);
		_blocksDict["local"] = array2;
		base.Width = (double)_blocksDict.Count * 56.0;
	}

	public void InitBoard(bool checkNow = false, DateTime? date = null)
	{
		if (!date.HasValue)
		{
			date = DateTime.Today;
		}
		_date = date;
		List<TimeOffset> list = ((!CalendarGeoHelper.TopFolded) ? InitFullBoard() : InitCollapsedBoard());
		foreach (TimeOffset item in list)
		{
			item.BaseOffset = item.Offset;
			item.DateTime = date.Value.AddMinutes(item.Offset);
		}
		List<TimeOffset> list2 = new List<TimeOffset>();
		foreach (string key in _blocksDict.Keys)
		{
			TimeLineBlock[] array = _blocksDict[key];
			TimeZoneInfo timeZoneInfo = ((key == "local") ? TimeZoneInfo.Local : TimeZoneUtils.GetTimeZoneInfo(key.Substring(0, key.Length - 1)));
			int num = (int)(timeZoneInfo.BaseUtcOffset - TimeZoneInfo.Local.BaseUtcOffset).TotalMinutes;
			bool checkNow2 = key == "local" && checkNow;
			bool showDrag = key == "local";
			for (int i = 0; i < array.Length; i++)
			{
				array[i].CheckNow = checkNow2;
				array[i].CheckCn = num % 60 == 0;
				if (i < list.Count)
				{
					array[i].Visibility = Visibility.Visible;
					TimeOffset timeOffset = list[i].Clone();
					timeOffset.TimeZone = timeZoneInfo;
					timeOffset.ShowDrag = showDrag;
					array[i].DataContext = timeOffset;
					list2.Add(timeOffset);
				}
				else
				{
					array[i].Visibility = Visibility.Collapsed;
					array[i].DataContext = new TimeOffset();
				}
			}
		}
		_items = list2;
	}

	private List<TimeOffset> InitCollapsedBoard()
	{
		List<TimeOffset> list = new List<TimeOffset>();
		int startHour = CalendarGeoHelper.GetStartHour(display: false);
		int endHour = CalendarGeoHelper.GetEndHour(display: false);
		list.Add(new TimeOffset
		{
			Offset = 0,
			Folded = true
		});
		list.Add(new TimeOffset
		{
			Offset = startHour * 60,
			Folded = true,
			ShowDivider = true
		});
		list.Add(new TimeOffset
		{
			Offset = (startHour + 1) * 60,
			IsFirst = true
		});
		for (int i = startHour + 1; i < endHour; i++)
		{
			list.Add(new TimeOffset
			{
				Offset = i * 60
			});
		}
		list.Add(new TimeOffset
		{
			Offset = endHour * 60,
			Folded = true
		});
		if (endHour < 24)
		{
			list.Add(new TimeOffset
			{
				Offset = 1440,
				Folded = true,
				ShowDivider = true
			});
		}
		return list;
	}

	private List<TimeOffset> InitFullBoard()
	{
		int startHour = CalendarGeoHelper.GetStartHour(display: false);
		int endHour = CalendarGeoHelper.GetEndHour(display: false);
		List<TimeOffset> list = new List<TimeOffset>();
		for (int i = 0; i < 25; i++)
		{
			TimeOffset timeOffset = new TimeOffset
			{
				Offset = i * 60
			};
			bool foldExpanded = (i >= 0 && i < startHour) || i >= endHour;
			bool showTopHandle = i == startHour;
			bool showBottomHandle = i == endHour - 1;
			timeOffset.FoldExpanded = foldExpanded;
			timeOffset.ShowTopHandle = showTopHandle;
			timeOffset.ShowBottomHandle = showBottomHandle;
			list.Add(timeOffset);
		}
		return list;
	}

	public void ExpandTimeline(TimeOffset model)
	{
		CalendarGeoHelper.TopFolded = false;
		CalendarGeoHelper.NotifyTopUnFolded(model.BaseOffset / 60 < LocalSettings.Settings.CollapsedEnd);
	}

	public void NotifyHandleDragged(int offset, bool isTop)
	{
		if (_items == null)
		{
			return;
		}
		_items.ForEach(delegate(TimeOffset item)
		{
			if (isTop)
			{
				item.ShowTopHandle = item.BaseOffset == offset + 60;
				CalendarGeoHelper.SetStartHour(offset / 60 + 1);
			}
			else
			{
				item.ShowBottomHandle = item.BaseOffset == offset;
				CalendarGeoHelper.SetEndHour(offset / 60 + 1);
			}
			if (isTop)
			{
				int? num = _items.FirstOrDefault((TimeOffset model) => model.ShowBottomHandle)?.BaseOffset;
				item.FoldExpanded = item.BaseOffset <= offset || item.BaseOffset > num;
			}
			else
			{
				int? num2 = _items.FirstOrDefault((TimeOffset model) => model.ShowTopHandle)?.BaseOffset;
				item.FoldExpanded = item.BaseOffset > offset || item.BaseOffset < num2;
			}
		});
	}

	public int GetBottomOffset()
	{
		if (_items != null)
		{
			int? num = _items.FirstOrDefault((TimeOffset model) => model.ShowBottomHandle)?.BaseOffset;
			if (num.HasValue)
			{
				return num.Value;
			}
		}
		return 0;
	}

	public int GetTopOffset()
	{
		if (_items != null)
		{
			int? num = _items.FirstOrDefault((TimeOffset model) => model.ShowTopHandle)?.BaseOffset;
			if (num.HasValue)
			{
				return num.Value;
			}
		}
		return 0;
	}

	public void OnHandleDrop(TimeOffset offset, bool mouseDown)
	{
		if (BottomHandleDragging || TopHandleDragging)
		{
			mouseDown = false;
		}
		BottomHandleDragging = false;
		TopHandleDragging = false;
	}

	private void OnFoldClick(object sender, MouseButtonEventArgs e)
	{
		CalendarGeoHelper.TopFolded = true;
	}

	public void SetTryDrag(bool tryDrag)
	{
		if (tryDrag)
		{
			TopExpandBorder.IsHitTestVisible = false;
			BottomExpandBorder.IsHitTestVisible = false;
		}
		else
		{
			TopExpandBorder.IsHitTestVisible = true;
			BottomExpandBorder.IsHitTestVisible = true;
		}
	}

	private void OnBorderEnter(object sender, MouseEventArgs e)
	{
		if (sender is Border border)
		{
			if (border.Equals(TopExpandBorder))
			{
				TopFoldHover = true;
				BottomFoldHover = false;
			}
			else if (border.Equals(BottomExpandBorder))
			{
				BottomFoldHover = true;
				TopFoldHover = false;
			}
			Utils.FindParent<CalendarTimelineView>(this)?.ShowOrHideFoldTooltip(show: true);
		}
	}

	private void OnBorderLeave(object sender, MouseEventArgs e)
	{
		if (sender is Border border)
		{
			if (border.Equals(TopExpandBorder))
			{
				TopFoldHover = false;
			}
			else if (border.Equals(BottomExpandBorder))
			{
				BottomFoldHover = false;
			}
			Utils.FindParent<CalendarTimelineView>(this)?.ShowOrHideFoldTooltip(show: false);
		}
	}

	public void Refresh()
	{
		List<string> displayTimelineTimeZoneNames = CalendarGeoHelper.GetDisplayTimelineTimeZoneNames();
		if (displayTimelineTimeZoneNames.Count == _blocksDict.Count)
		{
			List<string> list = _blocksDict.Keys.ToList();
			bool flag = false;
			for (int i = 0; i < displayTimelineTimeZoneNames.Count - 1; i++)
			{
				if (displayTimelineTimeZoneNames[i] != list[i])
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return;
			}
		}
		InitItems();
		InitBoard(Utils.FindParent<CalendarTimelineView>(this)?.ShowPointer() ?? false, _date);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/calendar/timeline.xaml", UriKind.Relative);
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
			Containner = (Grid)target;
			break;
		case 2:
			ItemsPanel = (StackPanel)target;
			break;
		case 3:
			TopExpandBorder = (Border)target;
			TopExpandBorder.MouseLeftButtonUp += OnFoldClick;
			TopExpandBorder.MouseEnter += OnBorderEnter;
			TopExpandBorder.MouseLeave += OnBorderLeave;
			break;
		case 4:
			BottomExpandBorder = (Border)target;
			BottomExpandBorder.MouseLeftButtonUp += OnFoldClick;
			BottomExpandBorder.MouseEnter += OnBorderEnter;
			BottomExpandBorder.MouseLeave += OnBorderLeave;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
