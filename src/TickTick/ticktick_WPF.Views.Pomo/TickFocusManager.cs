using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using Newtonsoft.Json;
using TickTickDao;
using TickTickModels;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.SyncServices;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Pomo.MainFocus;
using ticktick_WPF.Views.Pomo.MiniFocus;
using ticktick_WPF.Views.Remind;

namespace ticktick_WPF.Views.Pomo;

public static class TickFocusManager
{
	public static FocusConfig Config;

	public static FocusView MainFocus;

	private static bool _showClockPanel;

	public static MiniFocusWindow MiniFocusWindow;

	public static FocusImmerseWindow ImmerseWindow;

	public static bool SaveAfterClose;

	public static PomoStatus Status => Config.Status;

	public static bool Working => Config.IsWorking;

	public static bool IsPomo => Config.Type == 0;

	public static bool NoFocus => string.IsNullOrEmpty(Config.FocusVModel.FocusId);

	public static bool InRelax
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

	public static int DisplaySecond => Math.Min(43200, (int)Math.Round(Config.CurrentSeconds, 0, MidpointRounding.AwayFromZero));

	public static PomoStat PomoStatistics { get; set; }

	public static event FocusChange StatusChanged;

	public static event FocusChange TypeChanged;

	public static event FocusChange CurrentSecondChanged;

	public static event FocusChange DurationChanged;

	static TickFocusManager()
	{
		Config = new FocusConfig(OnCurrentSecondChange, OnStatusChange);
		SaveAfterClose = true;
		LocalSettings.Settings.PropertyChanged += OnPomoLocalSettingsChanged;
		PomoSoundHelper.InitSetFocusSound();
		LoadTodayData();
	}

	private static void OnPomoLocalSettingsChanged(object sender, PropertyChangedEventArgs e)
	{
		Func<bool> isCurrentUser;
		if (!(e.PropertyName != "PomoLocalSetting"))
		{
			isCurrentUser = LocalSettings.Settings.CaptureFocusSyncGuard();
			Dispatcher dispatcher = Application.Current?.Dispatcher;
			if (dispatcher != null)
			{
				dispatcher.BeginInvoke(new Action(RefreshWaitingFocus));
			}
			else
			{
				RefreshWaitingFocus();
			}
		}
		void RefreshWaitingFocus()
		{
			if (isCurrentUser() && Config.Type == 0 && Config.Status == PomoStatus.WaitingWork)
			{
				Config.SetStartSeconds();
			}
		}
	}

	private static void OnStatusChange()
	{
		TickFocusManager.StatusChanged?.Invoke();
	}

	private static void OnCurrentSecondChange()
	{
		TickFocusManager.CurrentSecondChanged?.Invoke();
	}

	public static void InitFocusControl()
	{
		if (MainFocus == null)
		{
			MainFocus = new FocusView(_showClockPanel);
		}
	}

	public static bool ConfirmSwitch(string id, string title, Window owner = null)
	{
		if (IsFocusing(id))
		{
			return false;
		}
		if (!Working)
		{
			return true;
		}
		List<Inline> list = new List<Inline>();
		string[] array = Utils.GetString(IsPomo ? "SwitchTimingTips" : "SwitchPomoTips").Split('0');
		if (array.Length == 2)
		{
			list.Add(new Run(array[0].Substring(0, array[0].Length - 1)));
			list.Add(new Run(title)
			{
				Foreground = ThemeUtil.GetColor("PrimaryColor")
			});
			list.Add(new Run(array[1].Substring(1, array[1].Length - 1)));
		}
		bool? flag = new CustomerDialog("", list, Utils.GetString("Ok"), Utils.GetString("Cancel"), owner).ShowDialog();
		if (flag.HasValue)
		{
			return flag != false;
		}
		return false;
	}

