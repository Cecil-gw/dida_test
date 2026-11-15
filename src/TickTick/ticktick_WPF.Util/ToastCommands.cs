namespace ticktick_WPF.Util;

public struct ToastCommands
{
	public string Argument;

	public string Content;

	public ToastCommands(string arg, string content)
	{
		Argument = arg;
		Content = content;
	}
}
