using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Habit;

public static class HabitStatisticsUtils
{
	private static DateTime ParseCheckinStamp(string stamp)
	{
		return DateTime.ParseExact(stamp, "yyyyMMdd", CultureInfo.InvariantCulture);
	}

	public static MonthPlanInfo GetPlanDaysInMonth(string repeatRule, DateTime start, DateTime end, List<HabitCheckInModel> checkIns, string[] habitExDates)
	{
		HabitRepeatInfo info = HabitUtils.BuildHabitRepeatInfo(repeatRule, habitExDates);
		return GetMonthPlanInfo(start.Date, end.Date, info, checkIns);
	}

	private static MonthPlanInfo GetMonthPlanInfo(DateTime start, DateTime end, HabitRepeatInfo info, List<HabitCheckInModel> checkIns)
	{
		MonthPlanInfo monthPlanInfo = new MonthPlanInfo();
		if (info.Type == HabitRepeatType.TimesInWeek)
		{
			DateTime weekStart = Utils.GetWeekStart(start);
			DateTime dateTime = weekStart.AddDays(6.0);
			DateTime weekStart2 = Utils.GetWeekStart(end);
			DateTime dateTime2 = weekStart2.AddDays(6.0);
			int num = (int)((weekStart2 - dateTime).TotalDays / 7.0);
			int firstWeekCount = info.Count;
			int lastWeekCount = info.Count;
			if (weekStart != start || dateTime2 != end)
			{
				int dateNum = DateUtils.GetDateNum(weekStart);
				int dateNum2 = DateUtils.GetDateNum(start);
				int dateNum3 = DateUtils.GetDateNum(dateTime2);
				int dateNum4 = DateUtils.GetDateNum(end);
				foreach (HabitCheckInModel item in checkIns.Where((HabitCheckInModel checkIn) => firstWeekCount > 0 || lastWeekCount > 0))
				{
					if ((item.CheckStatus == 2 || (item.Value > item.Goal && item.CheckStatus != 1)) && int.TryParse(item.CheckinStamp, out var result))
					{
						if (firstWeekCount > 0 && result >= dateNum && result < dateNum2)
						{
							firstWeekCount--;
						}
						if (lastWeekCount > 0 && result <= dateNum3 && result > dateNum4)
						{
							lastWeekCount--;
						}
					}
				}
			}
			firstWeekCount = ((!(weekStart == weekStart2) || weekStart.Month != start.Month) ? Math.Min(firstWeekCount, (dateTime.Date - start.Date).Days + 1) : 0);
			lastWeekCount = ((!(weekStart == weekStart2) || dateTime2.Month != end.Month) ? Math.Min(lastWeekCount, (end.Date - weekStart2.Date).Days + 1) : 0);
			monthPlanInfo.Count = Math.Min((end.Date - start.Date).Days + 1, num * info.Count + firstWeekCount + lastWeekCount);
			monthPlanInfo.StartDate = start;
			monthPlanInfo.EndDate = end;
		}
		else if (info.Type == HabitRepeatType.ByDay)
		{
			int num2 = 0;
			DateTime dateTime3 = start;
			while (dateTime3 <= end)
			{
				if (info.ByDays.Contains(dateTime3.DayOfWeek))
				{
					num2++;
				}
				dateTime3 = dateTime3.AddDays(1.0);
			}
			monthPlanInfo.StartDate = start;
			monthPlanInfo.EndDate = end;
			monthPlanInfo.Count = num2;
		}
		else
		{
			monthPlanInfo.Count = (end - start).Days / info.Interval + 1;
			monthPlanInfo.StartDate = start;
			monthPlanInfo.EndDate = end;
		}
		return monthPlanInfo;
	}

