using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace ticktick_WPF.Views.CountDown.ViewModels;

public class DateDisplayData : INotifyPropertyChanged
{
	private double _valueFontSize;

	private double _unitFontSize;

	private FontWeight _valueFontWeight = FontWeights.Normal;

	private Thickness _unitMargin;

	private double rawValueFontSize { get; set; }

	private double rawUnitFontSize { get; set; }

	public string Value { get; set; }

	public double ValueFontSize
	{
		get
		{
			return _valueFontSize;
		}
		set
		{
			_valueFontSize = value;
			NotifyPropertyChanged("ValueFontSize");
		}
	}

	public string Unit { get; set; }

	public double UnitFontSize
	{
		get
		{
			return _unitFontSize;
		}
		set
		{
			_unitFontSize = value;
			NotifyPropertyChanged("UnitFontSize");
		}
	}

	public FontWeight ValueFontWeight
	{
		get
		{
			return _valueFontWeight;
		}
		set
		{
			_valueFontWeight = value;
			NotifyPropertyChanged("ValueFontWeight");
		}
	}

	public Thickness UnitMargin
	{
		get
		{
			return _unitMargin;
		}
		set
		{
			_unitMargin = value;
			NotifyPropertyChanged("UnitMargin");
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public DateDisplayData()
	{
	}

	public DateDisplayData(string value, string unit, double valueSize, double unitSize, bool bold = false)
	{
		Value = value;
		Unit = unit;
		rawValueFontSize = valueSize;
		rawUnitFontSize = unitSize;
		if (bold)
		{
			ValueFontWeight = FontWeights.Bold;
		}
	}

	public void UpdateScale(double scale, bool isPreviewMini = false, bool isEn = false)
	{
		ValueFontSize = rawValueFontSize * scale;
		UnitFontSize = rawUnitFontSize * scale;
		if (isPreviewMini)
		{
			UnitMargin = new Thickness(2.0, 0.0, 2.0, 0.333 * ValueFontSize - 1.7);
		}
		else
		{
			UnitMargin = new Thickness(4.0, 0.0, 4.0, 0.221 * ValueFontSize - (isEn ? 4.0 : 1.633));
		}
	}
}
