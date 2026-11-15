using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.TaskList;

public class TaskItemIcons : StackPanel
{
	private Style _imageStyle;

	protected DisplayItemController _controller;

	public TaskItemIcons()
	{
		base.Height = 20.0;
		base.Orientation = Orientation.Horizontal;
		base.DataContextChanged += OnDataBind;
	}

	private void OnDataBind(object sender, DependencyPropertyChangedEventArgs e)
	{
		SetIcon(isDataContextChanged: true);
	}

	public void ResetIcons(bool showIcons = true)
	{
		SetIcon(isDataContextChanged: false, showIcons);
	}

	protected virtual void SetIcon(bool isDataContextChanged = false, bool showIcons = true)
	{
		if (!(base.DataContext is DisplayItemModel displayItemModel))
		{
			return;
		}
		int index = 0;
		if (showIcons)
		{
			if (displayItemModel.HasChildren || displayItemModel.IsParentTask)
			{
				ShowIcon(show: true, "Ic.TaskList.SubTask", "Subtask", ref index);
				ShowSubTasksCompletionRate(modelShowChildrenIcon: true, displayItemModel, ref index, isDataContextChanged);
			}
			ShowIcon(displayItemModel.GetShowAttachment(), "Ic.TaskList.Attachment", "Attachment", ref index);
			ShowIcon(displayItemModel.ShowLocation, "Ic.ScheduleDetail.Locate", "Location", ref index);
			ShowIcon(displayItemModel.ShowComment, "Ic.TaskList.Comment", "Comment", ref index);
			ShowIcon(displayItemModel.ShowDescription, "Ic.ScheduleDetail.Detail", "Description", ref index);
			ShowRepeatImage(displayItemModel, ref index);
			ShowIcon(displayItemModel.ShowReminder == true, "Ic.ScheduleDetail.Remind", "reminder", ref index);
			ShowProgress(displayItemModel, ref index);
			if (displayItemModel.Habit?.Type?.ToLower() != "boolean")
			{
				HabitCheckInModel habitCheckIn = displayItemModel.HabitCheckIn;
				if (habitCheckIn != null && habitCheckIn.Value > 0.0)
				{
					ShowHabitProgress(displayItemModel.HabitCheckIn, displayItemModel.Status != 0, ref index);
				}
				ShowHabitProgressText(displayItemModel, ref index);
			}
		}
		base.Children.RemoveRange(index, base.Children.Count - index);
		AddTimeText(displayItemModel, ref index);
	}

	protected void AddTimeText(DisplayItemModel model, ref int index)
	{
		bool flag = model.Enable && !model.IsHabit;
		if ((model.StartDate.HasValue && !model.IsNote) || !DateUtils.IsOutDated(model.StartDate, model.DueDate, model.IsAllDay))
		{
			if (base.Children.Count > index && base.Children[index] is TextBlock { Name: "TimeText" } textBlock)
			{
				textBlock.Cursor = (flag ? Cursors.Hand : Cursors.Arrow);
				if (flag)
				{
					textBlock.MouseLeftButtonUp -= ShowSetDateDialog;
					textBlock.MouseLeftButtonUp += ShowSetDateDialog;
				}
				else
				{
					textBlock.MouseLeftButtonUp -= ShowSetDateDialog;
				}
			}
			else
			{
				TextBlock textBlock2 = new TextBlock
				{
					Cursor = (flag ? Cursors.Hand : Cursors.Arrow),
					Margin = new Thickness(4.0, -1.0, 4.0, 0.0),
					Name = "TimeText"
				};
				textBlock2.SetResourceReference(FrameworkElement.StyleProperty, "TimeTextStyle");
				if (flag)
				{
					textBlock2.MouseLeftButtonUp += ShowSetDateDialog;
				}
				base.Children.Insert(index, textBlock2);
			}
			index++;
		}
		else if (base.Children.Count > index && base.Children[index] is TextBlock { Name: "TimeText" } textBlock3)
		{
			base.Children.Remove(textBlock3);
		}
	}

	private void ShowSetDateDialog(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		if (base.DataContext is DisplayItemModel { Enable: not false })
		{
			_controller?.SelectDate(this, -100.0);
		}
	}

