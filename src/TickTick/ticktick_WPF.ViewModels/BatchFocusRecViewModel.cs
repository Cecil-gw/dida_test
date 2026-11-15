using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Framework;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.SyncServices;
using ticktick_WPF.Util;
using ticktick_WPF.Views;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Pomo.FocusStatistics;

namespace ticktick_WPF.ViewModels;

internal class BatchFocusRecViewModel : INotifyPropertyChanged
{
	private int _selectedCount;

	private bool _isSelectedAll;

	private bool _hasFilter;

	private string _dateFilterText;

	private string _durationFilterText;

	private bool _isShowCustomDatePopup;

	private TimeData _timeData;

	private bool _isShowCustomDurationPopup;

	private string _customStartDuraText;

	private string _customEndDuraText;

	private bool _customStartDuraValid = true;

	private bool _customEndDuraValid = true;

	private bool _isLoading;

	private bool _isQuerying;

	private bool _isConfirmCustomDate;

	private bool _isConfirmCustomDuration;

	private bool _isEnableConfirmDate;

	private bool _isEnableConfirmDuration;

	public ObservableCollection<object> FilterItems { get; set; } = new ObservableCollection<object>();

	public string Title
	{
		get
		{
			if (SelectedCount != 0)
			{
				return string.Format(Utils.GetString("NSelected"), SelectedCount);
			}
			return Utils.GetString("BatchManagement");
		}
	}

	public int SelectedCount
	{
		get
		{
			return _selectedCount;
		}
		set
		{
			_selectedCount = value;
			NotifyPropertyChanged("SelectedCount");
			NotifyPropertyChanged("Title");
		}
	}

	public bool IsSelectedAll
	{
		get
		{
			return _isSelectedAll;
		}
		set
		{
			_isSelectedAll = value;
			NotifyPropertyChanged("IsSelectedAll");
		}
	}

	public bool HasFilter
	{
		get
		{
			return _hasFilter;
		}
		set
		{
			_hasFilter = value;
			NotifyPropertyChanged("HasFilter");
		}
	}

	public string DateFilterText
	{
		get
		{
			return _dateFilterText;
		}
		set
		{
			_dateFilterText = value;
			NotifyPropertyChanged("DateFilterText");
		}
	}

	public string DurationFilterText
	{
		get
		{
			return _durationFilterText;
		}
		set
		{
			_durationFilterText = value;
			NotifyPropertyChanged("DurationFilterText");
		}
	}

	public bool IsShowCustomDatePopup
	{
		get
		{
			return _isShowCustomDatePopup;
		}
		set
		{
			_isShowCustomDatePopup = value;
			NotifyPropertyChanged("IsShowCustomDatePopup");
		}
	}

	public TimeData TimeData
	{
		get
		{
			return _timeData;
		}
		set
		{
			_timeData = value;
			NotifyPropertyChanged("TimeData");
		}
	}

	public bool IsShowCustomDurationPopup
	{
		get
		{
			return _isShowCustomDurationPopup;
		}
		set
		{
			_isShowCustomDurationPopup = value;
			NotifyPropertyChanged("IsShowCustomDurationPopup");
		}
	}

	public string CustomStartDuraText
	{
		get
		{
			return _customStartDuraText;
		}
		set
		{
			_customStartDuraText = value;
			NotifyPropertyChanged("CustomStartDuraText");
			CheckCustomDuraTextValid();
		}
	}

	public string CustomEndDuraText
	{
		get
		{
			return _customEndDuraText;
		}
		set
		{
			_customEndDuraText = value;
			NotifyPropertyChanged("CustomEndDuraText");
			CheckCustomDuraTextValid();
		}
	}

	public bool CustomStartDuraValid
	{
		get
		{
			return _customStartDuraValid;
		}
		set
		{
			_customStartDuraValid = value;
			NotifyPropertyChanged("CustomStartDuraValid");
		}
	}

	public bool CustomEndDuraValid
	{
		get
		{
			return _customEndDuraValid;
		}
		set
		{
			_customEndDuraValid = value;
			NotifyPropertyChanged("CustomEndDuraValid");
		}
	}

	public ObservableCollection<ComboBoxViewModel> DateFilters { get; set; } = new ObservableCollection<ComboBoxViewModel>();

	public ObservableCollection<ComboBoxViewModel> DurationLenFilters { get; set; } = new ObservableCollection<ComboBoxViewModel>();

	public ObservableCollection<ComboBoxViewModel> AssociationStatusFilters { get; set; } = new ObservableCollection<ComboBoxViewModel>();

