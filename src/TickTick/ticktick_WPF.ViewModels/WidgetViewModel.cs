using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;

namespace ticktick_WPF.ViewModels;

public class WidgetViewModel : INotifyPropertyChanged
{
	private readonly CalendarWidgetModel _singleData;

	private readonly WidgetSettingModel _data;

	private readonly CountdownWidgetModel _countdownItem;

	private readonly HabitWidgetModel _habitItem;

	private readonly HabitWeekWidgetModel _habitWeekItem;

	public readonly string Id;

	public string Identity;

	public string Type;

	private string _themeId;

	private float _opacity;

	public string DisplayOption;

	private bool _autoHide;

	private bool _hideComplete;

	private bool _showCountDown;

	private double _left;

	private double _top;

	private double _width;

	private double _height;

	private string _collapseTitle;

	private bool _isLocked;

	private UniqueWidgetMode mode;

	private string _countdownStyle;

	private string _countdownColor;

	public string SortType { get; set; }

	public string GroupType { get; set; }

	public string Order { get; set; }

	public string CountdownId { get; set; }

	public CountdownWidgetModel.DisplayDescType CountdownDisplayDescType { get; set; }

	public string CountdownLocalImagePath { get; set; }

	public BackgroundModel CountdownBackground { get; set; }

	public DateDisplayFormatType CountdownDateFormatType { get; set; }

	public bool IsShowUnit { get; set; }

	public string ActiveViewId { get; set; }

	public string HabitId { get; set; }

	public WidgetType WidgetType { get; }

	public bool IsSingleMode => _singleData != null;

	public float Opacity
	{
		get
		{
			return _opacity;
		}
		set
		{
			_opacity = value;
			NotifyPropertyChanged("Opacity");
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
			_left = value;
			NotifyPropertyChanged("Left");
		}
	}

	public double Top
	{
		get
		{
			return _top;
		}
		set
		{
			_top = value;
			NotifyPropertyChanged("Top");
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
			_width = value;
			NotifyPropertyChanged("Width");
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
			_height = value;
			NotifyPropertyChanged("Height");
		}
	}

	public bool AutoHide
	{
		get
		{
			return _autoHide;
		}
		set
		{
			_autoHide = value;
			NotifyPropertyChanged("AutoHide");
		}
	}

	public bool HideComplete
	{
		get
		{
			return _hideComplete;
		}
		set
		{
			_hideComplete = value;
			NotifyPropertyChanged("HideComplete");
			NotifyPropertyChanged("ShowComplete");
		}
	}

	public bool ShowComplete
	{
		get
		{
			return !_hideComplete;
		}
		set
		{
			_hideComplete = !value;
			NotifyPropertyChanged("ShowComplete");
			NotifyPropertyChanged("HideComplete");
		}
	}

	public bool ShowCountDown
	{
		get
		{
			return _showCountDown;
		}
		set
		{
			_showCountDown = value;
			NotifyPropertyChanged("ShowCountDown");
		}
	}

	public string CollapseTitle
	{
		get
		{
			return _collapseTitle;
		}
		set
		{
			_collapseTitle = value;
			NotifyPropertyChanged("CollapseTitle");
		}
	}

	public string ThemeId
	{
		get
		{
			return _themeId;
		}
		set
		{
			_themeId = value;
			NotifyPropertyChanged("ThemeId");
		}
	}

	public string ViewThemeId { get; set; }

	public bool IsLocked
	{
		get
		{
			return _isLocked;
		}
		set
		{
			_isLocked = value;
			NotifyPropertyChanged("IsLocked");
		}
	}

	public string CountdownStyle
	{
		get
		{
			return _countdownStyle;
		}
		set
		{
			_countdownStyle = value;
			NotifyPropertyChanged("CountdownStyle");
		}
	}

	public string[] CountdownStyleColors
	{
		get
		{
			if (!string.IsNullOrEmpty(CountdownStyleColor))
			{
				return CountdownStyleColor.Split(',');
			}
			return new string[0];
		}
		set
		{
			if (value != null && value.Length != 0)
			{
				CountdownStyleColor = string.Join(",", value);
			}
			else
			{
				CountdownStyleColor = string.Empty;
			}
		}
	}

	public string CountdownStyleColor { get; set; }

	public event PropertyChangedEventHandler PropertyChanged;

	public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public WidgetViewModel()
	{
	}

