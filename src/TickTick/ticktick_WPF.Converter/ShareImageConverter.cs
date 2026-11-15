using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;

namespace ticktick_WPF.Converter;

public class ShareImageConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Length == 2 && values[0] is string projectId)
		{
			ProjectModel projectById = CacheManager.GetProjectById(projectId);
			if (projectById != null && projectById.IsShareList())
			{
				string permission = projectById.permission;
				if (!(permission == "read"))
				{
					if (permission == "comment")
					{
						return (DrawingImage)(Application.Current?.FindResource("CanCommentDrawingImage"));
					}
					return (DrawingImage)(Application.Current?.FindResource("CanEditDrawingImage"));
				}
				return (DrawingImage)(Application.Current?.FindResource("ReadOnlyDrawingImage"));
			}
		}
		return (DrawingImage)(Application.Current?.FindResource("CanEditDrawingImage"));
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
