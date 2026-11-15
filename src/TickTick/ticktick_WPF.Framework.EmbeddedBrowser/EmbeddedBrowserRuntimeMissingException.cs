using System;

namespace ticktick_WPF.Framework.EmbeddedBrowser;

internal sealed class EmbeddedBrowserRuntimeMissingException : InvalidOperationException
{
	public EmbeddedBrowserRuntimeMissingException(Exception innerException)
		: base("Microsoft Edge WebView2 Runtime is not installed.", innerException)
	{
	}
}
