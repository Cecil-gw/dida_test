namespace ticktick_WPF.Util.Sync.Model;

public class SyncTaskParentEtag
{
	public string id { get; set; }

	public string etag { get; set; }

	public string parentId { get; set; }

	public string[] childIds { get; set; }
}
