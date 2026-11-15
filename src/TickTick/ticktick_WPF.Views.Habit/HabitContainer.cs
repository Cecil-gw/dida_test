using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Detail;

namespace ticktick_WPF.Views.Habit;

public class HabitContainer : Grid
{
	private HabitDetailControl _habitDetail;

	private HabitListControl _habitList;

	private bool _hideDetail;

	private DetailHintPanel _hint;

	private bool _isDrawerStyle;

	private string _selectedHabitId { get; set; }

	public ColumnDefinition Column1 { get; set; }

	public ColumnDefinition Column2 { get; set; }

	public HabitContainer(bool isMainHabit)
	{
		Column1 = new ColumnDefinition
		{
			MinWidth = 500.0,
			Width = new GridLength(1.0, GridUnitType.Star)
		};
		base.ColumnDefinitions.Add(Column1);
		Column2 = new ColumnDefinition
		{
			MinWidth = 344.0,
			Width = new GridLength(LocalSettings.Settings.ExtraSettings.HabitDivide, GridUnitType.Star)
		};
		base.ColumnDefinitions.Add(Column2);
		InitHabitControl(isMainHabit);
		base.Unloaded += OnUnloaded;
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		DataChangedNotifier.HabitsChanged -= OnHabitChanged;
		if (_habitList != null)
		{
			_habitList.HabitSelected -= OnHabitSelected;
			_habitList.ItemsCountChanged -= OnHabitItemsCountChanged;
		}
		if (_habitDetail != null)
		{
			_habitDetail.HideDetail += ClearSelect;
		}
		base.SizeChanged -= OnSizeChanged;
		base.MouseLeftButtonUp -= OnMouseUp;
		base.Children.Clear();
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs e)
	{
		SetWindowSize(e.PreviousSize.Width, e.NewSize.Width);
	}

	private void SetWindowSize(double preview, double newWidth)
	{
		if (newWidth >= 860.0)
		{
			_isDrawerStyle = false;
			if (_hideDetail)
			{
				Column2.MinWidth = 344.0;
				_habitDetail.SetValue(Grid.ColumnProperty, 1);
				_habitDetail.SetValue(Panel.ZIndexProperty, 0);
				_habitDetail.BackGrid.Visibility = Visibility.Collapsed;
				TranslateTransform obj = (TranslateTransform)_habitDetail.RenderTransform;
				obj.BeginAnimation(TranslateTransform.XProperty, null);
				obj.X = 0.0;
				_habitDetail.Width = double.NaN;
				_habitDetail.HorizontalAlignment = HorizontalAlignment.Stretch;
				_habitDetail.Background = Brushes.Transparent;
				_habitDetail.Effect = null;
				double value = 0.5;
				Column1.Width = new GridLength(1.0, GridUnitType.Star);
				Column2.Width = new GridLength(value, GridUnitType.Star);
			}
			_hideDetail = false;
			_habitDetail.Background = Brushes.Transparent;
		}
		else if (newWidth < 860.0)
		{
			_isDrawerStyle = true;
			if (!_hideDetail)
			{
				Column2.MinWidth = 0.0;
				Column2.Width = new GridLength(0.0);
				_habitDetail.SetValue(Grid.ColumnProperty, 0);
				_habitDetail.SetValue(Panel.ZIndexProperty, 10);
				_habitDetail.BackGrid.Visibility = Visibility.Visible;
				_habitDetail.SetResourceReference(Panel.BackgroundProperty, "FoldAreaBackground");
				TranslateTransform obj2 = (TranslateTransform)_habitDetail.RenderTransform;
				obj2.BeginAnimation(TranslateTransform.XProperty, null);
				obj2.X = 360.0;
				_habitDetail.Width = 360.0;
				_habitDetail.HorizontalAlignment = HorizontalAlignment.Right;
			}
			_hideDetail = true;
			TryHideDetail();
		}
	}

