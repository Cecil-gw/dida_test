using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Pomo.MainFocus;

public class FocusTimeClock : Grid
{
	private PomoProgressBar _progress;

	private ClockControl _timeText;

	private Border _subtractButton;

	private Border _addButton;

	private TextBlock _bottomText;

	public FocusTimeClock()
	{
		base.Width = 268.0;
		base.Height = 268.0;
		base.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		base.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		base.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		Ellipse ellipse = new Ellipse
		{
			Fill = Brushes.Transparent
		};
		ellipse.SetValue(Grid.ColumnSpanProperty, 3);
		base.Children.Add(ellipse);
		InitProgressBar();
		InitTimeText();
		InitAdjustButton();
		SetTime();
		base.MouseEnter += OnMouseEnter;
		base.MouseLeave += OnMouseLeave;
	}

	private void OnMouseLeave(object sender, MouseEventArgs e)
	{
		ShowOrHideAdjustButton(show: false);
	}

	private void OnMouseEnter(object sender, MouseEventArgs e)
	{
		if (TickFocusManager.Status == PomoStatus.Working && TickFocusManager.IsPomo)
		{
			ShowOrHideAdjustButton(show: true);
		}
	}

	private void ShowOrHideAdjustButton(bool show)
	{
		DoubleAnimation doubleAnimation = AnimationUtils.GetDoubleAnimation(null, show ? 1 : 0, 120);
		if (show)
		{
			doubleAnimation.BeginTime = TimeSpan.FromMilliseconds(100.0);
		}
		_subtractButton.BeginAnimation(UIElement.OpacityProperty, doubleAnimation);
		_addButton.BeginAnimation(UIElement.OpacityProperty, doubleAnimation);
		if (TickFocusManager.Status != PomoStatus.Pause)
		{
			_bottomText?.BeginAnimation(UIElement.OpacityProperty, doubleAnimation);
		}
		_subtractButton.IsEnabled = show;
		_addButton.IsEnabled = show;
	}

	private void InitAdjustButton()
	{
		_subtractButton = new Border
		{
			CornerRadius = new CornerRadius(2.0),
			Width = 24.0,
			Height = 24.0,
			HorizontalAlignment = HorizontalAlignment.Right,
			Margin = new Thickness(0.0, 0.0, 8.0, 0.0),
			Opacity = 0.0
		};
		_subtractButton.SetResourceReference(FrameworkElement.StyleProperty, "HoverBorderStyle");
		Path path = new Path
		{
			Height = 18.0,
			Width = 18.0,
			Stretch = Stretch.Uniform,
			Data = Utils.GetGeometry("Ic.TaskType.PartiallySelectLinear")
		};
		path.SetResourceReference(Shape.FillProperty, "PrimaryColor");
		_subtractButton.Child = path;
		_subtractButton.SetValue(Grid.ColumnProperty, 0);
		base.Children.Add(_subtractButton);
		_addButton = new Border
		{
			CornerRadius = new CornerRadius(2.0),
			Width = 24.0,
			Height = 24.0,
			HorizontalAlignment = HorizontalAlignment.Left,
			Margin = new Thickness(8.0, 0.0, 0.0, 0.0),
			Opacity = 0.0
		};
		_addButton.SetResourceReference(FrameworkElement.StyleProperty, "HoverBorderStyle");
		Path path2 = new Path
		{
			Height = 18.0,
			Width = 18.0,
			Stretch = Stretch.Uniform,
			Data = Utils.GetGeometry("Ic.ProjectList.Add")
		};
		path2.SetResourceReference(Shape.FillProperty, "PrimaryColor");
		_addButton.SetValue(Grid.ColumnProperty, 2);
		_addButton.Child = path2;
		base.Children.Add(_addButton);
		_subtractButton.MouseLeftButtonUp += SubtractDuration;
		_addButton.MouseLeftButtonUp += AddDuration;
	}

	private void AddDuration(object sender, MouseButtonEventArgs e)
	{
		TickFocusManager.AdjustDuration(add: true);
	}

	private void SubtractDuration(object sender, MouseButtonEventArgs e)
	{
		TickFocusManager.AdjustDuration(add: false);
	}

