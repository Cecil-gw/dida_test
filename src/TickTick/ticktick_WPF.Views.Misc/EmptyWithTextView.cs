using System.Windows;
using System.Windows.Controls;

namespace ticktick_WPF.Views.Misc;

public class EmptyWithTextView : StackPanel
{
	private readonly EmptyView _emptyView;

	private readonly TextBlock _title;

	private readonly TextBlock _content;

	public EmptyWithTextView()
		: this(200.0)
	{
	}

	public EmptyWithTextView(double size)
	{
		base.HorizontalAlignment = HorizontalAlignment.Stretch;
		_emptyView = new EmptyView(size);
		base.Children.Add(_emptyView);
		_title = new TextBlock
		{
			FontSize = 14.0,
			Margin = new Thickness(0.0, 2.0, 0.0, 0.0),
			LineHeight = 20.0,
			TextAlignment = TextAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center,
			TextWrapping = TextWrapping.Wrap
		};
		_content = new TextBlock
		{
			FontSize = 12.0,
			Margin = new Thickness(10.0, 4.0, 10.0, 0.0),
			LineHeight = 17.0,
			TextAlignment = TextAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center,
			TextWrapping = TextWrapping.Wrap
		};
		_title.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity100");
		_content.SetResourceReference(TextBlock.ForegroundProperty, "BaseSolidColorOpacity60");
		base.Children.Add(_title);
		base.Children.Add(_content);
	}

	public void SetData(string title, string content, EmptyIconsSourceModel model, bool setIcon5Color = true)
	{
		_title.Text = title;
		_content.Text = content;
		_emptyView.SetIconsSource(model, setIcon5Color);
	}

	internal void SetContent(string content)
	{
		_content.Text = content;
	}
}
