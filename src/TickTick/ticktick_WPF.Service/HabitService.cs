using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.ReminderTime;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.Views;
using ticktick_WPF.Views.Habit;

namespace ticktick_WPF.Service;

public static class HabitService
{
	private static DateTime _lastGetSectionsTime;

	public static BitmapSource GetIcon(string iconRes, string color)
	{
		if (iconRes == null)
		{
			return null;
		}
		if (iconRes.StartsWith("habit"))
		{
			return new BitmapImage(new Uri("pack://application:,,,/Assets/Habits/" + iconRes.ToLower() + ".png"));
		}
		Border border = new Border();
		border.Width = 36.0;
		border.Height = 36.0;
		border.Background = (string.IsNullOrEmpty(color) ? Brushes.Transparent : ThemeUtil.GetColorInString(color));
		border.CornerRadius = new CornerRadius(18.0);
		border.Child = new TextBlock
		{
			FontSize = 16.0,
			Text = iconRes.Split('_').LastOrDefault(),
			Foreground = (string.IsNullOrEmpty(color) ? ThemeUtil.GetColor("PrimaryColor") : Brushes.White),
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center
		};
		return ThemeUtil.SaveAsWriteableBitmap(border);
	}

	public static BitmapSource GetWidgetCornerPic(string iconRes)
	{
		if (iconRes == null)
		{
			return null;
		}
		return new BitmapImage(new Uri("pack://application:,,,/Assets/Habits/Widget/widget_" + iconRes.ToLower() + ".png"));
	}

	public static async Task<(bool, string)> CheckSectionName(string newName)
	{
		if (string.IsNullOrEmpty(newName) || string.IsNullOrWhiteSpace(newName))
		{
			return (false, Utils.GetString("SectionNameCannotBeEmpty"));
		}
		if ((await GetHabitSections()).Exists((HabitSectionModel h) => h.DisplayName == newName || h.Name == newName))
		{
			return (false, Utils.GetString("SectionNameExisted"));
		}
		if (newName == Utils.GetString("HabitSectionOthers") || newName.ToLower() == "other")
		{
			return (false, Utils.GetString("SectionNameExisted"));
		}
		if (!string.IsNullOrEmpty(newName) && !string.IsNullOrWhiteSpace(newName) && NameUtils.IsValidColumnName(newName))
		{
			return (true, "");
		}
		return (false, Utils.GetString("SectionNotValid"));
	}

	public static async Task<List<HabitModel>> GetHabitsbyStatus(int status)
	{
		return await HabitDao.GetHabitsbyStatus(status);
	}

	public static async Task<List<HabitSectionModel>> GetHabitSections()
	{
		List<HabitSectionModel> sections = HabitSectionCache.GetSections();
		if (Utils.IsEmptyDate(_lastGetSectionsTime))
		{
			_lastGetSectionsTime = DateTime.Today.AddDays(-1.0);
		}
		TimeSpan timeSpan = DateTime.Now - _lastGetSectionsTime;
		bool flag = false;
		if (timeSpan.TotalSeconds > 59.0)
		{
			_lastGetSectionsTime = DateTime.Now;
			flag = true;
		}
		if ((sections == null || !sections.Any()) && flag)
		{
			await HabitSectionsSyncService.PullHabitSections();
			sections = HabitSectionCache.GetSections();
		}
		return sections.Where((HabitSectionModel section) => section.SyncStatus != -1).ToList();
	}

	public static async Task UpdateSection(HabitSectionModel model, bool needsNotify = true)
	{
		if (model.Id == HabitSectionModel.GetDefault().Id)
		{
			HabitSectionModel.GetDefault().IsOpen = model.IsOpen;
		}
		else
		{
			model.SyncStatus = 1;
			await HabitSectionDao.UpdateAsync(model);
		}
		DataChangedNotifier.NotifyHabitSectionChanged();
		HabitSectionsSyncService.BatchUpdateHabitSections();
	}

	public static async void UpdateSectionSortOrderById(string id, long order)
	{
		if (id == HabitSectionModel.GetDefault().Id)
		{
			HabitSectionModel.GetDefault().SortOrder = order;
			DataChangedNotifier.NotifyHabitsChanged();
			LocalSettings.Settings.HabitDefaultOrder = order;
			LocalSettings.Settings.Save();
			PushHabitSettings();
			return;
		}
		HabitSectionModel habitSectionModel = await HabitSectionDao.GetHabitSectionById(id);
		if (habitSectionModel != null)
		{
			habitSectionModel.SortOrder = order;
			habitSectionModel.SyncStatus = 1;
			await HabitSectionDao.UpdateAsync(habitSectionModel);
			await HabitSectionCache.SetSections();
			DataChangedNotifier.NotifyHabitsChanged();
			HabitSectionsSyncService.BatchUpdateHabitSections();
		}
	}

	public static async Task OnPomoTaskChanged(string habitId, long second, DateTime date)
	{
		HabitModel habitModel = await HabitDao.GetHabitById(habitId);
		if (habitModel != null && !habitModel.IsBoolHabit())
		{
			float num = 0f;
			if (FocusConstance.HourUnit.Contains(habitModel.Unit.Trim().ToLower()))
			{
				num = (float)second / 3600f;
			}
			if (FocusConstance.MinuteUnit.Contains(habitModel.Unit.Trim().ToLower()))
			{
				num = (float)second / 60f;
			}
			if (num > 0f)
			{
				await CheckInHabit(habitModel.Id, date, Math.Round(num, 2));
			}
		}
	}

	public static async void DeleteSection(HabitSectionModel model)
	{
		List<HabitModel> habits = await HabitDao.GetHabitsBySectionId(model.Id);
		string otherSectionId = HabitSectionModel.GetDefault().Id;
		List<HabitModel> list = await HabitDao.GetHabitsBySectionId(otherSectionId);
		long num = 268435456L;
		if (list != null && list.Any())
		{
			num = list.Select((HabitModel h) => h.SortOrder).Min();
		}
		foreach (HabitModel item in habits)
		{
			item.SyncStatus = ((item.SyncStatus != 0) ? 1 : 0);
			item.SectionId = otherSectionId;
			item.SortOrder = num - 268435456;
			num -= 268435456;
		}
		await HabitDao.UpdateAllAsync(habits);
		HabitSyncService.CommitHabits();
		model.SyncStatus = -1;
		await HabitSectionDao.UpdateAsync(model);
		await HabitSectionCache.SetSections();
		HabitSectionsSyncService.BatchUpdateHabitSections();
		DataChangedNotifier.NotifyHabitsChanged();
	}

