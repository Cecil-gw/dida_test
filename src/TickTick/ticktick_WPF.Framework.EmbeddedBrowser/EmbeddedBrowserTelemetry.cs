using System;
using Newtonsoft.Json.Linq;
using TickTickUtils.Lang;
using ticktick_WPF.Util;

namespace ticktick_WPF.Framework.EmbeddedBrowser;

internal static class EmbeddedBrowserTelemetry
{
	public static void Record(string useCase, EmbeddedBrowserEngine engine, string runtimeVersion, string result, long? initMilliseconds, long? loadMilliseconds, string error = null)
	{
		try
		{
			JObject jObject = new JObject
			{
				["engine"] = ((engine == EmbeddedBrowserEngine.WebView2) ? "webview2" : "legacy_ie"),
				["runtime"] = runtimeVersion ?? string.Empty,
				["os"] = Environment.OSVersion.VersionString,
				["arch"] = (Environment.Is64BitProcess ? "x64" : "x86"),
				["use_case"] = useCase ?? "unknown",
				["result"] = result ?? string.Empty
			};
			if (initMilliseconds.HasValue)
			{
				jObject["init_ms"] = initMilliseconds.Value;
			}
			if (loadMilliseconds.HasValue)
			{
				jObject["load_ms"] = loadMilliseconds.Value;
			}
			if (!string.IsNullOrEmpty(error))
			{
				jObject["error"] = error;
			}
			UserActCollectUtils.AddClickEventWithData("embedded_browser", useCase ?? "unknown", jObject);
			UtilLog.Info("EmbeddedBrowser " + useCase + " " + jObject);
		}
		catch
		{
		}
	}
}
