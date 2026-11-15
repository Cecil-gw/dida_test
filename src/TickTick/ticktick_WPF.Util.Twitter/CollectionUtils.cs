using System;
using System.Collections.ObjectModel;

namespace ticktick_WPF.Util.Twitter;

public static class CollectionUtils
{
	public static T GetLastItem<T>(Collection<T> source, int fromIndex, Func<T, bool> fun)
	{
		if (source == null)
		{
			return default(T);
		}
		for (int num = Math.Min(fromIndex, source.Count - 1); num >= 0; num--)
		{
			if (fun(source[num]))
			{
				return source[num];
			}
		}
		return default(T);
	}

	public static T GetFirstItem<T>(Collection<T> source, int fromIndex, Func<T, bool> fun)
	{
		if (source == null)
		{
			return default(T);
		}
		for (int i = Math.Max(fromIndex, 0); i < source.Count; i++)
		{
			if (fun(source[i]))
			{
				return source[i];
			}
		}
		return default(T);
	}
}
