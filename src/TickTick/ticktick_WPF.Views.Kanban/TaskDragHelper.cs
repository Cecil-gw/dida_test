using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Service.SortOrder;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.MainListView;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.Views.Kanban;

public static class TaskDragHelper
{
	public static async Task ChangeOrderInType(string sortOrderType, DisplayItemModel front, DisplayItemModel next, List<DisplayItemModel> dragModels, List<DisplayItemModel> siblings, ProjectIdentity projectIdentity)
	{
		if (dragModels == null || dragModels.Count == 0)
		{
			return;
		}
		string catId = projectIdentity.CatId;
		long frontOrder = ((front != null && (front.IsSection || front.Level != dragModels[0].Level)) ? long.MaxValue : (front?.SpecialOrder ?? long.MaxValue));
		long nextOrder = ((next != null && (next.IsSection || next.Level != dragModels[0].Level)) ? long.MaxValue : (next?.SpecialOrder ?? long.MaxValue));
		if (frontOrder != long.MaxValue && Math.Abs(frontOrder - nextOrder) < 16)
		{
			List<SyncSortOrderModel> source = await TaskSortOrderService.BatchResetSortOrder(sortOrderType, catId, siblings.Select((DisplayItemModel displayItemModel) => (Id: displayItemModel.Id, EntityType.GetEntityTypeNum(displayItemModel.Type))).ToList());
			SyncSortOrderModel syncSortOrderModel = source.FirstOrDefault((SyncSortOrderModel o) => o.EntityId == front?.Id);
			SyncSortOrderModel syncSortOrderModel2 = source.FirstOrDefault((SyncSortOrderModel o) => o.EntityId == next?.Id);
			frontOrder = syncSortOrderModel?.SortOrder ?? ((syncSortOrderModel2 != null) ? (syncSortOrderModel2.SortOrder - 268435456) : 0);
			nextOrder = syncSortOrderModel2?.SortOrder ?? (frontOrder + 268435456);
		}
		else
		{
			if (frontOrder == long.MaxValue && nextOrder != long.MaxValue)
			{
				frontOrder = nextOrder - 268435456;
			}
			if (nextOrder == long.MaxValue && frontOrder != long.MaxValue)
			{
				nextOrder = frontOrder + 268435456;
			}
			if (nextOrder == long.MaxValue && frontOrder == long.MaxValue)
			{
				frontOrder = 0L;
				nextOrder = 0L;
			}
		}
		for (int i = 0; i < dragModels.Count; i++)
		{
			long value = Math.Min(frontOrder, nextOrder) + Math.Abs(nextOrder - frontOrder) * (i + 1) / (dragModels.Count + 1);
			await TaskSortOrderService.InsertOrUpdateAsync(sortOrderType, catId, dragModels[i].Id, EntityType.GetEntityTypeNum(dragModels[i].Type), value);
		}
	}

	public static async Task ChangeNewDate(Section section, DisplayItemModel model, string columnId)
	{
		if (section == null || model == null || (section is OutdatedSection && model.OutDate()))
		{
			return;
		}
		DateTime? startDate = section.GetStartDate();
		if (startDate.HasValue)
		{
			if (model.IsTaskOrNote)
			{
				TaskModel taskModel = await TaskDao.GetThinTaskById(model.TaskId);
				if (!string.IsNullOrEmpty(taskModel?.attendId))
				{
					if (!AgendaHelper.CanAccessAgenda(taskModel))
					{
						Utils.Toast(Utils.GetString("AttendeeSetDate"));
					}
					return;
				}
				bool hasStartDate = model.StartDate.HasValue;
				await TaskService.SetDate(model.TaskId, startDate.Value, handleReminder: false, columnId);
				if (!hasStartDate)
				{
					await TaskService.SaveTaskReminders(new TaskModel
					{
						id = model.TaskId,
						reminders = TimeData.GetDefaultAllDayReminders().ToArray()
					});
				}
			}
			else if (model.IsItem)
			{
				await TaskService.SetSubtaskDate(model.TaskId, model.Id, startDate.Value);
			}
		}
		else
		{
			await TaskService.ClearDate(model.TaskId, columnId);
		}
	}

	private static int GetSortOrderType(DisplayItemModel model)
	{
		if (model.IsTaskOrNote)
		{
			return 1;
		}
		if (model.IsItem)
		{
			return 2;
		}
		if (model.IsEvent)
		{
			return 3;
		}
		return 1;
	}

