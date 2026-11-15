using System;
using System.Threading;
using System.Threading.Tasks;

namespace ticktick_WPF.Util;

public class TTAsyncTaskLocker<T>
{
	private readonly SemaphoreSlim _semaphore;

	public TTAsyncTaskLocker(int i, int j)
	{
		_semaphore = new SemaphoreSlim(i, j);
	}

	public async Task RunAsync(Func<T, Task> worker, T obj)
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
