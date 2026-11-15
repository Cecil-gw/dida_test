using ticktick_WPF.Resource;

namespace ticktick_WPF.Views.Calendar;

public class CalendarDisplaySettings
{
	public CalendarSettingModel MainCal = new CalendarSettingModel();

	public CalendarSettingModel CalWidget = new CalendarSettingModel();

	public CalendarSettingModel MainCalInList = new CalendarSettingModel
	{
		HeadSwitch = CalendarViewType.Day
	};

	public CalendarSettingModel ExtraWindowCal = new CalendarSettingModel();

	private CalendarSettingModel GetCalModel(CalendarViewSourceType type)
	{
		return type switch
		{
			CalendarViewSourceType.Main => MainCal, 
			CalendarViewSourceType.WithList => MainCalInList, 
			CalendarViewSourceType.Widget => CalWidget, 
			CalendarViewSourceType.ExtraWindow => ExtraWindowCal, 
			_ => MainCal, 
		};
	}

	public void SetHeadSwitch(CalendarViewSourceType type, CalendarViewType val, int num = 0)
	{
		CalendarSettingModel calModel = GetCalModel(type);
		calModel.HeadSwitch = val;
		calModel.MultiNum = num;
		LocalSettings.Settings.Save(force: true);
	}

	public void SetShowMiniCal(CalendarViewSourceType type, bool val)
	{
		GetCalModel(type).ShowMiniCal = val;
		LocalSettings.Settings.Save(force: true);
	}

	public void SetMiniCalWidth(CalendarViewSourceType type, double val)
	{
		GetCalModel(type).MiniCalWidth = val;
		LocalSettings.Settings.Save(force: true);
	}

	public void SetArrangeWidth(CalendarViewSourceType type, double val)
	{
		GetCalModel(type).ArrangeWidth = val;
		LocalSettings.Settings.Save(force: true);
	}

	public void SetShowArrange(CalendarViewSourceType type, bool val)
	{
		GetCalModel(type).ShowArrange = val;
		LocalSettings.Settings.Save(force: true);
	}

	public CalendarViewType GetHeadSwitch(CalendarViewSourceType type)
	{
		return GetCalModel(type).HeadSwitch;
	}

	public int GetMultiNum(CalendarViewSourceType type)
	{
		return GetCalModel(type).MultiNum;
	}

	public string GetHeadMode(CalendarViewSourceType type)
	{
		return GetCalModel(type).GetSwitchMode();
	}

	public bool ShowArrange(CalendarViewSourceType type)
	{
		return GetCalModel(type).ShowArrange;
	}

	public double ArrangeWidth(CalendarViewSourceType type)
	{
		return GetCalModel(type).ArrangeWidth;
	}

	public double MiniCalWidth(CalendarViewSourceType type)
	{
		return GetCalModel(type).MiniCalWidth;
	}

	public bool ShowMiniCal(CalendarViewSourceType type)
	{
		return GetCalModel(type).ShowMiniCal;
	}

	public void CheckNull()
	{
		if (MainCal == null)
		{
			MainCal = new CalendarSettingModel();
		}
		if (CalWidget == null)
		{
			CalWidget = new CalendarSettingModel();
		}
	}
}
