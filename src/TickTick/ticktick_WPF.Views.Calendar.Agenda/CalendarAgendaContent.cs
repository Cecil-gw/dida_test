using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Shapes;
using Emoji.Wpf;
using TickTickUtils.Lang;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Views.MarkDown.Colorizer;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.Views.Calendar.Agenda;

public class CalendarAgendaContent : Grid
{
	private readonly EmjTextBlock _title;

	private readonly AgendaContentIcons _icons;

	private EmjTextBlock _content;

	private ListItemTagsControl _tagPanel;

	private readonly Grid _contentGrid;

	private readonly Rectangle _botRect;

	private Border _colorRect;

	private TextBlock _timeText;

	public CalendarAgendaContent()
	{
		base.Cursor = Cursors.Hand;
		base.MinHeight = 44.0;
		base.Margin = new Thickness(0.0, 10.0, 0.0, 10.0);
		base.ColumnDefinitions.Add(new ColumnDefinition());
		base.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		_contentGrid = new Grid
		{
			VerticalAlignment = VerticalAlignment.Top,
			Margin = new Thickness(0.0, 10.0, 0.0, 10.0)
		};
		_contentGrid.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		_contentGrid.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		_contentGrid.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		_contentGrid.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		_botRect = new Rectangle
		{
			RadiusX = 6.0,
			RadiusY = 6.0,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
			Opacity = 0.1
		};
		_botRect.SetBinding(Shape.FillProperty, new Binding("ItemColor")
		{
			TargetNullValue = null
		});
		_botRect.SetValue(Grid.ColumnSpanProperty, 2);
		base.Children.Add(_botRect);
		_colorRect = new Border
		{
			Width = 4.0,
			CornerRadius = new CornerRadius(4.0, 0.0, 0.0, 4.0),
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Stretch
		};
		_colorRect.SetBinding(Panel.BackgroundProperty, new Binding("ItemColor")
		{
			TargetNullValue = null
		});
		_colorRect.SetValue(Grid.ColumnSpanProperty, 2);
		base.Children.Add(_colorRect);
		base.Children.Add(_contentGrid);
		_title = new EmjTextBlock
		{
			LineHeight = 20.0,
			ClipToBounds = true,
			FontSize = 14.0,
			TextTrimming = TextTrimming.CharacterEllipsis,
			TextWrapping = TextWrapping.Wrap,
			Margin = new Thickness(14.0, 1.0, 0.0, -1.0),
			VerticalAlignment = VerticalAlignment.Top
		};
		_title.SetResourceReference(TextBlock.ForegroundProperty, "BaseSolidColorOpacity80");
		_title.SetValue(Grid.RowProperty, 1);
		_contentGrid.Children.Add(_title);
		_icons = new AgendaContentIcons();
		base.Children.Add(_icons);
		_timeText = new TextBlock
		{
			LineHeight = 18.0,
			Margin = new Thickness(14.0, 0.0, 0.0, 0.0),
			VerticalAlignment = VerticalAlignment.Top,
			HorizontalAlignment = HorizontalAlignment.Left
		};
		_timeText.SetResourceReference(TextBlock.ForegroundProperty, "DateColorPrimary");
		_timeText.SetValue(Grid.RowProperty, 0);
		_timeText.SetBinding(TextBlock.TextProperty, "TimeSpanText");
		_timeText.SetBinding(UIElement.VisibilityProperty, "TimeSpanTextVisible");
		_contentGrid.Children.Add(_timeText);
		base.DataContextChanged += OnDataBind;
		base.MouseEnter += OnMouseLeaveOrLeave;
		base.MouseLeave += OnMouseLeaveOrLeave;
	}

	private void OnDataBind(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (e.NewValue is CalendarAgendaItemViewModel calendarAgendaItemViewModel)
		{
			if (calendarAgendaItemViewModel.IsEmpty || !string.IsNullOrEmpty(calendarAgendaItemViewModel.EmptyText))
			{
				base.Visibility = Visibility.Collapsed;
			}
			else
			{
				base.Visibility = Visibility.Visible;
			}
			_title.MaxHeight = (LocalSettings.Settings.ExtraSettings.ShowCalDetail ? 100 : 40);
			if (!calendarAgendaItemViewModel.IsEmpty)
			{
				SetTitle(calendarAgendaItemViewModel.Title, calendarAgendaItemViewModel.Status);
			}
			else
			{
				_title.Text = "";
			}
			TryShowContent(calendarAgendaItemViewModel);
			TryShowTag(calendarAgendaItemViewModel);
			SetBackground(calendarAgendaItemViewModel);
			_timeText.SetResourceReference(TextBlock.ForegroundProperty, (calendarAgendaItemViewModel.Status != 0) ? "BaseColorOpacity40" : ((!calendarAgendaItemViewModel.IsVirtual && DateUtils.IsOutDated(calendarAgendaItemViewModel.DisplayStartDate, calendarAgendaItemViewModel.DisplayDueDate, calendarAgendaItemViewModel.IsAllDay)) ? "OutDateColor" : "DateColorPrimary"));
			SetTextDecoration(calendarAgendaItemViewModel);
		}
	}