	private static void AddDeltaOrder(string date, string projectId, DisplayItemModel model, long order, ICollection<TaskSortOrderInDateModel> diffOrders)
	{
		TaskSortOrderInDateModel item = new TaskSortOrderInDateModel
		{
			userid = LocalSettings.Settings.LoginUserId,
			modifiedTime = Utils.GetNowTimeStamp(),
			taskid = model.Id,
			projectid = projectId,
			date = date,
			type = GetSortOrderType(model),
			sortOrder = order,
			status = 1
		};
		diffOrders.Add(item);
	}

	private static async Task SaveSectionSortOrders(List<DisplayItemModel> siblings, DisplayItemModel target, DisplayItemModel dragModel, string projectId, string date, IReadOnlyCollection<TaskSortOrderInDateModel> orders, bool below)
	{
		List<TaskSortOrderInDateModel> deltaOrders = new List<TaskSortOrderInDateModel>();
		long num = 0L;
		if (orders != null && orders.Count > 0)
		{
			num = orders.OrderByDescending((TaskSortOrderInDateModel order) => order.sortOrder).First().sortOrder + 268435456;
			foreach (DisplayItemModel model in siblings)
			{
				if ((model.IsTaskOrNote || model.IsItem || model.IsEvent) && orders.All((TaskSortOrderInDateModel order) => order.taskid != model.Id))
				{
					AddDeltaOrder(date, projectId, model, num, deltaOrders);
					num += 268435456;
				}
			}
		}
		else
		{
			foreach (DisplayItemModel item in siblings.Where((DisplayItemModel displayItemModel) => displayItemModel.IsTaskOrNote || displayItemModel.IsItem || displayItemModel.IsEvent))
			{
				AddDeltaOrder(date, projectId, item, num, deltaOrders);
				num += 268435456;
			}
		}
		if (deltaOrders.Count > 0)
		{
			await TaskSortOrderInDateDao.BatchInsertOrderInDate(deltaOrders);
		}
		List<TaskSortOrderInDateModel> list = new List<TaskSortOrderInDateModel>();
		if (orders != null)
		{
			list.AddRange(orders);
		}
		list.AddRange(deltaOrders);
		list = list.OrderBy((TaskSortOrderInDateModel order) => order.sortOrder).ToList();
		int num2 = ((target == null) ? (-1) : GetOrderIndex(target, list));
		long sortOrder2;
		if (num2 >= 0)
		{
			long sortOrder = list[num2].sortOrder;
			sortOrder2 = ((!below) ? ((num2 <= 0) ? (sortOrder - 268435456) : ((sortOrder + list[num2 - 1].sortOrder) / 2)) : ((num2 >= list.Count - 1) ? (sortOrder + 268435456) : ((sortOrder + list[num2 + 1].sortOrder) / 2)));
		}
		else
		{
			long sortOrder = list.Select((TaskSortOrderInDateModel o) => o.sortOrder).ToList().Min();
			sortOrder2 = sortOrder - 268435456;
		}
		await TaskSortOrderInDateDao.UpdateOrInsertTaskSortOrderInDateAsync(new TaskSortOrderInDateModel
		{
			userid = LocalSettings.Settings.LoginUserId,
			modifiedTime = Utils.GetNowTimeStamp(),
			taskid = dragModel.Id,
			projectid = projectId,
			date = date,
			type = GetSortOrderType(dragModel),
			sortOrder = sortOrder2,
			status = 1
		});
	}

	private static int GetOrderIndex(DisplayItemModel target, IReadOnlyList<TaskSortOrderInDateModel> orders)
	{
		for (int i = 0; i < orders.Count; i++)
		{
			if (orders[i].taskid == target.Id)
			{
				return i;
			}
		}
		return -1;
	}

