using System;
using System.Collections.Generic;
using Ical.Net.DataTypes;

namespace ticktick_WPF.Util.DateParser;

public interface IParser
{
	RecurrencePattern MatchRepeat(string matchStr, ref DateTime? date, DateTime baseDate, ref List<string> recgonizeStrList);

	bool MatchAfterTime(string matchStr, ref DateTime? baseTime, ref List<string> recgonizeStrList);

	TickTickDuration MatchAdvanceTime(string matchStr, ref List<string> recgonizeStrList);

	DateTime? MatchWeekday(string text, DateTime baseDate, ref List<string> recgonizeStrList);

	DateTime? MatchDate(string text, DateTime baseDate, ref List<string> recgonizeStrList);

	DateTime? MatchMonth(string text, DateTime baseDate, ref List<string> recgonizeStrList);

	DateTime? MatchSpecialDay(string text, DateTime baseDate, ref List<string> recgonizeStrList);

	bool MatchTime(string text, ref DateTime? parseDate, DateTime baseDate, ref List<string> recgonizeStrList);

	bool MatchSpecialTime(string text, ref DateTime? parseDate, DateTime baseDate, ref List<string> recgonizeStrList);
}
