using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Habit;

public class HabitLogControl : UserControl, IComponentConnector
{
	private bool _showEdit;

	internal HabitLogControl Root;

	internal TextBlock CheckInRecordHint;

	internal ItemsControl LogItems;

	private bool _contentLoaded;

	public HabitLogControl()
	{
		InitializeComponent();
	}

	public async void Load(string habitId, DateTime startDate, DateTime endDate, List<HabitCheckInModel> checkIns, List<HabitRecordModel> records)
	{
		if (!(LogItems.ItemsSource is ObservableCollection<CheckInLogViewModel>))
		{
			LogItems.ItemsSource = new ObservableCollection<CheckInLogViewModel>();
		}
		HabitModel habitModel = await HabitDao.GetHabitById(habitId);
		List<CheckInLogViewModel> list = new List<CheckInLogViewModel>();
		if (habitModel != null && records != null && records.Any())
		{
			List<HabitRecordModel> list2 = new List<HabitRecordModel>();
			HashSet<string> ids = new HashSet<string>();
			foreach (HabitRecordModel item in records.Where((HabitRecordModel habitRecordModel) => string.IsNullOrEmpty(habitRecordModel.Id) || !ids.Contains(habitRecordModel.Id)))
			{
				ids.Add(item.Id);
				list2.Add(item);
			}
			HashSet<int> stamps = new HashSet<int>();
			list2 = list2.OrderByDescending((HabitRecordModel m) => m.Stamp).ToList();
			int startNum = DateUtils.GetDateNum(startDate);
			int endNum = DateUtils.GetDateNum(endDate);
			foreach (HabitRecordModel record in list2.Where((HabitRecordModel habitRecordModel) => !stamps.Contains(habitRecordModel.Stamp) && habitRecordModel.Stamp >= startNum && habitRecordModel.Stamp <= endNum))
			{
				stamps.Add(record.Stamp);
				HabitCheckInModel habitCheckInModel = checkIns.FirstOrDefault((HabitCheckInModel check) => check.CheckinStamp == record.Stamp.ToString());
				bool completed = habitCheckInModel?.IsComplete() ?? false;
				bool unCompleted = habitCheckInModel?.IsUnComplete() ?? false;
				list.Add(new CheckInLogViewModel(record, habitModel, completed, unCompleted, _showEdit));
			}
		}
		CheckInRecordHint.Visibility = (list.Any() ? Visibility.Collapsed : Visibility.Visible);
		list.ForEach(delegate(CheckInLogViewModel d)
		{
			d.LineVisibility = Visibility.Visible;
		});
		if (list.Count > 0)
		{
			list[list.Count - 1].LineVisibility = Visibility.Collapsed;
		}
		ItemsSourceHelper.SetHidableItemsSource(LogItems, list);
	}

	public void OnShowEditChanged(bool showEdit)
	{
		_showEdit = showEdit;
		if (!(LogItems.ItemsSource is ObservableCollection<CheckInLogViewModel> observableCollection))
		{
			return;
		}
		foreach (CheckInLogViewModel item in observableCollection)
		{
			item.ShowEdit = showEdit;
		}
	}

	public void OnItemDeleted()
	{
		if (LogItems.ItemsSource is ObservableCollection<CheckInLogViewModel> source)
		{
			CheckInRecordHint.Visibility = (source.Any((CheckInLogViewModel item) => !item.IsHide) ? Visibility.Collapsed : Visibility.Visible);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/habit/habitlogcontrol.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			Root = (HabitLogControl)target;
			break;
		case 2:
			CheckInRecordHint = (TextBlock)target;
			break;
		case 3:
			LogItems = (ItemsControl)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
