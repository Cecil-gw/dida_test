using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ticktick_WPF.Views.Project;

public class AddSubscribeItem : INotifyPropertyChanged
{
	private bool _isHighlight;

	public string Type { get; set; }

	public string Title { get; set; }

	public string Describe { get; set; }

	public string ImagePath { get; set; }

	public bool IsHighlight
	{
		get
		{
			return _isHighlight;
		}
		set
		{
			_isHighlight = value;
			NotifyPropertyChanged("IsHighlight");
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
