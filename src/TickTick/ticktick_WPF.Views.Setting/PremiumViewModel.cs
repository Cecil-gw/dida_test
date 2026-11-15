using System.Collections.Generic;
using ticktick_WPF.Cache;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Setting;

public class PremiumViewModel : BaseViewModel
{
	public string Title { get; set; }

	public string Desc { get; set; }

	public string Image { get; set; }

	public PremiumViewModel(string title, string desc, string imageName)
	{
		Title = title;
		Desc = desc;
		Image = "../../Assets/ImageSource/" + imageName + ".png";
	}

	public static List<PremiumViewModel> BuildViewModels()
	{
		long proLimitByKey = LimitCache.GetProLimitByKey(Constants.LimitKind.DailyUploadNumber);
		long proLimitByKey2 = LimitCache.GetProLimitByKey(Constants.LimitKind.ReminderNumber);
		long proLimitByKey3 = LimitCache.GetProLimitByKey(Constants.LimitKind.ShareUserNumber);
		return new List<PremiumViewModel>
		{
			new PremiumViewModel(Utils.GetString("CalMonthView"), Utils.GetString("CalMonthViewDesc"), "CalMonthView"),
			new PremiumViewModel(Utils.GetString("CalTimelineView"), Utils.GetString("CalTimelineViewDesc"), "CalTimelineView"),
			new PremiumViewModel(Utils.GetString("Duration"), Utils.GetString("DurationDesc"), "Duration"),
			new PremiumViewModel(Utils.GetString("SubscribedCalendars"), Utils.GetString("SubscribeCalendarsDesc"), "SubscribedCalendars"),
			new PremiumViewModel(Utils.GetString("Filter"), Utils.GetString("FilterDesc"), "Filter"),
			new PremiumViewModel(Utils.GetString("CheckItemReminder"), Utils.GetString("CheckItemReminderDesc"), "CheckItemReminder"),
			new PremiumViewModel(Utils.GetString("PremiumThemesPro"), Utils.GetString("PremiumThemesDesc"), "PremiumThemes"),
			new PremiumViewModel(Utils.GetString("MoreListTaskItem"), Utils.GetString("MoreListTaskItemDesc"), "MoreListTaskItem"),
			new PremiumViewModel(Utils.GetString("MoreSharingMembersPro"), string.Format(Utils.GetString("MoreSharingMembersDesc"), proLimitByKey3), "MoreSharingMembers"),
			new PremiumViewModel(Utils.GetString("MoreAttachmentsPro"), string.Format(Utils.GetString("MoreAttachmentsDesc"), proLimitByKey), "MoreAttachments"),
			new PremiumViewModel(Utils.GetString("MoreRemindersPro"), string.Format(Utils.GetString("MoreRemindersDesc"), proLimitByKey2), "MoreReminders"),
			new PremiumViewModel(Utils.GetString("TaskActivities"), Utils.GetString("TaskActivitiesDesc"), "TaskActivities")
		};
	}
}
