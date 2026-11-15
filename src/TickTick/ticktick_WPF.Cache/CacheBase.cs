using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ticktick_WPF.Cache;

public abstract class CacheBase<T> : ICacheable
{
	private readonly ConcurrentDictionary<string, T> _cache = new ConcurrentDictionary<string, T>();

	public abstract Task Load();

	public async Task Clear()
	{
		_cache.Clear();
	}

	public ConcurrentDictionary<string, T> GetData()
	{
		return _cache;
	}

	public void Update(string key, T data)
	{
		if (data != null && key != null)
		{
			_cache[key] = data;
		}
	}

	public void Delete(string key)
	{
		if (key != null && _cache.ContainsKey(key))
		{
			_cache.TryRemove(key, out var _);
		}
	}

	protected void AssembleData(IEnumerable<T> data, Func<T, string> getId)
	{
		_cache.Clear();
		foreach (T datum in data)
		{
			string text = getId(datum);
			if (!string.IsNullOrEmpty(text) && !_cache.ContainsKey(text))
			{
				_cache.TryAdd(text, datum);
			}
		}
	}
}
