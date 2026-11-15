using ticktick_WPF.Util.Sync.Model;

namespace ticktick_WPF.Util.Sync;

public abstract class BatchHandler
{
	protected string userId;

	protected SyncResult syncResult;

	public BatchHandler(string userId, SyncResult syncResult)
	{
		this.userId = userId;
		this.syncResult = syncResult;
	}
}
