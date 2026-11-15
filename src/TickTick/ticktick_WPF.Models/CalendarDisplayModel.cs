using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using ticktick_WPF.Cache;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Models;

public class CalendarDisplayModel
{
	public double repeatDiff;

	public bool IsVirtual;

	public CourseDisplayModel CourseDisplayModel;

	public CountdownModel CountdownModel;

	private static List<CalendarDisplayModel> visualModels;

	public TaskBaseViewModel SourceViewModel { get; set; }

	public DateTime? StartDate
	{
		get
		{
			DateTime? startDate = SourceViewModel.StartDate;
			if (!startDate.HasValue)
			{
				if (!ToPoint)
				{
					return null;
				}
				return SourceViewModel.CompletedTime;
			}
			return startDate;
		}
	}

	public DateTime? DueDate
	{
		get
		{
			if (!ToPoint)
			{
				return SourceViewModel.DueDate;
			}
			return null;
		}
	}

	public DateTime? DisplayStartDate
	{
		get
		{
			if (!SourceViewModel.StartDate.HasValue)
			{
				if (!ToPoint)
				{
					return null;
				}
				return SourceViewModel.CompletedTime?.AddDays(repeatDiff + (ExtraDiff?.TotalDays ?? 0.0));
			}
			return ToPoint ? SourceViewModel.StartDate.Value.Date.AddDays(repeatDiff + (ExtraDiff?.TotalDays ?? 0.0)) : SourceViewModel.StartDate.Value.AddDays(repeatDiff + (ExtraDiff?.TotalDays ?? 0.0));
		}
	}

	public DateTime? DisplayDueDate
	{
		get
		{
			if (!ToPoint)
			{
				return SourceViewModel.DueDate?.AddDays(repeatDiff + (ExtraDiff?.TotalDays ?? 0.0));
			}
			return null;
		}
	}

	public bool? IsAllDay
	{
		get
		{
			if (!ToPoint)
			{
				return SourceViewModel.IsAllDay;
			}
			return false;
		}
	}

	public string Title => SourceViewModel.Title;

	public DateTime? CompletedTime => SourceViewModel.CompletedTime;

	public int Status => SourceViewModel.Status;

	public int Priority => SourceViewModel.Priority;

	public DisplayType Type => SourceViewModel.Type;

	public long SortOrder => SourceViewModel.SortOrder;

	public string ProjectId => SourceViewModel.ProjectId;

	public string Id => SourceViewModel.Id;

	public string EventTaskId { get; set; }

	public HabitCheckInModel CheckIn { get; set; }

	public TimeSpan? ExtraDiff { get; set; }

	public bool CanBatchSelect
	{
		get
		{
			if (SourceViewModel.IsTaskOrNote && repeatDiff < 1.0)
			{
				return SourceViewModel.Editable;
			}
			return false;
		}
	}

	public bool ToPoint { get; set; }

	public bool IsDragDisplayItem { get; set; }

	private CalendarDisplayModel()
	{
	}

	public CalendarDisplayModel(string title, string id, string color, DateTime startDate, DateTime? endTime, bool isAllDay, int status, int priority = 0, long sortOrder = 0L)
	{
		IsVirtual = true;
		SourceViewModel = new TaskBaseViewModel
		{
			Title = title,
			Id = id,
			StartDate = startDate,
			DueDate = endTime,
			Color = color,
			IsAllDay = isAllDay,
			Status = status,
			CompletedTime = ((status == 0) ? ((DateTime?)null) : new DateTime?(startDate)),
			Priority = priority,
			SortOrder = sortOrder
		};
	}

	public CalendarDisplayModel(CountdownModel countdown, DateTime targetDate)
	{
		CountdownModel = countdown;
		SourceViewModel = new CountDownBaseViewModel(countdown)
		{
			Title = countdown.Name,
			Id = countdown.Id + targetDate.ToString("yyyyMMddHHmmss"),
			StartDate = targetDate,
			DueDate = null,
			IsAllDay = true,
			Status = 0,
			CompletedTime = ((targetDate < DateTime.Today) ? ((DateTime?)null) : new DateTime?(targetDate)),
			Type = DisplayType.Countdown
		};
		if (countdown.CheckActualCanShowAge(DisplayTimerMode.CountDown))
		{
			SourceViewModel.Title += countdown.GetAge(targetDate);
		}
	}

