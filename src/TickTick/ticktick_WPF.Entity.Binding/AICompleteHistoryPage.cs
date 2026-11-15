using System.Collections.Generic;
using ticktick_WPF.Entity.Enums;

namespace ticktick_WPF.Entity.Binding;

internal class AICompleteHistoryPage
{
	public int Page { get; set; }

	public string SupplementQuestion { get; set; }

	public string SupplementInput { get; set; }

	public string Suggestion { get; set; }

	public List<string> Choices { get; set; }

	public List<AISubTaskEditItem> SubTasks { get; set; }

	public string SelectChoice { get; set; }

	public string EditPrompt { get; set; }

	public string LastSummary
	{
		get
		{
			if (!string.IsNullOrEmpty(SelectChoice))
			{
				return SelectChoice;
			}
			if (!string.IsNullOrEmpty(EditPrompt))
			{
				return EditPrompt;
			}
			return string.Empty;
		}
	}

	public AICompleteStep Step { get; set; }

	public AICompleteState State { get; set; }
}