	public bool IsLoading
	{
		get
		{
			return _isLoading;
		}
		set
		{
			_isLoading = value;
			NotifyPropertyChanged("IsLoading");
		}
	}

	public bool IsQuerying
	{
		get
		{
			return _isQuerying;
		}
		set
		{
			_isQuerying = value;
			NotifyPropertyChanged("IsQuerying");
		}
	}

	public bool IsConfirmCustomDate
	{
		get
		{
			return _isConfirmCustomDate;
		}
		set
		{
			_isConfirmCustomDate = value;
			NotifyPropertyChanged("IsConfirmCustomDate");
		}
	}

	public bool IsConfirmCustomDuration
	{
		get
		{
			return _isConfirmCustomDuration;
		}
		set
		{
			_isConfirmCustomDuration = value;
			NotifyPropertyChanged("IsConfirmCustomDuration");
		}
	}

	public bool IsEnableConfirmDate
	{
		get
		{
			return _isEnableConfirmDate;
		}
		set
		{
			_isEnableConfirmDate = value;
			NotifyPropertyChanged("IsEnableConfirmDate");
		}
	}

	public bool IsEnableConfirmDuration
	{
		get
		{
			return _isEnableConfirmDuration;
		}
		set
		{
			_isEnableConfirmDuration = value;
			NotifyPropertyChanged("IsEnableConfirmDuration");
		}
	}

	public DateFilterType CurrentDateFilter
	{
		get
		{
			DateFilterType? dateFilterType = (DateFilterType?)DateFilters.FirstOrDefault((ComboBoxViewModel x) => x.Selected)?.Value;
			if (dateFilterType.HasValue)
			{
				return dateFilterType.Value;
			}
			return DateFilterType.All;
		}
		set
		{
			foreach (ComboBoxViewModel dateFilter in DateFilters)
			{
				dateFilter.Selected = (DateFilterType)dateFilter.Value == value;
			}
		}
	}

	public DurationLenFilterType CurrentDurationLenFilter
	{
		get
		{
			DurationLenFilterType? durationLenFilterType = (DurationLenFilterType?)DurationLenFilters.FirstOrDefault((ComboBoxViewModel x) => x.Selected)?.Value;
			if (durationLenFilterType.HasValue)
			{
				return durationLenFilterType.Value;
			}
			return DurationLenFilterType.All;
		}
		set
		{
			foreach (ComboBoxViewModel durationLenFilter in DurationLenFilters)
			{
				durationLenFilter.Selected = (DurationLenFilterType)durationLenFilter.Value == value;
			}
		}
	}

	private AssociationStatusType CurrentAssociationFilter
	{
		get
		{
			AssociationStatusType? associationStatusType = (AssociationStatusType?)AssociationStatusFilters.FirstOrDefault((ComboBoxViewModel x) => x.Selected)?.Value;
			if (associationStatusType.HasValue)
			{
				return associationStatusType.Value;
			}
			return AssociationStatusType.All;
		}
		set
		{
			foreach (ComboBoxViewModel associationStatusFilter in AssociationStatusFilters)
			{
				associationStatusFilter.Selected = (AssociationStatusType?)associationStatusFilter.Value == value;
			}
		}
	}

	private int? customDurationMin { get; set; }

	private int? customDurationMax { get; set; }

	public string pageToken { get; set; }

	private long _timelinePullPoint { get; set; } = Utils.GetNowTimeStampInMills();

	private DateFilterType LastDateFilter { get; set; }

	private DurationLenFilterType LastDurationLenFilter { get; set; }

	public bool IsShowWindow { get; set; }

	public event PropertyChangedEventHandler PropertyChanged;

	public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public BatchFocusRecViewModel()
	{
		InitComboBox();
		FilterItems.CollectionChanged += Items_CollectionChanged;
	}

