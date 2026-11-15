using System;

namespace ticktick_WPF.Views.Misc;

public struct OptionCheckArgs
{
	public string Group;

	public string UId;

	public bool Selected;

	public OptionCheckArgs(string g, string i, bool s)
	{
		Group = g;
		UId = i;
		Selected = s;
	}

	public OptionCheckArgs(string g, Guid i, bool s)
	{
		Group = g;
		UId = i.ToString();
		Selected = s;
	}
}
