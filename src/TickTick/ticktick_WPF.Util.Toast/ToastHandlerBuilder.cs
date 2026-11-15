namespace ticktick_WPF.Util.Toast;

public class ToastHandlerBuilder
{
	public static IToastHandler Build(ToastType type)
	{
		if (IsRemindToast(type))
		{
			return new ReminderToastHandler();
		}
		if (IsPomoToast(type))
		{
			return new PomoToastHandler();
		}
		if (IsHabitToast(type))
		{
			return new HabitToastHandler();
		}
		return null;
	}

	public static bool IsRemindToast(ToastType type)
	{
		if (type != ToastType.RemindTask && type != ToastType.RemindCheckItem && type != ToastType.RemindEvent && type != ToastType.RemindHabit && type != ToastType.RemindCountdown)
		{
			return type == ToastType.RemindCourse;
		}
		return true;
	}

	public static bool IsPomoToast(ToastType type)
	{
		if (type != ToastType.PomoCompleted)
		{
			return type == ToastType.PomoRelaxCompleted;
		}
		return true;
	}

	public static bool IsHabitToast(ToastType type)
	{
		if (type != ToastType.MultiHabitCompleted)
		{
			return type == ToastType.BoolHabitCompleted;
		}
		return true;
	}
}
