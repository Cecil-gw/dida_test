using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Emoji.Wpf;
using ticktick_WPF.Models;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Calendar;

namespace ticktick_WPF.Views.Pomo.FocusStatistics;

public class FocusRecordItem : Border
{
	private TextBlock _dateText;

	private Grid _recordGrid;

	private Path _icon;

	private static Geometry _pomoData;

	private static Geometry _timerData;

	private TextBlock _timeText;

	private TextBlock _durationText;

	private EmjTextBlock _noteText;

	private DockPanel _lineStack;

	private StackPanel _titleStack;

	private Border _hoverBorder;

	static FocusRecordItem()
	{
		_pomoData = Utils.GetIcon("IcPomo");
		_timerData = Utils.GetIcon("IcPomoTimer");
	}

	public FocusRecordItem()
	{
		base.Background = Brushes.Transparent;
		base.MinHeight = 48.0;
		base.Margin = new Thickness(22.0, 0.0, 22.0, 0.0);
		base.DataContextChanged += OnDataBind;
	}

	private void OnDataBind(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (!(e.NewValue is FocusRecordItemViewModel focusRecordItemViewModel))
		{
			return;
		}
		_lineStack?.Children.Clear();
		_titleStack?.Children.Clear();
		if (!string.IsNullOrEmpty(focusRecordItemViewModel.DateText))
		{
			if (_dateText == null)
			{
				_dateText = new TextBlock
				{
					FontSize = 14.0,
					FontWeight = FontWeights.Bold,
					VerticalAlignment = VerticalAlignment.Center
				};
				_dateText.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity40");
				_dateText.SetBinding(TextBlock.TextProperty, "DateText");
			}
			Child = _dateText;
			return;
		}
		if (_recordGrid == null)
		{
			_recordGrid = new Grid();
			_recordGrid.RowDefinitions.Add(new RowDefinition
			{
				Height = new GridLength(24.0)
			});
			_recordGrid.RowDefinitions.Add(new RowDefinition
			{
				MinHeight = 24.0
			});
			_recordGrid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(24.0)
			});
			_recordGrid.ColumnDefinitions.Add(new ColumnDefinition());
			Border border = new Border
			{
				Width = 24.0,
				Height = 24.0,
				CornerRadius = new CornerRadius(12.0),
				HorizontalAlignment = HorizontalAlignment.Left
			};
			border.SetResourceReference(Border.BackgroundProperty, "PrimaryColor10");
			_icon = new Path
			{
				Width = 14.0,
				Height = 14.0,
				Stretch = Stretch.Uniform
			};
			_icon.SetResourceReference(Shape.FillProperty, "PrimaryColor");
			border.Child = _icon;
			_hoverBorder = new Border
			{
				CornerRadius = new CornerRadius(4.0),
				Margin = new Thickness(4.0, 0.0, -4.0, 4.0),
				Background = Brushes.Transparent
			};
			_hoverBorder.SetValue(Grid.RowSpanProperty, 2);
			_hoverBorder.SetValue(Grid.ColumnProperty, 1);
			BindBorderEvent(_hoverBorder);
			_timeText = new TextBlock
			{
				FontSize = 12.0,
				Margin = new Thickness(8.0, 0.0, 0.0, 0.0),
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Left,
				IsHitTestVisible = false
			};
			_timeText.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity40");
			_timeText.SetValue(Grid.ColumnProperty, 1);
			_timeText.SetBinding(TextBlock.TextProperty, "TimeText");
			_durationText = new TextBlock
			{
				FontSize = 12.0,
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Right,
				IsHitTestVisible = false
			};
			_durationText.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity40");
			_durationText.SetValue(Grid.ColumnProperty, 1);
			_durationText.SetBinding(TextBlock.TextProperty, "DurationText");
			_recordGrid.Children.Add(border);
			_recordGrid.Children.Add(_hoverBorder);
			_recordGrid.Children.Add(_timeText);
			_recordGrid.Children.Add(_durationText);
			_noteText = new EmjTextBlock
			{
				TextWrapping = TextWrapping.Wrap,
				TextTrimming = TextTrimming.CharacterEllipsis,
				MinHeight = 18.0,
				MaxHeight = 54.0,
				Margin = new Thickness(0.0, 0.0, 0.0, 0.0),
				ClipToBounds = true,
				IsHitTestVisible = false
			};
			_noteText.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity60");
			_lineStack = new DockPanel
			{
				VerticalAlignment = VerticalAlignment.Stretch
			};
			_lineStack.SetValue(Grid.RowProperty, 1);
			_titleStack = new StackPanel
			{
				IsHitTestVisible = false
			};
			_titleStack.SetValue(Grid.RowProperty, 1);
			_titleStack.SetValue(Grid.ColumnProperty, 1);
			_titleStack.Margin = new Thickness(8.0, 0.0, 0.0, 8.0);
			_recordGrid.Children.Add(_lineStack);
			_recordGrid.Children.Add(_titleStack);
		}
		ObservableCollection<string> titles = focusRecordItemViewModel.Titles;
		bool flag = (titles != null && titles.Count > 0) || !string.IsNullOrWhiteSpace(focusRecordItemViewModel.Note);
		_hoverBorder.Margin = new Thickness(4.0, 0.0, -4.0, (flag && focusRecordItemViewModel.IsLastItem) ? 8 : (flag ? 20 : 24));
		Child = _recordGrid;
		_icon.Data = ((focusRecordItemViewModel.Type == 0) ? _pomoData : _timerData);
		SetLines(focusRecordItemViewModel);
		SetTitles(focusRecordItemViewModel);
	}

	private void BindBorderEvent(Border border)
	{
		border.Cursor = Cursors.Hand;
		border.MouseLeave += delegate
		{
			border.Background = Brushes.Transparent;
		};
		border.MouseEnter += delegate
		{
			border.SetResourceReference(Border.BackgroundProperty, "BaseColorOpacity5");
		};
		border.MouseLeftButtonUp += async delegate
		{
			object dataContext = base.DataContext;
			FocusRecordItemViewModel vModel = dataContext as FocusRecordItemViewModel;
			if (vModel != null)
			{
				UserActCollectUtils.AddClickEvent("focus", "focus_tab", "focus_record");
				PomodoroModel pomodoroModel = await PomoService.GetAdjustPomo(vModel.FocusId);
				if (pomodoroModel != null)
				{
					PomoDetailWindow pomoDetailWindow = new PomoDetailWindow(new PomoDisplayViewModel(pomodoroModel, pomodoroModel.Tasks), (CalendarDisplayViewModel)null);
					pomoDetailWindow.SetCustomStyle();
					pomoDetailWindow.Closed += async delegate
					{
						await Task.Delay(200);
						await vModel.ResetTitle();
						ResetLineAndTitle(vModel);
					};
					pomoDetailWindow.Show(border, border.Width, showByMouse: true);
				}
			}
		};
	}

	private void ResetLineAndTitle(FocusRecordItemViewModel vModel)
	{
		if (base.DataContext is FocusRecordItemViewModel focusRecordItemViewModel && focusRecordItemViewModel.FocusId == vModel.FocusId)
		{
			_lineStack?.Children.Clear();
			_titleStack?.Children.Clear();
			SetLines(vModel);
			SetTitles(vModel);
		}
	}

	private void SetTitles(FocusRecordItemViewModel model)
	{
		ObservableCollection<string> titles = model.Titles;
		if (titles != null && titles.Count > 0)
		{
			for (int i = 0; i < model.Titles.Count; i++)
			{
				EmjTextBlock emjTextBlock = new EmjTextBlock
				{
					Text = model.Titles[i],
					FontSize = 14.0,
					TextWrapping = TextWrapping.NoWrap,
					TextTrimming = TextTrimming.CharacterEllipsis,
					Margin = new Thickness(0.0, 4.0, 0.0, 4.0)
				};
				emjTextBlock.SetResourceReference(TextBlock.ForegroundProperty, model.UnlinkedTitleIndexes.Contains(i) ? "BaseColorOpacity40" : "BaseColorOpacity100");
				_titleStack.Children.Add(emjTextBlock);
			}
		}
		if (!string.IsNullOrWhiteSpace(model.Note))
		{
			_noteText.Text = model.Note;
			_titleStack.Children.Add(_noteText);
		}
		ObservableCollection<string> titles2 = model.Titles;
		bool flag = (titles2 == null || titles2.Count <= 0) && string.IsNullOrWhiteSpace(model.Note);
		_titleStack.Margin = new Thickness(8.0, 0.0, 0.0, (!flag) ? (model.IsLastItem ? 8 : 20) : 0);
	}

	private void SetLines(FocusRecordItemViewModel model)
	{
		if (model.IsLastItem)
		{
			ObservableCollection<string> titles = model.Titles;
			if (titles == null || titles.Count <= 0)
			{
				return;
			}
		}
		ObservableCollection<string> titles2 = model.Titles;
		if (titles2 != null && titles2.Count > 0)
		{
			for (int i = 0; i < model.Titles.Count; i++)
			{
				Rectangle rectangle = GetRect();
				if (i == 0)
				{
					rectangle.Height = 7.0;
					rectangle.Margin = new Thickness(0.0, 4.0, 0.0, 0.0);
				}
				_lineStack.Children.Add(rectangle);
				_lineStack.Children.Add(GetEllipse());
			}
		}
		Rectangle rectangle2 = new Rectangle
		{
			Width = 2.0
		};
		ObservableCollection<string> titles3 = model.Titles;
		rectangle2.Margin = new Thickness(0.0, (titles3 == null || titles3.Count <= 0) ? 4 : 0, 0.0, 4.0);
		if (!model.IsLastItem)
		{
			rectangle2.SetResourceReference(Shape.FillProperty, "PrimaryColor10");
		}
		_lineStack.Children.Add(rectangle2);
		static Ellipse GetEllipse()
		{
			Ellipse ellipse = new Ellipse();
			ellipse.Width = 8.0;
			ellipse.Height = 8.0;
			ellipse.StrokeThickness = 1.0;
			ellipse.SetValue(DockPanel.DockProperty, Dock.Top);
			ellipse.SetResourceReference(Shape.StrokeProperty, "PrimaryColor");
			return ellipse;
		}
		static Rectangle GetRect()
		{
			Rectangle rectangle3 = new Rectangle();
			rectangle3.Width = 2.0;
			rectangle3.Height = 18.0;
			rectangle3.SetValue(DockPanel.DockProperty, Dock.Top);
			rectangle3.SetResourceReference(Shape.FillProperty, "PrimaryColor10");
			return rectangle3;
		}
	}
}
