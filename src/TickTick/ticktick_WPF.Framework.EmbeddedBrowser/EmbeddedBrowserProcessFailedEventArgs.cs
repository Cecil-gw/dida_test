using System;

namespace ticktick_WPF.Framework.EmbeddedBrowser;

internal sealed class EmbeddedBrowserProcessFailedEventArgs : EventArgs
{
	public string Reason { get; }

	public EmbeddedBrowserProcessFailedEventArgs(string reason)
	{
		Reason = reason;
	}
}
