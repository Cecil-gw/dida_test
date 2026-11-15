using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using TickTickUtils;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Misc;

public class ScrollableGroupTitle : UserControl, IComponentConnector, IStyleConnector
{
	private List<ScrollableGroupTitleItemViewModel> _models;

	private Timer _timer;

	internal LinearGradientBrush MaskBrush;

	internal ScrollViewer ScrollViewer;

	internal ItemsControl Items;

	internal Border SelectedBorder;

	internal TranslateTransform SelectedTransform;

	private bool _contentLoaded;

	public event EventHandler<string> ItemSelected;

	public ScrollableGroupTitle()
	{
		InitializeComponent();
	}

	public async void InitTitles(List<ScrollableGroupTitleItemViewModel> models)
	{
		_models = models;
		Items.ItemsSource = models;
		ScrollableGroupTitleItemViewModel selected = models.FirstOrDefault((ScrollableGroupTitleItemViewModel m) => m.Selected);
		if (selected == null)
		{
			return;
		}
		await Task.Delay(200);
		for (int num = 0; num < Items.Items.Count; num++)
		{
			if (Items.ItemContainerGenerator.ContainerFromIndex(num) is FrameworkElement frameworkElement && frameworkElement.DataContext == selected)
			{
				ScrollToItem(frameworkElement, anim: false);
			}
		}
	}

	public void InitCalendarProTitles(string selected = null, bool addTimer = true)
	{
		List<ScrollableGroupTitleItemViewModel> models = new List<ScrollableGroupTitleItemViewModel>
		{
			new ScrollableGroupTitleItemViewModel(Utils.GetString("YearView1"))
			{
				Value = "Year",
				Selected = (string.IsNullOrEmpty(selected) || selected == "Year")
			},
			new ScrollableGroupTitleItemViewModel(Utils.GetString("MonthView1"))
			{
				Value = "Month",
				Selected = (string.IsNullOrEmpty(selected) || selected == "Month")
			},
			new ScrollableGroupTitleItemViewModel(Utils.GetString("WeekView1"))
			{
				Value = "Week",
				Selected = (selected == "Week")
			},
			new ScrollableGroupTitleItemViewModel(Utils.GetString("DayView1"))
			{
				Value = "Day",
				Selected = (selected == "Day")
			},
			new ScrollableGroupTitleItemViewModel(Utils.GetString("AgendaView1"))
			{
				Value = "Agenda",
				Selected = (selected == "Agenda")
			},
			new ScrollableGroupTitleItemViewModel(Utils.GetString("MultiDayView1"))
			{
				Value = "MultiDays",
				Selected = (selected == "MultiDays")
			},
			new ScrollableGroupTitleItemViewModel(Utils.GetString("MultiWeekView1"))
			{
				Value = "MultiWeeks",
				Selected = (selected == "MultiWeeks")
			}
		};
		InitTitles(models);
		if (addTimer)
		{
			InitTimer();
		}
	}

	private void InitTimer()
	{
		Timer timer = (_timer = new Timer(5000.0));
		timer.Elapsed += delegate
		{
			List<ScrollableGroupTitleItemViewModel> models = _models;
			ScrollableGroupTitleItemViewModel scrollableGroupTitleItemViewModel = models?.FirstOrDefault((ScrollableGroupTitleItemViewModel m) => m.Selected);
			if (scrollableGroupTitleItemViewModel != null)
			{
				int num = models.IndexOf(scrollableGroupTitleItemViewModel);
				num = ((num != models.Count - 1) ? (num + 1) : 0);
				ScrollableGroupTitleItemViewModel next = models[num];
				scrollableGroupTitleItemViewModel.Selected = false;
				next.Selected = true;
				base.Dispatcher.Invoke(delegate
				{
					for (int i = 0; i < Items.Items.Count; i++)
					{
						if (Items.ItemContainerGenerator.ContainerFromIndex(i) is FrameworkElement frameworkElement && frameworkElement.DataContext == next)
						{
							this.ItemSelected?.Invoke(frameworkElement, next.Value);
							ScrollToItem(frameworkElement);
						}
					}
				});
			}
		};
		timer.Start();
		base.Unloaded += delegate
		{
			if (_timer != null)
			{
				Timer timer2 = _timer;
				timer2.Stop();
				timer2.Dispose();
			}
			_timer = null;
		};
	}

	private void OnItemClick(object sender, MouseButtonEventArgs e)
	{
		if (!(sender is FrameworkElement { DataContext: ScrollableGroupTitleItemViewModel { Selected: false } dataContext } frameworkElement))
		{
			return;
		}
		foreach (ScrollableGroupTitleItemViewModel model in _models)
		{
			model.Selected = false;
		}
		dataContext.Selected = true;
		_timer?.Stop();
		this.ItemSelected?.Invoke(frameworkElement, dataContext.Value);
		ScrollToItem(frameworkElement);
	}