	public static async Task<bool> TryStartFocusTimer(string id, Window owner)
	{
		TimerModel timerModel = await TimerDao.GetTimerById(id);
		if (timerModel == null)
		{
			return false;
		}
		if (!ConfirmSwitch(timerModel.Id, timerModel.Name, owner))
		{
			return false;
		}
		if (!FocusTimer.IsWorking)
		{
			SetFocusType((!(timerModel.Type == "pomodoro")) ? 1 : 0);
			if (timerModel.Type == "pomodoro")
			{
				Config.SetPomoSeconds(timerModel.PomodoroTime);
			}
		}
		FocusTimer.TryStartWithId(timerModel.Id, 2, force: true);
		return true;
	}

	public static async void TryStartFocusHabit(string id, bool isPomo)
	{
		if (IsFocusing(id))
		{
			Utils.Toast(Utils.GetString("TaskIsFocused"));
			return;
		}
		HabitModel habitModel = await HabitDao.GetHabitById(id);
		if (habitModel == null || !ConfirmSwitch(habitModel.Id, habitModel.Name))
		{
			return;
		}
		if (!FocusTimer.IsWorking)
		{
			SetFocusType((!isPomo) ? 1 : 0);
			if (isPomo)
			{
				TimerModel timerModel = await TimerDao.GetTimerByObjId(id);
				if (timerModel != null && timerModel.Type == "pomodoro")
				{
					Config.SetPomoSeconds(timerModel.PomodoroTime);
				}
			}
		}
		StartFocus(id, isHabit: true);
	}

	public static async Task StartFocus(string objId = "", bool isHabit = false, bool checkTimer = false)
	{
		if (checkTimer)
		{
			TimerModel timerModel = await TimerDao.GetTimerByObjId(objId);
			if (timerModel != null)
			{
				TryStartFocusTimer(timerModel.Id, null);
				return;
			}
		}
		if (!isHabit)
		{
			TaskBaseViewModel taskById = TaskCache.GetTaskById(objId);
			if (taskById != null)
			{
				ProjectModel projectById = CacheManager.GetProjectById(taskById.ProjectId);
				if (projectById != null)
				{
					Config.FocusVModel.SetIdentity(new NormalProjectIdentity(projectById));
				}
			}
		}
		FocusTimer.TryStartWithId(objId, isHabit ? 1 : 0, force: true);
	}

	public static void OnSessionChanged()
	{
		if (FocusTimer.IsTiming)
		{
			FocusTimer.StoreConfigAndStop();
		}
		else
		{
			FocusTimer.StartTimer();
		}
	}

	public static void ResetPomoSetting()
	{
		Config.TrySetSeconds();
	}

	public static bool IsFocusing(string id)
	{
		if ((id == Config.FocusVModel?.FocusId && !string.IsNullOrEmpty(Config.FocusVModel?.FocusId)) || (id == Config.FocusVModel?.ObjId && !string.IsNullOrEmpty(Config.FocusVModel?.ObjId)))
		{
			return FocusTimer.IsTiming;
		}
		return false;
	}

	public static void HidePomoReminder()
	{
		Application.Current?.Dispatcher?.Invoke(delegate
		{
			ReminderWindowManager.CloseAllPomoReminders();
		});
		SystemToastUtils.RemovePomoToast();
	}

	public static async Task NotifyPomoTaskChanged(string oldTaskId, string newTaskId)
	{
		if (!string.IsNullOrEmpty(newTaskId))
		{
			await Config.PieceHandler.ChangePieceTaskId(oldTaskId, newTaskId);
		}
		if (Config.FocusVModel.ObjId == oldTaskId)
		{
			await Config.FocusVModel.SetupTask("", 0);
			DateTime now = DateTime.Now;
			if (Status == PomoStatus.Working)
			{
				Config.PieceHandler.AddPiece(new FocusPieceModel
				{
					BeginTime = now
				});
			}
			if (Working)
			{
				FocusOptionUploader.AddOption(FocusOption.focus, now, manual: true);
			}
		}
	}

