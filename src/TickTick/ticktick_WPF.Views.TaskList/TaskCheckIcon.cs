using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ticktick_WPF.Models;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.TaskList;

public sealed class TaskCheckIcon : Canvas
{
	private readonly Path _icon;

	private Rectangle _backBorder;

	private bool _showBack;

	private Image _countdownImage;

	private Brush IconFill { get; set; }

	public TaskCheckIcon()
	{
		base.Height = 17.0;
		SetBinding(FrameworkElement.WidthProperty, "IconWidth");
		base.Background = Brushes.Transparent;
		_icon = new Path
		{
			Width = 17.0,
			Height = 17.0,
			Stretch = Stretch.Uniform
		};
		base.Children.Add(_icon);
		_icon.SetBinding(Path.DataProperty, "Icon");
		_icon.SetBinding(FrameworkElement.WidthProperty, "IconWidth");
		SetBinding(FrameworkElement.DataContextProperty, new Binding());
		base.DataContextChanged += TaskCheckIcon_DataContextChanged;
		base.MouseEnter += OnMouseOverChange;
		base.MouseLeave += OnMouseOverChange;
	}

	private void TaskCheckIcon_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (e.OldValue is DisplayItemModel displayItemModel)
		{
			displayItemModel.PropertyChanged -= DisplayItemModel_PropertyChanged;
		}
		if (e.NewValue is DisplayItemModel displayItemModel2)
		{
			displayItemModel2.PropertyChanged += DisplayItemModel_PropertyChanged;
			UpdateCountdownImage(displayItemModel2);
		}
	}

	private void DisplayItemModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == "CountdownIcon")
		{
			UpdateCountdownImage(sender as DisplayItemModel);
		}
	}

	private void UpdateCountdownImage(DisplayItemModel model)
	{
		if (model == null)
		{
			return;
		}
		if (model.IsCountdown)
		{
			if (_countdownImage == null)
			{
				_countdownImage = new Image
				{
					Width = 20.0,
					Height = 20.0,
					Margin = new Thickness(-4.0, -3.0, 0.0, 0.0),
					Stretch = Stretch.Uniform
				};
				base.Children.Add(_countdownImage);
			}
			if (_icon != null)
			{
				_icon.Visibility = Visibility.Collapsed;
			}
			if (_backBorder != null)
			{
				base.Children.Remove(_backBorder);
				_backBorder = null;
			}
			CountdownModel countdownModel = (model.SourceViewModel as CountDownBaseViewModel)?.Countdown;
			if (countdownModel != null)
			{
				_countdownImage.Source = CountdownService.GetIcon(countdownModel.IconRes, countdownModel.Color);
			}
		}
		else
		{
			if (_countdownImage != null)
			{
				base.Children.Remove(_countdownImage);
				_countdownImage = null;
			}
			if (_icon != null)
			{
				_icon.Visibility = Visibility.Visible;
			}
			SetIconColor(model.Type, model.Priority, model.Status);
		}
	}

	public TaskCheckIcon(double size)
	{
		base.Height = size;
		base.Width = size;
		base.Background = Brushes.Transparent;
		_icon = new Path
		{
			Width = size,
			Height = size,
			Stretch = Stretch.Uniform
		};
		base.Children.Add(_icon);
		_icon.SetBinding(Path.DataProperty, "Icon");
		SetBinding(FrameworkElement.DataContextProperty, new Binding());
		base.DataContextChanged += TaskCheckIcon_DataContextChanged;
		base.MouseEnter += OnMouseOverChange;
		base.MouseLeave += OnMouseOverChange;
	}

	private void OnMouseOverChange(object sender, MouseEventArgs e)
	{
		SetColor();
	}

	private void SetColor()
	{
		if (_countdownImage != null && _countdownImage.Visibility == Visibility.Visible)
		{
			return;
		}
		if (IconFill == null)
		{
			_icon.Fill = ThemeUtil.GetColor(base.IsMouseOver ? "BaseColorOpacity40" : "BaseColorOpacity10", this);
			if (_backBorder != null)
			{
				base.Children.Remove(_backBorder);
			}
			return;
		}
		_icon.Fill = IconFill;
		if (base.IsMouseOver && _showBack)
		{
			if (_backBorder == null)
			{
				_backBorder = new Rectangle
				{
					Width = 14.0,
					Height = 14.0,
					RadiusX = 2.0,
					RadiusY = 2.0,
					Opacity = 0.1
				};
				base.Children.Add(_backBorder);
			}
			_backBorder.Fill = IconFill;
		}
		else if (_backBorder != null)
		{
			base.Children.Remove(_backBorder);
			_backBorder = null;
		}
	}

	public void SetIconColor(DisplayType type, int priority, int status)
	{
		if (_countdownImage != null && _countdownImage.Visibility == Visibility.Visible)
		{
			return;
		}
		_showBack = type != DisplayType.Course && type != DisplayType.Event && type != DisplayType.Note && status == 0;
		if (status == 0 && (type == DisplayType.Task || type == DisplayType.CheckItem || type == DisplayType.Agenda))
		{
			switch (priority)
			{
			case 1:
				IconFill = ThemeUtil.GetColor("PriorityLowColor");
				break;
			case 3:
				IconFill = ThemeUtil.GetColor("PriorityMiddleColor");
				break;
			case 5:
				IconFill = ThemeUtil.GetColor("PriorityHighColor");
				break;
			case 0:
				IconFill = ThemeUtil.GetColor("BaseColorOpacity40", this);
				break;
			}
		}
		else if (status != 0)
		{
			IconFill = null;
		}
		else
		{
			IconFill = ThemeUtil.GetColor("BaseColorOpacity40", this);
		}
		SetColor();
	}

	public void SetSize(int size)
	{
		base.Height = size;
		_icon.Width = size;
		_icon.Height = size;
		if (_countdownImage != null)
		{
			_countdownImage.Width = size;
			_countdownImage.Height = size;
		}
	}
}
