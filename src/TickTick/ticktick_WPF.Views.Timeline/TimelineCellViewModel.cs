using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TickTickUtils;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Timeline;

public class TimelineCellViewModel : TimelineDisplayBase
{
	public bool PlayingAnimation;

	private double _maxWidth = double.NaN;

	private bool _isAllDay;

	private SolidColorBrush _backgroundBrush;

	private SolidColorBrush _borderBrush;

	private SolidColorBrush _thumbBrush;

	private SolidColorBrush _foregroundBrush;

	private double _titleOpacity;

	private Geometry _icon;

	private bool _inline = true;

	private Visibility _visibility;

	private bool _isParent;

	private bool _isOpen = true;

	private TimelineCellOperation _operation = TimelineCellOperation.None;

	private DateTime _startDate = DateTime.Today;

	private DateTime? _endDate;

	private double _left;

	private int _line;

	private string _avatarUrl;

	private static Geometry _noteIcon;

	private static Geometry _abandonIcon;

	private bool _forceShowBorder;

	private int _dragStatus;

	private double _width;

	private int _level;

	public List<TimelineCellViewModel> ChildItems = new List<TimelineCellViewModel>();

	public int Status => DisplayModel?.Status ?? 0;

	public override string Title
	{
		get
		{
			string text = BatchText;
			if (text == null)
			{
				TaskBaseViewModel displayModel = DisplayModel;
				if (displayModel == null)
				{
					return null;
				}
				text = displayModel.TitleWithoutLink;
			}
			return text;
		}
	}

	public long SortOrder => DisplayModel?.SortOrder ?? 0;

	public string Id => DisplayModel?.Id;

	public int Progress => DisplayModel?.Progress ?? 0;

	public double MaxWidth
	{
		get
		{
			return _maxWidth;
		}
		set
		{
			ChangeAndNotify(ref _maxWidth, value, "MaxWidth");
		}
	}

	public bool IsAllDay
	{
		get
		{
			return _isAllDay;
		}
		set
		{
			if (_isAllDay != value)
			{
				ChangeAndNotify(ref _isAllDay, value, "IsAllDay");
			}
		}
	}

	public SolidColorBrush BackgroundBrush
	{
		get
		{
			return _backgroundBrush;
		}
		set
		{
			_backgroundBrush = value;
			OnPropertyChanged("BackgroundBrush");
		}
	}

	public SolidColorBrush BorderBrush
	{
		get
		{
			return _borderBrush;
		}
		set
		{
			_borderBrush = value;
			OnPropertyChanged("BorderBrush");
		}
	}

	public SolidColorBrush ThumbBrush
	{
		get
		{
			return _thumbBrush;
		}
		set
		{
			_thumbBrush = value;
			OnPropertyChanged("ThumbBrush");
		}
	}

	public SolidColorBrush ForegroundBrush
	{
		get
		{
			return _foregroundBrush;
		}
		set
		{
			_foregroundBrush = value;
			OnPropertyChanged("ForegroundBrush");
		}
	}

	public double TitleOpacity
	{
		get
		{
			return _titleOpacity;
		}
		set
		{
			ChangeAndNotify(ref _titleOpacity, value, "TitleOpacity");
		}
	}

	public Geometry Icon
	{
		get
		{
			return _icon;
		}
		set
		{
			ChangeAndNotify(ref _icon, value, "Icon");
		}
	}

	public bool Inline
	{
		get
		{
			if (!IsNew)
			{
				return _inline;
			}
			return true;
		}
		set
		{
			if (_inline != value)
			{
				_inline = value;
				OnPropertyChanged("Inline");
			}
		}
	}

	public double BorderThickness
	{
		get
		{
			TimelineViewModel parent = Parent;
			return ((parent != null && parent.ColorType == TimelineColorType.NoColor && !Parent.IsDark) || _forceShowBorder) ? 1 : 0;
		}
	}

	public bool IsNew { get; set; }

	public double Width
	{
		get
		{
			if (_width < 0.1)
			{
				SetWidth();
			}
			return _width;
		}
	}

	public Visibility Visibility
	{
		get
		{
			if (_dragStatus != 1)
			{
				return _visibility;
			}
			return Visibility.Visible;
		}
		set
		{
			_visibility = value;
			OnPropertyChanged("Visibility");
		}
	}