	public static async Task ChangeTasksSortOrder(DisplayItemModel front, DisplayItemModel next, List<DisplayItemModel> models, string dropProjectId, List<DisplayItemModel> siblings, Section dropSection, ProjectIdentity projectIdentity)
	{
		if (models != null && models.Count != 0)
		{
			List<TaskSortOrderInProjectModel> source = await ProjectSortOrderDao.BatchResetSortOrder(dropProjectId, siblings?.Select((DisplayItemModel displayItemModel) => displayItemModel.Id).ToList());
			TaskSortOrderInProjectModel taskSortOrderInProjectModel = source.FirstOrDefault((TaskSortOrderInProjectModel o) => o.EntityId == front?.Id);
			TaskSortOrderInProjectModel taskSortOrderInProjectModel2 = source.FirstOrDefault((TaskSortOrderInProjectModel o) => o.EntityId == next?.Id);
			long frontOrder = taskSortOrderInProjectModel?.SortOrder ?? ((taskSortOrderInProjectModel2 != null) ? (taskSortOrderInProjectModel2.SortOrder - 268435456) : 0);
			long nextOrder = taskSortOrderInProjectModel2?.SortOrder ?? (frontOrder + 268435456);
			for (int i = 0; i < models.Count; i++)
			{
				long sortOrder = Math.Min(frontOrder, nextOrder) + Math.Abs(nextOrder - frontOrder) * (i + 1) / (models.Count + 1);
				await ProjectSortOrderDao.UpdateSortOrderInProject(models[i].Id, sortOrder, dropProjectId, EntityType.GetEntityType(models[i].Type));
			}
		}
	}

	public static async Task ChangeOrderInPriority(Section dropSection, IEnumerable<DisplayItemModel> siblings, ProjectIdentity projectIdentity, List<DisplayItemModel> models, DisplayItemModel front, DisplayItemModel next)
	{
		if (models == null || models.Count == 0)
		{
			return;
		}
		int priority = dropSection?.GetPriority() ?? 0;
		if (models.Count == 1)
		{
			DisplayItemModel displayItemModel = models[0];
			if (displayItemModel.IsEvent && priority != 0)
			{
				Utils.Toast(Utils.GetString("CannotDragEventToPrioritySection"));
				return;
			}
			if (displayItemModel.IsItem && displayItemModel.Priority != priority)
			{
				Utils.Toast(Utils.GetString("CannotDragSubTasksToPrioritySection"));
				return;
			}
			if (!displayItemModel.IsTask && !displayItemModel.IsItem && !displayItemModel.IsEvent)
			{
				return;
			}
		}
		List<TaskSortOrderInPriorityModel> source = await BatchResetSortOrderInPriority(siblings.Select((DisplayItemModel s) => s.Id).ToList(), priority, projectIdentity.CatId);
		TaskSortOrderInPriorityModel taskSortOrderInPriorityModel = source.FirstOrDefault((TaskSortOrderInPriorityModel o) => o.EntityId == front?.Id);
		TaskSortOrderInPriorityModel taskSortOrderInPriorityModel2 = source.FirstOrDefault((TaskSortOrderInPriorityModel o) => o.EntityId == next?.Id);
		long frontOrder = taskSortOrderInPriorityModel?.SortOrder ?? ((taskSortOrderInPriorityModel2 != null) ? (taskSortOrderInPriorityModel2.SortOrder - 268435456) : 0);
		long nextOrder = taskSortOrderInPriorityModel2?.SortOrder ?? (frontOrder + 268435456);
		for (int i = 0; i < models.Count; i++)
		{
			if (models[i].IsTask && models[i].Level == 0)
			{
				await TaskService.SetPriority(models[i].Id, priority, "", notify: false);
			}
			long childOrder = Math.Min(frontOrder, nextOrder) + Math.Abs(nextOrder - frontOrder) * (i + 1) / (models.Count + 1);
			await UpdateSortOrderInPriority(models[i].Id, childOrder, priority, projectIdentity.CatId, EntityType.GetEntityType(models[i].Type));
		}
	}

