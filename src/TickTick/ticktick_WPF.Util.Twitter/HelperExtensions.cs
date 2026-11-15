using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ticktick_WPF.Util.Twitter;

public static class HelperExtensions
{
	public static string Join<T>(this IEnumerable<T> items, string separator)
	{
		return string.Join(separator, items.ToArray());
	}

	public static IEnumerable<T> Concat<T>(this IEnumerable<T> items, T value)
	{
		return items.Concat(new T[1] { value });
	}

	public static string EncodeRFC3986(this string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return string.Empty;
		}
		return Regex.Replace(Uri.EscapeDataString(value), "(%[0-9a-f][0-9a-f])", (Match c) => c.Value.ToUpper()).Replace("(", "%28").Replace(")", "%29")
			.Replace("$", "%24")
			.Replace("!", "%21")
			.Replace("*", "%2A")
			.Replace("'", "%27")
			.Replace("%7E", "~");
	}
}
