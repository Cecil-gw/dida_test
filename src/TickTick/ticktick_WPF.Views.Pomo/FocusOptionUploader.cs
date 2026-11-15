using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TickTickDao;
using TickTickModels;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.Views.Pomo;

public static class FocusOptionUploader
{
	private static readonly SemaphoreLocker UploadLocker = new SemaphoreLocker();

	private static CancelModel _optionCanCel;

	public static async Task AddOption(FocusOption option, DateTime time, bool manual, string completedTaskId = null, string note = null)
	{
		FocusConfig config = TickFocusManager.Config;
		UtilLog.Info("Focus option " + option.ToString() + " oid " + config.Id + " tid " + config.FocusVModel?.FocusId);
		if (string.IsNullOrEmpty(config.Id) || (option == FocusOption.drop && !TickFocusManager.Working))
		{
			return;
		}
		if (!config.NeedUpload)
		{
			SaveLocalFocus();
			return;
		}
		FocusOptionModel focusOptionModel = new FocusOptionModel
		{
			id = Utils.GetGuid(),
			oId = config.Id,
			oType = config.Type,
			op = option.ToString(),
			time = time,
			manual = manual,
			focusOnId = config.FocusVModel?.FocusId,
			focusOnType = (config.FocusVModel?.Type ?? 0),
			focusOnTitle = (config.FocusVModel?.Title ?? string.Empty),
			firstFocusId = config.FirstFocusId,
			autoPomoLeft = config.AutoTimes,
			pomoCount = config.PomoCount,
			duration = ((config.Type == 0) ? (config.Second / 60) : 0)
		};
		if (!string.IsNullOrEmpty(completedTaskId))
		{
			focusOptionModel.focusOnId = completedTaskId;
			focusOptionModel.focusOnType = 0;
			focusOptionModel.focusOnTitle = TaskCache.GetTaskById(completedTaskId)?.Title;
		}
		if (focusOptionModel.focusOnTitle != null && focusOptionModel.focusOnTitle.Length > 64)
		{
			focusOptionModel.focusOnTitle = focusOptionModel.focusOnTitle.Substring(0, 64);
		}
		switch (option)
		{
		case FocusOption.drop:
			focusOptionModel.focusOnId = string.Empty;
			focusOptionModel.focusOnType = 0;
			break;
		case FocusOption.note:
			if (string.IsNullOrEmpty(note))
			{
				return;
			}
			focusOptionModel.note = note;
			break;
		}
		if (string.IsNullOrEmpty(focusOptionModel.firstFocusId))
		{
			focusOptionModel.firstFocusId = focusOptionModel.oId;
		}
		List<FocusOptionModel> list = new List<FocusOptionModel> { focusOptionModel };
		if (option == FocusOption.finish && !string.IsNullOrEmpty(config.Note))
		{
			FocusOptionModel focusOptionModel2 = focusOptionModel.Copy();
			focusOptionModel2.op = FocusOption.note.ToString();
			focusOptionModel2.id = Utils.GetGuid();
			focusOptionModel2.note = config.Note;
			list.Add(focusOptionModel2);
		}
		SaveOption(list, force: false, resetLocalPieces: false, delaySync: true);
	}

	public static async Task SaveOption(List<FocusOptionModel> options, bool force = false, bool resetLocalPieces = false, bool delaySync = false, bool needLog = false)
	{
		if (!UserDao.IsPro())
		{
			return;
		}
		if (options != null)
		{
			foreach (FocusOptionModel option in options)
			{
				await BaseDao<FocusOptionModel>.InsertAsync(option);
			}
		}
		force = force && LocalSettings.Settings.EnableFocus && LocalSettings.Settings.FocusKeepInSync;
		await UploadOptions(force, resetLocalPieces, delaySync, needLog);
	}

