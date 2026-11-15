using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ticktick_WPF.Views.Time;

public class DayCellControl : UserControl
{
	private Grid _rootGrid;

	private Border _rangeBorder;

	private Image _workImage;

	private TextBlock _dateText;

	private TextBlock _lunarText;

	public event EventHandler<DayViewModel> DayClicked;

	public event EventHandler<DayViewModel> DayDoubleClicked;

	public DayCellControl()
	{
		InitUI();
		base.MouseEnter += OnMouseEnter;
		base.MouseLeave += OnMouseLeave;
		base.MouseLeftButtonUp += OnClick;
		base.MouseDoubleClick += OnCellDoubleClick;
		base.DataContextChanged += OnDataBind;
		base.Unloaded += delegate
		{
			if (base.DataContext is DayViewModel source)
			{
				PropertyChangedEventManager.RemoveHandler(source, OnPropertyChanged, "");
			}
		};
	}

	private void OnDataBind(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (e.OldValue is DayViewModel source)
		{
			PropertyChangedEventManager.RemoveHandler(source, OnPropertyChanged, "");
		}
		if (e.NewValue is DayViewModel dayViewModel)
		{
			SetCursor(dayViewModel.IsFixed || !dayViewModel.CanSelect);
			SetRangeBorder(dayViewModel);
			_dateText.Text = dayViewModel.Date.Day.ToString();
			if (dayViewModel.IsRest)
			{
				_workImage.SetResourceReference(Image.SourceProperty, "RestDrawingImage");
			}
			else if (dayViewModel.IsWork)
			{
				_workImage.SetResourceReference(Image.SourceProperty, "WorkDrawingImage");
			}
			else
			{
				_workImage.Source = null;
			}
			if (dayViewModel.ShowLunarMonthFirstDay)
			{
				_lunarText.SetResourceReference(TextBlock.ForegroundProperty, "TickYellow");
			}
			else
			{
				Binding binding = new Binding("DateTextBrush")
				{
					Source = base.DataContext
				};
				_lunarText.SetBinding(TextBlock.ForegroundProperty, binding);
			}
			_lunarText.Visibility = (string.IsNullOrEmpty(dayViewModel.LunarText) ? Visibility.Collapsed : Visibility.Visible);
			_dateText.FontWeight = (dayViewModel.Selected ? FontWeights.Bold : FontWeights.Normal);
			PropertyChangedEventManager.AddHandler(dayViewModel, OnPropertyChanged, "");
			if (base.IsMouseOver)
			{
				dayViewModel.Hover = true;
			}
		}
	}

