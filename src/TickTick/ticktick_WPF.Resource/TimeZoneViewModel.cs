using System;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Resource;

public class TimeZoneViewModel : BaseViewModel
{
	private bool _selected;

	public TimeZoneInfo TimeZone { get; set; }

	public string DisplayName { get; set; }

	public string TimeZoneName { get; set; }

	public bool IsFloat { get; set; }

	public bool IsSplit { get; set; }

	public bool Selected
	{
		get
		{
			return _selected;
		}
		set
		{
			if (_selected != value)
			{
				_selected = value;
				OnPropertyChanged("Selected");
			}
		}
	}

	public TimeZoneViewModel()
	{
	}

	public TimeZoneViewModel(TimeZoneInfo tz)
	{
		TimeZone = tz;
		try
		{
			DisplayName = TimeZoneUtils.GetTimeZoneDisplayName(tz);
			TimeZoneName = TimeZoneUtils.GetTimeZoneName(tz);
		}
		catch (Exception)
		{
			DisplayName = tz?.DisplayName;
			TimeZoneName = string.Empty;
		}
		IsFloat = false;
	}

	public TimeZoneViewModel(bool isFloat, string tzName)
	{
		IsFloat = isFloat;
		if (!string.IsNullOrEmpty(tzName))
		{
			TimeZone = TimeZoneUtils.GetTimeZoneInfo(tzName);
			DisplayName = (IsFloat ? Utils.GetString("TimeRemainsUnchanged") : TimeZoneUtils.GetTimeZoneDisplayName(TimeZone));
			TimeZoneName = tzName;
		}
	}

	public static TimeZoneViewModel GetFloatModel()
	{
		return new TimeZoneViewModel
		{
			TimeZone = TimeZoneData.LocalTimeZoneModel.TimeZone,
			TimeZoneName = TimeZoneData.LocalTimeZoneModel.TimeZoneName,
			DisplayName = Utils.GetString("TimeRemainsUnchanged"),
			IsFloat = true
		};
	}

	public static string GetShortName(string displayName)
	{
		if (!string.IsNullOrEmpty(displayName))
		{
			int num = displayName.IndexOf("，", StringComparison.Ordinal);
			if (num == -1)
			{
				num = displayName.IndexOf(",", StringComparison.Ordinal);
			}
			if (num > 0)
			{
				return displayName.Remove(num);
			}
		}
		return displayName;
	}
}