	private static async Task UploadOptions(bool force, bool resetLocalPieces, bool delaySync = false, bool needLog = false)
	{
		Func<bool> isCurrentUser = LocalSettings.Settings.CaptureFocusSyncGuard();
		_optionCanCel?.Cancel();
		CancelModel cancelModel = new CancelModel();
		_optionCanCel = cancelModel;
		await UploadLocker.LockAsync(async delegate
		{
			if (isCurrentUser())
			{
				List<FocusOptionModel> options = await BaseDao<FocusOptionModel>.GetAllAsync();
				if (isCurrentUser())
				{
					if (!force)
					{
						if (options == null || options.Count <= 0)
						{
							return;
						}
					}
					long lastPoint = LocalSettings.Settings.FocusPushPoint;
					FocusSyncResultBean focusSyncResultBean = await Communicator.UploadFocusOptions(options);
					if (isCurrentUser())
					{
						if (!string.IsNullOrEmpty(focusSyncResultBean?.errorCode))
						{
							if (focusSyncResultBean.errorCode == "too_busy")
							{
								await Task.Delay(new Random().Next(1000, 2000));
								UploadOptions(force, resetLocalPieces);
							}
							else
							{
								UtilLog.Info("HandleRemoteFocusError " + focusSyncResultBean.errorCode);
								foreach (FocusOptionModel item in options)
								{
									await BaseDao<FocusOptionModel>.DeleteAsync(item);
								}
							}
						}
						else if (LocalSettings.Settings.FocusKeepInSync)
						{
							if (needLog && focusSyncResultBean?.current != null)
							{
								UtilLog.Info(JsonConvert.SerializeObject(focusSyncResultBean.current));
							}
							await HandleRemoteFocus(focusSyncResultBean, options, resetLocalPieces, cancelModel, isCurrentUser, lastPoint != focusSyncResultBean?.point, delaySync);
						}
					}
				}
			}
		});
	}

	private static async Task HandleRemoteFocus(FocusSyncResultBean model, List<FocusOptionModel> options, bool resetLocalPieces, CancelModel optionRandom, Func<bool> isCurrentUser, bool needHandleResult = false, bool delaySync = false)
	{
		if (model != null && model.point > 0)
		{
			if (!LocalSettings.Settings.ObserveSyncedFocusResult(model, isCurrentUser))
			{
				return;
			}
			foreach (FocusOptionModel option in options)
			{
				if (!isCurrentUser())
				{
					return;
				}
				await BaseDao<FocusOptionModel>.DeleteAsync(option);
			}
			if (isCurrentUser() && model.current != null && (needHandleResult || (model.current.status == 0 || model.current.status == 1) != TickFocusManager.Working))
			{
				string text = (TickFocusManager.Config.IsWorking ? TickFocusManager.Config.Id : null);
				string note = TickFocusManager.Config.Note;
				if (!(optionRandom?.IsCanceled() ?? false))
				{
					TickFocusManager.HandleFocusResult(model.current, resetLocalPieces, null, isCurrentUser);
				}
				SaveLocalFocus();
				LocalSettings.Settings.FocusPushPoint = model.point;
				await UpdateFocuses(model.updates?.ToList() ?? new List<FocusSyncStatusModel>(), (model.current.id != text) ? text : null, note, isCurrentUser);
			}
		}
		else
		{
			SaveLocalFocus();
		}
	}

	private static async void SaveLocalFocus()
	{
		Func<bool> isCurrentUser = LocalSettings.Settings.CaptureFocusSyncGuard();
		await Task.Delay(1000);
		if (isCurrentUser())
		{
			LocalSettings.Settings.ExtraSettings.CurrentFocus = TickFocusManager.Config.ToSaveSyncModel();
			LocalSettings.Settings.Save(force: true);
		}
	}

