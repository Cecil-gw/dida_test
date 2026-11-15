using System.Collections.Generic;
using ticktick_WPF.Models;

namespace ticktick_WPF.Views;

public static class UndoHelper
{
	public static string NeedToastFilteredTaskId;

	public static List<string> DeletingIds { get; } = new List<string>();

	public static BlockingSet<string> CanUndoIds { get; } = new BlockingSet<string>();

	public static BlockingSet<string> UndoingIds { get; } = new BlockingSet<string>();
}
