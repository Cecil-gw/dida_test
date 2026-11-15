using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CustomControl;

namespace ticktick_WPF.Views.Calendar;

public class OperationItemViewModel : UpDownSelectViewModel
{
	public ActionType Type { get; set; }

	public bool Enable { get; set; } = true;

	public List<OperationItemViewModel> SubActions { get; set; }

	public string Text { get; set; }

	public double ImageWidth { get; set; } = 24.0;

	public Thickness ImageMargin { get; set; } = new Thickness(12.0, 0.0, 0.0, 0.0);

	public DrawingImage Image { get; set; }

	public bool StayOpen { get; set; }

	public string ImageKey { get; set; } = "";

	public bool IsDelete { get; set; }

	public Geometry DeleteIcon { get; set; }

	public Geometry Icon { get; set; }

	public OperationItemViewModel(ActionType type)
	{
		Type = type;
		switch (type)
		{
		case ActionType.Archive:
			Icon = Utils.GetGeometry("Ic.Habit.Archive");
			Text = Utils.GetString("Archive");
			break;
		case ActionType.CancelArchive:
			Icon = Utils.GetGeometry("Ic.Habit.Unarchive");
			Text = Utils.GetString("CancelArchive");
			break;
		case ActionType.Skip:
			Icon = Utils.GetGeometry("Ic.ProjectList.Skip");
			Text = Utils.GetString("SkipHabit");
			break;
		case ActionType.UnComplete:
			Icon = Utils.GetGeometry("Ic.Common.Close");
			Text = Utils.GetString("Uncompleted");
			break;
		case ActionType.EditHabitLog:
			Icon = Utils.GetGeometry("Ic.Habit.Record");
			Text = Utils.GetString("EditLog");
			break;
		case ActionType.RecoverHabit:
			Icon = Utils.GetGeometry("Ic.Habit.Recover");
			Text = Utils.GetString("RecoverHabit");
			break;
		case ActionType.EditHabit:
			Icon = Utils.GetGeometry("Ic.Habit.Edit");
			Text = Utils.GetString("Edit");
			break;
		case ActionType.Delete:
			DeleteIcon = Utils.GetGeometry("Ic.ProjectList.Delete");
			Text = Utils.GetString("Delete");
			IsDelete = true;
			break;
		case ActionType.DeleteForever:
			DeleteIcon = Utils.GetGeometry("Ic.ProjectList.DeleteForever");
			Text = Utils.GetString("DeleteForever");
			IsDelete = true;
			break;
		case ActionType.Restore:
			Icon = Utils.GetGeometry("Ic.ProjectList.RecoverDelete");
			Text = Utils.GetString("Restore");
			break;
		case ActionType.StartFocus:
			Icon = Utils.GetGeometry("Ic.Habit.Focus");
			Text = Utils.GetString("StartFocus");
			break;
		case ActionType.EditStyle:
			Icon = Utils.GetGeometry("Ic.Habit.CardStyle");
			Text = Utils.GetString("checked_in_style");
			StayOpen = true;
			break;
		case ActionType.Checkmark:
			ImageWidth = 0.0;
			Text = Utils.GetString("checkmark");
			StayOpen = true;
			break;
		case ActionType.HabitIcon:
			ImageWidth = 0.0;
			Text = Utils.GetString("habit_icon");
			StayOpen = true;
			break;
		case ActionType.StartPomo:
			ImageWidth = 0.0;
			Text = Utils.GetString("StartPomo");
			break;
		case ActionType.StartTiming:
			ImageWidth = 0.0;
			Text = Utils.GetString("StartTiming");
			break;
		case ActionType.Reset:
			Icon = Utils.GetGeometry("Ic.Habit.Refresh");
			Text = Utils.GetString("Reset");
			break;
		}
	}
}
