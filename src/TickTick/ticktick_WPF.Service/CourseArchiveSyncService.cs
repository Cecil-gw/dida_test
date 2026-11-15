using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TickTickDao;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Service;

public class CourseArchiveSyncService
{
	public static void PullArchivedModels()
	{
		Task.Run(delegate
		{
			PullArchivedModelsAsync();
		});
	}

	public static async Task PullArchivedModelsAsync()
	{
		List<CourseArchiveSyncModel> archiveModels = await Communicator.GetArchivedCourses();
		List<ArchivedEventModel> obj = await ArchivedDao.GetArchivedModels(ArchiveKind.Course);
		Dictionary<string, ArchivedEventModel> localDict = new Dictionary<string, ArchivedEventModel>();
		List<ArchivedEventModel> added = new List<ArchivedEventModel>();
		foreach (ArchivedEventModel item in obj)
		{
			if (string.IsNullOrEmpty(item.Key) || localDict.ContainsKey(item.Key))
			{
				BaseDao<ArchivedEventModel>.DeleteAsync(item);
			}
			else
			{
				localDict[item.Key] = item;
			}
		}
		if (archiveModels == null)
		{
			return;
		}
		foreach (CourseArchiveSyncModel item2 in archiveModels)
		{
			if (!string.IsNullOrEmpty(item2.courseId))
			{
				if (localDict.ContainsKey(item2.courseId))
				{
					localDict.Remove(item2.courseId);
					continue;
				}
				added.Add(new ArchivedEventModel
				{
					UserId = LocalSettings.Settings.LoginUserId,
					Key = item2.courseId,
					Kind = 1,
					SyncStatus = 2
				});
			}
		}
		foreach (ArchivedEventModel item3 in localDict.Values.ToList())
		{
			BaseDao<ArchivedEventModel>.DeleteAsync(item3);
		}
		await BaseDao<ArchivedEventModel>.InsertAllAsync(added);
		if (localDict.Count > 0 || added.Count > 0)
		{
			DataChangedNotifier.NotifyCalendarChanged(200);
		}
	}

	public static async Task PushLocalArchivedModels()
	{
		List<ArchivedEventModel> locals = await ArchivedDao.GetArchivedModels(ArchiveKind.Course, needSync: true);
		CourseArchivePostModel courseArchivePostModel = new CourseArchivePostModel();
		foreach (ArchivedEventModel item in locals.Where((ArchivedEventModel local) => !string.IsNullOrEmpty(local.Key)))
		{
			switch (item.SyncStatus)
			{
			case 0:
			{
				string[] array = item.Key?.Split('_');
				string dateStamp = ((array != null && array.Length > 1) ? array[1] : "");
				courseArchivePostModel.add.Add(new CourseArchiveSyncModel
				{
					courseId = item.Key,
					dateStamp = dateStamp
				});
				break;
			}
			case -1:
				courseArchivePostModel.delete.Add(item.Key);
				break;
			}
		}
		if (courseArchivePostModel.Empty || !(await Communicator.PostArchivedCourses(courseArchivePostModel)))
		{
			return;
		}
		foreach (ArchivedEventModel item2 in locals.Where((ArchivedEventModel local) => !string.IsNullOrEmpty(local.Key)))
		{
			switch (item2.SyncStatus)
			{
			case 0:
				item2.SyncStatus = 2;
				await BaseDao<ArchivedEventModel>.UpdateAsync(item2);
				break;
			case -1:
				await BaseDao<ArchivedEventModel>.DeleteAsync(item2);
				break;
			}
		}
	}
}
