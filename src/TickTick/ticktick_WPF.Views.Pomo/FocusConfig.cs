using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Documents;
using Newtonsoft.Json;
using TickTickDao;
using TickTickModels;
using TickTickUtils;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Sync;

namespace ticktick_WPF.Views.Pomo;

public class FocusConfig
{
	private const int MinToSecFactor = 60;

	public DateTime LastPomoCompleteTime;

	private readonly SemaphoreLocker _harvestTaskBindLock = new SemaphoreLocker();

	private int _harvestTaskBindVersion;

	public int PomoCount;

	public long Second = 60L;

	private PomoStatus _previewStatus;

	private PomoStatus _status = PomoStatus.WaitingWork;

	private readonly Action _statusChange;

	private double _currentSeconds;

	private readonly Action _secondChange;

	public DateTime PauseTime;

	public DateTime? AutoStopTime;

	private int? _pomoDuration;

	private static bool AutoBreak => LocalSettings.Settings.AutoBreak;

	private static bool AutoNextPomo => LocalSettings.Settings.AutoNextPomo;

	public bool IsWorking
	{
		get
		{
			if (Status != PomoStatus.Working)
			{
				return Status == PomoStatus.Pause;
			}
			return true;
		}
	}

	public bool InRelax
	{
		get
		{
			if (Status != PomoStatus.WaitingRelax)
			{
				return Status == PomoStatus.Relaxing;
			}
			return true;
		}
	}

	public string Id { get; set; }

	public string LastEditDurationId { get; set; }

	public int Type { get; set; }

	public FocusViewModel FocusVModel { get; set; } = new FocusViewModel();

	public FocusPieceHandler PieceHandler { get; set; } = new FocusPieceHandler();

	public string FirstFocusId { get; set; }

	public DateTime StartTime { get; set; }

	public double PauseDuration { get; set; }

	public string Note { get; set; }

	public int AutoTimes { get; set; }

	public PomoStatus Status
	{
		get
		{
			return _status;
		}
		set
		{
			_previewStatus = _status;
			if (_status != value)
			{
				_status = value;
				_statusChange();
				OnStatusChanged();
			}
		}
	}

	public double CurrentSeconds
	{
		get
		{
			return _currentSeconds;
		}
		set
		{
			_currentSeconds = value;
			_secondChange();
		}
	}

	public bool FromRelax
	{
		get
		{
			if (_previewStatus != PomoStatus.Relaxing)
			{
				return _previewStatus == PomoStatus.WaitingRelax;
			}
			return true;
		}
	}

	public bool NeedUpload { get; set; }

	public FocusConfig(Action secondChange, Action statusChange)
	{
		_secondChange = secondChange;
		_statusChange = statusChange;
		Type = ((!(LocalSettings.Settings.PomoType == FocusConstance.Focus)) ? 1 : 0);
		SetPomoSeconds();
	}

	private void OnStatusChanged()
	{
		if (Status == PomoStatus.WaitingRelax)
		{
			DelayActionHandlerCenter.TryDoAction("StartWaitingRelax", delegate
			{
				PomoCount = 0;
				Status = PomoStatus.WaitingWork;
			}, 10800000);
		}
		else
		{
			DelayActionHandlerCenter.RemoveAction("StartWaitingRelax");
		}
		switch (Status)
		{
		case PomoStatus.WaitingWork:
			SetStartSeconds();
			App.RefreshIconMenu();
			JumpHelper.InitJumpList();
			break;
		case PomoStatus.WaitingRelax:
			AutoStopTime = DateTime.Now;
			SetRelaxSeconds();
			App.RefreshIconMenu();
			JumpHelper.InitJumpList();
			break;
		}
		if ((Status == PomoStatus.Working && !LocalSettings.Settings.AutoNextPomo) || (Status == PomoStatus.Relaxing && !LocalSettings.Settings.AutoBreak))
		{
			TickFocusManager.HidePomoReminder();
		}
	}

