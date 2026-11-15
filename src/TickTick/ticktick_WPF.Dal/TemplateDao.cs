using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Config;
using ticktick_WPF.Views.MainListView.DetailView;

namespace ticktick_WPF.Dal;

public static class TemplateDao
{
	public static async Task<bool> MergeTemplates(List<TaskTemplateModel> taskTemplates, List<TaskTemplateModel> noteTemplates, List<TaskTemplateModel> localTemplate)
	{
		if (taskTemplates == null && noteTemplates == null)
		{
			return false;
		}
		List<TaskTemplateModel> remoteTemplate = new List<TaskTemplateModel>();
		if (taskTemplates != null && taskTemplates.Count > 0)
		{
			foreach (TaskTemplateModel taskTemplate in taskTemplates)
			{
				taskTemplate.Kind = TemplateKind.Task.ToString();
				remoteTemplate.Add(taskTemplate);
			}
		}
		if (noteTemplates != null && noteTemplates.Count > 0)
		{
			foreach (TaskTemplateModel noteTemplate in noteTemplates)
			{
				noteTemplate.Kind = TemplateKind.Note.ToString();
				remoteTemplate.Add(noteTemplate);
			}
		}
		List<SyncStatusModel> localStatus = await (from m in App.Connection.Table<SyncStatusModel>()
			where m.Type >= 13 && m.Type <= 15 && m.UserId == LocalSettings.Settings.LoginUserId
			select m).ToListAsync();
		if (localTemplate != null)
		{
			List<TaskTemplateModel> updates = new List<TaskTemplateModel>();
			List<TaskTemplateModel> add = new List<TaskTemplateModel>();
			List<TaskTemplateModel> deleted = new List<TaskTemplateModel>(localTemplate);
			deleted.RemoveAll((TaskTemplateModel m) => (from s in localStatus
				where s.Type == 13
				select s.EntityId).Contains(m.Id));
			foreach (TaskTemplateModel item in remoteTemplate)
			{
				bool flag = true;
				item.UserId = LocalSettings.Settings.LoginUserId;
				foreach (TaskTemplateModel local in localTemplate)
				{
					if (local.Id == item.Id)
					{
						deleted.Remove(local);
						flag = false;
						if (!localStatus.Any((SyncStatusModel status) => status.EntityId == local.Id) && ShouldUpdateFromRemote(local, item))
						{
							item._Id = local._Id;
							updates.Add(item);
						}
						break;
					}
				}
				if (flag && !(from s in localStatus
					where s.Type == 15
					select s.EntityId).Contains(item.Id))
				{
					add.Add(item);
				}
			}
			await App.Connection.UpdateAllAsync(updates);
			await App.Connection.InsertAllAsync(add);
			foreach (TaskTemplateModel item2 in deleted)
			{
				await App.Connection.DeleteAsync(item2);
			}
			return updates.Count > 0 || add.Count > 0 || deleted.Count > 0;
		}
		await App.Connection.InsertAllAsync(taskTemplates);
		return remoteTemplate.Count > 0;
	}

	public static async void PullTemplates()
	{
		List<TaskTemplateModel> localTemplate = await GetLocalTemplate();
		TemplatesModel templatesModel = await Communicator.PullTemplates();
		if (templatesModel != null)
		{
			MergeTemplates(templatesModel.taskTemplates, templatesModel.noteTemplates, localTemplate);
		}
	}

	public static async Task AddTemplates(List<TaskTemplateModel> templates, bool isNote = false)
	{
		if (templates != null && templates.Count != 0)
		{
			List<TaskTemplateModel> reorderedTemplates = await PrepareOrdersForNewTemplates(templates, isNote);
			await App.Connection.InsertAllAsync(templates);
			TryPushTemplate(templates, reorderedTemplates, isNote);
		}
	}

