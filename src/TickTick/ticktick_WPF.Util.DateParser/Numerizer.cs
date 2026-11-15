using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace ticktick_WPF.Util.DateParser;

public class Numerizer
{
	protected class DirectNum
	{
		private readonly Regex _name;

		private readonly string _number;

		public DirectNum(string name, string number)
		{
			_name = new Regex(name, RegexOptions.IgnoreCase);
			_number = number;
		}

		public Regex GetName()
		{
			return _name;
		}

		public string GetNumber()
		{
			return _number;
		}
	}

	protected class TenPrefix
	{
		private readonly Regex _name;

		private readonly long _number;

		public TenPrefix(string name, long number)
		{
			_name = new Regex("(?:" + name + ")( *\\d(?=\\D|$))*", RegexOptions.IgnoreCase);
			_number = number;
		}

		public Regex GetName()
		{
			return _name;
		}

		public long GetNumber()
		{
			return _number;
		}
	}

	protected class BigPrefix
	{
		private readonly Regex _name;

		private readonly long _number;

		public BigPrefix(string name, long number)
		{
			_name = new Regex("(\\d*) *" + name, RegexOptions.IgnoreCase);
			_number = number;
		}

		public Regex GetName()
		{
			return _name;
		}

		public long GetNumber()
		{
			return _number;
		}
	}

	private static readonly Regex Dehalfer;

	private static readonly Regex Dehaalfer;

	protected static DirectNum[] DirectNums;

	protected static TenPrefix[] TenPrefixes;

	protected static BigPrefix[] BigPrefixes;

	static Numerizer()
	{
		Dehalfer = new Regex("a half", RegexOptions.IgnoreCase);
		Dehaalfer = new Regex("(\\d+)(?: | and |-)*haAlf", RegexOptions.IgnoreCase);
		DirectNums = new List<DirectNum>
		{
			new DirectNum("first", "1"),
			new DirectNum("second", "2"),
			new DirectNum("third", "3"),
			new DirectNum("fourth", "4"),
			new DirectNum("fifth", "5"),
			new DirectNum("sixth", "6"),
			new DirectNum("seventh", "7"),
			new DirectNum("eighth", "8"),
			new DirectNum("ninth", "9"),
			new DirectNum("tenth", "10"),
			new DirectNum("eleventh", "11"),
			new DirectNum("twelfth", "12"),
			new DirectNum("thirteenth", "13"),
			new DirectNum("fourteenth", "14"),
			new DirectNum("fifteenth", "15"),
			new DirectNum("sixteenth", "16"),
			new DirectNum("seventeenth", "17"),
			new DirectNum("eighteenth", "18"),
			new DirectNum("nineteenth", "19"),
			new DirectNum("twentieth", "20"),
			new DirectNum("thirtieth", "30"),
			new DirectNum("eleven", "11"),
			new DirectNum("twelve", "12"),
			new DirectNum("thirteen", "13"),
			new DirectNum("fourteen", "14"),
			new DirectNum("fifteen", "15"),
			new DirectNum("sixteen", "16"),
			new DirectNum("seventeen", "17"),
			new DirectNum("eighteen", "18"),
			new DirectNum("nineteen", "19"),
			new DirectNum("ninteen", "19"),
			new DirectNum("zero", "0"),
			new DirectNum("one", "1"),
			new DirectNum("two", "2"),
			new DirectNum("three", "3"),
			new DirectNum("four(\\W|$)", "4$1"),
			new DirectNum("five", "5"),
			new DirectNum("six(\\W|$)", "6$1"),
			new DirectNum("seven(\\W|$)", "7$1"),
			new DirectNum("eight(\\W|$)", "8$1"),
			new DirectNum("nine(\\W|$)", "9$1"),
			new DirectNum("ten", "10"),
			new DirectNum("\\ba\\b", "1")
		}.ToArray();
		TenPrefixes = new List<TenPrefix>
		{
			new TenPrefix("twenty", 20L),
			new TenPrefix("thirty", 30L),
			new TenPrefix("fourty", 40L),
			new TenPrefix("fifty", 50L),
			new TenPrefix("sixty", 60L),
			new TenPrefix("seventy", 70L),
			new TenPrefix("eighty", 80L),
			new TenPrefix("ninety", 90L),
			new TenPrefix("ninty", 90L)
		}.ToArray();
		BigPrefixes = new List<BigPrefix>
		{
			new BigPrefix("hundred", 100L),
			new BigPrefix("thousand", 1000L),
			new BigPrefix("million", 1000000L),
			new BigPrefix("billion", 1000000000L),
			new BigPrefix("trillion", 1000000000000L)
		}.ToArray();
	}

	public static string Numerize(string str)
	{
		string input = str;
		input = Dehalfer.Replace(input, "haAlf");
		input = DirectNums.Aggregate(input, (string current, DirectNum dn) => dn.GetName().Replace(current, dn.GetNumber()));
		TenPrefix[] tenPrefixes = TenPrefixes;
		foreach (TenPrefix tenPrefix in tenPrefixes)
		{
			Match match = tenPrefix.GetName().Match(input);
			if (match.Success)
			{
				do
				{
					input = tenPrefix.GetName().Replace(input, string.IsNullOrWhiteSpace(match.Groups[1].Value) ? tenPrefix.GetNumber().ToString() : (tenPrefix.GetNumber() + long.Parse(match.Groups[1].Value.Trim())).ToString());
					match = tenPrefix.GetName().Match(input);
				}
				while (match.Success);
			}
		}
		BigPrefix[] bigPrefixes = BigPrefixes;
		foreach (BigPrefix bigPrefix in bigPrefixes)
		{
			Match match2 = bigPrefix.GetName().Match(input);
			if (match2.Success)
			{
				do
				{
					input = bigPrefix.GetName().Replace(input, string.IsNullOrWhiteSpace(match2.Groups[1].Value) ? bigPrefix.GetNumber().ToString() : (bigPrefix.GetNumber() + long.Parse(match2.Groups[1].Value.Trim())).ToString());
					match2 = bigPrefix.GetName().Match(input);
				}
				while (match2.Success);
			}
		}
		Match match3 = Dehaalfer.Match(input);
		if (match3.Success)
		{
			do
			{
				input = Dehaalfer.Replace(input, (float.Parse(match3.Groups[1].Value.Trim()) + 0.5f).ToString(CultureInfo.InvariantCulture));
				match3 = Dehaalfer.Match(input);
			}
			while (match3.Success);
		}
		return input;
	}
}
