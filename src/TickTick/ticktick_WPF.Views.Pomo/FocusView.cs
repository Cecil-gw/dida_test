using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using TickTickDao;
using TickTickUtils;
using ticktick_WPF.Resource;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.Views.Pomo.FocusDetail;
using ticktick_WPF.Views.Pomo.MainFocus;

namespace ticktick_WPF.Views.Pomo;

public class FocusView : Grid
{
	private ColumnDefinition _column1;

	private ColumnDefinition _column2;

	public MainFocusControl _focusControl;

	private FocusDetailControl _focusDetail;

	private bool _animationPlaying;

	private bool _folded;

	private bool _initShowClockPanel;

	private static double _column2Width => LocalSettings.Settings.PomoLocalSetting.DetailWidth;

	public FocusView(bool initShowClockPanel = false)
	{
		_initShowClockPanel = initShowClockPanel;
		base.Resources.MergedDictionaries.Add(new ResourceDictionary
		{
			Source = new Uri("Views/Pomo/FocusResources.xaml", UriKind.Relative)
		});
		_column1 = new ColumnDefinition
		{
			MinWidth = 400.0,
			Width = new GridLength(1.0, GridUnitType.Star)
		};
		_column2 = new ColumnDefinition
		{
			MinWidth = 399.0,
			Width = new GridLength(_column2Width, GridUnitType.Star)
		};
		base.ColumnDefinitions.Add(_column1);
		base.ColumnDefinitions.Add(_column2);
		base.Loaded += OnLoaded;
		base.Unloaded += OnUnloaded;
		SetResourceReference(Panel.BackgroundProperty, "CardBackBottomColor");
		Rectangle rectangle = new Rectangle();
		rectangle.SetResourceReference(Shape.FillProperty, "CardBackColor");
		rectangle.SetValue(Grid.ColumnSpanProperty, 2);
		base.Children.Add(rectangle);
		GridSplitter gridSplitter = new GridSplitter
		{
			Width = 5.0,
			FocusVisualStyle = null,
			Background = Brushes.Transparent,
			IsTabStop = false,
			HorizontalAlignment = HorizontalAlignment.Left
		};
		gridSplitter.SetValue(Grid.ColumnProperty, 1);
		gridSplitter.SetValue(Panel.ZIndexProperty, 50);
		gridSplitter.DragCompleted += OnDragSplitCompleted;
		base.Children.Add(gridSplitter);
		Border border = new Border
		{
			Width = 5.0,
			FocusVisualStyle = null,
			Background = Brushes.Transparent,
			BorderThickness = new Thickness(1.0, 0.0, 0.0, 0.0),
			HorizontalAlignment = HorizontalAlignment.Left
		};
		border.SetResourceReference(Border.BorderBrushProperty, "BaseColorOpacity5");
		border.SetValue(Grid.ColumnProperty, 1);
		base.Children.Add(border);
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (e.NewSize.Width >= 800.0 && _folded)
		{
			_folded = false;
			DoubleAnimation doubleAnimation = AnimationUtils.GetDoubleAnimation(0.0, 1000.0, 240);
			_focusDetail.SetValue(Grid.ColumnProperty, 1);
			_focusDetail.SetFoldStyle(fold: false);
			doubleAnimation.Completed += delegate
			{
				_animationPlaying = false;
				_column2.BeginAnimation(ColumnDefinition.MaxWidthProperty, null);
				_column2.MaxWidth = double.PositiveInfinity;
				_column2.MinWidth = 399.0;
				LocalSettings.Settings.PomoLocalSetting.DetailWidth = 1.0;
				_column2.Width = new GridLength(1.0, GridUnitType.Star);
			};
			_animationPlaying = true;
			_column2.BeginAnimation(ColumnDefinition.MaxWidthProperty, doubleAnimation);
		}
		if (e.NewSize.Width < 800.0 && !_folded)
		{
			_folded = true;
			DoubleAnimation doubleAnimation2 = AnimationUtils.GetDoubleAnimation(_column2.ActualWidth, 0.0, 240);
			_column2.MinWidth = 0.0;
			doubleAnimation2.Completed += delegate
			{
				_focusDetail.SetFoldStyle(fold: true);
				_focusDetail.SetValue(Grid.ColumnProperty, 0);
			};
			_column2.BeginAnimation(ColumnDefinition.MaxWidthProperty, doubleAnimation2);
		}
		if (!_animationPlaying && e.NewSize.Width >= 800.0)
		{
			_column1.Width = new GridLength(1.0, GridUnitType.Star);
			LocalSettings.Settings.PomoLocalSetting.DetailWidth = (base.ActualWidth - _column1.ActualWidth) / _column1.ActualWidth;
			_column2.Width = new GridLength(_column2Width, GridUnitType.Star);
		}
	}

