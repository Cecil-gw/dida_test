using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.Views.Guide;
using ticktick_WPF.Views.Upgrade;

namespace ticktick_WPF.Views.Config;

public class DebugSettings : UserControl, IComponentConnector
{
	public static string DebugPremiumGroupA = "premium2_202407_a";

	public static string DebugPremiumGroupB = "premium2_202407_b";

	public static string DebugPremiumGroupC = "premium2_202407_c";

	public static string DebugPremiumGroupO = "premium2_202407_o1";

	public static string CurrentDebugPremiumGroup = DebugPremiumGroupC;

	public static string DebugAIVoiceGroupA = "ai_voice_202603_a";

	public static string DebugAIVoiceGroupO = "ai_voice_202603_o";

	public static string CurrentDebugAIVoiceGroup = DebugAIVoiceGroupA;

	private static int _proType = 0;

	internal Button ProSwitchBtn;

	internal TextBlock CalendarTest;

	internal Button PremiumGroup;

	internal Button AIVoiceGroup;

	private bool _contentLoaded;

	public DebugSettings()
	{
		InitializeComponent();
		InitPremiumGroup();
		InitAIVoiceGroup();
		SetProText();
	}

	private void InitPremiumGroup()
	{
		PremiumGroup.Content = "付费分组: " + CurrentDebugPremiumGroup;
	}

	private void InitAIVoiceGroup()
	{
		AIVoiceGroup.Content = "AI语音分组: " + CurrentDebugAIVoiceGroup;
	}

	private void ABTestClick(object sender, RoutedEventArgs e)
	{
	}

	private async void ConfigTestClick(object sender, RoutedEventArgs e)
	{
	}

	private void CalendarClick(object sender, RoutedEventArgs e)
	{
		CalendarDisplay calendarDisplay = new CalendarDisplay(new DateTime(2025, 7, 25), CalendarConvertType.Korean);
		if (calendarDisplay.OutLimit)
		{
			CalendarTest.Text = string.Empty;
		}
		else
		{
			CalendarTest.Text = calendarDisplay.DisplayText();
		}
	}

	private void LeapClick(object sender, RoutedEventArgs e)
	{
		System.Globalization.Calendar calendar = CalendarConverter.GetCalendar(CalendarConvertType.Hebcal);
		DateTime dateTime = new DateTime(1970, 1, 1);
		HashSet<string> hashSet = new HashSet<string>();
		for (int i = 0; i < 30000; i++)
		{
			dateTime = dateTime.AddDays(1.0);
			int year = calendar.GetYear(dateTime);
			int month = calendar.GetMonth(dateTime);
			int dayOfMonth = calendar.GetDayOfMonth(dateTime);
			int era = calendar.GetEra(dateTime);
			if (calendar.IsLeapMonth(year, month, era) && dayOfMonth == 1)
			{
				string text = string.Format("{0} - {1} - {2}, {3}, {4}", year, month, dayOfMonth, dateTime.ToString("yyyyMMdd", App.Ci), new CalendarDisplay(dateTime, CalendarConvertType.Hebcal).DisplayText());
				if (hashSet.Add(text))
				{
					TextBlock calendarTest = CalendarTest;
					calendarTest.Text = calendarTest.Text + text + "\n";
				}
			}
		}
		CalendarTest.Text += "finish";
	}

	private void ProClick(object sender, RoutedEventArgs e)
	{
		_proType = (_proType + 1) % 3;
		SetProText();
		MainWindowManager.SetCalendarProStatus();
	}

	private void SetProText()
	{
		switch (_proType)
		{
		case 0:
			ProSwitchBtn.Content = "AutoPro";
			break;
		case 1:
			ProSwitchBtn.Content = "ProTrue";
			break;
		case 2:
			ProSwitchBtn.Content = "ProFalse";
			break;
		}
	}

	public static bool GetDebugPro()
	{
		return UserDao.IsPro2();
	}

	private void MarkdownClick(object sender, RoutedEventArgs e)
	{
		LocalSettings settings = LocalSettings.Settings;
		settings.ExtraSettings.CopyNotShowDateStamp = "";
		settings.ExtraSettings.FirstShowCopyDialog = true;
		settings.ExtraSettings.CopyKeepStatus = false;
		settings.Save();
	}

