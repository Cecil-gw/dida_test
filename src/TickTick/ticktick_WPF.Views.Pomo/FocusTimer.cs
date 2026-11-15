using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Threading;
using TickTickUtils.Lang;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Sync;

namespace ticktick_WPF.Views.Pomo;

public class FocusTimer
{
	private static readonly SemaphoreLocker TimerLock;

	private static readonly SemaphoreLocker FocusSaveLock;

	private static readonly Timer FTimer;

	private static string _savedId;

	private static string _lastCompletePomoId;

	public static FocusConfig Config => TickFocusManager.Config;

	public static DateTime TimerStartTime { get; set; }

	public static double TimerPauseDuration { get; set; }

	public static bool IsTiming => FTimer.Enabled;

	public static bool IsPomo => Config.Type == 0;

	public static bool IsWorking
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

	private static PomoStatus Status
	{
		get
		{
			return Config.Status;
		}
		set
		{
			Config.Status = value;
		}
	}

	static FocusTimer()
	{
		TimerLock = new SemaphoreLocker();
		FocusSaveLock = new SemaphoreLocker();
		FTimer = new Timer(1000.0);
		FTimer.Elapsed += OnFTimerElapsed;
	}

	public static void BeginTimer(bool auto = false)
	{
		DateTime now = DateTime.Now;
		if (Status == PomoStatus.WaitingWork)
		{
			PomoSoundPlayer.StartPlaySound(trial: false);
			Config.StartFocus(now, auto);
			Status = PomoStatus.Working;
			if (LocalSettings.Settings.PomoLocalSetting.AutoShowWidget)
			{
				TickFocusManager.HideOrShowFocusWidget(force: true, 600);
			}
		}
		else
		{
			Status = PomoStatus.Relaxing;
		}
		TimerStartTime = now;
		TimerPauseDuration = 0.0;
		FTimer.Start();
		FocusOptionUploader.AddOption((Status != PomoStatus.Working) ? FocusOption.startBreak : FocusOption.start, now, !auto);
	}

	private static async void OnFTimerElapsed(object sender, ElapsedEventArgs e)
	{
		await TimerLock.LockAsync(async delegate
		{
			TimerStartTime = (Utils.IsEmptyDate(TimerStartTime) ? Config.StartTime : TimerStartTime);
			string id = Config.Id;
			double num = (DateTime.Now - TimerStartTime).TotalSeconds - TimerPauseDuration;
			if (IsPomo)
			{
				if (!(Config.CurrentSeconds <= 0.0) || !(Config.Id == _lastCompletePomoId))
				{
					Config.CurrentSeconds = (double)Config.Second - num;
					if (Config.CurrentSeconds <= 0.0)
					{
						_lastCompletePomoId = Config.Id;
						if (Application.Current.Dispatcher != null)
						{
							await Application.Current.Dispatcher.InvokeAsync((Func<Task>)async delegate
							{
								FTimer.Stop();
								Config.CurrentSeconds = 0.0;
								DateTime dateTime = DateTime.Now;
								bool num2 = Status == PomoStatus.Working;
								if (num2)
								{
									dateTime = TimerStartTime.AddSeconds(TimerPauseDuration + (double)Config.Second);
								}
								FocusOptionUploader.AddOption(num2 ? FocusOption.finish : FocusOption.endBreak, dateTime, manual: false);
								if (IsWorking)
								{
									Config.LastPomoCompleteTime = dateTime;
									await CompleteFocus(Config.Second, dateTime, Config.Type);
								}
								TickFocusManager.PlayTerminationSound(Status != PomoStatus.Working);
								await SwitchPomoBreak();
							});
						}
					}
				}
			}
			else if (id == Config.Id)
			{
				Config.CurrentSeconds = num;
				if (Config.CurrentSeconds >= 43200.0)
				{
					try
					{
						await StopTiming();
					}
					catch (Exception ex)
					{
						UtilLog.Error(ex);
					}
				}
			}
		});
	}

