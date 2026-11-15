using Newtonsoft.Json;

namespace ticktick_WPF.Models;

public class UploadBindCalendarSortDto
{
	[JsonProperty("id")]
	public string Id { get; set; }

	[JsonProperty("isUrl")]
	public bool IsUrl { get; set; }

	[JsonProperty("order")]
	public long Order { get; set; }
}
