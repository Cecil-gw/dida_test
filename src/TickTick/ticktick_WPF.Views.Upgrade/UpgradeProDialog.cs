using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using TickTickUtils;
using XamlAnimatedGif;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Upgrade;

public class UpgradeProDialog : MyWindow, IOkCancelWindow, IComponentConnector
{
	private readonly ProType _type;

	private string _teamId;

	internal Border DisplayImage;

	internal Image GifImage;

	internal PathFigure InsidePath;

	internal TextBlock UpdateTitleTextBlock;

	internal TextBlock UpdateContentTextBlock;

	internal TextBlock UpdateDescExtraTextBlock;

	internal Button UpdateNowButton;

	internal Button KnowMoreText;

	internal TextBlock HintText;

	internal Button CloseButton;

	private bool _contentLoaded;

	public UpgradeProDialog()
	{
		InitializeComponent();
	}

	public UpgradeProDialog(ProType type, bool imageExist, string name, string teamId = null)
	{
		InitializeComponent();
		_type = type;
		if (type == ProType.CalendarWidget)
		{
			type = ProType.CalendarView;
		}
		if (type == ProType.StickyColor)
		{
			type = ProType.PremiumThemes;
		}
		UpdateTitleTextBlock.Text = GetProTitle(type);
		KnowMoreText.Visibility = (IsTeamProType() ? Visibility.Collapsed : Visibility.Visible);
		string text = GetProContent(type);
		TeamModel team = CacheManager.GetTeam();
		if (team != null)
		{
			_teamId = team.id;
			text = string.Format(text, team?.name);
		}
		UpdateContentTextBlock.Text = text;
		string text2 = Utils.GetString(type.ToString() + "ExtraProSummary", string.Empty);
		if (!string.IsNullOrEmpty(text2))
		{
			UpdateDescExtraTextBlock.Text = text2;
			UpdateDescExtraTextBlock.Visibility = Visibility.Visible;
		}
		string text3 = Utils.GetString(type.ToString() + "ExtraProHint", string.Empty);
		if (!string.IsNullOrEmpty(text3))
		{
			HintText.Text = text3;
		}
		if (type == ProType.CalendarView && ABTestManager.UsePremiumV2CalendarProStyle())
		{
			ProCarouselControl proCarouselControl = new ProCarouselControl();
			DisplayImage.Child = proCarouselControl;
			UpdateTitleTextBlock.Text = proCarouselControl.SelectedItem?.Title;
			UpdateContentTextBlock.Text = proCarouselControl.SelectedItem?.Desc;
			proCarouselControl.CurrentChanged += delegate(object sender, CarouselItemViewModel model)
			{
				UpdateTitleTextBlock.Text = model.Title;
				UpdateContentTextBlock.Text = model.Desc;
				App.Window.OnCalendarProModelSwitch(model);
			};
			App.Window.MainCalendar?.SwitchVirtualView("Month");
		}
		else if (imageExist)
		{
			try
			{
				GifImage.SetValue(AnimationBehavior.SourceUriProperty, new Uri(AppPaths.UpgradeProDir + name));
			}
			catch (Exception)
			{
			}
		}
		UserActCollectUtils.SendUpgradePromptData(ProTypeToString(_type));
	}

	private bool IsTeamProType()
	{
		if (_type != ProType.TeamShareLimit)
		{
			return _type == ProType.MoreTeamGuest;
		}
		return true;
	}

	private string GetProContent(ProType type)
	{
		switch (type)
		{
		case ProType.MoreListsUnlimited:
			return string.Format(Utils.GetString(type.ToString() + "ProSummary"), LimitCache.GetProLimitByKey(Constants.LimitKind.ProjectNumber), Utils.GetAppName());
		case ProType.MoreTeamGuest:
			return string.Format(Utils.GetString(type.ToString() + "ProSummary"), LimitCache.GetProLimitByKey(Constants.LimitKind.ShareUserNumber), Utils.GetAppName());
		case ProType.IntegrationWithGoogle:
		case ProType.IntegrationWithNotion:
			return string.Format(Utils.GetString(type.ToString() + "ProSummary"), Utils.GetAppName());
		case ProType.ParallelShowInTask:
		case ProType.ParallelShowInCalendar:
			return Utils.GetString("TaskShowWithCalendarDesc");
		case ProType.MoreCountdowns:
		case ProType.MoreAnniversaries:
			return Utils.GetString("MoreCountdownsProSummary");
		case ProType.CountdownPremiumThemes:
			return string.Format(Utils.GetString(type.ToString() + "ProSummary"), Utils.GetAppName());
		case ProType.AICompleteSuggest:
			return Utils.GetString("AICompleteProDesc");
		case ProType.AIRecommendTask:
			return Utils.GetString("AIRecommendProDesc");
		case ProType.MoreAttachments:
		{
			long proLimitByKey = LimitCache.GetProLimitByKey(Constants.LimitKind.DailyUploadNumber);
			return string.Format(Utils.GetString("ProAttachmentLimitDesc"), proLimitByKey);
		}
		case ProType.AIAudio:
			return Utils.GetString("AIRecordingSummaryDesc");
		case ProType.ReminderForSubTasks:
			if (Utils.IsDida())
			{
				return Utils.GetString("ReminderForSubTasksDidaProSummary");
			}
			return Utils.GetString(type.ToString() + "ProSummary");
		case ProType.Matrix:
			return Utils.GetString("ManageMultipleViewsSummary");
		default:
			return Utils.GetString(type.ToString() + "ProSummary");
		}
	}

