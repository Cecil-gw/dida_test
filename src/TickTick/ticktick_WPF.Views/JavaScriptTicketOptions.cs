using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views;

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
[ComDefaultInterface(typeof(IJavaScriptTicketOptions))]
public sealed class JavaScriptTicketOptions : IJavaScriptTicketOptions
{
	private NavigateWebBrowserWindow _instance;

	public JavaScriptTicketOptions(NavigateWebBrowserWindow instance)
	{
		_instance = instance ?? throw new ArgumentNullException("instance");
	}

	public void toastForTicketWithMessage(string message)
	{
		NavigateWebBrowserWindow instance = _instance;
		if (instance == null)
		{
			return;
		}
		if (!instance.Dispatcher.CheckAccess())
		{
			instance.Dispatcher.BeginInvoke((Action)delegate
			{
				toastForTicketWithMessage(message);
			});
			return;
		}
		instance.RecordBridgeCallback("toast");
		if (instance.ToastAction != null)
		{
			instance.ToastAction(message);
		}
		else
		{
			Utils.Toast(message);
		}
	}

	public void uploadLogsForTicketWithID(string ticketId, string tt)
	{
		NavigateWebBrowserWindow instance = _instance;
		if (instance != null)
		{
			instance.RecordBridgeCallback("upload_logs");
			Communicator.UploadLogs(TicketLogUtils.GetRecentLogs(), ticketId, UserManager.GetToken());
		}
	}

	public string getXDeviceInfoForTicket()
	{
		NavigateWebBrowserWindow instance = _instance;
		if (instance == null)
		{
			return null;
		}
		instance.RecordBridgeCallback("device_info");
		return Utils.GetDeviceInfo();
	}

	public void setDefaultMatrixRule(string ruleType)
	{
		NavigateWebBrowserWindow instance = _instance;
		instance?.RecordBridgeCallback("matrix_rule");
		instance?.DoAction(needClose: true, ruleType);
	}

	internal void Dispose()
	{
		_instance = null;
	}

	public void navigateUrl(string url)
	{
		NavigateWebBrowserWindow instance = _instance;
		if (instance != null)
		{
			instance.RecordBridgeCallback("navigate_url");
			Utils.TryProcessStartUrl(url);
		}
	}

	public void navigateUrlWithToken(string url)
	{
		NavigateWebBrowserWindow instance = _instance;
		if (instance != null)
		{
			instance.RecordBridgeCallback("navigate_url_with_token");
			Utils.TryProcessStartUrlWithToken(url);
		}
	}

	public void submitHolidayCalendar(string payload)
	{
		NavigateWebBrowserWindow instance = _instance;
		instance?.RecordBridgeCallback("holiday_calendar");
		instance?.DoAction(needClose: true, payload);
	}

	public async void applyProjectTemplate(string projectTemplateId)
	{
		NavigateWebBrowserWindow instance = _instance;
		if (instance == null)
		{
			return;
		}
		if (!instance.Dispatcher.CheckAccess())
		{
			instance.Dispatcher.BeginInvoke((Action)delegate
			{
				applyProjectTemplate(projectTemplateId);
			});
			return;
		}
		instance.RecordBridgeCallback("project_template");
		if (!(await CheckProjectOverLimit(instance)))
		{
			bool num = await Communicator.ApplyProjectTemplate(projectTemplateId);
			if (num)
			{
				UserActCollectUtils.AddClickEvent("share_template", "template_landing_page", "save_succeed");
				closePage();
				SyncManager.Sync();
			}
			Utils.Toast(Utils.GetString(num ? "SaveSuccess" : "SaveFailed"));
		}
	}

	public void closePage()
	{
		NavigateWebBrowserWindow instance = _instance;
		if (instance != null)
		{
			if (!instance.Dispatcher.CheckAccess())
			{
				instance.Dispatcher.BeginInvoke(new Action(closePage));
				return;
			}
			instance.RecordBridgeCallback("close_page");
			instance.Close();
		}
	}

	private static async Task<bool> CheckProjectOverLimit(Window owner)
	{
		long projectLimitNumber = Utils.GetUserLimit(Constants.LimitKind.ProjectNumber);
		List<string> teamIds = (from t in CacheManager.GetTeams()
			select t.id).ToList();
		List<ProjectModel> source = (UserDao.IsPro() ? (await ProjectDao.GetAllProjects(withInbox: false, withClosed: false)).Where((ProjectModel p) => !TeamDao.IsTeamExpired(p.teamId)).ToList() : (await ProjectDao.GetAllProjects(withInbox: false)).ToList());
		int count = source.Where((ProjectModel p) => string.IsNullOrEmpty(p.teamId) || teamIds.Contains(p.teamId)).ToList().Count;
		if (count < projectLimitNumber)
		{
			UtilLog.Info($"ApplyProjectTemplateLimit : {projectLimitNumber}, current {count}");
			return false;
		}
		if (UserDao.IsPro())
		{
			string content = string.Format(Utils.GetString("ListsLimitAdd"), count);
			CustomerDialog customerDialog = new CustomerDialog(Utils.GetString("ProjectLimitTips"), content, MessageBoxButton.OK);
			customerDialog.Owner = owner;
			customerDialog.ShowDialog();
			return true;
		}
		await ProChecker.ShowUpgradeDialog(ProType.MoreLists, owner);
		return true;
	}
}