	public static async Task SetTaskProperty(TaskModel task, IDroppable droppable, ListViewContainer listView)
	{
		if (droppable == null || task == null)
		{
			return;
		}
		IToastShowWindow toastWindow = listView?.GetToastWindow() ?? Utils.GetToastWindow();
		if (!string.IsNullOrEmpty(droppable.ProjectId) && task.projectId != droppable.ProjectId)
		{
			string text = await GetFirstColumnIdByProject(droppable.ProjectId);
			string id = task.id;
			string projectId = droppable.ProjectId;
			string columnId = text;
			await TaskService.MoveProject(id, projectId, null, columnId);
			toastWindow?.ToastMoveProjectControl(droppable.ProjectId, task.title);
		}
		if (droppable.Priority != 0)
		{
			await TaskService.SetPriority(task.id, droppable.Priority);
		}
		if (droppable.DefaultDate.HasValue)
		{
			if (!string.IsNullOrEmpty(task.attendId) && !AgendaHelper.CanAccessAgenda(task))
			{
				Utils.Toast(Utils.GetString("AttendeeSetDate"));
				return;
			}
			bool hasStartDate = task.startDate.HasValue;
			await TaskService.SetDate(task.id, droppable.DefaultDate.Value, handleReminder: false);
			if (!hasStartDate)
			{
				await TaskService.SaveTaskReminders(new TaskModel
				{
					id = task.id,
					reminders = TimeData.GetDefaultAllDayReminders().ToArray()
				});
			}
			if (droppable is SmartProjectViewModel smartProjectViewModel && (smartProjectViewModel.Id == "_special_id_today" || smartProjectViewModel.Id == "_special_id_tomorrow"))
			{
				bool isToday = smartProjectViewModel.Id == "_special_id_today";
				TaskModel task2 = await TaskDao.GetThinTaskById(task.id);
				listView?.TryToastMoveControl(task2, isToday);
			}
		}
		if (droppable.IsCompleted)
		{
			if (task.kind == "NOTE")
			{
				return;
			}
			await TaskService.SetTaskStatus(task.id, 2);
		}
		if (droppable.IsAbandoned)
		{
			if (task.kind == "NOTE")
			{
				return;
			}
			await TaskService.SetTaskStatus(task.id, -1);
		}
		if (droppable.IsDeleted)
		{
			if (!string.IsNullOrEmpty(task.attendId))
			{
				if (await TaskOperationHelper.CheckIfAllowDeleteAgenda(task, App.Window))
				{
					await TaskService.DeleteAgenda(task.id, task.projectId, task.attendId);
				}
			}
			else
			{
				toastWindow?.TaskDeleted(task.id);
				listView?.TryExtractDetail();
			}
		}
		if (droppable.Tags.Any())
		{
			List<string> list = TagSerializer.ToTags(task.tag);
			list.AddRange(droppable.Tags.Except(list));
			await TaskService.SetTags(task.id, list);
		}
	}

	private static async Task<string> GetFirstColumnIdByProject(string projectId)
	{
		if (string.IsNullOrEmpty(projectId))
		{
			return string.Empty;
		}
		List<ColumnModel> list = await ColumnDao.GetColumnsByProjectId(projectId);
		if (list == null || !list.Any())
		{
			await ColumnDao.CheckProjectColumns(projectId);
			list = await ColumnDao.GetColumnsByProjectId(projectId);
		}
		return list?.OrderBy((ColumnModel c) => c.sortOrder).FirstOrDefault()?.id ?? string.Empty;
	}

	public static async Task CheckChildrenLevelAndSortOrder(string taskId)
	{
		TaskModel taskModel = await TaskDao.GetThinTaskById(taskId);
		if (taskModel != null)
		{
			Dictionary<string, Node<TaskBaseViewModel>> taskTreeInProject = TaskCache.GetTaskTreeInProject(taskModel.projectId);
			if (taskTreeInProject.ContainsKey(taskModel.id))
			{
				await CheckLevel(taskTreeInProject[taskModel.id], TaskCache.GetTaskLevel(taskId));
			}
		}
		static async Task CheckLevel(Node<TaskBaseViewModel> node, int level)
		{
			if (level >= 4)
			{
				List<Node<TaskBaseViewModel>> children = node.Children;
				if (children != null && children.Count > 0)
				{
					await TaskDao.BatchUpdateParent((from t in node.GetAllChildrenValue()
						select t.Id).ToList(), node.Value.ParentId);
				}
			}
			else
			{
				List<Node<TaskBaseViewModel>> children2 = node.Children;
				if (children2 != null && children2.Count > 0)
				{
					foreach (Node<TaskBaseViewModel> child in node.Children)
					{
						await CheckLevel(child, level + 1);
					}
				}
			}
		}
	}

	private static async Task<List<TaskSortOrderInPriorityModel>> BatchResetSortOrderInPriority(List<string> ids, int priority, string catId)
	{
		List<TaskSortOrderInPriorityModel> result = new List<TaskSortOrderInPriorityModel>();
		if (ids == null || ids.Count == 0)
		{
			return result;
		}
		List<TaskSortOrderInPriorityModel> models = (await TaskSortOrderInPriorityDao.GetSortOrders(catId, priority, null, containDelete: true)).Where((TaskSortOrderInPriorityModel m) => ids.Contains(m.EntityId)).ToList();
		long sortOrder = 0L;
		foreach (string id in ids)
		{
			TaskSortOrderInPriorityModel model = models.FirstOrDefault((TaskSortOrderInPriorityModel m) => m.EntityId == id);
			if (model != null)
			{
				model.SortOrder = sortOrder;
				model.SyncStatus = 1;
				model.Deleted = 0;
				model.UserId = LocalSettings.Settings.LoginUserId;
				await TaskSortOrderInPriorityDao.UpdateAsync(model);
			}
			else
			{
				model = new TaskSortOrderInPriorityModel
				{
					EntityId = id,
					EntityType = "task",
					Priority = priority,
					CatId = catId,
					SyncStatus = 0,
					SortOrder = sortOrder,
					UserId = LocalSettings.Settings.LoginUserId
				};
				await TaskSortOrderInPriorityDao.InsertAsync(model);
			}
			sortOrder += 268435456;
			result.Add(model);
		}
		return result;
	}