	public bool IsParent
	{
		get
		{
			return _isParent;
		}
		set
		{
			if (_isParent != value)
			{
				_isParent = value;
				OnPropertyChanged("IsParent");
			}
		}
	}

	public bool IsOpen
	{
		get
		{
			return _isOpen;
		}
		set
		{
			if (_isOpen != value)
			{
				_isOpen = value;
				OnPropertyChanged("IsOpen");
			}
		}
	}

	public string Tips
	{
		get
		{
			TimelineCellOperation pos = Operation.GetPos();
			DateTime? dateTime = EndDate?.AddDays(IsAllDay ? (-1) : 0);
			string text = string.Empty;
			if (pos != TimelineCellOperation.Full)
			{
				int num = ((!dateTime.HasValue) ? 1 : ((dateTime.Value.Date - StartDate.Date).Days + 1));
				text = $"{num} " + ((num > 1) ? Utils.GetString("PublicDays") : Utils.GetString("PublicDay"));
			}
			switch (pos)
			{
			case TimelineCellOperation.Start:
			{
				string text3 = StartDate.ToString("ddd", App.Ci);
				if (!Utils.IsEn())
				{
					return ticktick_WPF.Util.DateUtils.FormatMonthDay(StartDate, DateFormatLen.Middle) + " " + text3 + ", " + text;
				}
				return text3 + " " + ticktick_WPF.Util.DateUtils.FormatMonthDay(StartDate, DateFormatLen.Middle) + ", " + text;
			}
			case TimelineCellOperation.End:
			{
				DateTime date = dateTime ?? StartDate;
				string text2 = date.ToString("ddd", App.Ci);
				if (!Utils.IsEn())
				{
					return ticktick_WPF.Util.DateUtils.FormatMonthDay(date, DateFormatLen.Middle) + " " + text2 + ", " + text;
				}
				return text2 + " " + ticktick_WPF.Util.DateUtils.FormatMonthDay(date, DateFormatLen.Middle) + ", " + text;
			}
			default:
				if (!dateTime.HasValue)
				{
					return ticktick_WPF.Util.DateUtils.FormatMonthDay(StartDate, DateFormatLen.Middle) ?? "";
				}
				return ticktick_WPF.Util.DateUtils.FormatMonthDay(StartDate, DateFormatLen.Middle) + " - " + ticktick_WPF.Util.DateUtils.FormatMonthDay(dateTime.Value, DateFormatLen.Middle);
			}
		}
	}

	public TimelineCellOperation Operation
	{
		get
		{
			return _operation;
		}
		set
		{
			if (_operation == value)
			{
				return;
			}
			bool num = _operation.Contain(TimelineCellOperation.Edit) != value.Contain(TimelineCellOperation.Edit);
			bool flag = _operation.Contain(TimelineCellOperation.BatchSelect) != value.Contain(TimelineCellOperation.BatchSelect);
			_operation = value;
			if (_operation.RemovePos() == TimelineCellOperation.None || _operation.Contain(TimelineCellOperation.BatchSelect) || (_operation.Contain(TimelineCellOperation.Edit) && !_operation.Contain(TimelineCellOperation.Hover)))
			{
				if (Visibility != Visibility.Visible)
				{
					Visibility = Visibility.Visible;
				}
			}
			else if (Visibility != Visibility.Collapsed)
			{
				Visibility = Visibility.Collapsed;
			}
			if (value.IsEditingOrMoving())
			{
				OnPropertyChanged("Tips");
			}
			if (num || flag)
			{
				NotifyColorChanged(force: true);
			}
			if (flag)
			{
				Parent.OnItemBatchSelectChanged(this);
				OnPropertyChanged("BatchSelected");
			}
		}
	}

	public DateTime StartDate
	{
		get
		{
			return _startDate;
		}
		private set
		{
			if (_startDate != value)
			{
				_startDate = value;
				OnPropertyChanged("StartDate");
				OnPropertyChanged("Tips");
				SetWidth();
			}
		}
	}

	public DateTime? EndDate
	{
		get
		{
			return _endDate;
		}
		set
		{
			if (_endDate != value)
			{
				_endDate = value;
				OnPropertyChanged("EndDate");
				OnPropertyChanged("Tips");
				SetWidth();
			}
		}
	}

