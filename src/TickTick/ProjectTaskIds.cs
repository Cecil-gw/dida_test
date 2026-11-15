using System.Collections.Generic;
using ticktick_WPF.Models;
using ticktick_WPF.Util.Provider;

public class ProjectTaskIds
{
	public BlockingSet<string> UnCompletedTaskIds { get; set; } = new BlockingSet<string>();

	public BlockingSet<string> UnCompletedNoteIds { get; set; } = new BlockingSet<string>();

	public BlockingSet<string> UnCompletedItemIds { get; set; } = new BlockingSet<string>();

	public ProjectIdentity Project { get; set; }

	public int EventCounts { get; set; }

	public HashSet<string> GetIds()
	{
		HashSet<string> hashSet = new HashSet<string>();
		foreach (string item in UnCompletedTaskIds.Value)
		{
			hashSet.Add(item);
		}
		foreach (string item2 in UnCompletedNoteIds.Value)
		{
			hashSet.Add(item2);
		}
		foreach (string item3 in UnCompletedItemIds.Value)
		{
			hashSet.Add(item3);
		}
		return hashSet;
	}
}
