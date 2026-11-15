using System;
using ticktick_WPF.Models;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Pomo;

public class PomoSubtaskDetailModel : BaseViewModel
{
	public string Id;

	public long SortOrder;

	public string Title { get; set; }

	public int Status { get; set; }

	public DateTime? CompleteDate { get; set; }

	public PomoSubtaskDetailModel(TaskDetailItemModel item)
	{
		Id = item.id;
		Title = item.title;
		SortOrder = item.sortOrder;
		Status = item.status;
		CompleteDate = item.completedTime;
	}
}
