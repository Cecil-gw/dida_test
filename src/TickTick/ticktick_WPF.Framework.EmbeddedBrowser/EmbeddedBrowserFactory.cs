using System;
using Microsoft.Web.WebView2.Core;

namespace ticktick_WPF.Framework.EmbeddedBrowser;

internal static class EmbeddedBrowserFactory
{
	public const string RuntimeDownloadUrl = "https://go.microsoft.com/fwlink/p/?LinkId=2124703";

	private const int MinimumCompositionControlBuild = 17763;

	public static IEmbeddedBrowser Create(EmbeddedBrowserOptions options)
	{
		if (options == null)
		{
			throw new ArgumentNullException("options");
		}
		Version version = Environment.OSVersion.Version;
		IEmbeddedBrowser embeddedBrowser2;
		if (DetermineEngine(version, webView2Enabled: true, GetDebugOverride()) != EmbeddedBrowserEngine.WebView2)
		{
			IEmbeddedBrowser embeddedBrowser = new LegacyIeEmbeddedBrowser();
			embeddedBrowser2 = embeddedBrowser;
		}
		else
		{
			IEmbeddedBrowser embeddedBrowser = new WebView2EmbeddedBrowser(ShouldUseCompositionControl(options, version), options.UseTransparentBackground);
			embeddedBrowser2 = embeddedBrowser;
		}
		IEmbeddedBrowser embeddedBrowser3 = embeddedBrowser2;
		EmbeddedBrowserTelemetry.Record(options.UseCase, embeddedBrowser3.Engine, null, "selected", null, null);
		return embeddedBrowser3;
	}

	internal static bool ShouldUseCompositionControl(EmbeddedBrowserOptions options, Version osVersion)
	{
		if (options == null || options.DisableCompositionControl || !SupportsCompositionControl(osVersion))
		{
			return false;
		}
		if (!options.UseCompositionControl)
		{
			return string.Equals(options.UseCase, "ai_webapp", StringComparison.Ordinal);
		}
		return true;
	}

	private static bool SupportsCompositionControl(Version osVersion)
	{
		if (osVersion != null)
		{
			if (osVersion.Major <= 10)
			{
				if (osVersion.Major == 10)
				{
					return osVersion.Build >= 17763;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	internal static EmbeddedBrowserEngine DetermineEngine(Version osVersion, bool webView2Enabled, string forcedEngine)
	{
		if (osVersion == null || osVersion.Major < 10)
		{
			return EmbeddedBrowserEngine.LegacyIe;
		}
		if (string.Equals(forcedEngine, "legacy", StringComparison.OrdinalIgnoreCase) || string.Equals(forcedEngine, "ie", StringComparison.OrdinalIgnoreCase))
		{
			return EmbeddedBrowserEngine.LegacyIe;
		}
		if (string.Equals(forcedEngine, "webview2", StringComparison.OrdinalIgnoreCase))
		{
			return EmbeddedBrowserEngine.WebView2;
		}
		if (!webView2Enabled)
		{
			return EmbeddedBrowserEngine.LegacyIe;
		}
		return EmbeddedBrowserEngine.WebView2;
	}

	internal static bool CanOpenEmbeddedBrowser(Version osVersion, bool legacyIe11Available)
	{
		return CanOpenEmbeddedBrowser(osVersion, legacyIe11Available, webView2Enabled: true, GetDebugOverride());
	}

	internal static bool CanOpenEmbeddedBrowser(Version osVersion, bool legacyIe11Available, bool webView2Enabled, string forcedEngine)
	{
		if (osVersion == null)
		{
			return false;
		}
		return DetermineEngine(osVersion, webView2Enabled, forcedEngine) == EmbeddedBrowserEngine.WebView2 || legacyIe11Available;
	}

	public static bool IsWebView2RuntimeAvailable()
	{
		EmbeddedBrowserDiagnostics.RecordEnvironmentOnce();
		try
		{
			string availableBrowserVersionString = CoreWebView2Environment.GetAvailableBrowserVersionString();
			EmbeddedBrowserDiagnostics.RecordRuntimeDetection(availableBrowserVersionString, null);
			return !string.IsNullOrWhiteSpace(availableBrowserVersionString);
		}
		catch (Exception exception)
		{
			EmbeddedBrowserDiagnostics.RecordRuntimeDetection(null, exception);
			return false;
		}
	}

	private static string GetDebugOverride()
	{
		return null;
	}
}
