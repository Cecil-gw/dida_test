using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ticktick_WPF.Views.Calendar;

public class CalendarConferenceTypeImageConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values.Length == 2 && values[0] is string text && values[1] is ResourceDictionary resourceDictionary)
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>
			{
				{ "video", "DrawConferenceVideo" },
				{ "phone", "DrawConferencePhone" },
				{ "ohter", "DrawConferenceOther" }
			};
			string text2 = text.ToLower();
			text2 = (dictionary.ContainsKey(text2) ? text2 : "other");
			return resourceDictionary[dictionary[text2]];
		}
		return null;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
