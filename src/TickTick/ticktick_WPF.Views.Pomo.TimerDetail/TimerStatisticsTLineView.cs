using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using TickTickUtils;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Pomo.TimerDetail;

public class TimerStatisticsTLineView : Grid
{
	private TextBlock _totalDuration;

	private Border _switchButton;

	private TimerDetailLineView _lineView;

	private TimerDetailLineView _lineViewExtra;

	private TextBlock _popText;

	private Border _popBorder;

	private double _offset = -1.0;

	private Border _rightBorder;

	public TimerStatisticsTLineView()
	{
		base.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(36.0)
		});
		base.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(26.0)
		});
		base.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(170.0)
		});
		base.Background = Brushes.Transparent;
		InitTotalText();
		InitIntervalSwitch();
		InitDateSpan();
		InitTimeline();
		base.DataContextChanged += OnDataBind;
		base.MouseMove += OnMouseMove;
		base.Loaded += delegate
		{
			DataChangedNotifier.WeekStartFromChanged += OnWeekStartChanged;
			DataChangedNotifier.IsDarkChanged += OnIsDarkChanged;
		};
		base.Unloaded += delegate
		{
			DataChangedNotifier.WeekStartFromChanged -= OnWeekStartChanged;
			DataChangedNotifier.IsDarkChanged -= OnIsDarkChanged;
		};
	}

	private void OnIsDarkChanged(object sender, EventArgs e)
	{
		ReloadData();
	}

	private async void OnWeekStartChanged(object sender, EventArgs e)
	{
		object dataContext = base.DataContext;
		if (dataContext is TimerTimelineItemViewModel { Interval: "week" } model)
		{
			model.StartDate = Utils.GetWeekStart(DateTime.Today);
			model.EndDate = model.StartDate.AddDays(6.0);
			model.SetDateText();
			model.NotifySpanChanged();
			SetData(model, await model.GetStatistics());
		}
	}

	private void InitTimeline()
	{
		_lineView = new TimerDetailLineView
		{
			Margin = new Thickness(22.0, 0.0, 0.0, 0.0),
			RenderTransform = new TranslateTransform
			{
				X = 0.0
			},
			Background = Brushes.Transparent
		};
		_lineView.SetValue(Grid.RowProperty, 2);
		base.Children.Add(_lineView);
	}

	private void InitDateSpan()
	{
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Margin = new Thickness(20.0, 0.0, 0.0, 0.0)
		};
		Border border = new Border
		{
			Child = UiUtils.GetArrow(12.0, 90.0, "BaseColorOpacity100"),
			Width = 14.0,
			Height = 20.0,
			Cursor = Cursors.Hand
		};
		border.SetResourceReference(FrameworkElement.StyleProperty, "SmoothHoverBorderStyle40_60");
		border.MouseLeftButtonUp += OnLeftClick;
		stackPanel.Children.Add(border);
		TextBlock textBlock = new TextBlock
		{
			Margin = new Thickness(3.0, 0.0, 3.0, 0.0)
		};
		textBlock.SetResourceReference(FrameworkElement.StyleProperty, "Tag02");
		textBlock.SetBinding(TextBlock.TextProperty, "DateText");
		stackPanel.Children.Add(textBlock);
		Border border2 = new Border
		{
			Child = UiUtils.GetArrow(12.0, -90.0, "BaseColorOpacity100"),
			Width = 14.0,
			Height = 20.0,
			Cursor = Cursors.Hand
		};
		border2.SetResourceReference(FrameworkElement.StyleProperty, "SmoothHoverBorderStyle40_60");
		_rightBorder = new Border
		{
			Child = border2
		};
		stackPanel.Children.Add(_rightBorder);
		_rightBorder.MouseLeftButtonUp += OnRightClick;
		stackPanel.SetValue(Grid.RowProperty, 1);
		base.Children.Add(stackPanel);
	}

	private void InitIntervalSwitch()
	{
		_switchButton = new Border
		{
			Height = 24.0,
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Bottom,
			CornerRadius = new CornerRadius(12.0),
			Margin = new Thickness(0.0, 0.0, 22.0, 0.0),
			BorderThickness = new Thickness(1.0)
		};
		_switchButton.SetResourceReference(FrameworkElement.StyleProperty, "HoverBorderStyle");
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Margin = new Thickness(12.0, 0.0, 12.0, 0.0)
		};
		TextBlock textBlock = new TextBlock
		{
			Margin = new Thickness(0.0, 0.0, 6.0, 0.0)
		};
		textBlock.SetResourceReference(FrameworkElement.StyleProperty, "Body01");
		textBlock.SetBinding(TextBlock.TextProperty, "IntervalText");
		stackPanel.Children.Add(textBlock);
		Path arrow = UiUtils.GetArrow(12.0, 0.0, "BaseColorOpacity40");
		stackPanel.Children.Add(arrow);
		_switchButton.Child = stackPanel;
		base.Children.Add(_switchButton);
		_switchButton.MouseLeftButtonUp += OnSwitchClick;
	}

	private void InitTotalText()
	{
		_totalDuration = new TextBlock
		{
			FontSize = 24.0,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Bottom,
			Margin = new Thickness(22.0, 10.0, 0.0, 0.0)
		};
		_totalDuration.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity90");
		base.Children.Add(_totalDuration);
	}

	private void SetTotalDurationText(long duration)
	{
		UiUtils.SetTimeTextRun(_totalDuration, duration, 14, "BaseColorOpacity80");
	}

	private void OnSwitchClick(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext is TimerTimelineItemViewModel timerTimelineItemViewModel)
		{
			Mouse.Capture(null);
			EscPopup popup = new EscPopup
			{
				PlacementTarget = (sender as UIElement),
				StaysOpen = false,
				Placement = PlacementMode.Bottom,
				VerticalOffset = -5.0,
				HorizontalOffset = -5.0
			};
			CustomMenuList customMenuList = new CustomMenuList(new List<CustomMenuItemViewModel>
			{
				new CustomMenuItemViewModel((object)"week", Utils.GetString("TimelineWeek"), (DrawingImage)null, "")
				{
					Selected = (timerTimelineItemViewModel.Interval == "week")
				},
				new CustomMenuItemViewModel((object)"month", Utils.GetString("TimelineMonth"), (DrawingImage)null, "")
				{
					Selected = (timerTimelineItemViewModel.Interval == "month")
				},
				new CustomMenuItemViewModel((object)"year", Utils.GetString("TimelineYear"), (DrawingImage)null, "")
				{
					Selected = (timerTimelineItemViewModel.Interval == "year")
				}
			}, popup);
			customMenuList.Operated += OnActionSelected;
			customMenuList.Show();
		}
	}

	private async void OnActionSelected(object sender, object e)
	{
		object dataContext = base.DataContext;
		if (dataContext is TimerTimelineItemViewModel model)
		{
			string text = e.ToString();
			if (!(text != "week") || ProChecker.CheckPro(ProType.MoreCharts, Window.GetWindow(this)))
			{
				model.SetInterval(text);
				SetData(model, await model.GetStatistics());
			}
		}
	}

	private async void OnRightClick(object sender, MouseButtonEventArgs e)
	{
		object dataContext = base.DataContext;
		if (dataContext is TimerTimelineItemViewModel { StartDate: var dateTime, EndDate: var endDate } model)
		{
			switch (model.Interval)
			{
			case "week":
				dateTime = model.StartDate.AddDays(7.0);
				endDate = dateTime.AddDays(6.0);
				break;
			case "month":
				dateTime = model.StartDate.AddMonths(1);
				endDate = dateTime.AddMonths(1).AddDays(-1.0);
				break;
			case "year":
				dateTime = model.StartDate.AddYears(1);
				endDate = dateTime.AddYears(1).AddDays(-1.0);
				break;
			}
			if (!(dateTime >= DateTime.Today.AddDays(1.0)))
			{
				model.StartDate = dateTime;
				model.EndDate = endDate;
				model.SetDateText();
				model.NotifySpanChanged(getFront: false);
				Dictionary<string, long> statisticsData = await model.GetStatistics();
				SetLineAnimation(left: false);
				SetData(model, statisticsData);
			}
		}
	}

	private async void OnLeftClick(object sender, MouseButtonEventArgs e)
	{
		object dataContext = base.DataContext;
		if (dataContext is TimerTimelineItemViewModel model)
		{
			switch (model.Interval)
			{
			case "week":
				model.StartDate = model.StartDate.AddDays(-7.0);
				model.EndDate = model.StartDate.AddDays(6.0);
				break;
			case "month":
				model.StartDate = model.StartDate.AddMonths(-1);
				model.EndDate = model.StartDate.AddMonths(1).AddDays(-1.0);
				break;
			case "year":
				model.StartDate = model.StartDate.AddYears(-1);
				model.EndDate = model.StartDate.AddYears(1).AddDays(-1.0);
				break;
			}
			model.SetDateText();
			model.NotifySpanChanged();
			Dictionary<string, long> statisticsData = await model.GetStatistics();
			SetLineAnimation(left: true);
			SetData(model, statisticsData);
		}
	}

	private void SetLineAnimation(bool left)
	{
		if (_lineViewExtra == null)
		{
			_lineViewExtra = new TimerDetailLineView
			{
				Margin = new Thickness(20.0, 0.0, 0.0, 0.0),
				RenderTransform = new TranslateTransform
				{
					X = ((!left) ? 1 : (-1)) * 50
				},
				Visibility = Visibility.Collapsed
			};
			_lineViewExtra.SetValue(Grid.RowProperty, 2);
			base.Children.Add(_lineViewExtra);
		}
		TimerDetailLineView lineView = _lineView;
		_lineView = _lineViewExtra;
		_lineViewExtra = lineView;
		if (_lineView.RenderTransform is TranslateTransform translateTransform)
		{
			_lineView.Visibility = Visibility.Visible;
			DoubleAnimation doubleAnimation = AnimationUtils.GetDoubleAnimation(((!left) ? 1 : (-1)) * 80, 0.0, 180);
			doubleAnimation.EasingFunction = new QuadraticEase
			{
				EasingMode = EasingMode.EaseInOut
			};
			doubleAnimation.Completed += delegate
			{
				_lineViewExtra.Visibility = Visibility.Collapsed;
			};
			translateTransform.BeginAnimation(TranslateTransform.XProperty, doubleAnimation);
		}
		_lineView.BeginAnimation(UIElement.OpacityProperty, AnimationUtils.GetDoubleAnimation(0.0, 1.0, 100));
		_lineViewExtra.BeginAnimation(UIElement.OpacityProperty, AnimationUtils.GetDoubleAnimation(1.0, 0.0, 100));
	}

	private async void OnDataBind(object sender, DependencyPropertyChangedEventArgs e)
	{
		object newValue = e.NewValue;
		if (newValue is TimerTimelineItemViewModel model)
		{
			SetData(model, await model.GetStatistics());
			PropertyChangedEventManager.AddHandler(model, Reload, "Reload");
		}
	}

	private void Reload(object sender, PropertyChangedEventArgs e)
	{
		ReloadData();
	}

	private void ReloadData()
	{
		base.Dispatcher.Invoke((Func<Task>)async delegate
		{
			object dataContext = base.DataContext;
			if (dataContext is TimerTimelineItemViewModel model)
			{
				SetData(model, await model.GetStatistics());
			}
		});
	}

	private void SetData(TimerTimelineItemViewModel model, Dictionary<string, long> statisticsData)
	{
		long totalDurationText = ((statisticsData.Count > 0) ? statisticsData.Values.Sum() : 0);
		SetTotalDurationText(totalDurationText);
		_rightBorder.IsEnabled = model.EndDate <= DateTime.Today;
		_rightBorder.Opacity = (_rightBorder.IsEnabled ? 1.0 : 0.3);
		_lineView.SetData(model, statisticsData);
	}

	private void OnMouseMove(object sender, MouseEventArgs e)
	{
		if (_lineView.IsMouseOver)
		{
			var (num, timerDayItemViewModel) = _lineView.GetHoverItem(e);
			if (timerDayItemViewModel != null && timerDayItemViewModel.Minutes > 0)
			{
				if (Math.Abs(_offset - num) > 1.0)
				{
					_offset = num;
					DelayShowItemPopup(num, timerDayItemViewModel);
				}
				return;
			}
		}
		_offset = -1.0;
		HideItemPopup();
	}

	private async void DelayShowItemPopup(double offset, TimerDayItemViewModel item)
	{
		await Task.Delay(200);
		if (Math.Abs(_offset - offset) < 0.1)
		{
			ShowItemPopup(offset, item);
		}
	}

	private void HideItemPopup()
	{
		if (_popBorder != null)
		{
			_popBorder.BeginAnimation(UIElement.OpacityProperty, null);
			_popBorder.Opacity = 0.0;
		}
	}

	private void InitItemPop()
	{
		if (_popBorder == null)
		{
			_popBorder = new Border
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Opacity = 0.0,
				MinWidth = 40.0,
				IsHitTestVisible = false
			};
			_popBorder.SetValue(Grid.RowSpanProperty, 3);
			_popBorder.SetValue(Panel.ZIndexProperty, 1000);
			_popText = new TextBlock
			{
				FontSize = 13.0,
				Margin = new Thickness(10.0, 0.0, 10.0, 0.0)
			};
			_popText.SetResourceReference(TextBlock.ForegroundProperty, "PrimaryColor");
			ContentControl contentControl = new ContentControl
			{
				Margin = new Thickness(0.0, 4.0, 0.0, 4.0),
				MinWidth = 50.0
			};
			contentControl.SetResourceReference(FrameworkElement.StyleProperty, "PopupContentStyle");
			contentControl.Content = _popText;
			_popBorder.Child = contentControl;
			base.Children.Add(_popBorder);
		}
	}

	private void ShowItemPopup(double offset, TimerDayItemViewModel item)
	{
		InitItemPop();
		_popText.Text = item.GetDateText() + ", " + Utils.GetShortDurationString(item.Minutes * 60);
		double width = Utils.MeasureString(_popText.Text, new Typeface(Utils.GetDefaultFontFamily(), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), 13.0).Width;
		double left = Math.Max(10.0, 8.0 + offset - width / 2.0);
		_popBorder.Margin = new Thickness(left, 152 - item.ColumnHeight, 0.0, 0.0);
		_popBorder.BeginAnimation(UIElement.OpacityProperty, AnimationUtils.GetDoubleAnimation(0.0, 1.0, 150));
	}
}
