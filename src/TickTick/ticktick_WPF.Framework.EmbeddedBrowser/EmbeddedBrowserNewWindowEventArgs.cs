using System;

namespace ticktick_WPF.Framework.EmbeddedBrowser;

internal sealed class EmbeddedBrowserNewWindowEventArgs : EventArgs
{
	public Uri Uri { get; }

	public bool Handled { get; set; }

	public EmbeddedBrowserNewWindowEventArgs(Uri uri)
	{
		Uri = uri;
	}
}
