using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Calendar;

public class WeekMonthViewModel : BaseViewModel
{
	public const string Month = "0";

	public const string Week = "1";

	public const string Day = "2";

	public const string Agenda = "8";

	public const string Year = "10";

	private string _mode = "0";

	private string _displayText;

	public string Mode
	{
		get
		{
			return _mode;
		}
		set
		{
			_mode = value;
			OnPropertyChanged("Mode");
			SetDisplayText();
		}
	}

	public string DisplayText
	{
		get
		{
			return _displayText;
		}
		set
		{
			_displayText = value;
			OnPropertyChanged("DisplayText");
		}
	}

	private void SetDisplayText()
	{
		string mode = Mode;
		bool flag = mode.StartsWith("D");
		if (mode.StartsWith("W") || flag)
		{
			if (int.TryParse(mode.Substring(1), out var result))
			{
				if (flag)
				{
					DisplayText = result + " " + Utils.GetString((result > 1) ? "PublicDays" : "PublicDay");
				}
				else
				{
					DisplayText = result + " " + Utils.GetString((result > 1) ? "PublicWeeks" : "PublicWeek");
				}
			}
			return;
		}
		switch (mode)
		{
		case "8":
			DisplayText = Utils.GetString("Agenda");
			break;
		case "2":
			DisplayText = Utils.GetString("Day");
			break;
		case "1":
			DisplayText = Utils.GetString("Week");
			break;
		case "0":
			DisplayText = Utils.GetString("Month");
			break;
		case "10":
			DisplayText = Utils.GetString("TimelineYear");
			break;
		}
	}
}
