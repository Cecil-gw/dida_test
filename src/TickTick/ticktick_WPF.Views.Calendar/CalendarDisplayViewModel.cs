using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.DragAndDrop;
using ticktick_WPF.Util.KotlinUtils;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.Views.Calendar;

public class CalendarDisplayViewModel : INotifyPropertyChanged, AgendaHelper.IAgenda
{
	public readonly List<CalendarDisplayViewModel> Children = new List<CalendarDisplayViewModel>();

	private TaskBaseViewModel _sourceViewModel;

	private double _repeatDiff;

	private bool _dragging;

	private bool _batchDragging;

	private bool _isOpened = true;

	private bool _selected;

	private bool _batchSelected;

	private bool _tempBatchSelected;

	private SolidColorBrush _timeColor;

	private SolidColorBrush _titleColor;

	private SolidColorBrush _backColor;

	private SolidColorBrush _leftRectColor;

	private SolidColorBrush _backBorderColor;

	private SolidColorBrush _iconColor;

	private Thickness _backBorderThickness;

	private int _level;

	public int Column;

	public int ColumnSpan;

	public int Row;

	public CourseDisplayModel Course;

	public bool _inDark;

	private bool _inArrange;

	private static Dictionary<string, Geometry> _icons = new Dictionary<string, Geometry>();

	private const int ClassicPriorityBackOpacity = 56;

	private static BlockingSet<string> _errorColors = new BlockingSet<string>();

	private TaskCell _taskCell;

	private TaskBar _taskBar;

	public CalendarDisplayViewModel Parent { get; set; }

	public TaskBaseViewModel SourceViewModel
	{
		get
		{
			return _sourceViewModel;
		}
		set
		{
			_sourceViewModel = value;
			if (value != null)
			{
				Status = ((!(RepeatDiff > 0.0)) ? value.Status : 0);
			}
			else
			{
				Status = 0;
			}
		}
	}

	public double RepeatDiff
	{
		get
		{
			return _repeatDiff;
		}
		set
		{
			_repeatDiff = value;
			if (SourceViewModel != null)
			{
				Status = ((!(value > 0.0)) ? SourceViewModel.Status : 0);
			}
			else
			{
				Status = 0;
			}
		}
	}

	public string AttendId => SourceViewModel.AttendId;

	public string EventId
	{
		get
		{
			if (!IsCalendarEvent)
			{
				return string.Empty;
			}
			return SourceViewModel.Id;
		}
	}

	public string HabitId
	{
		get
		{
			if (!IsHabit)
			{
				return string.Empty;
			}
			return SourceViewModel.Id;
		}
	}

	public string PomoId
	{
		get
		{
			if (!IsPomo)
			{
				return string.Empty;
			}
			return SourceViewModel.Id;
		}
	}

	public string ItemId
	{
		get
		{
			if (!IsCheckItem)
			{
				return string.Empty;
			}
			return SourceViewModel.Id;
		}
	}

	public string ProjectId => SourceViewModel.ProjectId;

	public string RepeatFlag => SourceViewModel.RepeatFlag;

	public string RepeatFrom => SourceViewModel.RepeatFrom;

	public string TaskId
	{
		get
		{
			if (!IsTaskOrNote)
			{
				return string.Empty;
			}
			return SourceViewModel.Id;
		}
	}

	public string TimeText { get; set; }

	public Visibility TimeTextVisibility
	{
		get
		{
			if (!string.IsNullOrEmpty(TimeText))
			{
				return Visibility.Visible;
			}
			return Visibility.Collapsed;
		}
	}

	public bool ShowTimeText { get; set; } = true;

	public bool IsTaskOrNote
	{
		get
		{
			if (Type != DisplayType.Task && Type != DisplayType.Note)
			{
				return Type == DisplayType.Derivative;
			}
			return true;
		}
	}

	public bool IsTaskIconType
	{
		get
		{
			if (Type != DisplayType.Task && Type != DisplayType.CheckItem)
			{
				return Type == DisplayType.Agenda;
			}
			return true;
		}
	}

	public bool IsCountdown => Type == DisplayType.Countdown;

	public bool IsDerivative => Type == DisplayType.Derivative;

	public bool IsCalendarEvent => Type == DisplayType.Event;

	public bool IsSection => Type == DisplayType.Section;

	public bool IsHabit => Type == DisplayType.Habit;

	public bool IsPomo => Type == DisplayType.Pomo;

	public bool IsCheckItem => Type == DisplayType.CheckItem;

	public bool ShowInFocus { get; set; }

	public bool IsLoadMore { get; set; }

	public string SectionStatusName { get; set; }

	public bool ShowIcon => Icon != null;

	public Visibility IconVisible
	{
		get
		{
			if (!ShowIcon)
			{
				return Visibility.Collapsed;
			}
			return Visibility.Visible;
		}
	}

	public SolidColorBrush IconColor
	{
		get
		{
			return _iconColor;
		}
		set
		{
			_iconColor = value;
			OnPropertyChanged("IconColor");
		}
	}

	public bool CanDrag => SourceViewModel.Editable;

	public string CalendarId => SourceViewModel.CalendarId;

	public bool DragMode { get; set; }

	public TimeSpan? DragDiff { get; set; }

	public bool IsDragDisplayItem { get; set; }

	public Geometry Icon { get; set; }

	public bool IsCompleted => Status == 2;

	public bool IsAbandoned => Status == -1;

	public DisplayType Type
	{
		get
		{
			if (!(RepeatDiff > 0.0))
			{
				return SourceViewModel?.Type ?? DisplayType.Task;
			}
			return DisplayType.Derivative;
		}
	}

	public double DragVOffset { get; set; }

	public bool ToPoint { get; set; }

	public bool IsOpened
	{
		get
		{
			return _isOpened;
		}
		set
		{
			_isOpened = value;
			OnPropertyChanged("IsOpened");
		}
	}

	public int Priority => SourceViewModel.Priority;

	public string Title => SourceViewModel.TitleWithoutLink;

	public string Color { get; set; }

	public int Status { get; set; }

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

	public double Opacity
	{
		get
		{
			if (!Dragging)
			{
				return 1.0;
			}
			return 0.4;
		}
	}