	public WidgetViewModel(WidgetSettingModel settingModel)
	{
		WidgetType = WidgetType.List;
		_data = settingModel;
		Id = settingModel.id;
		Type = settingModel.type;
		Identity = settingModel.identity;
		_themeId = settingModel.themeId;
		_opacity = settingModel.opacity;
		DisplayOption = settingModel.displayOption;
		_autoHide = settingModel.autoHide;
		SortType = settingModel.sortType;
		GroupType = settingModel.groupType;
		Order = settingModel.order;
		_hideComplete = settingModel.hideComplete;
		_showCountDown = settingModel.showCountDown ?? LocalSettings.Settings.ShowCountDown;
		_isLocked = settingModel.isLocked;
		_left = settingModel.left;
		_top = settingModel.top;
		_width = settingModel.width;
		_height = settingModel.height;
		if (string.IsNullOrEmpty(SortType))
		{
			SortType = "dueDate";
		}
		if (GroupType == null)
		{
			if (SortType == "title" || SortType == "createdTime" || SortType == "modifiedTime")
			{
				GroupType = "none";
				return;
			}
			GroupType = SortType;
			SortType = ((SortType == "sortOrder") ? "sortOrder" : "dueDate");
		}
	}

	public WidgetViewModel(CalendarWidgetModel settingModel)
	{
		switch (settingModel.mode)
		{
		case UniqueWidgetMode.Calendar:
			WidgetType = WidgetType.Calendar;
			break;
		case UniqueWidgetMode.Matrix:
			WidgetType = WidgetType.Matrix;
			break;
		case UniqueWidgetMode.CountdownList:
			WidgetType = WidgetType.CountdownList;
			break;
		case UniqueWidgetMode.HabitList:
			WidgetType = WidgetType.HabitList;
			break;
		case UniqueWidgetMode.AI:
			WidgetType = WidgetType.AI;
			break;
		}
		_singleData = settingModel;
		Id = settingModel.id;
		_themeId = settingModel.themeId;
		_opacity = settingModel.opacity;
		DisplayOption = settingModel.displayOption;
		_autoHide = settingModel.autoHide;
		_hideComplete = settingModel.hideComplete;
		_isLocked = settingModel.isLocked;
		mode = settingModel.mode;
		IsShowUnit = settingModel.isShowUnit;
		ActiveViewId = settingModel.activeViewId;
		if (WidgetType == WidgetType.Matrix && string.IsNullOrEmpty(ActiveViewId))
		{
			ActiveViewId = LocalSettings.Settings.MatrixActiveViewId ?? "";
		}
		_left = settingModel.left;
		_top = settingModel.top;
		_width = settingModel.width;
		_height = settingModel.height;
	}

	public WidgetViewModel(WidgetBaseModel model)
	{
		if (!(model is CountdownWidgetModel countdownWidgetModel))
		{
			if (!(model is HabitWidgetModel habitWidgetModel))
			{
				if (model is HabitWeekWidgetModel habitWeekWidgetModel)
				{
					WidgetType = WidgetType.HabitWeekItem;
					_habitWeekItem = habitWeekWidgetModel;
					HabitId = habitWeekWidgetModel.HabitId;
				}
			}
			else
			{
				WidgetType = WidgetType.HabitItem;
				_habitItem = habitWidgetModel;
				HabitId = habitWidgetModel.HabitId;
			}
		}
		else
		{
			WidgetType = WidgetType.CountdownItem;
			_countdownItem = countdownWidgetModel;
			CountdownId = countdownWidgetModel.CountdownId;
			CountdownStyle = countdownWidgetModel.Style;
			CountdownStyleColors = countdownWidgetModel.StyleColors;
			CountdownLocalImagePath = countdownWidgetModel.LocalImagePath;
			CountdownDisplayDescType = countdownWidgetModel.DescType;
			CountdownBackground = countdownWidgetModel.BackgroundObject;
			CountdownDateFormatType = countdownWidgetModel.DateDisplayFormatType;
		}
		Id = model.Id;
		ThemeId = model.ThemeId;
		Opacity = model.Opacity;
		DisplayOption = model.DisplayOption;
		_isLocked = model.IsLocked;
		_left = model.Left;
		_top = model.Top;
		_width = model.Width;
		_height = model.Height;
		AutoHide = model.AutoHide;
	}