	public CalendarDisplayModel(CountdownModel countdown)
	{
		CountdownModel = countdown;
		DateTime dateAsDateTime = countdown.GetDateAsDateTime();
		SourceViewModel = new CountDownBaseViewModel(countdown)
		{
			Title = countdown.Name,
			Id = countdown.Id,
			StartDate = dateAsDateTime,
			DueDate = null,
			IsAllDay = true,
			Status = 0,
			CompletedTime = ((dateAsDateTime < DateTime.Today) ? ((DateTime?)null) : new DateTime?(dateAsDateTime)),
			Type = DisplayType.Countdown
		};
	}

	public CalendarDisplayModel(PomodoroModel pomo)
	{
		if (pomo == null)
		{
			return;
		}
		TaskBaseViewModel taskBaseViewModel = null;
		string id = pomo.Id;
		string title;
		if (pomo.Tasks == null || pomo.Tasks.Length == 0)
		{
			title = Utils.GetString("FocusTime");
		}
		else if (pomo.Tasks.Length == 1)
		{
			string title2 = pomo.Tasks[0].GetTitle();
			title = (string.IsNullOrEmpty(title2) ? Utils.GetString("FocusTime") : title2);
			taskBaseViewModel = TaskCache.GetTaskById(pomo.Tasks[0].TaskId);
		}
		else
		{
			Dictionary<string, TimeSpan> dictionary = new Dictionary<string, TimeSpan>();
			TimeSpan timeSpan = TimeSpan.MinValue;
			string text = Utils.GetString("FocusTime") + "@";
			string taskId = string.Empty;
			PomoTask[] tasks = pomo.Tasks;
			foreach (PomoTask pomoTask in tasks)
			{
				string empty = string.Empty;
				if (!string.IsNullOrEmpty(pomoTask.TaskId))
				{
					empty = pomoTask.GetTitle() + "@" + pomoTask.TaskId;
				}
				else if (!string.IsNullOrEmpty(pomoTask.HabitId))
				{
					empty = pomoTask.GetTitle() + "@" + pomoTask.HabitId;
				}
				else
				{
					string title3 = pomoTask.GetTitle();
					empty = (string.IsNullOrEmpty(title3) ? Utils.GetString("FocusTime") : (title3 + "@"));
				}
				TimeSpan timeSpan2 = pomoTask.EndTime - pomoTask.StartTime;
				if (dictionary.ContainsKey(empty))
				{
					dictionary[empty] += timeSpan2;
				}
				else
				{
					dictionary[empty] = timeSpan2;
				}
				if (dictionary[empty] > timeSpan)
				{
					timeSpan = dictionary[empty];
					text = empty;
					taskId = pomoTask.TaskId;
				}
			}
			title = text.Split('@').FirstOrDefault() + "...";
			taskBaseViewModel = TaskCache.GetTaskById(taskId);
		}
		SourceViewModel = new TaskBaseViewModel
		{
			Type = DisplayType.Pomo,
			Id = id,
			Title = title,
			IsAllDay = false,
			StartDate = pomo.StartTime,
			DueDate = pomo.EndTime,
			CompletedTime = pomo.EndTime,
			Color = taskBaseViewModel?.Color,
			Tag = taskBaseViewModel?.Tag,
			Priority = (taskBaseViewModel?.Priority ?? 0),
			PomoType = pomo.Type,
			PomoDuration = (long)(pomo.EndTime - pomo.StartTime).TotalSeconds - pomo.PauseDuration
		};
		SourceViewModel.SetDependenceModel(taskBaseViewModel, "Color", "Tag", "Priority");
	}

