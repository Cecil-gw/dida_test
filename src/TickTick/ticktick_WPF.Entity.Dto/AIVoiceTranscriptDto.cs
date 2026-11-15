using System.Collections.Generic;

namespace ticktick_WPF.Entity.Dto;

public class AIVoiceTranscriptDto
{
	public string language { get; set; }

	public string raw { get; set; }

	public List<string> text { get; set; }

	public List<long[]> timestamp { get; set; }
}