	public void SetTextDecoration(CalendarAgendaItemViewModel model)
	{
		_title.TextDecorations = ((LocalSettings.Settings.ExtraSettings.ShowCompleteLine == 1 && model.Status != 0) ? DeleteLineColorizer.HintStrikethrough : null);
	}

	private void TryShowTag(CalendarAgendaItemViewModel model)
	{
	}

	private async void OnTagDelete(object sender, string e)
	{
		object dataContext = base.DataContext;
		if (dataContext is CalendarAgendaItemViewModel model)
		{
			List<string> list = model.SourceViewModel.Tags.ToList();
			if (list.Contains(e))
			{
				list.Remove(e);
			}
			await TaskService.SetTags(model.Id, list);
			UtilLog.Info("AgendaContent.DeleteTag id " + model.Id + " tag " + e);
			SyncManager.TryDelaySync();
		}
	}

	private void TryShowContent(CalendarAgendaItemViewModel model)
	{
		if (LocalSettings.Settings.ExtraSettings.ShowCalDetail)
		{
			if (!string.IsNullOrEmpty(model.Content))
			{
				if (_content == null)
				{
					_content = new EmjTextBlock
					{
						LineHeight = 18.0,
						ClipToBounds = true,
						TextTrimming = TextTrimming.CharacterEllipsis,
						MaxHeight = 18.0,
						TextWrapping = TextWrapping.Wrap,
						Margin = new Thickness(14.0, 4.0, 0.0, -2.0)
					};
					_content.SetResourceReference(TextBlock.ForegroundProperty, "BaseSolidColorOpacity70");
					_content.SetBinding(EmjTextBlock.TextProperty, "Content");
					_content.SetValue(Grid.RowProperty, 2);
					_contentGrid.Children.Add(_content);
				}
			}
			else
			{
				ClearContent();
			}
		}
		else
		{
			ClearContent();
		}
		void ClearContent()
		{
			if (_content != null)
			{
				_contentGrid.Children.Remove(_content);
				_content = null;
			}
		}
	}

	public void SetIcons()
	{
		_icons.ResetIcons();
	}

	public void SetTitle(string title, int status)
	{
		if (string.IsNullOrEmpty(title))
		{
			_title.Text = Utils.GetString("NoTitle");
			_title.SetResourceReference(TextBlock.ForegroundProperty, "BaseSolidColorOpacity40");
		}
		else
		{
			_title.Text = title;
			_title.SetResourceReference(TextBlock.ForegroundProperty, (status == 0) ? "BaseSolidColorOpacity80" : "BaseColorOpacity40");
		}
	}

	public void OnStatusChanged()
	{
		if (base.DataContext is CalendarAgendaItemViewModel calendarAgendaItemViewModel)
		{
			SetBackground(calendarAgendaItemViewModel);
			_timeText.SetResourceReference(TextBlock.ForegroundProperty, (calendarAgendaItemViewModel.Status != 0) ? "BaseColorOpacity40" : (DateUtils.IsOutDated(calendarAgendaItemViewModel.DisplayStartDate, calendarAgendaItemViewModel.DisplayDueDate, calendarAgendaItemViewModel.IsAllDay) ? "OutDateColor" : "DateColorPrimary"));
			_title.SetResourceReference(TextBlock.ForegroundProperty, (calendarAgendaItemViewModel.Status == 0 && !string.IsNullOrEmpty(calendarAgendaItemViewModel.Title)) ? "BaseSolidColorOpacity80" : "BaseColorOpacity40");
		}
	}

	private void OnMouseLeaveOrLeave(object sender, MouseEventArgs e)
	{
		if (base.DataContext is CalendarAgendaItemViewModel background)
		{
			SetBackground(background);
		}
	}

	public void SetBackground(CalendarAgendaItemViewModel model)
	{
		_botRect.Opacity = (model.Selected ? 0.4 : (base.IsMouseOver ? 0.2 : 0.1));
		_colorRect.Opacity = ((model.Status == 0) ? 1.0 : (model.Selected ? 0.6 : (base.IsMouseOver ? 0.5 : 0.4)));
	}
}