	public CalendarDisplayModel(CourseDisplayModel courseDisplayModel)
	{
		CourseDisplayModel = courseDisplayModel;
		SourceViewModel = new TaskBaseViewModel
		{
			Title = courseDisplayModel.Title,
			Id = courseDisplayModel.Id,
			StartDate = courseDisplayModel.CourseStart,
			DueDate = ((courseDisplayModel.CourseStart < courseDisplayModel.CourseEnd) ? new DateTime?(courseDisplayModel.CourseEnd) : ((DateTime?)null)),
			Color = courseDisplayModel.Color,
			IsAllDay = false,
			Status = ((courseDisplayModel.Archived || CourseDisplayModel.CourseStart.Date < DateTime.Today) ? 2 : 0),
			CompletedTime = ((CourseDisplayModel.CourseStart.Date < DateTime.Today) ? ((DateTime?)null) : new DateTime?(courseDisplayModel.CourseStart)),
			Type = DisplayType.Course,
			Course = courseDisplayModel
		};
	}

	public static CalendarDisplayModel Build(HabitModel habit, int status, DateTime startDate, DateTime? completeTime)
	{
		bool value = true;
		if (startDate == DateTime.Today)
		{
			string[] reminders = habit.Reminders;
			if (reminders != null && reminders.Length != 0)
			{
				List<string> list = habit.Reminders.ToList();
				list.Sort();
				string text = list.FirstOrDefault((string r) => string.Compare(r, DateTime.Now.ToString("HH:mm"), StringComparison.Ordinal) >= 0) ?? list.LastOrDefault();
				if (text != null && int.TryParse(text.Substring(0, 2), out var result) && int.TryParse(text.Substring(3), out var result2))
				{
					startDate = startDate.AddHours(result).AddMinutes(result2);
					value = false;
				}
			}
		}
		return new CalendarDisplayModel
		{
			SourceViewModel = new HabitBaseViewModel(habit)
			{
				Status = status,
				StartDate = startDate,
				CompletedTime = completeTime,
				IsAllDay = value,
				SortOrder = habit.SortOrder
			}
		};
	}

	public CalendarDisplayModel(CalendarEventModel calendarEvent, int? status = null, string color = null)
	{
		SourceViewModel = new TaskBaseViewModel(calendarEvent)
		{
			StartDate = GetDateValue(calendarEvent.DueStart, calendarEvent.IsAllDay),
			DueDate = GetEndDateValue(calendarEvent.DueStart, calendarEvent.DueEnd, calendarEvent.IsAllDay),
			Status = (status ?? ((calendarEvent.DueStart.HasValue && calendarEvent.DueStart.Value < DateTime.Today && calendarEvent.DueEnd.HasValue && calendarEvent.DueEnd.Value <= DateTime.Today) ? 2 : calendarEvent.Status)),
			Color = color
		};
		EventTaskId = calendarEvent.TickTaskId;
	}

	private static DateTime? GetEndDateValue(DateTime? startDate, DateTime? endDate, bool isAllDay)
	{
		if (isAllDay && startDate.HasValue && endDate.HasValue && (endDate.Value - startDate.Value).Days == 1)
		{
			return null;
		}
		return GetDateValue(endDate, isAllDay);
	}

	private static DateTime? GetDateValue(DateTime? date, bool isAllDay)
	{
		if (!date.HasValue)
		{
			return null;
		}
		if (isAllDay)
		{
			return date.Value.Date;
		}
		return date;
	}

	public CalendarDisplayModel(TaskBaseViewModel model)
	{
		SourceViewModel = model;
	}

	private static int GetTypeByTaskType(int taskType, string taskKind)
	{
		switch (taskType)
		{
		case 0:
			switch (taskKind)
			{
			case "TEXT":
			case "CHECKLIST":
				return 0;
			case "NOTE":
				return 5;
			}
			break;
		case 1:
			return 1;
		case 2:
			return 3;
		case 3:
			return 4;
		}
		return 0;
	}

