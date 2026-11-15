using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using TickTickUtils.Lang;

namespace ticktick_WPF.Util;

public class ThreadUtil
{
	public static void DetachedRunOnUiThread(Action action)
	{
		if (action == null)
		{
			return;
		}
		new Thread((ThreadStart)delegate
		{
			Application.Current?.Dispatcher?.Invoke(delegate
			{
				Utils.LogActionTimes(action, 400, null, action.Target?.ToString() + " " + action.Method);
			}, DispatcherPriority.Normal);
		}).Start();
	}

	public static void DetachedRunOnUiBackThread(Action action)
	{
		if (action == null)
		{
			return;
		}
		new Thread((ThreadStart)delegate
		{
			Application.Current?.Dispatcher?.Invoke(delegate
			{
				Utils.LogActionTimes(action, 400, null, action.Target?.ToString() + " " + action.Method);
			}, DispatcherPriority.Background);
		}).Start();
	}

	public static void WriteLock(ReaderWriterLockSlim locker, Action action)
	{
		if (action == null)
		{
			return;
		}
		locker.EnterWriteLock();
		try
		{
			action();
		}
		catch (Exception ex)
		{
			UtilLog.Error(ex);
		}
		finally
		{
			locker.ExitWriteLock();
		}
	}

	public static T WriteLock<T>(ReaderWriterLockSlim locker, Func<T> action)
	{
		if (action != null)
		{
			locker.EnterWriteLock();
			try
			{
				return action();
			}
			catch (Exception ex)
			{
				UtilLog.Error(ex);
			}
			finally
			{
				locker.ExitWriteLock();
			}
		}
		return default(T);
	}

	public static void ReadLock(ReaderWriterLockSlim locker, Action action)
	{
		if (action == null)
		{
			return;
		}
		locker.EnterReadLock();
		try
		{
			action();
		}
		catch (Exception ex)
		{
			UtilLog.Error(ex);
		}
		finally
		{
			locker.ExitReadLock();
		}
	}

	public static T ReadLock<T>(ReaderWriterLockSlim locker, Func<T> action)
	{
		if (action != null)
		{
			locker.EnterReadLock();
			try
			{
				return action();
			}
			catch (Exception ex)
			{
				UtilLog.Error(ex);
			}
			finally
			{
				locker.ExitReadLock();
			}
		}
		return default(T);
	}
}
