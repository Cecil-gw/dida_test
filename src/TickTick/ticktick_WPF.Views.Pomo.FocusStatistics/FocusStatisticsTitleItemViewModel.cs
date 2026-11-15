using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace ticktick_WPF.Views.Pomo.FocusStatistics;

public class FocusStatisticsTitleItemViewModel : FocusStatisticsPanelItemViewModel, INotifyPropertyChanged
{
	private bool _showMoreButton = true;

	private Visibility _moreButtonVisibility;

	public string Title { get; set; }

	public bool ShowButton { get; set; }

	public bool ShowMoreButton
	{
		get
		{
			return _showMoreButton;
		}
		set
		{
			_showMoreButton = value;
			MoreButtonVisibility = ((!value) ? Visibility.Collapsed : Visibility.Visible);
		}
	}

	public Visibility MoreButtonVisibility
	{
		get
		{
			return _moreButtonVisibility;
		}
		set
		{
			_moreButtonVisibility = value;
			NotifyPropertyChanged("MoreButtonVisibility");
		}
	}

	public new event PropertyChangedEventHandler PropertyChanged;

	public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public FocusStatisticsTitleItemViewModel(string title, bool showButton)
	{
		Title = title;
		ShowButton = showButton;
		base.IsTitle = true;
	}
}
