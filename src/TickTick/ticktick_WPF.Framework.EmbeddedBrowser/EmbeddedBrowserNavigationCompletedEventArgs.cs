using System;

namespace ticktick_WPF.Framework.EmbeddedBrowser;

internal sealed class EmbeddedBrowserNavigationCompletedEventArgs : EventArgs
{
	public Uri Uri { get; }

	public bool IsSuccess { get; }

	public string Error { get; }

	public EmbeddedBrowserNavigationCompletedEventArgs(Uri uri, bool isSuccess, string error)
	{
		Uri = uri;
		IsSuccess = isSuccess;
		Error = error;
	}
}
