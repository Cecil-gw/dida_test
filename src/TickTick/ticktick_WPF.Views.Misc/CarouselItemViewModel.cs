using System.Collections.Generic;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Misc;

public class CarouselItemViewModel : BaseViewModel
{
	private string _title;

	private string _desc;

	private string _subTitle;

	private bool _selected;

	public string ImageUrl = "pack://application:,,,/Assets/LoginImage/LoginTask_{0}.png";

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

	public string Desc
	{
		get
		{
			return _desc;
		}
		set
		{
			_desc = value;
			OnPropertyChanged("Desc");
		}
	}

	public string SubTitle
	{
		get
		{
			return _subTitle;
		}
		set
		{
			_subTitle = value;
			OnPropertyChanged("SubTitle");
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

	public string Value { get; set; }

	public static List<CarouselItemViewModel> GetLoginCarouselModels()
	{
		return new List<CarouselItemViewModel>
		{
			new CarouselItemViewModel
			{
				_title = Utils.GetString("TaskLoginMessage"),
				ImageUrl = "pack://application:,,,/Assets/LoginImage/LoginTask_{0}.png"
			},
			new CarouselItemViewModel
			{
				_title = Utils.GetString("CalLoginMessage"),
				ImageUrl = "pack://application:,,,/Assets/LoginImage/LoginDate_{0}.png"
			},
			new CarouselItemViewModel
			{
				_title = Utils.GetString("MatrixLoginMessage"),
				ImageUrl = "pack://application:,,,/Assets/LoginImage/LoginHabit_{0}.png"
			},
			new CarouselItemViewModel
			{
				_title = Utils.GetString("PomoLoginMessage"),
				ImageUrl = "pack://application:,,,/Assets/LoginImage/LoginPomo_{0}.png"
			},
			new CarouselItemViewModel
			{
				_title = Utils.GetString("HabitLoginMessage"),
				ImageUrl = "pack://application:,,,/Assets/LoginImage/LoginHabit_{0}.png"
			}
		};
	}
}
