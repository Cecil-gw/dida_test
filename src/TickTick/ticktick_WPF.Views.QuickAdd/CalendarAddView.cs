using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.QuickAdd;

public class CalendarAddView : QuickAddView
{
	private string _accountKind;

	public new event EventHandler<CalendarEventModel> CalendarEventAdded;

	public CalendarAddView(BindCalendarAccountModel calendar, bool focus = false)
		: base(new BindAccountCalendarProjectIdentity(calendar), Scenario.TaskList, "light", focus)
	{
		_accountKind = calendar?.Kind;
		SetCalBorder.Visibility = Visibility.Collapsed;
		ContentHint.Text = Utils.GetString("SubscribeDescription");
	}

	public void SetAccountKind(string kind)
	{
		_accountKind = kind;
	}

	public override void ShowSelectProjectPopup(bool enter = false, UIElement target = null, ProjectShowExtra extra = null, bool ignoreProject = false, bool fromCommand = false)
	{
		if (base.Model.IsCalendar && target == null)
		{
			target = MoreGrid;
		}
		base.ShowSelectProjectPopup(enter, target, extra, ignoreProject, fromCommand);
	}

	protected override void CalendarSelect(string title, string id)
	{
		base.CalendarSelect(title, id);
		_accountKind = CacheManager.GetAccountCalById(CacheManager.GetBindCalendars().FirstOrDefault((BindCalendarModel cal) => cal.Id == id)?.AccountId)?.Kind;
	}

	public override async Task DoAddTask(string title)
	{
		if (!base.Model.IsCalendar)
		{
			base.Model.EnsureTaskProjectAfterCalendarConvert();
			ResetNonCalendarTimeZoneWhenDisabled();
			await base.DoAddTask(title);
		}
		else
		{
			if (string.IsNullOrEmpty(TitleText.EditBox.Text) && string.IsNullOrEmpty(ContentText.Text))
			{
				return;
			}
			string text = ((_accountKind == "caldav" || _accountKind == "icloud") ? Guid.NewGuid().ToString() : Utils.GetGuid());
			string id = text + "@" + base.Model.CalendarId;
			CalendarEventModel calEvent = new CalendarEventModel
			{
				UserId = Utils.GetCurrentUserIdInt().ToString(),
				Id = id,
				Uid = text,
				Title = title,
				Content = ContentText.GetPersistedText(),
				EventId = text,
				CalendarId = base.Model.CalendarId,
				Type = 0,
				TimeZone = (base.Model.TimeData?.TimeZone?.TimeZoneName ?? Utils.GetLocalTimeZone())
			};
			if (base.Model.TimeData == null || !base.Model.TimeData.StartDate.HasValue)
			{
				base.Model.TimeData = TimeData.InitDefaultDuration();
			}
			if (!base.Model.TimeData.DueDate.HasValue)
			{
				int num = TaskDefaultDao.GetDefaultSafely()?.Duration ?? 60;
				base.Model.TimeData.DueDate = ((base.Model.TimeData.IsAllDay == true) ? base.Model.TimeData.StartDate.Value.Date : base.Model.TimeData.StartDate.Value.AddMinutes(num));
			}
			if ((base.Model.TimeData?.StartDate.HasValue ?? false) && (base.Model.TimeData?.DueDate.HasValue ?? false))
			{
				calEvent.IsAllDay = base.Model.TimeData.IsAllDay == true;
				TaskDefaultModel defaultSafely = TaskDefaultDao.GetDefaultSafely();
				if (calEvent.IsAllDay)
				{
					calEvent.DueStart = base.Model.TimeData?.StartDate.Value;
					calEvent.DueEnd = base.Model.TimeData?.DueDate.Value;
					if (calEvent.DueStart.Value == calEvent.DueEnd.Value)
					{
						calEvent.DueEnd = calEvent.DueEnd.Value.AddDays(1.0);
					}
					if (!string.IsNullOrEmpty(defaultSafely.AllDayReminders) && base.Model.TimeData.IsDefault && _accountKind != "icloud")
					{
						calEvent.Reminders = JsonConvert.SerializeObject(defaultSafely.AllDayReminders.Split(',').Select(TriggerUtils.TriggerToReminder));
					}
				}
				else
				{
					calEvent.DueStart = base.Model.TimeData.StartDate.Value;
					calEvent.DueEnd = base.Model.TimeData.DueDate ?? base.Model.TimeData.StartDate.Value;
					if (!string.IsNullOrEmpty(defaultSafely.TimeReminders) && base.Model.TimeData.IsDefault && _accountKind != "icloud")
					{
						calEvent.Reminders = JsonConvert.SerializeObject(defaultSafely.TimeReminders.Split(',').Select(TriggerUtils.TriggerToReminder));
					}
				}
				if ((base.Model.TimeData.Reminders?.Any() ?? false) && _accountKind != "icloud")
				{
					calEvent.Reminders = JsonConvert.SerializeObject(base.Model.TimeData.Reminders.Select((TaskReminderModel reminder) => reminder.trigger).ToList().Select(TriggerUtils.TriggerToReminder)
						.ToList());
				}
				calEvent.RepeatFlag = base.Model.TimeData.RepeatFlag;
			}
			await CalendarService.AddEvent(calEvent);
			BindCalendarModel bindCalendarModel = CacheManager.GetBindCalendars().FirstOrDefault((BindCalendarModel cal) => cal.Id == base.Model.CalendarId);
			if (bindCalendarModel != null && bindCalendarModel.Accessible)
			{
				this.CalendarEventAdded?.Invoke(this, calEvent);
				base.Model.OriginalCalendarId = bindCalendarModel.Id;
				base.Model.OriginalCalendarName = bindCalendarModel.Name;
				base.Model.ProjectName = bindCalendarModel.Name;
			}
			ResetAfterAddTask();
			RefreshHint();
			DataChangedNotifier.NotifyCalendarChanged(200);
			SyncManager.TryDelaySync();
		}
	}
}
