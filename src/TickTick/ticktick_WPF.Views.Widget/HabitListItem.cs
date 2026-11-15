using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ticktick_WPF.Models;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Widget;

public class HabitListItem : INotifyPropertyChanged
{
	private double _height;

	public string Id { get; set; }

	public string Name { get; set; }

	public BitmapSource BitmapSource { get; set; }

	public string Describe { get; set; }

	public SolidColorBrush Background { get; set; }

	public bool IsShowProgress { get; set; }

	public double PercentValue { get; set; }

	public double Step { get; set; }

	public string Type { get; set; }

	public bool IsCompleted { get; set; }

	public bool IsAbandon { get; set; }

	public double Height
	{
		get
		{
			return _height;
		}
		set
		{
			_height = value;
			NotifyPropertyChanged("Height");
		}
	}

	public HabitModel Habit { get; set; }

	public HabitDayCheckModel CheckIn { get; set; }

	public event PropertyChangedEventHandler? PropertyChanged;

	public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