	public bool Dragging
	{
		get
		{
			if (!_dragging)
			{
				return _batchDragging;
			}
			return true;
		}
		set
		{
			_dragging = value;
			OnPropertyChanged("Dragging");
			OnPropertyChanged("Opacity");
			SetColor();
		}
	}

	public bool BatchDragging
	{
		get
		{
			return _batchDragging;
		}
		set
		{
			if (_batchDragging != value)
			{
				_batchDragging = value;
				if (value)
				{
					_tempBatchSelected = false;
				}
				OnPropertyChanged("Opacity");
				SetColor();
			}
		}
	}

	public bool TempBatchSelect
	{
		get
		{
			return _tempBatchSelected;
		}
		set
		{
			_tempBatchSelected = value;
			SetColor();
		}
	}

	public bool BatchSelected
	{
		get
		{
			if (!_tempBatchSelected)
			{
				return _batchSelected;
			}
			return true;
		}
		set
		{
			_batchSelected = value;
			_tempBatchSelected = false;
			SetColor();
		}
	}

	public bool Selected
	{
		get
		{
			return _selected;
		}
		set
		{
			_selected = value;
			SetColor();
			OnPropertyChanged("Selected");
		}
	}

	public string Tips { get; set; }

	public bool? IsAllDay
	{
		get
		{
			if (!ToPoint)
			{
				return SourceViewModel.IsAllDay;
			}
			return false;
		}
	}

	public DateTime? StartDate
	{
		get
		{
			DateTime? startDate = SourceViewModel.StartDate;
			if (!startDate.HasValue)
			{
				if (!ToPoint)
				{
					return null;
				}
				return SourceViewModel.CompletedTime;
			}
			return startDate;
		}
	}

	public DateTime? DueDate
	{
		get
		{
			if (!ToPoint)
			{
				return SourceViewModel.DueDate;
			}
			return null;
		}
	}

	public DateTime? DisplayStartDate
	{
		get
		{
			if (!SourceViewModel.StartDate.HasValue)
			{
				if (!ToPoint)
				{
					return null;
				}
				return SourceViewModel.CompletedTime?.AddDays(RepeatDiff + ((!Dragging && (!IsDragDisplayItem || TickDragHelper.Dropped)) ? 0.0 : (DragDiff?.TotalDays ?? 0.0)));
			}
			return ToPoint ? SourceViewModel.StartDate.Value.Date.AddDays(RepeatDiff + ((!Dragging && (!IsDragDisplayItem || TickDragHelper.Dropped)) ? 0.0 : (DragDiff?.TotalDays ?? 0.0))) : SourceViewModel.StartDate.Value.AddDays(RepeatDiff + ((!Dragging && (!IsDragDisplayItem || TickDragHelper.Dropped)) ? 0.0 : (DragDiff?.TotalDays ?? 0.0)));
		}
	}

	public DateTime? DisplayDueDate
	{
		get
		{
			if (!ToPoint)
			{
				return SourceViewModel.DueDate?.AddDays(RepeatDiff + ((!Dragging && (!IsDragDisplayItem || TickDragHelper.Dropped)) ? 0.0 : (DragDiff?.TotalDays ?? 0.0)));
			}
			return null;
		}
	}

	public SolidColorBrush TitleColor
	{
		get
		{
			return _titleColor;
		}
		set
		{
			_titleColor = value;
			OnPropertyChanged("TitleColor");
		}
	}

	public SolidColorBrush TimeColor
	{
		get
		{
			return _timeColor;
		}
		set
		{
			_timeColor = value;
			OnPropertyChanged("TimeColor");
		}
	}

	public SolidColorBrush LeftRectColor
	{
		get
		{
			return _leftRectColor;
		}
		set
		{
			_leftRectColor = value;
			OnPropertyChanged("LeftRectColor");
		}
	}

	public SolidColorBrush BackColor
	{
		get
		{
			return _backColor;
		}
		set
		{
			_backColor = value;
			OnPropertyChanged("BackColor");
		}
	}

	public SolidColorBrush BackBorderColor
	{
		get
		{
			return _backBorderColor;
		}
		set
		{
			_backBorderColor = value;
			OnPropertyChanged("BackBorderColor");
		}
	}

	public Thickness BackBorderThickness
	{
		get
		{
			return _backBorderThickness;
		}
		set
		{
			_backBorderThickness = value;
			OnPropertyChanged("BackBorderThickness");
		}
	}

	public double LoadMoreWidth { get; set; }

	public bool ForceShowIcon
	{
		get
		{
			if (SourceViewModel != null)
			{
				if (!IsDerivative)
				{
					return SourceViewModel.Status == -1;
				}
				return true;
			}
			return false;
		}
	}

	public bool WidthEnoughShowIcon { get; set; }

	public bool CanBatchSelected
	{
		get
		{
			if (IsTaskOrNote && RepeatDiff < 1.0)
			{
				return SourceViewModel.Editable;
			}
			return false;
		}
	}

	public DateTime? DragStartDate { get; set; }

	public event PropertyChangedEventHandler PropertyChanged;

	protected void OnPropertyChanged(string propertyName = null)
	{
		Application.Current?.Dispatcher?.InvokeAsync(delegate
		{
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
			_taskCell?.OnTaskPropertyChanged(propertyName);
			_taskBar?.OnTaskPropertyChanged(propertyName);
		});
	}

	public string GetTaskId()
	{
		return SourceViewModel.GetTaskId();
	}

	public string GetAttendId()
	{
		return AttendId;
	}

	public static Geometry GetIcon(string key)
	{
		if (_icons.TryGetValue(key, out var value))
		{
			return value;
		}
		value = Utils.GetGeometry(key);
		_icons[key] = value;
		return value;
	}

