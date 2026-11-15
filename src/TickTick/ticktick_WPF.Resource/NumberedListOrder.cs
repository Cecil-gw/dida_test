using ticktick_WPF.Util;

namespace ticktick_WPF.Resource;

public class NumberedListOrder
{
	public int Level { get; set; }

	public int Number { get; set; }

	public int LineNumber { get; set; }

	public NumberedListOrder(int level, int number, int lineNumber)
	{
		Level = level;
		Number = number;
		LineNumber = lineNumber;
	}

	public string GetDisplayText()
	{
		return Utils.GetDisplayText(Level, Number);
	}
}
