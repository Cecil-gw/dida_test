using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class MathConverter : IValueConverter
{
	private static readonly char[] _allOperators = new char[7] { '+', '-', '*', '/', '%', '(', ')' };

	private static readonly List<string> _grouping = new List<string> { "(", ")" };

	private static readonly List<string> _operators = new List<string> { "+", "-", "*", "/", "%" };

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		string text = parameter as string;
		text = text.Replace(" ", "");
		bool flag = !string.IsNullOrEmpty(text) && text.Contains("@VALUEPOS");
		text = text.Replace("@VALUEPOS", value.ToString());
		text = text.Replace("@VALUE", value.ToString());
		List<double> numbers = new List<double>();
		string[] array = text.Split(_allOperators);
		foreach (string text2 in array)
		{
			if (text2 != string.Empty && MathUtil.TryParseString2Double(text2, out var db))
			{
				numbers.Add(db);
			}
		}
		try
		{
			EvaluateMathString(ref text, ref numbers, 0);
		}
		catch (Exception)
		{
		}
		if (flag)
		{
			return (numbers[0] < 0.0) ? 0.0 : numbers[0];
		}
		return numbers[0];
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}

	private void EvaluateMathString(ref string mathEquation, ref List<double> numbers, int index)
	{
		string nextToken = GetNextToken(mathEquation);
		while (nextToken != string.Empty)
		{
			mathEquation = mathEquation.Remove(0, nextToken.Length);
			if (_grouping.Contains(nextToken))
			{
				if (!(nextToken == "("))
				{
					if (nextToken == ")")
					{
						break;
					}
				}
				else
				{
					EvaluateMathString(ref mathEquation, ref numbers, index);
				}
			}
			if (_operators.Contains(nextToken))
			{
				string nextToken2 = GetNextToken(mathEquation);
				if (nextToken2 == "(")
				{
					EvaluateMathString(ref mathEquation, ref numbers, index + 1);
				}
				MathUtil.TryParseString2Double(nextToken2, out var db);
				if (numbers.Count <= index + 1 || (db != numbers[index + 1] && !(nextToken2 == "(")))
				{
					break;
				}
				switch (nextToken)
				{
				case "+":
					numbers[index] += numbers[index + 1];
					break;
				case "-":
					numbers[index] -= numbers[index + 1];
					break;
				case "*":
					numbers[index] *= numbers[index + 1];
					break;
				case "/":
					numbers[index] /= numbers[index + 1];
					break;
				case "%":
					numbers[index] %= numbers[index + 1];
					break;
				}
				numbers.RemoveAt(index + 1);
			}
			nextToken = GetNextToken(mathEquation);
		}
	}

	private static string GetNextToken(string mathEquation)
	{
		if (mathEquation == string.Empty)
		{
			return string.Empty;
		}
		string text = "";
		for (int i = 0; i < mathEquation.Length; i++)
		{
			char value = mathEquation[i];
			if (_allOperators.Contains(value))
			{
				if (!(text == ""))
				{
					return text;
				}
				return value.ToString();
			}
			text += value;
		}
		return text;
	}
}
