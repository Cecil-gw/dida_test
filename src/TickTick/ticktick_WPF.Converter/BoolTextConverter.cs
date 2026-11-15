using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class BoolTextConverter : IValueConverter
{
	private readonly string _defaultKey;

	private readonly string _negativeKey;

	private readonly string _positiveKey;

	public BoolTextConverter(string positiveKey, string negativeKey, string defaultKey)
	{
		_positiveKey = positiveKey;
		_negativeKey = negativeKey;
		_defaultKey = defaultKey;
	}

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return Utils.GetBoolText(value, _positiveKey, _negativeKey, _defaultKey);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
