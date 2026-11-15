using System;
using System.Threading;
using System.Threading.Tasks;

namespace ticktick_WPF.Util;

public class TTAsyncLocker<T1, T2, T3, T4, TResult>
{
	private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

	public async Task<TResult> RunAsync(Func<T1, T2, T3, T4, Task<TResult>> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
	{
		await _semaphore.WaitAsync();
		try
		{
			return await worker(arg1, arg2, arg3, arg4);
		}
		finally
		{
			_semaphore.Release();
		}
	}
}
public class TTAsyncLocker<T1, T2, T3, TResult>
{
	private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

	public async Task<TResult> RunAsync(Func<T1, T2, T3, Task<TResult>> worker, T1 arg1, T2 arg2, T3 arg3)
	{
		await _semaphore.WaitAsync();
		try
		{
			return await worker(arg1, arg2, arg3);
		}
		finally
		{
			_semaphore.Release();
		}
	}
}
public class TTAsyncLocker<T1, T2, TResult>
{
	private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

	public async Task<TResult> RunAsync(Func<T1, T2, Task<TResult>> worker, T1 arg1, T2 arg2)
	{
		await _semaphore.WaitAsync();
		try
		{
			return await worker(arg1, arg2);
		}
		finally
		{
			_semaphore.Release();
		}
	}
}
public class TTAsyncLocker<T1, TResult>
{
	private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

	public async Task<TResult> RunAsync(Func<T1, Task<TResult>> worker, T1 arg1)
	{
		await _semaphore.WaitAsync();
		try
		{
			return await worker(arg1);
		}
		finally
		{
			_semaphore.Release();
		}
	}
}
public class TTAsyncLocker<TResult>
{
	private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

	public async Task<TResult> RunAsync(Func<Task<TResult>> worker)
	{
		await _semaphore.WaitAsync();
		try
		{
			return await worker();
		}
		finally
		{
			_semaphore.Release();
		}
	}
}
public class TTAsyncLocker
{
	private readonly SemaphoreSlim _semaphore;

	public TTAsyncLocker(int i, int j)
	{
		_semaphore = new SemaphoreSlim(i, j);
	}

	public async Task RunAsync(Action worker)
	{
		await _semaphore.WaitAsync();
		try
		{
			worker();
		}
		finally
		{
			_semaphore.Release();
		}
	}

	public async Task RunAsync(Func<Task> worker)
	{
		await _semaphore.WaitAsync();
		try
		{
			await worker();
		}
		finally
		{
			_semaphore.Release();
		}
	}

	public async Task RunAsync(Func<object, Task> worker, object obj)
	{
		await _semaphore.WaitAsync();
		try
		{
			await worker(obj);
		}
		finally
		{
			_semaphore.Release();
		}
	}
}