	public static void ShowPomoReminder(bool relax, bool isSkip)
	{
		if (!IsPomo || (!relax && isSkip))
		{
			return;
		}
		bool automatic = (relax ? LocalSettings.Settings.AutoBreak : LocalSettings.Settings.AutoNextPomo);
		if (!LocalSettings.Settings.ShowReminderInClient && !Utils.IsWindows7() && SystemToastUtils.CheckSystemToastEnable())
		{
			SystemToastUtils.ToastPomo(relax, automatic);
			return;
		}
		Application.Current?.Dispatcher?.Invoke(delegate
		{
			ReminderWindowManager.AddPomoReminder(relax, automatic);
		});
	}

	public static void BeginFocus(bool isPomo, bool forceShowWidget = false)
	{
		if (!Working)
		{
			if (isPomo != (LocalSettings.Settings.PomoType == FocusConstance.Focus))
			{
				SetFocusType((!isPomo) ? 1 : 0);
			}
			StartFocus();
		}
		if (forceShowWidget)
		{
			HideOrShowFocusWidget(force: true);
		}
	}

	public static void GetRemotePomos()
	{
		LoadTodayData(null, loadRemote: true, force: false);
	}

	public static void OnPomoSettingsChanged()
	{
		Config.TrySetSeconds();
	}

	public static void HandleRemoteOption(FocusOptionModel model)
	{
		if (Config.Id == null || Config.Id == model.oId)
		{
			if (Config.FocusVModel.FocusId != model.focusOnId)
			{
				Config.FocusVModel.SetupTask(model.focusOnId, model.focusOnType, model.focusOnTitle);
			}
			if (LocalSettings.Settings.FocusKeepInSync && Enum.TryParse<FocusOption>(model.op, ignoreCase: true, out var result))
			{
				switch (result)
				{
				case FocusOption.pause:
					FocusTimer.Pause(model.time, isManual: false);
					break;
				case FocusOption.@continue:
					FocusTimer.Continue(model.time, isManual: false);
					break;
				}
			}
		}
		FocusOptionUploader.SaveOption(null, force: true, resetLocalPieces: true, delaySync: false, needLog: true);
	}

	public static void HandleFocusResult(FocusSyncStatusModel model, bool resetLocalPieces, bool? localNeedSync = null, Func<bool> isCurrentUser = null)
	{
		isCurrentUser = isCurrentUser ?? LocalSettings.Settings.CaptureFocusSyncGuard();
		Application.Current?.Dispatcher?.Invoke(delegate
		{
			if (isCurrentUser())
			{
				HandleSyncStatus(model, resetLocalPieces, localNeedSync);
			}
		});
	}

