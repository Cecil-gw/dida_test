using System.Collections.Generic;
using Newtonsoft.Json;

namespace ticktick_WPF.Models;

public class SearchResultModel
{
	[JsonProperty("hits")]
	public List<SearchHits> Hits { get; set; }

	[JsonProperty("tasks")]
	public List<TaskModel> Tasks { get; set; }

	[JsonProperty("comments")]
	public List<CommentModel> Comments { get; set; }
}
