namespace ticktick_WPF.ViewModels;

public struct TitleChunk
{
	public string Text;

	public bool IsNum;

	public int RealLength;

	public double Num;

	public TitleChunk(string text, bool isNum, int realLength)
	{
		Text = text;
		IsNum = isNum;
		RealLength = realLength;
		Num = 0.0;
		if (isNum && double.TryParse(text, out var result))
		{
			Num = result;
		}
	}
}
