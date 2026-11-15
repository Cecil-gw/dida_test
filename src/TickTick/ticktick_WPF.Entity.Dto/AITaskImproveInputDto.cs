using System.Collections.Generic;

namespace ticktick_WPF.Entity.Dto;

public class AITaskImproveInputDto
{
	public string userInput { get; set; }

	public string scene { get; set; }

	public string preCompleteAdvice { get; set; }

	public List<string> preSubTasks { get; set; }

	public string userPrompt { get; set; }

	public string nextStepAdvice { get; set; }
}