	private static async Task<bool> StopTiming()
	{
		DispatcherOperation<Task<bool>> dispatcherOperation = Application.Current.Dispatcher.InvokeAsync(async delegate
		{
			FTimer.Stop();
			PomoSoundPlayer.StopPlaySound();
			if (Config.CurrentSeconds > 3600.0)
			{
				if (!(await EnsureTimingNum()))
				{
					RestoreTimingAfterFailedStop();
					return false;
				}
			}
			else if (Config.CurrentSeconds >= (double)Constants.MinFocusDuration)
			{
				await SaveTimingNum((long)Config.CurrentSeconds);
			}
			else
			{
				FocusOptionUploader.AddOption(FocusOption.drop, DateTime.Now, manual: true);
			}
			Status = PomoStatus.WaitingWork;
			Config.Reset();
			return true;
		});
		try
		{
			return await dispatcherOperation.Task.Unwrap();
		}
		catch
		{
			await Application.Current.Dispatcher.InvokeAsync(RestoreTimingAfterFailedStop);
			throw;
		}
	}

	private static void RestoreTimingAfterFailedStop()
	{
		if (Status == PomoStatus.Working)
		{
			PomoSoundPlayer.StartPlaySound(trial: false);
			FTimer.Start();
		}
	}

	private static async Task SaveTimingNum(long num)
	{
		DateTime time = Config.StartTime.AddSeconds(Config.PauseDuration + (double)num);
		await CompleteFocus(num, DateTime.Now, 1);
		if (num >= Constants.MinFocusDuration)
		{
			if (time > DateTime.Now)
			{
				time = DateTime.Now;
			}
			FocusOptionUploader.AddOption(FocusOption.finish, time, manual: true);
		}
	}

	private static async Task<bool> EnsureTimingNum()
	{
		long duration = (long)Config.CurrentSeconds;
		bool manual = duration < 43200;
		Window dialogParentWindow = TickFocusManager.GetDialogParentWindow(manual);
		while (!manual && dialogParentWindow == null)
		{
			await Task.Delay(500);
			dialogParentWindow = TickFocusManager.GetDialogParentWindow(manual: false);
		}
		EnsureTimingDialog ensureTimingDialog = new EnsureTimingDialog(duration);
		try
		{
			ensureTimingDialog.Owner = dialogParentWindow;
		}
		catch (Exception)
		{
			ensureTimingDialog.Owner = null;
		}
		long? selectedDuration = null;
		ensureTimingDialog.OnSaveClick += delegate(object o, long e)
		{
			selectedDuration = e;
		};
		ensureTimingDialog.ShowDialog();
		if (!selectedDuration.HasValue)
		{
			return false;
		}
		await SaveTimingNum(selectedDuration.Value);
		return true;
	}

	public static async Task SwitchPomoBreak(bool isSkip = false)
	{
		FTimer.Stop();
		PomoSoundPlayer.StopPlaySound();
		TickFocusManager.HidePomoReminder();
		switch (Status)
		{
		case PomoStatus.Working:
		case PomoStatus.Pause:
			if (LocalSettings.Settings.AutoNextPomo && LocalSettings.Settings.AutoBreak && Config.AutoTimes <= 0)
			{
				Status = PomoStatus.WaitingWork;
				FocusOptionUploader.AddOption(FocusOption.exit, DateTime.Now, manual: true);
				return;
			}
			Status = PomoStatus.WaitingRelax;
			break;
		case PomoStatus.Relaxing:
			Status = PomoStatus.WaitingWork;
			break;
		}
		TickFocusManager.ShowPomoReminder(Status == PomoStatus.WaitingRelax, isSkip);
		await Task.Delay(600);
		if (((Status == PomoStatus.WaitingWork && LocalSettings.Settings.AutoNextPomo) || (Status == PomoStatus.WaitingRelax && LocalSettings.Settings.AutoBreak)) && Config.AutoTimes > 0)
		{
			BeginTimer(auto: true);
		}
	}