	private static async Task UpdateSortOrderInPriority(string id, long childOrder, int priority, string catId, string type = "task")
	{
		TaskSortOrderInPriorityModel taskSortOrderInPriorityModel = (await TaskSortOrderInPriorityDao.GetSortOrders(catId, priority, id, containDelete: true)).FirstOrDefault();
		if (taskSortOrderInPriorityModel != null)
		{
			taskSortOrderInPriorityModel.SortOrder = childOrder;
			taskSortOrderInPriorityModel.SyncStatus = 1;
			taskSortOrderInPriorityModel.Deleted = 0;
			taskSortOrderInPriorityModel.UserId = LocalSettings.Settings.LoginUserId;
			await TaskSortOrderInPriorityDao.UpdateAsync(taskSortOrderInPriorityModel);
		}
		else
		{
			await TaskSortOrderInPriorityDao.InsertAsync(new TaskSortOrderInPriorityModel
			{
				EntityId = id,
				EntityType = type,
				Priority = priority,
				CatId = catId,
				SyncStatus = 0,
				SortOrder = childOrder,
				UserId = LocalSettings.Settings.LoginUserId
			});
		}
	}

	public static async Task InitSortModels(string sortKey, string cId, List<DisplayItemModel> allModels)
	{
		List<SyncSortOrderModel> list = await TaskSortOrderService.GetAsyncList(sortKey, cId, force: true);
		long num = 0L;
		List<SyncSortOrderModel> result = new List<SyncSortOrderModel>();
		if (list != null)
		{
			result.AddRange(list);
		}
		List<SyncSortOrderModel> list2 = new List<SyncSortOrderModel>();
		List<SyncSortOrderModel> update = new List<SyncSortOrderModel>();
		foreach (DisplayItemModel m in allModels)
		{
			SyncSortOrderModel syncSortOrderModel = list?.FirstOrDefault((SyncSortOrderModel s) => s.EntityId == m.EntityId);
			int entityTypeNum = EntityType.GetEntityTypeNum(m.Type);
			if (syncSortOrderModel == null)
			{
				syncSortOrderModel = new SyncSortOrderModel(sortKey)
				{
					EntityId = m.EntityId,
					SortOrder = num,
					GroupId = cId,
					Type = entityTypeNum,
					SyncStatus = 1
				};
				list2.Add(syncSortOrderModel);
				result.Add(syncSortOrderModel);
			}
			else
			{
				syncSortOrderModel.SortOrder = num;
				syncSortOrderModel.SyncStatus = 1;
				syncSortOrderModel.Type = entityTypeNum;
				update.Add(syncSortOrderModel);
			}
			m.SpecialOrder = num;
			num += 268435456;
		}
		if (list2.Any())
		{
			await SyncSortOrderDao.InsertAllAsync(list2);
		}
		if (update.Any())
		{
			await SyncSortOrderDao.UpdateAllAsync(update);
		}
		TaskSortOrderService.SetSortModels(sortKey, cId, result);
	}

	public static async Task ChangeOrderInType(string sortOrderType, long? frontOrder, long? nextOrder, List<DisplayItemModel> dragModels, string catId)
	{
		long front = frontOrder ?? (nextOrder - 268435456).GetValueOrDefault();
		long num = nextOrder ?? (frontOrder + 268435456).GetValueOrDefault();
		long step = (num - front) / (dragModels.Count + 1);
		for (int i = 0; i < dragModels.Count; i++)
		{
			long value = front + step * (i + 1);
			await TaskSortOrderService.InsertOrUpdateAsync(sortOrderType, catId, dragModels[i].EntityId, EntityType.GetEntityTypeNum(dragModels[i].Type), value);
		}
	}
}