	internal static List<CalendarDisplayModel> GetVirtualModels(DateTime startDate, DateTime dueDate)
	{
		if (visualModels == null)
		{
			if (ABTestManager.UsePremiumV2CalendarProStyle())
			{
				startDate = new DateTime(2023, 8, 28);
				visualModels = (from m in JsonConvert.DeserializeObject<List<CalendarVirtualModel>>(IOUtils.GetFileContentInResourceFile(GetVirtualFileName()))
					select new CalendarDisplayModel(m.title, m.id, m.color, startDate.AddDays(m.addDay).AddMinutes(m.start), (!m.dueSpan.HasValue) ? ((DateTime?)null) : new DateTime?(startDate.AddDays(m.addDay).AddMinutes(m.start + m.dueSpan.Value)), m.isAllDay, m.status, m.priority, m.sortOrder)).ToList();
				return visualModels.ToList();
			}
			visualModels = new List<CalendarDisplayModel>
			{
				new CalendarDisplayModel("制作ppt", "0", "#52B8D2", startDate.AddDays(1.0), null, isAllDay: true, 2, 0, 0L),
				new CalendarDisplayModel("月度计划", "1", "#86C0EF", startDate.AddDays(1.0), null, isAllDay: true, 2, 0, 0L),
				new CalendarDisplayModel("选题会", "2", "#52B8D2", startDate.AddDays(1.0), null, isAllDay: true, 2, 0, 0L),
				new CalendarDisplayModel("ifs探店", "3", "#FFE599", startDate.AddDays(2.0), null, isAllDay: true, 2, 0, 0L),
				new CalendarDisplayModel("打电话给Sandy", "4", "#F18181", startDate.AddDays(2.0).AddHours(15.0).AddMinutes(30.0), null, isAllDay: false, 2, 0, 0L),
				new CalendarDisplayModel("审美的乌托邦：俄国文学100讲", "5", "#5DD1A8", startDate.AddDays(3.0), startDate.AddDays(6.0), isAllDay: true, 2, 0, 0L),
				new CalendarDisplayModel("瑜伽课", "6", "#5DD1A8", startDate.AddDays(3.0).AddHours(18.0), null, isAllDay: false, 2, 0, 0L),
				new CalendarDisplayModel("旅行计划", "7", "#93C47D", startDate.AddDays(3.0), null, isAllDay: true, 2, 0, 0L),
				new CalendarDisplayModel("整理衣柜", "8", "#D1D7A8", startDate.AddDays(4.0), null, isAllDay: true, 2, 0, 0L),
				new CalendarDisplayModel("买狗粮", "9", "#D1D7A8", startDate.AddDays(4.0), null, isAllDay: true, 2, 0, 0L),
				new CalendarDisplayModel("实习生面试", "10", "#86C0EF", startDate.AddDays(5.0).AddHours(14.0), null, isAllDay: false, 2, 0, 0L),
				new CalendarDisplayModel("苏州旅行", "11", "#52B8D2", startDate.AddDays(6.0), startDate.AddDays(8.0), isAllDay: true, 2, 0, 0L),
				new CalendarDisplayModel("写影评", "12", "#F9CB9C", startDate.AddDays(7.0), null, isAllDay: true, 2, 0, 0L),
				new CalendarDisplayModel("vlog剪辑", "13", "#F9CB9C", startDate.AddDays(7.0), null, isAllDay: true, 2, 0, 0L),
				new CalendarDisplayModel("功能评审会议", "14", "#86C0EF", startDate.AddDays(8.0).AddHours(16.0).AddMinutes(30.0), null, isAllDay: false, 2, 0, 0L),
				new CalendarDisplayModel("浇花", "15", "#52B8D2", startDate.AddDays(8.0), null, isAllDay: true, 2, 0, 0L),
				new CalendarDisplayModel("晨间日记", "16", "#52B8D2", startDate.AddDays(8.0), null, isAllDay: true, 2, 0, 0L),
				new CalendarDisplayModel("买礼物", "17", "#D1D7A8", startDate.AddDays(9.0), null, isAllDay: true, 2, 0, 0L),
				new CalendarDisplayModel("超市采购", "18", "#D1D7A8", startDate.AddDays(9.0), null, isAllDay: true, 2, 0, 0L),
				new CalendarDisplayModel("瑜伽课", "19", "#5DD1A8", startDate.AddDays(9.0), null, isAllDay: true, 2, 0, 0L),
				new CalendarDisplayModel("需求会议", "20", "#A4AFC7", startDate.AddDays(9.0), null, isAllDay: true, 2, 0, 0L),
				new CalendarDisplayModel("学习Python", "21", "#F9CB9C", startDate.AddDays(10.0), startDate.AddDays(14.0), isAllDay: true, 2, 0, 0L),
				new CalendarDisplayModel("戏剧新生活", "22", "#52B8D2", startDate.AddDays(10.0).AddHours(12.0), null, isAllDay: false, 2, 0, 0L),
				new CalendarDisplayModel("今日工作复盘", "23", "#D1D7A8", startDate.AddDays(10.0).AddHours(18.0), null, isAllDay: false, 2, 0, 0L),
				new CalendarDisplayModel("新闻通稿", "24", "#F9CB9C", startDate.AddDays(11.0), null, isAllDay: true, 2, 0, 0L),
				new CalendarDisplayModel("问候父母", "25", "#F18181", startDate.AddDays(11.0), null, isAllDay: true, 2, 0, 0L),
				new CalendarDisplayModel("月度需求会议", "26", "#A4AFC7", startDate.AddDays(11.0).AddHours(17.0), null, isAllDay: false, 2, 0, 0L),
				new CalendarDisplayModel("理发", "27", "#D1D7A8", startDate.AddDays(12.0), null, isAllDay: true, 2, 0, 0L),
				new CalendarDisplayModel("旅游规划", "28", "#93C47D", startDate.AddDays(12.0), null, isAllDay: true, 2, 0, 0L),
				new CalendarDisplayModel("买礼物", "29", "#D1D7A8", startDate.AddDays(12.0), null, isAllDay: true, 2, 0, 0L),
				new CalendarDisplayModel("汗蒸", "30", "#B6D7A8", startDate.AddDays(13.0), null, isAllDay: true, 2, 0, 0L),
				new CalendarDisplayModel("周复盘", "31", "#D1D7A8", startDate.AddDays(13.0), null, isAllDay: true, 2, 0, 0L),
				new CalendarDisplayModel("练琴", "32", "#F9CB9C", startDate.AddDays(13.0), null, isAllDay: true, 2, 0, 0L),
				new CalendarDisplayModel("预定餐厅", "33", "#F2B04B", startDate.AddDays(13.0), null, isAllDay: true, 2, 0, 0L),
				new CalendarDisplayModel("公园野餐", "34", "#FFE599", startDate.AddDays(14.0), null, isAllDay: true, 2, 0, 0L),
				new CalendarDisplayModel("看话剧", "35", "#F9CB9C", startDate.AddDays(14.0), null, isAllDay: true, 2, 0, 0L),
				new CalendarDisplayModel("耳边风：马世芳的音乐时光机", "36", "#5DD1A8", startDate.AddDays(15.0).AddHours(11.0), startDate.AddDays(20.0), isAllDay: false, 2, 0, 0L),
				new CalendarDisplayModel("交电费", "37", "#D1D7A8", startDate.AddDays(15.0), null, isAllDay: true, 2, 0, 0L),
				new CalendarDisplayModel("交房租", "38", "#D1D7A8", startDate.AddDays(16.0), null, isAllDay: true, 2, 0, 0L),
				new CalendarDisplayModel("新功能宣传", "39", "#A4AFC7", startDate.AddDays(17.0), null, isAllDay: true, 2, 0, 0L),
				new CalendarDisplayModel("lily生日", "40", "#F18181", startDate.AddDays(17.0).AddHours(19.0), null, isAllDay: false, 2, 0, 0L),
				new CalendarDisplayModel("H5游戏策划", "41", "#F9CB9C", startDate.AddDays(18.0).AddHours(9.0), null, isAllDay: false, 2, 0, 0L),
				new CalendarDisplayModel("佳佳生日", "42", "#F18181", startDate.AddDays(18.0), null, isAllDay: true, 2, 0, 0L),
				new CalendarDisplayModel("拿快递", "43", "#D1D7A8", startDate.AddDays(19.0).AddHours(20.0).AddMinutes(0.0), null, isAllDay: false, 2, 0, 0L),
				new CalendarDisplayModel("视频脚本", "44", "#B6D7A8", startDate.AddDays(19.0).AddHours(16.0), null, isAllDay: false, 2, 0, 0L),
				new CalendarDisplayModel("部门培训", "45", "#86C0EF", startDate.AddDays(19.0).AddHours(13.0).AddMinutes(30.0), null, isAllDay: false, 2, 0, 0L),
				new CalendarDisplayModel("吃饭", "46", "#86C0EF", startDate.AddDays(20.0), null, isAllDay: true, 2, 0, 0L),
				new CalendarDisplayModel("买洗发水", "47", "#D1D7A8", startDate.AddDays(20.0).AddHours(19.0).AddMinutes(45.0), null, isAllDay: false, 2, 0, 0L),
				new CalendarDisplayModel("周复盘", "48", "#D1D7A8", startDate.AddDays(20.0).AddHours(20.0).AddMinutes(15.0), null, isAllDay: false, 2, 0, 0L),
				new CalendarDisplayModel("演唱会", "49", "#FFE599", startDate.AddDays(20.0).AddHours(14.0), null, isAllDay: false, 2, 0, 0L),
				new CalendarDisplayModel("艺术集市", "50", "#FFE599", startDate.AddDays(21.0), null, isAllDay: true, 2, 0, 0L),
				new CalendarDisplayModel("吃饭", "51", "#86C0EF", startDate.AddDays(21.0), null, isAllDay: true, 2, 0, 0L),
				new CalendarDisplayModel("吃饭", "52", "#86C0EF", startDate.AddDays(22.0), null, isAllDay: true, 2, 0, 0L),
				new CalendarDisplayModel("生日会", "53", "#F2B04B", startDate.AddDays(22.0).AddHours(15.0).AddMinutes(0.0), null, isAllDay: false, 2, 0, 0L),
				new CalendarDisplayModel("读《夜晚的潜水艇》", "54", "#52B8D2", startDate.AddDays(23.0).AddHours(15.0).AddMinutes(0.0), startDate.AddDays(27.0), isAllDay: false, 0, 0, 0L),
				new CalendarDisplayModel("瑜伽课", "55", "#5DD1A8", startDate.AddDays(23.0), null, isAllDay: true, 2, 0, 0L),
				new CalendarDisplayModel("部门例会", "56", "#52B8D2", startDate.AddDays(23.0).AddHours(18.0), null, isAllDay: false, 2, 0, 0L),
				new CalendarDisplayModel("宣传物料", "57", "#F9CB9C", startDate.AddDays(24.0).AddHours(12.0).AddMinutes(45.0), null, isAllDay: false, 2, 0, 0L),
				new CalendarDisplayModel("大使培训", "58", "#F9CB9C", startDate.AddDays(24.0), null, isAllDay: true, 2, 0, 0L),
				new CalendarDisplayModel("朋友圈海报", "59", "#F9CB9C", startDate.AddDays(24.0), null, isAllDay: true, 2, 0, 0L),
				new CalendarDisplayModel("背单词", "60", "#F9CB9C", startDate.AddDays(25.0), null, isAllDay: true, 0, 0, 0L),
				new CalendarDisplayModel("敷面膜", "61", "#B6D7A8", startDate.AddDays(25.0), null, isAllDay: true, 2, 0, 0L),
				new CalendarDisplayModel("线下沙龙嘉宾邀请", "62", "#F9CB9C", startDate.AddDays(25.0), null, isAllDay: true, 2, 0, 0L),
				new CalendarDisplayModel("嘉宾邀约", "63", "#F9CB9C", startDate.AddDays(25.0), null, isAllDay: true, 2, 0, 0L),
				new CalendarDisplayModel("回复社媒用户留言", "64", "#86C0EF", startDate.AddDays(25.0), null, isAllDay: true, 2, 0, 0L),
				new CalendarDisplayModel("增长方案", "65", "#F9CB9C", startDate.AddDays(26.0), null, isAllDay: true, 0, 0, 0L),
				new CalendarDisplayModel("看画展", "66", "#F9CB9C", startDate.AddDays(27.0), null, isAllDay: true, 0, 0, 0L),
				new CalendarDisplayModel("周复盘", "67", "#D1D7A8", startDate.AddDays(27.0), null, isAllDay: true, 0, 0, 0L),
				new CalendarDisplayModel("练琴", "68", "#F9CB9C", startDate.AddDays(27.0), null, isAllDay: true, 0, 0, 0L),
				new CalendarDisplayModel("看《恋恋风尘》", "69", "#52B8D2", startDate.AddDays(28.0), null, isAllDay: true, 0, 0, 0L),
				new CalendarDisplayModel("公园晨跑", "70", "#5DD1A8", startDate.AddDays(28.0), null, isAllDay: true, 0, 0, 0L),
				new CalendarDisplayModel("瑜伽课", "71", "#5DD1A8", startDate.AddDays(29.0), null, isAllDay: true, 0, 0, 0L),
				new CalendarDisplayModel("买牛奶", "72", "#D1D7A8", startDate.AddDays(29.0), null, isAllDay: true, 0, 0, 0L),
				new CalendarDisplayModel("看《脸庞，村庄》", "73", "#52B8D2", startDate.AddDays(29.0), null, isAllDay: true, 0, 0, 0L),
				new CalendarDisplayModel("采购奖品", "74", "#F9CB9C", startDate.AddDays(30.0).AddHours(12.0).AddMinutes(15.0), null, isAllDay: false, 0, 0, 0L),
				new CalendarDisplayModel("反馈统计", "75", "#86C0EF", startDate.AddDays(30.0), null, isAllDay: true, 0, 0, 0L),
				new CalendarDisplayModel("用户支持", "76", "#86C0EF", startDate.AddDays(31.0), null, isAllDay: true, 0, 0, 0L),
				new CalendarDisplayModel("月度复盘", "77", "#86C0EF", startDate.AddDays(31.0), null, isAllDay: true, 0, 0, 0L),
				new CalendarDisplayModel("制作宣传视频", "78", "#F9CB9C", startDate.AddDays(32.0).AddHours(9.0).AddMinutes(30.0), null, isAllDay: false, 0, 0, 0L),
				new CalendarDisplayModel("敷面膜", "79", "#B6D7A8", startDate.AddDays(32.0), null, isAllDay: true, 0, 0, 0L),
				new CalendarDisplayModel("经费核算", "80", "#F9CB9C", startDate.AddDays(33.0), null, isAllDay: true, 0, 0, 0L)
			};
		}
		return visualModels.ToList();
	}

	private static string GetVirtualFileName()
	{
		switch (LocalSettings.Settings.UserChooseLanguage)
		{
		case "zh-CN":
			return "/Assets/CalendarVirtualData_cn.json";
		case "en-US":
			return "/Assets/CalendarVirtualData_en.json";
		case "de-DE":
			return "/Assets/CalendarVirtualData_de.json";
		case "pt-BR":
			return "/Assets/CalendarVirtualData_pt.json";
		case "ru-RU":
			return "/Assets/CalendarVirtualData_ru.json";
		case "ja-JP":
			return "/Assets/CalendarVirtualData_ja.json";
		case "ko-KR":
			return "/Assets/CalendarVirtualData_ko.json";
		default:
			if (!Utils.IsCn())
			{
				return "/Assets/CalendarVirtualData_en.json";
			}
			return "/Assets/CalendarVirtualData_cn.json";
		}
	}
}
