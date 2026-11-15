using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Ical.Net;
using Ical.Net.DataTypes;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Habit;

public static class HabitUtils
{
	private static readonly Utils.TupleList<int, string, HabitRealType, HabitCheckInType> CheckInTuple = new Utils.TupleList<int, string, HabitRealType, HabitCheckInType>
	{
		{
			0,
			"boolean",
			HabitRealType.Auto,
			HabitCheckInType.BoolUnCompleted
		},
		{
			0,
			"boolean",
			HabitRealType.Manual,
			HabitCheckInType.BoolUnCompleted
		},
		{
			0,
			"boolean",
			HabitRealType.CompletedAll,
			HabitCheckInType.BoolUnCompleted
		},
		{
			0,
			"real",
			HabitRealType.Auto,
			HabitCheckInType.AutoUncompleted
		},
		{
			0,
			"real",
			HabitRealType.Manual,
			HabitCheckInType.ManualUnCompleted
		},
		{
			0,
			"real",
			HabitRealType.CompletedAll,
			HabitCheckInType.CompletedAllUnCompleted
		},
		{
			2,
			"boolean",
			HabitRealType.Auto,
			HabitCheckInType.BoolCompleted
		},
		{
			2,
			"boolean",
			HabitRealType.Manual,
			HabitCheckInType.BoolCompleted
		},
		{
			2,
			"boolean",
			HabitRealType.CompletedAll,
			HabitCheckInType.BoolCompleted
		},
		{
			2,
			"real",
			HabitRealType.Auto,
			HabitCheckInType.AutoCompleted
		},
		{
			2,
			"real",
			HabitRealType.Manual,
			HabitCheckInType.ManualCompleted
		},
		{
			2,
			"real",
			HabitRealType.CompletedAll,
			HabitCheckInType.CompletedAllCompleted
		}
	};

	private static DateTime _lastRemindDate;

