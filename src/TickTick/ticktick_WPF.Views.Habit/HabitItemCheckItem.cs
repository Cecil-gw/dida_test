using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Habit;

public class HabitItemCheckItem : Canvas
{
	private readonly Grid _checkGrid;

	private readonly TextBlock _weekText;

	private readonly TextBlock _dayText;

	private Ellipse _ell;

	private CycleProgressBar _progress;

	private Path _icon;

	private bool _isInChecking;

	private bool _canShowPop = true;

	private readonly HabitCheckItemMode _displayMode;

	public static readonly DependencyProperty MouseHoverProperty = DependencyProperty.Register("MouseHover", typeof(bool), typeof(HabitItemCheckItem), new PropertyMetadata(false, OnHoverChanged));

	private readonly Ellipse _hoverEll;

	public bool MouseHover
	{
		get
		{
			return (bool)GetValue(MouseHoverProperty);
		}
		set
		{
			SetValue(MouseHoverProperty, value);
		}
	}

	private static void OnHoverChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is HabitItemCheckItem habitItemCheckItem)
		{
			habitItemCheckItem.SetTextColor();
		}
	}

	private void SetTextColor()
	{
		if (base.DataContext is HabitDayCheckModel habitDayCheckModel)
		{
			_weekText?.SetResourceReference(TextBlock.ForegroundProperty, (habitDayCheckModel.IsToday || habitDayCheckModel.Selected) ? "PrimaryColor" : (MouseHover ? "BaseColorOpacity100" : "BaseSolidColorOpacity60"));
			_dayText?.SetResourceReference(TextBlock.ForegroundProperty, (habitDayCheckModel.IsToday || habitDayCheckModel.Selected) ? "PrimaryColor" : (MouseHover ? "BaseColorOpacity100" : "BaseSolidColorOpacity60"));
		}
	}

	public HabitItemCheckItem()
		: this(HabitCheckItemMode.Normal)
	{
	}

	public HabitItemCheckItem(HabitCheckItemMode mode)
	{
		_displayMode = mode;
		int num = ((_displayMode == HabitCheckItemMode.Normal) ? 20 : 24);
		base.Width = num;
		base.Height = ((_displayMode == HabitCheckItemMode.DateMode) ? 24 : ((_displayMode == HabitCheckItemMode.FilterMode) ? 60 : 20));
		_checkGrid = new Grid
		{
			Width = num,
			Height = num,
			Cursor = Cursors.Hand
		};
		_checkGrid.MouseRightButtonUp += CheckInRightClick;
		_checkGrid.MouseLeftButtonUp += OnCheckItemClick;
		_checkGrid.MouseEnter += OnMouseEnter;
		_checkGrid.MouseLeave += OnMouseLeave;
		base.Children.Add(_checkGrid);
		_ell = new Ellipse
		{
			Width = num,
			Height = num
		};
		_checkGrid.Children.Add(_ell);
		_ell.SetBinding(Shape.FillProperty, "EllColor");
		_hoverEll = new Ellipse
		{
			Width = num,
			Height = num,
			IsHitTestVisible = false,
			Visibility = Visibility.Collapsed,
			Opacity = 0.05
		};
		_checkGrid.Children.Add(_hoverEll);
		_hoverEll.SetResourceReference(Shape.FillProperty, "BaseColorOpacity100");
		_icon = new Path
		{
			Stretch = Stretch.Uniform,
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center,
			IsHitTestVisible = false
		};
		_checkGrid.Children.Add(_icon);
		_icon.SetBinding(Shape.FillProperty, "IconColor");
		if (_displayMode == HabitCheckItemMode.DateMode)
		{
			_icon.Width = 16.0;
			_icon.Height = 16.0;
		}
		else if (_displayMode == HabitCheckItemMode.FilterMode)
		{
			_icon.SetBinding(FrameworkElement.WidthProperty, "IconSize");
			_icon.SetBinding(FrameworkElement.HeightProperty, "IconSize");
		}
		else
		{
			_icon.Width = 14.0;
			_icon.Height = 14.0;
		}
		_icon.SetBinding(Path.DataProperty, "Icon");
		if (_displayMode == HabitCheckItemMode.FilterMode)
		{
			_weekText = new TextBlock
			{
				LineHeight = 14.0,
				TextAlignment = TextAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				FontSize = 10.0,
				Width = 24.0,
				Height = 14.0
			};
			base.Children.Add(_weekText);
			_dayText = new TextBlock
			{
				LineHeight = 15.0,
				TextAlignment = TextAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				FontSize = 12.0,
				Width = 24.0,
				Height = 15.0,
				FontWeight = FontWeights.Bold
			};
			base.Children.Add(_dayText);
			Canvas.SetTop(_dayText, 16.0);
			Canvas.SetTop(_checkGrid, 36.0);
		}
		base.DataContextChanged += OnDataBind;
		base.Loaded += delegate
		{
			if (base.DataContext is HabitDayCheckModel habitDayCheckModel)
			{
				PropertyChangedEventManager.RemoveHandler(habitDayCheckModel, OnPropertyChanged, "");
				PropertyChangedEventManager.AddHandler(habitDayCheckModel, OnPropertyChanged, "");
				SetCheckGridChild(habitDayCheckModel, sameHabit: true);
			}
		};
		base.Unloaded += delegate
		{
			if (base.DataContext is HabitDayCheckModel source)
			{
				PropertyChangedEventManager.RemoveHandler(source, OnPropertyChanged, "");
			}
		};
	}

	private void OnMouseLeave(object sender, MouseEventArgs e)
	{
		_hoverEll.Visibility = Visibility.Collapsed;
		_progress?.SetHoverVisible(visible: false);
	}

	private void OnMouseEnter(object sender, MouseEventArgs e)
	{
		_hoverEll.Visibility = Visibility.Visible;
		_progress?.SetHoverVisible(visible: true);
	}

	private void OnDataBind(object sender, DependencyPropertyChangedEventArgs e)
	{
		string text = "";
		DateTime dateTime = default(DateTime);
		if (e.OldValue is HabitDayCheckModel habitDayCheckModel)
		{
			PropertyChangedEventManager.RemoveHandler(habitDayCheckModel, OnPropertyChanged, "");
			text = habitDayCheckModel.HabitId;
			dateTime = habitDayCheckModel.Date;
		}
		if (base.DataContext is HabitDayCheckModel habitDayCheckModel2)
		{
			PropertyChangedEventManager.AddHandler(habitDayCheckModel2, OnPropertyChanged, "");
			if (_weekText != null)
			{
				_weekText.Text = habitDayCheckModel2.Date.ToString("ddd", App.Ci);
				_weekText.SetResourceReference(TextBlock.ForegroundProperty, habitDayCheckModel2.IsToday ? "PrimaryColor" : "BaseColorOpacity20");
			}
			if (_dayText != null)
			{
				_dayText.Text = habitDayCheckModel2.Date.Day.ToString() ?? "";
				_weekText?.SetResourceReference(TextBlock.ForegroundProperty, habitDayCheckModel2.IsToday ? "PrimaryColor" : "BaseSolidColorOpacity60");
				_dayText.SetResourceReference(TextBlock.ForegroundProperty, habitDayCheckModel2.IsToday ? "PrimaryColor" : "BaseSolidColorOpacity60");
			}
			SetCheckGridChild(habitDayCheckModel2, text == habitDayCheckModel2.HabitId && dateTime == habitDayCheckModel2.Date);
			_hoverEll.SetResourceReference(Shape.FillProperty, habitDayCheckModel2.Completed ? "TextInverseColor" : "BaseColorOpacity100");
			Ellipse hoverEll = _hoverEll;
			CycleProgressBar progress = _progress;
			hoverEll.Opacity = ((progress != null && progress.Visibility == Visibility.Visible) ? 0.0 : (habitDayCheckModel2.Completed ? 0.1 : 0.05));
		}
	}

	private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (!(base.DataContext is HabitDayCheckModel habitDayCheckModel))
		{
			return;
		}
		switch (e.PropertyName)
		{
		case "Status":
		{
			SetCheckGridChild(habitDayCheckModel, sameHabit: true);
			Ellipse hoverEll = _hoverEll;
			CycleProgressBar progress = _progress;
			hoverEll.Opacity = ((progress != null && progress.Visibility == Visibility.Visible) ? 0.0 : (habitDayCheckModel.Completed ? 0.1 : 0.05));
			_hoverEll.SetResourceReference(Shape.FillProperty, habitDayCheckModel.Completed ? "TextInverseColor" : "BaseColorOpacity100");
			break;
		}
		case "Percent":
			SetCheckGridChild(habitDayCheckModel, sameHabit: true);
			break;
		case "Selected":
			if (_dayText != null)
			{
				SetTextColor();
			}
			break;
		}
	}

	private void SetCheckGridChild(HabitDayCheckModel model, bool sameHabit)
	{
		_ = _displayMode;
		_ = 2;
		if (model.NeedCheck || _displayMode != HabitCheckItemMode.FilterMode)
		{
			bool flag = model.Status == 2 || model.Status == 1;
			if (model.IsBooleanHabit || flag || !(model.Date <= DateTime.Today) || (_displayMode != HabitCheckItemMode.FilterMode && !(model.Percent > 0.0)))
			{
				RemoveProgress();
				return;
			}
			if (_progress == null)
			{
				_progress = new CycleProgressBar
				{
					Width = base.Width,
					Height = base.Width,
					Thickness = ((_displayMode == HabitCheckItemMode.Normal) ? 3 : 4)
				};
				_progress.SetResourceReference(CycleProgressBar.UnderColorProperty, "BaseColorOpacity5");
				_progress.SetResourceReference(CycleProgressBar.TopColorProperty, "PrimaryColor");
				_checkGrid.Children.Add(_progress);
			}
			if (sameHabit)
			{
				_progress.TargetPercent = model.Percent;
			}
			else
			{
				_progress.BeginAnimation(CycleProgressBar.PercentProperty, null);
				_progress.Percent = model.Percent;
			}
			_progress.Visibility = Visibility.Visible;
			_progress.SetHoverVisible(base.IsMouseOver);
		}
		else
		{
			RemoveProgress();
		}
		void RemoveProgress()
		{
			if (_progress != null)
			{
				_progress.Visibility = Visibility.Hidden;
			}
		}
	}

	private async void OnCheckItemClick(object sender, MouseButtonEventArgs e)
	{
		if (_displayMode == HabitCheckItemMode.FilterMode)
		{
			return;
		}
		e.Handled = true;
		_checkGrid.IsEnabled = false;
		try
		{
			if (_isInChecking)
			{
				return;
			}
			_isInChecking = true;
			HabitDayCheckModel habitDayCheckModel = null;
			if (base.DataContext is HabitDayCheckModel habitDayCheckModel2)
			{
				habitDayCheckModel = habitDayCheckModel2;
			}
			if (habitDayCheckModel != null && habitDayCheckModel.CheckInModel != null)
			{
				HabitListItem habitListItem = Utils.FindParent<HabitListItem>(this);
				if (habitListItem != null)
				{
					await habitListItem.CheckDayCheck(habitDayCheckModel, this);
				}
			}
		}
		finally
		{
			_checkGrid.IsEnabled = true;
			_isInChecking = false;
		}
	}

	private void CheckInRightClick(object sender, MouseButtonEventArgs e)
	{
		if (_displayMode == HabitCheckItemMode.FilterMode || !_canShowPop)
		{
			return;
		}
		_canShowPop = false;
		e.Handled = true;
		object dataContext = base.DataContext;
		HabitDayCheckModel dayCheck = dataContext as HabitDayCheckModel;
		if (dayCheck == null)
		{
			return;
		}
		if (!dayCheck.Uncompleted && !dayCheck.Completed)
		{
			dayCheck.Selected = true;
			bool num = dayCheck.NeedCheck && (!dayCheck.NeedShowPercent || dayCheck.Percent == 0.0);
			List<OperationItemViewModel> list = new List<OperationItemViewModel>
			{
				new OperationItemViewModel(ActionType.EditHabitLog),
				new OperationItemViewModel(ActionType.UnComplete)
			};
			if (num)
			{
				list.Insert(1, new OperationItemViewModel(ActionType.Skip));
			}
			if (dayCheck.NeedShowPercent && dayCheck.Percent > 0.0)
			{
				list.Add(new OperationItemViewModel(ActionType.Reset));
			}
			OperationDialog operationDialog = new OperationDialog(dayCheck.CheckInModel.HabitId, list);
			operationDialog.SetPlaceTarget(this);
			operationDialog.Operated += async delegate(object obj, KeyValuePair<string, ActionType> arg)
			{
				switch (arg.Value)
				{
				case ActionType.EditHabitLog:
					HabitUtils.ShowHabitRecordWindow(dayCheck.HabitId, dayCheck.HabitName, dayCheck.Date, isEdit: true, !dayCheck.Completed);
					break;
				case ActionType.Skip:
					await HabitService.SkipHabit(dayCheck.HabitId, dayCheck.Date);
					Utils.FindParent<IToastShowWindow>(this)?.TryToastString(null, Utils.GetString("Skipped"));
					break;
				case ActionType.UnComplete:
					dayCheck?.UnCompleteHabit();
					break;
				case ActionType.Reset:
					await HabitService.ResetCheckInHabit(dayCheck.HabitId, dayCheck.Date, dayCheck.HabitType, this);
					break;
				}
				_canShowPop = true;
				dayCheck.Selected = false;
			};
			operationDialog.Closed += delegate
			{
				_canShowPop = true;
				dayCheck.Selected = false;
			};
			operationDialog.Show();
			return;
		}
		dayCheck.Selected = true;
		List<OperationItemViewModel> types = new List<OperationItemViewModel>
		{
			new OperationItemViewModel(ActionType.EditHabitLog),
			new OperationItemViewModel(ActionType.Reset)
		};
		OperationDialog operationDialog2 = new OperationDialog(dayCheck.HabitId, types);
		operationDialog2.SetPlaceTarget(this);
		operationDialog2.Operated += async delegate(object obj, KeyValuePair<string, ActionType> arg)
		{
			_canShowPop = true;
			switch (arg.Value)
			{
			case ActionType.EditHabitLog:
				HabitUtils.ShowHabitRecordWindow(dayCheck.HabitId, dayCheck.HabitName, dayCheck.Date, isEdit: true, dayCheck.Uncompleted);
				break;
			case ActionType.Reset:
				await HabitService.ResetCheckInHabit(dayCheck.HabitId, dayCheck.Date, dayCheck.HabitType, this);
				break;
			}
			dayCheck.Selected = false;
		};
		operationDialog2.Closed += delegate
		{
			_canShowPop = true;
			dayCheck.Selected = false;
		};
		operationDialog2.Show();
	}
}
