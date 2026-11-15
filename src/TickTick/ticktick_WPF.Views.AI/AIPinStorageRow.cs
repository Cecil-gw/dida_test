using SQLite;

namespace ticktick_WPF.Views.AI;

[Table("AIConversationPins")]
internal sealed class AIPinStorageRow
{
	[PrimaryKey]
	public string Scope { get; set; }

	public string Json { get; set; }
}
