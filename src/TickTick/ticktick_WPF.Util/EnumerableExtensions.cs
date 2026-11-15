using System;
using System.Collections.Generic;
using System.Linq;

namespace ticktick_WPF.Util;

internal static class EnumerableExtensions
{
	public static Dictionary<TKey, TValue> ToDictionaryEx<TElement, TKey, TValue>(this IEnumerable<TElement> source, Func<TElement, TKey> keyGetter, Func<TElement, TValue> valueGetter)
	{
		Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();
		foreach (TElement item in source)
		{
			TKey val = keyGetter(item);
			if (val != null && !dictionary.ContainsKey(val))
			{
				dictionary.Add(val, valueGetter(item));
			}
		}
		return dictionary;
	}

	public static Dictionary<TKey, List<TValue>> GroupEx<TElement, TKey, TValue>(this IEnumerable<TElement> source, Func<TElement, TKey> keyGetter, Func<TElement, TValue> valueGetter)
	{
		Dictionary<TKey, List<TValue>> dictionary = new Dictionary<TKey, List<TValue>>();
		foreach (TElement item2 in source)
		{
			TKey val = keyGetter(item2);
			if (val != null)
			{
				TValue item = valueGetter(item2);
				if (dictionary.ContainsKey(val))
				{
					dictionary[val].Add(item);
					continue;
				}
				dictionary[val] = new List<TValue> { item };
			}
		}
		return dictionary;
	}

	public static List<TElement> DistinctEx<TElement, TValue>(this IEnumerable<TElement> source, Func<TElement, TValue> valueGetter)
	{
		Dictionary<TValue, TElement> dictionary = new Dictionary<TValue, TElement>();
		foreach (TElement item in source)
		{
			if (item != null)
			{
				TValue val = valueGetter(item);
				if (val != null && !dictionary.ContainsKey(val))
				{
					dictionary.Add(val, item);
				}
			}
		}
		return dictionary.Values.ToList();
	}
}