	public double Left
	{
		get
		{
			return _left;
		}
		set
		{
			if (!(Math.Abs(_left - value) < 0.05))
			{
				_left = value;
				OnPropertyChanged("Left");
				Parent?.NotifyItemPosChanged(this);
			}
		}
	}

	public double Top { get; private set; } = -1.0;

	public int Line
	{
		get
		{
			return _line;
		}
		set
		{
			if (_line != value)
			{
				_line = value;
				OnPropertyChanged("Line");
			}
			double num = (double)Line * 40.0;
			if (Math.Abs(Top - num) > 0.1)
			{
				Top = num;
				OnPropertyChanged("Top");
				Parent?.NotifyItemPosChanged(this);
			}
		}
	}

	public string AvatarUrl
	{
		get
		{
			return _avatarUrl ?? string.Empty;
		}
		set
		{
			_avatarUrl = value;
			OnPropertyChanged("AvatarUrl");
			SetAvatar();
		}
	}

	public BitmapImage Avatar { get; set; }

	public int DragStatus
	{
		get
		{
			return _dragStatus;
		}
		set
		{
			if (value != _dragStatus)
			{
				_dragStatus = value;
				OnPropertyChanged("Visibility");
				OnPropertyChanged("DragStatus");
				OnPropertyChanged("ArrangeOpacity");
			}
		}
	}

	public TimelineViewModel Parent { get; }

	public TimelineCellViewModel ParentItem { get; set; }

	public TaskBaseViewModel DisplayModel { get; private set; }

	public override DisplayType Type => DisplayModel.Type;

	public bool Editable => DisplayModel.Editable;

	public bool BatchSelected => Operation.Contain(TimelineCellOperation.BatchSelect);

	public int Level
	{
		get
		{
			return _level;
		}
		set
		{
			_level = value;
			OnPropertyChanged("Level");
		}
	}

	public double ArrangeOpacity
	{
		get
		{
			if (DragStatus == 0)
			{
				return 1.0;
			}
			return 0.4;
		}
	}

	public bool CanDragSwitchLine { get; set; } = true;

	public int? StartDragLine { get; set; }

	public bool DragFromArrange { get; set; }

	public string BatchText { get; set; }

	public Task ClearDate()
	{
		return TaskService.ClearDate(Id);
	}

	public async Task CommitDate(TimeData data)
	{
		if (DisplayModel.Type == DisplayType.CheckItem)
		{
			return;
		}
		TaskModel taskModel = await TaskDao.GetThinTaskById(Id);
		if (taskModel != null)
		{
			TimeData timeData = new TimeData
			{
				StartDate = StartDate,
				DueDate = EndDate,
				IsAllDay = IsAllDay,
				RepeatFlag = ((data == null) ? taskModel.repeatFlag : data.RepeatFlag),
				RepeatFrom = ((data == null) ? taskModel.repeatFrom : data.RepeatFrom),
				ExDates = ((data != null) ? data.ExDates : ExDateSerilizer.ToArray(taskModel.exDates)?.ToList()),
				TimeZone = ((data == null) ? new TimeZoneViewModel(taskModel.Floating, taskModel.timeZone) : data.TimeZone)
			};
			TimeData timeData2 = timeData;
			List<TaskReminderModel> reminders;
			if (data == null)
			{
				List<TaskReminderModel> list = (taskModel.startDate.HasValue ? (await TaskReminderDao.GetRemindersByTaskId(taskModel.id)) : TimeData.GetDefaultAllDayReminders());
				reminders = list;
			}
			else
			{
				reminders = data.Reminders;
			}
			timeData2.Reminders = reminders;
			await TaskService.SetDate(Id, timeData);
			SyncManager.TryDelaySync();
		}
	}

