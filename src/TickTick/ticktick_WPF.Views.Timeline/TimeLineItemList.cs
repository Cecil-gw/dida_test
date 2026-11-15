using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Timeline;

public class TimeLineItemList : BlockingList<TimelineCellViewModel>
{
	private readonly List<TimelineCellViewModel> _changedModels = new List<TimelineCellViewModel>();

	private string _uid;

	public TimeLineItemList()
	{
		_uid = Utils.GetGuid();
	}

	public void OnItemPosChanged(TimelineCellViewModel cellModel)
	{
		lock (_changedModels)
		{
			_changedModels.Add(cellModel);
		}
		DelayActionHandlerCenter.TryDoAction(_uid + "NotifyTimelineItemsPosChanged", DoNotify, 10);
	}

	private void DoNotify(object sender, EventArgs e)
	{
		Application.Current?.Dispatcher?.Invoke(delegate
		{
			List<TimelineCellViewModel> models;
			lock (_changedModels)
			{
				models = _changedModels.ToList();
				_changedModels.Clear();
			}
			NotifyItemsChanged(models);
		});
	}
}
