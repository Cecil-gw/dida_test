namespace ticktick_WPF.Views.Timeline;

public static class TimelineExtensions
{
	public static string GetName(this TimelineColorType myEnum)
	{
		string text = myEnum.ToString();
		return char.ToLowerInvariant(text[0]) + text.Substring(1);
	}

	public static bool IsNormalStatus(this TimelineCellOperation myEnum)
	{
		TimelineCellOperation timelineCellOperation = myEnum.Remove(myEnum.GetPos());
		if (timelineCellOperation != TimelineCellOperation.None && timelineCellOperation != TimelineCellOperation.Hide)
		{
			return timelineCellOperation == TimelineCellOperation.Fold;
		}
		return true;
	}

	public static bool IsEditingOrMoving(this TimelineCellOperation myEnum)
	{
		if (!myEnum.Contain(TimelineCellOperation.Edit))
		{
			if (myEnum.GetPos() != 0)
			{
				return myEnum.Contain(TimelineCellOperation.Hover);
			}
			return false;
		}
		return true;
	}

	public static bool IsMoving(this TimelineCellOperation myEnum)
	{
		if (myEnum.GetPos() != 0)
		{
			return myEnum.Contain(TimelineCellOperation.Hover);
		}
		return false;
	}

	public static bool Contain(this TimelineCellOperation myEnum, TimelineCellOperation otherEnum)
	{
		return (myEnum & otherEnum) == otherEnum;
	}

	public static TimelineCellOperation Add(this TimelineCellOperation myEnum, TimelineCellOperation other)
	{
		return myEnum | other;
	}

	public static TimelineCellOperation Remove(this TimelineCellOperation myEnum, TimelineCellOperation other)
	{
		return myEnum & ~other;
	}

	public static TimelineCellOperation RemovePos(this TimelineCellOperation myEnum)
	{
		return myEnum.Remove(TimelineCellOperation.Start).Remove(TimelineCellOperation.Full).Remove(TimelineCellOperation.End);
	}

	public static TimelineCellOperation GetPos(this TimelineCellOperation myEnum)
	{
		return myEnum & (TimelineCellOperation.Start | TimelineCellOperation.Full | TimelineCellOperation.End);
	}

	public static TimelineCellOperation SetPos(this TimelineCellOperation myEnum, TimelineCellOperation other)
	{
		return myEnum.RemovePos() | other;
	}
}
