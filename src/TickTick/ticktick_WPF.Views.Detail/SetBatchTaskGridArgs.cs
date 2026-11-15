namespace ticktick_WPF.Views.Detail;

public class SetBatchTaskGridArgs
{
	public bool CanSwitchTaskOrNote = true;

	public bool CanDelete = true;

	public bool CanPinOrUnPin = true;

	public bool HasSamePriority = true;

	public bool HasSameDate = true;

	public bool HasSameProject = true;

	public bool HasSameTag = true;

	public string TagStr = string.Empty;

	public bool HasTask;

	public bool HasNote;

	public bool HasAbandoned;

	public bool HasUncompleted;

	public bool HasCompleted;

	public bool IsDeleted;

	public bool IsAllPinned = true;

	public bool IsAllCompleted = true;

	public bool CanPostpone;

	public bool CanPostponeTime;

	public bool CanSwitchTask
	{
		get
		{
			if (CanSwitchTaskOrNote)
			{
				return OnlyNote;
			}
			return false;
		}
	}

	public bool CanSwitchNote
	{
		get
		{
			if (CanSwitchTaskOrNote)
			{
				return OnlyTask;
			}
			return false;
		}
	}

	public bool CanMerge
	{
		get
		{
			if (!IsDeleted)
			{
				return OnlyTask;
			}
			return false;
		}
	}

	public bool CanLinkTask
	{
		get
		{
			if (!IsDeleted)
			{
				return OnlyTask;
			}
			return false;
		}
	}

	public bool OnlyNote
	{
		get
		{
			if (HasNote)
			{
				return !HasTask;
			}
			return false;
		}
	}

	public bool OnlyTask
	{
		get
		{
			if (HasTask)
			{
				return !HasNote;
			}
			return false;
		}
	}
}
