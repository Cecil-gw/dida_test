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
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Pomo;

public class FocusPieceHandler
{
	private List<FocusPieceModel> _pieceModels = new List<FocusPieceModel>();

	private FocusPieceModel _finalModel;

	private DateTime _startTime;

	public void Reset(DateTime startTime)
	{
		_pieceModels.Clear();
		_startTime = startTime;
		_finalModel = null;
	}

	public void AddPiece(FocusPieceModel model, bool force = false)
	{
		if (force || _finalModel == null || !Utils.IsEqualString(_finalModel.Id, model.Id))
		{
			if (_finalModel != null && !_finalModel.EndTime.HasValue)
			{
				_finalModel.EndTime = model.BeginTime;
			}
			_finalModel = model;
			_pieceModels.Add(model);
		}
	}

	public (FocusPieceModel, bool) GetLastRealPiece()
	{
		for (int num = _pieceModels.Count - 1; num >= 0; num--)
		{
			if (!(_pieceModels[num].Id == "StartPause"))
			{
				TimeSpan timeSpan = ((!_pieceModels[num].EndTime.HasValue || Utils.IsEmptyDate(_pieceModels[num].EndTime)) ? (DateTime.Now - _pieceModels[num].BeginTime) : (_pieceModels[num].EndTime.Value - _pieceModels[num].BeginTime));
				if (string.IsNullOrEmpty(_pieceModels[num].Id) || timeSpan < TimeSpan.FromMinutes(2.0))
				{
					return (_pieceModels[num], timeSpan >= TimeSpan.FromMinutes(2.0));
				}
			}
		}
		return (null, false);
	}

	public async Task ChangePieceTaskId(string oldTaskId, string newTaskId)
	{
		if (oldTaskId == newTaskId)
		{
			return;
		}
		TimerModel timer = await TimerDao.GetTimerByObjId(oldTaskId);
		bool needAdd = true;
		foreach (FocusPieceModel model in _pieceModels)
		{
			if (await model.GetTaskId() == oldTaskId || (timer != null && model.Id == timer.Id))
			{
				model.Id = newTaskId;
				model.Type = 0;
				if (needAdd)
				{
					await FocusOptionUploader.AddOption(FocusOption.focus, model.BeginTime, manual: false, newTaskId);
					needAdd = false;
				}
			}
			else
			{
				needAdd = model.Id != "StartPause";
			}
		}
	}

	public void AddPiece(FocusViewModel focusModel, DateTime? now = null)
	{
		AddPiece(new FocusPieceModel
		{
			BeginTime = (now ?? DateTime.Now),
			Id = focusModel.FocusId,
			Type = focusModel.Type
		});
	}

