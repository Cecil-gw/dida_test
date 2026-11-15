using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Newtonsoft.Json;
using ticktick_WPF.Models;
using ticktick_WPF.Service;
using ticktick_WPF.Views.Project;

namespace ticktick_WPF.Resource;

public class ExtraSettings
{
	public bool MiniCalendarEnabled;

	public bool FirstShowCopyDialog = true;

	public bool CopyKeepStatus;

	public string CopyNotShowDateStamp = "";

	public int ShowTipCountType1;

	public int ShowTipCountType2;

	public int ShowTipCountType3;

	public int EnterTaskCount;

	public bool DisableWidgetDesktopOwner { get; set; }

	public bool AIOpaqueBrowserBackground { get; set; }

	public bool AIUseStandardWebView { get; set; }

	public long LastPullYearPromoTime { get; set; }

	public bool UseSystemTheme { get; set; } = true;

	public string AppTheme { get; set; }

	public string AI_ApiBaseUrl { get; set; } = "";

	public string AI_ApiKey { get; set; } = "";

	public string AI_Model { get; set; } = "gpt-4o-mini";

	public int LastActiveTime { get; set; }

	public int LastCompleteTime { get; set; }

	public bool TLUsed { get; set; }

	public string AppFontFamily { get; set; }

	public long LastPullHolidayTime { get; set; }

	public bool ShowCalWeekend { get; set; } = true;

	public long FocusPushPoint { get; set; }

	public string StickyDefaultColor { get; set; }

	public bool StickyDefaultPin { get; set; }

	public bool StickyHideInTaskBar { get; set; }

	public bool ResetStickyWhenAlign { get; set; }

	public int StickySpacing { get; set; } = 20;

	public int StickyFont { get; set; } = 1;

	public double StickyOpacity { get; set; } = 100.0;

	public bool ShowNoteInCalArrange { get; set; } = true;

	public bool ShowParentInCalArrange { get; set; }

	public double FocusVolume { get; set; } = 100.0;

	public double ReminderVolume { get; set; } = 100.0;

	public bool MenuFold { get; set; }

	public bool ProjectParallelMenuFold { get; set; } = true;

	public string ProjectParallelSelectProject { get; set; }

	public ProjectExtra CalendarParallelSelectProject { get; set; } = new ProjectExtra();

	public string EpT { get; set; }

	public int CpltStoryTimes { get; set; }

	public string RemindSound { get; set; }

	public int RemindDetail { get; set; } = 1;

	public int NumDisplayType { get; set; }

	public int ShowCompleteLine { get; set; }

	public string RecentTimezone { get; set; }

	public int LastSnoozeMinutes { get; set; }

	public string ShowHideStickyShortCut { get; set; }

	public string CurrentFocus { get; set; }

	public bool TwitterNotified { get; set; }

	public string CalMultiViewNums { get; set; } = "5,2";

	public int SortOrderOfSummary { get; set; } = 6400;

	public bool KbShowAdd { get; set; }

	public bool KbShowColumnColor { get; set; }

	public bool PwShowAdd { get; set; } = true;

	public int KbSize { get; set; } = 1;

	public string TodayAttachmentCount { get; set; }

	public string SelectedSummaryTemplateId { get; set; }

	public string DefaultSummaryTemplate { get; set; }

	public bool DoNotDisturbInCalendar { get; set; }

	public bool SummaryFilterImported { get; set; }

	public bool TimelineShowNote { get; set; } = true;

	public bool TimelineShowParent { get; set; }

	public bool TimelineArrangeOverDue { get; set; }

	public bool ReminderCalculated { get; set; }

	public long BindCalendarExpireCheckTime { get; set; }

	public bool SummaryTemplateTooltipShown { get; set; }

	public Dictionary<string, string> ABTestData { get; set; } = new Dictionary<string, string>();

	public string AppConfigEtag { get; set; }

	public int AppConfigInterval { get; set; } = 14400;

	public int LastClearTaskTime { get; set; }

	public string TaskFoldStatus { get; set; }

	public int ShowProjectTimes { get; set; }

	public bool ShowMoveProjToTeam { get; set; } = true;

	public string ListCalendarSettings { get; set; }

