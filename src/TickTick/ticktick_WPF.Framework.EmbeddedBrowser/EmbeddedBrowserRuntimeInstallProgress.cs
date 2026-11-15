namespace ticktick_WPF.Framework.EmbeddedBrowser;

internal sealed class EmbeddedBrowserRuntimeInstallProgress
{
	public string Message { get; }

	public int? Percentage { get; }

	public bool IsInstalling { get; }

	public EmbeddedBrowserRuntimeInstallProgress(string message, int? percentage, bool isInstalling)
	{
		Message = message;
		Percentage = percentage;
		IsInstalling = isInstalling;
	}
}
