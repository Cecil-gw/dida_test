using Newtonsoft.Json;
using TickTickUtils.Lang;
using ticktick_WPF.Notifier;

namespace ticktick_WPF.Models;

public class TimeTableModel : PreferenceBaseModel
{
	public string currentTimetableId { get; set; }

	public bool displayInCalendar { get; set; }

	public bool isEnabled { get; set; }

	public bool displayInSmartProjects { get; set; }

	[JsonIgnore]
	public bool ShowInCal
	{
		get
		{
			if (isEnabled)
			{
				return displayInCalendar;
			}
			return false;
		}
	}

	[JsonIgnore]
	public bool ShowInSmart
	{
		get
		{
			if (isEnabled)
			{
				return displayInSmartProjects;
			}
			return false;
		}
	}

	public override bool SetRemoteValue(PreferenceBaseModel model)
	{
		if (model is TimeTableModel timeTableModel)
		{
			if (timeTableModel.mtime > base.mtime)
			{
				currentTimetableId = timeTableModel.currentTimetableId;
				displayInCalendar = timeTableModel.displayInCalendar;
				displayInSmartProjects = timeTableModel.displayInSmartProjects;
				isEnabled = timeTableModel.isEnabled;
				base.mtime = timeTableModel.mtime;
				DataChangedNotifier.OnScheduleChanged();
				UtilLog.Info("TimeTableModel.SetRemoteValue");
			}
			else if (timeTableModel.mtime < base.mtime)
			{
				return true;
			}
		}
		return false;
	}
}
