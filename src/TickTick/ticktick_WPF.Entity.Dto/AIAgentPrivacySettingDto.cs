using Newtonsoft.Json;

namespace ticktick_WPF.Entity.Dto;

public sealed class AIAgentPrivacySettingDto
{
	[JsonProperty("data_collection_opt_in")]
	public bool? DataCollectionOptIn { get; set; }

	[JsonIgnore]
	public bool IsEnabled => DataCollectionOptIn ?? true;
}
