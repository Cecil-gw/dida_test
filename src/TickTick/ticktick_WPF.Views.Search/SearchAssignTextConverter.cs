using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Search;

public class SearchAssignTextConverter : IValueConverter
{
	private const int MaxAssigneeNameLength = 8;

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is List<string> assignees)
		{
			return GetSearchAssigneeText(assignees);
		}
		return Utils.GetString("assignee");
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}

	private static string GetSearchAssigneeText(List<string> assignees)
	{
		switch (assignees.Count)
		{
		case 0:
			return Utils.GetString("assignee");
		case 1:
			return TrimAssigneeName(AssigneeCardViewModel.GetAssigneeTitle(assignees[0]));
		default:
		{
			string text = string.Empty;
			int num = Math.Min(3, assignees.Count);
			for (int i = 0; i < num; i++)
			{
				string text2 = TrimAssigneeName(AssigneeCardViewModel.GetAssigneeTitle(assignees[i]));
				text += ((i < num - 1) ? (text2 + " " + Utils.GetString("or") + " ") : text2);
			}
			if (assignees.Count > 3)
			{
				text = string.Format(Utils.GetString("AssignDisplayMessage"), text, assignees.Count - 3);
			}
			return text;
		}
		}
	}

	private static string TrimAssigneeName(string name)
	{
		if (string.IsNullOrEmpty(name) || name.Length <= 8)
		{
			return name;
		}
		return name.Substring(0, 8) + "...";
	}
}
