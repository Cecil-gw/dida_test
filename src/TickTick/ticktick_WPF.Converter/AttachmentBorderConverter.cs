using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class AttachmentBorderConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value != null)
		{
			return value.ToString() switch
			{
				"AUDIO" => ThemeUtil.GetColor("AudioBorderColor"), 
				"VIDEO" => ThemeUtil.GetColor("VideoBorderColor"), 
				"PDF" => ThemeUtil.GetColor("PdfBorderColor"), 
				"DOC" => ThemeUtil.GetColor("DocBorderColor"), 
				"TEXT" => ThemeUtil.GetColor("TxtBorderColor"), 
				"XLS" => ThemeUtil.GetColor("XlsBorderColor"), 
				"PPT" => ThemeUtil.GetColor("PptBorderColor"), 
				"CSV" => ThemeUtil.GetColor("CsvBorderColor"), 
				"ZIP" => ThemeUtil.GetColor("ZipBorderColor"), 
				"OTHER" => ThemeUtil.GetColor("OtherBorderColor"), 
				_ => ThemeUtil.GetColor("OtherBorderColor"), 
			};
		}
		return null;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
