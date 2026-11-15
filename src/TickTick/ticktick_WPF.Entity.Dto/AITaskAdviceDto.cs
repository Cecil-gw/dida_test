using System.Collections.Generic;

namespace ticktick_WPF.Entity.Dto;

internal class AITaskAdviceDto
{
	public string completeAdvice { get; set; }

	public List<string> nextStepAdvices { get; set; }
}
