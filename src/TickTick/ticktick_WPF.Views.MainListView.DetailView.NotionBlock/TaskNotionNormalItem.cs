using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Emoji.Wpf;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.MainListView.DetailView.NotionBlock;

public class TaskNotionNormalItem : Grid
{
	private readonly TextBlock _keyText;

	private readonly ContentControl _content;

	private double _keyWidth = 88.0;

	public TaskNotionNormalItem()
	{
		base.Height = 38.0;
		_keyText = new EmjTextBlock
		{
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Center,
			LineHeight = 20.0,
			FontSize = 14.0,
			Margin = new Thickness(16.0, 0.0, 0.0, 0.0),
			MinWidth = 56.0,
			TextTrimming = TextTrimming.CharacterEllipsis
		};
		_keyText.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity100");
		_keyText.SetBinding(TextBlock.TextProperty, "key");
		base.Children.Add(_keyText);
		_content = new ContentControl
		{
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(8.0, 0.0, 16.0, 0.0)
		};
		base.Children.Add(_content);
		base.DataContextChanged += OnDataContextChanged;
		base.SizeChanged += OnSizeChanged;
		if (_content != null)
		{
			_content.SizeChanged += OnContentSizeChanged;
		}
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs e)
	{
		AdjustTextWidths();
	}

	private void AdjustTextWidths()
	{
		ContentControl content = _content;
		if (content != null && content.ActualWidth > 0.0)
		{
			double num = base.ActualWidth - 16.0 - 8.0 - 16.0;
			double actualWidth = _content.ActualWidth;
			double maxWidth = Math.Max(56.0, num - actualWidth);
			_keyText.MaxWidth = maxWidth;
		}
	}

	private void OnContentSizeChanged(object sender, SizeChangedEventArgs e)
	{
		AdjustTextWidths();
		if (!(base.DataContext is TaskNotionBlockItemViewModel { type: "multi_select" } taskNotionBlockItemViewModel) || !(_content.Content is StackPanel stackPanel))
		{
			return;
		}
		double num = 0.0;
		int num2 = 0;
		int num3 = 0;
		double actualWidth = _keyText.ActualWidth;
		foreach (object child in stackPanel.Children)
		{
			if (child is Border { Tag: var tag } border && tag is double num4)
			{
				bool num5 = num <= 1.0;
				num += num4;
				if (num5 || num < base.ActualWidth - actualWidth - 28.0 - (double)((num3 < taskNotionBlockItemViewModel.multi_select.Count - 1) ? 24 : 0))
				{
					num2++;
					border.Visibility = Visibility.Visible;
				}
				else
				{
					border.Visibility = Visibility.Collapsed;
				}
			}
			if (child is TextBlock textBlock)
			{
				textBlock.Visibility = ((num2 >= taskNotionBlockItemViewModel.multi_select.Count) ? Visibility.Collapsed : Visibility.Visible);
				textBlock.Text = "+" + (taskNotionBlockItemViewModel.multi_select.Count - num2);
			}
			num3++;
		}
	}