	public void SetIcon(bool notify = false)
	{
		if (!ForceShowIcon && !IsConfigShowIcon() && !StaticPropertiesModel.Instance.AltKeyDown && SourceViewModel.ResponseStatusType != ResponseStatusType.Declined)
		{
			if (Icon != null)
			{
				Icon = null;
				OnPropertyChanged("Icon");
				OnPropertyChanged("ShowIcon");
				OnPropertyChanged("IconVisible");
			}
			return;
		}
		switch (Type)
		{
		case DisplayType.Note:
			Icon = GetIcon("Ic.CalendarType.Note");
			break;
		case DisplayType.Countdown:
			Icon = GetIcon("Ic.CalendarType.Countdown");
			break;
		case DisplayType.Derivative:
			Icon = GetIcon("Ic.CalendarType.Repeat");
			break;
		case DisplayType.CheckItem:
			if (Status == 0)
			{
				Icon = GetIcon("Ic.CalendarType.Checkitem");
			}
			else
			{
				Icon = GetIcon("Ic.CalendarType.Completed");
			}
			break;
		case DisplayType.Event:
		{
			int accountType = SubscribeCalendarHelper.GetAccountType(CacheManager.GetAccountCalById(CacheManager.GetBindCalendarById(SourceViewModel.CalendarId)?.AccountId));
			if (SourceViewModel.ResponseStatusType == ResponseStatusType.Declined)
			{
				if (IsConfigShowIcon() || StaticPropertiesModel.Instance.AltKeyDown)
				{
					Icon = GetIcon("Ic.CalendarType.Refuse");
				}
				else
				{
					Icon = GetIcon("Ic.CalendarType.No");
				}
				break;
			}
			switch (accountType)
			{
			case 1:
				Icon = GetIcon("Ic.CalendarType.Google");
				break;
			case 2:
				Icon = GetIcon("Ic.CalendarType.Outlook");
				break;
			case 3:
				Icon = GetIcon("Ic.CalendarType.Exchange");
				break;
			case 4:
				Icon = GetIcon("Ic.CalendarType.ICloud");
				break;
			case 5:
				Icon = GetIcon("Ic.CalendarType.Caldav");
				break;
			case 6:
				Icon = GetIcon("Ic.CalendarType.Url");
				break;
			default:
				Icon = GetIcon("Ic.CalendarType.CalendarSubscribe");
				break;
			}
			break;
		}
		case DisplayType.Agenda:
			Icon = GetIcon("Ic.CalendarType.Schedule");
			break;
		case DisplayType.Task:
			if (Status == -1)
			{
				if (IsConfigShowIcon() || StaticPropertiesModel.Instance.AltKeyDown)
				{
					Icon = GetIcon("Ic.CalendarType.Abandon");
				}
				else
				{
					Icon = GetIcon("Ic.CalendarType.No");
				}
			}
			else if (Status == 2)
			{
				Icon = GetIcon("Ic.CalendarType.Completed");
			}
			else
			{
				Icon = ((SourceViewModel.Kind == "CHECKLIST") ? GetIcon("Ic.CalendarType.CheckitemTitle") : GetIcon("Ic.CalendarType.Uncompleted"));
			}
			break;
		case DisplayType.Habit:
			if (IsAbandoned)
			{
				Icon = GetIcon("Ic.CalendarType.HabitAbandon");
			}
			else if (IsCompleted)
			{
				Icon = GetIcon("Ic.CalendarType.HabitComplete");
			}
			else
			{
				Icon = GetIcon("Ic.CalendarType.Habit");
			}
			break;
		case DisplayType.Pomo:
			Icon = GetIcon((SourceViewModel.PomoType == 0) ? "Ic.CalendarType.Pomo" : "Ic.CalendarType.Focus");
			break;
		case DisplayType.Course:
			Icon = GetIcon("Ic.CalendarType.Course");
			break;
		}
		if (notify)
		{
			OnPropertyChanged("Icon");
		}
		OnPropertyChanged("ShowIcon");
		OnPropertyChanged("IconVisible");
	}

	public static CalendarDisplayViewModel Build(CalendarDisplayModel model)
	{
		if (model == null)
		{
			return new CalendarDisplayViewModel();
		}
		string displayColor = GetDisplayColor(model.SourceViewModel, model.CourseDisplayModel, model.IsVirtual);
		CalendarDisplayViewModel calendarDisplayViewModel = new CalendarDisplayViewModel();
		calendarDisplayViewModel.Color = displayColor;
		calendarDisplayViewModel.Course = model.CourseDisplayModel;
		calendarDisplayViewModel.RepeatDiff = model.repeatDiff;
		calendarDisplayViewModel.TimeText = ((model.CourseDisplayModel == null) ? null : ticktick_WPF.Util.DateUtils.FormatHourMinuteText(model.CourseDisplayModel.CourseStart));
		calendarDisplayViewModel.SourceViewModel = model.SourceViewModel;
		calendarDisplayViewModel.SetIcon();
		return calendarDisplayViewModel;
	}

	public async void SetSourceModel(TaskBaseViewModel vm, bool setEvent = false)
	{
		TaskBaseViewModel old = SourceViewModel;
		SourceViewModel = vm;
		if (setEvent)
		{
			await Task.Delay(new Random().Next(50, 200));
		}
		if (old != null)
		{
			PropertyChangedEventManager.RemoveHandler(old, OnTaskPropertyChanged, string.Empty);
		}
		if (setEvent && vm != null)
		{
			PropertyChangedEventManager.AddHandler(vm, OnTaskPropertyChanged, string.Empty);
		}
		RefreshDisplayStyle();
	}

