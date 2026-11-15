using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Calendar.Agenda;

public class AgendaContentIcons : StackPanel
{
	private Rectangle _avatar;

	public AgendaContentIcons()
	{
		base.Height = 20.0;
		base.Orientation = Orientation.Horizontal;
		SetValue(Grid.ColumnProperty, 1);
		base.Margin = new Thickness(0.0, 10.0, 12.0, 0.0);
		base.VerticalAlignment = VerticalAlignment.Top;
		base.HorizontalAlignment = HorizontalAlignment.Right;
	}

	public void ResetIcons()
	{
		SetIcon();
	}

	private void SetIcon()
	{
		if (base.DataContext is CalendarAgendaItemViewModel calendarAgendaItemViewModel)
		{
			if (calendarAgendaItemViewModel.SourceViewModel.IsHabit)
			{
				SetHabitIcons(calendarAgendaItemViewModel);
			}
			else if (calendarAgendaItemViewModel.SourceViewModel.IsPomo)
			{
				SetPomoDuration(calendarAgendaItemViewModel);
			}
			else
			{
				SetTaskIcons(calendarAgendaItemViewModel);
			}
		}
	}

	private void SetPomoDuration(CalendarAgendaItemViewModel model)
	{
		TextBlock textBlock;
		if (base.Children.Count == 1)
		{
			textBlock = base.Children[0] as TextBlock;
			if (textBlock != null)
			{
				goto IL_0060;
			}
		}
		base.Children.Clear();
		textBlock = new TextBlock
		{
			FontSize = 12.0
		};
		textBlock.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity40");
		base.Children.Add(textBlock);
		goto IL_0060;
		IL_0060:
		textBlock.Text = Utils.GetShortDurationString(model.SourceViewModel.PomoDuration, showZeroMinute: false);
	}

	private void SetTaskIcons(CalendarAgendaItemViewModel model)
	{
		int index = 0;
		ShowImage(model.ShowDescription, "HasDescDrawingImage", "Description", ref index);
		ShowRepeatImage(model, ref index);
		ShowImage(model.ShowReminder == true, "reminderDrawingImage", "reminder", ref index);
		ShowProgress(model, ref index);
		try
		{
			Rectangle rectangle = ((base.Children.Count > 0) ? (base.Children[base.Children.Count - 1] as Rectangle) : null);
			if (base.Children.Count > index + ((rectangle != null) ? 1 : 0))
			{
				base.Children.RemoveRange(index, base.Children.Count - index - ((rectangle != null) ? 1 : 0));
			}
			ShowAvatar(model, rectangle);
		}
		catch
		{
		}
	}

	protected void ShowProgress(CalendarAgendaItemViewModel model, ref int index)
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
					Margin = new Thickness(2.0, 0.0, 2.0, 0.0),
					IsHitTestVisible = false
				};
				taskCircleProgress2.SetResourceReference(TaskCircleProgress.SegmentColorProperty, "BaseColorOpacity20");
				taskCircleProgress2.ToolTip = model.ProgressContent;
				base.Children.Insert(index, taskCircleProgress2);
			}
			index++;
		}
	}

	public void SetHabitIcons(CalendarAgendaItemViewModel model)
	{
		if (!(model.SourceViewModel is HabitBaseViewModel habitBaseViewModel))
		{
			return;
		}
		habitBaseViewModel.HabitCheckIn = model.HabitCheckIn;
		int index = 0;
		if (habitBaseViewModel.Habit?.Type?.ToLower() != "boolean")
		{
			HabitCheckInModel habitCheckIn = model.HabitCheckIn;
			if (habitCheckIn != null && habitCheckIn.Value > 0.0)
			{
				ShowHabitProgress(model.HabitCheckIn, model.Status != 0, ref index);
			}
			base.Children.RemoveRange(index, base.Children.Count - index);
		}
		else
		{
			base.Children.Clear();
		}
	}

	protected void ShowHabitProgressText(HabitBaseViewModel model, ref int index)
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
			Thickness = 1.5,
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

	private void ShowRepeatImage(CalendarAgendaItemViewModel model, ref int index)
	{
		if (model.ShowRepeat)
		{
			string toolTip;
			if (RepeatUtils.GetRepeatType(model.RepeatFrom, model.RepeatFlag) == RepeatFromType.Custom)
			{
				toolTip = Utils.GetString("RepeatByCustom");
			}
			else if (string.IsNullOrEmpty(model.RepeatFlag) || model.RepeatFlag == "RRULE:FREQ=NONE")
			{
				toolTip = string.Empty;
			}
			else
			{
				string repeatFlag = model.RepeatFlag;
				toolTip = ((repeatFlag.Contains("FREQ=DAILY") && repeatFlag.Contains("TT_SKIP=HOLIDAY,WEEKEND") && (!repeatFlag.Contains("INTERVAL") || repeatFlag.Contains("INTERVAL=1"))) ? Utils.GetString("OfficialWorkingDays") : RRuleUtils.RRule2String(model.RepeatFrom, model.RepeatFlag, model.StartDate));
			}
			if (base.Children.Count > index && base.Children[index] is Image image)
			{
				image.SetResourceReference(Image.SourceProperty, "RepeatDrawingImage");
				image.ToolTip = toolTip;
			}
			else
			{
				Image image2 = new Image();
				image2.SetResourceReference(Image.SourceProperty, "RepeatDrawingImage");
				image2.SetResourceReference(FrameworkElement.StyleProperty, "ListIcon");
				image2.ToolTip = toolTip;
				image2.SetValue(ToolTipService.InitialShowDelayProperty, 200);
				base.Children.Insert(index, image2);
			}
			index++;
		}
	}

	private void ShowImage(bool show, string source, string tooltip, ref int index)
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

	private void ShowAvatar(CalendarAgendaItemViewModel model, Rectangle avatar)
	{
		if (!string.IsNullOrEmpty(model.AvatarUrl))
		{
			if (avatar == null)
			{
				avatar = new Rectangle
				{
					Width = 20.0,
					Height = 20.0,
					RadiusX = 10.0,
					RadiusY = 10.0,
					Margin = new Thickness(2.0, 0.0, 0.0, 0.0)
				};
				base.Children.Add(avatar);
			}
			avatar.Opacity = ((model.Status == 0) ? 1.0 : 0.4);
			SetAvatarImage(avatar, model);
			avatar.ToolTip = AvatarHelper.GetCacheUserName(model.Assignee, model.ProjectId);
		}
		else if (avatar != null)
		{
			base.Children.Remove(avatar);
		}
	}

	private async void SetAvatarImage(Rectangle avatar, CalendarAgendaItemViewModel model)
	{
		ImageBrush imageBrush = new ImageBrush();
		ImageBrush imageBrush2 = imageBrush;
		imageBrush2.ImageSource = await model.GetAvatar();
		avatar.Fill = imageBrush;
	}
}