	private async void OnDragSplitCompleted(object sender, DragCompletedEventArgs e)
	{
		await Task.Delay(10);
		_column1.Width = new GridLength(1.0, GridUnitType.Star);
		LocalSettings.Settings.PomoLocalSetting.DetailWidth = (base.ActualWidth - _column1.ActualWidth) / _column1.ActualWidth;
		_column2.Width = new GridLength(_column2Width, GridUnitType.Star);
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		base.Children.Remove(_focusControl);
		base.Children.Remove(_focusDetail);
	}

	private async void OnLoaded(object sender, RoutedEventArgs e)
	{
		base.Loaded -= OnLoaded;
		_focusControl = new MainFocusControl(await TimerDao.GetDisplayTimersAsync(), _initShowClockPanel)
		{
			MinWidth = 400.0
		};
		_focusControl.SetValue(Grid.ColumnProperty, 0);
		base.Children.Add(_focusControl);
		TimerSyncService.PullRemoteTimers();
		_focusDetail = new FocusDetailControl
		{
			MinWidth = 399.0
		};
		_focusDetail.SetValue(Grid.ColumnProperty, 1);
		base.Children.Add(_focusDetail);
		await Task.Delay(20);
		if (base.ActualWidth < 800.0)
		{
			_folded = true;
			_column2.MaxWidth = 0.0;
			_column2.MinWidth = 0.0;
			_focusDetail.SetFoldStyle(fold: true);
			_focusDetail.SetValue(Grid.ColumnProperty, 0);
		}
		base.SizeChanged += OnSizeChanged;
	}

	public void ReloadStatistics()
	{
		_focusDetail?.ReloadStatistics();
	}

	public void OnTimerSelect(string id)
	{
		_focusDetail?.OnTimerSelect(id);
		if (!string.IsNullOrEmpty(id))
		{
			if (_focusDetail?.RenderTransform is TranslateTransform translateTransform)
			{
				_focusDetail.Visibility = Visibility.Visible;
				DoubleAnimation doubleAnimation = AnimationUtils.GetDoubleAnimation(null, 0.0, 240);
				translateTransform.BeginAnimation(TranslateTransform.XProperty, doubleAnimation);
			}
		}
		else if (_focusDetail?.RenderTransform is TranslateTransform translateTransform2)
		{
			DoubleAnimation doubleAnimation2 = AnimationUtils.GetDoubleAnimation(null, 400.0, 240);
			doubleAnimation2.Completed += delegate
			{
				_focusDetail.Visibility = Visibility.Collapsed;
			};
			translateTransform2.BeginAnimation(TranslateTransform.XProperty, doubleAnimation2);
		}
	}

	public void TryFoldDetail(bool force = false)
	{
		if (_focusDetail != null && _focusControl != null && (force || (!_focusDetail.IsMouseOver && !_focusControl.IsListMouseOver)) && _focusDetail.RenderTransform is TranslateTransform translateTransform)
		{
			DoubleAnimation doubleAnimation = AnimationUtils.GetDoubleAnimation(null, 400.0, 240);
			doubleAnimation.Completed += delegate
			{
				_focusDetail.Visibility = Visibility.Collapsed;
			};
			translateTransform.BeginAnimation(TranslateTransform.XProperty, doubleAnimation);
		}
	}

	public void ClearTimerSelect()
	{
		TryFoldDetail(force: true);
		_focusControl?.ClearSelect();
		_focusDetail?.OnTimerSelect(null);
	}

	public void UpdateTimerDuration(string timerId, long duration)
	{
		_focusControl?.UpdateTimerDuration(timerId, duration);
	}

	public bool GetClockPanelShow()
	{
		return _focusControl?.ClockPanelShow() ?? true;
	}

	public bool ExistTimer()
	{
		return _focusControl?.ExistTimer() ?? true;
	}

	public void OnDropFocus()
	{
		_focusControl?.TryFoldClockPanel();
	}

	public void Reload()
	{
	}

	public bool GetIsShowClockPanel()
	{
		return _focusControl?.IsShowClockPanel ?? false;
	}
}
