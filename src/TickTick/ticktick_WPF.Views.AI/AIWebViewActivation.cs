namespace ticktick_WPF.Views.AI;

internal sealed class AIWebViewActivation
{
	public AIWebAppView WebView { get; }

	public AIWebAppView PreviousWebView { get; }

	public bool RequiresPageSynchronization { get; }

	public AIWebViewActivation(AIWebAppView webView, AIWebAppView previousWebView, bool requiresPageSynchronization)
	{
		WebView = webView;
		PreviousWebView = previousWebView;
		RequiresPageSynchronization = requiresPageSynchronization;
	}
}
