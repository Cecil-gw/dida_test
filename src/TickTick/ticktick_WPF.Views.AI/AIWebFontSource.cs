namespace ticktick_WPF.Views.AI;

internal sealed class AIWebFontSource
{
	public string FileName { get; }

	public string FontWeight { get; }

	public AIWebFontSource(string fileName, string fontWeight)
	{
		FileName = fileName;
		FontWeight = fontWeight;
	}
}
