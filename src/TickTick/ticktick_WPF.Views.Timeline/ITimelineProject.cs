using System.Collections.Generic;
using ticktick_WPF.Models;
using ticktick_WPF.Views.TaskList;

namespace ticktick_WPF.Views.Timeline;

internal interface ITimelineProject
{
	TimelineModel GetTimelineModel();

	void CommitTimeline(TimelineModel model);

	List<SortTypeViewModel> GetTimelineSortTypes();
}