	private void InitComboBox()
	{
		foreach (DateFilterType value in Enum.GetValues(typeof(DateFilterType)))
		{
			ComboBoxViewModel comboBoxViewModel = new ComboBoxViewModel(value, Utils.GetString(value.DescriptionAttr()), 34.0);
			if (value == DateFilterType.All)
			{
				comboBoxViewModel.SelectedTitle = Utils.GetString("date");
			}
			DateFilters.Add(comboBoxViewModel);
		}
		DateFilterText = Utils.GetString("date");
		foreach (DurationLenFilterType value2 in Enum.GetValues(typeof(DurationLenFilterType)))
		{
			string empty = string.Empty;
			ComboBoxViewModel comboBoxViewModel2 = new ComboBoxViewModel(title: (value2 != DurationLenFilterType.All && value2 != DurationLenFilterType.Custom) ? string.Format(Utils.GetString("LessThanXMin"), value2.DescriptionAttr()) : Utils.GetString(value2.DescriptionAttr()), value: value2, height: 34.0);
			if (value2 == DurationLenFilterType.All)
			{
				comboBoxViewModel2.SelectedTitle = Utils.GetString("DurationLength");
			}
			DurationLenFilters.Add(comboBoxViewModel2);
		}
		DurationFilterText = Utils.GetString("DurationLength");
		foreach (AssociationStatusType value3 in Enum.GetValues(typeof(AssociationStatusType)))
		{
			ComboBoxViewModel comboBoxViewModel3 = new ComboBoxViewModel(value3, Utils.GetString(value3.DescriptionAttr()), 34.0);
			if (value3 == AssociationStatusType.All)
			{
				comboBoxViewModel3.SelectedTitle = Utils.GetString("LinkedStatus");
			}
			AssociationStatusFilters.Add(comboBoxViewModel3);
		}
	}

	public async Task Init()
	{
		IsShowWindow = true;
		await UpdateItems(clear: true);
	}

