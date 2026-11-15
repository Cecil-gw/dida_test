using System.Collections.Generic;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Views.Setting;

public class TaskDefaultViewModel
{
	public List<string> Tags { get; set; }

	public int DateMode { get; set; }

	public int PriorityIndex { get; set; }

	public int DateIndex { get; set; }

	public int AddTo { get; set; }

	public int Duration { get; set; }

	public TaskDefaultViewModel(TaskDefaultModel model)
	{
		DateMode = model.DateMode;
		PriorityIndex = PriorityToIndex(model.Priority);
		AddTo = model.AddTo;
		DateIndex = Constants.DateValue.ToSelectIndex(model.Date);
		Duration = model.Duration;
		Tags = model.Tags;
	}

	public TaskDefaultModel ToModel()
	{
		return new TaskDefaultModel
		{
			DateMode = DateMode,
			Priority = IndexToPriority(PriorityIndex),
			AddTo = AddTo,
			Date = Constants.DateValue.ToValue(DateIndex),
			Duration = Duration,
			Tags = Tags
		};
	}

	private static int PriorityToIndex(int priority)
	{
		return priority switch
		{
			5 => 0, 
			3 => 1, 
			1 => 2, 
			0 => 3, 
			_ => 3, 
		};
	}

	private static int IndexToPriority(int index)
	{
		return index switch
		{
			0 => 5, 
			1 => 3, 
			2 => 1, 
			3 => 0, 
			_ => 0, 
		};
	}
}