	public static void Pause(DateTime time, bool isManual = true)
	{
		if (Status != PomoStatus.Pause)
		{
			Status = PomoStatus.Pause;
			Config.OnPause(time);
			FTimer.Stop();
			PomoSoundPlayer.StopPlaySound();
			if (isManual)
			{
				FocusOptionUploader.AddOption(FocusOption.pause, time, manual: true);
			}
		}
	}

	public static void Continue(DateTime? time, bool isManual = true)
	{
		Status = PomoStatus.Working;
		PomoSoundPlayer.StartPlaySound(trial: false);
		if (time.HasValue)
		{
			Config.OnContinue(time.Value);
		}
		TimerPauseDuration = Config.PauseDuration;
		FTimer.Start();
		if (isManual && time.HasValue)
		{
			Config.PieceHandler.AddPiece(Config.FocusVModel, time.Value);
			FocusOptionUploader.AddOption(FocusOption.@continue, time.Value, manual: true);
		}
	}

	public static void Drop()
	{
		switch (Status)
		{
		case PomoStatus.Relaxing:
		case PomoStatus.WaitingRelax:
			FocusOptionUploader.AddOption(FocusOption.endBreak, DateTime.Now, manual: true);
			SkipRelax();
			break;
		case PomoStatus.Working:
		case PomoStatus.Pause:
			StopOrAbandon();
			break;
		case PomoStatus.WaitingWork:
			break;
		}
	}

	public static async Task<bool> StopOrAbandon(bool waitSync = false)
	{
		bool flag = ((!IsPomo) ? (await TryStopTiming(waitSync)) : (await TryAbandonPomo()));
		if (flag)
		{
			TickFocusManager.OnDrop();
		}
		return flag;
	}

	public static async Task<bool> TryAbandonPomo()
	{
		long second = (long)Math.Floor((double)Config.Second - Config.CurrentSeconds);
		bool? result = false;
		if (IsWorking && second >= 30)
		{
			await App.Instance.Dispatcher.InvokeAsync(delegate
			{
				Window dialogParentWindow = TickFocusManager.GetDialogParentWindow(manual: true);
				PomoAbandonDialog pomoAbandonDialog = new PomoAbandonDialog(!Config.FocusVModel.NoTask && !Config.FocusVModel.IsHabit, second >= Constants.MinFocusDuration);
				try
				{
					pomoAbandonDialog.Owner = dialogParentWindow;
				}
				catch (Exception)
				{
					pomoAbandonDialog.Owner = null;
				}
				pomoAbandonDialog.ShowDialog();
				result = pomoAbandonDialog.Saved;
			});
		}
		if (result.HasValue)
		{
			DateTime now = DateTime.Now;
			if (result.Value && second >= Constants.MinFocusDuration)
			{
				await CompleteFocus(second, now, 0);
				FocusOptionUploader.AddOption(FocusOption.finish, now, manual: true);
				Reset();
			}
			else
			{
				FocusOptionUploader.AddOption(FocusOption.drop, now, manual: true);
				ForceDrop();
			}
		}
		return result.HasValue;
	}

	public static void ForceDrop()
	{
		FTimer.Stop();
		PomoSoundPlayer.StopPlaySound();
		Status = PomoStatus.WaitingWork;
		Config.Reset();
		TickFocusManager.HidePomoReminder();
	}

	public static void Reset(bool isExit = true, bool resetId = true)
	{
		FTimer.Stop();
		TickFocusManager.ClearLocalFocus();
		if (isExit)
		{
			FocusOptionUploader.AddOption(FocusOption.exit, DateTime.Now, manual: true);
		}
		PomoSoundPlayer.StopPlaySound();
		Config.Reset(resetId);
		TickFocusManager.HidePomoReminder();
	}

