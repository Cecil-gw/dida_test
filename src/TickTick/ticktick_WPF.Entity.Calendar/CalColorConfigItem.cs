using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace ticktick_WPF.Entity.Calendar;

public class CalColorConfigItem : INotifyPropertyChanged
{
	public enum CalColorItemDataType
	{
		Unknow,
		Project,
		Tag,
		Habit,
		Countdown
	}

	private bool _isOpen = true;

	private string _colorStr;

	public bool CanSetColor { get; set; } = true;

	public bool IsParent
	{
		get
		{
			if (Items != null)
			{
				return Items.Any();
			}
			return false;
		}
	}

	public ObservableCollection<CalColorConfigItem> Items { get; set; }

	public bool IsOpen
	{
		get
		{
			return _isOpen;
		}
		set
		{
			_isOpen = value;
			NotifyPropertyChanged("IsOpen");
		}
	}

	public string Id { get; set; }

	public CalColorItemDataType DataType { get; set; }

	public IconType IconType { get; set; }

	public string Emoji { get; set; }

	public ImageSource ImageSource { get; set; }

	public Geometry Geometry { get; set; }

	public string ImagePath { get; set; }

	public string Name { get; set; }

	public string ColorStr
	{
		get
		{
			return _colorStr;
		}
		set
		{
			_colorStr = value;
			NotifyPropertyChanged("ColorStr");
		}
	}

	public bool IsInbox { get; set; }

	public event PropertyChangedEventHandler PropertyChanged;

	public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
