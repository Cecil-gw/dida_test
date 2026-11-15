using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace System.Globalization;

[Serializable]
public class VietnameseCalendar : Calendar
{
	private static readonly ResourceManager resource = new ResourceManager("mscorlib", Assembly.GetAssembly(typeof(int)));

	private static readonly int[] yearsCode = new int[400]
	{
		3193507, 5679952, 4336544, 2927457, 5415792, 3953128, 6345056, 4908208, 3631398, 5823136,
		4479824, 3217106, 5647072, 4104928, 2679506, 5163344, 3724630, 6075680, 4634256, 3300772,
		5789136, 4335056, 2926003, 5415600, 4040887, 6334800, 4895904, 3519141, 5942608, 4478384,
		3156852, 5645680, 4215545, 6574768, 5138768, 3698006, 6183584, 4631376, 3299028, 5786336,
		4367728, 2966867, 5296800, 3926183, 6346064, 4872864, 3452325, 5936592, 4606688, 3058356,
		5547216, 4117176, 6599312, 5027152, 3692375, 6172064, 4756944, 3296629, 5786032, 4367536,
		2991283, 5270160, 3845528, 6318928, 4991840, 3511141, 5935984, 4606320, 3172708, 5432480,
		3992170, 6478480, 5135056, 3746518, 6171360, 4756192, 3328725, 5687632, 4248736, 2872483,
		5289616, 3823527, 6313392, 4990416, 3577269, 5935792, 4499792, 3070292, 5551264, 3978576,
		2648914, 5133744, 3811190, 6169968, 4739760, 3320485, 5695824, 4221600, 2800291, 5286736,
		3951576, 6441696, 5023088, 3691733, 6083168, 4512080, 3233108, 5658272, 4233936, 2774482,
		5262048, 3843510, 6333648, 4772432, 3396181, 5813568, 4380320, 2928034, 5412272, 4147575,
		6572400, 5022896, 3585205, 6056528, 4615504, 3222356, 5647200, 4232560, 2904818, 5261680,
		3827046, 6214816, 4778576, 3369621, 5790416, 4467552, 3114723, 5411552, 4049111, 6474064,
		5035168, 3528870, 5944656, 4609696, 3253684, 5645776, 4231888, 2806450, 5286224, 3716439,
		6188192, 4765008, 3494741, 5787040, 4367792, 3097971, 5526192, 3975592, 6351184, 5008032,
		3583654, 5942096, 4606816, 3189476, 5678448, 4215392, 2683491, 5167424, 3726151, 6084256,
		4757200, 3427797, 5917392, 4367568, 2938036, 5419600, 3986776, 6337856, 4896160, 3626406,
		6067632, 4606384, 3189108, 5678256, 4237904, 2730578, 5139744, 3779911, 6204256, 4756336,
		3427061, 5917040, 4482224, 2913443, 5302864, 4024920, 6444704, 4893392, 3577557, 6066912,
		4639072, 3070292, 5559456, 4119120, 2782546, 5133984, 3712935, 6202832, 4887216, 3320501,
		5810512, 4371616, 2931364, 5287248, 3954137, 6441888, 5023152, 3625334, 6050416, 4614448,
		3176756, 5532320, 4107600, 2775890, 5262176, 3712742, 6202592, 4772448, 3336805, 5690656,
		4250272, 2971299, 5396176, 3951355, 6441424, 5022928, 3657910, 5943888, 4502816, 3071269,
		5551520, 4085200, 2774450, 5261744, 3843447, 6202544, 4762192, 3387989, 5795104, 4238688,
		2968419, 5395312, 4082152, 6343024, 5002416, 3631270, 5954128, 4479648, 3122852, 5548752,
		4215520, 2675427, 5163344, 3724631, 6214816, 4643152, 3300693, 5789344, 4368080, 2905556,
		5395120, 3975608, 6465840, 4895888, 3454630, 5942608, 4609440, 3058532, 5547376, 4215472,
		2797939, 5138736, 3697463, 6187680, 4762960, 3353301, 5778272, 4367728, 3035876, 5296480,
		3860824, 6346016, 4905616, 3496614, 5920464, 4598496, 3189204, 5546704, 4116816, 2681170,
		5158176, 3725095, 6204832, 4871600, 3550645, 5916080, 4498096, 3060404, 5548368, 3978585,
		6449952, 5025104, 3692390, 6050672, 4736368, 3302772, 5788336, 4221264, 2783571, 5266080,
		3910311, 6203088, 4868832, 3515109, 5940560, 4379296, 3007140, 5428560, 4086459, 6444704,
		5019344, 3754422, 6179504, 4630736, 3200181, 5681808, 4240720, 2780498, 5262752, 3904871,
		6329712, 4868528, 3451253, 5924016, 4483728, 2931348, 5401424, 4074336, 2665313, 5018992,
		3689190, 6082912, 4646048, 3075365, 5560976, 4217680, 2897619, 5253856, 3838935, 6329040,
		4901200, 3331414, 5813408, 4372112, 3038612, 5395888, 4072954, 6563248, 5149360, 3582646,
		6056272, 4617376, 3256997, 5549392, 4216224, 2796403, 5383536, 3822455, 6312624, 4876624,
		3435862, 5790368, 4369232, 3036884, 5524192, 3974512, 2647250, 5034592, 3599014, 5952848,
		4610720, 3190181, 5674448, 4213456, 2795955, 5285072, 3855031, 6206032, 4764992, 3396950
	};