	public static async Task<bool> TryStopTiming(bool waitSync)
	{
		bool? result = Config.CurrentSeconds >= (double)Constants.MinFocusDuration;
		if (IsWorking && Config.CurrentSeconds >= 30.0 && Config.CurrentSeconds < (double)Constants.MinFocusDuration)
		{
			PomoAbandonDialog pomoAbandonDialog = new PomoAbandonDialog(taskSelected: false, canSave: false);
			pomoAbandonDialog.ShowDialog();
			result = pomoAbandonDialog.Saved;
		}
		if (result.HasValue)
		{
			if (!result.Value)
			{
				Config.CurrentSeconds = 0.0;
			}
			if (!(await StopTiming()))
			{
				return false;
			}
			if (result.Value && waitSync)
			{
				await Task.Delay(1000);
			}
		}
		return result.HasValue;
	}

	public static void SkipRelax()
	{
		FocusOptionUploader.AddOption(FocusOption.endBreak, DateTime.Now, manual: true);
		Config.Status = PomoStatus.Relaxing;
		SwitchPomoBreak(isSkip: true);
	}

	public static void StoreConfigAndStop()
	{
		FTimer.Stop();
	}

	public static void StartTimer()
	{
		if (Status == PomoStatus.Working || Status == PomoStatus.Relaxing)
		{
			FTimer.Start();
		}
	}

	public static void Stop()
	{
		FTimer.Stop();
	}

	private static async Task CompleteFocus(long duration, DateTime time, int type)
	{
		if (duration >= Constants.MinFocusDuration)
		{
			Config.PieceHandler.AddPiece(new FocusPieceModel
			{
				Id = "StartPause",
				BeginTime = time
			}, force: true);
			await SaveFocus(type, duration);
		}
	}

	private static async Task SaveFocus(int type, long duration)
	{
		string focusId = Config.Id;
		await FocusSaveLock.LockAsync(() => SaveFocusCore(focusId, type, duration));
	}

	private static async Task SaveFocusCore(string focusId, int type, long duration)
	{
		if (_savedId == focusId || duration < Constants.MinFocusDuration)
		{
			return;
		}
		List<PomoTask> list = await Config.PieceHandler.GetPomoTaskModels(duration);
		PomodoroModel pomodoroModel = new PomodoroModel
		{
			Id = focusId,
			StartTime = Config.PieceHandler.GetBeginTime(),
			SyncStatus = 0,
			Type = type,
			Status = 1,
			UserId = Utils.GetCurrentUserStr(),
			Note = Config.Note
		};
		long num = 0L;
		List<PomoTask> list2 = new List<PomoTask>();
		if (list != null && list.Count > 0)
		{
			foreach (PomoTask item in list)
			{
				if (item.TaskId == "StartPause")
				{
					num += Utils.GetTotalSecond(item.StartTime, item.EndTime);
					continue;
				}
				item.PomoId = pomodoroModel.Id;
				DateTime dateTime = pomodoroModel.StartTime.AddSeconds(num + duration);
				if (item.EndTime > dateTime)
				{
					if (item.StartTime < dateTime)
					{
						item.EndTime = dateTime;
						list2.Add(item);
					}
					break;
				}
				list2.Add(item);
			}
			pomodoroModel.PauseDuration = num;
			pomodoroModel.Tasks = list2.ToArray();
		}
		if (pomodoroModel.Tasks == null || pomodoroModel.Tasks.Length == 0)
		{
			Config.PieceHandler.PrintLog();
		}
		pomodoroModel.EndTime = pomodoroModel.StartTime.AddSeconds(pomodoroModel.PauseDuration + duration);
		await PomoService.SaveFocusModel(pomodoroModel);
		_savedId = focusId;
	}

	public static async Task TryStartWithId(string id, int type = 0, bool force = false)
	{
		await Config.SetFocusId(id, type);
		if (force)
		{
			TryBeginFocus();
		}
	}

	private static async void TryBeginFocus()
	{
		switch (Status)
		{
		case PomoStatus.Relaxing:
		case PomoStatus.WaitingRelax:
			SkipRelax();
			BeginTimer();
			break;
		case PomoStatus.WaitingWork:
			BeginTimer();
			break;
		case PomoStatus.Pause:
			Continue(DateTime.Now);
			break;
		}
	}
}
