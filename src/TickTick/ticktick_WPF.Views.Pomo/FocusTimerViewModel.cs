using System.Threading.Tasks;
using TickTickModels;
using ticktick_WPF.Notifier;
using ticktick_WPF.Service;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Pomo;

public class FocusTimerViewModel : SortableListItemViewModel
{
	private bool _selected;

	private string _title;

	private string _durationText;

	private int _timerStatus;

	private string _icon;

	private string _color;

	private bool _focusing;

	public string ObjId { get; set; }

	public string ObjType { get; set; }

	public string Icon
	{
		get
		{
			return _icon;
		}
		set
		{
			_icon = value;
			OnPropertyChanged("Icon");
		}
	}

	public string Color
	{
		get
		{
			return _color;
		}
		set
		{
			_color = value;
			OnPropertyChanged("Color");
		}
	}

	public string DurationText
	{
		get
		{
			return _durationText;
		}
		set
		{
			if (!(_durationText == value))
			{
				_durationText = value;
				OnPropertyChanged("DurationText");
			}
		}
	}

	public string Title
	{
		get
		{
			return _title;
		}
		set
		{
			_title = value;
			OnPropertyChanged("Title");
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
			if (_selected != value)
			{
				_selected = value;
				OnPropertyChanged("Selected");
			}
		}
	}

	public bool Focusing
	{
		get
		{
			return _focusing;
		}
		set
		{
			if (_focusing != value)
			{
				_focusing = value;
				OnPropertyChanged("Focusing");
			}
		}
	}

	public bool Archive { get; set; }

	public FocusTimerViewModel(TimerModel timer)
	{
		base.Id = timer.Id;
		ObjId = timer.ObjId;
		ObjType = timer.ObjType;
		_title = timer.Name;
		_icon = timer.Icon;
		_color = timer.Color;
		base.SortOrder = timer.SortOrder;
		Archive = timer.Status == 1;
		SetFocusing();
		CheckTimerName();
	}

	public async Task CheckTimerName()
	{
		if (!string.IsNullOrEmpty(ObjId))
		{
			string text = await TimerService.GetObjTitle(ObjId, ObjType);
			if (text != null && text != Title)
			{
				Title = text;
				TimerService.UpdateName(base.Id, text);
			}
		}
	}

	public override void SaveSortOrder()
	{
		TimerService.SaveSortOrder(base.Id, base.SortOrder);
		PomoNotifier.NotifyTimerChanged();
	}

	public void SetFocusing()
	{
		Focusing = !Archive && TickFocusManager.Working && TickFocusManager.Config.FocusVModel.TimerId == base.Id;
	}
}