	private string GetProTitle(ProType type)
	{
		switch (type)
		{
		case ProType.MoreTeamGuest:
		case ProType.TaskActivities:
		case ProType.NoteActivities:
		case ProType.IntegrationWithGoogle:
		case ProType.Matrix:
		case ProType.MatrixWidget:
		case ProType.TimeLine:
		case ProType.EmailReminder:
		case ProType.TeamShareLimit:
		case ProType.FocusAutoSync:
		case ProType.FocusMiniStyle:
		case ProType.IntegrationWithNotion:
		case ProType.CountdownWidget:
			return Utils.GetString(type.ToString() + string.Empty);
		case ProType.MoreListsUnlimited:
			return Utils.GetString("MoreListsPro");
		case ProType.ParallelShowInTask:
		case ProType.ParallelShowInCalendar:
			return Utils.GetString("TaskShowWithCalendar");
		case ProType.AICompleteSuggest:
			return Utils.GetString("AICompleteProTitle");
		case ProType.AIRecommendTask:
			return Utils.GetString("AIRecommendProTitle");
		case ProType.CountdownCustomRepeat:
			return Utils.GetString("CustomRepeat");
		case ProType.AIAudio:
			return Utils.GetString("AIRecordingSummaryTitle");
		case ProType.ConstantReminder:
			return Utils.GetString(ProType.ConstantReminder.ToString());
		case ProType.AIAssistant:
			return Utils.GetString(ProType.AIAssistant.ToString());
		default:
			return Utils.GetString(type.ToString() + "Pro");
		}
	}

