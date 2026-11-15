using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util.Sync.Model;

namespace ticktick_WPF.Util.Sync;

public class TaskSortOrderTransfer
{
	public static TaskSortOrder ConvertLocalToRemote(TaskSortOrderInDateModel localOrder)
	{
		return new TaskSortOrder
		{
			id = localOrder.taskid,
			order = localOrder.sortOrder,
			type = localOrder.type
		};
	}

	public static TaskSortOrder ConvertLocalToRemote(TaskSortOrderInProjectModel localOrder)
	{
		return new TaskSortOrder
		{
			id = localOrder.EntityId,
			order = localOrder.SortOrder,
			type = EntityType.GetEntityTypeNum(localOrder.EntityType)
		};
	}

	public static TaskSortOrder ConvertLocalToRemote(TaskSortOrderInPriorityModel localOrder)
	{
		return new TaskSortOrder
		{
			id = localOrder.EntityId,
			order = localOrder.SortOrder,
			type = EntityType.GetEntityTypeNum(localOrder.EntityType)
		};
	}
}
