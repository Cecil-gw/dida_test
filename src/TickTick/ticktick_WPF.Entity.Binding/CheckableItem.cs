using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ticktick_WPF.Entity.Binding;

internal class CheckableItem<T> : INotifyPropertyChanged
{
	private bool _isChecked;

	private string _name;

	public bool IsChecked
	{
		get
		{
			return _isChecked;
		}
		set
		{
			_isChecked = value;
			NotifyPropertyChanged("IsChecked");
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

	public T Data { get; set; }

	public event PropertyChangedEventHandler PropertyChanged;

	public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
internal class CheckableItem : CheckableItem<object>
{
}