	public static Dictionary<string, HabitIconInfo> IconInfos { get; } = new Dictionary<string, HabitIconInfo>
	{
		{
			"habit_daily_check_in",
			new HabitIconInfo("habit_daily_check_in", "每天进步一点点", "#97E38B", "#187809", "#70C362")
		},
		{
			"habit_drink_water",
			new HabitIconInfo("habit_drink_water", "喝水", "#80F3CD", "#056E4C", "#6FE9BD")
		},
		{
			"habit_eat_breakfast",
			new HabitIconInfo("habit_eat_breakfast", "吃早餐", "#FF9BAC", "#9B1027", "#ED70A4")
		},
		{
			"habit_eat_fruits",
			new HabitIconInfo("habit_eat_fruits", "吃水果", "#97E38B", "#187809", "#A1CE61")
		},
		{
			"habit_early_to_rise",
			new HabitIconInfo("habit_early_to_rise", "早起", "#FEE068", "#786103", "#F8D550")
		},
		{
			"habit_early_to_bed",
			new HabitIconInfo("habit_early_to_bed", "早睡", "#6E81ED", "#2134A3", "#7B42EB")
		},
		{
			"habit_learn_words",
			new HabitIconInfo("habit_learn_words", "背单词", "#7BC4FA", "#045692", "#6FE9BD")
		},
		{
			"habit_reading",
			new HabitIconInfo("habit_reading", "阅读", "#A0EFED", "#06807D", "#A1CE61")
		},
		{
			"habit_quit_snacks",
			new HabitIconInfo("habit_quit_snacks", "戒零食", "#F18BEC", "#8E0E88", "#CF65F6")
		},
		{
			"habit_exercising",
			new HabitIconInfo("habit_exercising", "锻炼身体", "#BF8AF5", "#5A08AE", "#4AA6EF")
		},
		{
			"habit_meditating",
			new HabitIconInfo("habit_meditating", "冥想", "#6E81ED", "#2134A3", "#CF65F6")
		},
		{
			"habit_keep_calm",
			new HabitIconInfo("habit_keep_calm", "不发脾气", "#A0EFED", "#007976", "#5992F8")
		},
		{
			"habit_jogging",
			new HabitIconInfo("habit_jogging", "跑步", "#FDB368", "#854401", "#F2B04C")
		},
		{
			"habit_stretch",
			new HabitIconInfo("habit_stretch", "拉伸", "#97E38B", "#187809", "#ED70A4")
		},
		{
			"habit_yoga",
			new HabitIconInfo("habit_yoga", "瑜伽", "#F18BEC", "#8E0E88", "#CF65F6")
		},
		{
			"habit_push_ups",
			new HabitIconInfo("habit_push_ups", "俯卧撑", "#FDB368", "#854401", "#6FE9BD")
		},
		{
			"habit_cycling",
			new HabitIconInfo("habit_cycling", "骑车", "#97E38B", "#187809", "#A1CE61")
		},
		{
			"habit_swimming",
			new HabitIconInfo("habit_swimming", "游泳", "#7BC4FA", "#045692", "#65D7FF")
		},
		{
			"habit_learn_language",
			new HabitIconInfo("habit_learn_language", "学语言", "#7BC4FA", "#045692", "#ED70A4")
		},
		{
			"habit_learn_instrument",
			new HabitIconInfo("habit_learn_instrument", "学乐器", "#FF8585", "#9F0808", "#5992F8")
		},
		{
			"habit_do_homework",
			new HabitIconInfo("habit_do_homework", "写作", "#FEE068", "#786103", "#E5DF2B")
		},
		{
			"habit_keep_diary",
			new HabitIconInfo("habit_keep_diary", "写日记", "#FF8585", "#9F0808", "#EE8C6F")
		},
		{
			"habit_save_money",
			new HabitIconInfo("habit_save_money", "记账", "#FEE068", "#786103", "#F2B04C")
		},
		{
			"habit_self_reflection",
			new HabitIconInfo("habit_self_reflection", "每日总结", "#7BC4FA", "#045692", "#EE8C6F")
		},
		{
			"habit_eat_dinner",
			new HabitIconInfo("habit_eat_dinner", "吃晚饭", "#7BC4FA", "#045692", "#4AA6EF")
		},
		{
			"habit_eat_veggies",
			new HabitIconInfo("habit_eat_veggies", "吃蔬菜", "#FEE068", "#786103", "#F8D550")
		},
		{
			"habit_quit_sugar",
			new HabitIconInfo("habit_quit_sugar", "戒糖", "#F18BEC", "#8E0E88", "#52B8D2")
		},
		{
			"habit_deep_breath",
			new HabitIconInfo("habit_deep_breath", "深呼吸", "#97E38B", "#187809", "#6FE9BD")
		},
		{
			"habit_stop_overthinking",
			new HabitIconInfo("habit_stop_overthinking", "不抱怨", "#7BC4FA", "#045692", "#5992F8")
		},
		{
			"habit_listen_music",
			new HabitIconInfo("habit_listen_music", "听歌", "#80F3CD", "#056E4C", "#F2B04C")
		},
		{
			"habit_watch_movie",
			new HabitIconInfo("habit_watch_movie", "看电影", "#FDB368", "#854401", "#EE8C6F")
		},
		{
			"habit_no_video_games",
			new HabitIconInfo("habit_no_video_games", "禁游戏", "#F18BEC", "#8E0E88", "#52B8D2")
		},
		{
			"habit_beat_phone_addiction",
			new HabitIconInfo("habit_beat_phone_addiction", "少玩手机", "#F18BEC", "#8E0E88", "#7B42EB")
		},
		{
			"habit_contact",
			new HabitIconInfo("habit_contact", "保持联系", "#FF9BAC", "#9B1027", "#A1CE61")
		},
		{
			"habit_praise_others",
			new HabitIconInfo("habit_praise_others", "赞美别人", "#FF8585", "#9F0808", "#ED70A4")
		},
		{
			"habit_help_others",
			new HabitIconInfo("habit_help_others", "帮助他人", "#FF8585", "#9F0808", "#EE8C6F")
		},
		{
			"habit_introspect",
			new HabitIconInfo("habit_introspect", "反省自己", "#97E38B", "#187809", "#F8D550")
		},
		{
			"habit_plan",
			new HabitIconInfo("habit_plan", "今日计划", "#7BC4FA", "#045692", "#52B8D2")
		},
		{
			"habit_stay_positive",
			new HabitIconInfo("habit_stay_positive", "积极向上", "#FF8585", "#9F0808", "#A1CE61")
		},
		{
			"habit_your_apprearance",
			new HabitIconInfo("habit_your_apprearance", "注意形象", "#A0EFED", "#007976", "#CF65F6")
		},
		{
			"habit_take_photos",
			new HabitIconInfo("habit_take_photos", "拍照", "#A0EFED", "#007976", "#4AA6EF")
		},
		{
			"habit_eye_protection",
			new HabitIconInfo("habit_eye_protection", "护眼", "#D7EB63", "#637400", "#7B42EB")
		},
		{
			"habit_brush_teeth",
			new HabitIconInfo("habit_brush_teeth", "刷牙", "#7BC4FA", "#045692", "#A1CE61")
		},
		{
			"habit_take_shower",
			new HabitIconInfo("habit_take_shower", "洗澡", "#A0EFED", "#007976", "#CF65F6")
		},
		{
			"habit_cleaning",
			new HabitIconInfo("habit_cleaning", "打扫卫生", "#FF8585", "#9F0808", "#EC6A66")
		},
		{
			"habit_housework",
			new HabitIconInfo("habit_housework", "做家务", "#A0EFED", "#007976", "#ED70A4")
		},
		{
			"habit_water_flowers",
			new HabitIconInfo("habit_water_flowers", "浇花", "#D7EB63", "#637400", "#4AA6EF")
		},
		{
			"habit_walk_the_dog",
			new HabitIconInfo("habit_walk_the_dog", "遛狗", "#FF9BAC", "#9B1027", "#7B42EB")
		},
		{
			"habit_cat_keeper",
			new HabitIconInfo("habit_cat_keeper", "做铲屎官", "#FF9BAC", "#9B1027", "#EE8C6F")
		},
		{
			"habit_watch_documentary",
			new HabitIconInfo("habit_watch_documentary", "看纪录片", "#6E81ED", "#2134A3", "#ED70A4")
		},
		{
			"habit_watch_news",
			new HabitIconInfo("habit_watch_news", "看新闻", "#7BC4FA", "#045692", "#A1CE61")
		},
		{
			"habit_watch_tv_show",
			new HabitIconInfo("habit_watch_tv_show", "看综艺", "#F18BEC", "#8E0E88", "#5992F8")
		},
		{
			"habit_watch_soap_opera",
			new HabitIconInfo("habit_watch_soap_opera", "看电视剧", "#FF8585", "#9F0808", "#F8D550")
		},
		{
			"habit_not_swear",
			new HabitIconInfo("habit_not_swear", "不说脏话", "#6E81ED", "#2134A3", "#5992F8")
		},
		{
			"habit_skincare",
			new HabitIconInfo("habit_skincare", "护肤", "#FF9BAC", "#9B1027", "#CF65F6")
		},
		{
			"habit_take_walk",
			new HabitIconInfo("habit_take_walk", "散步", "#97E38B", "#187809", "#A1CE61")
		},
		{
			"habit_keep_fit",
			new HabitIconInfo("habit_keep_fit", "减肥", "#D7EB63", "#637400", "#CF65F6")
		},
		{
			"habit_quit_smoking",
			new HabitIconInfo("habit_quit_smoking", "戒烟", "#F18BEC", "#8E0E88", "#52B8D2")
		},
		{
			"habit_quit_drinking",
			new HabitIconInfo("habit_quit_drinking", "戒酒", "#F18BEC", "#8E0E88", "#EC6A66")
		},
		{
			"habit_take_medicine",
			new HabitIconInfo("habit_take_medicine", "吃药", "#A0EFED", "#007976", "#F8D550")
		},
		{
			"habit_stand",
			new HabitIconInfo("habit_stand", "站立", "#FEE068", "#786103", "#FFCE77")
		},
		{
			"habit_neck_exercises",
			new HabitIconInfo("habit_neck_exercises", "活动颈部", "#97E38B", "#187809", "#B0DE81")
		},
		{
			"habit_say_love_u",
			new HabitIconInfo("habit_say_love_u", "说我爱你", "#FF9BAC", "#9B1027", "#FA9BB0")
		},
		{
			"habit_smile_to_yourself",
			new HabitIconInfo("habit_smile_to_yourself", "给自己一个微笑", "#FF8585", "#9F0808", "#FF7877")
		},
		{
			"habit_roll_eyes",
			new HabitIconInfo("habit_roll_eyes", "转眼运动", "##6FE79A", "#4EA36D", "#6FE79A")
		},
		{
			"habit_control_temper",
			new HabitIconInfo("habit_control_temper", "不发脾气", "#FFCE77", "#786103", "#FFCE77")
		}
	};

