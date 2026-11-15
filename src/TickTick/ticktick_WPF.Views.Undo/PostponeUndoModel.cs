using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Undo;

public class PostponeUndoModel : UndoController
{
	private List<string> _originalTasks;

	private List<string> _originalCheckItems;

	private List<TimeData> _originalTimeDates;

	private List<TaskModel> _originalTaskModels;

	private List<TaskDetailItemModel> _originalItemModels;

	private readonly HashSet<string> _fullTasks = new HashSet<string>();

	public async Task Do()
	{
		if (_fullTasks.Count > 0 || _originalCheckItems.Count > 0)
		{
			if (_originalCheckItems != null && _originalCheckItems.Any())
			{
				_originalItemModels = await TaskDetailItemDao.BatchGetItems(_originalCheckItems);
				await TaskService.BatchSetCheckItemToToday(_originalItemModels);
			}
			if (_originalTasks != null && _originalTasks.Any())
			{
				_originalTaskModels = await TaskDao.GetThinTasksInBatch(_originalTasks);
				TimeData model = new TimeData
				{
					StartDate = DateTime.Today,
					BatchData = new BatchData
					{
						IsTimeUnified = false,
						IsRepeatUnified = false,
						IsDateUnified = true
					}
				};
				await TaskService.BatchSetDate(_originalTasks, model, notEditFlag: true);
			}
			if (_fullTasks.Any())
			{
				TaskChangeNotifier.NotifyBatchDateChanged(_fullTasks.ToList(), this, sync: false);
			}
			if (_originalCheckItems != null && _originalCheckItems.Any())
			{
				TaskChangeNotifier.OnCheckItemsChanged(_originalCheckItems);
			}
		}
	}

	public override string GetTitle()
	{
		return "";
	}

	public override string GetContent()
	{
		return Utils.GetString("Postponed");
	}

	public override async void Undo()
	{
		if (!_fullTasks.Any())
		{
			List<TaskDetailItemModel> originalItemModels = _originalItemModels;
			if (originalItemModels == null || originalItemModels.Count <= 0)
			{
				return;
			}
		}
		if (_originalTaskModels != null && _originalTaskModels.Count > 0)
		{
			await TaskDao.BatchUpdateTasks(_originalTaskModels, checkUserId: false, CheckMatchedType.CheckSmart);
		}
		if (_originalItemModels != null && _originalItemModels.Count > 0)
		{
			await TaskDetailItemDao.BatchUpdateChecklists(_originalItemModels);
		}
		if (_fullTasks.Any())
		{
			TaskChangeNotifier.NotifyBatchDateChanged(_fullTasks.ToList(), this, sync: false);
		}
		if (_originalCheckItems != null && _originalCheckItems.Any())
		{
			TaskChangeNotifier.OnCheckItemsChanged(_originalCheckItems);
		}
	}

	public override void Finished()
	{
		SyncManager.TryDelaySync();
	}

	public bool InitData(List<TaskBaseViewModel> models)
	{
		if (models == null || models.Count <= 0)
		{
			return false;
		}
		_fullTasks.Clear();
		models.ForEach(delegate(TaskBaseViewModel o)
		{
			if (o.IsTask && !string.IsNullOrEmpty(o.GetTaskId()))
			{
				_fullTasks.Add(o.Id);
			}
		});
		List<string> list = (from item in models
			where item.IsTask && (!item.IsAgenda || (item.IsAgenda && item.AttendId == item.Id))
			select item.Id).ToList();
		List<string> list2 = (from item in models
			where item.IsCheckItem
			select item.Id).Distinct().ToList();
		if (list.Count > 0)
		{
			_originalTasks = list;
		}
		if (list2.Count > 0)
		{
			_originalCheckItems = list2;
		}
		if (list.Count <= 0)
		{
			return list2.Count > 0;
		}
		return true;
	}
}