	private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (!(base.DataContext is TaskNotionBlockItemViewModel taskNotionBlockItemViewModel))
		{
			return;
		}
		_keyWidth = Utils.MeasureStringWidth(taskNotionBlockItemViewModel.key, 16.0);
		_keyWidth = Math.Min(_keyWidth, 88.0);
		switch (taskNotionBlockItemViewModel.type)
		{
		case "people":
			SetPeopleContent(taskNotionBlockItemViewModel.People);
			break;
		case "select":
			SetSelectContent(new List<TaskNotionBlockItemSelectViewModel> { taskNotionBlockItemViewModel.select });
			break;
		case "status":
			SetStatusContent(taskNotionBlockItemViewModel.status);
			break;
		case "multi_select":
			base.SizeChanged -= OnContentSizeChanged;
			base.SizeChanged += OnContentSizeChanged;
			_content.SizeChanged -= OnContentSizeChanged;
			_content.SizeChanged += OnContentSizeChanged;
			SetSelectContent(taskNotionBlockItemViewModel.multi_select);
			break;
		case "number":
			SetTextContent(taskNotionBlockItemViewModel.number.ToString() ?? "");
			break;
		case "date":
		{
			TaskNotionBlockItemDateViewModel date = taskNotionBlockItemViewModel.date;
			if (date != null && date.start.HasValue)
			{
				if (taskNotionBlockItemViewModel.date.isAllDay && taskNotionBlockItemViewModel.date.end.HasValue)
				{
					taskNotionBlockItemViewModel.date.end = taskNotionBlockItemViewModel.date.end?.AddDays(1.0);
				}
				string textContent = DateUtils.FormatDateString(taskNotionBlockItemViewModel.date.start.Value, taskNotionBlockItemViewModel.date.end, taskNotionBlockItemViewModel.date.isAllDay);
				SetTextContent(textContent);
			}
			break;
		}
		}
	}

	private void SetTextContent(string text)
	{
		TextBlock textBlock = new TextBlock
		{
			FontSize = 12.0,
			LineHeight = 18.0,
			VerticalAlignment = VerticalAlignment.Center,
			TextTrimming = TextTrimming.CharacterEllipsis,
			Margin = new Thickness(_keyWidth + 36.0, 0.0, 0.0, 0.0)
		};
		textBlock.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity60");
		textBlock.Text = text;
		_content.Content = textBlock;
	}

	private void SetStatusContent(TaskNotionBlockItemSelectViewModel status)
	{
		Border border = new Border
		{
			Height = 22.0,
			CornerRadius = new CornerRadius(11.0)
		};
		border.SetResourceReference(Panel.BackgroundProperty, "BaseColorOpacity6");
		StackPanel stackPanel = (StackPanel)(border.Child = new StackPanel
		{
			Orientation = Orientation.Horizontal
		});
		Ellipse ellipse = new Ellipse
		{
			Width = 8.0,
			Height = 8.0,
			Margin = new Thickness(7.0, 0.0, 0.0, 0.0)
		};
		ellipse.SetResourceReference(Shape.FillProperty, "BaseColorOpacity40");
		stackPanel.Children.Add(ellipse);
		EmjTextBlock emjTextBlock = new EmjTextBlock
		{
			MaxWidth = 120.0,
			TextTrimming = TextTrimming.CharacterEllipsis,
			ClipToBounds = true,
			FontSize = 13.0,
			LineHeight = 17.0,
			Height = 17.0,
			TextWrapping = TextWrapping.Wrap,
			Padding = new Thickness(4.0, 0.0, 7.0, 0.0),
			VerticalAlignment = VerticalAlignment.Center,
			Text = status.name
		};
		emjTextBlock.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity100");
		stackPanel.Children.Add(emjTextBlock);
		_content.Content = border;
	}

	private void SetSelectContent(List<TaskNotionBlockItemSelectViewModel> selects)
	{
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal
		};
		foreach (TaskNotionBlockItemSelectViewModel select in selects)
		{
			Border border = new Border
			{
				Height = 22.0,
				CornerRadius = new CornerRadius(4.0),
				Margin = new Thickness(4.0, 0.0, 0.0, 0.0)
			};
			border.SetResourceReference(Panel.BackgroundProperty, "BaseColorOpacity6");
			EmjTextBlock emjTextBlock = new EmjTextBlock
			{
				MaxWidth = 120.0,
				TextTrimming = TextTrimming.CharacterEllipsis,
				ClipToBounds = true,
				FontSize = 13.0,
				LineHeight = 17.0,
				Height = 17.0,
				TextWrapping = TextWrapping.Wrap,
				Padding = new Thickness(6.0, 0.0, 6.0, 0.0),
				VerticalAlignment = VerticalAlignment.Center,
				Text = select.name
			};
			double val = Utils.MeasureStringWidth(select.name, 13.0);
			border.Tag = Math.Min(val, 120.0) + 16.0;
			emjTextBlock.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity100");
			border.Child = emjTextBlock;
			stackPanel.Children.Add(border);
		}
		if (selects.Count > 1)
		{
			TextBlock textBlock = new TextBlock
			{
				FontSize = 12.0,
				LineHeight = 16.0,
				Padding = new Thickness(4.0, 0.0, 0.0, 0.0),
				VerticalAlignment = VerticalAlignment.Center,
				Visibility = Visibility.Collapsed
			};
			textBlock.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity40");
			stackPanel.Children.Add(textBlock);
		}
		_content.Content = stackPanel;
	}

	private void SetPeopleContent(List<TaskNotionBlockItemPeopleViewModel> peoples)
	{
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal
		};
		if (peoples.Count > 1)
		{
			Grid grid = new Grid();
			stackPanel.Children.Add(grid);
			for (int i = 0; i < peoples.Count && i < 5; i++)
			{
				TaskNotionBlockItemPeopleViewModel people = peoples[i];
				Border border = GetPeople(people);
				border.SetValue(Panel.ZIndexProperty, peoples.Count + 10 - i);
				border.Margin = new Thickness(i * 19, 0.0, 0.0, 0.0);
				grid.Children.Add(border);
			}
			if (peoples.Count > 5)
			{
				TextBlock textBlock = new TextBlock
				{
					FontSize = 12.0,
					LineHeight = 16.0,
					Padding = new Thickness(4.0, 0.0, 0.0, 0.0),
					VerticalAlignment = VerticalAlignment.Center,
					Opacity = 0.3,
					Text = "+" + (peoples.Count - 5)
				};
				textBlock.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity100");
				stackPanel.Children.Add(textBlock);
			}
		}
		else if (peoples.Count == 1)
		{
			TaskNotionBlockItemPeopleViewModel taskNotionBlockItemPeopleViewModel = peoples[0];
			Border element = GetPeople(taskNotionBlockItemPeopleViewModel);
			stackPanel.Children.Add(element);
			EmjTextBlock emjTextBlock = new EmjTextBlock
			{
				FontSize = 13.0,
				LineHeight = 17.0,
				Padding = new Thickness(4.0, 0.0, 0.0, 0.0),
				MaxWidth = 120.0,
				TextTrimming = TextTrimming.CharacterEllipsis,
				VerticalAlignment = VerticalAlignment.Center,
				Text = taskNotionBlockItemPeopleViewModel.name
			};
			emjTextBlock.SetResourceReference(TextBlock.ForegroundProperty, "BaseSolidColorOpacity60");
			stackPanel.Children.Add(emjTextBlock);
		}
		_content.Content = stackPanel;
		Border GetPeople(TaskNotionBlockItemPeopleViewModel taskNotionBlockItemPeopleViewModel2)
		{
			Border border2 = new Border
			{
				Width = 26.0,
				Height = 26.0,
				BorderThickness = new Thickness(0.0),
				CornerRadius = new CornerRadius(13.0),
				Cursor = Cursors.Hand,
				HorizontalAlignment = HorizontalAlignment.Left
			};
			if (!string.IsNullOrWhiteSpace(taskNotionBlockItemPeopleViewModel2.email))
			{
				ToolTip toolTip = new ToolTip
				{
					Content = taskNotionBlockItemPeopleViewModel2.email,
					Placement = PlacementMode.Center,
					VerticalOffset = -30.0
				};
				toolTip.SetResourceReference(FrameworkElement.StyleProperty, "TooltipStyle");
				border2.ToolTip = toolTip;
			}
			border2.SetResourceReference(Border.BorderBrushProperty, "NotionPeopleBorderColor");
			border2.SetResourceReference(Border.BackgroundProperty, "NotionPeopleBorderColor");
			Border border3 = (Border)(border2.Child = new Border
			{
				Width = 24.0,
				Height = 24.0,
				CornerRadius = new CornerRadius(12.0),
				BorderThickness = new Thickness(1.0)
			});
			border3.SetResourceReference(Panel.BackgroundProperty, "NotionPeopleTextBackColor");
			border3.SetResourceReference(Border.BorderBrushProperty, "BaseColorOpacity10");
			TextBlock textBlock2 = new TextBlock
			{
				FontSize = 12.0,
				LineHeight = 16.0,
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Center
			};
			string name = taskNotionBlockItemPeopleViewModel2.name;
			if (name != null && name.Length > 0)
			{
				textBlock2.Text = taskNotionBlockItemPeopleViewModel2.name.Substring(0, 1).ToUpper();
			}
			textBlock2.SetResourceReference(TextBlock.ForegroundProperty, "NotionPeopleTextColor");
			border3.Child = textBlock2;
			LoadAvatar(border2, taskNotionBlockItemPeopleViewModel2.avatar_url);
			return border2;
		}
	}

	private async Task LoadAvatar(Border border, string avatarUrl)
	{
		if (!string.IsNullOrWhiteSpace(avatarUrl))
		{
			BitmapImage bitmapImage = await AvatarHelper.GetNotionAvatarByUrlAsync(avatarUrl);
			if (bitmapImage != null)
			{
				border.Child = null;
				border.Background = new ImageBrush
				{
					ImageSource = bitmapImage
				};
				border.BorderThickness = new Thickness(1.0);
			}
		}
	}
}
