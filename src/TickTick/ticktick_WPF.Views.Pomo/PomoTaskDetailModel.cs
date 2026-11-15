using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Pomo;

public class PomoTaskDetailModel : BaseViewModel
{
	public string TaskId;

	public int TaskStatus;

	public string ProjectId;

	public string Title { get; set; }

	public string Content { get; set; }

	public string Desc { get; set; }

	public bool IsText { get; set; } = true;

	public bool ShowDesc { get; set; }

	public ObservableCollection<PomoSubtaskDetailModel> Items { get; set; }

	public void SortItems()
	{
		if (!IsText)
		{
			ObservableCollection<PomoSubtaskDetailModel> items = Items;
			if (items != null && items.Count > 0)
			{
				List<PomoSubtaskDetailModel> list = Items.ToList();
				SortItems(list);
				Items.Clear();
				list.ForEach(Items.Add);
			}
		}
	}

	private static void SortItems(List<PomoSubtaskDetailModel> items)
	{
		items.Sort(delegate(PomoSubtaskDetailModel a, PomoSubtaskDetailModel b)
		{
			if (a.Status == b.Status)
			{
				if (a.Status == 0)
				{
					return a.SortOrder.CompareTo(b.SortOrder);
				}
				if (a.CompleteDate.HasValue && b.CompleteDate.HasValue)
				{
					return a.CompleteDate.Value.CompareTo(a.CompleteDate.Value);
				}
			}
			return a.Status.CompareTo(b.Status);
		});
	}

	public static async Task<PomoTaskDetailModel> GetDetailModel(string taskId)
	{
		TaskModel taskModel = await TaskDao.GetThinTaskById(taskId);
		if (taskModel == null || taskModel.status != 0)
		{
			return null;
		}
		PomoTaskDetailModel model = new PomoTaskDetailModel
		{
			TaskId = taskId,
			Title = taskModel.title,
			Content = TaskUtils.ReplaceAttachmentTextInString(taskModel.content),
			Desc = taskModel.desc,
			IsText = (taskModel.kind == "TEXT"),
			ShowDesc = (taskModel.kind != "TEXT" && !string.IsNullOrEmpty(taskModel.desc)),
			TaskStatus = taskModel.status,
			ProjectId = taskModel.projectId
		};
		if (!model.IsText)
		{
			List<PomoSubtaskDetailModel> obj = (await TaskDetailItemDao.GetCheckItemsByTaskId(taskId))?.Select((TaskDetailItemModel sub) => new PomoSubtaskDetailModel(sub)).ToList();
			SortItems(obj);
			model.Items = new ObservableCollection<PomoSubtaskDetailModel>();
			obj?.ForEach(model.Items.Add);
		}
		return model;
	}
}