	[JsonIgnore]
	public bool ShowCalDetail => false;

	public SmartProjectsOption SmartProjectsOption { get; set; }

	public bool HabitDateSelected { get; set; }

	public double HabitDivide { get; set; } = 0.75;

	public bool ShowCalCheckBox { get; set; }

	public List<string> HiddenCalTimeZones { get; set; }

	public string TaskDetailLayout { get; set; }

	public ListViewColumnStyle ListViewColumnStyle { get; set; }

	public int CalFontSize { get; set; } = 2;

	public string LastVersion { get; set; } = "-1";

	public int GraceDateNotifyDate { get; set; }

	public DateTime? DuplicateSubsNotifyDate { get; set; }

	public DateTime WeiboLoginWarnDate { get; set; }

	public double TaskWindowWidth { get; set; } = -1.0;

	public double TaskWindowHeight { get; set; } = -1.0;

	public bool ShowCountdownInCal { get; set; } = true;

	public bool HasCreatedInitCountdowns { get; set; }

	public string LastSelectCountdownGroup { get; set; }

	[JsonIgnore]
	public bool IsNewVersion { get; set; }

	[JsonIgnore]
	public long LastPomoChangeMonthCheckPoint { get; set; }

	public bool HasSendLocalCalendarVis { get; set; }

	public bool HasSendLocalUrlVis { get; set; }

	public string LastShowReleaseNoteVersion { get; set; }

	public string LastUpdateNotifyImportantVersion { get; set; }

	public string LastUpdateRedDotVersion { get; set; }

	public string LastUpdateRedDotNewVersion { get; set; }

	public AppUpdateFromType? AppUpdateFrom { get; set; }

	public bool HasShowPomoApendTimeTip { get; set; }

	public HashSet<string> HasSetFlags { get; set; }

	public int ConstantReminderTipCount { get; set; }

	public InterfaceStyle InterfaceStyle { get; set; }

	public string SelectTheme { get; set; }

	public string LastDarkTheme { get; set; }

	public string CustomDarkThemeColor { get; set; }

	public bool IsShowProjectListColor { get; set; } = true;

	public Size LastAIAudioWindowSize { get; set; }

	public bool AIFloatingEntryDocked { get; set; }

	public AIFloatingEntryPlacement AIFloatingEntryPlacement { get; set; }

	public int GetCalendarDefaultMultiNum(bool isMultiDay)
	{
		if (CalMultiViewNums != null)
		{
			string[] array = CalMultiViewNums.Split(',');
			if (array.Length == 2 && int.TryParse(array[(!isMultiDay) ? 1u : 0u], out var result))
			{
				if (isMultiDay)
				{
					return Math.Max(1, Math.Min(14, result));
				}
				return Math.Max(2, Math.Min(6, result));
			}
		}
		if (!isMultiDay)
		{
			return 2;
		}
		return 5;
	}

	public void SetCalendarDefaultMultiNum(bool isMultiDay, int num)
	{
		if (isMultiDay)
		{
			int calendarDefaultMultiNum = GetCalendarDefaultMultiNum(isMultiDay: false);
			CalMultiViewNums = num + "," + calendarDefaultMultiNum;
		}
		else
		{
			CalMultiViewNums = GetCalendarDefaultMultiNum(isMultiDay: true) + "," + num;
		}
	}

	public bool CheckHasFlag(HasSetFlag flag)
	{
		switch (flag)
		{
		case HasSetFlag.SendLocalAccountVis:
			if (HasSendLocalCalendarVis)
			{
				return true;
			}
			break;
		case HasSetFlag.SendLocalUrlVis:
			if (HasSendLocalUrlVis)
			{
				return true;
			}
			break;
		case HasSetFlag.PomoAppendHint:
			if (HasShowPomoApendTimeTip)
			{
				return true;
			}
			break;
		}
		if (HasSetFlags == null || !HasSetFlags.Any())
		{
			return false;
		}
		return HasSetFlags.Contains(flag.ToString());
	}

	public void SetHasFlag(HasSetFlag flag)
	{
		if (HasSetFlags == null)
		{
			HasSetFlags = new HashSet<string>();
		}
		HasSetFlags.Add(flag.ToString());
	}
}