	public static Dictionary<string, string> TextIconColorDict { get; } = new Dictionary<string, string>
	{
		{
			string.Empty,
			"#CDD9FF"
		},
		{ "#4772FA", "#CDD9FF" },
		{ "#E06666", "#FAD4D4" },
		{ "#CC0000", "#F4A8A8" },
		{ "#930000", "#F6A9A9" },
		{ "#B45F06", "#FFE2C4" },
		{ "#BF9002", "#FFF5D6" },
		{ "#73761D", "#F3F699" },
		{ "#38761D", "#B6F29D" },
		{ "#E13E39", "#FBC9C8" },
		{ "#E69138", "#6D3B07" },
		{ "#F1C231", "#634B03" },
		{ "#A0BB31", "#445306" },
		{ "#6CBE5E", "#115705" },
		{ "#EC6666", "#FFDBDB" },
		{ "#F2B04B", "#824F02" },
		{ "#FFD966", "#846403" },
		{ "#DDE358", "#606400" },
		{ "#93C47D", "#216204" },
		{ "#F18181", "#810606" },
		{ "#F9CB9C", "#773E04" },
		{ "#FFE599", "#7C5E07" },
		{ "#D1D7A8", "#626F06" },
		{ "#B6D7A8", "#236806" },
		{ "#B79A8F", "#6E2205" },
		{ "#C9B49D", "#683A08" },
		{ "#C0B89E", "#5F4B08" },
		{ "#BDC2B0", "#4B6605" },
		{ "#B6C5B0", "#216207" },
		{ "#2A9364", "#B9F2D9" },
		{ "#134F5C", "#A4DEEB" },
		{ "#134397", "#AFC8F3" },
		{ "#491C75", "#D3B3F2" },
		{ "#731B63", "#F0B4E5" },
		{ "#3BB780", "#CCF2E1" },
		{ "#2AA6C1", "#D3F0F6" },
		{ "#3876E4", "#BFD3F5" },
		{ "#9842EB", "#E3CAFA" },
		{ "#BE3B83", "#FAC4E1" },
		{ "#5DD1A8", "#066845" },
		{ "#52B8D2", "#04495B" },
		{ "#5992F8", "#E7EEFA" },
		{ "#9F4CEF", "#EEDDFF" },
		{ "#D25294", "#FFE0F0" },
		{ "#6FE9BE", "#05734C" },
		{ "#7CEDEB", "#04716F" },
		{ "#86C0EF", "#094C82" },
		{ "#CF66F7", "#F9E8FF" },
		{ "#ED70A5", "#FFE9F3" },
		{ "#ABC4BC", "#078F64" },
		{ "#ABBDC4", "#0F6181" },
		{ "#A4AFC7", "#274A98" },
		{ "#ABA6B5", "#3F1591" },
		{ "#A9949E", "#620833" }
	};