	public static int GetBestStreakDays(string repeatRule, List<HabitCheckInModel> checkIns, string[] habitExDates)
	{
		if (checkIns == null || !checkIns.Any())
		{
			return 0;
		}
		if (checkIns.Count == 1)
		{
			return (checkIns[0].Value >= checkIns[0].Goal) ? 1 : 0;
		}
		HabitRepeatInfo habitRepeatInfo = HabitUtils.BuildHabitRepeatInfo(repeatRule, habitExDates);
		if (habitRepeatInfo.Type == HabitRepeatType.Daily)
		{
			return CalculateDailyBestStreak(checkIns, habitRepeatInfo.Interval);
		}
		if (habitRepeatInfo.Type == HabitRepeatType.TimesInWeek)
		{
			return CalculateNDaysInWeekBestStreak(checkIns, habitRepeatInfo.Count);
		}
		if (habitRepeatInfo.Type == HabitRepeatType.ByDay)
		{
			return CalculateByDaysBestStreak(checkIns, habitRepeatInfo.ByDays);
		}
		return CalculateDailyBestStreak(checkIns, 1);
	}

	private static int CalculateNDaysInWeekCurrentStreakDays(List<HabitCheckInModel> checkIns, int count)
	{
		DateTime weekStart = Utils.GetWeekStart(DateTime.Today);
		DateTime today = DateTime.Today;
		int num = GetMatchCountInSpan(weekStart, today, checkIns);
		for (int i = 1; i < 10000; i++)
		{
			DateTime start = weekStart.AddDays(7 * i * -1);
			DateTime end = start.AddDays(6.0);
			int matchCountInSpan = GetMatchCountInSpan(start, end, checkIns);
			num += GetMatchCountInSpan(start, end, checkIns);
			if (matchCountInSpan < count)
			{
				break;
			}
		}
		return num;
	}