	public static async Task HandleSyncStatus(FocusSyncStatusModel model, bool resetLocalPieces, bool? localNeedSync = null)
	{
		LocalSettings.Settings.ObserveSyncedFocusStatus(model);
		if ((!Config.NeedUpload && Config.IsWorking) || (model.type != Config.Type && (model.IsDropOrExit() || (model.type == 1 && model.status >= 2))))
		{
			return;
		}
		resetLocalPieces = resetLocalPieces || Config.Id != model.id || model.status == 2;
		if (model.type != Config.Type)
		{
			SetFocusType(model.type);
			FocusTimer.Stop();
			Config.Status = PomoStatus.WaitingWork;
		}
		if ((Config.Id == model.id && Working && model.status == 2) || Config.Id != model.id)
		{
			FocusOptionUploader.AddOption(FocusOption.note, DateTime.Now, manual: true, null, Config.Note);
		}
		if (Config.Id != model.id)
		{
			Config.Id = model.id;
			Config.Note = string.Empty;
		}
		if (model.IsDropOrExit())
		{
			FocusTimer.Reset(isExit: false);
			return;
		}
		if (model.status != 3)
		{
			Config.NeedUpload = localNeedSync ?? true;
			Config.FirstFocusId = model.firstId;
			if (resetLocalPieces)
			{
				Config.PieceHandler.SetRemoteModel(model);
			}
			if (model.status < 2)
			{
				FocusLogModel currentFocusOn = model.GetCurrentFocusOn();
				Config.FocusVModel.SetupTask(currentFocusOn?.id ?? string.Empty, (currentFocusOn?.type).GetValueOrDefault(), currentFocusOn?.title);
			}
		}
		else if (Config.Status == PomoStatus.WaitingWork)
		{
			return;
		}
		switch (model.status)
		{
		case 0:
			Config.Second = ((model.type == 0) ? (model.duration * 60) : 0);
			Config.PauseDuration = model.GetPauseTime();
			Config.StartTime = model.startTime;
			FocusTimer.TimerStartTime = model.startTime;
			FocusTimer.TimerPauseDuration = Config.PauseDuration;
			if (Config.Status != PomoStatus.Working)
			{
				FocusTimer.Continue(null, isManual: false);
			}
			FocusTimer.StartTimer();
			UtilLog.Info("SyncFocus " + Config.StartTime.ToString() + " pauseTime " + Config.PauseDuration + "   pause " + model.pauseLogs?.Length);
			HidePomoReminder();
			break;
		case 1:
			Config.Second = ((model.type == 0) ? (model.duration * 60) : 0);
			Config.PauseDuration = model.GetPauseTime();
			Config.StartTime = model.startTime;
			FocusTimer.TimerStartTime = model.startTime;
			FocusTimer.TimerPauseDuration = Config.PauseDuration;
			if (Config.Status != PomoStatus.Pause)
			{
				FocusTimer.Pause(model.GetLastPauseTime(), isManual: false);
			}
			else
			{
				DateTime lastPauseTime = model.GetLastPauseTime();
				Config.PauseTime = model.GetLastPauseTime();
				double num = (lastPauseTime - Config.StartTime).TotalSeconds - Config.PauseDuration;
				Config.CurrentSeconds = ((Config.Type == 0) ? ((double)Config.Second - num) : num);
			}
			HidePomoReminder();
			break;
		case 2:
		{
			if (model.type == 1 || (!string.IsNullOrEmpty(model.firstId) && model.firstId != model.id && model.autoPomoLeft <= 0))
			{
				FocusTimer.Reset(isExit: false);
				break;
			}
			if (model.type == 0 && (model.focusBreak?.endTime.HasValue ?? false))
			{
				PomoStatus status = Config.Status;
				Config.Status = PomoStatus.WaitingRelax;
				Config.Status = PomoStatus.WaitingWork;
				FocusTimer.Stop();
				if (status != PomoStatus.WaitingWork)
				{
					ShowPomoReminder(relax: false, isSkip: false);
				}
				break;
			}
			FocusBreakModel focusBreak = model.focusBreak;
			if (focusBreak != null && focusBreak.duration > 0 && (model.focusBreak?.startTime.HasValue ?? false) && model.focusBreak.startTime > model.startTime && model.focusBreak.startTime.Value.AddMinutes(model.focusBreak.duration.Value) > DateTime.Now)
			{
				if (Config.Status != PomoStatus.Relaxing)
				{
					Config.Status = PomoStatus.Relaxing;
				}
				Config.Second = model.focusBreak.duration.Value * 60;
				FocusTimer.TimerStartTime = model.focusBreak.startTime.Value;
				FocusTimer.TimerPauseDuration = 0.0;
				FocusTimer.StartTimer();
				HidePomoReminder();
			}
			else if (model.type == 0 && Config.Status != PomoStatus.Relaxing && Config.Status != PomoStatus.WaitingRelax)
			{
				Config.Status = PomoStatus.Working;
				FocusTimer.SwitchPomoBreak();
			}
			break;
		}
		case 3:
			FocusTimer.Reset(isExit: false);
			break;
		}
		Config.AutoTimes = model.autoPomoLeft;
		Config.PomoCount = model.pomoCount;
	}

	public static void PlayTerminationSound(bool isRest)
	{
		RemindSoundPlayer.PlayFocusRemindSound(isRest);
	}