	private static readonly string[] selestialStems = new string[10] { "Giáp", "Ất", "Bính", "Đinh", "Mậu", "Kỷ", "Canh", "Tân", "Nhâm", "Quý" };

	private static readonly string[] terrestrialBranches = new string[12]
	{
		"Tý", "Sửu", "Dần", "Mão", "Thìn", "Tỵ", "Ngọ", "Mùi", "Thân", "Dậu",
		"Tuất", "Hợi"
	};

	private static readonly string[] monthSpeechNames = new string[12]
	{
		"Giêng", "Hai", "Ba", "Tư", "Năm", "Sáu", "Bảy", "Tám", "Chín", "Mười",
		"Một", "Chạp"
	};

	private static readonly string[] propitiousHour = new string[6] { "110100101100", "001101001011", "110011010010", "101100110100", "001011001101", "010010110011" };

	private static readonly string[] minorSolarTerms = new string[24]
	{
		"Xuân Phân", "Thanh Minh", "Cốc Vũ", "Lập Hạ", "Tiểu Mãn", "Mang Chủng", "Hạ Chí", "Tiểu Thử", "Đại Thử", "Lập Thu",
		"Xử Thử", "Bạch Lộ", "Thu Phân", "Hàn Lộ", "Sương Giáng", "Lập Đông", "Tiểu Tuyết", "Đại Tuyết", "Đông Chí", "Tiểu Hàn",
		"Đại Hàn", "Lập Xuân", "Vũ Thủy", "Kinh Trập"
	};

	public const int VietnameseEra = 1;

	private static readonly DateTime minDate = new DateTime(1800, 1, 25);

	private static readonly DateTime maxDate = new DateTime(2199, 12, 31, 23, 59, 59, 999);

	public override CalendarAlgorithmType AlgorithmType => CalendarAlgorithmType.LunisolarCalendar;

	[ComVisible(false)]
	public override int[] Eras => new int[1] { 1 };

	[ComVisible(false)]
	public override DateTime MinSupportedDateTime => minDate;

	[ComVisible(false)]
	public override DateTime MaxSupportedDateTime => maxDate;

	public override int TwoDigitYearMax
	{
		get
		{
			if (base.TwoDigitYearMax < 0)
			{
				base.TwoDigitYearMax = GetSystemTwoDigitYearSetting(1, GetYear(maxDate));
			}
			return base.TwoDigitYearMax;
		}
		set
		{
			VerifyWritable();
			if (value < MinCalendarYear || value > MaxCalendarYear)
			{
				throw new ArgumentOutOfRangeException("value", string.Format(CultureInfo.CurrentCulture, resource.GetString("ArgumentOutOfRange_Range"), MinCalendarYear, MaxCalendarYear));
			}
			base.TwoDigitYearMax = value;
		}
	}

	internal static int MinCalendarYear => minDate.Year;

	internal static int MaxCalendarYear => maxDate.Year;

	[DllImport("Kernel32.dll", BestFitMapping = false, CharSet = CharSet.Auto, ThrowOnUnmappableChar = true)]
	internal static extern int GetCalendarInfo(int localeId, int calendarId, int calendarType, StringBuilder data, int dataSize, ref int value);