	private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (!(base.DataContext is DayViewModel dayViewModel))
		{
			return;
		}
		if (e.PropertyName == "IsRest")
		{
			if (dayViewModel.IsRest)
			{
				_workImage.SetResourceReference(Image.SourceProperty, "RestDrawingImage");
			}
			else if (dayViewModel.IsWork)
			{
				_workImage.SetResourceReference(Image.SourceProperty, "WorkDrawingImage");
			}
			else
			{
				_workImage.Source = null;
			}
		}
		if (e.PropertyName == "Date")
		{
			_dateText.Text = dayViewModel.Date.Day.ToString();
		}
		if (e.PropertyName == "Selected")
		{
			_dateText.FontWeight = (dayViewModel.Selected ? FontWeights.Bold : FontWeights.Normal);
		}
	}

	private void OnMouseEnter(object sender, MouseEventArgs e)
	{
		if (base.DataContext != null && base.DataContext is DayViewModel dayViewModel)
		{
			dayViewModel.Hover = true;
		}
	}

	private void OnMouseLeave(object sender, MouseEventArgs e)
	{
		if (base.DataContext != null && base.DataContext is DayViewModel dayViewModel)
		{
			dayViewModel.Hover = false;
		}
	}

	public void SetModelHover(bool hover)
	{
		if (base.DataContext != null && base.DataContext is DayViewModel dayViewModel)
		{
			dayViewModel.Hover = hover;
		}
	}

	private void OnClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		if (base.DataContext != null && base.DataContext is DayViewModel { CanSelect: not false, IsFixed: false } dayViewModel)
		{
			this.DayClicked?.Invoke(this, dayViewModel);
		}
	}

	private void OnCellDoubleClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		if (base.DataContext != null && base.DataContext is DayViewModel { CanSelect: not false, IsFixed: false, CanDoubleSelect: not false } dayViewModel)
		{
			dayViewModel.Selected = !dayViewModel.Selected;
			this.DayDoubleClicked?.Invoke(this, dayViewModel);
		}
	}

	public void InitUI()
	{
		_rootGrid = new Grid
		{
			Height = 34.0,
			Cursor = Cursors.Hand
		};
		base.Content = _rootGrid;
		_rootGrid.SizeChanged += OnSizeChanged;
		InitCircle();
		InitText();
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (_rangeBorder != null && _rangeBorder.Visibility == Visibility.Visible && base.DataContext is DayViewModel dayViewModel)
		{
			_rangeBorder.Width = GetRangeWidth(dayViewModel.Selection, base.ActualWidth);
		}
	}

	private void InitText()
	{
		Grid grid = new Grid
		{
			VerticalAlignment = VerticalAlignment.Stretch,
			HorizontalAlignment = HorizontalAlignment.Center
		};
		grid.SetValue(Panel.ZIndexProperty, 6);
		_rootGrid.Children.Add(grid);
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Vertical,
			VerticalAlignment = VerticalAlignment.Center
		};
		grid.Children.Add(stackPanel);
		_dateText = new TextBlock
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			TextAlignment = TextAlignment.Center,
			FontSize = 12.0,
			Margin = new Thickness(0.0)
		};
		_dateText.SetBinding(TextBlock.ForegroundProperty, "DateTextBrush");
		stackPanel.Children.Add(_dateText);
		_lunarText = new TextBlock
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			Opacity = 0.9,
			Margin = new Thickness(0.0, -1.0, 0.0, 0.0),
			FontSize = 7.0,
			TextTrimming = TextTrimming.CharacterEllipsis,
			TextWrapping = TextWrapping.WrapWithOverflow
		};
		_lunarText.SetBinding(TextBlock.TextProperty, "LunarText");
		stackPanel.Children.Add(_lunarText);
		Ellipse ellipse = new Ellipse
		{
			Height = 4.0,
			Width = 4.0,
			VerticalAlignment = VerticalAlignment.Bottom,
			HorizontalAlignment = HorizontalAlignment.Center,
			Opacity = 0.85
		};
		ellipse.SetBinding(UIElement.VisibilityProperty, "ShowHasTasks");
		ellipse.SetBinding(FrameworkElement.MarginProperty, "HasTasksMargin");
		ellipse.SetResourceReference(Shape.FillProperty, "PrimaryColor");
		grid.Children.Add(ellipse);
	}

	private void InitCircle()
	{
		Grid grid = new Grid
		{
			Width = 30.0,
			Height = 30.0,
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center
		};
		grid.SetValue(Panel.ZIndexProperty, 5);
		_rootGrid.Children.Add(grid);
		Rectangle rectangle = new Rectangle
		{
			Height = 30.0,
			Width = 30.0,
			RadiusX = 4.0,
			RadiusY = 4.0
		};
		rectangle.SetBinding(Shape.StrokeThicknessProperty, "TabBorderThickness");
		rectangle.SetResourceReference(Shape.StrokeProperty, "TabBorderColor");
		grid.Children.Add(rectangle);
		_workImage = new Image
		{
			Height = 10.0,
			Width = 10.0,
			VerticalAlignment = VerticalAlignment.Top,
			HorizontalAlignment = HorizontalAlignment.Right,
			Stretch = Stretch.Uniform
		};
		_workImage.SetBinding(UIElement.OpacityProperty, "WorkRestOpacity");
		grid.Children.Add(_workImage);
		Path path = new Path
		{
			Height = 30.0,
			Width = 30.0,
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center,
			Stretch = Stretch.Fill
		};
		path.SetBinding(Path.DataProperty, "EllipseGeometry");
		path.SetBinding(Shape.FillProperty, "EllipseColor");
		grid.Children.Add(path);
	}

	private void SetCursor(bool isArrow)
	{
		_rootGrid.Cursor = (isArrow ? Cursors.Arrow : Cursors.Hand);
	}

	private void SetRangeBorder(DayViewModel model)
	{
		if (model.Selection != SelectionMode.None)
		{
			if (_rangeBorder == null)
			{
				_rangeBorder = new Border
				{
					BorderThickness = new Thickness(0.0),
					Height = 30.0,
					VerticalAlignment = VerticalAlignment.Center
				};
				_rangeBorder.SetResourceReference(Control.BackgroundProperty, "DateRangeSelectColor");
				_rangeBorder.SetValue(Panel.ZIndexProperty, 1);
				_rootGrid.Children.Add(_rangeBorder);
			}
			_rangeBorder.HorizontalAlignment = ((model.Selection == SelectionMode.Start) ? HorizontalAlignment.Right : ((model.Selection != SelectionMode.End) ? HorizontalAlignment.Stretch : HorizontalAlignment.Left));
			switch (model.Selection)
			{
			case SelectionMode.Start:
				_rangeBorder.CornerRadius = new CornerRadius(15.0, 0.0, 0.0, 15.0);
				break;
			case SelectionMode.Middle:
				_rangeBorder.CornerRadius = new CornerRadius(0.0, 0.0, 0.0, 0.0);
				break;
			case SelectionMode.End:
				_rangeBorder.CornerRadius = new CornerRadius(0.0, 15.0, 15.0, 0.0);
				break;
			default:
				_rangeBorder.CornerRadius = new CornerRadius(15.0, 15.0, 15.0, 15.0);
				break;
			}
			_rangeBorder.Width = Math.Max(1.0, GetRangeWidth(model.Selection, base.ActualWidth));
			_rangeBorder.Visibility = Visibility.Visible;
		}
		else if (_rangeBorder != null)
		{
			_rangeBorder.Visibility = Visibility.Collapsed;
		}
	}

	private double GetRangeWidth(SelectionMode mode, double width)
	{
		switch (mode)
		{
		case SelectionMode.Full:
			return width - 2.0;
		case SelectionMode.Start:
		case SelectionMode.End:
			return width - (width - 30.0) / 2.0;
		default:
			return width;
		}
	}
}
