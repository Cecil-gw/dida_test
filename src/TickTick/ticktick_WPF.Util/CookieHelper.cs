using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace ticktick_WPF.Util;

public class CookieHelper
{
	private const int InternetCookieHttponly = 8192;

	private const int INTERNET_OPTION_END_BROWSER_SESSION = 42;

	[DllImport("wininet.dll", SetLastError = true)]
	private static extern bool InternetGetCookieEx(string url, string cookieName, StringBuilder cookieData, ref int size, int dwFlags, IntPtr lpReserved);

	public static CookieContainer GetUriCookieContainer(Uri uri)
	{
		CookieContainer cookieContainer = null;
		int size = 131072;
		StringBuilder stringBuilder = new StringBuilder(size);
		if (!InternetGetCookieEx(uri.ToString(), null, stringBuilder, ref size, 8192, IntPtr.Zero))
		{
			if (size < 0)
			{
				return null;
			}
			stringBuilder = new StringBuilder(size);
			if (!InternetGetCookieEx(uri.ToString(), null, stringBuilder, ref size, 8192, IntPtr.Zero))
			{
				return null;
			}
		}
		if (stringBuilder.Length > 0)
		{
			cookieContainer = new CookieContainer();
			cookieContainer.SetCookies(uri, stringBuilder.ToString().Replace(';', ','));
		}
		return cookieContainer;
	}

	public static IReadOnlyCollection<string> GetCookieNames(Uri uri)
	{
		if (uri == null)
		{
			throw new ArgumentNullException("uri");
		}
		int size = 131072;
		StringBuilder stringBuilder = new StringBuilder(size);
		if (!InternetGetCookieEx(uri.AbsoluteUri, null, stringBuilder, ref size, 8192, IntPtr.Zero))
		{
			if (size <= 0)
			{
				return (IReadOnlyCollection<string>)(object)Array.Empty<string>();
			}
			stringBuilder = new StringBuilder(size);
			if (!InternetGetCookieEx(uri.AbsoluteUri, null, stringBuilder, ref size, 8192, IntPtr.Zero))
			{
				return (IReadOnlyCollection<string>)(object)Array.Empty<string>();
			}
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		string[] array = stringBuilder.ToString().Split(';');
		foreach (string text in array)
		{
			int num = text.IndexOf('=');
			if (num > 0)
			{
				hashSet.Add(text.Substring(0, num).Trim());
			}
		}
		return hashSet;
	}

	[DllImport("wininet.dll", SetLastError = true)]
	private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int lpdwBufferLength);

	public static void ClearCookie()
	{
		InternetSetOption(IntPtr.Zero, 42, IntPtr.Zero, 0);
	}

	[DllImport("wininet.dll", CharSet = CharSet.Unicode, EntryPoint = "InternetSetCookieExW", ExactSpelling = true, SetLastError = true)]
	private static extern bool InternetSetCookieEx([In] string lpszUrl, [In] string lpszCookieName, [In] string lpszCookieData, uint dwFlags, [In] IntPtr dwReserved);

	public static void SetCookie(string uri, string cookie)
	{
		SetCookie(uri, cookie, DateTime.UtcNow.AddDays(20.0));
	}

	public static void SetCookie(string uri, string cookie, DateTime expires)
	{
		InternetSetCookieEx(uri, null, cookie + "; expires = " + expires.ToUniversalTime().ToString("R", CultureInfo.InvariantCulture), 0u, IntPtr.Zero);
	}

	public static void SetCookieOutDate(string uri, string cookie)
	{
		SetCookieOutDate(uri, cookie, includeHttpOnly: false);
	}

	public static void SetCookieOutDate(string uri, string cookie, bool includeHttpOnly)
	{
		InternetSetCookieEx(uri, null, cookie + "; expires = " + DateTime.UtcNow.AddDays(-1.0).ToString("R", CultureInfo.InvariantCulture), includeHttpOnly ? 8192u : 0u, IntPtr.Zero);
	}
}