	public static void OnTasksChanged(TasksChangeEventArgs e)
	{
		if (e.DeletedChangedIds.Contains(Config.FocusVModel.FocusId))
		{
			FocusTimer.TryStartWithId(string.Empty);
		}
		if (e.TaskTextChangedIds.Contains(Config.FocusVModel.ObjId))
		{
			TaskBaseViewModel taskById = TaskCache.GetTaskById(Config.FocusVModel.ObjId);
			Config.FocusVModel.Title = taskById?.Title;
		}
	}

	public static Geometry GetSoundIcon()
	{
		return Utils.GetGeometry("Ic.Focus." + FocusConstance.GetSoundIndex(LocalSettings.Settings.PomoSound)) ?? Utils.GetGeometry("Ic.Focus.Mute");
	}

	public static void SetFocusType(int type)
	{
		if (Config.Type != type)
		{
			bool isExit = (Config.FromRelax && !string.IsNullOrEmpty(Config.Id) && IsPomo) || InRelax;
			LocalSettings.Settings.PomoType = ((type == 0) ? FocusConstance.Focus : FocusConstance.Timing);
			Config.Type = type;
			FocusTimer.Reset(isExit, resetId: false);
			TickFocusManager.TypeChanged?.Invoke();
			LoadTodayData(IsPomo, loadRemote: true);
		}
	}

	public static Window GetDialogParentWindow(bool manual)
	{
		if (!manual)
		{
			if (ImmerseWindow != null)
			{
				return ImmerseWindow;
			}
			if (App.Window.IsActive && App.Window.IsVisible && App.Window.ShowFocus)
			{
				return App.Window;
			}
			if (MiniFocusWindow != null)
			{
				return MiniFocusWindow;
			}
		}
		else
		{
			if (App.Window.IsActive && App.Window.IsVisible)
			{
				return App.Window;
			}
			if (MiniFocusWindow != null && MiniFocusWindow.IsActive)
			{
				return MiniFocusWindow;
			}
		}
		return (Window)(((object)ImmerseWindow) ?? ((object)App.Window));
	}

	public static void AdjustDuration(bool add)
	{
		if (IsPomo && Working)
		{
			UserActCollectUtils.AddClickEvent("focus", "pomo_running", add ? "click_+" : "click_-");
			Config.AdjustDuration(add);
			TickFocusManager.DurationChanged?.Invoke();
		}
	}

	public static async Task OnFocusIdSelected(string id, int type)
	{
		if (string.IsNullOrEmpty(id))
		{
			return;
		}
		FocusTimer.TryStartWithId(id, type);
		if (Status == PomoStatus.WaitingWork && !Config.FromRelax && IsPomo)
		{
			TimerModel timerModel = await TimerDao.GetTimerByIdOrObjId(id);
			if (timerModel != null && timerModel.Status == 0 && timerModel.Type == "pomodoro" && !timerModel.Deleted)
			{
				Config.SetPomoSeconds(timerModel.PomodoroTime);
			}
		}
	}

	public static void ShowImmerseWindow(Window owner)
	{
		if (ImmerseWindow == null)
		{
			ImmerseWindow = new FocusImmerseWindow
			{
				Owner = owner
			};
			ImmerseWindow.Show();
			return;
		}
		try
		{
			ImmerseWindow.Activate();
		}
		catch (Exception)
		{
			ImmerseWindow = null;
			ImmerseWindow = new FocusImmerseWindow
			{
				Owner = owner
			};
			ImmerseWindow.Show();
		}
	}

	public static void OpenStatisticsInWeb()
	{
		Utils.TryProcessStartUrlWithToken("/webapp/#statistics/pomo?enablePomo=true");
	}

	public static void ReloadStatistics()
	{
		LoadStatistics();
		MainFocus?.ReloadStatistics();
	}