	public void StartFocus(DateTime now, bool auto = false)
	{
		Id = Utils.GetGuid();
		NeedUpload = LocalSettings.Settings.FocusKeepInSync;
		AutoStopTime = null;
		Note = string.Empty;
		PieceHandler.Reset(now);
		PieceHandler.AddPiece(FocusVModel, now);
		PauseDuration = 0.0;
		StartTime = now;
		if (auto)
		{
			auto = LocalSettings.Settings.AutoBreak && LocalSettings.Settings.AutoNextPomo;
		}
		if (!auto && Type == 0)
		{
			AutoTimes = LocalSettings.Settings.AutoPomoTimes;
		}
		if (Type == 1 || !auto || string.IsNullOrEmpty(FirstFocusId) || !LocalSettings.Settings.FocusKeepInSync)
		{
			FirstFocusId = Id;
		}
		if (Type == 0)
		{
			if ((now - LastPomoCompleteTime).TotalMinutes > 60.0)
			{
				PomoCount = 0;
			}
			if (LocalSettings.Settings.LongBreakEvery >= 1 && PomoCount >= 1 && PomoCount % LocalSettings.Settings.LongBreakEvery == 0)
			{
				PomoCount = 0;
			}
			AutoTimes--;
		}
		else
		{
			PomoCount = 0;
		}
		PomoCount++;
	}

	public void SetRelaxSeconds()
	{
		Second = GetRelaxMinutes() * 60;
		CurrentSeconds = Second;
	}

	public long GetRelaxMinutes()
	{
		bool flag = LocalSettings.Settings.LongBreakEvery >= 1 && PomoCount >= 1 && PomoCount % LocalSettings.Settings.LongBreakEvery != 0;
		if ((DateTime.Now - LastPomoCompleteTime).TotalMinutes > 60.0)
		{
			flag = true;
		}
		return flag ? LocalSettings.Settings.ShortBreakDuration : LocalSettings.Settings.LongBreakDuration;
	}

	public void SetPomoSeconds(int? dur = null)
	{
		_pomoDuration = dur;
		SetStartSeconds();
	}

	public void SetStartSeconds()
	{
		if (Type == 0)
		{
			int num = _pomoDuration ?? LocalSettings.Settings.PomoDuration;
			int num2 = ((num == 0) ? 25 : Math.Max(Constants.MinFocusDuration / 60, num));
			Second = num2 * 60;
			CurrentSeconds = Second;
		}
		else
		{
			Second = 0L;
			CurrentSeconds = 0.0;
		}
	}

	public void OnPause(DateTime time)
	{
		PieceHandler.AddPiece(new FocusPieceModel
		{
			Id = "StartPause",
			BeginTime = time
		});
		double num = (time - StartTime).TotalSeconds - PauseDuration;
		PauseTime = time;
		CurrentSeconds = ((Type == 0) ? ((double)Second - num) : num);
	}

	public void OnContinue(DateTime time)
	{
		double totalSeconds = (time - PauseTime).TotalSeconds;
		PauseDuration += totalSeconds;
	}

	public void Reset(bool resetId = true)
	{
		if (resetId)
		{
			FocusVModel.SetupTask(string.Empty, 0);
		}
		AutoTimes = 0;
		PomoCount = 0;
		AutoStopTime = null;
		_previewStatus = PomoStatus.WaitingWork;
		_status = PomoStatus.WaitingWork;
		_statusChange();
		SetPomoSeconds();
		OnStatusChanged();
	}

