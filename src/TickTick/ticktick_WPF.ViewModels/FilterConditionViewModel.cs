using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using ticktick_WPF.Util;

namespace ticktick_WPF.ViewModels;

public class FilterConditionViewModel : BaseViewModel
{
	private bool _canSelectLogic;

	private bool _isAllSelected = true;

	private LogicType _logic = LogicType.Or;

	private bool _showAll;

	private List<LogicType> _supportedLogic;

	private CondType _type;

	private Geometry _allData = Utils.GetGeometry("Ic.ProjectList.All");

	private int _allPathWidth = 18;

	public CondType Type
	{
		get
		{
			return _type;
		}
		set
		{
			_type = value;
			OnPropertyChanged("Type");
		}
	}

	public LogicType Logic
	{
		get
		{
			return _logic;
		}
		set
		{
			_logic = value;
			OnPropertyChanged("Logic");
		}
	}

	public bool CanSelectLogic
	{
		get
		{
			return _canSelectLogic;
		}
		set
		{
			_canSelectLogic = value;
			OnPropertyChanged("CanSelectLogic");
		}
	}

	public bool WithTagsSelected { get; set; }

	public bool WithoutTagsSelected { get; set; }

	public bool AllLogicEnabled { get; set; } = true;

	public bool ShowAll
	{
		get
		{
			return _showAll;
		}
		set
		{
			_showAll = value;
			OnPropertyChanged("ShowAll");
		}
	}

	public ObservableCollection<FilterListItemViewModel> ItemsSource { get; set; }

	public bool IsAllSelected
	{
		get
		{
			return _isAllSelected;
		}
		set
		{
			_isAllSelected = value;
			OnPropertyChanged("IsAllSelected");
		}
	}

	public List<LogicType> SupportedLogic
	{
		get
		{
			return _supportedLogic;
		}
		set
		{
			_supportedLogic = value;
			OnPropertyChanged("SupportedLogic");
		}
	}

	public Geometry AllData
	{
		get
		{
			return _allData;
		}
		set
		{
			_allData = value;
			OnPropertyChanged("AllData");
		}
	}

	public Visibility AllIconVisible { get; set; }

	public int AllPathWidth
	{
		get
		{
			return _allPathWidth;
		}
		set
		{
			_allPathWidth = value;
			OnPropertyChanged("AllPathWidth");
		}
	}

	public bool ShowLogic
	{
		get
		{
			if (_supportedLogic.Count > 0)
			{
				return !ShowAll;
			}
			return false;
		}
		set
		{
			OnPropertyChanged("ShowLogic");
		}
	}
}