	internal static int GetTwoDigitYearMax(int calendarId)
	{
		try
		{
			int value = 0;
			if (GetCalendarInfo(1066, calendarId, 536870960, null, 0, ref value) < 1)
			{
				Console.WriteLine("Native function Kernel32.GetCalendarInfo() returns 0." + Environment.NewLine + "Call GetLastError() to get extended error information." + Environment.NewLine + "GetLastError() can return one of the following error codes:" + Environment.NewLine + " * ERROR_INSUFFICIENT_BUFFER" + Environment.NewLine + " * ERROR_INVALID_FLAGS" + Environment.NewLine + " * ERROR_INVALID_PARAMETER");
				return -1;
			}
			return value;
		}
		catch (Exception arg)
		{
			Console.WriteLine("Native function Kernel32.GetCalendarInfo() calling exception: {0}", arg);
		}
		return int.MinValue;
	}

	internal static int GetSystemTwoDigitYearSetting(int calendarId, int defaultYearValue)
	{
		int num = GetTwoDigitYearMax(calendarId);
		if (num < 0)
		{
			RegistryKey registryKey = null;
			try
			{
				registryKey = Registry.CurrentUser.OpenSubKey("Control Panel\\International\\Calendars\\TwoDigitYearMax", writable: false);
			}
			catch (ObjectDisposedException)
			{
			}
			catch (ArgumentException)
			{
			}
			if (registryKey != null)
			{
				try
				{
					object value = registryKey.GetValue(calendarId.ToString(CultureInfo.InvariantCulture));
					if (value != null)
					{
						try
						{
							num = int.Parse(value.ToString(), CultureInfo.InvariantCulture);
						}
						catch (ArgumentException)
						{
						}
						catch (FormatException)
						{
						}
						catch (OverflowException)
						{
						}
					}
				}
				finally
				{
					registryKey.Close();
				}
			}
			if (num < 0)
			{
				num = defaultYearValue;
			}
		}
		return num;
	}

	internal void VerifyWritable()
	{
		if (base.IsReadOnly)
		{
			throw new InvalidOperationException(resource.GetString("InvalidOperation_ReadOnly"));
		}
	}

	internal static void CheckTicksRange(long ticks)
	{
		if (ticks < minDate.Ticks || ticks > maxDate.Ticks)
		{
			throw new ArgumentOutOfRangeException("time", string.Format(CultureInfo.CurrentCulture, resource.GetString("ArgumentOutOfRange_CalendarRange"), minDate, maxDate));
		}
	}

	internal static void CheckEraRange(int era)
	{
		if (era != 0 && era != 1)
		{
			throw new ArgumentOutOfRangeException("era", resource.GetString("ArgumentOutOfRange_InvalidEraValue"));
		}
	}

	internal static void CheckYearRange(int year)
	{
		if (year < MinCalendarYear || year > MaxCalendarYear)
		{
			throw new ArgumentOutOfRangeException("year", string.Format(CultureInfo.CurrentCulture, resource.GetString("ArgumentOutOfRange_Range"), MinCalendarYear, MaxCalendarYear));
		}
	}

	internal static void CheckMonthRange(int month, int leapMonth)
	{
		switch (month)
		{
		case 13:
			if (leapMonth != 0)
			{
				break;
			}
			goto default;
		default:
			throw new ArgumentOutOfRangeException("month", resource.GetString("ArgumentOutOfRange_Month"));
		case 1:
		case 2:
		case 3:
		case 4:
		case 5:
		case 6:
		case 7:
		case 8:
		case 9:
		case 10:
		case 11:
		case 12:
			break;
		}
	}

	private static int GetYearCode(int year)
	{
		return yearsCode[year - 1800];
	}

	private static DateTime GetLunarNewYear(int year, DateTimeKind copied)
	{
		return new DateTime(year, 1, 1, 0, 0, 0, copied).AddDays(GetYearCode(year) >> 17);
	}

	private static DateTime GetLunarNewYear(DateTime time)
	{
		DateTime lunarNewYear = GetLunarNewYear(time.Year, time.Kind);
		if (lunarNewYear > time)
		{
			lunarNewYear = GetLunarNewYear(time.Year - 1, time.Kind);
		}
		return lunarNewYear;
	}

	private static int GetLunarLeapMonth(int year)
	{
		CheckYearRange(year);
		int num = GetYearCode(year) & 0xF;
		if (num <= 0)
		{
			return 0;
		}
		return num + 1;
	}

	private static int GetMonthLength(int year, int month, int leapMonth)
	{
		if (leapMonth > 0)
		{
			if (month == leapMonth)
			{
				month = 0;
			}
			else if (month > leapMonth)
			{
				month--;
			}
		}
		if (((GetYearCode(year) >> 16 - month) & 1) != 0)
		{
			return 30;
		}
		return 29;
	}

