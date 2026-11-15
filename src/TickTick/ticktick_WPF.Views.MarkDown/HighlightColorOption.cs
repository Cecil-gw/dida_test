using System.ComponentModel;
using System.Globalization;
using System.Windows.Media;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.MarkDown;

public class HighlightColorOption : INotifyPropertyChanged
{
	private bool _isSelected;

	public string Color { get; private set; }

	public Brush Brush { get; private set; }

	public bool IsClear { get; private set; }

	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			if (_isSelected != value)
			{
				_isSelected = value;
				this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsSelected"));
			}
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	public static HighlightColorOption Clear()
	{
		return new HighlightColorOption
		{
			IsClear = true,
			Brush = ThemeUtil.GetColor("PrimaryColor")
		};
	}

	public static HighlightColorOption FromColor(HighlightColorData color)
	{
		return new HighlightColorOption
		{
			Color = color.OptionColor.ToUpper(CultureInfo.InvariantCulture),
			Brush = color.OptionBrush
		};
	}
}