	public async Task BatchCommitGroup(int line, List<string> batchIds)
	{
		TimelineGroupViewModel group = Parent.GroupModels.LastOrDefault((TimelineGroupViewModel c) => c.Line <= line);
		if (group == null)
		{
			return;
		}
		if (batchIds == null || batchIds.Count <= 0)
		{
			return;
		}
		if (!group.IsOpen)
		{
			Operation = TimelineCellOperation.Fold;
		}
		switch (Parent.TimelineSortOption.groupBy)
		{
		case "priority":
		{
			if (int.TryParse(group.Id ?? "", out var result))
			{
				await TaskService.BatchSetPriority(batchIds, result);
			}
			return;
		}
		case "project":
		{
			ProjectModel projectById = CacheManager.GetProjectById(group.Id);
			if (projectById != null && projectById.IsEnable())
			{
				await TaskService.BatchMoveProject(TaskDao.GetTreeTopIds(batchIds, null), new MoveProjectArgs(projectById));
			}
			return;
		}
		case "assignee":
			foreach (string batchId in batchIds)
			{
				TaskBaseViewModel taskById = TaskCache.GetTaskById(batchId);
				if (!(taskById.Assignee != group.Id))
				{
					continue;
				}
				if (!string.IsNullOrEmpty(group.Id) && group.Id != "-1")
				{
					List<ShareUserModel> projectUsers = AvatarHelper.GetProjectUsers(taskById.ProjectId);
					if (projectUsers == null || projectUsers.Count == 0 || projectUsers.All((ShareUserModel a) => (a.userId.ToString() ?? "") != group.Id))
					{
						Utils.Toast(Utils.GetString("ChangeAssigneeError"));
						break;
					}
				}
				await TaskService.SetAssignee(taskById.Id, group.Id, notify: false);
			}
			return;
		}
		foreach (string batchId2 in batchIds)
		{
			TaskBaseViewModel task = TaskCache.GetTaskById(batchId2);
			if (task.ColumnId != group.Id)
			{
				task.ColumnId = group.Id;
				if (!string.IsNullOrEmpty(task.ParentId))
				{
					await TaskDao.UpdateParent(task.Id, string.Empty);
				}
				await TaskService.SaveTaskColumnId(task.Id, group.Id);
			}
		}
	}

	public async Task CommitGroup(int line)
	{
		TaskModel taskModel = await TaskDao.GetTaskById(Id);
		TimelineGroupViewModel group = Parent.GroupModels.LastOrDefault((TimelineGroupViewModel c) => c.Line <= line);
		if (group == null || taskModel == null)
		{
			return;
		}
		if (!group.IsOpen)
		{
			Operation = TimelineCellOperation.Fold;
		}
		switch (Parent.TimelineSortOption.groupBy)
		{
		case "priority":
		{
			if (int.TryParse(group.Id ?? "", out var result))
			{
				await TaskService.SetPriority(Id, result);
			}
			return;
		}
		case "project":
		{
			ProjectModel projectById = CacheManager.GetProjectById(group.Id);
			if (projectById != null && projectById.IsEnable() && taskModel.projectId != group.Id)
			{
				await TaskService.MoveProject(Id, group.Id, false);
			}
			return;
		}
		case "assignee":
			if (!(taskModel.assignee != group.Id))
			{
				return;
			}
			if (!string.IsNullOrEmpty(group.Id) && group.Id != "-1")
			{
				List<ShareUserModel> projectUsers = AvatarHelper.GetProjectUsers(taskModel.projectId);
				if (projectUsers == null || projectUsers.Count == 0 || projectUsers.All((ShareUserModel a) => (a.userId.ToString() ?? "") != group.Id))
				{
					Utils.Toast(Utils.GetString("ChangeAssigneeError"));
					return;
				}
			}
			await TaskService.SetAssignee(Id, group.Id, notify: false);
			return;
		}
		if (taskModel.columnId != group.Id)
		{
			taskModel.columnId = group.Id;
			if (!string.IsNullOrEmpty(taskModel.parentId))
			{
				await TaskDao.UpdateParent(taskModel.id, string.Empty);
			}
			await TaskService.SaveTaskColumnId(Id, group.Id);
		}
	}

	private void SetIcon()
	{
		if (DisplayModel == null)
		{
			Icon = null;
		}
		else
		{
			Icon = GetIcon(DisplayModel.Kind == "NOTE", DisplayModel.Status == -1);
		}
	}

	private static Geometry GetIcon(bool note, bool abandon)
	{
		if (note)
		{
			if (_noteIcon == null)
			{
				_noteIcon = Utils.GetGeometry("Ic.TaskType.Note");
			}
			return _noteIcon;
		}
		if (abandon)
		{
			if (_abandonIcon == null)
			{
				_abandonIcon = Utils.GetGeometry("Ic.Common.Close");
			}
			return _abandonIcon;
		}
		return null;
	}

