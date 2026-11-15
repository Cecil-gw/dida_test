using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Emoji.Wpf;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Pomo.MainFocus;

public class SmallFocusView : Grid
{
	private FocusObjIcon _icon;

	private ClockControl _timeClock;

	private EmjTextBlock _titleText;

	private TextBlock _add1Text;

	private HoverIconButton _option;

	private HoverIconButton _dropOption;

	public SmallFocusView()
	{
		base.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		base.ColumnDefinitions.Add(new ColumnDefinition());
		base.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		base.Height = 72.0;
		Border border = new Border
		{
			BorderThickness = new Thickness(0.0, 1.0, 0.0, 0.0)
		};
		border.SetResourceReference(Panel.BackgroundProperty, "SmallFocusBackground");
		border.SetResourceReference(Border.BorderBrushProperty, "BaseColorOpacity5");
		border.SetValue(Grid.ColumnSpanProperty, 3);
		base.Children.Add(border);
		InitIcon();
		InitTimePanel();
		InitButtons();
		base.Loaded += OnLoaded;
		base.Unloaded += OnUnload;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		TickFocusManager.CurrentSecondChanged += SetCountText;
		PropertyChangedEventManager.AddHandler(TickFocusManager.Config.FocusVModel, OnTitleChanged, "Title");
		PropertyChangedEventManager.AddHandler(TickFocusManager.Config.FocusVModel, OnFocusChanged, "NoTask");
	}

	private void OnUnload(object sender, RoutedEventArgs e)
	{
		TickFocusManager.CurrentSecondChanged -= SetCountText;
		PropertyChangedEventManager.RemoveHandler(TickFocusManager.Config.FocusVModel, OnTitleChanged, "Title");
		PropertyChangedEventManager.RemoveHandler(TickFocusManager.Config.FocusVModel, OnFocusChanged, "NoTask");
	}

	private void InitButtons()
	{
		StackPanel stackPanel = new StackPanel
		{
			Margin = new Thickness(0.0, 0.0, 24.0, 0.0),
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Right,
			Orientation = Orientation.Horizontal
		};
		stackPanel.SetValue(Grid.ColumnProperty, 2);
		_option = new HoverIconButton
		{
			Height = 36.0,
			Width = 36.0
		};
		_option.BackBorder.CornerRadius = new CornerRadius(18.0);
		_option.BackBorder.SetResourceReference(FrameworkElement.StyleProperty, "SmoothHoverBorderStyle10_20");
		_option.BackBorder.SetResourceReference(Panel.BackgroundProperty, "PrimaryColor");
		_option.IsImage = false;
		_option.ImageWidth = 20.0;
		_option.ImageOpacity = 1.0;
		_option.SetIconColor("PrimaryColor");
		_option.MouseLeftButtonUp += OnOptionClick;
		stackPanel.Children.Add(_option);
		_dropOption = new HoverIconButton
		{
			Height = 36.0,
			Width = 36.0,
			Margin = new Thickness(16.0, 0.0, 0.0, 0.0)
		};
		_dropOption.BackBorder.CornerRadius = new CornerRadius(18.0);
		_dropOption.BackBorder.SetResourceReference(FrameworkElement.StyleProperty, "SmoothHoverBorderStyle10_20");
		_dropOption.BackBorder.SetResourceReference(Panel.BackgroundProperty, "BaseColorOpacity60");
		_dropOption.IsImage = false;
		_dropOption.ImageWidth = 20.0;
		_dropOption.ImageOpacity = 1.0;
		_dropOption.IconData = Utils.GetIcon("IcPomoStop");
		_dropOption.SetIconColor("BaseColorOpacity60");
		_dropOption.MouseLeftButtonUp += OnDropClick;
		stackPanel.Children.Add(_dropOption);
		base.Children.Add(stackPanel);
		SetButtons();
	}

	private void InitIcon()
	{
		_icon = new FocusObjIcon(30.0)
		{
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Left,
			Margin = new Thickness(22.0, 0.0, 0.0, 0.0)
		};
		base.Children.Add(_icon);
		SetPomoGotIcon();
	}

	private void InitTimePanel()
	{
		StackPanel stackPanel = new StackPanel
		{
			Margin = new Thickness(10.0, 4.0, 10.0, 0.0),
			VerticalAlignment = VerticalAlignment.Center
		};
		stackPanel.SetValue(Grid.ColumnProperty, 1);
		_titleText = new EmjTextBlock
		{
			TextWrapping = TextWrapping.Wrap,
			TextTrimming = TextTrimming.CharacterEllipsis,
			ClipToBounds = true,
			Height = 16.0
		};
		_titleText.SetResourceReference(FrameworkElement.StyleProperty, "Tag02");
		stackPanel.Children.Add(_titleText);
		SetTitleText();
		_add1Text = new TextBlock
		{
			Text = "+1",
			Foreground = ThemeUtil.GetColor("PomoRed"),
			FontSize = 24.0,
			Margin = new Thickness(0.0, -4.0, 0.0, 0.0)
		};
		stackPanel.Children.Add(_add1Text);
		_timeClock = new ClockControl
		{
			FontSize = 24.0,
			Margin = new Thickness(0.0, -4.0, 0.0, 0.0)
		};
		stackPanel.Children.Add(_timeClock);
		SetTimeText();
		base.Children.Add(stackPanel);
	}

