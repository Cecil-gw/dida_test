using System;

namespace ticktick_WPF.Util;

public class UriUtils
{
	public static string AddAttribute(string url, string key, string val)
	{
		if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(key))
		{
			return url;
		}
		int num = url.IndexOf("?", StringComparison.Ordinal);
		int num2 = url.IndexOf("#", StringComparison.Ordinal);
		string text = ((num >= 0 || num2 >= 0) ? url.Substring(0, (num >= 0) ? num : num2) : url);
		string text2 = ((num >= 0) ? url.Substring(num, ((num2 >= 0) ? num2 : url.Length) - num) : string.Empty);
		string text3 = ((num2 >= 0) ? url.Substring(num2, url.Length - num2) : string.Empty);
		string text4 = (string.IsNullOrEmpty(text2) ? "?" : (text2.EndsWith("?") ? "" : "&"));
		text2 = text2 + text4 + Utils.UrlEncode0(key) + "=" + Utils.UrlEncode0(val);
		return text + text2 + text3;
	}

	public static string AddLangAttribute(string url)
	{
		return AddAttribute(url, "lang", Utils.GetLanguage());
	}
}