	private void SetColor()
	{
		if (DisplayModel == null)
		{
			return;
		}
		bool isDark = Parent.IsDark;
		int status = DisplayModel.Status;
		int priority = DisplayModel.Priority;
		bool flag = Operation.Contain(TimelineCellOperation.Edit);
		bool flag2 = Operation.Contain(TimelineCellOperation.BatchSelect);
		TimelineColorType timelineColorType = ((!flag2) ? Parent.ColorType : TimelineColorType.NoColor);
		string empty = string.Empty;
		if (timelineColorType == TimelineColorType.NoColor)
		{
			if (status == 0)
			{
				TitleOpacity = (flag ? 1.0 : 0.9);
			}
			else
			{
				TitleOpacity = (flag ? 0.6 : ((!isDark) ? 0.3 : 0.2));
			}
		}
		else
		{
			TitleOpacity = ((status == 0 && flag) ? 1.0 : ((status == 0 || flag) ? 0.8 : (Parent.IsDark ? 0.2 : 0.4)));
		}
		empty = timelineColorType switch
		{
			TimelineColorType.Priority => (DisplayModel.Type == DisplayType.Note) ? ThemeUtil.GetColor("PrimaryColor").ToString() : Utils.GetPriorityColor(priority).ToString(), 
			TimelineColorType.List => DisplayModel.Color, 
			TimelineColorType.Tag => (from t in CacheManager.GetTags(DisplayModel?.Tags)
				orderby t.sortOrder
				select t).FirstOrDefault()?.color, 
			_ => (!isDark) ? ((Status == 0) ? "#FFFFFF" : "#F5F5F5") : ((Status == 0) ? "#3C3C3C" : "#2A2A2A"), 
		};
		if (isDark && empty == ThemeUtil.GetColor("BaseColorOpacity40").Color.ToString())
		{
			empty = "#66FFFFFF";
		}
		string text = empty?.ToLower();
		if (string.IsNullOrEmpty(empty) || empty.ToLower() == "transparent")
		{
			text = ThemeUtil.GetColorString("ColorPrimary", isDark);
		}
		double num = 1.0;
		if (text != null && text.StartsWith("#") && text.Length > 7)
		{
			num = (double)Convert.ToInt32(text.Substring(1, 2), 16) / 256.0;
		}
		int num2 = ((status != 0) ? (flag ? 40 : 20) : (flag ? 100 : 60));
		num2 = (int)((double)num2 * num);
		_forceShowBorder = flag2 || (num2 < 10 && !isDark);
		BorderBrush = ThemeUtil.GetColor(flag2 ? "PrimaryColor" : "BaseColorOpacity10");
		if (flag2)
		{
			BackgroundBrush = (isDark ? ThemeUtil.GetColorInDict(BorderBrush.Color.ToString(), 10) : ThemeUtil.GetBlendColor("#FFFFFF", BorderBrush.Color.ToString(), 1f, 0.05f));
			ThumbBrush = Brushes.Transparent;
			ForegroundBrush = ThemeUtil.GetColorInDict(isDark ? "#FFFFFF" : "#191919", 90);
			return;
		}
		double colorGrayscale = ColorUtils.GetColorGrayscale(text, num2, (!isDark) ? 1 : 0);
		if (!flag)
		{
			ThumbBrush = ThemeUtil.GetColorInDict((colorGrayscale <= 0.8) ? "#FFFFFF" : "#191919", ((colorGrayscale > 0.8 && status != 0) || (isDark && status != 0)) ? 20 : ((!isDark && colorGrayscale > 0.8 && status == 0) ? 40 : 60));
		}
		else
		{
			ThumbBrush = Brushes.Transparent;
		}
		if (!flag && !isDark && colorGrayscale < 0.5)
		{
			num2 = 40;
		}
		if (flag && !isDark && colorGrayscale < 0.167)
		{
			num2 = 80;
		}
		ForegroundBrush = ThemeUtil.GetColorInDict((isDark || (flag && Inline && colorGrayscale <= 0.65)) ? "#FFFFFF" : "#191919", 90);
		if (timelineColorType == TimelineColorType.NoColor)
		{
			num2 = 100;
		}
		BackgroundBrush = ThemeUtil.GetColorInDict(text, num2);
	}

