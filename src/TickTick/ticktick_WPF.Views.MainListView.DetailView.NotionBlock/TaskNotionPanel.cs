using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using TickTickUtils;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.MainListView.DetailView.NotionBlock;

public sealed class TaskNotionPanel : Border
{
	private const double NormalModeCodeBlockLeftMargin = 5.0;

	private const double StickyModeCodeBlockLeftMargin = 3.0;

	private readonly ItemsControl _itemsControl;

	private TaskNotionBlockViewModel _model;

	private readonly Line _split;

	public TaskNotionPanel(bool inSticky)
	{
		double num = (inSticky ? 3.0 : 5.0);
		base.Margin = new Thickness(num, 12.0, num, 16.0);
		SetResourceReference(Border.BackgroundProperty, inSticky ? "StickyNotionBackColor" : "BaseColorOpacity2");
		base.CornerRadius = new CornerRadius(14.0);
		base.Resources.MergedDictionaries.Add(new ResourceDictionary
		{
			Source = new Uri("Views/MainListView/DetailView/NotionBlock/NotionBlockResources.xaml", UriKind.Relative)
		});
		_itemsControl = new ItemsControl();
		_itemsControl.SetResourceReference(ItemsControl.ItemTemplateSelectorProperty, "NotionItemTemplateSelector");
		StackPanel stackPanel = (StackPanel)(Child = new StackPanel
		{
			Margin = new Thickness(0.0, 6.0, 0.0, 6.0)
		});
		stackPanel.Children.Add(_itemsControl);
		_split = new Line();
		_split.SetResourceReference(FrameworkElement.StyleProperty, "SplitLineStyle");
		_split.Margin = new Thickness(16.0, 6.0, 16.0, 6.0);
		stackPanel.Children.Add(_split);
		stackPanel.Children.Add(CreateNotionButton());
	}

	private Grid CreateNotionButton()
	{
		Grid obj = new Grid
		{
			Height = 48.0
		};
		Border border = new Border
		{
			Height = 28.0,
			Cursor = Cursors.Hand,
			CornerRadius = new CornerRadius(6.0),
			Margin = new Thickness(0.0, 0.0, 16.0, 0.0),
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Center,
			BorderThickness = new Thickness(1.0)
		};
		obj.Children.Add(border);
		border.SetResourceReference(FrameworkElement.StyleProperty, "HoverBorderStyle");
		border.MouseLeftButtonUp += delegate
		{
			Utils.TryProcessStartUrl(_model?.pageUrl);
		};
		TextBlock textBlock = new TextBlock
		{
			VerticalAlignment = VerticalAlignment.Center,
			LineHeight = 16.0,
			FontSize = 12.0,
			Margin = new Thickness(12.0, 0.0, 12.0, 0.0),
			Text = Utils.GetString("GoToView")
		};
		textBlock.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity100");
		border.Child = textBlock;
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(16.0, 0.0, 0.0, 0.0)
		};
		obj.Children.Add(stackPanel);
		Image element = new Image
		{
			Width = 18.0,
			Height = 18.0,
			Stretch = Stretch.Uniform,
			VerticalAlignment = VerticalAlignment.Center,
			Source = ImageUtils.GetResourceImage("pack://application:,,,/Assets/ImageSource/notion_36.png")
		};
		stackPanel.Children.Add(element);
		TextBlock textBlock2 = new TextBlock
		{
			VerticalAlignment = VerticalAlignment.Center,
			LineHeight = 20.0,
			FontSize = 14.0,
			Margin = new Thickness(6.0, 0.0, 0.0, 0.0),
			Text = "Notion"
		};
		textBlock2.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity100");
		stackPanel.Children.Add(textBlock2);
		return obj;
	}

	public void SetModel(TaskNotionBlockViewModel model)
	{
		_model = model;
		_itemsControl.ItemsSource = _model?.Items;
		_split.Visibility = ((model == null || model.Items.Count < 1) ? Visibility.Collapsed : Visibility.Visible);
	}

	public double GetHeight()
	{
		if (base.Visibility != Visibility.Visible)
		{
			return 0.0;
		}
		return base.ActualHeight + 28.0;
	}
}
