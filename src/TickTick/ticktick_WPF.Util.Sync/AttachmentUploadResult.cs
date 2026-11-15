using Newtonsoft.Json;

namespace ticktick_WPF.Util.Sync;

public class AttachmentUploadResult
{
	[JsonProperty(PropertyName = "id")]
	public string Id { get; set; }
}
