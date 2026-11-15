using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace ticktick_WPF.Framework;

public static class RichTextBoxBinder
{
	public static readonly DependencyProperty DocumentProperty = DependencyProperty.RegisterAttached("Document", typeof(FlowDocument), typeof(RichTextBoxBinder), new PropertyMetadata(null, OnDocumentChanged));

	public static FlowDocument GetDocument(RichTextBox obj)
	{
		return (FlowDocument)obj.GetValue(DocumentProperty);
	}

	public static void SetDocument(RichTextBox obj, FlowDocument value)
	{
		obj.SetValue(DocumentProperty, value);
	}

	private static void OnDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is RichTextBox richTextBox && e.NewValue is FlowDocument document)
		{
			richTextBox.Document = document;
		}
	}
}