	private void InitProgressBar()
	{
		_progress = new PomoProgressBar
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			Height = 260.0,
			Width = 260.0,
			Angle = TickFocusManager.Config.GetDisplayAngle(next: false),
			StrokeThickness = 4.0,
			IsStrokeMode = TickFocusManager.IsPomo
		};
		_progress.SetValue(Grid.ColumnSpanProperty, 3);
		_progress.SetResourceReference(PomoProgressBar.UnderColorProperty, "BaseColorOpacity10");
		_progress.SetResourceReference(PomoProgressBar.TopColorProperty, "PrimaryColor");
		base.Children.Add(_progress);
	}

	private void InitTimeText()
	{
		_timeText = new ClockControl
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			FontSize = 45.0,
			Background = Brushes.Transparent
		};
		_timeText.SetValue(Grid.ColumnProperty, 1);
		_timeText.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity100");
		_timeText.MouseLeftButtonUp += OnTimeClick;
		_timeText.ShowHour = !TickFocusManager.IsPomo;
		base.Children.Add(_timeText);
		_bottomText = new TextBlock
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			TextAlignment = TextAlignment.Center,
			FontSize = 14.0,
			Background = Brushes.Transparent,
			Margin = new Thickness(0.0, 72.0, 0.0, 0.0),
			Opacity = 0.0
		};
		_bottomText.SetValue(Grid.ColumnProperty, 1);
		_bottomText.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity60");
		base.Children.Add(_bottomText);
		SetTime();
	}

	private void OnTimeClick(object sender, MouseButtonEventArgs e)
	{
		if (TickFocusManager.IsPomo && TickFocusManager.Status == PomoStatus.WaitingWork)
		{
			EscPopup obj = new EscPopup
			{
				StaysOpen = false,
				PlacementTarget = _timeText,
				Placement = PlacementMode.Center,
				HorizontalOffset = 2.0,
				VerticalOffset = 80.0
			};
			obj.Child = new PomoModitySpanConfig(obj);
			obj.IsOpen = true;
			UserActCollectUtils.AddClickEvent("focus", "focus_tab", "click_time");
		}
	}

	public void SetTime()
	{
		if (!_progress.IsStrokeMode && TickFocusManager.Config.CurrentSeconds == 0.0)
		{
			_progress.Reset();
		}
		if (TickFocusManager.Status == PomoStatus.Working || TickFocusManager.Status == PomoStatus.Relaxing)
		{
			_progress.TargetAngel = TickFocusManager.Config.GetDisplayAngle(next: true);
		}
		else
		{
			_progress.SetAngle(TickFocusManager.Config.GetDisplayAngle(next: false));
		}
		if (!_progress.IsStrokeMode && TickFocusManager.Config.CurrentSeconds >= 30.0)
		{
			_progress.HideLeftMask();
		}
		SetTimeText();
	}

	private void SetTimeText()
	{
		_timeText.SetTime(TickFocusManager.DisplaySecond);
		if (TickFocusManager.Status == PomoStatus.Pause)
		{
			return;
		}
		double num = (double)TickFocusManager.Config.Second + TickFocusManager.Config.PauseDuration;
		if (!(num < 0.0))
		{
			if (num > 2592000.0)
			{
				UtilLog.Info("FocusTimeClock SetTimeText Error Time " + TickFocusManager.Config.Second + "  " + TickFocusManager.Config.PauseDuration);
				num = 2592000.0;
			}
			DateTime startTime = TickFocusManager.Config.StartTime;
			DateTime startTime2 = TickFocusManager.Config.StartTime.AddSeconds(num);
			_bottomText.Text = ticktick_WPF.Util.DateUtils.GetTimeText(startTime) + " - " + ticktick_WPF.Util.DateUtils.GetTimeText(startTime2);
		}
	}

	public void SetProgressMode()
	{
		_progress.IsStrokeMode = TickFocusManager.IsPomo;
		_timeText.ShowHour = !TickFocusManager.IsPomo;
		_progress.Reset();
	}

	public void OnStatusChanged()
	{
		if (TickFocusManager.Status == PomoStatus.Working || TickFocusManager.Status == PomoStatus.Relaxing)
		{
			SetTime();
		}
		if (TickFocusManager.Status != PomoStatus.Working)
		{
			ShowOrHideAdjustButton(show: false);
		}
		if (TickFocusManager.Status == PomoStatus.Pause)
		{
			_bottomText.BeginAnimation(UIElement.OpacityProperty, null);
			_bottomText.Opacity = 1.0;
			_bottomText.Text = Utils.GetString("Paused");
		}
		else
		{
			_bottomText.Opacity = (base.IsMouseOver ? 1 : 0);
		}
		if (TickFocusManager.IsPomo && TickFocusManager.Status == PomoStatus.WaitingWork)
		{
			_timeText.Cursor = Cursors.Hand;
		}
		else
		{
			_timeText.Cursor = Cursors.Arrow;
		}
		if (TickFocusManager.Status == PomoStatus.Relaxing)
		{
			_progress.SetResourceReference(PomoProgressBar.TopColorProperty, "PomoGreen");
		}
		else
		{
			_progress.SetResourceReference(PomoProgressBar.TopColorProperty, "PrimaryColor");
		}
	}

	public void SetSize(double width)
	{
		width = Math.Round(width, 0, MidpointRounding.AwayFromZero);
		double num = width - (double)((int)width % 4);
		if (width - num > 2.0)
		{
			num += 4.0;
		}
		base.Width = num;
		base.Height = num;
		_progress.SetSize(num);
		_timeText.FontSize = num / 6.0;
		_bottomText.Margin = new Thickness(0.0, 72.0 + num / 6.0 - 44.0, 0.0, 0.0);
	}
}
