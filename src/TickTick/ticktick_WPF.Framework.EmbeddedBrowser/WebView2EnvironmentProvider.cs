using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

namespace ticktick_WPF.Framework.EmbeddedBrowser;

internal static class WebView2EnvironmentProvider
{
	private static readonly object SyncRoot = new object();

	private static Task<CoreWebView2Environment> _environmentTask;

	public static async Task<CoreWebView2Environment> GetAsync()
	{
		Task<CoreWebView2Environment> task;
		lock (SyncRoot)
		{
			task = _environmentTask ?? (_environmentTask = CreateAsync());
		}
		try
		{
			return await task;
		}
		catch
		{
			lock (SyncRoot)
			{
				if (_environmentTask == task)
				{
					_environmentTask = null;
				}
			}
			throw;
		}
	}

	private static Task<CoreWebView2Environment> CreateAsync()
	{
		string text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Tick_Tick", "WebView2");
		Directory.CreateDirectory(text);
		return CoreWebView2Environment.CreateAsync(null, text);
	}
}
