namespace ticktick_WPF.Views;

internal class RemindContentItem
{
	public string Text { get; set; }

	public double FontSize { get; set; } = 12.0;

	public double LineHeight { get; set; }

	public double Opacity { get; set; } = 0.5;

	public bool Wrap { get; set; } = true;

	public RemindContentItem(string text, double fontsize = 12.0, double opactiy = 0.5, double lineHeight = double.NaN)
	{
		Text = text;
		FontSize = fontsize;
		Opacity = opactiy;
		LineHeight = lineHeight;
	}
}
