using System;
using System.Windows;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Calendar;

public class TaskCellViewModel : CalendarDisplayViewModel
{
	private bool _dragging;

	private double _height;

	private bool _hideIndicator = true;

	private double _horizontalOffset;

	private string _textColor;

	private double _verticalOffset;

	private double _width;

	public bool NewAdd;

	private TaskCell _taskCell;

	public bool ShowDragHandle
	{
		get
		{
			if ((base.Type == DisplayType.Task || base.Type == DisplayType.Derivative) && !NewAdd && base.Type != DisplayType.Note)
			{
				return base.CanDrag;
			}
			return false;
		}
	}

	public string Identity => base.SourceViewModel.Id + base.DisplayStartDate?.ToString("_ddHHmm");

	public double VerticalOffset
	{
		get
		{
			return _verticalOffset;
		}
		set
		{
			_verticalOffset = value;
			OnPropertyChanged("Margin");
		}
	}

	public Thickness Margin => new Thickness(2 + MathUtil.Ceiling(_horizontalOffset), MathUtil.Ceiling(_verticalOffset), 0.0, 0.0);

	public double HorizontalOffset
	{
		get
		{
			return _horizontalOffset;
		}
		set
		{
			_horizontalOffset = value;
			OnPropertyChanged("Margin");
		}
	}

	public double Width
	{
		get
		{
			return _width;
		}
		set
		{
			if (Math.Abs(_width - value) > 1.0)
			{
				_width = value;
				OnPropertyChanged("Width");
			}
		}
	}

	public double Height
	{
		get
		{
			return _height;
		}
		set
		{
			_height = Math.Max(value, ((base.IsAllDay ?? true) || !base.DueDate.HasValue) ? CalendarGeoHelper.MinPointHeight : CalendarGeoHelper.MinHeight);
			OnPropertyChanged("Height");
		}
	}

	public DateTime? BaseDate
	{
		get
		{
			if (!BaseOnStart)
			{
				return base.DisplayDueDate;
			}
			return base.DisplayStartDate;
		}
	}

	public bool BaseOnStart { get; set; }

	public bool BarMode { get; set; }

	public bool IsCourse => Course != null;

	public int ZIndex { get; set; }

	public DateTime? OriginalStart { get; set; }

	public static TaskCellViewModel Build(CalendarDisplayModel model, DateTime targetDate)
	{
		DateTime? displayStartDate = model.DisplayStartDate;
		string displayColor = CalendarDisplayViewModel.GetDisplayColor(model.SourceViewModel, model.CourseDisplayModel, model.IsVirtual);
		TaskCellViewModel taskCellViewModel = new TaskCellViewModel();
		taskCellViewModel.Color = displayColor;
		taskCellViewModel.RepeatDiff = model.repeatDiff;
		taskCellViewModel.BaseOnStart = !displayStartDate.HasValue || targetDate.Date == displayStartDate.Value.Date;
		taskCellViewModel.Course = model.CourseDisplayModel;
		taskCellViewModel.TimeText = model.CourseDisplayModel?.Room;
		taskCellViewModel.ToPoint = model.ToPoint;
		taskCellViewModel.SetSourceModel(model.SourceViewModel);
		taskCellViewModel.SetIcon();
		return taskCellViewModel;
	}

	public static TaskCellViewModel Copy(TaskCellViewModel model)
	{
		return new TaskCellViewModel
		{
			SourceViewModel = model.SourceViewModel.Copy(),
			Color = model.Color,
			Width = model.Width,
			Height = model.Height,
			VerticalOffset = model.VerticalOffset,
			HorizontalOffset = model.HorizontalOffset,
			Icon = model.Icon,
			BackColor = model.BackColor,
			LeftRectColor = model.LeftRectColor,
			TitleColor = model.TitleColor,
			TimeColor = model.TimeColor,
			IconColor = model.IconColor,
			BackBorderColor = model.BackBorderColor,
			BackBorderThickness = model.BackBorderThickness,
			BaseOnStart = model.BaseOnStart,
			DragMode = model.DragMode,
			BarMode = model.BarMode,
			RepeatDiff = model.RepeatDiff,
			BatchSelected = model.BatchSelected
		};
	}

	private static DisplayType ConvertDisplayType(int type)
	{
		return type switch
		{
			0 => DisplayType.Task, 
			1 => DisplayType.CheckItem, 
			2 => DisplayType.Derivative, 
			3 => DisplayType.Event, 
			5 => DisplayType.Note, 
			6 => DisplayType.Pomo, 
			_ => DisplayType.Task, 
		};
	}

	public void GetColor(bool dark)
	{
		base.Color = CalendarDisplayViewModel.GetDisplayColor(base.SourceViewModel, Course);
		SetTheme(dark);
	}

	public new TaskCellViewModel Clone()
	{
		return new TaskCellViewModel
		{
			SourceViewModel = base.SourceViewModel.Copy(),
			Icon = base.Icon,
			Color = base.Color,
			BackColor = base.BackColor,
			LeftRectColor = base.LeftRectColor,
			TitleColor = base.TitleColor,
			TimeColor = base.TimeColor,
			IconColor = base.IconColor,
			BackBorderColor = base.BackBorderColor,
			BackBorderThickness = base.BackBorderThickness,
			RepeatDiff = base.RepeatDiff,
			BaseOnStart = BaseOnStart,
			DragVOffset = base.DragVOffset,
			OriginalStart = OriginalStart,
			BatchSelected = base.BatchSelected
		};
	}
}