	private DoubleAnimation GetAnimation(double from, double to)
	{
		DoubleAnimation doubleAnimation = new DoubleAnimation
		{
			Duration = new Duration(TimeSpan.FromMilliseconds(180.0)),
			From = from,
			To = to
		};
		if (to != 0.0)
		{
			doubleAnimation.Completed += delegate
			{
				_habitDetail.Effect = null;
			};
		}
		return doubleAnimation;
	}

	private void InitHabitControl(bool isMainHabit)
	{
		base.SizeChanged += OnSizeChanged;
		base.MouseLeftButtonUp += OnMouseUp;
		DataChangedNotifier.HabitsChanged += OnHabitChanged;
		_habitList = new HabitListControl(isMainHabit);
		_habitList.HabitSelected += OnHabitSelected;
		_habitList.ItemsCountChanged += OnHabitItemsCountChanged;
		SetResourceReference(Panel.BackgroundProperty, "CardBackColor");
		base.Children.Add(_habitList);
		_habitDetail = new HabitDetailControl
		{
			IsIndependent = true,
			OuterContainer = 
			{
				Margin = new Thickness(0.0, 30.0, 0.0, 0.0)
			}
		};
		base.Children.Add(_habitDetail);
		_habitDetail.SetValue(Grid.ColumnProperty, 1);
		_habitDetail.Visibility = Visibility.Collapsed;
		_habitDetail.RenderTransform = new TranslateTransform
		{
			X = 0.0
		};
		_habitDetail.HideDetail += ClearSelect;
		Column1.Width = new GridLength(1.0, GridUnitType.Star);
		Column2.Width = new GridLength(LocalSettings.Settings.ExtraSettings.HabitDivide, GridUnitType.Star);
		Column2.MinWidth = 344.0;
		Border border = new Border
		{
			Width = 5.0,
			BorderThickness = new Thickness(1.0, 0.0, 0.0, 0.0),
			HorizontalAlignment = HorizontalAlignment.Left
		};
		border.SetResourceReference(Border.BorderBrushProperty, "BaseColorOpacity5");
		base.Children.Add(border);
		border.SetValue(Grid.ColumnProperty, 1);
		GridSplitter gridSplitter = new GridSplitter
		{
			Width = 5.0,
			Background = Brushes.Transparent,
			IsTabStop = false,
			HorizontalAlignment = HorizontalAlignment.Left,
			FocusVisualStyle = null
		};
		gridSplitter.DragStarted += OnSplitterDragStarted;
		gridSplitter.DragCompleted += OnSplitterDragCompleted;
		base.Children.Add(gridSplitter);
		gridSplitter.SetValue(Grid.ColumnProperty, 1);
		gridSplitter.SetValue(Panel.ZIndexProperty, 100);
		_hint = new DetailHintPanel(isTask: false);
		base.Children.Add(_hint);
		_hint.SetValue(Grid.ColumnProperty, 1);
		SetWindowSize(810.0, base.Width);
	}

	private void OnSplitterDragStarted(object sender, DragStartedEventArgs e)
	{
		double value = Column2.ActualWidth / Column1.ActualWidth;
		Column1.Width = new GridLength(1.0, GridUnitType.Star);
		Column2.Width = new GridLength(value, GridUnitType.Star);
	}

	private void OnSplitterDragCompleted(object sender, DragCompletedEventArgs e)
	{
		LocalSettings.Settings.ExtraSettings.HabitDivide = Column2.ActualWidth / Column1.ActualWidth;
		Column1.Width = new GridLength(1.0, GridUnitType.Star);
		Column2.Width = new GridLength(LocalSettings.Settings.ExtraSettings.HabitDivide, GridUnitType.Star);
	}

	private void ClearSelect(object sender, EventArgs e)
	{
		NavigateItem(null);
	}

	private void OnHabitItemsCountChanged(object sender, int e)
	{
		_hint.Visibility = ((e == 0 || _habitDetail.Visibility == Visibility.Visible) ? Visibility.Collapsed : Visibility.Visible);
	}