	private static DateTime GetFirstStartDate(List<HabitCheckInModel> checkIns, List<DayOfWeek> byDays)
	{
		DateTime checkDate = DateTime.Today;
		if (checkIns.Exists((HabitCheckInModel checkIn) => checkIn.CheckinStamp == checkDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture) && checkIn.Value >= checkIn.Goal))
		{
			return checkDate;
		}
		for (int num = 1; num < 7; num++)
		{
			checkDate = checkDate.AddDays(-1.0);
			if (checkIns.Exists((HabitCheckInModel checkIn) => checkIn.CheckinStamp == checkDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture) && checkIn.Value >= checkIn.Goal) || byDays.Contains(checkDate.DayOfWeek))
			{
				return checkDate;
			}
			if (byDays.Contains(checkDate.DayOfWeek))
			{
				return checkDate;
			}
		}
		return checkDate;
	}

	private static int CalculateByDaysCurrentStreakDays(List<HabitCheckInModel> checkIns, List<DayOfWeek> byDays)
	{
		if (checkIns == null || !checkIns.Any())
		{
			return 0;
		}
		int num = 0;
		DateTime checkDate = GetFirstStartDate(checkIns, byDays);
		DateTime dateTime = ParseCheckinStamp(checkIns.First().CheckinStamp);
		while (checkDate >= dateTime)
		{
			bool num2 = byDays.Contains(checkDate.DayOfWeek) || checkDate == DateTime.Today;
			bool flag = checkIns.Exists((HabitCheckInModel checkIn) => checkIn.CheckinStamp == checkDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture) && checkIn.Value >= checkIn.Goal);
			if (num2 && !flag)
			{
				break;
			}
			checkDate = checkDate.AddDays(-1.0);
			if (flag)
			{
				num++;
			}
		}
		return num;
	}

	public static async Task<HabitStatisticsModel> CalculateHabitStatInfo(string habitId, string repeatRule)
	{
		List<HabitCheckInModel> list = await HabitCheckinCache.GetHabitCheckInsById(habitId);
		if (list == null || !list.Any())
		{
			return new HabitStatisticsModel();
		}
		List<HabitCheckInModel> snapshot = list.ToList();
		return await Task.Run(() => CalculateHabitStatInfo(snapshot, repeatRule));
	}

	public static HabitStatisticsModel CalculateHabitStatInfo(List<HabitCheckInModel> checkIns, string repeatRule)
	{
		checkIns = (from checkIn in checkIns
			where checkIn.IsComplete()
			orderby int.Parse(checkIn.CheckinStamp)
			select checkIn).ToList();
		HabitRepeatInfo habitRepeatInfo = HabitUtils.BuildHabitRepeatInfo(repeatRule);
		if (habitRepeatInfo.Type == HabitRepeatType.Daily)
		{
			return CalculateDailyStatInfo(checkIns, habitRepeatInfo.Interval);
		}
		if (habitRepeatInfo.Type == HabitRepeatType.TimesInWeek)
		{
			return CalculateNDaysInWeekStatInfo(checkIns, habitRepeatInfo.Count);
		}
		if (habitRepeatInfo.Type == HabitRepeatType.ByDay)
		{
			if (habitRepeatInfo.ByDays.Count == 7)
			{
				return CalculateDailyStatInfo(checkIns, 1);
			}
			return CalculateByDaysStatInfo(checkIns, habitRepeatInfo.ByDays);
		}
		return new HabitStatisticsModel();
	}

	private static HabitStatisticsModel CalculateByDaysStatInfo(List<HabitCheckInModel> checkIns, List<DayOfWeek> byDays)
	{
		return new HabitStatisticsModel
		{
			TotalCheckIns = GetTotalCheckInCount(checkIns),
			CurrentStreak = CalculateByDaysCurrentStreakDays(checkIns, byDays),
			MaxStreak = CalculateByDaysBestStreak(checkIns, byDays)
		};
	}

	private static int CalculateByDaysBestStreak(List<HabitCheckInModel> checkIns, List<DayOfWeek> byDays)
	{
		if (checkIns == null || checkIns.Count == 0)
		{
			return 0;
		}
		if (checkIns.Count == 1)
		{
			return (checkIns[0].Value >= checkIns[0].Goal) ? 1 : 0;
		}
		List<WeekCheckInStatInfo> list = new List<WeekCheckInStatInfo>();
		DateTime date = ParseCheckinStamp(checkIns[checkIns.Count - 1].CheckinStamp);
		DateTime dateTime = ParseCheckinStamp(checkIns[0].CheckinStamp);
		DateTime start = Utils.GetWeekStart(date);
		while (start.AddDays(7.0) > dateTime)
		{
			WeekCheckInStatInfo item = new WeekCheckInStatInfo(start, start.AddDays(6.0));
			list.Add(item);
			start = start.AddDays(-7.0);
		}
		if (list.Any())
		{
			foreach (WeekCheckInStatInfo item2 in list)
			{
				item2.Count = GetMatchCountInSpan(item2.WeekStart, item2.WeekEnd, checkIns);
				item2.Completed = IsByDaysMatchInWeek(item2.WeekStart, item2.WeekEnd, byDays, checkIns);
			}
		}
		Dictionary<int, List<WeekCheckInStatInfo>> dictionary = new Dictionary<int, List<WeekCheckInStatInfo>>();
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].Completed)
			{
				dictionary.Add(i, new List<WeekCheckInStatInfo> { list[i] });
				int j;
				for (j = i; j < list.Count - 1 && list[j + 1].Completed; j++)
				{
					dictionary[i].Add(list[j + 1]);
				}
				int num = j - i;
				i += num;
			}
		}
		if (dictionary.Any())
		{
			int num2 = 0;
			for (int k = 0; k < dictionary.Count; k++)
			{
				List<WeekCheckInStatInfo> value = dictionary.ElementAt(k).Value;
				WeekCheckInStatInfo weekCheckInStatInfo = value.Last();
				WeekCheckInStatInfo weekCheckInStatInfo2 = value.First();
				int num3 = value.Sum((WeekCheckInStatInfo week) => week.Count);
				int lastContinueCount = GetLastContinueCount(weekCheckInStatInfo.WeekStart.AddDays(-7.0), weekCheckInStatInfo.WeekStart.AddDays(-1.0), checkIns, byDays);
				int firstContinueCount = GetFirstContinueCount(weekCheckInStatInfo2.WeekEnd.AddDays(1.0), weekCheckInStatInfo2.WeekEnd.AddDays(7.0), checkIns, byDays);
				num3 = num3 + lastContinueCount + firstContinueCount;
				if (num3 > num2)
				{
					num2 = num3;
				}
			}
			return num2;
		}
		int num4 = 0;
		foreach (WeekCheckInStatInfo item3 in list)
		{
			int maxMatchInSpan = GetMaxMatchInSpan(item3.WeekStart, item3.WeekEnd, checkIns, byDays);
			if (maxMatchInSpan >= num4)
			{
				num4 = maxMatchInSpan;
			}
		}
		return num4;
	}

	private static int GetFirstContinueCount(DateTime start, DateTime end, List<HabitCheckInModel> checkIns, List<DayOfWeek> byDays)
	{
		int num = 0;
		DateTime checkDate = start;
		while (checkDate <= end)
		{
			bool num2 = byDays.Contains(checkDate.DayOfWeek);
			bool flag = checkIns.Exists((HabitCheckInModel checkIn) => checkIn.CheckinStamp == checkDate.Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture) && checkIn.Value >= checkIn.Goal);
			if (num2 && !flag)
			{
				break;
			}
			checkDate = checkDate.AddDays(1.0);
			if (flag)
			{
				num++;
			}
		}
		return num;
	}

	private static int GetLastContinueCount(DateTime start, DateTime end, List<HabitCheckInModel> checkIns, List<DayOfWeek> byDays)
	{
		int num = 0;
		DateTime checkDate = end;
		while (checkDate >= start)
		{
			bool num2 = byDays.Contains(checkDate.DayOfWeek);
			bool flag = checkIns.Exists((HabitCheckInModel checkIn) => checkIn.CheckinStamp == checkDate.Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture) && checkIn.Value >= checkIn.Goal);
			if (num2 && !flag)
			{
				break;
			}
			checkDate = checkDate.AddDays(-1.0);
			if (flag)
			{
				num++;
			}
		}
		return num;
	}

	private static bool IsByDaysMatchInWeek(DateTime start, DateTime end, List<DayOfWeek> byDays, List<HabitCheckInModel> checkIns)
	{
		foreach (DayOfWeek byDay in byDays)
		{
			while (start <= end)
			{
				if (start.DayOfWeek == byDay && !checkIns.Exists((HabitCheckInModel checkIn) => checkIn.CheckinStamp == start.ToString("yyyyMMdd", CultureInfo.InvariantCulture) && checkIn.Value >= checkIn.Goal))
				{
					return false;
				}
				start = start.AddDays(1.0);
			}
		}
		return true;
	}

	private static HabitStatisticsModel CalculateNDaysInWeekStatInfo(List<HabitCheckInModel> checkIns, int count)
	{
		return new HabitStatisticsModel
		{
			TotalCheckIns = GetTotalCheckInCount(checkIns),
			CurrentStreak = CalculateNDaysInWeekCurrentStreakDays(checkIns, count),
			MaxStreak = CalculateNDaysInWeekBestStreak(checkIns, count)
		};
	}

	private static int CalculateNDaysInWeekBestStreak(List<HabitCheckInModel> checkIns, int count)
	{
		int result;
		List<HabitCheckInModel> list = checkIns?.Where((HabitCheckInModel c) => int.TryParse(c.CheckinStamp, out result) && result > 20000101).ToList();
		if (list == null || list.Count == 0)
		{
			return 0;
		}
		if (list.Count == 1)
		{
			return (list[0].Value >= list[0].Goal) ? 1 : 0;
		}
		List<WeekCheckInStatInfo> list2 = new List<WeekCheckInStatInfo>();
		DateTime date = ParseCheckinStamp(list[list.Count - 1].CheckinStamp);
		DateTime dateTime = ParseCheckinStamp(list[0].CheckinStamp);
		DateTime start = Utils.GetWeekStart(date);
		while (start.AddDays(7.0) > dateTime)
		{
			WeekCheckInStatInfo item = new WeekCheckInStatInfo(start, start.AddDays(6.0));
			list2.Add(item);
			start = start.AddDays(-7.0);
		}
		if (list2.Any())
		{
			foreach (WeekCheckInStatInfo item2 in list2)
			{
				item2.Count = GetMatchCountInSpan(item2.WeekStart, item2.WeekEnd, list);
				item2.Completed = item2.Count >= count || (item2.WeekStart <= DateTime.Today && item2.WeekEnd >= DateTime.Today);
			}
		}
		Dictionary<int, List<WeekCheckInStatInfo>> dictionary = new Dictionary<int, List<WeekCheckInStatInfo>>();
		int num;
		for (num = 0; num < list2.Count; num++)
		{
			dictionary.Add(num, new List<WeekCheckInStatInfo> { list2[num] });
			int num2 = num;
			while (num2 < list2.Count - 1 && (list2[num2].Completed || list2[num2 + 1].Completed))
			{
				num2++;
				dictionary[num].Add(list2[num2]);
				if (!list2[num2].Completed)
				{
					break;
				}
			}
			num = Math.Max(num, num2 - 1);
		}
		if (dictionary.Any())
		{
			int num3 = 0;
			for (int num4 = 0; num4 < dictionary.Count; num4++)
			{
				int num5 = dictionary.ElementAt(num4).Value.Sum((WeekCheckInStatInfo week) => week.Count);
				num5 = num5;
				if (num5 > num3)
				{
					num3 = num5;
				}
			}
			return num3;
		}
		return CalculateDailyBestStreak(list, 1);
	}

	private static int GetMaxMatchInSpan(DateTime start, DateTime end, List<HabitCheckInModel> checkIns, List<DayOfWeek> byDays)
	{
		int matchCountInSpan = GetMatchCountInSpan(start, end, checkIns);
		int weekStartContinuousDays = GetWeekStartContinuousDays(start, checkIns, byDays);
		int weekEndContinuousDays = GetWeekEndContinuousDays(end, checkIns, byDays);
		int weekEndContinuousDays2 = GetWeekEndContinuousDays(start.AddDays(-1.0), checkIns, byDays);
		int weekStartContinuousDays2 = GetWeekStartContinuousDays(end.AddDays(1.0), checkIns, byDays);
		if (weekStartContinuousDays <= 0 && weekEndContinuousDays <= 0)
		{
			return matchCountInSpan;
		}
		if (weekStartContinuousDays > 0 && weekEndContinuousDays <= 0)
		{
			return weekStartContinuousDays + weekEndContinuousDays2;
		}
		if (weekStartContinuousDays <= 0 && weekEndContinuousDays > 0)
		{
			return weekEndContinuousDays + weekStartContinuousDays2;
		}
		if (weekStartContinuousDays > 0 && weekEndContinuousDays > 0)
		{
			int num = weekStartContinuousDays + weekEndContinuousDays2;
			int num2 = weekEndContinuousDays + weekStartContinuousDays2;
			if (num <= num2)
			{
				return num2;
			}
			return num;
		}
		return matchCountInSpan;
	}

	private static int GetWeekStartContinuousDays(DateTime start, List<HabitCheckInModel> checkIns, List<DayOfWeek> byDays)
	{
		int num = 0;
		for (int i = 0; i < 7; i++)
		{
			DateTime currentDay = start.AddDays(i);
			bool num2 = checkIns.Exists((HabitCheckInModel day) => day.Value >= day.Goal && day.CheckinStamp == currentDay.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
			bool flag = byDays.Contains(currentDay.DayOfWeek);
			if (num2)
			{
				num++;
			}
			if (!num2 && flag)
			{
				break;
			}
		}
		return num;
	}

	private static int GetWeekEndContinuousDays(DateTime end, List<HabitCheckInModel> checkIns, List<DayOfWeek> byDays)
	{
		int num = 0;
		for (int i = 0; i < 7; i++)
		{
			DateTime currentDay = end.AddDays(i * -1);
			bool num2 = checkIns.Exists((HabitCheckInModel day) => day.Value >= day.Goal && day.CheckinStamp == currentDay.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
			bool flag = byDays.Contains(currentDay.DayOfWeek);
			if (num2)
			{
				num++;
			}
			if (!num2 && flag)
			{
				break;
			}
		}
		return num;
	}

	private static int GetMatchCountInSpan(DateTime start, DateTime end, List<HabitCheckInModel> checkIns)
	{
		int num = 0;
		DateTime checkDate = start;
		while (checkDate <= end)
		{
			if (checkIns.Exists((HabitCheckInModel checkIn) => checkIn.CheckinStamp == checkDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture) && checkIn.IsComplete()))
			{
				num++;
			}
			checkDate = checkDate.AddDays(1.0);
		}
		return num;
	}

	public static int GetTotalCheckInCount(IEnumerable<HabitCheckInModel> checkIns)
	{
		return checkIns?.Count((HabitCheckInModel checkIn) => checkIn.Value >= checkIn.Goal) ?? 0;
	}

	private static HabitStatisticsModel CalculateDailyStatInfo(List<HabitCheckInModel> checkIns, int interval)
	{
		return new HabitStatisticsModel
		{
			TotalCheckIns = GetTotalCheckInCount(checkIns),
			CurrentStreak = CalculateDailyCurrentStreak(checkIns, interval),
			MaxStreak = CalculateDailyBestStreak(checkIns, interval)
		};
	}

	private static int CalculateDailyBestStreak(List<HabitCheckInModel> checkIns, int interval)
	{
		List<HabitCheckInModel> list = checkIns?.Where((HabitCheckInModel c) => int.TryParse(c.CheckinStamp, out var result) && result > 20000101).ToList();
		if (list == null || list.Count == 0)
		{
			return 0;
		}
		if (list.Count == 1)
		{
			return (list[0].Value >= list[0].Goal) ? 1 : 0;
		}
		list = list.Where((HabitCheckInModel c) => c.IsComplete()).ToList();
		int num = 0;
		if (list.Count >= 1)
		{
			List<DateTime> list2 = list.Select((HabitCheckInModel ch) => DateTime.ParseExact(ch.CheckinStamp, "yyyyMMdd", CultureInfo.InvariantCulture)).ToList();
			list2.Sort();
			int num2 = 0;
			DateTime dateTime = list2.Min();
			foreach (DateTime item in list2)
			{
				if ((item - dateTime).TotalDays <= (double)interval)
				{
					num2++;
				}
				else
				{
					num = Math.Max(num, num2);
					num2 = 1;
				}
				dateTime = item;
			}
			num = Math.Max(num, num2);
		}
		return num;
	}

	private static int CalculateDailyCurrentStreak(List<HabitCheckInModel> checkIns, int interval)
	{
		int result;
		List<HabitCheckInModel> source = checkIns?.Where((HabitCheckInModel c) => int.TryParse(c.CheckinStamp, out result) && result > 20000101).ToList();
		if (checkIns == null || checkIns.Count == 0)
		{
			return 0;
		}
		if (source.Last((HabitCheckInModel checkIn) => checkIn.IsComplete()) == null)
		{
			return 0;
		}
		int num = 0;
		try
		{
			List<DateTime> list = source.Select((HabitCheckInModel checkIn) => ParseCheckinStamp(checkIn.CheckinStamp)).ToList();
			list.Sort((DateTime a, DateTime b) => b.CompareTo(a));
			DateTime dateTime = DateTime.Today;
			foreach (DateTime item in list)
			{
				if ((dateTime - item).TotalDays <= (double)interval)
				{
					num++;
					dateTime = item;
					continue;
				}
				break;
			}
		}
		catch (Exception)
		{
		}
		return num;
	}
}
