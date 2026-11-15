using System.Windows.Media;

namespace ticktick_WPF.Views.AI;

internal sealed class AIHomeSuggestionItem
{
	public Geometry Icon { get; }

	public string Title { get; }

	public string Prompt { get; }

	public bool SubmitImmediately { get; }

	public string TrackingLabel { get; }

	public AIHomeSuggestionItem(Geometry icon, string title, string prompt, bool submitImmediately, string trackingLabel)
	{
		Icon = icon;
		Title = title;
		Prompt = prompt;
		SubmitImmediately = submitImmediately;
		TrackingLabel = trackingLabel;
	}
}
