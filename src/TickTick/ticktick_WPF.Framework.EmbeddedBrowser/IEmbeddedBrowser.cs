using System;
using System.Threading.Tasks;
using System.Windows;

namespace ticktick_WPF.Framework.EmbeddedBrowser;

internal interface IEmbeddedBrowser
{
	FrameworkElement View { get; }

	EmbeddedBrowserEngine Engine { get; }

	string RuntimeVersion { get; }

	Uri Source { get; }

	event EventHandler<EmbeddedBrowserNavigationStartingEventArgs> NavigationStarting;

	event EventHandler<EmbeddedBrowserNavigationCompletedEventArgs> NavigationCompleted;

	event EventHandler<EmbeddedBrowserNewWindowEventArgs> NewWindowRequested;

	event EventHandler<EmbeddedBrowserProcessFailedEventArgs> ProcessFailed;

	Task InitializeAsync(EmbeddedBrowserOptions options);

	void Navigate(Uri uri);

	Task SetCookieAsync(Uri uri, string name, string value, DateTime? expires = null, bool includeSubdomains = false);

	Task DeleteCookieAsync(Uri uri, string name);

	Task DeleteAllCookiesAsync(Uri uri);

	Task RegisterHostObjectAsync(string name, object hostObject);

	Task<string> ExecuteScriptAsync(string script);

	void Stop();

	Task DisposeAsync();
}
