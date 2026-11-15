using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ticktick_WPF.Entity.Calendar;

internal class YearViewItem : INotifyPropertyChanged
{
	private double _offset;

	public double Offset
	{
		get
		{
			return _offset;
		}
		set
		{
			_offset = value;
			NotifyPropertyChanged("Offset");
		}
	}

	public double TargetOffset { get; set; }

	public bool IsViewStart { get; set; }

	public event PropertyChangedEventHandler PropertyChanged;

	public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