	private void OnTaskPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (Course != null)
		{
			return;
		}
		string propertyName = e.PropertyName;
		if (propertyName == null)
		{
			return;
		}
		switch (propertyName.Length)
		{
		default:
			return;
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
				return;
			}
			break;
		case 8:
		{
			char c = propertyName[0];
			if (c != 'I')
			{
				if (c != 'P' || !(propertyName == "Priority"))
				{
					return;
				}
				break;
			}
			if (!(propertyName == "IsAllDay"))
			{
				return;
			}
			goto IL_012e;
		}
		case 4:
			switch (propertyName[0])
			{
			default:
				return;
			case 'T':
				if (!(propertyName == "Type"))
				{
					return;
				}
				break;
			case 'K':
				if (!(propertyName == "Kind"))
				{
					return;
				}
				break;
			}
			break;
		case 9:
			if (!(propertyName == "StartDate"))
			{
				return;
			}
			goto IL_012e;
		case 7:
			if (!(propertyName == "DueDate"))
			{
				return;
			}
			goto IL_012e;
		case 3:
			if (!(propertyName == "Tag"))
			{
				return;
			}
			break;
		case 6:
			if (!(propertyName == "Status"))
			{
				return;
			}
			break;
		case 10:
			{
				if (!(propertyName == "CalendarId"))
				{
					return;
				}
				break;
			}
			IL_012e:
			SetTimeText(this is TaskCellViewModel);
			return;
		}
		RefreshDisplayStyle();
	}

	public void RefreshDisplayStyle(bool notifyIcon = true)
	{
		if (SourceViewModel == null)
		{
			Color = null;
			Icon = null;
			OnPropertyChanged("Icon");
			OnPropertyChanged("ShowIcon");
			OnPropertyChanged("IconVisible");
		}
		else
		{
			Status = SourceViewModel.Status;
			Color = GetDisplayColor(SourceViewModel, Course);
			SetIcon(notifyIcon);
			SetColor();
		}
	}

	public static CalendarDisplayViewModel Build(TaskBaseViewModel task)
	{
		return Build(new CalendarDisplayModel(task));
	}

	public CalendarDisplayViewModel Clone()
	{
		return new CalendarDisplayViewModel
		{
			SourceViewModel = SourceViewModel.Copy(),
			Icon = Icon,
			Color = Color,
			IconColor = IconColor,
			_backColor = _backColor,
			TitleColor = TitleColor,
			TimeColor = TimeColor,
			_backBorderColor = _backBorderColor,
			_backBorderThickness = _backBorderThickness,
			RepeatDiff = RepeatDiff,
			WidthEnoughShowIcon = WidthEnoughShowIcon,
			BatchSelected = BatchSelected,
			Row = Row
		};
	}

	public CalendarDisplayViewModel MemberwiseClone1()
	{
		return MemberwiseClone() as CalendarDisplayViewModel;
	}

	public void SetTheme(bool isDark)
	{
		_inDark = isDark;
		SetColor();
	}

	public void SetColor()
	{
		if (ShowInFocus)
		{
			SetFocusViewItemColor();
		}
		else
		{
			if (SourceViewModel == null)
			{
				return;
			}
			CalendarConfig.CalendarItemStyle calendarItemStyle = LocalSettings.Settings?.UserPreference?.CalendarConfig?.ItemStyleType ?? CalendarConfig.CalendarItemStyle.Classic;
			bool flag = Selected || DragMode || _batchSelected;
			if (_inDark && Color == "#66191919")
			{
				Color = "#66FFFFFF";
			}
			else if (!_inDark && Color == "#66FFFFFF")
			{
				Color = "#66191919";
			}
			(string color, double opacity) baseColorAndOpacity = GetBaseColorAndOpacity();
			string text = baseColorAndOpacity.color;
			double item = baseColorAndOpacity.opacity;
			int leftRectOpacity = GetLeftRectOpacity(calendarItemStyle);
			(string color, double opacity) borderColorAndOpacity = GetBorderColorAndOpacity(calendarItemStyle, flag, text);
			string item2 = borderColorAndOpacity.color;
			double item3 = borderColorAndOpacity.opacity;
			int num = (BatchSelected ? 10 : GetBackOpacity(calendarItemStyle, flag));
			double colorGrayscale = ColorUtils.GetColorGrayscale(text, num, (!_inDark) ? 1 : 0);
			num = (int)((double)num * item);
			(string color, int opacity) titleColorAndOpacity = GetTitleColorAndOpacity(calendarItemStyle, flag, colorGrayscale, text);
			string item4 = titleColorAndOpacity.color;
			int item5 = titleColorAndOpacity.opacity;
			int iconOpacity = GetIconOpacity(calendarItemStyle, flag);
			int timeTextOpacity = GetTimeTextOpacity(calendarItemStyle, flag, item5);
			IconColor = ThemeUtil.GetColorInDict(item4, iconOpacity);
			TimeColor = ThemeUtil.GetColorInDict(item4, timeTextOpacity);
			TitleColor = ThemeUtil.GetColorInDict(item4, item5);
			string color = text;
			try
			{
				if (calendarItemStyle == CalendarConfig.CalendarItemStyle.Modern)
				{
					if (Status != 0 || IsPomo)
					{
						Color color2 = ((!_inDark) ? new SolidColorBrush(ThemeUtil.GetColorValue("black")).Color : new SolidColorBrush(ThemeUtil.GetColorValue("white")).Color);
						if (!string.IsNullOrEmpty(text) && (text.Length == 6 || text.Length == 8) && !text.StartsWith("#"))
						{
							text = "#" + text;
						}
						Color color3 = ColorHelper.BlendWithAlpha(ThemeUtil.GetColorInDict(color2.ToString(), 2).Color, (Color)ColorConverter.ConvertFromString(text));
						color = ColorHelper.ColorConverter.ConvertToString(color3);
					}
				}
				else if (IsCountdown && flag)
				{
					Color color4 = ((!_inDark) ? new SolidColorBrush(ThemeUtil.GetColorValue("black")).Color : new SolidColorBrush(ThemeUtil.GetColorValue("white")).Color);
					if (!string.IsNullOrEmpty(text) && (text.Length == 6 || text.Length == 8) && !text.StartsWith("#"))
					{
						text = "#" + text;
					}
					Color color5 = ColorHelper.BlendWithAlpha(ThemeUtil.GetColorInDict(color4.ToString(), 8).Color, (Color)ColorConverter.ConvertFromString(text));
					color = ColorHelper.ColorConverter.ConvertToString(color5);
				}
			}
			catch (Exception ex)
			{
				UtilLog.Error($"{text} SetColorBlendError : {ex.Message} , type: {Type} ,taskId: {TaskId}");
			}
			BackColor = ThemeUtil.GetColorInDict(color, DragMode ? 100 : num);
			BackBorderColor = ThemeUtil.GetColorInDict(item2, (int)item3);
			BackBorderThickness = ((BatchSelected || (IsPomo && calendarItemStyle != CalendarConfig.CalendarItemStyle.Modern)) ? new Thickness(1.0) : new Thickness(0.0));
			if (calendarItemStyle == CalendarConfig.CalendarItemStyle.Modern)
			{
				LeftRectColor = ThemeUtil.GetColorInDict(text, leftRectOpacity);
			}
		}
	}

	public static string TryGetColor(string taskColor, string taskTag, int taskPriority)
	{
		string result = taskColor;
		switch (CalendarUtils.GetTaskColorMode())
		{
		case CalTaskColorMode.Tag:
			result = (from t in CacheManager.GetTags(TagSerializer.ToTags(taskTag)?.ToArray())
				orderby t.sortOrder
				select t).FirstOrDefault()?.color;
			break;
		case CalTaskColorMode.Priority:
			result = Utils.GetPriorityColor(taskPriority).Color.ToString();
			break;
		}
		return result;
	}

	public static string GetDisplayColor(TaskBaseViewModel sourceViewModel, CourseDisplayModel course = null, bool isVirtual = false)
	{
		if (sourceViewModel == null)
		{
			return null;
		}
		if (isVirtual || course != null || sourceViewModel.IsEvent || sourceViewModel.IsCourse)
		{
			return sourceViewModel.Color;
		}
		return TryGetColor(sourceViewModel.Color, sourceViewModel.Tag, sourceViewModel.Priority);
	}

	private (string color, double opacity) GetBaseColorAndOpacity()
	{
		string text = Color;
		if (BatchSelected)
		{
			text = ThemeUtil.GetColorString("ColorPrimary", _inDark);
		}
		if (SourceViewModel.IsCountdown)
		{
			text = ((!_selected && !_batchSelected) ? "#FFAE58" : "#FF9320");
			CalendarColorConfigItem calendarColorConfigItem = LocalSettings.Settings?.UserPreference?.CalendarColorConfig?.Countdown;
			if (calendarColorConfigItem != null && Enum.TryParse<CalCountdownColorMode>(calendarColorConfigItem.ColorType, ignoreCase: true, out var result))
			{
				switch (result)
				{
				case CalCountdownColorMode.Shared:
					if (!string.IsNullOrEmpty(calendarColorConfigItem.SharedColor))
					{
						text = calendarColorConfigItem.SharedColor;
					}
					break;
				case CalCountdownColorMode.Custom:
					if (calendarColorConfigItem?.ColorMap != null && calendarColorConfigItem.ColorMap.Any())
					{
						CountdownModel countdownModel = (SourceViewModel as CountDownBaseViewModel)?.Countdown;
						if (countdownModel != null && calendarColorConfigItem.ColorMap.TryGetValue(countdownModel.Id, out var value) && !string.IsNullOrEmpty(value))
						{
							text = value;
						}
					}
					break;
				}
			}
		}
		else if (SourceViewModel.IsEvent || SourceViewModel.IsCourse)
		{
			text = ((!string.IsNullOrEmpty(SourceViewModel.Color) && SourceViewModel.Color.ToLower() != "transparent") ? SourceViewModel.Color : ThemeUtil.GetColorString("ColorPrimary", _inDark));
		}
		else if (SourceViewModel.IsHabit)
		{
			text = ThemeUtil.GetColorString("ColorPrimary", _inDark);
			CalendarColorConfigItem calendarColorConfigItem2 = LocalSettings.Settings?.UserPreference?.CalendarColorConfig?.Habit;
			if (calendarColorConfigItem2 != null && Enum.TryParse<CalHabitColorMode>(calendarColorConfigItem2.ColorType, ignoreCase: true, out var result2))
			{
				switch (result2)
				{
				case CalHabitColorMode.Shared:
					if (!string.IsNullOrEmpty(calendarColorConfigItem2.SharedColor))
					{
						text = calendarColorConfigItem2.SharedColor;
					}
					break;
				case CalHabitColorMode.Custom:
					if (calendarColorConfigItem2?.ColorMap != null && calendarColorConfigItem2.ColorMap.Any())
					{
						HabitModel habitModel = (SourceViewModel as HabitBaseViewModel)?.Habit;
						if (habitModel != null && calendarColorConfigItem2.ColorMap.TryGetValue(habitModel.Id, out var value2) && !string.IsNullOrEmpty(value2))
						{
							text = value2;
						}
					}
					break;
				}
			}
		}
		else if (string.IsNullOrEmpty(Color) || Color.ToLower() == "transparent")
		{
			text = ThemeUtil.GetColorString("ColorPrimary", _inDark);
		}
		if (!BatchSelected && IsClassicPriorityTaskColorMode())
		{
			TaskBaseViewModel sourceViewModel = SourceViewModel;
			text = ((sourceViewModel != null && sourceViewModel.Priority == 0) ? GetClassicPriorityNoColor() : ThemeUtil.GetNoAlphaColorString(text));
		}
		double item = 1.0;
		if (text != null && text.Length > 7)
		{
			string value3 = text.Substring(1, 2);
			try
			{
				item = (double)Convert.ToInt32(value3, 16) / 256.0;
			}
			catch (Exception)
			{
				if (!_errorColors.Contains(text))
				{
					_errorColors.Add(text);
					UtilLog.Info("GetColorAlphaError : " + text);
				}
			}
		}
		return (color: text, opacity: item);
	}

	private int GetBackOpacity(CalendarConfig.CalendarItemStyle calendarTaskbarStyle, bool selected)
	{
		int num = 100;
		if (calendarTaskbarStyle != CalendarConfig.CalendarItemStyle.Modern)
		{
			if (IsPomo)
			{
				return 10;
			}
			if (IsCountdown)
			{
				return 100;
			}
			if (Status == 0 && !IsPomo)
			{
				return selected ? 100 : (IsClassicPriorityTaskColorMode() ? 56 : 60);
			}
			return selected ? 40 : 20;
		}
		if (Status == 0 && !IsPomo)
		{
			return selected ? 40 : 30;
		}
		return selected ? 20 : 8;
	}

	private bool IsClassicPriorityTaskColorMode()
	{
		if ((LocalSettings.Settings?.UserPreference?.CalendarConfig?.ItemStyleType ?? CalendarConfig.CalendarItemStyle.Classic) != CalendarConfig.CalendarItemStyle.Modern && IsTaskOrNote)
		{
			return CalendarUtils.GetTaskColorMode() == CalTaskColorMode.Priority;
		}
		return false;
	}

	private string GetClassicPriorityNoColor()
	{
		return ThemeUtil.GetNoAlphaColorString(ThemeUtil.GetColorValue(_inDark ? "color_#666666" : "color_#A1A1A1").ToString());
	}

	private int GetLeftRectOpacity(CalendarConfig.CalendarItemStyle calendarTaskbarStyle)
	{
		int result = 100;
		if (calendarTaskbarStyle == CalendarConfig.CalendarItemStyle.Modern)
		{
			result = ((Status != 0 || IsPomo) ? 30 : 100);
		}
		return result;
	}

	private (string color, double opacity) GetBorderColorAndOpacity(CalendarConfig.CalendarItemStyle calendarTaskbarStyle, bool selected, string baseColor)
	{
		string text = null;
		text = ((calendarTaskbarStyle != CalendarConfig.CalendarItemStyle.Modern) ? ((BatchSelected || IsPomo) ? baseColor : "#FFFFFF") : (BatchSelected ? baseColor : "transparent"));
		int num = 20;
		if (BatchSelected)
		{
			num = (_dragging ? 60 : 100);
		}
		else
		{
			int num2 = (selected ? 40 : 30);
			double num3 = (IsPomo ? 1 : 2);
			num = (int)((double)num2 * num3);
		}
		return (color: text, opacity: num);
	}

	private (string color, int opacity) GetTitleColorAndOpacity(CalendarConfig.CalendarItemStyle calendarTaskbarStyle, bool selected, double grayscale, string baseColor)
	{
		int item = 100;
		if (calendarTaskbarStyle != CalendarConfig.CalendarItemStyle.Modern)
		{
			item = ((!BatchSelected) ? ((!IsCountdown) ? ((!IsPomo) ? ((Status == 0 && selected) ? 100 : ((Status == 0 || selected) ? 80 : (_inDark ? 20 : 40))) : (selected ? 60 : 40)) : (selected ? 100 : ((StartDate.HasValue && StartDate.Value < DateTime.Today.Date) ? 80 : 100))) : ((!Dragging) ? ((Status == 0) ? 100 : 60) : (_inDark ? 20 : 40)));
		}
		else if ((Status != 0 || IsPomo) && !selected)
		{
			item = (_inDark ? 20 : 40);
		}
		string text = "#191919";
		if (calendarTaskbarStyle == CalendarConfig.CalendarItemStyle.Modern)
		{
			if (_inDark)
			{
				text = "#FFFFFF";
			}
			if (!_inDark)
			{
				if ((Status == 0 && !IsPomo) || selected)
				{
					Color color = ColorHelper.BlendWithAlpha(ThemeUtil.GetColorInDict(baseColor, 30).Color, (Color)ColorConverter.ConvertFromString(text));
					text = ColorHelper.ColorConverter.ConvertToString(color);
				}
			}
			else
			{
				Color color2 = ColorHelper.BlendWithAlpha(ThemeUtil.GetColorInDict(baseColor, 10).Color, (Color)ColorConverter.ConvertFromString(text));
				text = ColorHelper.ColorConverter.ConvertToString(color2);
			}
		}
		else if ((!selected && _inDark) || (selected && grayscale <= 0.65))
		{
			text = "#FFFFFF";
		}
		if (calendarTaskbarStyle != CalendarConfig.CalendarItemStyle.Modern && IsCountdown)
		{
			text = "#FFFFFF";
		}
		return (color: text, opacity: item);
	}

	private int GetIconOpacity(CalendarConfig.CalendarItemStyle calendarTaskbarStyle, bool selected)
	{
		int num = 100;
		if (calendarTaskbarStyle != CalendarConfig.CalendarItemStyle.Modern && IsCountdown)
		{
			num = 100;
		}
		else
		{
			num = 40;
			if (IsTaskOrNote)
			{
				if (Status != 0 || IsPomo)
				{
					num = (selected ? 20 : 10);
				}
			}
			else if (Status != 0 || IsPomo)
			{
				num = (selected ? 50 : 24);
			}
		}
		return num;
	}

	private int GetTimeTextOpacity(CalendarConfig.CalendarItemStyle calendarTaskbarStyle, bool selected, int titleOpacity)
	{
		int num = 100;
		if (Status == 0 && !IsPomo)
		{
			return 60;
		}
		return selected ? 100 : (_inDark ? 15 : 20);
	}

	private void SetFocusViewItemColor()
	{
		string text = (_inDark ? "#FFFFFF" : "#191919");
		TitleColor = ThemeUtil.GetColorInDict(text, 40);
		BackColor = ThemeUtil.GetColorInDict(text, Selected ? 10 : 5);
		BackBorderColor = ThemeUtil.GetColorInDict(IsPomo ? text : null, 10);
		CalendarConfig.CalendarItemStyle calendarItemStyle = LocalSettings.Settings?.UserPreference?.CalendarConfig?.ItemStyleType ?? CalendarConfig.CalendarItemStyle.Classic;
		BackBorderThickness = ((IsPomo && calendarItemStyle != CalendarConfig.CalendarItemStyle.Modern) ? new Thickness(1.0) : new Thickness(0.0));
	}

	public void SetTimeText(bool inArrange, bool isSpan)
	{
		_inArrange = inArrange;
		SetTimeText(isSpan);
	}

	private void SetTimeText(bool isSpan)
	{
		if (!ShowTimeText || !string.IsNullOrEmpty(HabitId))
		{
			return;
		}
		if (!DisplayStartDate.HasValue)
		{
			TimeText = string.Empty;
			OnPropertyChanged("TimeText");
			return;
		}
		if (_inArrange)
		{
			DateTime? dateTime = DisplayDueDate;
			if (!dateTime.HasValue)
			{
				dateTime = DisplayStartDate;
			}
			else if (IsAllDay ?? true)
			{
				dateTime = dateTime.Value.AddSeconds(-1.0);
			}
			TimeText = ticktick_WPF.Util.DateUtils.FormatDateCheckYear(dateTime.Value);
		}
		else if (!(IsAllDay ?? true))
		{
			string text = ticktick_WPF.Util.DateUtils.FormatHourMinuteText(DisplayStartDate.Value);
			if (isSpan && DisplayDueDate.HasValue && DisplayDueDate.Value != DisplayStartDate.Value)
			{
				text = text + " - " + ticktick_WPF.Util.DateUtils.FormatHourMinuteText(DisplayDueDate.Value);
			}
			TimeText = text;
		}
		else
		{
			TimeText = string.Empty;
		}
		OnPropertyChanged("TimeText");
		OnPropertyChanged("TimeTextVisibility");
	}

	public List<CalendarDisplayViewModel> GetChildren(bool checkOpen = true)
	{
		List<CalendarDisplayViewModel> result = new List<CalendarDisplayViewModel>();
		GetChild(Children, Level + 1, IsOpened);
		return result;
		void GetChild(List<CalendarDisplayViewModel> children, int level, bool isOpen)
		{
			foreach (CalendarDisplayViewModel child in children)
			{
				if (!checkOpen || isOpen)
				{
					result.Add(child);
				}
				child.Level = level;
				if (child.Children.Count > 0)
				{
					GetChild(child.Children, level + 1, isOpen && child.IsOpened);
				}
			}
		}
	}

	public void SetTaskCell(TaskCell taskCell)
	{
		_taskCell = taskCell;
	}

	public void SetTaskBar(TaskBar taskBar)
	{
		_taskBar = taskBar;
	}

	~CalendarDisplayViewModel()
	{
		_taskCell = null;
		_taskBar = null;
	}

	public async Task SaveDate(DateTime? date, Window window, bool toAllDay = false)
	{
		if (date.HasValue)
		{
			DateTime dropDate = date.Value;
			if (Utils.IfCtrlPressed())
			{
				await TryCopy(window);
			}
			await HandleDropDate(dropDate, toAllDay);
		}
	}

	private async Task TryCopy(Window owner)
	{
		int step = 0;
		try
		{
			if (SourceViewModel != null)
			{
				step = 1;
				if (!string.IsNullOrEmpty(ItemId))
				{
					step = 2;
					string id = (await TaskDetailItemDao.CopyItem(ItemId))?.id;
					TaskBaseViewModel taskBaseViewModel = SourceViewModel.Copy();
					taskBaseViewModel.Id = id;
					SetSourceModel(taskBaseViewModel, setEvent: true);
					step = 3;
				}
				else if (IsTaskOrNote)
				{
					step = 4;
					string id2 = (await TaskService.CopyTask(GetTaskId(), owner, notify: true, delayNotify: true)).id;
					TaskBaseViewModel taskBaseViewModel2 = SourceViewModel.Copy();
					taskBaseViewModel2.Id = id2;
					SetSourceModel(taskBaseViewModel2, setEvent: true);
					step = 5;
				}
			}
		}
		catch (Exception)
		{
			UtilLog.Info("CalendarDisplayViewModel.TryCopy exception " + step);
		}
	}

	private async Task HandleDropDate(DateTime date, bool toAllDay)
	{
		if (IsCalendarEvent)
		{
			await NotifyEventDrop(date, toAllDay);
		}
		else if (!string.IsNullOrEmpty(ItemId))
		{
			await NotifyCheckItemDrop(date, toAllDay);
		}
		else
		{
			await NotifyTaskDrop(date, toAllDay);
		}
	}

	public async Task NotifyTaskDrop(DateTime date, bool toAllDay, bool batchHandle = false)
	{
		if (!StartDate.HasValue)
		{
			SourceViewModel.IsAllDay = true;
		}
		else if (StartDate.Value == date && IsAllDay == toAllDay)
		{
			return;
		}
		DateTime? dateTime = DueDate;
		DateTime? dateTime2 = StartDate;
		bool previewAllDay = SourceViewModel.IsAllDay ?? true;
		if (toAllDay && !(SourceViewModel.IsAllDay ?? true))
		{
			if (dateTime.HasValue)
			{
				TimeSpan? timeSpan = dateTime - StartDate;
				if (timeSpan.HasValue && timeSpan.GetValueOrDefault().TotalDays < 24.0)
				{
					dateTime = null;
				}
				else
				{
					toAllDay = false;
				}
			}
			if (toAllDay)
			{
				dateTime2 = dateTime2?.Date;
			}
		}
		TimeData revisedDate = TaskService.GetChangedData(dateTime2, dateTime, toAllDay || !StartDate.HasValue || (IsAllDay ?? true), date, RepeatFlag, RepeatFrom);
		if (!string.IsNullOrEmpty(RepeatFlag))
		{
			TaskModel task = await TaskDao.GetThinTaskById(GetTaskId());
			if (task != null && StartDate.HasValue && task.startDate.HasValue)
			{
				List<TaskReminderModel> reminders = await TaskReminderDao.GetRemindersByTaskId(task.id);
				TimeData timeData = new TimeData
				{
					StartDate = revisedDate.StartDate,
					DueDate = revisedDate.DueDate,
					IsAllDay = revisedDate.IsAllDay,
					Reminders = reminders,
					RepeatFrom = RepeatFrom,
					RepeatFlag = RepeatFlag,
					ExDates = ((task.exDate != null) ? task.exDate.ToList() : new List<string>())
				};
				await ModifyRepeatHandler.TryUpdateDueDateOnlyDate(TaskId, DisplayStartDate, DisplayDueDate, timeData, date, 1, 0, batchHandle);
			}
		}
		else
		{
			await TaskService.SetDate(TaskId, revisedDate, handleReminder: false);
			if (!StartDate.HasValue || previewAllDay != (revisedDate.IsAllDay ?? true))
			{
				await TaskService.SaveTaskReminders(new TaskModel
				{
					id = TaskId,
					reminders = ((revisedDate.IsAllDay ?? true) ? TimeData.GetDefaultAllDayReminders().ToArray() : TimeData.GetDefaultTimeReminders().ToArray())
				});
			}
		}
	}

	private async Task NotifyEventDrop(DateTime date, bool toAllDay)
	{
		TimeData changedData = TaskService.GetChangedData(StartDate, DueDate, toAllDay || (IsAllDay ?? true), date, RepeatFlag, RepeatFrom);
		await CalendarService.SaveEventTime(EventId, new TimeData
		{
			StartDate = changedData.StartDate,
			DueDate = changedData.DueDate,
			IsAllDay = changedData.IsAllDay,
			RepeatFlag = RepeatFlag
		});
	}

	private async Task NotifyCheckItemDrop(DateTime date, bool toAllDay)
	{
		await TaskService.SetSubtaskDate(GetTaskId(), ItemId, date, toAllDay);
	}

	public TaskCellViewModel ToDragTaskCellViewModel()
	{
		TaskCellViewModel taskCellViewModel = new TaskCellViewModel();
		taskCellViewModel.Color = Color;
		taskCellViewModel.RepeatDiff = RepeatDiff;
		taskCellViewModel.BaseOnStart = true;
		taskCellViewModel.Course = Course;
		taskCellViewModel.TimeText = TimeText;
		taskCellViewModel.DragVOffset = DragVOffset;
		taskCellViewModel.SetSourceModel(SourceViewModel.Copy());
		taskCellViewModel.SetIcon();
		taskCellViewModel._batchSelected = BatchSelected;
		return taskCellViewModel;
	}

	public async Task SetDropTimeSpan(DateTime? start, DateTime? end, Window window = null, bool? isAllDay = false)
	{
		if (!isAllDay.HasValue && start == DisplayStartDate && end == DisplayDueDate)
		{
			return;
		}
		if (Utils.IfCtrlPressed())
		{
			await TryCopy(window);
		}
		if (IsCalendarEvent)
		{
			await CalendarService.SaveEventTime(EventId, new TimeData
			{
				StartDate = start,
				DueDate = end,
				IsAllDay = (isAllDay ?? IsAllDay),
				RepeatFlag = RepeatFlag
			});
		}
		if (IsTaskOrNote)
		{
			bool needSetReminder = !StartDate.HasValue || (isAllDay.HasValue && (IsAllDay ?? true));
			await TaskService.SetDateSpan(TaskId, start, end, needSetReminder, isAllDay);
			if (needSetReminder)
			{
				await TaskService.SaveTaskReminders(new TaskModel
				{
					id = TaskId,
					reminders = TimeData.GetDefaultTimeReminders().ToArray()
				});
			}
		}
		if (IsCheckItem)
		{
			TimeDataModel newDate = new TimeDataModel
			{
				TaskId = GetTaskId(),
				ItemId = ItemId,
				IsAllDay = false,
				StartDate = start,
				DueDate = null
			};
			await TaskService.SetCheckItemDate(GetTaskId(), ItemId, newDate);
		}
	}

	public bool IsEqualTask(CalendarDisplayViewModel model)
	{
		if (model == null)
		{
			return false;
		}
		if (model.TaskId == TaskId)
		{
			DateTime? displayStartDate = model.DisplayStartDate;
			DateTime? displayStartDate2 = DisplayStartDate;
			if (displayStartDate.HasValue != displayStartDate2.HasValue)
			{
				return false;
			}
			if (!displayStartDate.HasValue)
			{
				return true;
			}
			return displayStartDate.GetValueOrDefault() == displayStartDate2.GetValueOrDefault();
		}
		return false;
	}

	public bool IsConfigShowIcon()
	{
		DisplayType? displayType;
		switch (Type)
		{
		case DisplayType.Task:
		case DisplayType.CheckItem:
		case DisplayType.Agenda:
			displayType = DisplayType.Task;
			break;
		case DisplayType.Pomo:
		case DisplayType.Timer:
			displayType = DisplayType.Pomo;
			break;
		default:
			displayType = Type;
			break;
		}
		return LocalSettings.Settings.IsShowCalendarIcon(displayType.Value);
	}

	public async void OnCheckBoxClick(IToastShowWindow toastWindow)
	{
		if (SourceViewModel == null)
		{
			return;
		}
		if (!SourceViewModel.Editable)
		{
			string permission = SourceViewModel.Permission;
			if (!(permission == "comment"))
			{
				if (permission == "read")
				{
					toastWindow?.TryToastString(null, string.Format(Utils.GetString("NoPermissionToEdit"), Utils.GetString("ReadOnly")));
				}
			}
			else
			{
				toastWindow?.TryToastString(null, string.Format(Utils.GetString("NoPermissionToEdit"), Utils.GetString("CanComment")));
			}
			return;
		}
		switch (Type)
		{
		case DisplayType.Derivative:
			if (await ModifyRepeatHandler.CompleteOrSkipRecurrence(TaskId, DisplayStartDate, toastWindow))
			{
				int num3 = 2;
				SourceViewModel.Status = num3;
				await TaskService.SetTaskStatus(TaskId, num3, needUndo: true, toastWindow);
			}
			break;
		case DisplayType.Task:
		case DisplayType.Agenda:
		{
			TaskModel taskModel2 = await TaskDao.GetThinTaskById(SourceViewModel.GetTaskId());
			if (taskModel2 == null || string.IsNullOrEmpty(taskModel2.attendId) || AgendaHelper.CanAccessAgenda(taskModel2))
			{
				int num = ((Status == 0) ? 2 : 0);
				CalendarDisplayViewModel calendarDisplayViewModel = this;
				int status = (SourceViewModel.Status = num);
				calendarDisplayViewModel.Status = status;
				SetColor();
				SetIcon(notify: true);
				await TaskService.SetTaskStatus(TaskId, num, needUndo: true, toastWindow, inDetail: false, num == 2);
			}
			break;
		}
		case DisplayType.CheckItem:
		{
			TaskModel taskModel = await TaskDao.GetThinTaskById(SourceViewModel.GetTaskId());
			if (taskModel != null && !string.IsNullOrEmpty(taskModel.attendId) && !AgendaHelper.CanAccessAgenda(taskModel))
			{
				toastWindow.TryToastString(null, Utils.GetString("OnlyOwnerCanCompleteSubtask"));
			}
			if (Status != 1)
			{
				Utils.PlayCompletionSound();
			}
			await TaskDetailItemService.CompleteCheckItem(ItemId, handleTask: true, completeOnly: false, needUndo: true, toastWindow);
			SyncManager.TryDelaySync();
			break;
		}
		}
	}
}