	[ComVisible(false)]
	public override int GetEra(DateTime time)
	{
		CheckTicksRange(time.Ticks);
		return 1;
	}

	public override int GetYear(DateTime time)
	{
		CheckTicksRange(time.Ticks);
		if (!(GetLunarNewYear(time.Year, time.Kind) > time))
		{
			return time.Year;
		}
		return time.Year - 1;
	}

	public override int GetMonth(DateTime time)
	{
		FromDateTime(time, out var _, out var month, out var _);
		return month;
	}

	public override int GetDayOfMonth(DateTime time)
	{
		FromDateTime(time, out var _, out var _, out var day);
		return day;
	}

	public override DayOfWeek GetDayOfWeek(DateTime time)
	{
		CheckTicksRange(time.Ticks);
		return (DayOfWeek)((int)Math.Floor((double)time.Ticks / 864000000000.0 + 1.0) % 7);
	}

	public override int GetDayOfYear(DateTime time)
	{
		FromDateTime(time, out var year, out var month, out var day, out var leapMonth);
		for (int i = 1; i < month; i++)
		{
			day += GetMonthLength(year, i, leapMonth);
		}
		return day;
	}

	public override bool IsLeapYear(int year, int era)
	{
		return GetLeapMonth(year, era) > 0;
	}

	public override bool IsLeapMonth(int year, int month, int era)
	{
		int leapMonth = GetLeapMonth(year, era);
		CheckMonthRange(month, leapMonth);
		if (leapMonth > 0)
		{
			return month == leapMonth;
		}
		return false;
	}

	public override bool IsLeapDay(int year, int month, int day, int era)
	{
		int leapMonth = GetLeapMonth(year, era);
		CheckMonthRange(month, leapMonth);
		int monthLength = GetMonthLength(year, month, leapMonth);
		if (day < 1 || day > monthLength)
		{
			throw new ArgumentOutOfRangeException("day", string.Format(CultureInfo.CurrentCulture, resource.GetString("ArgumentOutOfRange_Day"), monthLength, month));
		}
		if (leapMonth > 0)
		{
			return month == leapMonth;
		}
		return false;
	}

	public override int GetMonthsInYear(int year, int era)
	{
		if (!IsLeapYear(year, era))
		{
			return 12;
		}
		return 13;
	}

	public override int GetDaysInMonth(int year, int month, int era)
	{
		int leapMonth = GetLeapMonth(year, era);
		CheckMonthRange(month, leapMonth);
		return GetMonthLength(year, month, leapMonth);
	}

	public override int GetDaysInYear(int year, int era)
	{
		int leapMonth = GetLeapMonth(year, era);
		int num = 0;
		int num2 = ((leapMonth > 0) ? 13 : 12);
		for (int i = 1; i <= num2; i++)
		{
			num += GetMonthLength(year, i, leapMonth);
		}
		return num;
	}

	public override DateTime AddMonths(DateTime time, int months)
	{
		FromDateTime(time, out var year, out var month, out var day);
		month += months;
		while (month < 1 || month > GetMonthsInYear(year, 1))
		{
			if (month < 1)
			{
				year--;
				CheckYearRange(year);
				month += GetMonthsInYear(year, 1);
			}
			else
			{
				month -= GetMonthsInYear(year, 1);
				year++;
				CheckYearRange(year);
			}
		}
		int daysInMonth = GetDaysInMonth(year, month, 1);
		if (day > daysInMonth)
		{
			day = daysInMonth;
		}
		return ToDateTime(year, month, day, time.Hour, time.Minute, time.Second, time.Millisecond, 1);
	}

	public override DateTime AddYears(DateTime time, int years)
	{
		FromDateTime(time, out var year, out var month, out var day, out var leapMonth);
		year += years;
		CheckYearRange(year);
		leapMonth = GetLeapMonth(year, 0);
		if (month > 12 && leapMonth == 0)
		{
			month = 12;
		}
		int monthLength = GetMonthLength(year, month, leapMonth);
		if (day > monthLength)
		{
			day = monthLength;
		}
		return new DateTime(year, month, day, time.Hour, time.Minute, time.Second, time.Millisecond, this);
	}

