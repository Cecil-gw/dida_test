using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace ticktick_WPF.Views.Template;

public static class TemplateScrollHelper
{
	public static readonly DependencyProperty SearchKeywordProperty = DependencyProperty.RegisterAttached("SearchKeyword", typeof(string), typeof(TemplateScrollHelper), new PropertyMetadata(null, OnSearchKeywordChanged));

	public static string GetSearchKeyword(DependencyObject obj)
	{
		return (string)obj.GetValue(SearchKeywordProperty);
	}

	public static void SetSearchKeyword(DependencyObject obj, string value)
	{
		obj.SetValue(SearchKeywordProperty, value);
	}

	private static void OnSearchKeywordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		ScrollViewer scrollViewer = d as ScrollViewer;
		if (scrollViewer != null)
		{
			scrollViewer.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, (Action)delegate
			{
				ScrollToFirstMatch(scrollViewer);
			});
		}
	}

	private static void ScrollToFirstMatch(ScrollViewer scrollViewer)
	{
		if (scrollViewer == null)
		{
			return;
		}
		string searchKeyword = GetSearchKeyword(scrollViewer);
		if (string.IsNullOrEmpty(searchKeyword))
		{
			scrollViewer.ScrollToTop();
			return;
		}
		foreach (TextBlock item in FindVisualChildren<TextBlock>(scrollViewer))
		{
			string previewText = TemplateHighlightHelper.GetPreviewText(item);
			if (!string.IsNullOrEmpty(previewText))
			{
				int num = previewText.ToLower().IndexOf(searchKeyword.ToLower());
				if (num >= 0)
				{
					ScrollToTextBlock(scrollViewer, item, num);
					break;
				}
			}
		}
	}

	private static void ScrollToTextBlock(ScrollViewer scrollViewer, TextBlock textBlock, int matchIndex)
	{
		try
		{
			if (!(scrollViewer.Content is Visual visual))
			{
				return;
			}
			Point point = textBlock.TransformToVisual(visual).Transform(new Point(0.0, 0.0));
			double num = 0.0;
			try
			{
				if (textBlock.Inlines.Count > 0)
				{
					TextPointer positionAtOffset = textBlock.ContentStart.GetPositionAtOffset(matchIndex, LogicalDirection.Forward);
					if (positionAtOffset != null)
					{
						num = positionAtOffset.GetCharacterRect(LogicalDirection.Forward).Top;
					}
				}
			}
			catch
			{
			}
			double val = scrollViewer.VerticalOffset + point.Y + num - 17.0;
			scrollViewer.ScrollToVerticalOffset(Math.Max(0.0, val));
		}
		catch
		{
		}
	}

	private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
	{
		if (parent == null)
		{
			yield break;
		}
		for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
		{
			DependencyObject child = VisualTreeHelper.GetChild(parent, i);
			if (child is T val)
			{
				yield return val;
			}
			foreach (T item in FindVisualChildren<T>(child))
			{
				yield return item;
			}
		}
	}
}