	private async void OnHabitChanged(object sender, EventArgs e)
	{
		base.Dispatcher.Invoke((Func<Task>)async delegate
		{
			if (await HabitService.IsHabitDeleted(_selectedHabitId))
			{
				TryHideDetail();
				_selectedHabitId = null;
				if (_habitDetail != null)
				{
					_habitDetail.Visibility = Visibility.Collapsed;
				}
				_hint.Visibility = Visibility.Visible;
			}
		});
	}

	private void OnMouseUp(object sender, MouseButtonEventArgs e)
	{
		if ((_habitDetail == null || (!_habitDetail.IsMouseOver && !_habitDetail.CompletedCyclesControl.CompletedCyclesDetailPopup.IsOpen && !_habitDetail.HabitMonthControl.ManuallyCheckInPopup.IsOpen && !_habitDetail.MorePopup.IsOpen)) && !_habitList.IsMouseOver)
		{
			TryHideDetail();
		}
	}

	private void OnHabitSelected(object sender, string habitId)
	{
		UserActCollectUtils.AddClickEvent("habit", "habit_detail_location", "show_in_habit");
		if (_hideDetail)
		{
			_habitDetail.Effect = new DropShadowEffect
			{
				BlurRadius = 10.0,
				Direction = 180.0,
				Opacity = 0.04
			};
			TranslateTransform translateTransform = (TranslateTransform)_habitDetail.RenderTransform;
			translateTransform.BeginAnimation(TranslateTransform.XProperty, GetAnimation(translateTransform.X, 0.0));
		}
		_selectedHabitId = habitId;
		if (string.IsNullOrEmpty(habitId))
		{
			if (_hideDetail)
			{
				TryHideDetail();
			}
			else
			{
				_habitDetail.Visibility = Visibility.Collapsed;
				_hint.Visibility = Visibility.Visible;
			}
		}
		else
		{
			UserActCollectUtils.AddClickEvent("habit", "view_action", "habit_detail");
			_habitDetail.Visibility = Visibility.Visible;
			_hint.Visibility = Visibility.Collapsed;
			_habitDetail.Load(habitId, DateTime.Today, pullRemote: true, force: true, !_isDrawerStyle);
			_habitDetail.ScrollToTop();
		}
		if (sender == null)
		{
			_habitList.TrySelectItem(habitId);
		}
	}

	public void TryHideDetail()
	{
		if (_hideDetail)
		{
			TranslateTransform translateTransform = (TranslateTransform)_habitDetail.RenderTransform;
			translateTransform.BeginAnimation(TranslateTransform.XProperty, GetAnimation(translateTransform.X, 360.0));
		}
	}

	public void NavigateItem(string habitId)
	{
		OnHabitSelected(null, habitId);
	}

	public void CheckInHabit(string habitId)
	{
		_habitList.CheckInHabit(habitId);
	}

	public void HideDetail()
	{
		_habitDetail.Visibility = Visibility.Collapsed;
		_hint.Visibility = Visibility.Visible;
	}

	public void LoadHabits()
	{
		_habitList.GetHabitModels();
	}

	public string GetSelectedHabitId()
	{
		return _habitDetail.GetHabitId();
	}

	public void OnWindowMouseDown(MouseButtonEventArgs e)
	{
		if (_habitDetail != null && !_habitDetail.IsMouseOver && !_habitDetail.MorePopup.IsOpen && !_habitDetail.CompletedCyclesControl.CompletedCyclesDetailPopup.IsOpen && !_habitDetail.HabitMonthControl.ManuallyCheckInPopup.IsOpen && !_habitDetail.MorePopup.IsOpen && !(e.GetPosition(this).X > base.Width - _habitDetail.ActualWidth) && !_habitList.HabitItems.IsMouseOver)
		{
			TryHideDetail();
		}
	}

	public async void Reload()
	{
		if (_habitDetail.Visibility == Visibility.Visible)
		{
			_habitDetail.Reload();
		}
		await _habitList.GetHabitModels();
		_habitList.TrySelectItem(_selectedHabitId);
	}
}