	public void SetLeft()
	{
		Left = (double)(StartDate.Date - Parent.StartDate).Days * Parent.OneDayWidth;
	}

	public void SetWidth()
	{
		int val = 1;
		double oneDayWidth = Parent.OneDayWidth;
		DateTime? endDate = EndDate;
		if (endDate.HasValue)
		{
			val = (endDate.GetValueOrDefault().AddSeconds(-1.0).Date - StartDate.Date).Days + 1;
		}
		val = Math.Max(1, val);
		double num = Math.Max((double)val * oneDayWidth - 4.0, 6.0);
		if (Math.Abs(num - _width) > 0.1)
		{
			_width = num;
			Inline = num > 24.0;
			OnPropertyChanged("Width");
		}
	}

	public void TrySetStartDate(DateTime? time)
	{
		if (time.HasValue)
		{
			DateTime valueOrDefault = time.GetValueOrDefault();
			StartDate = valueOrDefault;
		}
		else
		{
			StartDate = default(DateTime);
		}
	}

	public bool SetLeft(double left, bool notify)
	{
		if (Math.Abs(_left - left) < 0.05)
		{
			return false;
		}
		_left = left;
		OnPropertyChanged("Left");
		if (notify)
		{
			Parent?.NotifyItemPosChanged(this);
		}
		return true;
	}

	public void SetLine(int line, bool notify)
	{
		if (_line != line)
		{
			_line = line;
			OnPropertyChanged("Line");
		}
		double num = (double)Line * 40.0;
		if (Math.Abs(Top - num) > 0.1)
		{
			Top = num;
			OnPropertyChanged("Top");
			if (notify)
			{
				Parent?.NotifyItemPosChanged(this);
			}
		}
	}

	public void SetLineValue(int line)
	{
		if (!_line.Equals(line))
		{
			_line = line;
		}
	}

	public async void SetAvatar()
	{
		Avatar = await AvatarHelper.GetAvatarByUrlAsync(AvatarUrl);
		OnPropertyChanged("Avatar");
	}

	public TimelineCellViewModel(TimelineViewModel parent, TaskBaseViewModel displayModel, bool bind = true)
	{
		TimelineCellViewModel timelineCellViewModel = this;
		Parent = parent ?? TimelineViewModel.Instance;
		if (bind)
		{
			Task.Run(delegate
			{
				PropertyChangedEventManager.AddHandler(displayModel, timelineCellViewModel.OnModelPropertyChanged, string.Empty);
			});
		}
		SetBaseVm(displayModel);
	}

	private void Rebind(TaskBaseViewModel remove, TaskBaseViewModel add)
	{
		if (remove != null)
		{
			PropertyChangedEventManager.RemoveHandler(remove, OnModelPropertyChanged, string.Empty);
		}
		if (add != null)
		{
			PropertyChangedEventManager.AddHandler(add, OnModelPropertyChanged, string.Empty);
		}
	}

	private void SetBaseVm(TaskBaseViewModel baseVm)
	{
		if (baseVm != null)
		{
			DisplayModel = baseVm;
			_startDate = DisplayModel.StartDate ?? DateTime.Today;
			_endDate = DisplayModel.DueDate;
			_isAllDay = (DisplayModel.IsAllDay ?? true) || !DisplayModel.StartDate.HasValue;
			_avatarUrl = ((!string.IsNullOrEmpty(DisplayModel.Assignee)) ? AvatarHelper.GetCacheUrl(DisplayModel.Assignee, DisplayModel.ProjectId) : string.Empty);
			SetIcon();
			SetAvatar();
		}
	}

	public void ResetBaseVm(TaskBaseViewModel baseVm)
	{
		TaskBaseViewModel old = DisplayModel;
		DisplayModel = baseVm;
		if (old != baseVm)
		{
			Task.Run(delegate
			{
				Rebind(old, baseVm);
			});
		}
		if (baseVm != null)
		{
			StartDate = DisplayModel.StartDate ?? DateTime.Today;
			EndDate = DisplayModel.DueDate;
			IsAllDay = (DisplayModel.IsAllDay ?? true) || !DisplayModel.StartDate.HasValue;
			_avatarUrl = ((!string.IsNullOrEmpty(DisplayModel.Assignee)) ? AvatarHelper.GetCacheUrl(DisplayModel.Assignee, DisplayModel.ProjectId) : string.Empty);
			OnPropertyChanged("Id");
			OnPropertyChanged("Title");
			OnPropertyChanged("Status");
			SetIcon();
			SetAvatar();
		}
	}

