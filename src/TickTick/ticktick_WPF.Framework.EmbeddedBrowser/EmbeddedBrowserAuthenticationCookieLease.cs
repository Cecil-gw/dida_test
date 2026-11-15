using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ticktick_WPF.Framework.EmbeddedBrowser;

internal sealed class EmbeddedBrowserAuthenticationCookieLease
{
	private sealed class LeaseState
	{
		public int Count { get; set; }

		public Dictionary<string, Uri> CleanupUris { get; } = new Dictionary<string, Uri>(StringComparer.OrdinalIgnoreCase);
	}

	private const string AuthenticationCookieName = "t";

	private static readonly SemaphoreSlim LeaseGate = new SemaphoreSlim(1, 1);

	private static readonly Dictionary<string, LeaseState> LeaseStates = new Dictionary<string, LeaseState>(StringComparer.OrdinalIgnoreCase);

	private readonly IEmbeddedBrowser _browser;

	private readonly string _leaseKey;

	private bool _released;

	private EmbeddedBrowserAuthenticationCookieLease(IEmbeddedBrowser browser, string leaseKey)
	{
		_browser = browser;
		_leaseKey = leaseKey;
	}

	internal static async Task<EmbeddedBrowserAuthenticationCookieLease> AcquireAsync(IEmbeddedBrowser browser, Uri cookieScopeUri, IEnumerable<Uri> cleanupUris, string token, DateTime expires, Func<bool> canUseBrowser = null)
	{
		if (browser == null)
		{
			throw new ArgumentNullException("browser");
		}
		if (cookieScopeUri == null || !cookieScopeUri.IsAbsoluteUri)
		{
			throw new ArgumentException("Cookie scope must be an absolute URI.", "cookieScopeUri");
		}
		string leaseKey = GetLeaseKey(browser.Engine, cookieScopeUri);
		IReadOnlyCollection<Uri> requestedCleanupUris = NormalizeCleanupUris(cleanupUris, cookieScopeUri);
		await LeaseGate.WaitAsync();
		try
		{
			if (canUseBrowser != null && !canUseBrowser())
			{
				return null;
			}
			LeaseStates.TryGetValue(leaseKey, out var existingState);
			bool isFirstLease = existingState == null;
			if (isFirstLease)
			{
				await DeleteCookiesAsync(browser, requestedCleanupUris);
			}
			if (canUseBrowser != null && !canUseBrowser())
			{
				return null;
			}
			await browser.SetCookieAsync(cookieScopeUri, "t", token, expires, includeSubdomains: true);
			if (canUseBrowser != null && !canUseBrowser())
			{
				if (isFirstLease)
				{
					await DeleteCookiesAsync(browser, requestedCleanupUris);
				}
				return null;
			}
			LeaseState leaseState = existingState ?? new LeaseState();
			leaseState.Count++;
			foreach (Uri item in requestedCleanupUris)
			{
				leaseState.CleanupUris[item.AbsoluteUri] = item;
			}
			LeaseStates[leaseKey] = leaseState;
			return new EmbeddedBrowserAuthenticationCookieLease(browser, leaseKey);
		}
		finally
		{
			LeaseGate.Release();
		}
	}

	internal async Task ReleaseAsync()
	{
		await LeaseGate.WaitAsync();
		try
		{
			if (_released)
			{
				return;
			}
			_released = true;
			if (LeaseStates.TryGetValue(_leaseKey, out var value))
			{
				value.Count = Math.Max(0, value.Count - 1);
				if (value.Count <= 0)
				{
					LeaseStates.Remove(_leaseKey);
					await DeleteCookiesAsync(_browser, value.CleanupUris.Values);
				}
			}
		}
		finally
		{
			LeaseGate.Release();
		}
	}

	private static string GetLeaseKey(EmbeddedBrowserEngine engine, Uri cookieScopeUri)
	{
		return engine.ToString() + "|" + cookieScopeUri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
	}

	private static IReadOnlyCollection<Uri> NormalizeCleanupUris(IEnumerable<Uri> cleanupUris, Uri cookieScopeUri)
	{
		Dictionary<string, Uri> dictionary = new Dictionary<string, Uri>(StringComparer.OrdinalIgnoreCase);
		if (cleanupUris != null)
		{
			foreach (Uri item in cleanupUris.Where((Uri uri) => uri != null && uri.IsAbsoluteUri))
			{
				dictionary[item.AbsoluteUri] = item;
			}
		}
		dictionary[cookieScopeUri.AbsoluteUri] = cookieScopeUri;
		return (IReadOnlyCollection<Uri>)(object)dictionary.Values.ToArray();
	}

	private static async Task DeleteCookiesAsync(IEmbeddedBrowser browser, IEnumerable<Uri> cleanupUris)
	{
		Exception firstException = null;
		foreach (Uri cleanupUri in cleanupUris)
		{
			try
			{
				await browser.DeleteCookieAsync(cleanupUri, "t");
			}
			catch (Exception ex)
			{
				firstException = firstException ?? ex;
			}
		}
		if (firstException != null)
		{
			throw firstException;
		}
	}
}
