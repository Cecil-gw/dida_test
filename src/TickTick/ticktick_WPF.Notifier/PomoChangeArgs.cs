using System.Collections.Generic;

namespace ticktick_WPF.Notifier;

public class PomoChangeArgs
{
	public string Id;

	public List<string> Ids;

	public PomoChangeType ChangeType;
}
