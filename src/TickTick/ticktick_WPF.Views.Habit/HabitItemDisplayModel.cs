using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ticktick_WPF.Views.Habit;

public class HabitItemDisplayModel : INotifyPropertyChanged
{
	public static readonly HabitItemDisplayModel DisplayModel = new HabitItemDisplayModel();

	private bool _showCurrentStreak;

	public bool ShowCurrentStreak
	{
		get
		{
			return _showCurrentStreak;
		}
		set
		{
			_showCurrentStreak = value;
			OnPropertyChanged("ShowCurrentStreak");
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
