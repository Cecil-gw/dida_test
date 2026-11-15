using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Habit;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Widget;

internal class HabitEventsHelper
{
	public static async Task CheckDayCheck(HabitModel model, HabitDayCheckModel dayCheck, FrameworkElement ui)
	{
		if (model == null || dayCheck == null)
		{
			return;
		}
		model = await HabitDao.GetHabitById(model.Id);
		if (model != null)
		{
			if (dayCheck.Status == 1 || !(dayCheck.CheckInModel.Value < dayCheck.CheckInModel.Goal))
			{
				await ResetHabit(model, dayCheck, ui);
			}
			else
			{
				await CheckHabit(model, dayCheck, ui);
			}
		}
	}

	private static async Task ResetHabit(HabitModel model, HabitDayCheckModel dayCheck, FrameworkElement element)
	{
		if (model != null)
		{
			if (dayCheck.IsBooleanHabit || Math.Abs(model.Step) < 0.001)
			{
				dayCheck.CheckInModel.Value = 0.0;
				dayCheck.Percent = 0.0;
				dayCheck.Status = 0;
				await Task.Delay(10);
			}
			if (await HabitService.ResetCheckInHabit(model.Id, dayCheck.Date, model.Type, element))
			{
				dayCheck.SetData(null, model);
			}
		}
	}

	private static async Task CheckHabit(HabitModel model, HabitDayCheckModel dayCheck, FrameworkElement _clickElement)
	{
		if (model != null)
		{
			if (model.IsBoolHabit())
			{
				await CheckInBooleanHabit(model, dayCheck, _clickElement);
				return;
			}
			if (Math.Abs(model.Step + 1.0) <= 0.001)
			{
				ManuallyRecordCheckIn(dayCheck, _clickElement);
				return;
			}
			HabitAction action = ((!(Math.Abs(model.Step) < 1E-05)) ? HabitAction.AutoRecord : HabitAction.CheckIn);
			await CheckInRealHabit(dayCheck, model, model.Step, _clickElement, action);
		}
	}

	private static async Task CheckInBooleanHabit(HabitModel model, HabitDayCheckModel dayCheck, FrameworkElement element)
	{
		if (dayCheck?.CheckInModel != null && model != null)
		{
			dayCheck.CheckInModel.Value = dayCheck.CheckInModel.Goal;
			dayCheck.Status = 2;
			Utils.PlayCompletionSound();
			await Task.Delay(16);
			await HabitService.CheckInHabit(model.Id, dayCheck.Date, -1.0, increment: true, checkShowRecord: true, 0, null, element, HabitAction.CheckIn);
		}
	}

	private static async Task ManuallyRecordCheckIn(HabitDayCheckModel dayCheck, UIElement element)
	{
		if (dayCheck == null)
		{
			return;
		}
		dayCheck.Selected = true;
		HabitModel habit = await HabitDao.GetHabitById(dayCheck.HabitId);
		if (habit == null)
		{
			return;
		}
		EscPopup popup = new EscPopup
		{
			PlacementTarget = element,
			Placement = PlacementMode.Bottom,
			StaysOpen = false,
			HorizontalOffset = 0.0,
			VerticalOffset = 0.0,
			PopupAnimation = PopupAnimation.Fade
		};
		ManualRecordCheckinControl manual = new ManualRecordCheckinControl();
		popup.Child = manual;
		manual.Init(habit.Unit);
		popup.Closed += delegate
		{
			dayCheck.Selected = false;
		};
		manual.Save += async delegate(object sender, double step)
		{
			if (step > 0.0)
			{
				await CheckInRealHabit(dayCheck, habit, step, element, HabitAction.ManualRecord);
			}
		};
		manual.Save += delegate
		{
			popup.IsOpen = false;
		};
		manual.Cancel += delegate
		{
			popup.IsOpen = false;
		};
		await Task.Delay(10);
		popup.IsOpen = true;
		Utils.Focus(manual.CheckInText);
		Keyboard.Focus(manual.CheckInText);
		HwndHelper.SetFocus(popup);
	}

	private static async Task CheckInRealHabit(HabitDayCheckModel dayCheck, HabitModel habit, double step, UIElement placement, HabitAction action)
	{
		HabitCheckInModel habitCheckInModel = await HabitService.CheckInHabit(habit.Id, dayCheck.Date, step, increment: true, checkShowRecord: true, 0, null, placement, action);
		if (habitCheckInModel != null)
		{
			Utils.PlayCompletionSound();
			dayCheck.CheckInModel.Goal = habitCheckInModel.Goal;
			dayCheck.CheckInModel.Value = Math.Min(habitCheckInModel.GetDisplayValue(), habitCheckInModel.Goal);
			dayCheck.Status = habitCheckInModel.CheckStatus;
			dayCheck.Percent = Math.Min((int)(dayCheck.CheckInModel.Value / dayCheck.CheckInModel.Goal * 100.0), 100);
			if (dayCheck.Percent >= 100.0)
			{
				dayCheck.Status = 2;
			}
		}
	}
}
