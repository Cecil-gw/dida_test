using System;
using System.Collections.Concurrent;

namespace ticktick_WPF.Util;

public static class DelayActionHandlerCenter
{
	private static ConcurrentDictionary<string, DelayActionHandler> _actionHandlers = new ConcurrentDictionary<string, DelayActionHandler>();

	public static void TryDoAction(string uid, EventHandler handler, int interval = 300)
	{
		if (!_actionHandlers.TryGetValue(uid, out var value))
		{
			value = new DelayActionHandler(interval);
			_actionHandlers.TryAdd(uid, value);
		}
		value.SetAction(handler);
		value.DoAction += delegate
		{
			if (_actionHandlers.TryRemove(uid, out var value2))
			{
				value2.Dispose();
			}
		};
		value.TryDoAction();
	}

	public static void RemoveAction(string id)
	{
		if (_actionHandlers.TryRemove(id, out var value))
		{
			value.StopAndClear();
		}
	}
}
