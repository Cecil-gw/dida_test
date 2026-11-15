using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using ticktick_WPF.Resource;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Converter;

public class PtfAddButtonVisibleConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values.Length == 4)
		{
			object obj = values[1];
			bool flag = default(bool);
			int num;
			if (obj is bool)
			{
				flag = (bool)obj;
				num = 1;
			}
			else
			{
				num = 0;
			}
			if (((uint)num & (flag ? 1u : 0u)) != 0)
			{
				return Visibility.Collapsed;
			}
			if (values[0] is TeamGroupViewModel teamGroupViewModel && values[3] is string text)
			{
				if (teamGroupViewModel.TeamId == text)
				{
					return Visibility.Visible;
				}
				return Visibility.Collapsed;
			}
			if (values[0] is PtfAllViewModel ptfAllViewModel && values[2] is PtfType ptfType && ptfAllViewModel.Type == ptfType)
			{
				return Visibility.Visible;
			}
		}
		return Visibility.Collapsed;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
