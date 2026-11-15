using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using Newtonsoft.Json;
using ticktick_WPF.Entity.Enums;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Entity.Binding;

public class AIRecommendItem : INotifyPropertyChanged
{
	private Geometry _icon;

	private SolidColorBrush _iconFill;

	private bool _isSelected;

	private bool _isLastDefault;

	private bool _isCompleted;

	private string _name;

	private string _description;

	public AIRecommendTaskInfo TaskInfo { get; set; }

	public string TaskId => TaskInfo?.Task.Id;

	[JsonIgnore]
	public Action<bool> OnRemoveAnimate { get; set; }

	[JsonIgnore]
	public Action OnRemoveCompleted { get; set; }

	public Geometry Icon
	{
		get
		{
			return _icon;
		}
		set
		{
			_icon = value;
			NotifyPropertyChanged("Icon");
		}
	}

	public SolidColorBrush IconFill
	{
		get
		{
			return _iconFill;
		}
		set
		{
			_iconFill = value;
			NotifyPropertyChanged("IconFill");
		}
	}

	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			_isSelected = value;
			NotifyPropertyChanged("IsSelected");
		}
	}

	public bool IsLastDefault
	{
		get
		{
			return _isLastDefault;
		}
		set
		{
			if (_isLastDefault != value)
			{
				_isLastDefault = value;
				NotifyPropertyChanged("IsLastDefault");
			}
		}
	}

	public bool IsCompleted
	{
		get
		{
			return _isCompleted;
		}
		set
		{
			_isCompleted = value;
			NotifyPropertyChanged("IsCompleted");
		}
	}

	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			_name = value;
			NotifyPropertyChanged("Name");
		}
	}

	public string Description
	{
		get
		{
			return _description;
		}
		set
		{
			_description = value;
			NotifyPropertyChanged("Description");
		}
	}

	public RecommendTotalGroupType TotalType { get; set; }

	public AIRecommendGroupType GroupType { get; set; }

	public event PropertyChangedEventHandler PropertyChanged;

	public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public AIRecommendItem()
	{
	}

	public AIRecommendItem(AIRecommendTaskInfo taskInfo)
	{
		TaskInfo = taskInfo;
	}

	public void UpdateIcon(int status)
	{
		Icon = ThemeUtil.GetTaskIconGeometry(DisplayType.Task, status, TaskInfo.Task.Kind, 0);
		if (status == 0)
		{
			switch (TaskInfo.Task.Priority)
			{
			case 1:
				IconFill = ThemeUtil.GetColor("PriorityLowColor");
				break;
			case 3:
				IconFill = ThemeUtil.GetColor("PriorityMiddleColor");
				break;
			case 5:
				IconFill = ThemeUtil.GetColor("PriorityHighColor");
				break;
			case 0:
				IconFill = ThemeUtil.GetColor("BaseColorOpacity40");
				break;
			case 2:
			case 4:
				break;
			}
		}
		else
		{
			IconFill = ThemeUtil.GetColor("BaseColorOpacity20");
		}
	}
}
