using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class AttachmentIconConverter : IValueConverter
{
	private static readonly Dictionary<string, string> AttachmentIconDict = new Dictionary<string, string>
	{
		{ "AUDIO", "pack://application:,,,/Assets/Attachment_Icon/Attachment_audio" },
		{ "VIDEO", "pack://application:,,,/Assets/Attachment_Icon/Attachment_video" },
		{ "PDF", "pack://application:,,,/Assets/Attachment_Icon/Attachment_pdf" },
		{ "DOC", "pack://application:,,,/Assets/Attachment_Icon/Attachment_doc" },
		{ "TEXT", "pack://application:,,,/Assets/Attachment_Icon/Attachment_txt" },
		{ "XLS", "pack://application:,,,/Assets/Attachment_Icon/Attachment_xls" },
		{ "PPT", "pack://application:,,,/Assets/Attachment_Icon/Attachment_ppt" },
		{ "CSV", "pack://application:,,,/Assets/Attachment_Icon/Attachment_csv" },
		{ "ZIP", "pack://application:,,,/Assets/Attachment_Icon/Attachment_zip" },
		{ "OTHER", "pack://application:,,,/Assets/Attachment_Icon/Attachment_unknown" }
	};

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value != null)
		{
			string text = AttachmentIconDict["OTHER"];
			string key = value.ToString();
			if (AttachmentIconDict.ContainsKey(key))
			{
				text = AttachmentIconDict[key];
			}
			if (parameter is AttachmentConvertParam { IsFailed: not false } attachmentConvertParam)
			{
				text += "_failed";
				if (attachmentConvertParam.IsDark)
				{
					text += "_dark";
				}
			}
			return text + ".png";
		}
		return null;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
