using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace TickTickUtils.Lang;

public class UtilRun
{
	public static R InvokeOnUI<R>(Func<R> func)
	{
		return InvokeOnUI(null, func);
	}

	public static R InvokeOnUI<R>(string msg, Func<R> func)
	{
		if (func == null)
		{
			throw new NullReferenceException("func=" + func);
		}
		Dispatcher dispatcher = GlobalSettings.AppInstance?.Dispatcher;
		if (dispatcher == null || dispatcher.Thread == Thread.CurrentThread)
		{
			return func();
		}
		Stopwatch stopwatch = Stopwatch.StartNew();
		R result = default(R);
		Exception ex = null;
		SemaphoreSlim semaphore = new SemaphoreSlim(0, 1);
		try
		{
			dispatcher.Invoke(delegate
			{
				try
				{
					result = func();
				}
				catch (Exception ex2)
				{
					ex = ex2;
				}
				finally
				{
					semaphore.Release();
				}
			});
			semaphore.Wait();
		}
		finally
		{
			if (semaphore != null)
			{
				((IDisposable)semaphore).Dispose();
			}
		}
		stopwatch.Stop();
		if (ex != null || stopwatch.ElapsedMilliseconds > 300)
		{
			UtilLog.Warn("InvokeOnUI " + stopwatch.ElapsedMilliseconds + "ms, " + ((msg == null) ? func.ToString() : msg) + ((ex == null) ? "" : (", ex: " + ex)));
		}
		if (ex != null)
		{
			throw ex;
		}
		return result;
	}

	public static void Dispose(IDisposable disposable)
	{
		try
		{
			disposable?.Dispose();
		}
		catch (Exception ex)
		{
			UtilLog.Warn("UtilRun.Dispose disposable=" + disposable?.ToString() + ", ex: " + ex);
		}
	}

	public static void LogTask(Task task)
	{
		if (task != null && task.IsFaulted)
		{
			UtilLog.Warn("UtilRun.LogTask task: " + task?.ToString() + ", ex: " + task.Exception);
		}
	}

	public static void Quiet(Task task)
	{
		if (task != null && task.Exception != null)
		{
			UtilLog.Debug("quiet wait task done: " + task?.ToString() + ", ex: " + task.Exception);
		}
	}

	public static void Watch(string info, Action action, bool ignoreException = false)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		try
		{
			action();
			stopwatch.Stop();
			UtilLog.Info(info + ", elapsed: " + stopwatch.Elapsed.ToString());
		}
		catch (Exception ex)
		{
			stopwatch.Stop();
			UtilLog.Warn(info + ", elapsed: " + stopwatch.Elapsed.ToString() + ", ex: " + ex);
			if (!ignoreException)
			{
				throw;
			}
		}
	}

	public static Func<T, Task<T>> ToFuncTask<T>(Func<Task> func)
	{
		return async delegate(T t)
		{
			await func();
			return t;
		};
	}

	public static Func<T, Task<R>> ToFuncTask<T, R>(Func<Task<R>> func)
	{
		return (T t) => func();
	}

	public static Func<T, T> ToFunc<T>(Action action)
	{
		return delegate(T t)
		{
			action();
			return t;
		};
	}

	public static Func<T, T> ToFunc<T>(Action<T> action)
	{
		return delegate(T t)
		{
			action(t);
			return t;
		};
	}

	public static Task WhenAllAsync(params Task[] tasks)
	{
		return Task.WhenAll(tasks);
	}

	public static Task OnProgressAsync(Task task, Action<ProgressEventArgs> action)
	{
		return OnProgressAsync(task, 100, 50, 200, action);
	}

	public static async Task OnProgressAsync(Task task, int max, int delay, int period, Action<ProgressEventArgs> action)
	{
		Stopwatch watch = Stopwatch.StartNew();
		ProgressEventArgs args = new ProgressEventArgs(max, delay, period);
		CancellationTokenSource source = new CancellationTokenSource();
		try
		{
			CancellationToken token = source.Token;
			Task waiter = task.ContinueWith(delegate
			{
				source.Cancel();
			});
			await Task.Delay(delay, token).ContinueWith(Quiet);
			for (int i = 0; i <= max; i++)
			{
				try
				{
					args.Elapsed = watch.ElapsedMilliseconds;
					args.Progress = i;
					bool flag = IsDone(task);
					if (args.IsCompleted() && !flag)
					{
						break;
					}
					if (flag)
					{
						args.Progress = 100;
					}
					action(args);
					goto IL_0280;
				}
				catch (Exception ex)
				{
					UtilLog.Error("OnProgress " + args?.ToString() + ", " + task?.ToString() + ", " + action?.ToString() + ", ex: " + ex);
				}
				break;
				IL_0280:
				await Task.Delay(period, token).ContinueWith(Quiet);
			}
			await waiter;
		}
		finally
		{
			if (source != null)
			{
				((IDisposable)source).Dispose();
			}
		}
	}

	public static bool IsDone(Task task)
	{
		if (task != null && !task.IsCompleted && !task.IsFaulted)
		{
			return task.IsCanceled;
		}
		return true;
	}
}