	public static async void DeleteSectionById(string sectionId)
	{
		HabitSectionModel habitSectionModel = await HabitSectionDao.GetHabitSectionById(sectionId);
		if (habitSectionModel != null)
		{
			DeleteSection(habitSectionModel);
		}
	}

	public static async Task AddSections(List<HabitSectionModel> models)
	{
		List<HabitSectionModel> sections = HabitSectionCache.GetSections();
		sections.Add(HabitSectionModel.GetDefault());
		models.ForEach(delegate(HabitSectionModel model)
		{
			model.SyncStatus = 0;
			model.ModifiedTime = DateTime.Now;
			if (sections != null && sections.Any())
			{
				model.SortOrder = sections.Select((HabitSectionModel item) => item.SortOrder).Max() + 268435456;
			}
			else
			{
				model.SortOrder = 0L;
			}
			sections.Add(model);
		});
		await HabitSectionDao.InsertAllAsync(models);
		await HabitSectionCache.SetSections();
		HabitSectionsSyncService.BatchUpdateHabitSections();
		DataChangedNotifier.NotifyHabitsChanged();
	}

	public static async Task<string> GetSectionDefaultTime(string sectionName)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>
		{
			{
				Utils.GetString("HabitSectionMorning"),
				"09:00"
			},
			{
				Utils.GetString("HabitSectionAfternoon"),
				"13:00"
			},
			{
				Utils.GetString("HabitSectionNight"),
				"20:00"
			}
		};
		dictionary["morning"] = "09:00";
		dictionary["afternoon"] = "13:00";
		dictionary["night"] = "20:00";
		string key = sectionName.TrimEnd().TrimStart().ToLower();
		return dictionary.ContainsKey(key) ? dictionary[key] : string.Empty;
	}

	public static async Task InitSections()
	{
		await AddSections(new List<HabitSectionModel>
		{
			new HabitSectionModel("_morning"),
			new HabitSectionModel("_afternoon"),
			new HabitSectionModel("_night")
		});
	}

	public static async Task<bool> PullRemoteCheckinAndRecordData(HabitModel habit)
	{
		if (habit != null)
		{
			int checkStamp = habit.CheckStamp;
			int lastSeasonStamp = ticktick_WPF.Util.DateUtils.GetDateNum(DateTime.Today.AddDays(-91.0));
			if (habit.CheckStamp > 0)
			{
				checkStamp = Math.Min(lastSeasonStamp, checkStamp);
			}
			bool checkInChanged = await PullRemoteCheckIn(habit.Id, checkStamp);
			bool recordChanged = await PullRemoteHabitRecord(habit.Id, checkStamp);
			bool localDirty = await HabitCheckInDao.HandleLocalDirtyCheckIns(habit.Id);
			await HabitDao.SaveHabitCheckStamp(habit.Id, lastSeasonStamp);
			return checkInChanged || recordChanged || localDirty;
		}
		return false;
	}

	public static async Task<bool> PullAndMergeRemoteHabitSections()
	{
		List<HabitSectionModel> list = await Communicator.PullRemoteHabitSections();
		if (list != null)
		{
			return await MergeRemoteHabitSections(list);
		}
		return false;
	}

	public static async Task<List<HabitModel>> PullRemoteHabits(bool needMerge = true)
	{
		List<HabitModel> habits = await Communicator.PullRemoteHabits();
		if (habits != null)
		{
			if (needMerge && await MergeRemoteHabits(habits))
			{
				DataChangedNotifier.NotifyHabitsChanged();
			}
			UtilLog.Info($"PullRemoteHabits : Count {habits.Count}");
		}
		return habits;
	}

	public static async Task<HabitCheckInModel> CheckInHabit(string habitId, DateTime date, double step = -1.0, bool increment = true, bool checkShowRecord = true, int delay = 0, IToastShowWindow window = null, object sender = null, HabitAction action = HabitAction.None)
	{
		window = window ?? Utils.GetToastWindow();
		bool isRealArchived = false;
		HabitModel habit = await HabitDao.GetHabitById(habitId);
		if (habit != null)
		{
			if (IsInvalidCheckInAction(habit, step, increment, action))
			{
				UtilLog.Warn($"Ignore invalid habit check-in {habit.Id},{ticktick_WPF.Util.DateUtils.GetDateNum(date)},step:{step},action:{action}");
				return null;
			}
			bool needRecord = true;
			if (!increment)
			{
				HabitCheckInModel habitCheckInModel = await HabitCheckInDao.GetHabitCheckInsByHabitIdAndStamp(habitId, date.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
				if (habitCheckInModel != null && habitCheckInModel.Value >= habitCheckInModel.Goal)
				{
					needRecord = false;
				}
			}
			HabitCheckInModel checkIn = await CheckInHabit(habit, date, step, increment, action);
			if (checkIn == null)
			{
				return null;
			}
			if (checkIn != null && DateTime.Now.Hour < 3)
			{
				HabitUtils.WhenCheckInMidnight(habit.Reminder, window);
			}
			UtilLog.Info("CheckInHabit " + habitId + "," + ticktick_WPF.Util.DateUtils.GetDateNum(date) + "," + checkIn?.Value + "," + checkIn?.Goal);
			if (checkIn != null && checkIn.Value >= checkIn.Goal)
			{
				if (habit.RecordEnable == true && needRecord && checkShowRecord)
				{
					await HabitUtils.ShowHabitRecordWindow(habitId, habit.Name, date, isEdit: false);
				}
				if (ticktick_WPF.Util.DateUtils.ParseDateTime(checkIn.CheckinStamp) >= ticktick_WPF.Util.DateUtils.ParseDateTime(habit.TargetStartDate?.ToString()) && habit.TargetDays > 0 && habit.CompletedCyclesList?.LastOrDefault()?.Streak == habit.TargetDays - 1)
				{
					string content = string.Format(Utils.GetString("HabitCompleteCycleInfo"), habit.TargetDays);
					CustomerDialog customerDialog = new CustomerDialog(null, content, Utils.GetString("HabitContinue"), Utils.GetString("EndAndArchive"));
					try
					{
						customerDialog.Owner = ((window is FrameworkElement dependencyObject) ? Window.GetWindow(dependencyObject) : App.Window);
					}
					catch (Exception)
					{
					}
					bool isClosed = false;
					customerDialog.CloseClick += delegate
					{
						isClosed = true;
					};
					if (!customerDialog.ShowDialog() == true && !isClosed)
					{
						isRealArchived = true;
						(window ?? App.Window).TryToastString(null, Utils.GetString("Archived"));
						DataChangedNotifier.NotifyHabitArchived(habitId);
						await Task.Delay(100);
						await HabitDao.ChangeHabitArchiveStatus(habitId, isArchive: true);
					}
				}
			}
			await HabitDao.UpdateHabitInfo(habitId);
			UnSkipHabit(habit.Id, date);
			if (delay > 0)
			{
				await Task.Delay(delay);
			}
			DataChangedNotifier.NotifyHabitCyclesChanged(habit.Id);
			DataChangedNotifier.NotifyHabitCheckInChanged(checkIn, sender);
			if (isRealArchived)
			{
				DataChangedNotifier.NotifyHabitsChanged();
			}
			HabitSyncService.CommitHabitCheckIns();
			HabitSyncService.CommitHabits();
			return checkIn;
		}
		return null;
	}

	public static async Task<bool> ResetCheckInHabit(string habitId, DateTime date, string type = "boolean", object sender = null)
	{
		HabitModel habit = await HabitDao.GetHabitById(habitId);
		if (habit != null)
		{
			if (!habit.IsBooleanType() && (int)habit.Step != 0)
			{
				bool? flag = new CustomerDialog(Utils.GetString("ResetCheckIn"), string.Format(Utils.GetString("ResetCheckInHint"), ticktick_WPF.Util.DateUtils.FormatMonthDay(date, DateFormatLen.Long)), Utils.GetString("Reset"), Utils.GetString("Cancel")).ShowDialog();
				if (!flag.HasValue || !flag.Value)
				{
					return false;
				}
			}
			await ResetCheckInHabit(habitId, date);
			await HabitDao.UpdateHabitInfo(habitId);
			DataChangedNotifier.NotifyHabitCyclesChanged(habit.Id);
			DataChangedNotifier.NotifyHabitCheckInChanged(new HabitCheckInModel
			{
				HabitId = habitId,
				CheckinStamp = date.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
				Goal = habit.Goal,
				Value = 0.0
			}, sender);
			HabitSyncService.CommitHabitCheckIns();
			HabitSyncService.CommitHabits();
		}
		return true;
	}

	public static async Task<bool> PullRemoteHabitRecord(string habitId, int stamp)
	{
		List<HabitRecordModel> list = await Communicator.PullHabitRecords(habitId, stamp);
		if (list != null)
		{
			return await MergeRemoteHabitRecords(habitId, list, stamp);
		}
		return false;
	}

	public static async Task<bool> PullRemoteHabitsCheckIn(List<string> habitIds, int checkStamp = 0)
	{
		bool changed = false;
		CheckInCollection collection = await Communicator.PullHabitsCheckIns(habitIds, checkStamp);
		if (collection?.CheckIns != null && collection.CheckIns.Any())
		{
			foreach (string key in collection.CheckIns.Keys)
			{
				bool flag = await MergeRemoteHabitCheckIns(key, collection.CheckIns[key], checkStamp);
				changed = changed || flag;
			}
			UtilLog.Info("PullRemoteHabitsCheckIn" + checkStamp + "   " + collection.CheckIns.Count + "   " + changed);
		}
		return changed;
	}

	public static async Task PullRemoteHabitsRecord(List<string> habitIds, int checkStamp = 0)
	{
		await MergeRemoteRecords(await Communicator.PullHabitsRecords(habitIds, checkStamp), checkStamp);
	}

	public static async Task<bool> PullRemoteCheckIn(string habitId, int stamp)
	{
		return await MergeRemoteHabitCheckIns(habitId, await Communicator.PullHabitCheckIns(habitId, stamp), stamp);
	}

	public static async Task DeleteCheckInRecord(string logId)
	{
		await HabitRecordDao.DeleteHabitRecord(logId);
		SyncManager.TryDelaySync();
		DataChangedNotifier.NotifyHabitLogChanged();
	}

	public static async Task SaveHabitCheckinRecord(string logId, string text, int score)
	{
		await HabitRecordDao.SaveHabitCheckinRecord(logId, text, score);
		SyncManager.TryDelaySync();
		DataChangedNotifier.NotifyHabitLogChanged();
	}

	public static async Task AddCheckInRecord(CheckInLogViewModel vm)
	{
		await HabitRecordDao.AddCheckInRecord(vm);
		SyncManager.TryDelaySync();
		DataChangedNotifier.NotifyHabitLogChanged();
	}

	public static async Task PullRemoteConfig()
	{
		HabitSettingsModel config = await Communicator.GetRemoteHabitConfig();
		if (config != null && string.IsNullOrEmpty(config.errorId))
		{
			LocalSettings.Settings.HabitInToday = config.ShowInToday == true;
			long habitDefaultOrder = config.DefaultSection?.SortOrder ?? (await HabitSectionDao.GetMaxSortOrder());
			LocalSettings.Settings.HabitDefaultOrder = habitDefaultOrder;
			LocalSettings.Settings.Save();
			HabitSectionModel.GetDefault().SortOrder = config.DefaultSection?.SortOrder ?? HabitSectionModel.GetDefault().SortOrder;
		}
	}

	public static async Task PushHabitSettings()
	{
		Communicator.PushHabitConfig(new HabitSettingsModel
		{
			ShowInCalendar = LocalSettings.Settings.HabitInCal,
			ShowInToday = LocalSettings.Settings.HabitInToday,
			DefaultSection = new HabitDefaultSection
			{
				SortOrder = LocalSettings.Settings.HabitDefaultOrder
			},
			Enabled = LocalSettings.Settings.ShowHabit
		});
	}

	public static async Task UncompleteHabit(string habitId, DateTime date, object sender = null)
	{
		HabitModel habit = await HabitDao.GetHabitById(habitId);
		if (habit != null)
		{
			HabitCheckInModel checkIn = await HabitCheckInDao.GetHabitCheckInByHabitIdAndStamp(habitId, date.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
			if (checkIn == null || checkIn.Status == -1)
			{
				checkIn = new HabitCheckInModel
				{
					Id = Utils.GetGuid(),
					HabitId = habit.Id,
					UserId = Utils.GetCurrentUserIdInt().ToString(),
					CheckinStamp = date.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
					Value = 0.0,
					Status = 0,
					CheckStatus = 1,
					CheckinTime = DateTime.Now,
					Goal = habit.Goal
				};
				checkIn.opTime = DateTime.Now;
				await HabitCheckInDao.InsertAsync(checkIn);
			}
			else
			{
				checkIn.CheckStatus = 1;
				checkIn.Status = ((checkIn.Status != 0) ? 1 : 0);
				checkIn.opTime = DateTime.Now;
				await HabitCheckInDao.UpdateAsync(checkIn);
			}
			if (DateTime.Now.Hour < 3)
			{
				HabitUtils.WhenCheckInMidnight(habit.Reminder);
			}
			if (habit.RecordEnable == true)
			{
				HabitUtils.ShowHabitRecordWindow(habit.Id, habit.Name, date, isEdit: false, unCompleted: true);
			}
			await HabitDao.UpdateHabitInfo(habitId);
			UnSkipHabit(habitId, date);
			DataChangedNotifier.NotifyHabitCyclesChanged(habit.Id);
			DataChangedNotifier.NotifyHabitCheckInChanged(checkIn, sender);
			HabitSyncService.CommitHabitCheckIns();
			HabitSyncService.CommitHabits();
		}
	}

	public static async Task<HabitCheckInModel> CheckInHabit(HabitModel habit, DateTime date, double step = -1.0, bool increment = true, HabitAction action = HabitAction.None)
	{
		HabitCheckInModel result = null;
		if (habit != null)
		{
			if (IsInvalidCheckInAction(habit, step, increment, action))
			{
				UtilLog.Warn($"Ignore invalid habit check-in {habit.Id},{ticktick_WPF.Util.DateUtils.GetDateNum(date)},step:{step},action:{action}");
				return null;
			}
			result = ((!habit.IsBooleanType()) ? (await CheckInRealHabit(habit, date, step, increment)) : (await CheckInBoolHabit(habit, date)));
			UtilLog.Info($"CheckInHabit {habit.Id},{date},type:{habit.Type},step:{step},{increment}");
		}
		return result;
	}

	internal static bool IsInvalidRealIncrement(HabitModel habit, double step, bool increment)
	{
		if (habit != null && !habit.IsBooleanType() && increment && step <= 0.0)
		{
			return Math.Abs(habit.Step) > 0.0;
		}
		return false;
	}

	internal static bool IsInvalidCheckInAction(HabitModel habit, double step, bool increment, HabitAction action)
	{
		if (habit == null)
		{
			return false;
		}
		if (action != HabitAction.None)
		{
			HabitCheckInType habitCheckInType = HabitUtils.GetHabitCheckInType(0, habit.Type, habit.Step, habit.Goal);
			if (action switch
			{
				HabitAction.CheckIn => (habitCheckInType == HabitCheckInType.BoolUnCompleted || habitCheckInType == HabitCheckInType.BoolCompleted || habitCheckInType == HabitCheckInType.CompletedAllUnCompleted || habitCheckInType == HabitCheckInType.CompletedAllCompleted) ? 1 : 0, 
				HabitAction.AutoRecord => ((habitCheckInType == HabitCheckInType.AutoUncompleted || habitCheckInType == HabitCheckInType.AutoCompleted) && Math.Abs(step - habit.Step) < 1E-05) ? 1 : 0, 
				HabitAction.ManualRecord => (habitCheckInType == HabitCheckInType.ManualUnCompleted || habitCheckInType == HabitCheckInType.ManualCompleted || habitCheckInType == HabitCheckInType.AutoUncompleted || habitCheckInType == HabitCheckInType.AutoCompleted) ? 1 : 0, 
				_ => 0, 
			} == 0)
			{
				return true;
			}
		}
		return IsInvalidRealIncrement(habit, step, increment);
	}

	private static async Task<HabitCheckInModel> CheckInRealHabit(HabitModel habit, DateTime date, double step, bool increment)
	{
		HabitCheckInType type = HabitUtils.GetHabitCheckInType(0, habit.Type, habit.Step, habit.Goal);
		HabitCheckInModel checkIn = await HabitCheckInDao.GetHabitCheckInByHabitIdAndStamp(habit.Id, date.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
		if (checkIn == null || checkIn.Status == -1)
		{
			double num = step;
			if (type == HabitCheckInType.CompletedAllUnCompleted)
			{
				num = ((increment && step <= 0.0) ? habit.Goal : step);
			}
			bool flag = num >= habit.Goal;
			HabitCheckInModel model = new HabitCheckInModel
			{
				Id = Utils.GetGuid(),
				HabitId = habit.Id,
				UserId = Utils.GetCurrentUserIdInt().ToString(),
				CheckinStamp = date.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
				Value = num,
				Status = 0,
				CheckStatus = (flag ? 2 : 0),
				Goal = habit.Goal
			};
			if (flag)
			{
				model.CheckinTime = DateTime.Now;
			}
			else
			{
				model.CheckinTime = null;
			}
			model.opTime = DateTime.Now;
			await HabitCheckInDao.InsertAsync(model);
			return model;
		}
		if (checkIn.Value < 0.0)
		{
			checkIn.Value = 0.0;
		}
		if (increment)
		{
			if (type == HabitCheckInType.CompletedAllUnCompleted && step <= 0.0)
			{
				checkIn.Value = checkIn.Goal;
			}
			else
			{
				checkIn.Value += step;
			}
		}
		else
		{
			checkIn.Value = step;
			if (date.Date == DateTime.Today && Math.Abs(checkIn.Value) < 1E-05)
			{
				checkIn.Goal = habit.Goal;
			}
		}
		checkIn.Status = ((checkIn.Status != 0) ? 1 : 0);
		if (checkIn.Value >= checkIn.Goal)
		{
			checkIn.CheckinTime = DateTime.Now;
			checkIn.CheckStatus = 2;
		}
		else
		{
			checkIn.CheckinTime = null;
			checkIn.CheckStatus = 0;
		}
		checkIn.opTime = DateTime.Now;
		await HabitCheckInDao.UpdateAsync(checkIn);
		return checkIn;
	}

	private static async Task<HabitCheckInModel> CheckInBoolHabit(HabitModel habit, DateTime date)
	{
		HabitCheckInModel checkIn = await HabitCheckInDao.GetHabitCheckInByHabitIdAndStamp(habit.Id, date.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
		if (checkIn == null || checkIn.Status == -1)
		{
			HabitCheckInModel model = new HabitCheckInModel
			{
				Id = Utils.GetGuid(),
				HabitId = habit.Id,
				UserId = Utils.GetCurrentUserIdInt().ToString(),
				CheckinStamp = date.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
				CheckinTime = DateTime.Now,
				Value = 1.0,
				Status = 0,
				Goal = 1.0,
				CheckStatus = 2
			};
			model.opTime = DateTime.Now;
			await HabitCheckInDao.InsertAsync(model);
			return model;
		}
		if (Math.Abs(checkIn.Value) < 0.0001)
		{
			checkIn.Value = checkIn.Goal;
		}
		else
		{
			checkIn.Goal = checkIn.Value;
		}
		checkIn.CheckStatus = 2;
		checkIn.Status = 1;
		checkIn.CheckinTime = DateTime.Now;
		checkIn.opTime = DateTime.Now;
		await HabitCheckInDao.UpdateAsync(checkIn);
		return checkIn;
	}

	public static async Task ResetCheckInHabit(string habitId, DateTime date)
	{
		HabitCheckInModel habitCheckInModel = await HabitCheckInDao.GetHabitCheckInByHabitIdAndStamp(habitId, date.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
		if (habitCheckInModel != null)
		{
			if (habitCheckInModel.Status == 0)
			{
				await HabitCheckInDao.DeleteAsync(habitCheckInModel);
				return;
			}
			habitCheckInModel.Value = 0.0;
			habitCheckInModel.CheckStatus = 0;
			habitCheckInModel.Status = -1;
			habitCheckInModel.opTime = DateTime.Now;
			await HabitCheckInDao.UpdateAsync(habitCheckInModel);
		}
	}

	public static async Task<bool> MergeRemoteHabitSections(List<HabitSectionModel> remoteSections)
	{
		List<HabitSectionModel> added = new List<HabitSectionModel>();
		List<HabitSectionModel> updated = new List<HabitSectionModel>();
		List<HabitSectionModel> deleted = new List<HabitSectionModel>();
		List<HabitSectionModel> list = (await HabitSectionDao.GetAllHabitSections()) ?? new List<HabitSectionModel>();
		if (remoteSections.Any())
		{
			foreach (HabitSectionModel remote in remoteSections)
			{
				HabitSectionModel habitSectionModel = list.FirstOrDefault((HabitSectionModel habit) => habit.Id == remote.Id);
				if (habitSectionModel != null)
				{
					if (habitSectionModel.Etag != remote.Etag && habitSectionModel.SyncStatus == 2)
					{
						remote._Id = habitSectionModel._Id;
						remote.UserId = Utils.GetCurrentUserIdInt().ToString();
						remote.SyncStatus = 2;
						updated.Add(remote);
					}
					list.Remove(habitSectionModel);
				}
				else
				{
					remote.UserId = Utils.GetCurrentUserIdInt().ToString();
					remote.SyncStatus = 2;
					added.Add(remote);
				}
			}
			if (list.Any())
			{
				deleted.AddRange(list.Where((HabitSectionModel local) => local.SyncStatus == 2));
			}
			if (added.Any())
			{
				await App.Connection.InsertAllAsync(added);
			}
			if (updated.Any())
			{
				await App.Connection.UpdateAllAsync(updated);
			}
			if (deleted.Any())
			{
				foreach (HabitSectionModel item in deleted)
				{
					await App.Connection.DeleteAsync(item);
				}
			}
		}
		else if (list.Any())
		{
			foreach (HabitSectionModel item2 in list)
			{
				if (item2.SyncStatus != 0)
				{
					await App.Connection.DeleteAsync(item2);
				}
			}
			return true;
		}
		return added.Any() || updated.Any() || deleted.Any();
	}

	public static async Task MergeRemoteRecords(HabitRecordCheckResult remoteRecords, int checkStamp)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		List<HabitRecordModel> list = await HabitRecordDao.GetHabitRecords(checkStamp);
		List<HabitRecordModel> list2 = new List<HabitRecordModel>();
		List<HabitRecordModel> updated = new List<HabitRecordModel>();
		List<HabitRecordModel> deleted = new List<HabitRecordModel>();
		if (remoteRecords?.HabitRecords != null && remoteRecords.HabitRecords.Any())
		{
			foreach (List<HabitRecordModel> value in remoteRecords.HabitRecords.Values)
			{
				foreach (HabitRecordModel record in value)
				{
					HabitRecordModel habitRecordModel = list.FirstOrDefault((HabitRecordModel model) => model.Stamp == record.Stamp && model.HabitId == record.HabitId);
					if (habitRecordModel != null)
					{
						if (habitRecordModel.Status == 0 || habitRecordModel.Status == 1)
						{
							list.Remove(habitRecordModel);
							continue;
						}
						if ((habitRecordModel.Content != record.Content || habitRecordModel.Emoji != record.Emoji) && habitRecordModel.Deleted != 1)
						{
							habitRecordModel.Content = record.Content;
							habitRecordModel.Emoji = record.Emoji;
							updated.Add(habitRecordModel);
						}
						list.Remove(habitRecordModel);
					}
					else
					{
						record.UserId = userId;
						record.Status = 2;
						record.Deleted = 0;
						list2.Add(record);
					}
				}
			}
		}
		if (list.Any())
		{
			deleted.AddRange(list.Where((HabitRecordModel habitRecordModel2) => habitRecordModel2.Status == 2));
		}
		if (list2.Any())
		{
			await App.Connection.InsertAllAsync(list2);
		}
		if (updated.Any())
		{
			await App.Connection.UpdateAllAsync(updated);
		}
		if (!deleted.Any())
		{
			return;
		}
		foreach (HabitRecordModel item in deleted)
		{
			await App.Connection.DeleteAsync(item);
		}
	}

	public static async Task HandleTodayCheckIn(HabitModel current, HabitModel modify, bool isPull = false)
	{
		if (!(current.Type != modify.Type) && !(Math.Abs(current.Goal - modify.Goal) > 0.0001))
		{
			return;
		}
		string userId = Utils.GetCurrentUserIdInt().ToString();
		string stamp = DateTime.Today.ToString("yyyyMMdd");
		HabitCheckInModel todayCheckIn = await (from v in App.Connection.Table<HabitCheckInModel>()
			where v.UserId == userId && v.HabitId == modify.Id && v.CheckinStamp == stamp
			select v).FirstOrDefaultAsync();
		if (todayCheckIn == null)
		{
			return;
		}
		bool needLog = todayCheckIn.Value < todayCheckIn.Goal;
		if (modify.IsBoolHabit() && !current.IsBoolHabit())
		{
			bool flag = todayCheckIn.Value >= todayCheckIn.Goal;
			todayCheckIn.Goal = 1.0;
			if (todayCheckIn.Value > 0.0)
			{
				todayCheckIn.Value = 1.0;
				if (!flag)
				{
					todayCheckIn.CheckinTime = DateTime.Now;
				}
				todayCheckIn.CheckStatus = 2;
			}
		}
		else if (!modify.IsBoolHabit() && current.IsBoolHabit())
		{
			if (!(todayCheckIn.Value < todayCheckIn.Goal))
			{
				return;
			}
			todayCheckIn.Goal = modify.Goal;
			todayCheckIn.Value = 0.0;
			todayCheckIn.CheckinTime = null;
		}
		else if (!modify.IsBoolHabit() && !current.IsBoolHabit())
		{
			bool flag2 = todayCheckIn.Value >= todayCheckIn.Goal;
			todayCheckIn.Goal = modify.Goal;
			if (todayCheckIn.Value >= todayCheckIn.Goal)
			{
				todayCheckIn.CheckinTime = (flag2 ? todayCheckIn.CheckinTime : new DateTime?(DateTime.Now));
				todayCheckIn.CheckStatus = 2;
			}
			else
			{
				todayCheckIn.CheckStatus = ((todayCheckIn.CheckStatus != 2) ? todayCheckIn.CheckStatus : 0);
				if (todayCheckIn.CheckStatus != 1)
				{
					todayCheckIn.CheckinTime = null;
				}
			}
		}
		if (!isPull)
		{
			todayCheckIn.Status = ((todayCheckIn.Status != 0) ? 1 : 0);
			await App.Connection.UpdateAsync(todayCheckIn);
			if (modify.RecordEnable == true && needLog && todayCheckIn.Value >= todayCheckIn.Goal)
			{
				HabitUtils.ShowHabitRecordWindow(modify.Id, modify.Name, DateTime.Today);
			}
		}
		else
		{
			await App.Connection.UpdateAsync(todayCheckIn);
		}
	}

	public static async Task UpdateHabit(HabitModel habit)
	{
		HabitModel exist = await HabitDao.GetHabitById(habit.Id);
		if (exist != null)
		{
			await HandleTodayCheckIn(exist, habit);
			exist.Name = habit.Name;
			exist.IconRes = habit.IconRes;
			exist.Color = habit.Color;
			exist.RepeatRule = habit.RepeatRule;
			exist.Type = habit.Type;
			exist.Goal = habit.Goal;
			exist.Step = habit.Step;
			exist.Unit = habit.Unit;
			exist.Reminder = habit.Reminder;
			exist.AnnoyingAlert = habit.AnnoyingAlert;
			exist.Status = habit.Status;
			exist.SyncStatus = ((exist.SyncStatus != 0) ? 1 : 0);
			exist.RecordEnable = habit.RecordEnable;
			exist.ModifiedTime = DateTime.Now;
			HabitModel habitModel = exist;
			habitModel.TotalCheckIns = await HabitDao.GetHabitTotalCheckCount(exist.Id);
			exist.TargetDays = habit.TargetDays;
			exist.TargetStartDate = habit.TargetStartDate;
			habitModel = exist;
			habitModel.CompletedCyclesList = await HabitDao.GetHabitCompletedCycles(habit);
			exist.CompletedCycles = exist.CompletedCyclesList.Count((CompletedCycle c) => c.isComplete);
			exist.SectionId = habit.SectionId;
			await App.Connection.UpdateAsync(exist);
			HabitSyncService.CommitHabits();
		}
		else
		{
			habit.UserId = LocalSettings.Settings.LoginUserId;
			habit.Id = (string.IsNullOrEmpty(habit.Id) ? Utils.GetGuid() : habit.Id);
			habit.SyncStatus = habit.SyncStatus;
			habit.ModifiedTime = DateTime.Now;
			await App.Connection.InsertAsync(habit);
			HabitSyncService.CommitHabits(immediately: true);
		}
		HabitSyncService.CommitHabitCheckIns();
	}

	public static async Task<bool> MergeRemoteHabits(List<HabitModel> remoteHabits)
	{
		List<HabitModel> list = (await HabitDao.GetAllHabits()) ?? new List<HabitModel>();
		foreach (HabitModel local in list)
		{
			HabitModel habitModel = remoteHabits.FirstOrDefault((HabitModel habit) => habit.Id == local.Id);
			if (habitModel != null)
			{
				HandleTodayCheckIn(local, habitModel, isPull: true);
			}
		}
		List<HabitModel> added = new List<HabitModel>();
		List<HabitModel> updated = new List<HabitModel>();
		List<HabitModel> deleted = new List<HabitModel>();
		if (remoteHabits.Any())
		{
			foreach (HabitModel remote in remoteHabits)
			{
				HabitModel habitModel2 = list.FirstOrDefault((HabitModel habit) => habit.Id == remote.Id);
				if (habitModel2 != null)
				{
					if (habitModel2.SyncStatus == 2 && (habitModel2.Etag != remote.Etag || habitModel2.AnnoyingAlert != remote.AnnoyingAlert))
					{
						remote._Id = habitModel2._Id;
						remote.UserId = Utils.GetCurrentUserIdInt().ToString();
						remote.SyncStatus = 2;
						if (remote.Type != habitModel2.Type)
						{
							UtilLog.Info("HabitTypeChanged " + remote.Id + " remote :" + remote.Type + " local :" + habitModel2.Type);
						}
						updated.Add(remote);
					}
					list.Remove(habitModel2);
				}
				else
				{
					remote.UserId = Utils.GetCurrentUserIdInt().ToString();
					remote.SyncStatus = 2;
					if (remote.Type != "Boolean" && remote.Type != "Real")
					{
						UtilLog.Info("HabitTypeError " + remote.Id + " remote :" + remote.Type);
					}
					added.Add(remote);
				}
			}
			if (list.Any())
			{
				deleted.AddRange(list.Where((HabitModel habitModel3) => habitModel3.SyncStatus == 2));
			}
			if (added.Any())
			{
				await App.Connection.InsertAllAsync(added);
			}
			if (updated.Any())
			{
				await App.Connection.UpdateAllAsync(updated);
			}
			if (deleted.Any())
			{
				foreach (HabitModel item in deleted)
				{
					await App.Connection.DeleteAsync(item);
				}
			}
		}
		else if (list.Any())
		{
			foreach (HabitModel item2 in list)
			{
				if (item2.SyncStatus != 0)
				{
					await App.Connection.DeleteAsync(item2);
				}
			}
			return true;
		}
		return added.Any() || updated.Any() || deleted.Any();
	}

	public static async Task<bool> MergeRemoteHabitCheckIns(string habitId, List<HabitCheckInModel> checkIns, int afterStamp)
	{
		if (checkIns == null)
		{
			return false;
		}
		List<HabitCheckInModel> added = new List<HabitCheckInModel>();
		List<HabitCheckInModel> updated = new List<HabitCheckInModel>();
		List<HabitCheckInModel> deleted = new List<HabitCheckInModel>();
		List<HabitCheckInModel> list = await HabitCheckInDao.GetHabitCheckInsByHabitIdAndStamp(habitId, afterStamp);
		string userId = Utils.GetCurrentUserIdInt().ToString();
		List<string> list2 = new List<string>();
		int i;
		for (i = checkIns.Count - 1; i >= 0; i--)
		{
			bool flag = HabitCheckInDao.NormalizeNegativeValue(checkIns[i], markForSync: true);
			if (list2.Contains(checkIns[i].CheckinStamp))
			{
				HabitCheckInModel habitCheckInModel = list.FirstOrDefault((HabitCheckInModel model) => model.Id == checkIns[i].Id);
				if (habitCheckInModel != null)
				{
					habitCheckInModel.Status = -1;
					updated.Add(habitCheckInModel);
					list.Remove(habitCheckInModel);
				}
				else
				{
					checkIns[i].UserId = userId;
					checkIns[i].Status = -1;
					added.Add(checkIns[i]);
				}
				continue;
			}
			list2.Add(checkIns[i].CheckinStamp);
			HabitCheckInModel habitCheckInModel2 = list.FirstOrDefault((HabitCheckInModel model) => model.Id == checkIns[i].Id);
			if (habitCheckInModel2 != null)
			{
				if (habitCheckInModel2.Status != 2)
				{
					list.Remove(habitCheckInModel2);
					continue;
				}
				if (Math.Abs(habitCheckInModel2.Value - checkIns[i].Value) > 0.0 || Math.Abs(habitCheckInModel2.Goal - checkIns[i].Goal) > 0.0 || habitCheckInModel2.CheckStatus != checkIns[i].CheckStatus)
				{
					habitCheckInModel2.Value = checkIns[i].Value;
					habitCheckInModel2.Goal = checkIns[i].Goal;
					habitCheckInModel2.CheckinTime = checkIns[i].CheckinTime;
					habitCheckInModel2.CheckStatus = checkIns[i].CheckStatus;
					updated.Add(habitCheckInModel2);
				}
				if (flag)
				{
					habitCheckInModel2.Status = ((habitCheckInModel2.Status != 0) ? 1 : 0);
					habitCheckInModel2.opTime = DateTime.Now;
					if (!updated.Contains(habitCheckInModel2))
					{
						updated.Add(habitCheckInModel2);
					}
				}
				list.Remove(habitCheckInModel2);
			}
			else
			{
				checkIns[i].UserId = userId;
				checkIns[i].Status = (flag ? 1 : 2);
				added.Add(checkIns[i]);
			}
		}
		if (list.Any())
		{
			deleted.AddRange(list.Where((HabitCheckInModel record) => record.Status == 2));
		}
		string log = "MergeHCI id:" + habitId;
		if (added.Any())
		{
			await App.Connection.InsertAllAsync(added);
			log += " ,add :";
			log = added.Aggregate(log, (string current, HabitCheckInModel checkIn) => current + $" {checkIn.CheckinStamp}-{checkIn.CheckStatus},");
		}
		if (updated.Any())
		{
			await App.Connection.UpdateAllAsync(updated);
			log += " ,update :";
			log = updated.Aggregate(log, (string current, HabitCheckInModel checkIn) => current + $" {checkIn.CheckinStamp}-{checkIn.CheckStatus},");
		}
		if (deleted.Any())
		{
			log += " ,delete :";
			foreach (HabitCheckInModel delete in deleted)
			{
				await App.Connection.DeleteAsync(delete);
				log = log + delete.CheckinStamp + ", ";
			}
		}
		bool isModify = added.Any() || updated.Any() || deleted.Any();
		if (isModify)
		{
			await HabitDao.UpdateHabitInfo(habitId, isUpdateSyncStatus: false);
			UtilLog.Info(log);
			HabitCheckinCache.OnHabitCheckInChanged(null, habitId);
		}
		return isModify;
	}

	public static async Task<bool> MergeRemoteHabitRecords(string habitId, IEnumerable<HabitRecordModel> records, int stamp)
	{
		List<HabitRecordModel> added = new List<HabitRecordModel>();
		List<HabitRecordModel> updated = new List<HabitRecordModel>();
		List<HabitRecordModel> deleted = new List<HabitRecordModel>();
		List<HabitRecordModel> list = await HabitRecordDao.GetHabitRecordsByHabitId(habitId, stamp);
		string userId = Utils.GetCurrentUserIdInt().ToString();
		foreach (HabitRecordModel record in records)
		{
			HabitRecordModel habitRecordModel = list.FirstOrDefault((HabitRecordModel model) => model.Stamp == record.Stamp);
			if (habitRecordModel != null)
			{
				if (habitRecordModel.Status == 0 || habitRecordModel.Status == 1)
				{
					list.Remove(habitRecordModel);
					continue;
				}
				if (habitRecordModel.Content != record.Content && habitRecordModel.Deleted != 1)
				{
					habitRecordModel.Content = record.Content;
					updated.Add(habitRecordModel);
				}
				list.Remove(habitRecordModel);
			}
			else
			{
				record.UserId = userId;
				record.Status = 2;
				record.Deleted = 0;
				added.Add(record);
			}
		}
		if (list.Any())
		{
			deleted.AddRange(list.Where((HabitRecordModel habitRecordModel2) => habitRecordModel2.Status == 2));
		}
		if (added.Any())
		{
			await App.Connection.InsertAllAsync(added);
		}
		if (updated.Any())
		{
			await App.Connection.UpdateAllAsync(updated);
		}
		if (deleted.Any())
		{
			foreach (HabitRecordModel item in deleted)
			{
				await App.Connection.DeleteAsync(item);
			}
		}
		return added.Any() || updated.Any() || deleted.Any();
	}

	public static async Task SaveHabitSectionEtag(string sectionId, string etag)
	{
		HabitSectionModel habitSectionModel = await HabitSectionDao.GetHabitSectionById(sectionId);
		if (habitSectionModel != null)
		{
			habitSectionModel.Etag = etag;
			habitSectionModel.SyncStatus = 2;
			await App.Connection.UpdateAsync(habitSectionModel);
		}
	}

	public static async Task<bool> IsHabitDeleted(string habitId)
	{
		if (string.IsNullOrEmpty(habitId))
		{
			return true;
		}
		HabitModel habitModel = await HabitDao.GetHabitById(habitId);
		return habitModel == null || habitModel.Status == -1;
	}

	public static async Task SkipHabit(string habitId, DateTime? skipDate = null)
	{
		DateTime targetDate = skipDate ?? DateTime.Today;
		HabitModel habit = await HabitDao.GetHabitById(habitId);
		if (habit == null)
		{
			return;
		}
		List<string> list = habit.ExDates?.ToList() ?? new List<string>();
		if (!list.Contains(targetDate.ToString("yyyyMMdd")))
		{
			list.RemoveAll((string e) => DateTime.TryParseExact(e, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result) && result <= targetDate.AddDays(-30.0));
			list.Add(targetDate.ToString("yyyyMMdd"));
			habit.ExDates = list.ToArray();
			habit.ModifiedTime = DateTime.Now;
			habit.SyncStatus = ((habit.SyncStatus != 0) ? 1 : 0);
			habit.ModifiedTime = DateTime.Now;
			await HabitDao.UpdateAsync(habit);
			await HabitReminderCalculator.RecalHabitReminder(habit.Id);
			SyncManager.TryDelaySync();
			DataChangedNotifier.NotifyHabitSkip(habitId);
		}
	}

	public static async Task UnSkipHabit(string habitId, DateTime date)
	{
		HabitModel habit = await HabitDao.GetHabitById(habitId);
		if (habit != null)
		{
			List<string> list = habit.ExDates?.ToList() ?? new List<string>();
			string item = date.Date.ToString("yyyyMMdd");
			if (list.Contains(item))
			{
				list.Remove(item);
				habit.ExDates = list.ToArray();
				habit.SyncStatus = ((habit.SyncStatus != 0) ? 1 : 0);
				habit.ModifiedTime = DateTime.Now;
				await HabitDao.UpdateAsync(habit);
				await HabitReminderCalculator.RecalHabitReminder(habit.Id);
				SyncManager.TryDelaySync();
				DataChangedNotifier.NotifyHabitSkip(habitId);
			}
		}
	}

	public static async Task SaveHabitIcon(string habitId, string icon, string color)
	{
		HabitModel habitModel = await HabitDao.GetHabitById(habitId);
		if (habitModel != null && (habitModel.IconRes != icon || habitModel.Color != color))
		{
			habitModel.IconRes = icon;
			habitModel.Color = color;
			habitModel.ModifiedTime = DateTime.Now;
			habitModel.SyncStatus = ((habitModel.SyncStatus != 0) ? 1 : 0);
			await HabitDao.UpdateAsync(habitModel);
		}
	}

	public static async Task<int> GetCurrentDays(HabitModel habit)
	{
		if (habit.TargetDays > 0)
		{
			return (habit.CompletedCyclesList?.LastOrDefault())?.Streak ?? 0;
		}
		return await HabitCheckInDao.CountValidHabitCheckInsByHabitId(habit.Id);
	}
}
