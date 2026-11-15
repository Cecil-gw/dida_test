using System.Collections.Generic;

namespace ticktick_WPF.Entity.Dto;

internal class AITaskImproveDto : AITaskAdviceDto
{
	public List<string> subTasks { get; set; }
}