	public static string GetActCType()
	{
		string result = "focus_tab";
		switch (Status)
		{
		case PomoStatus.Working:
			result = (IsPomo ? "pomo_running" : "stopwatch_running");
			break;
		case PomoStatus.Relaxing:
			result = "pomo_relaxing";
			break;
		case PomoStatus.Pause:
			result = (IsPomo ? "pomo_paused" : "stopwatch_pause");
			break;
		case PomoStatus.WaitingWork:
			if (IsPomo && Config.FromRelax)
			{
				result = "pomo_again";
			}
			break;
		case PomoStatus.WaitingRelax:
			result = "pomo_finished";
			break;
		}
		return result;
	}

	public static void Clear()
	{
		FocusTimer.Stop();
		Config.Reset();
	}

	public static void OnDrop()
	{
		MainFocus?.OnDropFocus();
		LocalSettings.Settings.ExtraSettings.CurrentFocus = null;
		LocalSettings.Settings.Save(force: true);
	}

	public static void OnTimerChanged(TimerModel timer)
	{
		if (Config.FocusVModel.FocusId == timer.Id)
		{
			Config.FocusVModel.Title = timer.Name;
		}
	}

	public static async void LoadSavedFocus()
	{
		_ = 1;
		try
		{
			await FocusOptionUploader.SaveOption(null, force: true, resetLocalPieces: true);
			await Task.Delay(3000);
			if (Status == PomoStatus.WaitingWork && string.IsNullOrEmpty(Config.Id))
			{
				FocusSyncStatusModel focusSyncStatusModel = JsonConvert.DeserializeObject<FocusSyncStatusModel>(LocalSettings.Settings.ExtraSettings.CurrentFocus);
				if (focusSyncStatusModel != null && !Utils.IsEmptyDate(focusSyncStatusModel.startTime))
				{
					focusSyncStatusModel.autoPomoLeft = 0;
					HandleFocusResult(focusSyncStatusModel, resetLocalPieces: true, JsonConvert.DeserializeObject<FocusSaveStatusModel>(LocalSettings.Settings.ExtraSettings.CurrentFocus)?.needSync);
				}
				UtilLog.Info("LoadSavedFocus:" + LocalSettings.Settings.ExtraSettings.CurrentFocus);
			}
		}
		catch (Exception)
		{
		}
	}

	public static void SetFocusInitPanel()
	{
		_showClockPanel = MainFocus?.GetIsShowClockPanel() ?? false;
	}

	public static async void ClearLocalFocus()
	{
		await Task.Delay(400);
		LocalSettings.Settings.ExtraSettings.CurrentFocus = null;
		LocalSettings.Settings.Save(force: true);
	}

	public static void CloseFocus()
	{
		SaveAfterClose = false;
		MiniFocusWindow?.CloseWithReason("DisableFocus");
		ImmerseWindow?.Close();
		LocalSettings.Settings.ExtraSettings.CurrentFocus = string.Empty;
		LocalSettings.Settings.Save(force: true);
		FocusTimer.StopOrAbandon(waitSync: true);
	}

	public static async Task AppendDuration(DateTime now, Grid toastGrid)
	{
		string pomoId = FocusTimer.Config.Id;
		PomodoroModel pomodoroModel = await PomoDao.GetPomoById(pomoId);
		if (pomodoroModel == null)
		{
			UtilLog.Error("last focus id“" + pomoId + "” not found");
			return;
		}
		int num = (int)(now - pomodoroModel.EndTime).TotalMinutes * 60;
		long val = Math.Max(0L, Utils.GetTotalSecond(pomodoroModel.StartTime, pomodoroModel.EndTime) - pomodoroModel.PauseDuration) + num;
		val = Math.Min(val, 10800L);
		long adjustTime = val * 1000;
		PomoNotifier.NotifyPageChange(PomoPageEventType.EditConfirm, (int)val, PomoDurationChangeSource.AppendDuration);
		CustomUndoToast customUndoToast = new CustomUndoToast();
		customUndoToast.SetText(Utils.GetString("Added"));
		customUndoToast.SetEvent(async delegate
		{
			UserActCollectUtils.AddClickEvent("focus", "pomo_finished", "add_duration_exceeded");
			await PomoService.UpdateLastPomoDuration(pomoId, adjustTime);
			PomoNotifier.NotifyPageChange(PomoPageEventType.EditFinish, null, PomoDurationChangeSource.AppendDuration);
		}, delegate
		{
			PomoNotifier.NotifyPageChange(PomoPageEventType.EditUndo, null, PomoDurationChangeSource.AppendDuration);
		});
		WindowToastHelper.ShowAndHideToast(toastGrid, customUndoToast);
	}