	private static async void TryPushTemplate(List<TaskTemplateModel> addedTemplates, List<TaskTemplateModel> updatedTemplates, bool isNote)
	{
		TaskTemplateSyncBean taskTemplateSyncBean = new TaskTemplateSyncBean();
		taskTemplateSyncBean.Add.AddRange(addedTemplates);
		taskTemplateSyncBean.Update.AddRange(updatedTemplates);
		BatchUpdateResult result = await Communicator.PushTemplates(taskTemplateSyncBean, isNote);
		BatchUpdateResult batchUpdateResult = result;
		if (batchUpdateResult != null && batchUpdateResult.Id2etag?.Count > 0)
		{
			await SaveCommitResultBackToDb(result.Id2etag);
		}
		BatchUpdateResult batchUpdateResult2 = result;
		if (batchUpdateResult2 == null || !(batchUpdateResult2.Id2error?.Keys.Count > 0))
		{
			return;
		}
		foreach (TaskTemplateModel item in addedTemplates.Where((TaskTemplateModel t) => result.Id2error.Keys.Contains(t.Id)))
		{
			await SaveTemplateSyncStatus(item.Id, 13);
		}
		foreach (TaskTemplateModel item2 in updatedTemplates.Where((TaskTemplateModel t) => result.Id2error.Keys.Contains(t.Id)))
		{
			await SaveTemplateSyncStatus(item2.Id, 14);
		}
		SyncManager.Sync();
	}

	private static async Task<List<TaskTemplateModel>> PrepareOrdersForNewTemplates(List<TaskTemplateModel> templates, bool isNote)
	{
		List<TaskTemplateModel> source = await GetLocalTemplate();
		List<TaskTemplateModel> reorderedTemplates = new List<TaskTemplateModel>();
		foreach (IGrouping<string, TaskTemplateModel> item in from template in templates
			group template by NormalizeTemplateKind(template, isNote))
		{
			string kind = item.Key;
			List<TaskTemplateModel> list = item.ToList();
			list.ForEach(delegate(TaskTemplateModel template)
			{
				template.Kind = kind;
				template.UserId = LocalSettings.Settings.LoginUserId;
			});
			List<TaskTemplateModel> list2 = (from template in source
				where NormalizeTemplateKind(template, isNote: false) == kind
				orderby 0
				select template).ToList();
			list2.Sort(TemplateOrderHelper.Compare);
			List<TaskTemplateModel> list3 = new List<TaskTemplateModel>();
			list3.AddRange(list);
			list3.AddRange(list2);
			for (int num = 0; num < list3.Count; num++)
			{
				long num2 = (long)num * 268435456L;
				if (list3[num].Order != num2)
				{
					list3[num].Order = num2;
					if (!list.Contains(list3[num]))
					{
						reorderedTemplates.Add(list3[num]);
					}
				}
			}
		}
		if (reorderedTemplates.Count > 0)
		{
			await App.Connection.UpdateAllAsync(reorderedTemplates);
		}
		return reorderedTemplates;
	}

	private static string NormalizeTemplateKind(TaskTemplateModel template, bool isNote)
	{
		if (!string.IsNullOrEmpty(template.Kind))
		{
			return template.Kind;
		}
		if (!isNote)
		{
			return TemplateKind.Task.ToString();
		}
		return TemplateKind.Note.ToString();
	}

	private static bool ShouldUpdateFromRemote(TaskTemplateModel local, TaskTemplateModel remote)
	{
		if (!(local.Etag != remote.Etag))
		{
			return local.Order != remote.Order;
		}
		return true;
	}

	public static async Task SaveCommitResultBackToDb(Dictionary<string, string> id2Etag)
	{
		if (id2Etag == null || id2Etag.Count == 0)
		{
			return;
		}
		string userId = Utils.GetCurrentUserIdInt().ToString();
		HashSet<string> templateIds = new HashSet<string>(id2Etag.Keys);
		List<TaskTemplateModel> list = await (from model in App.Connection.Table<TaskTemplateModel>()
			where model.UserId == userId && templateIds.Contains(model.Id)
			select model).ToListAsync();
		foreach (TaskTemplateModel item in list)
		{
			if (id2Etag.TryGetValue(item.Id, out var value))
			{
				item.Etag = value;
			}
		}
		if (list.Count > 0)
		{
			await App.Connection.UpdateAllAsync(list);
		}
	}

	private static async Task SaveTemplateSyncStatus(string entityId, int syncType)
	{
		SyncStatusModel status = new SyncStatusModel
		{
			EntityId = entityId,
			Type = syncType,
			UserId = LocalSettings.Settings.LoginUserId,
			ModifyPoint = DateTime.Now.Ticks
		};
		SyncStatusModel syncStatusModel = await (from m in App.Connection.Table<SyncStatusModel>()
			where m.EntityId == status.EntityId && m.UserId == status.UserId
			select m).FirstOrDefaultAsync();
		if (syncStatusModel != null)
		{
			syncStatusModel.ModifyPoint = status.ModifyPoint;
			syncStatusModel.Type = status.Type;
			await App.Connection.UpdateAsync(syncStatusModel);
		}
		else
		{
			await App.Connection.InsertAsync(status);
		}
	}

