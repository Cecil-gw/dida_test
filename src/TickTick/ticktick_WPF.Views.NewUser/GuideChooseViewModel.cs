using System.Windows;
using System.Windows.Media;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.NewUser;

public class GuideChooseViewModel : BaseViewModel
{
	private bool _selected;

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

	public string Name { get; set; }

	public string Emoji { get; set; }

	public Geometry Icon { get; set; }

	public Visibility ShowIcon { get; set; } = Visibility.Collapsed;

	public Visibility ShowEmoji { get; set; } = Visibility.Collapsed;

	public GuideChooseViewModel(string name)
	{
		Name = name;
	}

	public GuideChooseViewModel(string name, Geometry icon)
	{
		Name = name;
		Icon = icon;
		ShowIcon = Visibility.Visible;
	}

	public GuideChooseViewModel(string name, string emoji)
	{
		Name = name;
		Emoji = emoji;
		ShowEmoji = Visibility.Visible;
	}
}
