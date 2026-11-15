using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using ticktick_WPF.Models;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Widget;

public class HabitWeekWidgetViewModel : INotifyPropertyChanged
{
	private bool _isDark;

	public string Id { get; set; }

	public string Name { get; set; }

	public ImageSource Icon { get; set; }

	public string Days { get; set; }

	public string Describe { get; set; }

	public SolidColorBrush Background { get; set; }

	public ObservableCollection<HabitDayCheckModel> WeekCheckIns { get; set; } = new ObservableCollection<HabitDayCheckModel>();

	public HabitModel Habit { get; set; }

	public bool IsDark
	{
		get
		{
			return _isDark;
		}
		set
		{
			_isDark = value;
			NotifyPropertyChanged("IsDark");
		}
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
