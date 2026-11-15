using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ticktick_WPF.Models;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Pomo.MainFocus;

namespace ticktick_WPF.Views.Pomo;

public class FocusOptionButtons : StackPanel
{
	private const string Start = "Start";

	private const string Pause = "Pause";

	private const string Continue = "Continue";

	private const string StartBreak = "StartBreak";

	private const string SkipBreak = "SkipBreak";

	private const string Stop = "Stop";

	protected const string Exit = "Exit";

	private double _buttonWidth = 140.0;

	private double _buttonHeight = 48.0;

	public void SetButtons()
	{
		base.Children.Clear();
		switch (TickFocusManager.Status)
		{
		case PomoStatus.Working:
			AddButton("Pause", "Pause");
			break;
		case PomoStatus.Relaxing:
			AddButton("SkipBreak", "StopRelax", isFill: true, isRest: true);
			AddButton("Exit", "Exit", isFill: false, isRest: true);
			break;
		case PomoStatus.WaitingWork:
		{
			bool fromRelax = TickFocusManager.Config.FromRelax;
			AddButton("Start", fromRelax ? "ContinuePomo" : "Start", isFill: true);
			if (fromRelax)
			{
				AddButton("Exit", "Exit");
			}
			break;
		}
		case PomoStatus.WaitingRelax:
			AddButton("StartBreak", "BeginRelax", isFill: true, isRest: true);
			AddButton("SkipBreak", "SkipRelax", isFill: false, isRest: true);
			AddButton("Exit", "Exit", isFill: false, isRest: true);
			break;
		case PomoStatus.Pause:
			AddButton("Continue", "Continue", isFill: true);
			AddButton("Stop", "End");
			break;
		}
	}

	protected virtual void AddButton(string option, string textKey, bool isFill = false, bool isRest = false)
	{
		double num = 16.0 - (48.0 - _buttonHeight) / 4.0;
		Button button = new Button
		{
			Margin = new Thickness(0.0, num, 0.0, 0.0),
			Tag = option,
			Content = Utils.GetString(textKey),
			Width = _buttonWidth,
			Height = _buttonHeight,
			FontSize = num
		};
		button.SetResourceReference(FrameworkElement.StyleProperty, "FocusOptionButtonStyle");
		if (!isFill)
		{
			button.Background = Brushes.Transparent;
			button.BorderThickness = new Thickness(1.0);
			button.SetResourceReference(Control.BorderBrushProperty, isRest ? "PomoGreen" : "PrimaryColor");
			button.SetResourceReference(Control.ForegroundProperty, isRest ? "PomoGreen" : "PrimaryColor");
		}
		else if (isRest)
		{
			button.SetResourceReference(Control.BackgroundProperty, "PomoGreen");
		}
		button.Click += OnOptionClick;
		base.Children.Add(button);
	}

	protected virtual async void AddTodayStatistic()
	{
		TextBlock text = new TextBlock
		{
			Height = 18.0,
			FontSize = 13.0,
			HorizontalAlignment = HorizontalAlignment.Center,
			TextAlignment = TextAlignment.Center,
			Margin = new Thickness(0.0, 16.0, 0.0, 30.0)
		};
		text.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity60");
		base.Children.Add(text);
		PomoStat pomoStat = await PomoService.LoadStatistics(TickFocusManager.IsPomo);
		text.Text = string.Format(Utils.GetString("TodayStatistics"), pomoStat.TodayPomos + Utils.GetString("Pomos"), Utils.GetDurationString(pomoStat.TodayDuration, isShort: false, showZero: true, showSpan: false));
	}

	protected void OnOptionClick(object sender, RoutedEventArgs e)
	{
		if (!(sender is FrameworkElement { Tag: string tag }))
		{
			return;
		}
		string data = "";
		string actCType = TickFocusManager.GetActCType();
		switch (tag)
		{
		case "Start":
		{
			data = ((TickFocusManager.IsPomo && TickFocusManager.Config.FromRelax) ? "continue" : "start");
			MainFocusControl mainFocusControl = Utils.FindParent<MainFocusControl>(this);
			if (mainFocusControl != null)
			{
				UserActCollectUtils.AddClickEvent("focus", "start_from_tab", mainFocusControl.ExistTimer() ? "action_bar_expand" : "default_page");
				UserActCollectUtils.AddClickEvent("focus", "start_from", "tab");
			}
			FocusTimer.BeginTimer();
			break;
		}
		case "Pause":
			data = "pause";
			FocusTimer.Pause(DateTime.Now);
			break;
		case "Continue":
			data = "continue";
			FocusTimer.Continue(DateTime.Now);
			break;
		case "StartBreak":
			data = "relax";
			FocusTimer.BeginTimer();
			break;
		case "SkipBreak":
			data = ((TickFocusManager.Status == PomoStatus.Relaxing) ? "finish" : "skip");
			FocusTimer.SkipRelax();
			break;
		case "Stop":
			data = "end";
			FocusTimer.StopOrAbandon();
			break;
		case "Exit":
			data = "exit";
			FocusTimer.Reset();
			break;
		}
		UserActCollectUtils.AddClickEvent("focus", actCType, data);
	}

	public void SetButtonWidth(double width, double height)
	{
		_buttonWidth = Math.Round(width, 0, MidpointRounding.ToEven);
		height = Math.Round(height, 0, MidpointRounding.AwayFromZero);
		_buttonHeight = height - (double)((int)height % 4);
		if (height - _buttonHeight > 2.0)
		{
			_buttonHeight += 4.0;
		}
		double num = 16.0 - (48.0 - _buttonHeight) / 4.0;
		foreach (object child in base.Children)
		{
			if (child is Button button)
			{
				button.Width = _buttonWidth;
				button.Height = _buttonHeight;
				button.FontSize = num;
				button.Margin = new Thickness(0.0, num, 0.0, 0.0);
			}
		}
	}
}
