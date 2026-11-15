using System;
using System.Collections.Generic;
using System.Linq;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Calendar;

public class CalendarTimeZoneConfigItemViewModel : SortableListItemViewModel
{
	public TimeZoneViewModel TimeZone { get; set; }

	public string Emoji { get; set; }

	public string Label { get; set; }

	public bool Editable { get; set; } = true;

	public bool IsLocal { get; set; }

	public bool CanDelete
	{
		get
		{
			if (Editable)
			{
				return !ParentDragging;
			}
			return false;
		}
	}

	public bool CanDrag
	{
		get
		{
			if (Editable && base.CanSort)
			{
				return !ParentDragging;
			}
			return false;
		}
		set
		{
			base.CanSort = value;
			OnPropertyChanged("CanDrag");
		}
	}

	public CalendarTimeZoneConfigItemViewModel()
	{
		base.CanSort = false;
	}

	private CalendarTimeZoneConfigItemViewModel(TimeZoneViewModel tz, string tzLabel)
	{
		TimeZone = tz;
		Emoji = EmojiHelper.GetFirstEmoji(tzLabel);
		Label = ((Emoji != null) ? tzLabel.Substring(Emoji.Length) : tzLabel);
	}

	public override void OnParentDraggingChanged()
	{
		OnPropertyChanged("CanDelete");
		OnPropertyChanged("CanDrag");
	}

	public static List<CalendarTimeZoneConfigItemViewModel> BuildViewModels()
	{
		List<CalendarTimeZoneConfigItemViewModel> list = new List<CalendarTimeZoneConfigItemViewModel>();
		TimeZoneViewModel local = TimeZoneData.LocalTimeZoneModel;
		CalTimelineTimeZones calendarDisplayTimeZones = LocalSettings.Settings.CalendarDisplayTimeZones;
		list.Add(new CalendarTimeZoneConfigItemViewModel(local, calendarDisplayTimeZones?.current?.FirstOrDefault((CalTimeZone t) => t.timeZone == local.TimeZoneName)?.label)
		{
			Editable = false,
			CanSort = false,
			IsLocal = true
		});
		if (calendarDisplayTimeZones != null && calendarDisplayTimeZones.additional?.Count > 0)
		{
			foreach (CalTimeZone item in calendarDisplayTimeZones.additional)
			{
				TimeZoneInfo timeZoneInfo = TimeZoneUtils.GetTimeZoneInfo(item.timeZone);
				if (timeZoneInfo != null)
				{
					list.Add(new CalendarTimeZoneConfigItemViewModel(new TimeZoneViewModel(timeZoneInfo), item.label));
				}
			}
		}
		return list;
	}
}
