namespace ticktick_WPF.Views.AI;

internal static class AIWebViewHostPolicy
{
	public const int MaximumRetainedWebViews = 1;

	public static bool CanRetainWebView(AIWebViewHost host)
	{
		if (host != AIWebViewHost.MainWindow)
		{
			return host == AIWebViewHost.FloatingWindow;
		}
		return true;
	}
}
