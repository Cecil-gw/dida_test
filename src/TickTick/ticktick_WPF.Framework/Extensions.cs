using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace ticktick_WPF.Framework;

internal static class Extensions
{
	public static string DescriptionAttr(this Enum source)
	{
		DescriptionAttribute[] array = (DescriptionAttribute[])source.GetType().GetField(source.ToString()).GetCustomAttributes(typeof(DescriptionAttribute), inherit: false);
		if (array != null && array.Length != 0)
		{
			return array[0].Description;
		}
		return source.ToString();
	}

	public static DateTime AddDaysSafely(this DateTime dt, double days)
	{
		double num = 864000000000.0;
		long num2 = (long)(days * num);
		if (num2 > 0)
		{
			if (DateTime.MaxValue.Ticks - num2 < dt.Ticks)
			{
				return DateTime.MaxValue;
			}
		}
		else if (num2 < 0 && DateTime.MinValue.Ticks - num2 > dt.Ticks)
		{
			return DateTime.MinValue;
		}
		return dt.AddTicks(num2);
	}

	public static IEnumerable<List<T>> ChunkBy<T>(this List<T> source, int chunkSize)
	{
		for (int i = 0; i < source.Count; i += chunkSize)
		{
			yield return source.GetRange(i, Math.Min(chunkSize, source.Count - i));
		}
	}

	public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int chunkSize)
	{
		using IEnumerator<T> enumerator = source.GetEnumerator();
		while (enumerator.MoveNext())
		{
			List<T> list = new List<T>(chunkSize);
			do
			{
				list.Add(enumerator.Current);
			}
			while (list.Count < chunkSize && enumerator.MoveNext());
			yield return list;
		}
	}
}