	private void ShowSubTasksCompletionRate(bool modelShowChildrenIcon, DisplayItemModel model, ref int index, bool isDataContextChanged)
	{
		if (LocalSettings.Settings.ShowDetails && !LocalSettings.Settings.InSearch && !model.InMatrix && modelShowChildrenIcon)
		{
			if (base.Children.Count > index && base.Children[index] is TextBlock textBlock && textBlock.Name != "TimeText")
			{
				textBlock.Text = model.CompletionRate;
			}
			else
			{
				TextBlock textBlock2 = new TextBlock
				{
					VerticalAlignment = VerticalAlignment.Center,
					Margin = new Thickness(2.0, 0.0, 1.0, 0.0),
					MaxWidth = 150.0,
					TextTrimming = TextTrimming.CharacterEllipsis
				};
				textBlock2.SetResourceReference(FrameworkElement.StyleProperty, "TaskListViewTitleStyle");
				textBlock2.SetResourceReference(TextBlock.ForegroundProperty, (model.Status == 0) ? "BaseColorOpacity60" : "BaseColorOpacity20");
				textBlock2.Text = model.CompletionRate;
				base.Children.Insert(index, textBlock2);
			}
			index++;
		}
	}

	protected void ShowHabitProgressText(DisplayItemModel model, ref int index)
	{
		if (model.Habit != null)
		{
			string text = ((model.HabitCheckIn == null) ? (0 + "/" + model.Habit.Goal + " " + model.Habit.Unit) : (model.HabitCheckIn.Value + "/" + model.HabitCheckIn.Goal + " " + model.Habit.Unit));
			if (base.Children.Count > index && base.Children[index] is TextBlock textBlock && textBlock.Name != "TimeText")
			{
				textBlock.Text = text;
				textBlock.ToolTip = text;
			}
			else
			{
				TextBlock textBlock2 = new TextBlock
				{
					VerticalAlignment = VerticalAlignment.Center,
					Margin = new Thickness(2.0, 0.0, 0.0, 0.0),
					Text = text,
					ToolTip = text,
					MaxWidth = 122.0,
					TextTrimming = TextTrimming.CharacterEllipsis
				};
				textBlock2.SetResourceReference(FrameworkElement.StyleProperty, "TaskListViewTitleStyle");
				textBlock2.SetResourceReference(TextBlock.ForegroundProperty, (model.Status == 0) ? "BaseColorOpacity60" : "BaseColorOpacity20");
				base.Children.Insert(index, textBlock2);
			}
			index++;
		}
	}

	protected void ShowHabitProgress(HabitCheckInModel checkIn, bool completed, ref int index)
	{
		if (checkIn == null)
		{
			return;
		}
		int num = Math.Min((int)(checkIn.Value / checkIn.Goal * 100.0), 100);
		CycleProgressBar cycleProgressBar;
		if (base.Children.Count > index)
		{
			cycleProgressBar = base.Children[index] as CycleProgressBar;
			if (cycleProgressBar != null)
			{
				cycleProgressBar.TargetPercent = num;
				goto IL_011a;
			}
		}
		cycleProgressBar = new CycleProgressBar
		{
			Duration = 10,
			Height = 12.0,
			Width = 12.0,
			Thickness = 2.0,
			HorizontalAlignment = HorizontalAlignment.Center,
			TargetPercent = num,
			Margin = new Thickness(4.0, 0.0, 0.0, 0.0),
			Opacity = (completed ? 0.4 : 0.8)
		};
		base.Children.Insert(index, cycleProgressBar);
		cycleProgressBar.SetResourceReference(CycleProgressBar.UnderColorProperty, "BaseColorOpacity20");
		cycleProgressBar.SetResourceReference(CycleProgressBar.TopColorProperty, "PrimaryColor");
		goto IL_011a;
		IL_011a:
		cycleProgressBar.ToolTip = num + "%";
		index++;
	}