	public async Task OnTaskSelected(FocusPieceModel pieceModel)
	{
		DateTime now = DateTime.Now;
		if (pieceModel == null || string.IsNullOrEmpty(pieceModel.Id))
		{
			if (Status == PomoStatus.Working)
			{
				PieceHandler.AddPiece(new FocusPieceModel
				{
					BeginTime = DateTime.Now
				});
			}
			if (IsWorking)
			{
				FocusOptionUploader.AddOption(FocusOption.focus, now, manual: true);
			}
		}
		else
		{
			if (!IsWorking)
			{
				return;
			}
			var (last, flag) = PieceHandler.GetLastRealPiece();
			DateTime time = (pieceModel.BeginTime = now);
			bool flag2 = last != null;
			if (flag && last != null && string.IsNullOrEmpty(last.Id))
			{
				List<Inline> inlines = new List<Inline>
				{
					new Run(Utils.GetString("SwitchPomoTaskTip"))
					{
						Foreground = ThemeUtil.GetColor("BaseColorOpacity100_80")
					}
				};
				string name = string.Empty;
				TimerModel timer = await TimerDao.GetTimerByIdOrObjId(pieceModel.Id);
				if (pieceModel.Type == 1 || timer?.ObjType == "habit")
				{
					HabitModel habitModel = await HabitDao.GetHabitById(timer?.ObjId ?? pieceModel.Id);
					if (habitModel != null)
					{
						name = habitModel.Name;
					}
				}
				else
				{
					TaskModel taskModel = await TaskDao.GetThinTaskById(timer?.ObjId ?? pieceModel.Id);
					if (taskModel != null)
					{
						name = taskModel.title;
					}
				}
				if (string.IsNullOrEmpty(name) && timer != null && !timer.Deleted)
				{
					name = timer.Name;
				}
				inlines.Add(new Run(" " + name + " ")
				{
					Foreground = ThemeUtil.GetColor("PrimaryColor")
				});
				inlines.Add(new Run(Utils.IsCn() ? "？" : "?")
				{
					Foreground = ThemeUtil.GetColor("BaseColorOpacity100_80")
				});
				bool? flag3 = new CustomerDialog(string.Empty, inlines, Utils.GetString("BelongTo"), Utils.GetString("NotBelongTo")).ShowDialog();
				flag2 = flag3 == true;
				if (flag3 == true)
				{
					time = last.BeginTime;
				}
			}
			if (flag2)
			{
				last.Id = pieceModel.Id;
				last.Type = pieceModel.Type;
			}
			else if (Status == PomoStatus.Working)
			{
				PieceHandler.AddPiece(pieceModel);
			}
			FocusOptionUploader.AddOption(FocusOption.focus, time, manual: true);
		}
	}

	private async Task SaveHarvestTaskSelection(FocusPieceModel pieceModel, string pomoId, int version)
	{
		if (version > 0 && !string.IsNullOrEmpty(pomoId) && version == _harvestTaskBindVersion)
		{
			string focusId = pieceModel?.Id ?? string.Empty;
			int focusType = pieceModel?.Type ?? 0;
			await PomoService.RebindLatestPomoTask(pomoId, focusId, focusType);
		}
	}

	private async Task ApplyFocusSelection(FocusPieceModel pieceModel)
	{
		await FocusVModel.SetupTask(pieceModel.Id, pieceModel.Type);
		await OnTaskSelected(pieceModel);
	}

	public async Task SetFocusId(string focusId, int type = 0)
	{
		FocusPieceModel pieceModel = new FocusPieceModel
		{
			Id = focusId,
			Type = type
		};
		if (Type != 0 || Status != PomoStatus.WaitingRelax || string.IsNullOrEmpty(Id))
		{
			await ApplyFocusSelection(pieceModel);
			return;
		}
		string harvestPomoId = Id;
		int harvestVersion = ++_harvestTaskBindVersion;
		await _harvestTaskBindLock.LockAsync(async delegate
		{
			if (harvestVersion == _harvestTaskBindVersion)
			{
				await ApplyFocusSelection(pieceModel);
				await SaveHarvestTaskSelection(pieceModel, harvestPomoId, harvestVersion);
			}
		});
	}

	public void TrySetSeconds()
	{
		switch (Status)
		{
		case PomoStatus.WaitingWork:
			SetPomoSeconds();
			break;
		case PomoStatus.WaitingRelax:
			SetRelaxSeconds();
			break;
		}
	}