	public static async Task EditDuration(Point? locate = null, Rect? ownerRect = null, Grid toastGrid = null, Window ownerWindow = null, bool isDark = false)
	{
		string pomoId = FocusTimer.Config.Id;
		PomodoroModel pomo = await PomoDao.GetPomoById(pomoId);
		if (pomo == null)
		{
			UtilLog.Error("last focus id“" + pomoId + "” not found");
			return;
		}
		EditFocusDurationDialog dialog = new EditFocusDurationDialog(isDark)
		{
			Owner = (ownerWindow ?? App.Window)
		};
		if (locate.HasValue)
		{
			dialog.Left = locate.Value.X;
			dialog.Top = locate.Value.Y;
		}
		if (ownerRect.HasValue)
		{
			dialog.OwnerControlRect = ownerRect.Value;
		}
		if (!locate.HasValue && !ownerRect.HasValue)
		{
			dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
		}
		await dialog.LoadItem(pomo);
		dialog.ShowDialog();
		int targetSeconds = dialog.TargetSeconds;
		if (targetSeconds <= 0)
		{
			return;
		}
		int adjustTime = targetSeconds * 1000;
		if (!pomo.AdjustTime.HasValue || pomo.AdjustTime != adjustTime)
		{
			PomoNotifier.NotifyPageChange(PomoPageEventType.EditConfirm, targetSeconds);
			CustomUndoToast customUndoToast = new CustomUndoToast();
			customUndoToast.SetText(Utils.GetString("Saved"));
			customUndoToast.SetEvent(async delegate
			{
				PomoNotifier.NotifyPageChange(PomoPageEventType.EditFinish);
				await PomoService.UpdateLastPomoDuration(pomoId, adjustTime);
				UserActCollectUtils.AddClickEvent("focus", "pomo_finished", "edit_focus_duration");
			}, delegate
			{
				PomoNotifier.NotifyPageChange(PomoPageEventType.EditUndo);
			});
			WindowToastHelper.ShowAndHideToast(toastGrid, customUndoToast);
		}
	}

	public static async void HideOrShowFocusWidget(bool force, int delay = 400)
	{
		await Task.Delay(delay);
		MiniFocusWindow miniFocusWindow = MiniFocusWindow;
		if (miniFocusWindow == null)
		{
			ShowMiniFocusWindow();
		}
		else if (force || !miniFocusWindow.IsActive)
		{
			miniFocusWindow.LogWindowState("ActivateRequested", $"force={force}");
			miniFocusWindow.Show();
			miniFocusWindow.Activate();
		}
		else
		{
			miniFocusWindow?.CloseWithReason("ToggleFocusWidget");
		}
	}

	private static void ShowMiniFocusWindow()
	{
		double pixelsPerDip = VisualTreeHelper.GetDpi(App.Window).PixelsPerDip;
		double num = LocalSettings.Settings.PomoWindowDpiScale / pixelsPerDip;
		num = ((num <= 0.0) ? 1.0 : num);
		double left = (double)LocalSettings.Settings.PomoWindowLeft * num;
		double top = (double)LocalSettings.Settings.PomoWindowTop * num;
		if (LocalSettings.Settings.PomoWindowLeft == -1)
		{
			left = (int)(SystemParameters.PrimaryScreenWidth - 280.0 - 40.0);
			top = (int)(SystemParameters.PrimaryScreenHeight - 397.0 - 100.0);
		}
		bool flag = LocalSettings.Settings.PomoLocalSetting.DisplayType == "Circle";
		MiniFocusWindow miniFocusWindow = new MiniFocusWindow
		{
			Left = left,
			Top = top
		};
		miniFocusWindow.Closed += delegate
		{
			MiniFocusWindow = null;
		};
		UtilLog.Info("FocusWidgetLocation : " + $"Setting {LocalSettings.Settings.PomoWindowLeft},{LocalSettings.Settings.PomoWindowTop},System {SystemParameters.WorkArea.Width},{SystemParameters.WorkArea.Height},{flag}");
		MiniFocusWindow = miniFocusWindow;
		MiniFocusWindow.TryShowWindow();
	}

