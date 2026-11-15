using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TickTickUtils.Lang;

public class UtilCollection
{
	public static Task InvokeIfAbsentAsync<T>(HashSet<T> set, T key, Func<Task> func)
	{
		return InvokeIfAbsentAsync(set, key, UtilRun.ToFuncTask<T>(func));
	}

	public static Task<R> InvokeIfAbsentAsync<T, R>(HashSet<T> set, T key, Func<Task<R>> func)
	{
		return InvokeIfAbsentAsync(set, key, UtilRun.ToFuncTask<T, R>(func));
	}

	public static async Task<R> InvokeIfAbsentAsync<T, R>(HashSet<T> set, T key, Func<T, Task<R>> func)
	{
		if (set == null || func == null)
		{
			string[] obj = new string[6]
			{
				"HashSet=",
				set?.ToString(),
				", key=",
				null,
				null,
				null
			};
			T val = key;
			obj[3] = val?.ToString();
			obj[4] = ", func=";
			obj[5] = func?.ToString();
			throw new NullReferenceException(string.Concat(obj));
		}
		lock (set)
		{
			if (set.Contains(key))
			{
				return default(R);
			}
			if (!set.Add(key))
			{
				return default(R);
			}
		}
		try
		{
			return await func(key);
		}
		finally
		{
			lock (set)
			{
				set.Remove(key);
			}
		}
	}

	public static void InvokeIfAbsent<T>(HashSet<T> set, T key, Action action)
	{
		InvokeIfAbsent(set, key, UtilRun.ToFunc<T>(action));
	}

	public static void InvokeIfAbsent<T>(HashSet<T> set, T key, Action<T> action)
	{
		InvokeIfAbsent(set, key, UtilRun.ToFunc(action));
	}

	public static R InvokeIfAbsent<T, R>(HashSet<T> set, T key, Func<T, R> func)
	{
		if (set == null || func == null)
		{
			string[] obj = new string[6]
			{
				"HashSet=",
				set?.ToString(),
				", key=",
				null,
				null,
				null
			};
			T val = key;
			obj[3] = val?.ToString();
			obj[4] = ", func=";
			obj[5] = func?.ToString();
			throw new NullReferenceException(string.Concat(obj));
		}
		lock (set)
		{
			if (set.Contains(key))
			{
				return default(R);
			}
			if (!set.Add(key))
			{
				return default(R);
			}
		}
		try
		{
			return func(key);
		}
		finally
		{
			lock (set)
			{
				set.Remove(key);
			}
		}
	}

	public static V GetValue<K, V>(IDictionary<K, V> dict, K key)
	{
		TryGetValue(dict, key, out var value);
		return value;
	}

	public static bool TryGetValue<K, V>(IDictionary<K, V> dict, K key, out V value)
	{
		if (dict == null || key == null)
		{
			value = default(V);
			return false;
		}
		return dict.TryGetValue(key, out value);
	}
}
