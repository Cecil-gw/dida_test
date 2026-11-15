using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Search;

namespace ticktick_WPF.Views.Template;

public class TemplateHighlightHelper
{
	public static readonly DependencyProperty HighlightTextProperty = DependencyProperty.RegisterAttached("HighlightText", typeof(string), typeof(TemplateHighlightHelper), new PropertyMetadata(null, OnHighlightChanged));

	public static readonly DependencyProperty SearchKeywordProperty = DependencyProperty.RegisterAttached("SearchKeyword", typeof(string), typeof(TemplateHighlightHelper), new PropertyMetadata(null, OnHighlightChanged));

	public static string GetHighlightText(DependencyObject obj)
	{
		return (string)obj.GetValue(HighlightTextProperty);
	}

	public static string GetPreviewText(DependencyObject obj)
	{
		return TaskUtils.GetContentMarkdownDisplayText(GetHighlightText(obj));
	}

	public static void SetHighlightText(DependencyObject obj, string value)
	{
		obj.SetValue(HighlightTextProperty, value);
	}

	public static string GetSearchKeyword(DependencyObject obj)
	{
		return (string)obj.GetValue(SearchKeywordProperty);
	}

	public static void SetSearchKeyword(DependencyObject obj, string value)
	{
		obj.SetValue(SearchKeywordProperty, value);
	}

	private static void OnHighlightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is TextBlock textBlock)
		{
			UpdateHighlight(textBlock);
		}
	}

	private static void UpdateHighlight(TextBlock textBlock)
	{
		string previewText = GetPreviewText(textBlock);
		string text = GetSearchKeyword(textBlock) ?? "";
		textBlock.Inlines.Clear();
		if (string.IsNullOrEmpty(text))
		{
			textBlock.Inlines.Add(previewText);
			return;
		}
		MatchCollection matchCollection = new Regex(Regex.Escape(text), RegexOptions.IgnoreCase).Matches(previewText);
		if (matchCollection.Count == 0)
		{
			textBlock.Inlines.Add(previewText);
			return;
		}
		int num = 0;
		foreach (Match item in matchCollection)
		{
			if (item.Index > num)
			{
				textBlock.Inlines.Add(previewText.Substring(num, item.Index - num));
			}
			textBlock.Inlines.Add(new Run(previewText.Substring(item.Index, item.Length))
			{
				Background = SearchHelper.GetSearchHighlightColor()
			});
			num = item.Index + item.Length;
		}
		if (num < previewText.Length)
		{
			textBlock.Inlines.Add(previewText.Substring(num));
		}
	}
}
