using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.ViewModels;

public class CourseItemViewModel : BaseViewModel
{
	public string WeekText { get; set; }

	public string LessonText { get; set; }

	public string Room { get; set; }

	public string Teacher { get; set; }

	public string TimeTitle { get; set; }

	public Visibility ShowTimeTitle
	{
		get
		{
			if (string.IsNullOrEmpty(TimeTitle))
			{
				return Visibility.Collapsed;
			}
			return Visibility.Visible;
		}
	}

	public Visibility ShowRoom
	{
		get
		{
			if (string.IsNullOrEmpty(Room))
			{
				return Visibility.Collapsed;
			}
			return Visibility.Visible;
		}
	}

	public Visibility ShowTeacher
	{
		get
		{
			if (string.IsNullOrEmpty(Teacher))
			{
				return Visibility.Collapsed;
			}
			return Visibility.Visible;
		}
	}

	public CourseItemViewModel(CourseDetailModel item, int index)
	{
		Room = item.Room;
		Teacher = item.Teacher;
		TimeTitle = ((index > 0) ? string.Format(Utils.GetString("CourseTime"), index) : null);
		LessonText = DateUtils.GetWeekTextByWeekDay(item.Weekday) + ", " + string.Format(Utils.GetString("LessonNum"), (item.StartLesson == item.EndLesson) ? (item.StartLesson.ToString() ?? "") : (item.StartLesson + " - " + item.EndLesson));
		WeekText = GetWeekText(item.Weeks);
	}

	private static string GetWeekText(int[] weeks)
	{
		if (weeks == null || weeks.Length == 0)
		{
			return null;
		}
		if (weeks.Length == 1)
		{
			return string.Format(Utils.GetString("InWeekNum"), weeks[0]);
		}
		List<int> list = weeks.OrderBy((int week) => week).ToList();
		int num = list.Last();
		int num2 = list.First();
		if (num - num2 + 1 == weeks.Length)
		{
			return string.Format(Utils.GetString("InWeekNum"), num2 + " - " + num);
		}
		bool flag = true;
		for (int num3 = 0; num3 < list.Count - 1; num3++)
		{
			if (list[num3 + 1] - list[num3] != 2)
			{
				flag = false;
				break;
			}
		}
		if (flag)
		{
			return string.Format(Utils.GetString("InWeekNum"), num2 + " - " + num) + " (" + Utils.GetString((list[0] % 2 == 1) ? "Odd" : "Even") + ")";
		}
		int num4 = -1;
		int num5 = -1;
		string text = string.Empty;
		foreach (int item in list)
		{
			if (num4 == -1)
			{
				num4 = item;
				num5 = item;
			}
			else if (num5 + 1 == item)
			{
				num5 = item;
			}
			else
			{
				text = text + (string.IsNullOrEmpty(text) ? "" : ", ") + string.Format(Utils.GetString("InWeekNum"), (num4 == num5) ? (num4 + string.Empty) : (num4 + " - " + num5));
				num4 = item;
				num5 = item;
			}
		}
		if (num4 != -1)
		{
			text = text + (string.IsNullOrEmpty(text) ? "" : ", ") + string.Format(Utils.GetString("InWeekNum"), (num4 == num5) ? (num4 + string.Empty) : (num4 + " - " + num5));
		}
		return text;
	}
}
