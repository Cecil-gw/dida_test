using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using TickTickDao;
using TickTickUtils;
using ticktick_WPF.Dal;
using ticktick_WPF.Resource;
using ticktick_WPF.Views;
using ticktick_WPF.Views.Upgrade;

namespace ticktick_WPF.Util;

public static class ProChecker
{
	private static bool _dialogShowing;

	private static UpgradeProDialog _calendarProDialog;

	public static async Task ShowUpgradeDialog(ProType type, Window owner = null, string teamId = null)
	{
		await Task.CompletedTask;
		return;
		if (!_dialogShowing)
		{
			if (owner == null)
			{
				owner = App.Window;
			}
			_dialogShowing = true;
			string imageName = SetImage(type);
			UpgradeProDialog upgradeProDialog = new UpgradeProDialog(type, await TryDownloadBackground(imageName), imageName, teamId);
			try
			{
				upgradeProDialog.Owner = owner;
			}
			catch (Exception)
			{
				upgradeProDialog.Owner = null;
			}
			if (upgradeProDialog.Owner == null)
			{
				upgradeProDialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
				upgradeProDialog.Topmost = true;
			}
			if (type == ProType.TimeLine)
			{
				LocalSettings.Settings.ExtraSettings.TLUsed = true;
				LocalSettings.Settings.Save();
			}
			if (type == ProType.CalendarView)
			{
				upgradeProDialog.Show();
				_calendarProDialog = upgradeProDialog;
				owner?.Activate();
			}
			else
			{
				upgradeProDialog.ShowDialog();
			}
			_dialogShowing = false;
		}
	}

	public static void TryCloseCalendarProDialog()
	{
		try
		{
			_calendarProDialog?.Close();
			_calendarProDialog = null;
		}
		catch (Exception)
		{
		}
	}

	public static string SetImage(ProType type)
	{
		string text = type.ToString();
		if (type == ProType.MoreAnniversaries)
		{
			type = ProType.MoreCountdowns;
		}
		bool flag = false;
		switch (type)
		{
		case ProType.CalendarView:
			text = "CalendarProMonth";
			break;
		case ProType.StickyColor:
			text = ProType.PremiumThemes.ToString();
			break;
		case ProType.CalendarWidget:
			text = ProType.CalendarView.ToString();
			break;
		case ProType.MoreListsUnlimited:
			text = ProType.MoreLists.ToString();
			break;
		case ProType.SummaryTemplate:
			text = ProType.SummaryStyle.ToString();
			break;
		case ProType.SummaryStyle:
			text = "SummaryContent";
			break;
		case ProType.TimeLine:
			flag = true;
			break;
		case ProType.ParallelShowInTask:
			text = "TaskAndCalendar";
			break;
		case ProType.MoreCountdowns:
		case ProType.MoreAnniversaries:
			text = ProType.MoreCountdowns.ToString();
			break;
		case ProType.MoreHabits:
			text = "MoreHabits_v2";
			break;
		case ProType.ParallelShowInCalendar:
			text = "CalendarAndTask";
			break;
		case ProType.EmailReminder:
		case ProType.AIAssistant:
			text += "_dida";
			break;
		}
		text += (Utils.IsCn() ? "_cn" : "_en");
		return text + (flag ? ".gif" : ".png");
	}

	public static async Task<bool> TryDownloadBackground(string name)
	{
		return await Task.Run(async () => await TryDownloadBackgroundAsync(name));
	}

	private static async Task<bool> TryDownloadBackgroundAsync(string name)
	{
		if (!Directory.Exists(AppPaths.UpgradeProDir))
		{
			Directory.CreateDirectory(AppPaths.UpgradeProDir);
		}
		string localPath = AppPaths.UpgradeProDir + name;
		if (File.Exists(localPath))
		{
			FileStream fileStream = File.OpenRead(localPath);
			if (fileStream.Length != 0L)
			{
				fileStream.Close();
				return true;
			}
			fileStream.Close();
		}
		string address = "https://" + BaseUrl.GetPullDomain() + "/windows/static/v8/" + name;
		if (Utils.IsNetworkAvailable())
		{
			using (WebClient webClient = new WebClient())
			{
				try
				{
					await webClient.DownloadFileTaskAsync(address, localPath);
					return true;
				}
				catch (Exception)
				{
					IOUtils.DeleteFile(localPath);
					return false;
				}
			}
		}
		return false;
	}

	public static bool CheckPro(ProType type, Window owner = null)
	{
		if (type == ProType.AdvancedSearch)
		{
			return true;
		}
		if (UserDao.IsUserValid())
		{
			return true;
		}
		ShowUpgradeDialog(type, owner);
		return false;
	}

	private static void ShowOverLimitDialog()
	{
		ShowUpgradeDialog(ProType.MoreTasks);
	}

	private static void ShowProLimit(long limit)
	{
		new CustomerDialog(Utils.GetString("LimitTips"), string.Format(Utils.GetString("ProListCountLimit"), limit), MessageBoxButton.OK, Utils.GetToastWindow() as Window).ShowDialog();
	}

	public static async Task<bool> CheckTaskLimit(string projectId, int addCount = 1)
	{
		await Task.CompletedTask;
		return false;
		long limit = Utils.GetUserLimit(Constants.LimitKind.ProjectTaskNumber);
		int num = (await TaskDao.GetUncompletedTasksInProject(projectId)).Count + addCount;
		if (projectId == LocalSettings.Settings.InServerBoxId)
		{
			if (!UserDao.IsPro() && num > 999)
			{
				ShowOverLimitDialog();
				return true;
			}
		}
		else if (num > limit)
		{
			if (UserDao.IsPro())
			{
				ShowProLimit(limit);
				return true;
			}
			ShowOverLimitDialog();
			return true;
		}
		return false;
	}

	public static async Task<bool> CheckTimerLimit(Window owner)
	{
		await Task.CompletedTask;
		return false;
		int num = await TimerDao.GetActiveTimerCountAsync();
		long userLimit = Utils.GetUserLimit(Constants.LimitKind.TimerNumber);
		if (num >= userLimit)
		{
			if (UserDao.IsPro())
			{
				CustomerDialog customerDialog = new CustomerDialog(Utils.GetString("LimitTips"), string.Format(Utils.GetString("ProTimersLimit"), userLimit), MessageBoxButton.OK);
				customerDialog.Owner = owner;
				customerDialog.ShowDialog();
			}
			else
			{
				CheckPro(ProType.MoreTimers, owner);
			}
			return false;
		}
		return true;
	}
}