	public void UpdateCountdownModel(CountdownModel countdown)
	{
		countdown.LocalImagePath = CountdownLocalImagePath;
		countdown.StyleColors = CountdownStyleColors;
		countdown.Style = CountdownStyle;
		countdown.WidgetDescType = CountdownDisplayDescType;
		countdown.BackgroundObject = CountdownBackground;
		countdown.DateDisplayFormatType = CountdownDateFormatType;
	}

	public async Task Save()
	{
		if (IsSingleMode)
		{
			await SaveSingleModel();
			return;
		}
		switch (WidgetType)
		{
		case WidgetType.List:
			SaveProjectList();
			break;
		case WidgetType.CountdownItem:
			SaveCountdownItem();
			break;
		case WidgetType.HabitItem:
			SaveHabitItem();
			break;
		case WidgetType.HabitWeekItem:
			SaveHabitWeekItem();
			break;
		}
	}

	private async Task SaveCountdownItem()
	{
		_countdownItem.CountdownId = CountdownId;
		_countdownItem.Style = CountdownStyle;
		_countdownItem.StyleColors = CountdownStyleColors;
		_countdownItem.LocalImagePath = CountdownLocalImagePath;
		_countdownItem.BackgroundObject = CountdownBackground;
		_countdownItem.DescType = CountdownDisplayDescType;
		_countdownItem.DateDisplayFormatType = CountdownDateFormatType;
		_countdownItem.DisplayOption = DisplayOption;
		_countdownItem.Left = Left;
		_countdownItem.Top = Top;
		_countdownItem.Width = Width;
		_countdownItem.Height = Height;
		_countdownItem.IsLocked = IsLocked;
		_countdownItem.ThemeId = ThemeId;
		_countdownItem.Opacity = Opacity;
		_countdownItem.AutoHide = AutoHide;
		await WidgetBaseDao<CountdownWidgetModel>.UpdateWidget(_countdownItem);
	}

	private async Task SaveHabitItem()
	{
		_habitItem.HabitId = HabitId;
		_habitItem.DisplayOption = DisplayOption;
		_habitItem.IsLocked = IsLocked;
		_habitItem.Opacity = Opacity;
		_habitItem.ThemeId = ThemeId;
		_habitItem.Left = Left;
		_habitItem.Top = Top;
		_habitItem.Width = Width;
		_habitItem.Height = Height;
		_habitItem.AutoHide = AutoHide;
		await WidgetBaseDao<HabitWidgetModel>.UpdateWidget(_habitItem);
	}

	private async Task SaveHabitWeekItem()
	{
		_habitWeekItem.HabitId = HabitId;
		_habitWeekItem.ThemeId = ThemeId;
		_habitWeekItem.Opacity = Opacity;
		_habitWeekItem.DisplayOption = DisplayOption;
		_habitWeekItem.Left = Left;
		_habitWeekItem.Top = Top;
		_habitWeekItem.Width = Width;
		_habitWeekItem.Height = Height;
		_habitWeekItem.IsLocked = IsLocked;
		_habitWeekItem.AutoHide = AutoHide;
		await WidgetBaseDao<HabitWeekWidgetModel>.UpdateWidget(_habitWeekItem);
	}

	private async Task SaveProjectList()
	{
		_data.opacity = _opacity;
		_data.left = _left;
		_data.top = _top;
		_data.width = _width;
		_data.height = _height;
		_data.identity = Identity;
		_data.type = Type;
		_data.autoHide = _autoHide;
		_data.hideComplete = _hideComplete;
		_data.showCountDown = _showCountDown;
		_data.displayOption = DisplayOption;
		_data.sortType = SortType;
		_data.groupType = GroupType ?? SortType;
		_data.order = Order;
		_data.themeId = _themeId;
		_data.isLocked = _isLocked;
		await ProjectWidgetDao.UpdateWidget(_data);
	}

	private async Task SaveSingleModel()
	{
		_singleData.opacity = _opacity;
		_singleData.left = _left;
		_singleData.top = _top;
		_singleData.width = _width;
		_singleData.height = _height;
		_singleData.autoHide = _autoHide;
		_singleData.hideComplete = _hideComplete;
		_singleData.displayOption = DisplayOption;
		_singleData.themeId = ThemeId;
		_singleData.mode = mode;
		_singleData.isLocked = _isLocked;
		_singleData.isShowUnit = IsShowUnit;
		_singleData.activeViewId = ActiveViewId;
		await SingleWidgetDao.SaveWidget(_singleData);
	}
}
