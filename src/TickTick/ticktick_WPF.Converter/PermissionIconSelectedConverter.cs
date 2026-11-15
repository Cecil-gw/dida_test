using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Converter;

public class PermissionIconSelectedConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is Constants.ProjectPermission projectPermission && parameter is string s && int.TryParse(s, out var result) && projectPermission == (Constants.ProjectPermission)result)
		{
			return true;
		}
		return false;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