	private void UpgradeClick(object sender, RoutedEventArgs e)
	{
		if (CurrentDebugPremiumGroup == DebugPremiumGroupC)
		{
			CurrentDebugPremiumGroup = DebugPremiumGroupO;
		}
		else if (CurrentDebugPremiumGroup == DebugPremiumGroupB)
		{
			CurrentDebugPremiumGroup = DebugPremiumGroupC;
		}
		else if (CurrentDebugPremiumGroup == DebugPremiumGroupO)
		{
			CurrentDebugPremiumGroup = DebugPremiumGroupA;
		}
		else if (CurrentDebugPremiumGroup == DebugPremiumGroupA)
		{
			CurrentDebugPremiumGroup = DebugPremiumGroupB;
		}
		InitPremiumGroup();
	}

	private void AIVoiceClick(object sender, RoutedEventArgs e)
	{
		if (CurrentDebugAIVoiceGroup == DebugAIVoiceGroupA)
		{
			CurrentDebugAIVoiceGroup = DebugAIVoiceGroupO;
		}
		else
		{
			CurrentDebugAIVoiceGroup = DebugAIVoiceGroupA;
		}
		InitAIVoiceGroup();
	}

	private void UpgradeClick2(object sender, RoutedEventArgs e)
	{
		Utils.StartUpgrade("settings");
	}

	private async void GetOrderSpec()
	{
		await Communicator.GetSubscribeSpecList();
		UtilLog.Info("");
	}

	private void GetOrderClick(object sender, RoutedEventArgs e)
	{
		GetOrderSpec();
	}

	private void PaySuccessClick(object sender, RoutedEventArgs e)
	{
		PaySuccessWindow paySuccessWindow = new PaySuccessWindow(DateTime.Today);
		paySuccessWindow.Owner = App.Window;
		paySuccessWindow.ShowDialog();
	}

	private void PayFailureClick(object sender, RoutedEventArgs e)
	{
		PayFailureWindow payFailureWindow = new PayFailureWindow();
		payFailureWindow.Owner = App.Window;
		payFailureWindow.ShowDialog();
	}

	private void NumberTestClick(object sender, RoutedEventArgs e)
	{
		Utils.ConvertNumberToLetters(26);
		Utils.ConvertNumberToLetters(27);
		Utils.ConvertNumberToLetters(28);
		UtilLog.Info("");
	}

	private void FuncGuideTest(object sender, RoutedEventArgs e)
	{
		CommonGuideWindow commonGuideWindow = new CommonGuideWindow();
		commonGuideWindow.Set8FuncGuide();
		commonGuideWindow.ShowDialog();
	}

	private void MDClearTest(object sender, RoutedEventArgs e)
	{
		UtilLog.Info(TaskUtils.CleanMarkdownMarksInString("<mark style=\"background: #FF5582A6;\">高亮的内容</mark>"));
	}

	private void HintTest(object sender, RoutedEventArgs e)
	{
		CustomerDialog customerDialog = new CustomerDialog(Utils.GetString("LimitTips"), string.Format(Utils.GetString("attachments_upload_limit_dialog_msg"), 10, 99), MessageBoxButton.OK);
		customerDialog.Owner = Window.GetWindow(this);
		customerDialog.ShowDialog();
	}

	private async void CountdownIntroTest(object sender, RoutedEventArgs e)
	{
		await FeatureInfoWindow.Show(Utils.GetString("CountdownTitle"), Utils.GetString("countdown_guide_desc"), null, "MoreCountdowns" + (Utils.IsCn() ? "_cn" : "_en"), Utils.GetString("EnjoyNow"), delegate
		{
			LocalSettings.Settings.ShowOrHideTabBar("COUNTDOWN", show: true);
			CountdownSyncService.SyncCountdowns();
			App.NavigateCountdown();
		}, isGif: false);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/config/debugsettings.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			((Button)target).Click += PaySuccessClick;
			break;
		case 2:
			((Button)target).Click += PayFailureClick;
			break;
		case 3:
			((Button)target).Click += CalendarClick;
			break;
		case 4:
			((Button)target).Click += LeapClick;
			break;
		case 5:
			ProSwitchBtn = (Button)target;
			ProSwitchBtn.Click += ProClick;
			break;
		case 6:
			((Button)target).Click += MarkdownClick;
			break;
		case 7:
			((Button)target).Click += NumberTestClick;
			break;
		case 8:
			((Button)target).Click += MDClearTest;
			break;
		case 9:
			((Button)target).Click += HintTest;
			break;
		case 10:
			((Button)target).Click += FuncGuideTest;
			break;
		case 11:
			CalendarTest = (TextBlock)target;
			break;
		case 12:
			((Button)target).Click += CountdownIntroTest;
			break;
		case 13:
			PremiumGroup = (Button)target;
			PremiumGroup.Click += UpgradeClick;
			break;
		case 14:
			AIVoiceGroup = (Button)target;
			AIVoiceGroup.Click += AIVoiceClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