	private void Items_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		if (!IsShowWindow)
		{
			return;
		}
		if (e.NewItems != null)
		{
			foreach (object newItem in e.NewItems)
			{
				if (newItem is GroupCheckFocusRecordItemViewModel groupCheckFocusRecordItemViewModel)
				{
					groupCheckFocusRecordItemViewModel.OnCheckedCountChanged = (Action)Delegate.Combine(groupCheckFocusRecordItemViewModel.OnCheckedCountChanged, new Action(OnCheckedCountChanged));
				}
			}
		}
		if (e.OldItems == null)
		{
			return;
		}
		foreach (object oldItem in e.OldItems)
		{
			if (oldItem is GroupCheckFocusRecordItemViewModel groupCheckFocusRecordItemViewModel2)
			{
				groupCheckFocusRecordItemViewModel2.OnCheckedCountChanged = (Action)Delegate.Remove(groupCheckFocusRecordItemViewModel2.OnCheckedCountChanged, new Action(OnCheckedCountChanged));
			}
		}
	}

	private void OnCheckedCountChanged()
	{
		if (IsShowWindow)
		{
			IEnumerable<GroupCheckFocusRecordItemViewModel> source = FilterItems.OfType<GroupCheckFocusRecordItemViewModel>();
			SelectedCount = source.Sum((GroupCheckFocusRecordItemViewModel x) => x.SelectedCount);
			IsSelectedAll = source.Any() && SelectedCount == source.Sum((GroupCheckFocusRecordItemViewModel x) => x.Items.Count);
		}
	}

	public void SelectedAllOrClear()
	{
		bool isSelectedAll = IsSelectedAll;
		isSelectedAll = !isSelectedAll;
		foreach (GroupCheckFocusRecordItemViewModel item in FilterItems.OfType<GroupCheckFocusRecordItemViewModel>())
		{
			item.DateTitle.IsSelected = isSelectedAll;
		}
	}

	public async Task DeleteSelected()
	{
		if (SelectedCount == 0)
		{
			return;
		}
		string title = ((SelectedCount == 1) ? Utils.GetString("DeleteFocusRecord") : Utils.GetString("DeleteFocusRecords"));
		string content = ((SelectedCount == 1) ? Utils.GetString("DeleteFocusRecordMsg") : Utils.GetString("DeleteFocusRecordsMsg"));
		bool? flag = new CustomerDialog(title, content, Utils.GetString("Delete"), Utils.GetString("Cancel"))
		{
			Owner = App.Window
		}.ShowDialog();
		if (!flag.HasValue || !flag.Value)
		{
			return;
		}
		await PomoService.DeleteByIds((from x in FilterItems.OfType<GroupCheckFocusRecordItemViewModel>().SelectMany((GroupCheckFocusRecordItemViewModel x) => x.Items.Where((CheckFocusRecordItemViewModel o) => o.IsSelected == true))
			where !string.IsNullOrEmpty(x.FocusId)
			select x.FocusId).ToList());
		for (int num = FilterItems.Count - 1; num >= 0; num--)
		{
			if (FilterItems[num] is GroupCheckFocusRecordItemViewModel groupCheckFocusRecordItemViewModel)
			{
				if (groupCheckFocusRecordItemViewModel.DateTitle.IsSelected == true)
				{
					FilterItems.RemoveAt(num);
				}
				else if (!groupCheckFocusRecordItemViewModel.DateTitle.IsSelected.HasValue)
				{
					for (int num2 = groupCheckFocusRecordItemViewModel.Items.Count - 1; num2 >= 0; num2--)
					{
						if (groupCheckFocusRecordItemViewModel.Items[num2].IsSelected == true)
						{
							groupCheckFocusRecordItemViewModel.Items.RemoveAt(num2);
						}
					}
				}
			}
		}
		SelectedCount = 0;
		IsSelectedAll = false;
	}

	public void OnNotifyItemChanged(PomoChangeArgs args)
	{
		if (!IsShowWindow)
		{
			return;
		}
		List<string> list = new List<string>();
		if (!string.IsNullOrEmpty(args.Id))
		{
			list.Add(args.Id);
		}
		if (args.Ids != null && args.Ids.Any())
		{
			list.AddRange(args.Ids);
		}
		if (!list.Any())
		{
			return;
		}
		switch (args.ChangeType)
		{
		case PomoChangeType.Updated:
		{
			AssociationStatusType currentAssociationFilter = CurrentAssociationFilter;
			if (currentAssociationFilter == AssociationStatusType.All)
			{
				break;
			}
			{
				foreach (GroupCheckFocusRecordItemViewModel item in FilterItems.OfType<GroupCheckFocusRecordItemViewModel>().ToList())
				{
					for (int num2 = item.Items.Count - 1; num2 >= 0; num2--)
					{
						if (list.Contains(item.Items[num2].FocusId) && ((currentAssociationFilter == AssociationStatusType.No && item.Items[num2].Titles.Any()) || (currentAssociationFilter == AssociationStatusType.Has && !item.Items[num2].Titles.Any())))
						{
							item.Items.RemoveAt(num2);
						}
					}
					if (!item.Items.Any())
					{
						FilterItems.Remove(item);
					}
				}
				break;
			}
		}
		case PomoChangeType.Deleted:
		{
			foreach (GroupCheckFocusRecordItemViewModel item2 in FilterItems.OfType<GroupCheckFocusRecordItemViewModel>().ToList())
			{
				for (int num = item2.Items.Count - 1; num >= 0; num--)
				{
					if (list.Contains(item2.Items[num].FocusId))
					{
						item2.Items.RemoveAt(num);
					}
				}
				if (!item2.Items.Any())
				{
					FilterItems.Remove(item2);
				}
			}
			break;
		}
		case PomoChangeType.Added:
			break;
		}
	}

	public void ClearFilter()
	{
		HasFilter = false;
		int? num = (customDurationMax = null);
		customDurationMin = num;
		DurationLenFilters.FirstOrDefault((ComboBoxViewModel x) => (DurationLenFilterType)x.Value == DurationLenFilterType.Custom).SelectedTitle = Utils.GetString("Custom");
		DateFilterText = DateFilters.FirstOrDefault((ComboBoxViewModel x) => (DateFilterType)x.Value == DateFilterType.All).SelectedTitle;
		DateFilters.FirstOrDefault((ComboBoxViewModel x) => (DateFilterType)x.Value == DateFilterType.Custom).SelectedTitle = Utils.GetString("Custom");
		DurationFilterText = DurationLenFilters.FirstOrDefault((ComboBoxViewModel x) => (DurationLenFilterType)x.Value == DurationLenFilterType.All).SelectedTitle;
		UpdateItems(clear: true);
	}

	public async Task OnComboSelectChange(BatchFocusFilterType filterType)
	{
		switch (filterType)
		{
		case BatchFocusFilterType.Date:
		{
			DateFilterType filter2 = CurrentDateFilter;
			if (filter2 == DateFilterType.Custom)
			{
				IsConfirmCustomDate = false;
				if (TimeData != null)
				{
					TimeData.PropertyChanged -= OnTimeDataChange;
				}
				TimeData = new TimeData
				{
					StartDate = TimeData?.StartDate,
					DueDate = TimeData?.DueDate
				};
				TimeData.PropertyChanged += OnTimeDataChange;
				IsShowCustomDatePopup = true;
				IsEnableConfirmDate = CheckCustomDateValid();
				return;
			}
			if (LastDateFilter == DateFilterType.Custom)
			{
				DateFilters.FirstOrDefault((ComboBoxViewModel x) => (DateFilterType)x.Value == DateFilterType.Custom).SelectedTitle = Utils.GetString("Custom");
			}
			if (filter2 != DateFilterType.Custom)
			{
				DateFilterText = DateFilters.FirstOrDefault((ComboBoxViewModel x) => (DateFilterType)x.Value == filter2).SelectedTitle;
			}
			break;
		}
		case BatchFocusFilterType.Duration:
		{
			DurationLenFilterType filter = CurrentDurationLenFilter;
			if (filter == DurationLenFilterType.Custom)
			{
				IsConfirmCustomDuration = false;
				CustomStartDuraText = customDurationMin?.ToString();
				CustomEndDuraText = customDurationMax?.ToString();
				IsShowCustomDurationPopup = true;
				IsEnableConfirmDuration = CheckCustomDurationValid();
				return;
			}
			if (LastDurationLenFilter == DurationLenFilterType.Custom)
			{
				DurationLenFilters.FirstOrDefault((ComboBoxViewModel x) => (DurationLenFilterType)x.Value == DurationLenFilterType.Custom).SelectedTitle = Utils.GetString("Custom");
			}
			if (filter != DurationLenFilterType.Custom)
			{
				DurationFilterText = DurationLenFilters.FirstOrDefault((ComboBoxViewModel x) => (DurationLenFilterType)x.Value == filter).SelectedTitle;
			}
			break;
		}
		}
		await UpdateItems(clear: true);
	}

	private void CheckCustomDuraTextValid()
	{
		int? num = null;
		int? num2 = null;
		int result;
		if (string.IsNullOrEmpty(CustomStartDuraText))
		{
			CustomStartDuraValid = true;
		}
		else if (!int.TryParse(CustomStartDuraText, out result))
		{
			CustomStartDuraValid = false;
		}
		else
		{
			num = result;
			CustomStartDuraValid = result >= 5 && result <= 720;
		}
		int result2;
		if (string.IsNullOrEmpty(CustomEndDuraText))
		{
			CustomEndDuraValid = true;
		}
		else if (!int.TryParse(CustomEndDuraText, out result2))
		{
			CustomEndDuraValid = false;
		}
		else
		{
			num2 = result2;
			CustomEndDuraValid = result2 >= 5 && result2 <= 720;
		}
		if (num.HasValue && num2.HasValue && num > num2)
		{
			bool customStartDuraValid = (CustomEndDuraValid = false);
			CustomStartDuraValid = customStartDuraValid;
		}
		IsEnableConfirmDuration = CheckCustomDurationValid();
	}

	public bool CheckCustomDurationValid()
	{
		if ((string.IsNullOrEmpty(CustomStartDuraText) && string.IsNullOrEmpty(CustomEndDuraText)) || !CustomStartDuraValid || !CustomEndDuraValid)
		{
			return false;
		}
		return true;
	}

	public async Task SetCustomDuration()
	{
		if (!IsConfirmCustomDuration)
		{
			CurrentDurationLenFilter = LastDurationLenFilter;
			IsShowCustomDurationPopup = false;
		}
		else if ((!string.IsNullOrEmpty(CustomStartDuraText) || !string.IsNullOrEmpty(CustomEndDuraText)) && CustomStartDuraValid && CustomEndDuraValid)
		{
			IsShowCustomDurationPopup = false;
			customDurationMin = (string.IsNullOrEmpty(CustomStartDuraText) ? ((int?)null) : new int?(int.Parse(CustomStartDuraText)));
			customDurationMax = (string.IsNullOrEmpty(CustomEndDuraText) ? ((int?)null) : new int?(int.Parse(CustomEndDuraText)));
			ComboBoxViewModel comboBoxViewModel = DurationLenFilters.FirstOrDefault((ComboBoxViewModel x) => (DurationLenFilterType)x.Value == DurationLenFilterType.Custom);
			string text = string.Empty;
			if (customDurationMin.HasValue && customDurationMax.HasValue)
			{
				text = ((customDurationMin != customDurationMax) ? string.Format("{0} {1} {2} {3}", customDurationMin, Utils.IsEn() ? "~" : "-", customDurationMax, Utils.GetString("PublicUpMinutes")) : string.Format("{0} {1}", customDurationMax, Utils.GetString("PublicUpMinutes")));
			}
			else if (!customDurationMin.HasValue)
			{
				text = string.Format(Utils.GetString("AtMostXMin"), customDurationMax);
			}
			else if (!customDurationMax.HasValue)
			{
				text = string.Format(Utils.GetString("AtLeastXMin"), customDurationMin);
			}
			BatchFocusRecViewModel batchFocusRecViewModel = this;
			string durationFilterText = (comboBoxViewModel.SelectedTitle = text);
			batchFocusRecViewModel.DurationFilterText = durationFilterText;
			await UpdateItems(clear: true);
		}
	}

	private void OnTimeDataChange(object sender, PropertyChangedEventArgs e)
	{
		IsEnableConfirmDate = CheckCustomDateValid();
	}

	public bool CheckCustomDateValid()
	{
		TimeData timeData = TimeData;
		if (timeData != null && timeData.StartDate.HasValue)
		{
			TimeData timeData2 = TimeData;
			if (timeData2 != null && timeData2.DueDate.HasValue)
			{
				return true;
			}
		}
		return false;
	}

	public async Task SetCustomDate()
	{
		if (!IsConfirmCustomDate)
		{
			CurrentDateFilter = LastDateFilter;
			IsShowCustomDatePopup = false;
		}
		else if (TimeData.StartDate.HasValue || TimeData.DueDate.HasValue)
		{
			IsShowCustomDatePopup = false;
			ComboBoxViewModel comboBoxViewModel = DateFilters.FirstOrDefault((ComboBoxViewModel x) => (DateFilterType)x.Value == DateFilterType.Custom);
			bool flag = !TimeData.StartDate.HasValue || TimeData.StartDate.Value.Year == DateTime.Today.Year;
			bool flag2 = !TimeData.DueDate.HasValue || TimeData.DueDate.Value.Year == DateTime.Today.Year;
			string text = "?";
			if (TimeData.StartDate.HasValue)
			{
				text = ((!(flag && flag2)) ? ticktick_WPF.Util.DateUtils.FormatYearMonthDay(TimeData.StartDate.Value, DateFormatLen.Middle) : ticktick_WPF.Util.DateUtils.FormatMonthDay(TimeData.StartDate.Value, DateFormatLen.Long));
			}
			string text2 = "?";
			if (TimeData.DueDate.HasValue)
			{
				text2 = ((!(flag && flag2)) ? ticktick_WPF.Util.DateUtils.FormatYearMonthDay(TimeData.DueDate.Value, DateFormatLen.Middle) : ticktick_WPF.Util.DateUtils.FormatMonthDay(TimeData.DueDate.Value, DateFormatLen.Long));
			}
			string text3 = text + " - " + text2;
			if (TimeData.StartDate == TimeData.DueDate && TimeData.StartDate.HasValue)
			{
				text3 = text;
			}
			BatchFocusRecViewModel batchFocusRecViewModel = this;
			string dateFilterText = (comboBoxViewModel.SelectedTitle = text3);
			batchFocusRecViewModel.DateFilterText = dateFilterText;
			await UpdateItems(clear: true);
		}
	}

	public async Task UpdateItems(bool clear)
	{
		if (!IsShowWindow || IsLoading)
		{
			return;
		}
		IsLoading = true;
		try
		{
			if (clear)
			{
				pageToken = null;
				_timelinePullPoint = Utils.GetNowTimeStampInMills();
				FilterItems.Clear();
			}
			else
			{
				object obj = FilterItems.LastOrDefault();
				if (obj is FocusRecordLoadMoreViewModel)
				{
					FilterItems.Remove(obj);
				}
			}
			List<PomodoroModel> list = await QueryItems(await GetFilterModel(), !clear);
			if (list != null)
			{
				await LoadItems(list);
				OnCheckedCountChanged();
			}
		}
		catch (Exception ex)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
		}
		finally
		{
			IsLoading = false;
		}
	}

	private async Task<PomoFocusRecFilterModel> GetFilterModel()
	{
		DateFilterType currentDateFilter = CurrentDateFilter;
		DurationLenFilterType currentDurationLenFilter = CurrentDurationLenFilter;
		AssociationStatusType currentAssociationFilter = CurrentAssociationFilter;
		LastDateFilter = currentDateFilter;
		LastDurationLenFilter = currentDurationLenFilter;
		if (currentDateFilter == DateFilterType.All && currentDurationLenFilter == DurationLenFilterType.All && currentAssociationFilter == AssociationStatusType.All)
		{
			HasFilter = false;
			return null;
		}
		HasFilter = true;
		PomoFocusRecFilterModel pomoFocusRecFilterModel = new PomoFocusRecFilterModel();
		switch (currentDateFilter)
		{
		case DateFilterType.All:
			pomoFocusRecFilterModel.beginTime = null;
			pomoFocusRecFilterModel.endTime = null;
			break;
		case DateFilterType.Today:
			pomoFocusRecFilterModel.beginTime = DateTime.Today;
			pomoFocusRecFilterModel.endTime = DateTime.Today.AddDays(1.0);
			break;
		case DateFilterType.Yesterday:
			pomoFocusRecFilterModel.beginTime = DateTime.Today.AddDays(-1.0);
			pomoFocusRecFilterModel.endTime = DateTime.Today;
			break;
		case DateFilterType.ThisWeek:
		{
			DateTime weekStart = Utils.GetWeekStart(DateTime.Today);
			pomoFocusRecFilterModel.beginTime = weekStart;
			pomoFocusRecFilterModel.endTime = weekStart.AddDays(7.0).AddDays(1.0);
			break;
		}
		case DateFilterType.ThisMonth:
		{
			DateTime now = DateTime.Now;
			DateTime value = new DateTime(now.Year, now.Month, 1);
			pomoFocusRecFilterModel.beginTime = value;
			pomoFocusRecFilterModel.endTime = value.AddMonths(1);
			break;
		}
		case DateFilterType.LastMonth:
		{
			DateTime dateTime = DateTime.Now.AddMonths(-1);
			DateTime value2 = new DateTime(dateTime.Year, dateTime.Month, 1);
			pomoFocusRecFilterModel.beginTime = value2;
			pomoFocusRecFilterModel.endTime = value2.AddMonths(1);
			break;
		}
		case DateFilterType.Custom:
			if (TimeData != null)
			{
				if (TimeData.StartDate.HasValue && TimeData.DueDate.HasValue)
				{
					pomoFocusRecFilterModel.beginTime = TimeData.StartDate;
					pomoFocusRecFilterModel.endTime = TimeData.DueDate.Value.AddDays(1.0);
				}
				if (TimeData.StartDate.HasValue)
				{
					pomoFocusRecFilterModel.beginTime = TimeData.StartDate;
				}
				else if (TimeData.DueDate.HasValue)
				{
					pomoFocusRecFilterModel.endTime = TimeData.DueDate.Value.AddDays(1.0);
				}
			}
			break;
		}
		switch (currentDurationLenFilter)
		{
		case DurationLenFilterType.All:
			pomoFocusRecFilterModel.minDuration = null;
			pomoFocusRecFilterModel.maxDuration = null;
			break;
		case DurationLenFilterType.Less10min:
			pomoFocusRecFilterModel.maxDuration = 10;
			break;
		case DurationLenFilterType.Less25min:
			pomoFocusRecFilterModel.maxDuration = 25;
			break;
		case DurationLenFilterType.Less45min:
			pomoFocusRecFilterModel.maxDuration = 45;
			break;
		case DurationLenFilterType.Less60min:
			pomoFocusRecFilterModel.maxDuration = 60;
			break;
		case DurationLenFilterType.Custom:
			if (customDurationMin.HasValue && customDurationMax.HasValue)
			{
				pomoFocusRecFilterModel.minDuration = customDurationMin;
				pomoFocusRecFilterModel.maxDuration = customDurationMax;
			}
			else if (!customDurationMin.HasValue)
			{
				pomoFocusRecFilterModel.maxDuration = customDurationMax;
				pomoFocusRecFilterModel.minDuration = -1;
			}
			else if (!customDurationMax.HasValue)
			{
				pomoFocusRecFilterModel.minDuration = customDurationMin;
				pomoFocusRecFilterModel.maxDuration = -1;
			}
			break;
		}
		switch (currentAssociationFilter)
		{
		case AssociationStatusType.All:
			pomoFocusRecFilterModel.relation = PomoFocusRecRelation.All;
			break;
		case AssociationStatusType.Has:
			pomoFocusRecFilterModel.relation = PomoFocusRecRelation.Related;
			break;
		case AssociationStatusType.No:
			pomoFocusRecFilterModel.relation = PomoFocusRecRelation.Unrelated;
			break;
		}
		pomoFocusRecFilterModel.pageToken = pageToken;
		return pomoFocusRecFilterModel;
	}

	private async Task<List<PomodoroModel>> QueryItems(PomoFocusRecFilterModel filterModel, bool fromLoadMore)
	{
		Func<bool> isCurrentUser = LocalSettings.Settings.CaptureFocusSyncGuard();
		IsQuerying = true;
		try
		{
			if (!Utils.IsNetworkAvailable())
			{
				Utils.Toast(Utils.GetString("NoNetwork"));
				return null;
			}
			List<PomodoroModel> focuses;
			if (filterModel == null)
			{
				focuses = await Communicator.GetFocusTimeline(_timelinePullPoint);
				if (focuses == null || !isCurrentUser())
				{
					return null;
				}
				foreach (PomodoroModel item in focuses)
				{
					LocalSettings.Settings.ObserveSyncedFocusRecord(item);
				}
				DateTime end = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1)).AddMilliseconds(_timelinePullPoint);
				focuses = focuses.OrderByDescending((PomodoroModel f) => f.StartTime).ToList();
				DateTime dateTime = focuses.LastOrDefault()?.StartTime ?? default(DateTime);
				long timelinePullPoint = ((focuses.Count >= 30) ? Utils.GetTimeStamp(dateTime) : (-1));
				pageToken = null;
				_timelinePullPoint = timelinePullPoint;
				if (fromLoadMore)
				{
					await PomoSyncService.MergeRemotes(focuses, dateTime, end, null);
				}
				return focuses;
			}
			filterModel.pageSize = 30;
			PomoFocusRecQueryResultModel pomoFocusRecQueryResultModel = await Communicator.QueryPomoRecWithFilter(filterModel);
			if (pomoFocusRecQueryResultModel == null || !isCurrentUser())
			{
				return null;
			}
			pageToken = pomoFocusRecQueryResultModel.pageToken;
			_timelinePullPoint = -1L;
			focuses = pomoFocusRecQueryResultModel.results;
			if (focuses != null && focuses.Any())
			{
				await PomoSyncService.MergeFilteredRemotePage(focuses, focuses.LastOrDefault().StartTime, focuses.FirstOrDefault().EndTime, null);
			}
			return focuses;
		}
		catch (Exception)
		{
			throw;
		}
		finally
		{
			IsQuerying = false;
		}
	}

	public async Task LoadItems(IEnumerable<PomodoroModel> list)
	{
		if (!IsShowWindow)
		{
			return;
		}
		Dictionary<DateTime, List<CheckFocusRecordItemViewModel>> datePomoDict = new Dictionary<DateTime, List<CheckFocusRecordItemViewModel>>();
		foreach (PomodoroModel item2 in list)
		{
			PomodoroModel pomodoroModel = await PomoService.GetAdjustPomo(item2);
			PomoTask[] tasks = pomodoroModel.Tasks;
			DateTime date = pomodoroModel.EndTime.Date;
			CheckFocusRecordItemViewModel item = new CheckFocusRecordItemViewModel(pomodoroModel, date, tasks);
			if (datePomoDict.ContainsKey(date))
			{
				datePomoDict[date].Add(item);
			}
			else
			{
				datePomoDict[date] = new List<CheckFocusRecordItemViewModel> { item };
			}
			if (pomodoroModel.StartTime.Date != pomodoroModel.EndTime.Date)
			{
				date = pomodoroModel.StartTime.Date;
				item = new CheckFocusRecordItemViewModel(pomodoroModel, date, tasks);
				if (datePomoDict.ContainsKey(date))
				{
					datePomoDict[date].Add(item);
					continue;
				}
				datePomoDict[date] = new List<CheckFocusRecordItemViewModel> { item };
			}
		}
		IEnumerable<GroupCheckFocusRecordItemViewModel> source = FilterItems.OfType<GroupCheckFocusRecordItemViewModel>();
		int num = 0;
		foreach (KeyValuePair<DateTime, List<CheckFocusRecordItemViewModel>> kv in datePomoDict)
		{
			List<CheckFocusRecordItemViewModel> value = kv.Value;
			if (value != null && value.Count > 0)
			{
				kv.Value.Last().IsLastItem = true;
				GroupCheckFocusRecordItemViewModel groupCheckFocusRecordItemViewModel = source.FirstOrDefault((GroupCheckFocusRecordItemViewModel x) => x.DateTitle.Date == kv.Key);
				if (groupCheckFocusRecordItemViewModel != null)
				{
					foreach (CheckFocusRecordItemViewModel item3 in kv.Value)
					{
						groupCheckFocusRecordItemViewModel.Items.Add(item3);
					}
				}
				else
				{
					GroupCheckFocusRecordItemViewModel groupCheckFocusRecordItemViewModel2 = new GroupCheckFocusRecordItemViewModel
					{
						DateTitle = new CheckFocusRecordItemViewModel(kv.Key)
					};
					foreach (CheckFocusRecordItemViewModel item4 in kv.Value)
					{
						groupCheckFocusRecordItemViewModel2.Items.Add(item4);
					}
					FilterItems.Add(groupCheckFocusRecordItemViewModel2);
				}
			}
			num++;
		}
		if (!string.IsNullOrEmpty(pageToken) || _timelinePullPoint > 0)
		{
			FilterItems.Add(new FocusRecordLoadMoreViewModel());
		}
	}

	public async Task LoadMore()
	{
		if (!string.IsNullOrEmpty(pageToken) || _timelinePullPoint >= 0)
		{
			UpdateItems(clear: false);
		}
	}
}
