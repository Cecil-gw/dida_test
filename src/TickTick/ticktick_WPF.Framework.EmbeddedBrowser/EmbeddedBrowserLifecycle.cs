using System;
using System.Threading;
using System.Threading.Tasks;

namespace ticktick_WPF.Framework.EmbeddedBrowser;

internal static class EmbeddedBrowserLifecycle
{
	private sealed class NoopLease : IDisposable
	{
		public static readonly NoopLease Instance = new NoopLease();

		public void Dispose()
		{
		}
	}

	private sealed class SemaphoreLease : IDisposable
	{
		private SemaphoreSlim _semaphore;

		public SemaphoreLease(SemaphoreSlim semaphore)
		{
			_semaphore = semaphore;
		}

		public void Dispose()
		{
			Interlocked.Exchange(ref _semaphore, null)?.Release();
		}
	}

	private static readonly SemaphoreSlim WebView2Gate = new SemaphoreSlim(1, 1);

	public static async Task<IDisposable> EnterAsync(EmbeddedBrowserEngine engine)
	{
		if (engine != EmbeddedBrowserEngine.WebView2)
		{
			return NoopLease.Instance;
		}
		await WebView2Gate.WaitAsync();
		return new SemaphoreLease(WebView2Gate);
	}
}
