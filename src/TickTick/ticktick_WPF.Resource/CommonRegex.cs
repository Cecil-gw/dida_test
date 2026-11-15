using System.Text.RegularExpressions;

namespace ticktick_WPF.Resource;

public static class CommonRegex
{
	public static readonly Regex SpanFilterRegex = new Regex("span\\((.*)~(.*)\\)");
}
