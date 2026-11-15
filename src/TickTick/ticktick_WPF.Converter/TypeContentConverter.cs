using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Converter;

public class TypeContentConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values.Length == 2)
		{
			CardType cardType = (CardType)values[0];
			string text = string.Empty;
			if (values[1] != null)
			{
				text = values[1].ToString();
			}
			switch (cardType)
			{
			case CardType.AddMore:
				return Utils.GetString("AddMore");
			case CardType.InitFilter:
				if (!string.IsNullOrEmpty(text))
				{
					return text;
				}
				return Utils.GetString("filterCondition");
			case CardType.InitLogic:
				return Utils.GetString("logic");
			case CardType.LogicAnd:
				return Utils.GetString("and");
			case CardType.LogicOr:
				return Utils.GetString("or");
			default:
				return text;
			}
		}
		return string.Empty;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
