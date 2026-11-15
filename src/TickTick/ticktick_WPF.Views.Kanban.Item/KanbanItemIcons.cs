using System.Windows;
using System.Windows.Controls;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Pomo;
using ticktick_WPF.Views.TaskList;

namespace ticktick_WPF.Views.Kanban.Item;

public class KanbanItemIcons : TaskItemIcons
{
	private TextBlock _text;

	protected override void SetIcon(bool isDataContextChanged = false, bool showIcons = true)
	{
		if (!(base.DataContext is DisplayItemModel displayItemModel))
		{
			return;
		}
		int index = 0;
		AddTimeText(displayItemModel, ref index);
		if (displayItemModel.Habit?.Type?.ToLower() != "boolean")
		{
			HabitCheckInModel habitCheckIn = displayItemModel.HabitCheckIn;
			if (habitCheckIn != null && habitCheckIn.Value > 0.0)
			{
				ShowHabitProgress(displayItemModel.HabitCheckIn, displayItemModel.Status != 0, ref index);
			}
			ShowHabitProgressText(displayItemModel, ref index);
			base.Children.RemoveRange(index, base.Children.Count - index);
		}
		ShowIcon(displayItemModel.GetShowAttachment(), "Ic.TaskList.Attachment", "Attachment", ref index);
		ShowIcon(displayItemModel.ShowLocation, "Ic.BatchEdit.Locate", "Location", ref index);
		ShowIcon(displayItemModel.ShowComment, "Ic.TaskList.Comment", "Comment", ref index);
		ShowIcon(displayItemModel.ShowDescription, "Ic.TaskList.Detail", "Description", ref index);
		ShowRepeatImage(displayItemModel, ref index);
		ShowIcon(displayItemModel.ShowReminder == true, "Ic.ScheduleDetail.Remind", "reminder", ref index);
		ShowProgress(displayItemModel, ref index);
		base.Children.RemoveRange(index, base.Children.Count - index);
		if (displayItemModel.ShowPomo)
		{
			AddPomoSummary(displayItemModel.PomoSummary);
		}
		base.Visibility = ((base.Children.Count <= 0) ? Visibility.Collapsed : Visibility.Visible);
		Utils.FindParent<KanbanItemView>(this)?.SetContainerMargin(displayItemModel);
	}

	private void AddPomoSummary(PomodoroSummaryModel pomoSummary)
	{
		TaskItemPomoSummaryControl taskItemPomoSummaryControl = new TaskItemPomoSummaryControl
		{
			Margin = new Thickness(2.0, 2.0, 0.0, 2.0),
			MaxWidth = 150.0
		};
		base.Children.Add(taskItemPomoSummaryControl);
		taskItemPomoSummaryControl.SetData(pomoSummary);
	}
}