	public override DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era)
	{
		int leapMonth = GetLeapMonth(year, era);
		CheckMonthRange(month, leapMonth);
		int monthLength = GetMonthLength(year, month, leapMonth);
		if (day < 1 || day > monthLength)
		{
			throw new ArgumentOutOfRangeException("day", string.Format(CultureInfo.CurrentCulture, resource.GetString("ArgumentOutOfRange_Day"), monthLength, month));
		}
		DateTime lunarNewYear = GetLunarNewYear(year, DateTimeKind.Unspecified);
		monthLength = day - 1;
		while (--month > 0)
		{
			monthLength += GetMonthLength(year, month, leapMonth);
		}
		return lunarNewYear.AddDays((double)monthLength + (double)hour / 24.0 + (double)minute / 1440.0 + (double)second / 86400.0 + (double)millisecond / 86400000.0);
	}

	public static void FromDateTime(DateTime time, out int year, out int month, out int day)
	{
		FromDateTime(time, out year, out month, out day, out var _);
	}

	public static void FromDateTime(DateTime time, out int year, out int month, out int day, out int leapMonth)
	{
		CheckTicksRange(time.Ticks);
		DateTime lunarNewYear = GetLunarNewYear(time);
		year = lunarNewYear.Year;
		leapMonth = GetLunarLeapMonth(year);
		int num = JulianDayNumber(time) - JulianDayNumber(lunarNewYear);
		int i = 0;
		int num2 = 0;
		month = 1;
		for (; i < num; i += num2)
		{
			num2 = GetMonthLength(year, month++, leapMonth);
		}
		day = 1;
		if (i > num)
		{
			month--;
			day += num - i + num2;
		}
	}

	public override int GetLeapMonth(int year, int era)
	{
		CheckEraRange(era);
		return GetLunarLeapMonth(year);
	}

	public override int ToFourDigitYear(int year)
	{
		year = base.ToFourDigitYear(year);
		CheckYearRange(year);
		return year;
	}

	public static int JulianDayNumber(DateTime time)
	{
		int num = (int)Math.Floor((14.0 - (double)time.Month) / 12.0);
		int num2 = time.Year + 4800 - num;
		int num3 = time.Month + 12 * num - 3;
		int num4 = time.Day + (int)Math.Floor((153.0 * (double)num3 + 2.0) / 5.0) + (int)Math.Floor(365.25 * (double)num2);
		int num5 = num4 - (int)Math.Floor(0.01 * (double)num2) + (int)Math.Floor(0.0025 * (double)num2) - 32045;
		if (num5 < 2299161)
		{
			num5 = num4 - 32083;
		}
		return num5;
	}

	public static double SunLongitude(DateTime dt)
	{
		DateTime time = dt.ToUniversalTime();
		double num = ((double)JulianDayNumber(time) + (double)time.Hour / 24.0 - 0.5 + (double)time.Minute / 1440.0 + (double)time.Second / 86400.0 + (double)time.Millisecond / 86400000.0 - 2451545.0) / 36525.0;
		double num2 = num * num;
		double num3 = Math.PI / 180.0;
		double num4 = 357.52911 + 35999.05029 * num - 0.0001537 * num2 - 4.8E-07 * num * num2;
		double num5 = 280.46646 + 36000.76983 * num + 0.0003032 * num2;
		double num6 = (1.914602 - 0.004817 * num - 1.4E-05 * num2) * Math.Sin(num3 * num4) + (0.019993 - 0.000101 * num) * Math.Sin(num3 * 2.0 * num4) + 0.000289 * Math.Sin(num3 * 3.0 * num4);
		double num7 = num5 + num6;
		double num8 = 125.04 - 1934.136 * num;
		double num9 = num7 - (0.00569 + 0.00478 * Math.Sin(num8 * num3));
		return num9 - (double)((int)Math.Floor(num9 / 360.0) * 360);
	}

	public static string GetMinorSolarTerms(DateTime dt)
	{
		CheckTicksRange(dt.Ticks);
		double num = SunLongitude(dt);
		if (num > 283.980685381734 && num < 283.998429466957)
		{
			num += 1.019314618264603;
		}
		else if (num > 268.982233589428 && num < 268.997418971464)
		{
			num += 1.017766410571303;
		}
		else if (num > 238.994360790922 && num < 238.998815336006)
		{
			num += 1.0056392090773516;
		}
		else if (num > 298.985299986357 && num < 298.999892901616)
		{
			num += 1.014700013642128;
		}
		else if (num > 313.988180094718 && num < 313.995561330842)
		{
			num += 1.011819905281312;
		}
		else if (num > 253.986425624963 && num < 253.997690579273)
		{
			num += 1.0135743750361217;
		}
		else if (num > 328.99464285669 && num < 328.997513050222)
		{
			num += 1.0053571433090627;
		}
		else if (num > 343.998197182546 && num < 343.999951809195)
		{
			num += 1.0018028174527065;
		}
		else if ((!(num > 194.002942346778) || !(num < 194.013760798233)) && (!(num > 179.000208750401) || !(num < 179.01862004219)) && (!(num > 164.000901415201) || !(num < 164.027376866403)) && (!(num > 149.000992194126) || !(num < 149.035876318057)) && (!(num > 134.000122471553) || !(num < 134.038649430489)) && (!(num > 119.00248079265) || !(num < 119.035483698587)) && (!(num > 104.001461964773) || !(num < 104.046198136007)) && (!(num > 89.0013457619861) || !(num < 89.0445839193854)) && (!(num > 74.000266017334) || !(num < 74.0402776596676)) && (!(num > 59.0034474952262) || !(num < 59.034047233342)) && (!(num > 44.0005194120248) || !(num < 44.026037863635)) && (!(num > 29.0003734693891) || !(num < 29.0216673913857)) && (!(num > 14.0025458727477) || !(num < 14.015670561109)) && (!(num > 359.002244547314) || !(num < 359.005330829566)) && Math.Round(num, 11) != 209.00033593538)
		{
			num += 1.0;
		}
		int num2 = (int)Math.Floor(num / 15.0);
		return minorSolarTerms[num2 % 24];
	}

	public static string GetYearName(int year)
	{
		CheckYearRange(year);
		return string.Format(CultureInfo.InvariantCulture, "{0} {1}", selestialStems[(year + 6) % 10], terrestrialBranches[(year + 8) % 12]);
	}

	public static string GetDayName(DateTime dt)
	{
		CheckTicksRange(dt.Ticks);
		int num = JulianDayNumber(dt);
		return string.Format(CultureInfo.InvariantCulture, "{0} {1}", selestialStems[(num + 9) % 10], terrestrialBranches[(num + 1) % 12]);
	}

	public static string GetMonthName(int year, int month)
	{
		int lunarLeapMonth = GetLunarLeapMonth(year);
		CheckMonthRange(month, lunarLeapMonth);
		int num = ((lunarLeapMonth > 0 && month >= lunarLeapMonth) ? (-1) : 0);
		return string.Format(CultureInfo.InvariantCulture, "{0} {1}{2}", selestialStems[(year * 12 + month + num + 3) % 10], terrestrialBranches[(month + num + 1) % 12], (lunarLeapMonth > 0 && month == lunarLeapMonth) ? " (nhuận)" : string.Empty);
	}

	public static string GetMonthSpeechName(int year, int month)
	{
		int lunarLeapMonth = GetLunarLeapMonth(year);
		CheckMonthRange(month, lunarLeapMonth);
		int num = ((lunarLeapMonth > 0 && month >= lunarLeapMonth) ? (-1) : 0);
		return string.Format(CultureInfo.InvariantCulture, "Tháng {0}{1}", monthSpeechNames[month + num - 1], (lunarLeapMonth > 0 && month == lunarLeapMonth) ? " (nhuận)" : string.Empty);
	}

	public static string GetPropitiousHour(DateTime dt, bool withHours = true)
	{
		int num = (JulianDayNumber(dt) + 1) % 12;
		string text = propitiousHour[num % 6];
		StringBuilder stringBuilder = new StringBuilder();
		string format = (withHours ? "{0} ({1}-{2}), " : "{0}, ");
		for (int i = 0; i < 12; i++)
		{
			if (text[i] != '0')
			{
				stringBuilder.AppendFormat(CultureInfo.CurrentCulture, format, terrestrialBranches[i], (i * 2 + 23) % 24, (i * 2 + 1) % 24);
			}
		}
		if (stringBuilder.Length > 0 && stringBuilder.ToString().EndsWith(", ", StringComparison.Ordinal))
		{
			stringBuilder.Length -= 2;
		}
		return stringBuilder.ToString();
	}

	public static string GetHourZeroName(DateTime time)
	{
		int num = JulianDayNumber(time);
		return string.Format(CultureInfo.InvariantCulture, "{0} {1}", selestialStems[(num - 1) * 2 % 10], terrestrialBranches[0]);
	}
}
