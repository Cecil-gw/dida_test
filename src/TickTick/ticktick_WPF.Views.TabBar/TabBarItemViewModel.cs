using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Newtonsoft.Json;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.AI;
using ticktick_WPF.Views.Calendar;

namespace ticktick_WPF.Views.TabBar;

public class TabBarItemViewModel : BaseViewModel
{
	public const string Task = "Task";

	public const string Calendar = "Calendar";

	public const string Habit = "Habit";

	public const string Pomo = "Pomo";

	public const string Matrix = "Matrix";

	public const string Search = "Search";

	public const string CountDown = "CountDown";

	public const string AI = "AI";

	private string _toolTip;

	private bool _selected;

	private bool _show = true;

	private bool _dragging;

	private long _sortOrder;

	private string _guideText;

	public string Module;

	public Geometry Icon { get; set; }

	public string GuideText
	{
		get
		{
			return _guideText;
		}
		set
		{
			_guideText = value;
			OnPropertyChanged("GuideText");
		}
	}

	public string ToolTip
	{
		get
		{
			return _toolTip;
		}
		set
		{
			_toolTip = value;
			OnPropertyChanged("Tooltip");
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
			OnPropertyChanged("Selected");
		}
	}

	public bool Show
	{
		get
		{
			return _show;
		}
		set
		{
			if (_show != value)
			{
				_show = value;
				OnPropertyChanged("Show");
			}
		}
	}

	public bool Dragging
	{
		get
		{
			return _dragging;
		}
		set
		{
			_dragging = value;
			OnPropertyChanged("Dragging");
		}
	}

	public long SortOrder
	{
		get
		{
			return _sortOrder;
		}
		set
		{
			if (_sortOrder != value)
			{
				_sortOrder = value;
				OnPropertyChanged("SortOrder");
			}
		}
	}

	public string Name { get; set; }

	public TabBarItemViewModel()
	{
	}

	public TabBarItemViewModel(string module, string selected)
	{
		Module = module;
		switch (module)
		{
		case "Task":
			Icon = Utils.GetGeometry("Ic.Tabbar.List");
			Name = Utils.GetString("Task");
			break;
		case "Calendar":
			Icon = Utils.GetGeometry("Ic.Tabbar.Calendar");
			Name = Utils.GetString("Calendar");
			break;
		case "Matrix":
			Icon = Utils.GetGeometry("Ic.Tabbar.Martix");
			Name = Utils.GetString("Matrix");
			break;
		case "Habit":
			Icon = Utils.GetGeometry("Ic.Tabbar.Habit");
			Name = Utils.GetString("HabitView");
			break;
		case "Search":
			Icon = Utils.GetGeometry("Ic.Tabbar.Search");
			Name = Utils.GetString("QuickSearch");
			break;
		case "Pomo":
			Icon = Utils.GetGeometry("Ic.Tabbar.Focus");
			Name = Utils.GetString("PomoFocus");
			break;
		case "CountDown":
			Icon = Utils.GetGeometry("Ic.Tabbar.Countdown");
			Name = Utils.GetString("tab_bar_countdown");
			break;
		case "AI":
			Icon = Utils.GetGeometry("Ic.Tabbar.AI");
			Name = Utils.GetString("AI");
			break;
		}
		Selected = module == selected;
	}

	public bool CanHide()
	{
		if (!(Module == "Matrix") && !(Module == "Habit") && !(Module == "Pomo") && !(Module == "Calendar") && !(Module == "CountDown"))
		{
			return Module == "AI";
		}
		return true;
	}

	public bool CanOpenWindow()
	{
		return Module != "Search";
	}

	public bool IsWindowOpened()
	{
		return Module switch
		{
			"AI" => AIWindow.IsShowing, 
			"Calendar" => CalendarWindow.IsShowing, 
			"Matrix" => MatrixWindow.IsShowing, 
			"Habit" => HabitWindow.IsShowing, 
			"Task" => TaskWindow.IsShowing, 
			"Pomo" => FocusWindow.IsShowing, 
			_ => false, 
		};
	}

	public bool IsParallelViewShow()
	{
		if ((Module == "Task" || Module == "Calendar") && LocalSettings.Settings.ExtraSettings.ListCalendarSettings != null)
		{
			try
			{
				ListCalendarSettings listCalendarSettings = JsonConvert.DeserializeObject<ListCalendarSettings>(LocalSettings.Settings.ExtraSettings.ListCalendarSettings);
				if ((Module == "Calendar" && listCalendarSettings.ShowLInC) || (Module == "Task" && listCalendarSettings.ShowCInL))
				{
					return true;
				}
			}
			catch
			{
				return false;
			}
		}
		return false;
	}

	public async void OpenOrCloseWindow(Window window)
	{
		await System.Threading.Tasks.Task.Delay(100);
		string module = Module;
		if (module == null)
		{
			return;
		}
		switch (module.Length)
		{
		case 4:
			switch (module[0])
			{
			case 'P':
				if (module == "Pomo")
				{
					FocusWindow.OpenOrCloseWindow(window);
				}
				break;
			case 'T':
				if (module == "Task")
				{
					TaskWindow.OpenOrCloseWindow(window);
				}
				break;
			}
			break;
		case 2:
			if (module == "AI")
			{
				AIWindow.OpenOrCloseWindow(window);
			}
			break;
		case 8:
			if (module == "Calendar")
			{
				CalendarWindow.OpenOrCloseWindow(window);
			}
			break;
		case 6:
			if (module == "Matrix")
			{
				MatrixWindow.OpenOrCloseWindow(window);
			}
			break;
		case 5:
			if (module == "Habit")
			{
				HabitWindow.OpenOrCloseWindow(window);
			}
			break;
		case 9:
			if (module == "CountDown")
			{
				CountdownWindow.OpenOrCloseWindow(window);
			}
			break;
		case 3:
		case 7:
			break;
		}
	}
}