	private void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		string propertyName = e.PropertyName;
		if (propertyName == null)
		{
			return;
		}
		switch (propertyName.Length)
		{
		case 5:
			switch (propertyName[0])
			{
			default:
				return;
			case 'T':
				if (propertyName == "Title")
				{
					OnPropertyChanged("Title");
				}
				return;
			case 'C':
				break;
			}
			if (!(propertyName == "Color"))
			{
				break;
			}
			goto IL_01e9;
		case 8:
			switch (propertyName[2])
			{
			default:
				return;
			case 'A':
				if (propertyName == "IsAllDay" && sender is TaskBaseViewModel taskBaseViewModel)
				{
					IsAllDay = taskBaseViewModel.IsAllDay ?? true;
				}
				return;
			case 'i':
				break;
			case 's':
				if (propertyName == "Assignee")
				{
					AvatarUrl = ((!string.IsNullOrEmpty(DisplayModel?.Assignee)) ? AvatarHelper.GetCacheUrl(DisplayModel?.Assignee, DisplayModel?.ProjectId) : string.Empty);
				}
				return;
			case 'o':
				if (propertyName == "Progress")
				{
					OnPropertyChanged("Progress");
				}
				return;
			}
			if (!(propertyName == "Priority"))
			{
				break;
			}
			goto IL_01e9;
		case 3:
			if (propertyName == "Tag" && Parent.ColorType == TimelineColorType.Tag)
			{
				SetColor();
			}
			break;
		case 9:
			if (propertyName == "StartDate" && sender is TaskBaseViewModel taskBaseViewModel3)
			{
				TrySetStartDate(taskBaseViewModel3.StartDate);
			}
			break;
		case 7:
			if (propertyName == "DueDate" && sender is TaskBaseViewModel taskBaseViewModel2)
			{
				EndDate = taskBaseViewModel2.DueDate;
			}
			break;
		case 6:
			if (propertyName == "Status")
			{
				OnPropertyChanged("Status");
				Operation = Operation.Remove(TimelineCellOperation.Edit);
				SetColor();
				SetIcon();
			}
			break;
		case 4:
			{
				if (propertyName == "Kind")
				{
					SetIcon();
				}
				break;
			}
			IL_01e9:
			SetColor();
			break;
		}
	}

	public async Task SetColorAndAvatar()
	{
		Task.Yield();
		if (Avatar == null)
		{
			SetAvatar();
		}
		if (BackgroundBrush == null)
		{
			SetColor();
		}
	}

	public void NotifyWidthChanged()
	{
		OnPropertyChanged("Width");
	}

	public void NotifyColorChanged(bool force = false)
	{
		if (BackgroundBrush != null || force)
		{
			SetColor();
			OnPropertyChanged("BorderThickness");
		}
	}

	public double GetLeft()
	{
		if (_left < 0.0 && _left + Width > 0.0)
		{
			return 0.0;
		}
		return _left;
	}

	public void CheckAvatarUrl()
	{
		if (!string.IsNullOrEmpty(DisplayModel.Assignee) && DisplayModel.Assignee != "-1" && string.IsNullOrEmpty(_avatarUrl))
		{
			_avatarUrl = AvatarHelper.GetCacheUrl(DisplayModel.Assignee, DisplayModel.ProjectId);
		}
	}

	public bool AllParentItemOpen()
	{
		for (TimelineCellViewModel parentItem = ParentItem; parentItem != null; parentItem = parentItem.ParentItem)
		{
			if (!parentItem.IsOpen)
			{
				return false;
			}
		}
		return true;
	}

	public void ToggleArrangeItemOpen()
	{
		if (Parent != null)
		{
			Parent.ToggleArrangeItemOpen(this);
		}
	}

	public void UpdatePosition(double offset, TimelineViewModel timelineModel)
	{
		DateTime dateTime;
		bool num2;
		DateTime? dueDate;
		if (Operation.Contain(TimelineCellOperation.Start) || Operation.Contain(TimelineCellOperation.Full))
		{
			int num = (int)Math.Round(offset / Parent.OneDayWidth, 0, MidpointRounding.AwayFromZero);
			dateTime = Parent.StartDate.AddDays(num).AddHours(StartDate.Hour).AddMinutes(StartDate.Minute);
			int days = (dateTime - StartDate).Days;
			if (Operation.Contain(TimelineCellOperation.Full))
			{
				if (BatchSelected)
				{
					Parent.OnBatchDrag(days);
					return;
				}
				if (DisplayModel.StartDate.HasValue && DisplayModel.DueDate.HasValue)
				{
					double totalDays = (DisplayModel.DueDate - DisplayModel.StartDate).Value.TotalDays;
					_endDate = dateTime.AddDays(totalDays);
				}
				else if (timelineModel.TimelineRange == "month")
				{
					EndDate = dateTime.AddDays(5.0);
				}
				goto IL_0284;
			}
			if (DisplayModel.DueDate.HasValue)
			{
				DateTime dateTime2 = dateTime;
				dueDate = DisplayModel.DueDate;
				if (dueDate.HasValue)
				{
					num2 = dateTime2 > dueDate.GetValueOrDefault();
					goto IL_01ff;
				}
			}
			else
			{
				DateTime dateTime2 = dateTime;
				dueDate = DisplayModel.StartDate;
				if (dueDate.HasValue)
				{
					num2 = dateTime2 > dueDate.GetValueOrDefault();
					goto IL_01ff;
				}
			}
			goto IL_0202;
		}
		goto IL_0296;
		IL_0202:
		dueDate = EndDate;
		if (dueDate.HasValue)
		{
			DateTime valueOrDefault = dueDate.GetValueOrDefault();
			if (dateTime >= valueOrDefault.AddDays(IsAllDay ? (-1) : 0))
			{
				EndDate = null;
			}
		}
		else
		{
			EndDate = (IsAllDay ? StartDate.AddDays(1.0).Date : StartDate);
		}
		goto IL_0284;
		IL_0296:
		if (Operation.Contain(TimelineCellOperation.End))
		{
			double num3 = offset / Parent.OneDayWidth;
			if (num3 - (double)(int)num3 > 0.3)
			{
				num3 += 1.0;
			}
			if (IsAllDay)
			{
				if (Math.Abs(num3) == 0.0)
				{
					return;
				}
			}
			else
			{
				num3 -= 1.0;
			}
			if (num3 < 0.0)
			{
				return;
			}
			DateTime? dateTime3 = StartDate.Date.AddDays((int)num3);
			dueDate = EndDate;
			if (dueDate.HasValue)
			{
				DateTime valueOrDefault2 = dueDate.GetValueOrDefault();
				dateTime3 = dateTime3?.AddHours(valueOrDefault2.Hour).AddMinutes(valueOrDefault2.Minute);
			}
			else
			{
				dateTime3 = dateTime3?.AddHours(StartDate.Hour).AddMinutes(StartDate.Minute);
			}
			if (dateTime3?.AddDays(IsAllDay ? (-1) : 0) <= StartDate)
			{
				dateTime3 = null;
			}
			EndDate = dateTime3;
		}
		UpdateHoverBorderPosition(timelineModel);
		return;
		IL_01ff:
		if (num2)
		{
			return;
		}
		goto IL_0202;
		IL_0284:
		TrySetStartDate(dateTime);
		SetLeft();
		goto IL_0296;
	}

	public void UpdateHoverBorderPosition(TimelineViewModel timelineModel)
	{
		DateTime item = EndDate?.AddSeconds(-1.0).Date ?? StartDate.Date;
		timelineModel.HoverStartEndTuple = new Tuple<DateTime, DateTime>(StartDate.Date, item);
	}

	~TimelineCellViewModel()
	{
		if (DisplayModel != null)
		{
			PropertyChangedEventManager.RemoveHandler(DisplayModel, OnModelPropertyChanged, string.Empty);
		}
	}

	public void ResetDate()
	{
		StartDate = DisplayModel.StartDate ?? DateTime.Today;
		EndDate = DisplayModel.DueDate;
		IsAllDay = (DisplayModel.IsAllDay ?? true) || !DisplayModel.StartDate.HasValue;
	}

	public void SetBatchText(int count)
	{
		BatchText = ((count > 0) ? string.Format(Utils.GetString("MultiTasks"), count) : null);
		OnPropertyChanged("Title");
	}
}