	private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (!CloseButton.IsMouseOver && (!(DisplayImage.Child is ProCarouselControl proCarouselControl) || !proCarouselControl.IsButtonMouseOver()) && e.LeftButton == MouseButtonState.Pressed)
		{
			DragMove();
			e.Handled = false;
		}
	}

	protected override void OnClosed(EventArgs e)
	{
		if (DisplayImage.Child is ProCarouselControl proCarouselControl)
		{
			DisplayImage.Child = null;
			proCarouselControl.Dispose();
		}
		base.OnClosed(e);
	}

	private void CloseButton_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private static string ProTypeToString(ProType type)
	{
		string empty = string.Empty;
		switch (type)
		{
		case ProType.Matrix:
			return "matrix";
		case ProType.TimeLine:
			return "timeline";
		case ProType.MatrixWidget:
			return "matrix_widget";
		case ProType.CountdownWidget:
			return "countdown_widget";
		case ProType.CalendarView:
			return "calendar_view_dialog";
		case ProType.Duration:
			return "time_duration";
		case ProType.Filter:
			return "filter";
		case ProType.MoreSharingMembers:
			return "share_count";
		case ProType.MoreReminders:
			return "reminder_count";
		case ProType.MoreLists:
			return "list_count";
		case ProType.MoreSubTasks:
			return "sub_task_count";
		case ProType.MoreTasks:
			return "task_count";
		case ProType.TaskActivities:
		case ProType.NoteActivities:
			return "task_activity";
		case ProType.ListActivities:
			return "project_activity";
		case ProType.MoreAttachments:
			return "upload_count";
		case ProType.ReminderForSubTasks:
			return "sub_task_reminder";
		case ProType.PremiumThemes:
			return "theme";
		case ProType.SubscribeCalendar:
			return "subscribe_calendar";
		case ProType.EstimatedDuration:
			return "estimated_duration";
		case ProType.GoogleEventSync:
			return "subscribe_calendar";
		case ProType.PomoSound:
			return "white_noises";
		case ProType.MoreHabits:
			return "habit_count";
		case ProType.AppIcon:
			return "app_icon";
		case ProType.Font:
			return "fonts";
		case ProType.StickyColor:
			return "sticky_note_color";
		case ProType.EmailReminder:
			return "email_notifications";
		case ProType.MoreTimers:
			return "timer_count";
		case ProType.MoreCharts:
			return "timer_statistics";
		case ProType.TeamShareLimit:
			return "more_team_share_lists";
		case ProType.MoreTeamGuest:
			return "more_team_guests_count";
		case ProType.CalendarWidget:
			return "calendar_widget";
		case ProType.FocusAutoSync:
			return "focus_sync";
		case ProType.FocusMiniStyle:
			return "focus_mini_style";
		case ProType.SummaryTemplate:
			return "summary_template";
		case ProType.MiniCalendar:
			return "mini_calendar";
		case ProType.SummaryStyle:
			return "summary_contents";
		case ProType.IntegrationWithGoogle:
			return "integrate_google";
		case ProType.IntegrationWithNotion:
			return "integrate_notion";
		case ProType.ParallelShowInTask:
		case ProType.ParallelShowInCalendar:
			return "split_view";
		case ProType.MoreCountdowns:
		case ProType.MoreAnniversaries:
			return "more_countdown";
		case ProType.CountdownPremiumThemes:
			return "countdown_styles";
		case ProType.AICompleteSuggest:
			return "ai_suggestion";
		case ProType.AIRecommendTask:
			return "ai_suggested_tasks";
		case ProType.CountdownCustomRepeat:
			return "custom_repeat";
		case ProType.AIAudio:
			return "ai_recording";
		case ProType.ConstantReminder:
			return "constant_reminder";
		case ProType.AIAssistant:
			return "ai_assistant";
		default:
			return type.ToString();
		}
	}

	private async void UpdateNowClick(object sender, RoutedEventArgs e)
	{
		if (IsTeamProType())
		{
			Utils.TryProcessStartUrlWithToken("#tu/" + _teamId);
			Close();
		}
		else if ((await UserDao.QueryUserModelListDbByIdAsync(LocalSettings.Settings.LoginUserId)).teamUser)
		{
			ShowUpgradeOptionPopup();
		}
		else
		{
			PerformPersonalUpgrade();
		}
	}

	private void ShowUpgradeOptionPopup()
	{
		List<CustomMenuItemViewModel> types = new List<CustomMenuItemViewModel>
		{
			new CustomMenuItemViewModel((object)0, Utils.GetString("upgrade_for_personal"), (Geometry)null, ""),
			new CustomMenuItemViewModel((object)1, Utils.GetString("upgrade_for_team"), (Geometry)null, "")
		};
		EscPopup popup = new EscPopup
		{
			StaysOpen = false,
			Placement = PlacementMode.Bottom,
			PlacementTarget = UpdateNowButton,
			HorizontalOffset = -5.0,
			VerticalOffset = -5.0
		};
		CustomMenuList customMenuList = new CustomMenuList(types, popup);
		customMenuList.Operated += OnActionSelected;
		customMenuList.Show();
	}

	private void OnActionSelected(object sender, object e)
	{
		if (e is int num)
		{
			switch (num)
			{
			case 0:
				PerformPersonalUpgrade();
				break;
			case 1:
				Utils.TryProcessStartUrlWithToken("#tu/" + _teamId);
				Close();
				break;
			}
		}
	}

	private void PerformPersonalUpgrade()
	{
		if (DisplayImage.Child is ProCarouselControl proCarouselControl)
		{
			proCarouselControl.StopTimer();
		}
		Utils.StartUpgrade(ProTypeToString(_type), this);
	}

	public void OnCancel()
	{
		Close();
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		if (_type == ProType.CalendarView && LocalSettings.Settings.MainWindowDisplayModule == 1)
		{
			string text = "";
			bool flag = true;
			if (DisplayImage.Child is ProCarouselControl proCarouselControl)
			{
				text = proCarouselControl.CurrentDisplayValue();
				flag = !proCarouselControl.IsTimerStop();
				App.Window.ShowCalendarProView(text, flag);
			}
			else
			{
				App.Window.MainCalendar.ShowProToast();
			}
		}
		base.OnClosing(e);
	}

	public void Ok()
	{
		Close();
	}

	private void KnowMoreClick(object sender, RoutedEventArgs e)
	{
		Close();
		Utils.StartUpgradeOnWeb("prompt_learn_more");
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/upgrade/updatemessagewindow.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	internal Delegate _CreateDelegate(Type delegateType, string handler)
	{
		return Delegate.CreateDelegate(delegateType, this, handler);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			DisplayImage = (Border)target;
			break;
		case 2:
			GifImage = (Image)target;
			break;
		case 3:
			InsidePath = (PathFigure)target;
			break;
		case 4:
			UpdateTitleTextBlock = (TextBlock)target;
			break;
		case 5:
			UpdateContentTextBlock = (TextBlock)target;
			break;
		case 6:
			UpdateDescExtraTextBlock = (TextBlock)target;
			break;
		case 7:
			UpdateNowButton = (Button)target;
			UpdateNowButton.Click += UpdateNowClick;
			break;
		case 8:
			KnowMoreText = (Button)target;
			KnowMoreText.Click += KnowMoreClick;
			break;
		case 9:
			HintText = (TextBlock)target;
			break;
		case 10:
			CloseButton = (Button)target;
			CloseButton.Click += CloseButton_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