	private static async Task UpdateFocuses(List<FocusSyncStatusModel> focusModels, string localCurrentId, string localNote, Func<bool> isCurrentUser)
	{
		foreach (FocusSyncStatusModel focusModel in focusModels)
		{
			FocusSyncStatusModel focus = focusModel;
			PomodoroModel local = await PomoDao.GetPomoById(focus.id);
			if (!isCurrentUser())
			{
				return;
			}
			List<PomoTask> pomoTasks;
			if (local != null)
			{
				pomoTasks = await PomoDao.GetPomoTasksByPomoId(focus.id);
				if (!focus.valid || (focus.status != 2 && (DateTime.Now - local.EndTime).TotalSeconds > 20.0))
				{
					if (local.SyncStatus == 0)
					{
						await DeletePomoTasks();
						await PomoDao.DeleteAsync(local);
					}
					else
					{
						await PomoService.DeleteById(local.Id);
					}
				}
				else
				{
					if (IsSamePieces(pomoTasks, focus.focusTasks) || focus.status != 2)
					{
						continue;
					}
					await DeletePomoTasks();
					List<PomoTask> newPomoTasks = await GetPomoTasksInFocus(focus);
					foreach (PomoTask task in newPomoTasks.Where((PomoTask pomoTask) => pomoTask.TaskId != null))
					{
						PomodoroSummaryModel pomodoroSummaryModel = await PomoSummaryDao.GetPomoByTaskId(task.TaskId);
						object[] array = pomodoroSummaryModel?.focuses?.FirstOrDefault((object[] f) => f.Length == 3 && f[0] as string == focus.id);
						long totalSecond = Utils.GetTotalSecond(task.StartTime, task.EndTime);
						if (array != null)
						{
							array[2] = (array[2] as long?).GetValueOrDefault() + totalSecond;
						}
						else
						{
							if (pomodoroSummaryModel == null)
							{
								pomodoroSummaryModel = new PomodoroSummaryModel
								{
									id = Utils.GetGuid(),
									userId = LocalSettings.Settings.LoginUserId,
									taskId = task.TaskId
								};
							}
							pomodoroSummaryModel.AddFocuses(new object[3] { focus.id, focus.type, totalSecond });
						}
						await PomoSummaryDao.SavePomoSummary(pomodoroSummaryModel);
					}
					await PomoTaskDao.InsertAllAsync(newPomoTasks);
					DateTime? endTime = focus.endTime;
					if (endTime.HasValue)
					{
						local.EndTime = endTime.Value;
						local.PauseDuration = (int)focus.GetPauseTime(getTotal: true);
						if (Utils.IsEmptyDate(local.StartTime))
						{
							UtilLog.Info("FocusOptionUploader.UpdateFocuses : " + local.Id);
						}
						else
						{
							await PomoDao.UpdatePomo(local);
						}
					}
				}
			}
			else
			{
				if (!(focus.id == localCurrentId) || !focus.valid || focus.status != 2 || !focus.endTime.HasValue)
				{
					continue;
				}
				List<PomoTask> newPomoTasks = await GetPomoTasksInFocus(focus);
				foreach (PomoTask task in newPomoTasks.Where((PomoTask pomoTask) => pomoTask.TaskId != null))
				{
					PomodoroSummaryModel pomodoroSummaryModel2 = await PomoSummaryDao.GetPomoByTaskId(task.TaskId);
					object[] array2 = pomodoroSummaryModel2?.focuses?.FirstOrDefault((object[] f) => f.Length == 3 && f[0] as string == focus.id);
					long totalSecond2 = Utils.GetTotalSecond(task.StartTime, task.EndTime);
					if (array2 != null)
					{
						array2[2] = (array2[2] as long?).GetValueOrDefault() + totalSecond2;
					}
					else
					{
						if (pomodoroSummaryModel2 == null)
						{
							pomodoroSummaryModel2 = new PomodoroSummaryModel
							{
								id = Utils.GetGuid(),
								userId = LocalSettings.Settings.LoginUserId,
								taskId = task.TaskId
							};
						}
						pomodoroSummaryModel2.AddFocuses(new object[3] { focus.id, focus.type, totalSecond2 });
					}
					await PomoSummaryDao.SavePomoSummary(pomodoroSummaryModel2);
				}
				await PomoTaskDao.InsertAllAsync(newPomoTasks);
				await PomoDao.InsertAsync(new PomodoroModel
				{
					Id = focus.id,
					UserId = LocalSettings.Settings.LoginUserId,
					SyncStatus = 0,
					StartTime = focus.startTime,
					EndTime = focus.endTime.Value,
					PauseDuration = (long)focus.GetPauseTime(getTotal: true),
					Type = focus.type,
					Note = localNote,
					Status = 1
				});
			}
			async Task DeletePomoTasks()
			{
				foreach (PomoTask task2 in pomoTasks)
				{
					if (task2.TaskId != null)
					{
						PomodoroSummaryModel pomodoroSummaryModel3 = await PomoSummaryDao.GetPomoByTaskId(task2.TaskId);
						object[] array3 = pomodoroSummaryModel3?.focuses?.FirstOrDefault((object[] f) => f.Length == 3 && f[0] as string == focus.id);
						if (array3 != null)
						{
							pomodoroSummaryModel3.focuses.Remove(array3);
							await PomoSummaryDao.SavePomoSummary(pomodoroSummaryModel3);
						}
					}
					await PomoTaskDao.DeleteAsync(task2);
				}
			}
		}
	}