	public static HabitCheckInType GetHabitCheckInType(int status, string type, double step, double goal)
	{
		if (status == -1)
		{
			status = 2;
		}
		HabitRealType habitRealType;
		if (step > 0.0)
		{
			habitRealType = HabitRealType.Auto;
		}
		else if (Math.Abs(step) <= 0.0)
		{
			habitRealType = HabitRealType.CompletedAll;
		}
		else
		{
			habitRealType = HabitRealType.Manual;
		}
		string normalizedType = HabitModel.NormalizeType(type, goal, step).ToLowerInvariant();
		return CheckInTuple.FirstOrDefault((Tuple<int, string, HabitRealType, HabitCheckInType> item) => item.Item1 == status && item.Item2 == normalizedType && item.Item3 == habitRealType)?.Item4 ?? HabitCheckInType.AutoUncompleted;
	}

	public static async Task<bool> IsHabitValidInToday(HabitModel habit, List<HabitCheckInModel> checkIns)
	{
		if (!(await CheckHabitStartDate(habit, DateTime.Today)))
		{
			return false;
		}
		if (!string.IsNullOrEmpty(habit.RepeatRule))
		{
			List<DateTime> list = new List<DateTime>();
			List<DateTime> list2 = new List<DateTime>();
			if (checkIns != null)
			{
				foreach (HabitCheckInModel checkIn in checkIns)
				{
					if (checkIn.CheckStatus != 0 && DateTime.TryParseExact(checkIn.CheckinStamp, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
					{
						switch (checkIn.CheckStatus)
						{
						case 2:
							list.Add(result);
							break;
						case 1:
							list2.Add(result);
							break;
						}
					}
				}
			}
			HabitRepeatInfo habitRepeatInfo = BuildHabitRepeatInfo(habit.RepeatRule);
			switch (habitRepeatInfo.Type)
			{
			case HabitRepeatType.ByDay:
				if (habitRepeatInfo.ByDays.Any() && !habitRepeatInfo.ByDays.Contains(DateTime.Today.DayOfWeek))
				{
					return false;
				}
				break;
			case HabitRepeatType.Daily:
			{
				list?.Sort((DateTime a, DateTime b) => a.CompareTo(b));
				DateTime dateTime = list.LastOrDefault((DateTime d) => d.Date <= DateTime.Today);
				return (DateTime.Today - dateTime.Date).Days >= habitRepeatInfo.Interval;
			}
			case HabitRepeatType.TimesInWeek:
			{
				DateTime weekStart = Utils.GetWeekStart(DateTime.Today);
				DateTime weekEnd = ((weekStart.AddDays(6.0) > DateTime.Today) ? DateTime.Today : weekStart.AddDays(6.0));
				int num = list?.Count((DateTime d) => d >= weekStart && d < DateTime.Today) ?? 0;
				int num2 = list?.Count((DateTime d) => d > DateTime.Today && d <= weekEnd) ?? 0;
				int num3 = habitRepeatInfo.Count - num - num2;
				num2 += list2.Count((DateTime d) => d > DateTime.Today && d <= weekEnd);
				return (weekEnd - DateTime.Today).Days - num2 < num3;
			}
			}
		}
		return true;
	}

	public static bool IsReminderValid(string reminderText)
	{
		if (!string.IsNullOrEmpty(reminderText))
		{
			string[] array = reminderText.Split(',');
			if (array.Any())
			{
				string[] array2 = array;
				foreach (string text in array2)
				{
					if (!text.Contains(":"))
					{
						continue;
					}
					string[] array3 = text.Split(':');
					if (array3.Length == 2 && int.TryParse(array3[0], out var result) && int.TryParse(array3[1], out var result2))
					{
						int num = result * 60 + result2;
						int num2 = DateTime.Now.Hour * 60 + DateTime.Now.Minute;
						if (num > num2)
						{
							return true;
						}
					}
				}
			}
		}
		return false;
	}

	public static HabitRepeatInfo BuildHabitRepeatInfo(string repeatFlag, string[] habitExDates = null)
	{
		if (!string.IsNullOrEmpty(repeatFlag))
		{
			RecurrenceModel recurrenceModel = RecurrenceModel.GetRecurrenceModel(repeatFlag);
			if (recurrenceModel.Frequency == FrequencyType.Weekly)
			{
				Match match = new Regex("TT_TIMES=(\\d{0,})").Match(repeatFlag);
				if (match.Success && int.TryParse(match.Groups[1].ToString(), out var result))
				{
					return new HabitRepeatInfo
					{
						Type = HabitRepeatType.TimesInWeek,
						Count = result,
						ExDates = habitExDates
					};
				}
				if (recurrenceModel.ByDay.Any())
				{
					List<DayOfWeek> byDays = recurrenceModel.ByDay.Select((WeekDay day) => day.DayOfWeek).ToList();
					return new HabitRepeatInfo
					{
						Type = HabitRepeatType.ByDay,
						ByDays = byDays,
						ExDates = habitExDates
					};
				}
			}
			else if (recurrenceModel.Frequency == FrequencyType.Daily)
			{
				return new HabitRepeatInfo
				{
					Type = HabitRepeatType.Daily,
					Interval = ((recurrenceModel.Interval == 0) ? 1 : recurrenceModel.Interval),
					ExDates = habitExDates
				};
			}
		}
		return new HabitRepeatInfo
		{
			Type = HabitRepeatType.Daily,
			ExDates = habitExDates
		};
	}

	public static bool InitShowHint(HabitRepeatInfo repeatInfo, DateTime date, DateTime firstCheckDay, List<(DateTime, bool?)> checkedDates, bool checkCurrentWeek = false)
	{
		string[] exDates = repeatInfo.ExDates;
		if (exDates != null && exDates.Contains(date.Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture)))
		{
			return false;
		}
		if (repeatInfo.Type != HabitRepeatType.Daily || repeatInfo.Interval <= 1)
		{
			return NeedShowHint(repeatInfo, date, checkedDates, checkCurrentWeek);
		}
		return NeedShowHintDailyInterval(repeatInfo.Interval, date, firstCheckDay, checkedDates, checkCurrentWeek);
	}

	private static bool NeedShowHintDailyInterval(int interval, DateTime date, DateTime firstCheckDay, List<(DateTime, bool?)> checkedDates, bool checkToday = false)
	{
		checkedDates?.Sort(((DateTime, bool?) a, (DateTime, bool?) b) => a.CompareTo(b));
		DateTime? dateTime = checkedDates?.LastOrDefault(((DateTime, bool?) d) => d.Item2 == true && d.Item1.Date < date.Date).Item1;
		if (Utils.IsEmptyDate(dateTime))
		{
			dateTime = firstCheckDay;
			if (dateTime == date.Date)
			{
				return true;
			}
		}
		if (Utils.IsEmptyDate(dateTime))
		{
			dateTime = DateTime.Today;
		}
		if (dateTime < date.Date)
		{
			if (checkToday && date.Date == DateTime.Today)
			{
				return (date.Date - dateTime.Value).Days >= interval;
			}
			return (date.Date - dateTime.Value).Days % interval == 0;
		}
		return false;
	}

	private static bool NeedShowHint(HabitRepeatInfo repeatInfo, DateTime date, List<(DateTime, bool?)> checkedDates, bool checkCurrentWeek = false)
	{
		switch (repeatInfo.Type)
		{
		case HabitRepeatType.ByDay:
			if (!repeatInfo.ByDays.Contains(date.DayOfWeek))
			{
				break;
			}
			goto case HabitRepeatType.Daily;
		case HabitRepeatType.Daily:
			return true;
		case HabitRepeatType.TimesInWeek:
		{
			DateTime weekStart = Utils.GetWeekStart(date);
			DateTime weekEnd = weekStart.AddDays(6.0);
			int num = checkedDates?.Count(((DateTime, bool?) d) => d.Item2 != false && d.Item1 >= weekStart && d.Item1 < date.Date) ?? 0;
			int num2 = checkedDates?.Count(((DateTime, bool?) d) => d.Item2 != false && d.Item1 > date.Date && d.Item1 <= weekEnd) ?? 0;
			int num3 = repeatInfo.Count - num - num2;
			num2 += checkedDates?.Count(((DateTime, bool?) d) => d.Item2 == false && d.Item1 > date.Date && d.Item1 <= weekEnd) ?? 0;
			if (checkCurrentWeek)
			{
				return num3 > 0;
			}
			int num4 = 0;
			for (int num5 = 0; num5 < (weekEnd - date.Date).Days; num5++)
			{
				string[] exDates = repeatInfo.ExDates;
				if (exDates != null && exDates.Contains(date.Date.AddDays(num5).ToString("yyyyMMdd", CultureInfo.InvariantCulture)))
				{
					num4++;
				}
			}
			if ((weekEnd - date.Date).Days - num4 - num2 < num3)
			{
				return true;
			}
			return false;
		}
		}
		return false;
	}

	public static string GetHabitHintText(string habitIconRes)
	{
		habitIconRes = (string.IsNullOrWhiteSpace(habitIconRes) ? "habit_daily_check_in" : habitIconRes);
		string[] array = habitIconRes.Split('_');
		if (array.Length > 1 && array[0] == "txt")
		{
			return array[1];
		}
		return Utils.GetString(array.Where((string str) => str.Length >= 1).Aggregate("", (string current, string str) => current + str.Substring(0, 1).ToUpper() + str.Substring(1).ToLower()));
	}

	public static async Task ShowHabitRecordWindow(string habitId, string habitName, DateTime date, bool isEdit = true, bool unCompleted = false)
	{
		HabitModel habit = await HabitDao.GetHabitById(habitId);
		if (habit != null)
		{
			int stamp = int.Parse(date.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
			HabitRecordModel record = await HabitRecordDao.GetHabitRecordsByHabitIdAndDate(habitId, stamp);
			HabitRecordModel habitRecordModel = record;
			if (habitRecordModel != null && habitRecordModel.Deleted == 1)
			{
				App.Connection.DeleteAsync(record);
				record = null;
			}
			Application.Current?.Dispatcher?.Invoke(delegate
			{
				EditHabitLogWindow editHabitLogWindow = new EditHabitLogWindow(new CheckInLogViewModel
				{
					HabitId = habitId,
					HabitName = habitName,
					Content = (record?.Content ?? string.Empty),
					Date = date,
					Score = (record?.Emoji ?? 0),
					IconRes = habit.IconRes,
					Color = habit.Color,
					IsBoolHabit = habit.IsBoolHabit(),
					UnCompleted = unCompleted
				}, isEdit, record == null);
				editHabitLogWindow.Owner = App.Window;
				editHabitLogWindow.ShowDialog();
			});
		}
	}

	public static string GetUnitText(string unit)
	{
		if (unit == "次" || unit == "Count")
		{
			return Utils.GetString("Count");
		}
		return unit;
	}

	public static async Task<bool> CheckSectionLimit()
	{
		List<HabitSectionModel> obj = HabitSectionCache.GetSections()?.Where((HabitSectionModel section) => section.SyncStatus != -1).ToList();
		if (obj != null && obj.Count >= 18)
		{
			if (UserDao.IsPro())
			{
				new CustomerDialog(Utils.GetString("LimitTips"), Utils.GetString("ExceedMaxSectionCountMessage"), MessageBoxButton.OK).ShowDialog();
			}
			return false;
		}
		return true;
	}

	public static async Task<bool> CheckHabitLimit()
	{
		List<HabitModel> obj = (await HabitDao.GetAllHabits())?.Where((HabitModel habit) => habit.SyncStatus != -1 && habit.Status != 1).ToList();
		long userLimit = Utils.GetUserLimit(Constants.LimitKind.HabitNumber);
		if (obj?.Count >= userLimit)
		{
			if (UserDao.IsPro())
			{
				new CustomerDialog(Utils.GetString("LimitTips"), string.Format(Utils.GetString("ProHabitsLimit"), userLimit), MessageBoxButton.OK).ShowDialog();
			}
			else
			{
				ProChecker.ShowUpgradeDialog(ProType.MoreHabits);
			}
			return false;
		}
		return true;
	}

	public static void WhenCheckInMidnight(string habitReminder, IToastShowWindow window = null)
	{
		if (_lastRemindDate.Date == DateTime.Today)
		{
			return;
		}
		bool flag = true;
		if (!string.IsNullOrEmpty(habitReminder))
		{
			string[] array = habitReminder.Split(',');
			if (array.Any())
			{
				string[] array2 = array;
				foreach (string text in array2)
				{
					if (text.Contains(":"))
					{
						string[] array3 = text.Split(':');
						if (array3.Length == 2 && int.TryParse(array3[0], out var result) && result < 3)
						{
							flag = false;
						}
					}
				}
			}
		}
		if (flag)
		{
			_lastRemindDate = DateTime.Now;
			string text2 = string.Format(Utils.GetString("NotifyCheckHabitMidnight"), DateTime.Now.ToString("HH:mm"));
			if (window == null)
			{
				Utils.Toast(text2);
			}
			else
			{
				window.TryToastString(null, text2);
			}
		}
	}

	public static async Task<bool> CheckHabitStartDate(HabitModel habit, DateTime date)
	{
		if (habit.GetStartDate().Date <= date)
		{
			return true;
		}
		return false;
	}

	public static string GetIconBackColor(string iconRes, string color)
	{
		string text = null;
		if (!string.IsNullOrEmpty(iconRes))
		{
			if (IconInfos.TryGetValue(iconRes, out var value))
			{
				text = value.BackColor;
			}
			else if (iconRes.StartsWith("txt_"))
			{
				text = color;
			}
		}
		if (string.IsNullOrEmpty(text))
		{
			text = ColorHelper.ColorConverter.ConvertToString(ThemeUtil.GetColorValue("ColorPrimary"));
		}
		return text;
	}

	public static string GetWidgetTextColor(string iconRes, string color)
	{
		if (!string.IsNullOrEmpty(iconRes))
		{
			if (IconInfos.TryGetValue(iconRes, out var value))
			{
				return value.TextColor;
			}
			if (iconRes.StartsWith("txt_"))
			{
				color = ((color == null) ? string.Empty : color);
				if (TextIconColorDict.TryGetValue(color, out var value2))
				{
					return value2;
				}
				return TextIconColorDict[string.Empty];
			}
		}
		return TextIconColorDict[string.Empty];
	}
}