	public async Task<List<PomoTask>> GetPomoTaskModels(long duration)
	{
		List<PomoTask> pomoTasks = new List<PomoTask>();
		long count = 0L;
		List<FocusPieceModel> list = _pieceModels.ToList();
		foreach (FocusPieceModel piece in list)
		{
			if (!piece.EndTime.HasValue)
			{
				break;
			}
			DateTime start = piece.BeginTime;
			DateTime end = piece.EndTime.Value;
			if (piece.Id == "StartPause")
			{
				pomoTasks.Add(new PomoTask
				{
					TaskId = piece.Id,
					StartTime = start,
					EndTime = end
				});
				continue;
			}
			if (count >= duration)
			{
				break;
			}
			long totalSecond = Utils.GetTotalSecond(start, end);
			count += totalSecond;
			bool overLimit = false;
			if (count > duration)
			{
				end = start.AddSeconds(duration + totalSecond - count);
				overLimit = true;
			}
			TimerModel timer = null;
			if (!string.IsNullOrEmpty(piece.Id))
			{
				timer = await TimerDao.GetTimerByIdOrObjId(piece.Id);
			}
			if (piece.Type == 1 || timer?.ObjType == "habit")
			{
				HabitModel habitModel = await HabitDao.GetHabitById(timer?.ObjId ?? piece.Id);
				pomoTasks.Add(new PomoTask
				{
					HabitId = (habitModel?.Id ?? timer?.ObjId),
					TagString = null,
					StartTime = start,
					EndTime = end,
					ProjectName = null,
					Title = habitModel?.Name,
					TimerSid = timer?.Id,
					TimerName = timer?.Name
				});
			}
			else
			{
				TaskModel task = await TaskDao.GetThinTaskById(timer?.ObjId ?? piece.Id);
				pomoTasks.Add(new PomoTask
				{
					TaskId = (task?.id ?? timer?.ObjId),
					TagString = task?.tag,
					StartTime = start,
					EndTime = end,
					ProjectName = ((task == null) ? null : CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == task.projectId)?.name),
					Title = task?.title,
					TimerSid = timer?.Id,
					TimerName = timer?.Name
				});
			}
			if (!overLimit)
			{
				continue;
			}
			break;
		}
		return pomoTasks;
	}

	public DateTime GetBeginTime()
	{
		if (_startTime == DateTime.MinValue)
		{
			_startTime = _pieceModels.FirstOrDefault()?.BeginTime ?? DateTime.Now;
		}
		return _startTime;
	}

	public void SetRemoteModel(FocusSyncStatusModel model)
	{
		FocusPieceHandler pieceHandleFromSyncModel = GetPieceHandleFromSyncModel(model);
		_startTime = pieceHandleFromSyncModel._startTime;
		_pieceModels = pieceHandleFromSyncModel._pieceModels;
		_finalModel = pieceHandleFromSyncModel._finalModel;
	}

	public static FocusPieceHandler GetPieceHandleFromSyncModel(FocusSyncStatusModel model)
	{
		FocusPieceHandler focusPieceHandler = new FocusPieceHandler();
		focusPieceHandler._startTime = model.startTime;
		FocusLogModel focusLogModel = model.focusOnLogs?.FirstOrDefault((FocusLogModel f) => f.time == model.startTime);
		focusPieceHandler.AddPiece(new FocusPieceModel
		{
			BeginTime = model.startTime,
			Id = (focusLogModel?.id ?? string.Empty),
			Type = (focusLogModel?.type).GetValueOrDefault()
		});
		List<(bool, FocusLogModel)> list = new List<(bool, FocusLogModel)>();
		if (model.focusOnLogs != null)
		{
			list.AddRange(model.focusOnLogs.Select((FocusLogModel log) => (false, log: log)));
		}
		if (model.pauseLogs != null)
		{
			list.AddRange(model.pauseLogs.Select((FocusLogModel log) => (true, log: log)));
		}
		list.Sort(((bool, FocusLogModel) a, (bool, FocusLogModel) b) => a.Item2.time.CompareTo(b.Item2.time));
		bool flag = false;
		string text = focusLogModel?.id ?? string.Empty;
		int type = 0;
		foreach (var (flag2, focusLogModel2) in list)
		{
			if (focusLogModel2.time == model.startTime)
			{
				continue;
			}
			if (!flag2)
			{
				if (flag)
				{
					text = focusLogModel2.id;
					type = focusLogModel2.type.GetValueOrDefault();
					continue;
				}
				if (text != focusLogModel2.id)
				{
					text = focusLogModel2.id;
					type = focusLogModel2.type.GetValueOrDefault();
					focusPieceHandler.AddPiece(new FocusPieceModel
					{
						BeginTime = focusLogModel2.time,
						Id = (text ?? string.Empty),
						Type = focusLogModel2.type.GetValueOrDefault()
					});
				}
			}
			else if (focusLogModel2.type == 1)
			{
				focusPieceHandler.AddPiece(new FocusPieceModel
				{
					BeginTime = focusLogModel2.time,
					Id = (text ?? string.Empty),
					Type = type
				});
			}
			else
			{
				focusPieceHandler.AddPiece(new FocusPieceModel
				{
					BeginTime = focusLogModel2.time,
					Id = "StartPause"
				});
			}
			flag = flag2 && focusLogModel2.type == 0;
		}
		if (model.status == 2)
		{
			focusPieceHandler.AddPiece(new FocusPieceModel
			{
				BeginTime = (model.endTime ?? DateTime.Now)
			}, force: true);
		}
		HandleShortPieces(focusPieceHandler);
		return focusPieceHandler;
	}

	private static void HandleShortPieces(FocusPieceHandler pieceHandle)
	{
		List<FocusPieceModel> list = new List<FocusPieceModel>();
		long num = 0L;
		string text = string.Empty;
		foreach (FocusPieceModel item in pieceHandle._pieceModels.Where((FocusPieceModel piece) => piece.Id != "StartPause"))
		{
			if (text != (item.Id ?? string.Empty))
			{
				if (num < 120 && !string.IsNullOrEmpty(item.Id))
				{
					foreach (FocusPieceModel item2 in list)
					{
						item2.Id = item.Id;
						item2.Type = item.Type;
					}
				}
				else
				{
					list.Clear();
					num = 0L;
				}
				list.Add(item);
				num += item.GetDuration();
				text = item.Id;
			}
			else
			{
				list.Add(item);
				num += item.GetDuration();
			}
		}
	}

	public List<(DateTime, DateTime)> GetPauseSpans()
	{
		List<(DateTime, DateTime)> list = new List<(DateTime, DateTime)>();
		foreach (FocusPieceModel item2 in _pieceModels.ToList())
		{
			DateTime beginTime = item2.BeginTime;
			DateTime item = item2.EndTime ?? DateTime.Now;
			if (item2.Id == "StartPause")
			{
				list.Add((beginTime, item));
			}
		}
		return list;
	}

	public List<FocusPieceModel> GetPieces()
	{
		return _pieceModels.ToList();
	}

	public void PrintLog()
	{
		try
		{
			string text = "FocusPieceLog " + JsonConvert.SerializeObject(_pieceModels);
			UtilLog.Info(text);
			UserActCollectUtils.SendException(new Exception(text), ExceptionType.Custom);
		}
		catch (Exception)
		{
		}
	}
}