	protected void ShowProgress(DisplayItemModel model, ref int index)
	{
		if (model.ShowProgress)
		{
			if (base.Children.Count > index && base.Children[index] is TaskCircleProgress taskCircleProgress)
			{
				taskCircleProgress.Percentage = model.Progress;
				taskCircleProgress.ToolTip = model.ProgressContent;
			}
			else
			{
				TaskCircleProgress taskCircleProgress2 = new TaskCircleProgress
				{
					Percentage = model.Progress,
					Radius = 4.5,
					StrokeThickness = 1.0,
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center,
					Margin = new Thickness(2.0, 0.0, 2.0, 0.0)
				};
				taskCircleProgress2.SetResourceReference(TaskCircleProgress.SegmentColorProperty, "BaseColorOpacity40");
				taskCircleProgress2.ToolTip = model.ProgressContent;
				base.Children.Insert(index, taskCircleProgress2);
			}
			index++;
		}
	}

	protected void ShowRepeatImage(DisplayItemModel model, ref int index)
	{
		if (model.ShowRepeat)
		{
			string tooltip;
			if (RepeatUtils.GetRepeatType(model.RepeatFrom, model.RepeatFlag) == RepeatFromType.Custom)
			{
				tooltip = Utils.GetString("RepeatByCustom");
			}
			else if (string.IsNullOrEmpty(model.RepeatFlag) || model.RepeatFlag == "RRULE:FREQ=NONE")
			{
				tooltip = string.Empty;
			}
			else
			{
				string repeatFlag = model.RepeatFlag;
				tooltip = ((repeatFlag.Contains("FREQ=DAILY") && repeatFlag.Contains("TT_SKIP=HOLIDAY,WEEKEND") && (!repeatFlag.Contains("INTERVAL") || repeatFlag.Contains("INTERVAL=1"))) ? Utils.GetString("OfficialWorkingDays") : RRuleUtils.RRule2String(model.RepeatFrom, model.RepeatFlag, model.StartDate));
			}
			ShowIcon(show: true, "Ic.ScheduleDetail.Repeat", tooltip, ref index, isRepeat: true);
		}
	}

	private void ShowSubTaskImage(string source, string tooltip, ref int index, bool isDataContextChanged)
	{
		if (base.Children.Count > index && base.Children[index] is Image image)
		{
			image.SetResourceReference(Image.SourceProperty, source);
			image.SetResourceReference(FrameworkElement.ToolTipProperty, tooltip);
		}
		else
		{
			Image image2 = new Image();
			image2.SetResourceReference(Image.SourceProperty, source);
			image2.SetResourceReference(FrameworkElement.StyleProperty, "ListIcon");
			image2.SetResourceReference(FrameworkElement.ToolTipProperty, tooltip);
			base.Children.Insert(index, image2);
		}
		index++;
	}

	protected void ShowIcon(bool show, string source, string tooltip, ref int index, bool isRepeat = false)
	{
		if (!show)
		{
			return;
		}
		if (base.Children.Count > index && base.Children[index] is Border border)
		{
			(border.Child as Path)?.SetResourceReference(Path.DataProperty, source);
			if (!isRepeat)
			{
				border.SetResourceReference(FrameworkElement.ToolTipProperty, tooltip);
			}
			else
			{
				border.ToolTip = tooltip;
			}
		}
		else
		{
			Path path = new Path();
			path.SetResourceReference(Path.DataProperty, source);
			path.SetResourceReference(FrameworkElement.StyleProperty, "ListPathIcon");
			Border border2 = new Border
			{
				Background = Brushes.Transparent
			};
			if (!isRepeat)
			{
				border2.SetResourceReference(FrameworkElement.ToolTipProperty, tooltip);
			}
			else
			{
				border2.ToolTip = tooltip;
				border2.SetValue(ToolTipService.InitialShowDelayProperty, 200);
			}
			border2.Child = path;
			base.Children.Insert(index, border2);
		}
		index++;
	}

	protected void ShowImage(bool show, string source, string tooltip, ref int index)
	{
		if (show)
		{
			if (base.Children.Count > index && base.Children[index] is Image image)
			{
				image.SetResourceReference(Image.SourceProperty, source);
				image.SetResourceReference(FrameworkElement.ToolTipProperty, tooltip);
			}
			else
			{
				Image image2 = new Image();
				image2.SetResourceReference(Image.SourceProperty, source);
				image2.SetResourceReference(FrameworkElement.StyleProperty, "ListIcon");
				image2.SetResourceReference(FrameworkElement.ToolTipProperty, tooltip);
				base.Children.Insert(index, image2);
			}
			index++;
		}
	}

	public void SetController(DisplayItemController controller)
	{
		_controller = controller;
	}
}
