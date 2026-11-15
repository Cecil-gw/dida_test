using ticktick_WPF.Models;

namespace ticktick_WPF.Notifier;

public static class ItemChangeNotifier
{
	public static void NotifyDeletedUndo(TaskDetailItemModel item)
	{
		TaskChangeNotifier.OnCheckItemChanged(item.id);
	}

	public static void NotifyItemDeleted(TaskDetailItemModel item, object sender = null)
	{
		TaskChangeNotifier.OnCheckItemChanged(item.id);
	}

	public static void NotifyItemDateChanged(TaskDetailItemModel item)
	{
		TaskChangeNotifier.OnCheckItemChanged(item.id, needSync: true);
	}

	public static void NotifyItemStatusChanged(string id)
	{
		TaskChangeNotifier.OnCheckItemChanged(id, needSync: true);
	}
}