	private void SetTimeText()
	{
		_timeClock.Visibility = ((TickFocusManager.Status == PomoStatus.WaitingRelax) ? Visibility.Collapsed : Visibility.Visible);
		_add1Text.Visibility = ((TickFocusManager.Status != PomoStatus.WaitingRelax) ? Visibility.Collapsed : Visibility.Visible);
		_timeClock.SetTime(TickFocusManager.DisplaySecond);
		_timeClock.SetResourceReference(ClockControl.ForegroundProperty, TickFocusManager.InRelax ? "PomoGreen" : "BaseColorOpacity90");
	}

	private void SetTitleText()
	{
		base.Dispatcher.Invoke(delegate
		{
			_titleText.Visibility = ((TickFocusManager.Status == PomoStatus.WaitingRelax) ? Visibility.Collapsed : Visibility.Visible);
			if (string.IsNullOrEmpty(TickFocusManager.Config.FocusVModel.FocusId))
			{
				switch (TickFocusManager.Status)
				{
				case PomoStatus.Working:
					_titleText.Text = Utils.GetString("Focusing");
					break;
				case PomoStatus.Relaxing:
					_titleText.Text = Utils.GetString("Relaxing");
					break;
				case PomoStatus.WaitingWork:
					_titleText.Text = Utils.GetString("Focus");
					break;
				case PomoStatus.Pause:
					_titleText.Text = Utils.GetString("Paused");
					break;
				case PomoStatus.WaitingRelax:
					break;
				}
			}
			else
			{
				_titleText.Text = TickFocusManager.Config.FocusVModel.Title;
			}
		});
	}

	private void SetButtons()
	{
		_dropOption.Visibility = ((TickFocusManager.Status != PomoStatus.Relaxing && TickFocusManager.Status != PomoStatus.Pause) ? Visibility.Collapsed : Visibility.Visible);
		_option.Visibility = ((TickFocusManager.Status == PomoStatus.Relaxing) ? Visibility.Collapsed : Visibility.Visible);
		string text = ((TickFocusManager.Status == PomoStatus.WaitingRelax) ? "PomoGreen" : "PrimaryColor");
		_option.BackBorder.SetResourceReference(Panel.BackgroundProperty, text);
		_option.IconData = Utils.GetIcon((TickFocusManager.Status == PomoStatus.Working) ? "IcPomoPause" : "IcPomoStart");
		_option.SetIconColor(text);
	}

	public void OnStatusChanged()
	{
		_icon.SetIcon();
		SetPomoGotIcon();
		SetTitleText();
		SetTimeText();
		SetButtons();
	}

	private void SetPomoGotIcon()
	{
		if (TickFocusManager.Status == PomoStatus.WaitingRelax)
		{
			BitmapImage bitmapImage = new BitmapImage();
			bitmapImage.BeginInit();
			bitmapImage.DecodePixelWidth = 60;
			bitmapImage.UriSource = new Uri("pack://application:,,,/Assets/get_pomo.png");
			bitmapImage.EndInit();
			bitmapImage.Freeze();
			_icon.Background = new ImageBrush
			{
				ImageSource = bitmapImage
			};
			_icon.Width = 40.0;
			_icon.Height = 40.0;
			_icon.Child = null;
		}
		else
		{
			_icon.Width = 30.0;
			_icon.Height = 30.0;
			_icon.SetFocusIcon(TickFocusManager.Config.FocusVModel.FocusId, TickFocusManager.Config.FocusVModel.Type);
		}
	}

	private void OnFocusChanged(object sender, PropertyChangedEventArgs e)
	{
		base.Dispatcher.Invoke(delegate
		{
			SetPomoGotIcon();
		});
	}

	private void OnTitleChanged(object sender, PropertyChangedEventArgs e)
	{
		SetTitleText();
	}

	public void OnFocusTypeChanged()
	{
		base.Dispatcher.Invoke(delegate
		{
			_icon.SetIcon();
		});
	}

	private void SetCountText()
	{
		base.Dispatcher.Invoke(delegate
		{
			_timeClock.SetTime(TickFocusManager.DisplaySecond);
		});
	}

	private void OnDropClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		FocusTimer.Drop();
		UserActCollectUtils.AddClickEvent("timer", "action_bar", "end");
	}

	private void OnOptionClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		switch (TickFocusManager.Status)
		{
		case PomoStatus.WaitingRelax:
			FocusTimer.BeginTimer();
			UserActCollectUtils.AddClickEvent("timer", "action_bar", "relax");
			break;
		case PomoStatus.WaitingWork:
			UserActCollectUtils.AddClickEvent("timer", "action_bar", TickFocusManager.Config.FromRelax ? "again" : "start");
			FocusTimer.BeginTimer();
			UserActCollectUtils.AddClickEvent("focus", "start_from_tab", "action_bar");
			UserActCollectUtils.AddClickEvent("focus", "start_from", "tab");
			break;
		case PomoStatus.Working:
			FocusTimer.Pause(DateTime.Now);
			UserActCollectUtils.AddClickEvent("timer", "action_bar", "pause");
			break;
		case PomoStatus.Pause:
			FocusTimer.Continue(DateTime.Now);
			UserActCollectUtils.AddClickEvent("timer", "action_bar", "continue");
			break;
		case PomoStatus.Relaxing:
			break;
		}
	}

	public void ReloadIcon()
	{
		SetPomoGotIcon();
	}
}
