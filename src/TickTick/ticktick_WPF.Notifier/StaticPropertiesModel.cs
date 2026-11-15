using System.Windows.Data;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Notifier;

public class StaticPropertiesModel : BaseViewModel
{
	public static StaticPropertiesModel Instance = new StaticPropertiesModel();

	public static Binding HitTestBinding = new Binding("ItemHitTestVisible")
	{
		Source = Instance
	};

	private bool _windowAllowDrop;

	private bool _altKeyDown;

	public bool ItemHitTestVisible => !WindowAllowDrop;

	public bool WindowAllowDrop
	{
		get
		{
			return false;
		}
		set
		{
			if (_windowAllowDrop != value)
			{
				_windowAllowDrop = value;
			}
		}
	}

	public bool AltKeyDown
	{
		get
		{
			return _altKeyDown;
		}
		set
		{
			if (_altKeyDown != value)
			{
				_altKeyDown = value;
				OnPropertyChanged("AltKeyDown");
				DataChangedNotifier.NotifyCalendarShowIconChanged();
				DataChangedNotifier.NotifyCountdownShowRemarkChanged();
			}
		}
	}

	public void NotifyPropertyChanged(string name)
	{
		OnPropertyChanged(name);
	}
}
