namespace ticktick_WPF.Resource;

public class Status
{
	public const int DeleteNo = 0;

	public const int DeleteTrash = 1;

	public const int DeleteForever = 2;

	public const int SyncExist = -2;

	public const int SyncNew = 0;

	public const int SyncUpdate = 1;

	public const int SyncDone = 2;

	public const int SyncInit = 3;

	public const int SyncDelete = -1;

	public const int SyncErrorUpLimit = 4;

	public const int UnArchived = 0;

	public const int Abandoned = -1;

	public const int Uncompleted = 0;

	public const int Archived = 1;

	public const int Completed = 2;

	public const int SubTaskUncompleted = 0;

	public const int SubTaskCompleted = 1;

	public const int UnCheckedHabit = 0;

	public const int UncompletedHabit = 1;

	public const int CompletedHabit = 2;

	public const int Focusing = 0;

	public const int FocusPause = 1;

	public const int FocusFinish = 2;

	public const int FocusAbandon = 3;
}