	public static async Task<List<TaskTemplateModel>> GetLocalTemplate()
	{
		string sql = " select * from TaskTemplateModel where UserId = '" + LocalSettings.Settings.LoginUserId + "' order by CreatedTime desc ";
		object obj = await App.Connection.QueryAsync<TaskTemplateModel>(sql, Array.Empty<object>());
		((List<TaskTemplateModel>)obj)?.ForEach((Action<TaskTemplateModel>)delegate(TaskTemplateModel m)
		{
			if (m.ItemsToString != null)
			{
				m.Items = JsonConvert.DeserializeObject<List<string>>(m.ItemsToString);
			}
			if (m.TagsToString != null)
			{
				m.Tags = JsonConvert.DeserializeObject<List<string>>(m.TagsToString);
			}
			if (m.ChidlrenToString != null)
			{
				m.Chidlren = JsonConvert.DeserializeObject<List<TaskTemplateModel>>(m.ChidlrenToString);
			}
		});
		if (obj == null)
		{
			obj = new List<TaskTemplateModel>();
		}
		return (List<TaskTemplateModel>)obj;
	}

	public static async void UpdateTemplate(TaskTemplateModel model)
	{
		App.Connection.UpdateAsync(model);
		BatchUpdateResult result = await Communicator.PushTemplates(new TaskTemplateSyncBean
		{
			Update = { model }
		}, model.Kind == TemplateKind.Note.ToString());
		if (result != null && result.Id2etag?.Count > 0)
		{
			await SaveCommitResultBackToDb(result.Id2etag);
		}
		if (result != null && result.Id2error?.Keys.Count > 0)
		{
			SyncStatusModel status = new SyncStatusModel
			{
				EntityId = model.Id,
				Type = 14,
				UserId = LocalSettings.Settings.LoginUserId,
				ModifyPoint = DateTime.Now.Ticks
			};
			SyncStatusModel syncStatusModel = await (from m in App.Connection.Table<SyncStatusModel>()
				where m.EntityId == status.EntityId && m.UserId == status.UserId
				select m).FirstOrDefaultAsync();
			if (syncStatusModel == null)
			{
				await App.Connection.InsertAsync(status);
			}
			else
			{
				syncStatusModel.ModifyPoint = status.ModifyPoint;
				await App.Connection.UpdateAsync(syncStatusModel);
			}
			SyncManager.Sync();
		}
	}

	public static async void UpdateTemplates(List<TaskTemplateModel> models)
	{
		if (models == null || models.Count == 0)
		{
			return;
		}
		await App.Connection.UpdateAllAsync(models);
		bool shouldSync = false;
		foreach (IGrouping<bool, TaskTemplateModel> group in from model in models
			group model by NormalizeTemplateKind(model, isNote: false) == TemplateKind.Note.ToString())
		{
			TaskTemplateSyncBean taskTemplateSyncBean = new TaskTemplateSyncBean();
			taskTemplateSyncBean.Update.AddRange(group);
			BatchUpdateResult result = await Communicator.PushTemplates(taskTemplateSyncBean, group.Key);
			BatchUpdateResult batchUpdateResult = result;
			if (batchUpdateResult != null && batchUpdateResult.Id2etag?.Count > 0)
			{
				await SaveCommitResultBackToDb(result.Id2etag);
			}
			BatchUpdateResult batchUpdateResult2 = result;
			if (batchUpdateResult2 == null || !(batchUpdateResult2.Id2error?.Keys.Count > 0))
			{
				continue;
			}
			foreach (TaskTemplateModel item in group.Where((TaskTemplateModel template) => result.Id2error.Keys.Contains(template.Id)))
			{
				await SaveTemplateSyncStatus(item.Id, 14);
			}
			shouldSync = true;
		}
		if (shouldSync)
		{
			SyncManager.Sync();
		}
	}

