using System;

namespace ticktick_WPF.Framework.EmbeddedBrowser;

internal sealed class EmbeddedBrowserNavigationStartingEventArgs : EventArgs
{
	public Uri Uri { get; }

	public bool Cancel { get; set; }

	public EmbeddedBrowserNavigationStartingEventArgs(Uri uri)
	{
		Uri = uri;
	}
}