	public double GetDisplayPercent()
	{
		if (Type == 0)
		{
			return ((double)Second - CurrentSeconds) / (double)Second;
		}
		return CurrentSeconds % 60.0 / 60.0;
	}

	public double GetDisplayAngle(bool next)
	{
		if (Type == 0)
		{
			return ((double)Second - CurrentSeconds + (double)(next ? 1 : 0)) / (double)Second * 360.0;
		}
		return (CurrentSeconds + (double)(next ? 1 : 0)) % 60.0 * 6.0;
	}

	public void AdjustDuration(bool isAdd)
	{
		if (Type == 0 && IsWorking)
		{
			long num = Second + (isAdd ? 300 : (-300));
			double num2 = (DateTime.Now - StartTime).TotalSeconds - PauseDuration;
			double num3 = ((Status == PomoStatus.Pause) ? CurrentSeconds : ((double)num - num2));
			if (num3 <= 0.0 || num < Constants.MinFocusDuration)
			{
				FocusTimer.TryAbandonPomo();
			}
			else if (num <= 10800)
			{
				Second = num;
				CurrentSeconds = num3;
				FocusOptionUploader.AddOption(FocusOption.changeDuration, DateTime.Now, manual: true);
			}
		}
	}

	public List<(DateTime, DateTime)> GetPauseSpans()
	{
		return PieceHandler.GetPauseSpans();
	}

	public void SetNote(object sender, string note)
	{
		if (IsWorking)
		{
			Note = note;
			PomoNotifier.NotifyFocusNoteChanged(sender);
		}
		else if (AutoStopTime.HasValue)
		{
			Note = note;
			PomoService.SaveNote(Id, note);
		}
	}

	public string ToSaveSyncModel()
	{
		if (Status != PomoStatus.Working && Status != PomoStatus.Pause)
		{
			return null;
		}
		FocusSaveStatusModel focusSaveStatusModel = new FocusSaveStatusModel
		{
			id = Id,
			startTime = StartTime,
			endTime = null,
			duration = ((Type == 0 && IsWorking) ? (Second / 60) : 0),
			status = ((Status != PomoStatus.Working) ? ((Status == PomoStatus.Pause) ? 1 : 2) : 0),
			type = Type,
			valid = true,
			exited = false,
			firstId = FirstFocusId,
			autoPomoLeft = AutoTimes,
			pomoCount = PomoCount,
			needSync = NeedUpload
		};
		FocusBreakModel focusBreakModel = new FocusBreakModel();
		if (Status == PomoStatus.WaitingWork)
		{
			focusBreakModel.endTime = DateTime.Now;
		}
		if (Status == PomoStatus.Relaxing)
		{
			focusBreakModel.startTime = FocusTimer.TimerStartTime;
			focusBreakModel.duration = Second / 60;
		}
		focusSaveStatusModel.focusBreak = focusBreakModel;
		List<FocusLogModel> list = new List<FocusLogModel>();
		List<FocusLogModel> list2 = new List<FocusLogModel>();
		foreach (FocusPieceModel piece in PieceHandler.GetPieces())
		{
			if (piece.Id != "StartPause")
			{
				FocusLogModel focusLogModel = list.LastOrDefault();
				if (focusLogModel == null || focusLogModel.id != piece.Id)
				{
					list.Add(new FocusLogModel
					{
						type = piece.Type,
						id = piece.Id,
						time = piece.BeginTime
					});
				}
				continue;
			}
			list2.Add(new FocusLogModel
			{
				type = 0,
				time = piece.BeginTime
			});
			if (piece.EndTime.HasValue)
			{
				list2.Add(new FocusLogModel
				{
					type = 1,
					time = piece.EndTime.Value
				});
			}
		}
		focusSaveStatusModel.focusOnLogs = list.ToArray();
		focusSaveStatusModel.pauseLogs = list2.ToArray();
		return JsonConvert.SerializeObject(focusSaveStatusModel);
	}
}