	public static async void DeleteTemplate(TaskTemplateModel model)
	{
		if (model == null)
		{
			return;
		}
		App.Connection.DeleteAsync(model);
		BatchUpdateResult obj = await Communicator.PushTemplates(new TaskTemplateSyncBean
		{
			Delete = { model.Id }
		}, model.Kind == TemplateKind.Note.ToString());
		if (obj != null && obj.Id2error?.Keys.Count > 0)
		{
			SyncStatusModel status = new SyncStatusModel
			{
				EntityId = model.Id,
				Type = 15,
				UserId = LocalSettings.Settings.LoginUserId,
				ModifyPoint = DateTime.Now.Ticks
			};
			SyncStatusModel syncStatusModel = await (from m in App.Connection.Table<SyncStatusModel>()
				where m.EntityId == status.EntityId && m.UserId == status.UserId
				select m).FirstOrDefaultAsync();
			if (syncStatusModel == null)
			{
				await App.Connection.InsertAsync(status);
			}
			else if (syncStatusModel.Type != 14)
			{
				await App.Connection.DeleteAsync(syncStatusModel);
			}
			else
			{
				syncStatusModel.ModifyPoint = status.ModifyPoint;
				syncStatusModel.Type = status.Type;
				await App.Connection.UpdateAsync(syncStatusModel);
			}
			SyncManager.Sync();
		}
	}

	public static async Task<TaskTemplateModel> GetTemplateById(string templateId)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		TaskTemplateModel taskTemplateModel = await (from model in App.Connection.Table<TaskTemplateModel>()
			where model.Id == templateId && model.UserId == userId
			select model).FirstOrDefaultAsync();
		if (taskTemplateModel != null)
		{
			if (taskTemplateModel.ItemsToString != null)
			{
				taskTemplateModel.Items = JsonConvert.DeserializeObject<List<string>>(taskTemplateModel.ItemsToString);
			}
			if (taskTemplateModel.TagsToString != null)
			{
				taskTemplateModel.Tags = JsonConvert.DeserializeObject<List<string>>(taskTemplateModel.TagsToString);
			}
			if (taskTemplateModel.ChidlrenToString != null)
			{
				taskTemplateModel.Chidlren = JsonConvert.DeserializeObject<List<TaskTemplateModel>>(taskTemplateModel.ChidlrenToString);
			}
		}
		return taskTemplateModel;
	}

	public static async Task SaveTemplate(string taskId, TaskDetailView control)
	{
		TaskTemplateModel newTemp = await TaskTemplateModel.Build(taskId);
		if (newTemp == null)
		{
			control.TryToast(Utils.GetString("IsEmptyTask"));
			return;
		}
		List<TaskTemplateModel> localTemplates = await GetLocalTemplate();
		List<string> exsitTitles = (from t in localTemplates?.Where((TaskTemplateModel t) => t.IsNote == newTemp.IsNote)
			select t.Title).ToList();
		EditTemplateWindow saveWindow = new EditTemplateWindow(newTemp.Title, exsitTitles, isAdd: true, Utils.GetString(newTemp.IsNote ? "AddNoteTemplateText" : "AddTaskTemplateText"))
		{
			Title = Utils.GetString("SaveAsTemplate"),
			Owner = Window.GetWindow(control)
		};
		saveWindow.Replace += delegate(object obj, string title)
		{
			newTemp.Title = title;
			ReplaceTemplates(newTemp, localTemplates);
			control.TryToast(Utils.GetString("SaveTemplateSuccess"));
		};
		saveWindow.Save += delegate(object obj, string title)
		{
			newTemp.Title = title;
			AddTemplates(new List<TaskTemplateModel> { newTemp }, newTemp.IsNote);
			control.TryToast(Utils.GetString("SaveTemplateSuccess"));
		};
		saveWindow.Closed += delegate
		{
			saveWindow.Owner?.Activate();
		};
		saveWindow.ShowDialog();
	}

	private static void ReplaceTemplates(TaskTemplateModel newTemp, List<TaskTemplateModel> localTemplates)
	{
		TaskTemplateModel taskTemplateModel = localTemplates.FirstOrDefault((TaskTemplateModel temp) => temp.Kind == newTemp.Kind && temp.Title == newTemp.Title);
		if (taskTemplateModel != null)
		{
			newTemp._Id = taskTemplateModel._Id;
			newTemp.Id = taskTemplateModel.Id;
			newTemp.CreatedTime = taskTemplateModel.CreatedTime;
			newTemp.Etag = taskTemplateModel.Etag;
			UpdateTemplate(newTemp);
		}
		else
		{
			AddTemplates(new List<TaskTemplateModel> { newTemp }, newTemp.IsNote);
		}
	}
}
