using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using ticktick_WPF.Util;

namespace ticktick_WPF.Resource;

public class IconLayerItem : INotifyPropertyChanged
{
	private Geometry? _geometry;

	private SolidColorBrush? _brush = ThemeUtil.GetColor("BaseColorOpacity100");

	private double _opacity = 1.0;

	public Geometry? Geometry
	{
		get
		{
			return _geometry;
		}
		set
		{
			_geometry = value;
			OnPropertyChanged("Geometry");
		}
	}

	public SolidColorBrush? Brush
	{
		get
		{
			return _brush;
		}
		set
		{
			_brush = value;
			OnPropertyChanged("Brush");
		}
	}

	public double Opacity
	{
		get
		{
			return _opacity;
		}
		set
		{
			_opacity = value;
			OnPropertyChanged("Opacity");
		}
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