	public static async Task<bool> ExitPomo()
	{
		LocalSettings.Settings.ExtraSettings.CurrentFocus = string.Empty;
		await LocalSettings.Settings.Save(force: true);
		if (await FocusTimer.StopOrAbandon(waitSync: true))
		{
			SaveAfterClose = false;
			MiniFocusWindow?.CloseWithReason("ExitPomo");
			return true;
		}
		return false;
	}

	public static async void LoadTodayData(bool? isPomo = null, bool loadRemote = false, bool force = true)
	{
		bool flag = true;
		if (loadRemote)
		{
			if (isPomo.HasValue)
			{
				flag = ((!isPomo.Value) ? (await PomoSyncService.Pull7Days(isPomo: false)) : (await PomoSyncService.Pull7Days(isPomo: true)));
			}
			else
			{
				bool flag2 = await PomoSyncService.Pull7Days(isPomo: true);
				if (!flag2)
				{
					flag2 = await PomoSyncService.Pull7Days(isPomo: false);
				}
				flag = flag2;
			}
		}
		if (flag || force)
		{
			LoadStatistics();
		}
	}

	public static async void LoadStatistics()
	{
		try
		{
			PomoStatistics = await PomoService.LoadStatistics(IsPomo);
		}
		catch (Exception ex)
		{
			UtilLog.Error("LoadStatisticsError " + ex);
			PomoStatistics = new PomoStat();
		}
		MiniFocusWindow?.OnStatisticsChanged();
	}

	public static void OnDisplayTypeChanged()
	{
		MiniFocusWindow?.SetDisplayType();
	}

	public static void ContinueOrPause()
	{
		switch (Status)
		{
		case PomoStatus.Working:
			UserActCollectUtils.AddShortCutEvent("focus", "continue_pause");
			FocusTimer.Pause(DateTime.Now);
			break;
		case PomoStatus.Pause:
			UserActCollectUtils.AddShortCutEvent("focus", "continue_pause");
			FocusTimer.Continue(DateTime.Now);
			break;
		}
	}

	public static void StartOrDrop()
	{
		switch (Status)
		{
		case PomoStatus.WaitingWork:
			UserActCollectUtils.AddShortCutEvent("focus", "start_finish");
			FocusTimer.BeginTimer();
			break;
		case PomoStatus.Working:
		case PomoStatus.Pause:
			UserActCollectUtils.AddShortCutEvent("focus", "start_finish");
			FocusTimer.Drop();
			break;
		case PomoStatus.Relaxing:
		case PomoStatus.WaitingRelax:
			break;
		}
	}

	public static int[] GetStatisticsTypesSafely()
	{
		if (IsPomo)
		{
			string miniStatisticsTypes = LocalSettings.Settings.PomoLocalSetting.MiniStatisticsTypes;
			if (!string.IsNullOrEmpty(miniStatisticsTypes))
			{
				string[] array = miniStatisticsTypes.Split(',');
				if (array.Length == 2 && int.TryParse(array[0], out var result) && int.TryParse(array[1], out var result2) && result != result2 && MathUtil.Between(result, 0, 3) && MathUtil.Between(result2, 0, 3))
				{
					return new int[2] { result, result2 };
				}
			}
			return new int[2] { 0, 2 };
		}
		return new int[2] { 0, 2 };
	}
}
