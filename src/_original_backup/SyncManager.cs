using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.Util.Sync.Model;
using ticktick_WPF.Views.Pomo;

namespace ticktick_WPF.Util;

public class SyncManager
{
	private static readonly SyncService SyncService;

	private static string _delayUid;

	static SyncManager()
	{
		_delayUid = Utils.GetGuid();
		SyncService = new SyncService();
		SyncService.Init();
		SyncService.SyncFinished -= OnSyncFinished;
		SyncService.SyncFinished += OnSyncFinished;
		NetworkChange.NetworkAvailabilityChanged -= OnNetWorkChanged;
		NetworkChange.NetworkAvailabilityChanged += OnNetWorkChanged;
	}

	private static void OnNetWorkChanged(object sender, NetworkAvailabilityEventArgs e)
	{
		FocusOptionUploader.SaveOption(null, force: true, resetLocalPieces: true);
	}

	private static void OnSyncFinished(object sender, SyncResult e)
	{
		ThreadUtil.DetachedRunOnUiThread(delegate
		{
			MainWindowManager.HandleSyncFinished(e).ContinueWith(UtilRun.LogTask);
		});
	}

	public static async Task PullProjectTasks(string projectId)
	{
		await SyncService.PullProjectTasks(projectId);
	}

	public static async Task Sync(int type = 0)
	{
		if (string.IsNullOrEmpty(LocalSettings.Settings.LoginUserId))
		{
			return;
		}
		if (type == 0)
		{
			string lastCheckSyncDate = LocalSettings.Settings.LastCheckSyncDate;
			if (DateTime.Today.ToString("yyyyMMdd", App.Ci) != lastCheckSyncDate)
			{
				type = 1;
			}
		}
		if (type == 1)
		{
			LocalSettings.Settings.LastCheckSyncDate = DateTime.Today.ToString("yyyyMMdd", App.Ci);
		}
		MainWindowManager.BeginSyncStory();
		await SyncService.DoSync(type);
	}

	public static void TryDelaySync(int delay = 4000)
	{
		DelayActionHandlerCenter.TryDoAction(_delayUid, delegate
		{
			Sync();
		}, delay);
	}

	public static void Clear()
	{
		SyncService.Clear();
	}

	public static async void Init()
	{
		SyncService.Init();
		await Task.Delay(1000);
		Sync(1);
		WebSocketService.InitAsync().ContinueWith(UtilRun.LogTask);
	}

	private static async void LogUnSyncData()
	{
		StringBuilder builder = new StringBuilder();
		bool project = await ProjectDao.CheckUnSyncProjects();
		bool projectGroup = await ProjectGroupDao.CheckUnSyncItem();
		bool sync = await SyncStatusDao.CheckUnSyncItem();
		string value = $"\nproject sync {project}, group sync {projectGroup}, task sync {sync}\n";
		builder.Append(value);
		if (!project)
		{
			Dictionary<string, string> dictionary = await ProjectDao.GetNeedPostProject();
			builder.Append("------------------\n");
			builder.Append("project sync failed\n");
			if (dictionary != null && dictionary.Count > 0)
			{
				foreach (KeyValuePair<string, string> p in dictionary)
				{
					ProjectModel projectModel = await ProjectDao.GetProjectById(p.Key);
					builder.Append("project_id:" + p.Key + ", project_name:" + projectModel?.name + ", status:" + p.Value + "\n");
				}
			}
		}
		if (!projectGroup)
		{
			builder.Append("------------------\n");
			builder.Append("group sync failed\n");
			List<ProjectGroupModel> list = await ProjectGroupDao.GetNeedPostProjectGroup();
			if (list != null && list.Count > 0)
			{
				foreach (ProjectGroupModel item in list)
				{
					builder.Append("group_id:" + item.id + ", group_name:" + item.name + ", status:" + item.sync_status + "\n");
				}
			}
		}
		if (sync)
		{
			return;
		}
		builder.Append("------------------\n");
		builder.Append("task sync failed\n");
		List<SyncStatusModel> list2 = await SyncStatusDao.GetAllSyncStatus();
		if (list2 == null || list2.Count <= 0)
		{
			return;
		}
		foreach (SyncStatusModel model in list2)
		{
			TaskModel taskModel = await TaskDao.GetThinTaskById(model.EntityId);
			string arg = ((taskModel == null) ? "null" : taskModel.title);
			builder.Append($"task_id:{model.EntityId}, task_name:{arg}, status:{model.Type}\n");
		}
	}
}
