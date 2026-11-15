using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Converter;

public class RecentCommentConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value != null && value is CommentViewModel)
		{
			CommentViewModel commentViewModel = (CommentViewModel)value;
			if (parameter.ToString() == "Content")
			{
				return commentViewModel.Content;
			}
			if (parameter.ToString() == "CreateDate")
			{
				return DateUtils.FormatCommentTime(commentViewModel.CreateDate);
			}
			if (parameter.ToString() == "Avatar")
			{
				return commentViewModel.Avatar;
			}
			if (parameter.ToString() == "UserName")
			{
				return commentViewModel.UserName;
			}
		}
		return string.Empty;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
