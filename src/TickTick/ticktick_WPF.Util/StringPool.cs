using System.Collections.Concurrent;

namespace ticktick_WPF.Util;

public static class StringPool
{
	private static ConcurrentDictionary<string, string> _stringPool = new ConcurrentDictionary<string, string>();

	public static string GetOrCreate(string str)
	{
		if (string.IsNullOrEmpty(str))
		{
			return str;
		}
		if (!_stringPool.TryGetValue(str, out var value))
		{
			_stringPool[str] = str;
			return str;
		}
		return value;
	}
}