	public void StopTimer()
	{
		_timer?.Stop();
	}

	private void ScrollToItem(FrameworkElement element, bool anim = true)
	{
		double num = element.TranslatePoint(new Point(0.0, 0.0), Items).X;
		if (num <= 1.0)
		{
			num = 1.0;
		}
		if (ScrollViewer.ScrollableWidth > 0.0)
		{
			double x = num + 40.0 - (base.ActualWidth - element.ActualWidth) / 2.0;
			ScrollTo(x);
		}
		if (anim)
		{
			DoubleAnimation doubleAnimation = AnimationUtils.GetDoubleAnimation(SelectedBorder.Width, element.ActualWidth, 400);
			doubleAnimation.EasingFunction = new CubicEase
			{
				EasingMode = EasingMode.EaseOut
			};
			DoubleAnimation doubleAnimation2 = AnimationUtils.GetDoubleAnimation(SelectedTransform.X, num, 400);
			doubleAnimation2.EasingFunction = new CubicEase
			{
				EasingMode = EasingMode.EaseOut
			};
			SelectedBorder.BeginAnimation(FrameworkElement.WidthProperty, doubleAnimation);
			SelectedTransform.BeginAnimation(TranslateTransform.XProperty, doubleAnimation2);
		}
		else
		{
			SelectedBorder.BeginAnimation(FrameworkElement.WidthProperty, null);
			SelectedTransform.BeginAnimation(TranslateTransform.XProperty, null);
			SelectedBorder.Width = element.ActualWidth;
			SelectedTransform.X = num;
		}
	}

	private async void ScrollTo(double x)
	{
		double scrollableWidth = ScrollViewer.ScrollableWidth;
		int num = 0;
		x = Math.Min(scrollableWidth, Math.Max(num, x));
		double current = ScrollViewer.HorizontalOffset;
		int step = (int)(x - current) / 8;
		ShowSideArrow(x);
		for (int i = 0; i < 8; i++)
		{
			ScrollViewer.ScrollToHorizontalOffset(current + (double)(step * i));
			await Task.Delay(20);
		}
		ScrollViewer.ScrollToHorizontalOffset(x);
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs e)
	{
		ShowSideArrow(ScrollViewer.HorizontalOffset, wait: true);
	}

	private async void ShowSideArrow(double offset, bool wait = false)
	{
		if (wait)
		{
			await Task.Delay(30);
		}
		bool flag = ScrollViewer.ScrollableWidth > 0.0 && offset > 0.0;
		bool flag2 = ScrollViewer.ScrollableWidth > 0.0 && offset < ScrollViewer.ScrollableWidth;
		if (ScrollViewer.ScrollableWidth > 0.0)
		{
			base.HorizontalAlignment = HorizontalAlignment.Stretch;
		}
		else
		{
			ScrollViewer.ScrollToHorizontalOffset(0.0);
			base.HorizontalAlignment = HorizontalAlignment.Center;
		}
		if (ScrollViewer.ActualWidth > 80.0)
		{
			MaskBrush.GradientStops[1].Offset = (flag ? (16.0 / ScrollViewer.ActualWidth) : 0.0);
			MaskBrush.GradientStops[2].Offset = (flag ? (80.0 / ScrollViewer.ActualWidth) : 0.0);
			MaskBrush.GradientStops[3].Offset = (flag2 ? (1.0 - 80.0 / ScrollViewer.ActualWidth) : 1.0);
			MaskBrush.GradientStops[4].Offset = (flag2 ? (1.0 - 16.0 / ScrollViewer.ActualWidth) : 1.0);
		}
	}

	public void Select(string selected)
	{
		ScrollableGroupTitleItemViewModel scrollableGroupTitleItemViewModel = _models.FirstOrDefault((ScrollableGroupTitleItemViewModel c) => c.Value == selected);
		if (scrollableGroupTitleItemViewModel == null)
		{
			return;
		}
		foreach (ScrollableGroupTitleItemViewModel model in _models)
		{
			model.Selected = false;
		}
		scrollableGroupTitleItemViewModel.Selected = true;
		this.ItemSelected?.Invoke(this, scrollableGroupTitleItemViewModel.Value);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/misc/scrollablegrouptitle.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			((ScrollableGroupTitle)target).SizeChanged += OnSizeChanged;
			break;
		case 2:
			MaskBrush = (LinearGradientBrush)target;
			break;
		case 3:
			ScrollViewer = (ScrollViewer)target;
			break;
		case 4:
			Items = (ItemsControl)target;
			Items.SizeChanged += OnSizeChanged;
			break;
		case 6:
			SelectedBorder = (Border)target;
			break;
		case 7:
			SelectedTransform = (TranslateTransform)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IStyleConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 5)
		{
			((Border)target).MouseLeftButtonUp += OnItemClick;
		}
	}
}