	private static bool IsSamePieces(List<PomoTask> pomoTasks, FocusTaskModel[] focusTasks)
	{
		if (pomoTasks?.Count != focusTasks?.Length)
		{
			return false;
		}
		if (pomoTasks == null)
		{
			return true;
		}
		foreach (PomoTask pomoTask in pomoTasks)
		{
			FocusTaskModel focusTaskModel = focusTasks.FirstOrDefault((FocusTaskModel f) => EqualTime(f.startTime, pomoTask.StartTime));
			if (focusTaskModel == null)
			{
				return false;
			}
			if (!EqualTime(focusTaskModel.endTime, pomoTask.EndTime))
			{
				return false;
			}
		}
		return true;
		static bool EqualTime(DateTime? a, DateTime? b)
		{
			if (a.HasValue && b.HasValue)
			{
				return Math.Abs((a.Value - b.Value).TotalSeconds) < 5.0;
			}
			return false;
		}
	}

	public static async Task<List<PomoTask>> GetPomoTasksInFocus(FocusSyncStatusModel model)
	{
		List<PomoTask> result = new List<PomoTask>();
		if (model.focusTasks == null || model.focusTasks.Length == 0)
		{
			List<PomoTask> list = await FocusPieceHandler.GetPieceHandleFromSyncModel(model).GetPomoTaskModels(model.GetFocusDuration());
			foreach (PomoTask item in list)
			{
				item.PomoId = model.id;
			}
			return list;
		}
		FocusTaskModel[] focusTasks = model.focusTasks;
		foreach (FocusTaskModel focusTask in focusTasks)
		{
			if (!focusTask.endTime.HasValue)
			{
				continue;
			}
			PomoTask pomoTask = new PomoTask
			{
				StartTime = focusTask.startTime,
				EndTime = focusTask.endTime.Value,
				PomoId = model.id
			};
			if (!focusTask.type.HasValue || focusTask.type == 0)
			{
				pomoTask.TaskId = focusTask.id;
				TaskModel taskModel = await TaskDao.GetTaskById(focusTask.id);
				pomoTask.ProjectName = CacheManager.GetProjectById(taskModel?.projectId)?.name;
				pomoTask.Title = taskModel?.title ?? focusTask.title;
				pomoTask.TagString = taskModel?.tag;
			}
			else if (focusTask.type == 2)
			{
				TimerModel timerModel = await TimerDao.GetTimerById(focusTask.id);
				pomoTask.TimerSid = focusTask.id;
				if (timerModel != null)
				{
					pomoTask.TimerName = timerModel.Name;
					if (!string.IsNullOrEmpty(timerModel.ObjId))
					{
						if (timerModel.ObjType == "task")
						{
							pomoTask.TaskId = timerModel.ObjId;
							TaskModel taskModel2 = await TaskDao.GetTaskById(timerModel.ObjId);
							if (taskModel2 != null)
							{
								pomoTask.ProjectName = CacheManager.GetProjectById(taskModel2?.projectId)?.name;
								pomoTask.Title = taskModel2?.title;
								pomoTask.TagString = TagSerializer.ToJsonContent(pomoTask.Tags?.ToList());
							}
						}
						else if (timerModel.ObjType == "habit")
						{
							pomoTask.HabitId = timerModel.ObjId;
							pomoTask.Title = timerModel.Name;
						}
					}
				}
				else
				{
					pomoTask.TimerName = focusTask.title;
				}
			}
			else
			{
				pomoTask.HabitId = focusTask.id;
				pomoTask.Title = (await HabitDao.GetHabitById(focusTask.id))?.Name ?? focusTask.title;
			}
			result.Add(pomoTask);
		}
		return result;
	}

	private static void SaveFocusStatus()
	{
	}
}
