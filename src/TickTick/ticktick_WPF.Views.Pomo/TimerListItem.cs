using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Emoji.Wpf;
using TickTickDao;
using TickTickModels;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Pomo.FocusStatistics;
using ticktick_WPF.Views.Pomo.MainFocus;

namespace ticktick_WPF.Views.Pomo;

public class TimerListItem : Grid
{
	private Border backBorder;

	private DockPanel dockPanel;

	private Point _mouseDownPoint;

	private Line line;

	public bool InPopup;

	private Image _iconImage = new Image();

	private readonly Border _optionIcon = new Border();

	private readonly TextBlock _durationText = new TextBlock();

	private readonly EmjTextBlock _titleText = new EmjTextBlock
	{
		TextWrapping = TextWrapping.Wrap,
		ClipToBounds = true,
		Height = 20.0,
		TextTrimming = TextTrimming.CharacterEllipsis
	};

	private PomoProgressBar _focusProgress;

	public TimerListItem()
	{
		base.Height = 54.0;
		base.Background = Brushes.Transparent;
		backBorder = new Border
		{
			CornerRadius = new CornerRadius(10.0),
			Margin = new Thickness(10.0, 0.0, 10.0, 1.0)
		};
		base.Children.Add(backBorder);
		dockPanel = new DockPanel();
		base.Children.Add(dockPanel);
		InitIcon();
		InitOptionIcon();
		InitDurationText();
		InitTitleText();
		base.DataContextChanged += OnDataBind;
		base.MouseEnter += OnMouseEnter;
		base.MouseLeave += OnMouseLeave;
		base.MouseMove += OnMouseMove;
		base.PreviewMouseLeftButtonDown += OnMouseDown;
		base.MouseRightButtonUp += OnRightClick;
		base.MouseLeftButtonUp += OnItemClick;
		base.Unloaded += delegate
		{
			TickFocusManager.CurrentSecondChanged -= OnFocusSecondChanged;
		};
	}

	private void InitIcon()
	{
		_iconImage.Width = 30.0;
		_iconImage.Height = 30.0;
		_iconImage.SetValue(DockPanel.DockProperty, Dock.Left);
		_iconImage.Margin = new Thickness(22.0, 0.0, 0.0, 0.0);
		dockPanel.Children.Add(_iconImage);
		line = new Line
		{
			X1 = 0.0,
			X2 = 1.0,
			Stretch = Stretch.Uniform,
			StrokeThickness = 1.0,
			Margin = new Thickness(0.0, 0.0, 22.0, 0.0)
		};
		line.SetResourceReference(Shape.StrokeProperty, "BaseColorOpacity5");
		line.SetValue(DockPanel.DockProperty, Dock.Bottom);
		dockPanel.Children.Add(line);
	}

	private void InitOptionIcon()
	{
		_optionIcon.SetValue(DockPanel.DockProperty, Dock.Right);
		_optionIcon.Width = 22.0;
		_optionIcon.Height = 22.0;
		_optionIcon.Background = Brushes.Transparent;
		_optionIcon.Margin = new Thickness(0.0, 0.0, 22.0, 0.0);
		_optionIcon.Cursor = Cursors.Hand;
		dockPanel.Children.Add(_optionIcon);
		_optionIcon.MouseLeftButtonUp += OnOptionIconClick;
	}

	private void InitDurationText()
	{
		_durationText.SetValue(DockPanel.DockProperty, Dock.Right);
		_durationText.MinWidth = 30.0;
		_durationText.SetResourceReference(FrameworkElement.StyleProperty, "Body06");
		_durationText.TextAlignment = TextAlignment.Right;
		_durationText.Margin = new Thickness(0.0, 0.0, 6.0, 0.0);
		dockPanel.Children.Add(_durationText);
		_durationText.SetBinding(TextBlock.TextProperty, "DurationText");
	}

	private void InitTitleText()
	{
		_titleText.SetResourceReference(FrameworkElement.StyleProperty, "Body06");
		_titleText.Margin = new Thickness(12.0, 0.0, 8.0, 0.0);
		_titleText.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity100_80");
		_titleText.FontSize = 14.0;
		dockPanel.Children.Add(_titleText);
		_titleText.SetBinding(EmjTextBlock.TextProperty, "Title");
	}

	private void OnDataBind(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (e.OldValue is FocusTimerViewModel source)
		{
			PropertyChangedEventManager.RemoveHandler(source, VmPropertyChanged, "");
		}
		if (e.NewValue is FocusTimerViewModel focusTimerViewModel)
		{
			_iconImage.Source = HabitService.GetIcon(focusTimerViewModel.Icon, focusTimerViewModel.Color);
			if (focusTimerViewModel.Archive)
			{
				TickFocusManager.CurrentSecondChanged -= OnFocusSecondChanged;
				_optionIcon.Child = null;
				_optionIcon.Visibility = Visibility.Collapsed;
				_focusProgress = null;
			}
			else
			{
				_optionIcon.Visibility = Visibility.Visible;
				SetOptionChild(focusTimerViewModel.Focusing);
			}
			SetItemBackground(focusTimerViewModel.Selected);
			PropertyChangedEventManager.AddHandler(focusTimerViewModel, VmPropertyChanged, "");
			base.Opacity = ((!focusTimerViewModel.Dragging || InPopup) ? 1 : 0);
		}
	}

	private void VmPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (!(base.DataContext is FocusTimerViewModel focusTimerViewModel))
		{
			return;
		}
		switch (e.PropertyName)
		{
		case "Dragging":
			base.Opacity = ((!focusTimerViewModel.Dragging || InPopup) ? 1 : 0);
			break;
		case "Focusing":
			if (!focusTimerViewModel.Archive)
			{
				SetOptionChild(focusTimerViewModel.Focusing);
			}
			break;
		case "Selected":
			SetItemBackground(focusTimerViewModel.Selected);
			break;
		}
	}

	private void SetItemBackground(bool selected)
	{
		if (!selected)
		{
			if (base.IsMouseOver)
			{
				backBorder.SetResourceReference(Panel.BackgroundProperty, "BaseColorOpacity3");
			}
			else
			{
				backBorder.Background = Brushes.Transparent;
			}
		}
		else
		{
			backBorder.SetResourceReference(Panel.BackgroundProperty, "ItemSelectedColor");
		}
		line.Visibility = ((selected || base.IsMouseOver) ? Visibility.Hidden : Visibility.Visible);
	}

	private void SetOptionChild(bool focusing)
	{
		if (focusing)
		{
			_focusProgress = new PomoProgressBar
			{
				TimingSize = 2,
				Height = 18.0,
				Width = 18.0,
				Angle = 0.0,
				StrokeThickness = 3.0,
				IsStrokeMode = TickFocusManager.IsPomo,
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Center
			};
			_focusProgress.SetResourceReference(PomoProgressBar.UnderColorProperty, "BaseColorOpacity10");
			_focusProgress.SetResourceReference(PomoProgressBar.TopColorProperty, "PrimaryColor");
			TickFocusManager.CurrentSecondChanged -= OnFocusSecondChanged;
			TickFocusManager.CurrentSecondChanged += OnFocusSecondChanged;
			_optionIcon.Child = _focusProgress;
			_focusProgress.Angle = TickFocusManager.Config.GetDisplayAngle(next: false);
			if (!_focusProgress.IsStrokeMode && TickFocusManager.Config.CurrentSeconds >= 30.0)
			{
				_focusProgress.HideLeftMask();
			}
		}
		else
		{
			Path path = new Path
			{
				Data = Utils.GetIcon("IcPomoStart"),
				Width = 18.0,
				Height = 18.0
			};
			path.SetResourceReference(Shape.FillProperty, "PrimaryColor");
			path.SetResourceReference(FrameworkElement.StyleProperty, "Path01");
			_optionIcon.Child = path;
			TickFocusManager.CurrentSecondChanged -= OnFocusSecondChanged;
			_focusProgress = null;
		}
	}

	private void OnFocusSecondChanged()
	{
		base.Dispatcher.Invoke(delegate
		{
			PomoProgressBar focusProgress = _focusProgress;
			if (focusProgress != null)
			{
				focusProgress.Angle = TickFocusManager.Config.GetDisplayAngle(next: false);
				if (!focusProgress.IsStrokeMode && TickFocusManager.Config.CurrentSeconds >= 30.0)
				{
					focusProgress.HideLeftMask();
				}
			}
		});
	}

	private void OnRightClick(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext is FocusTimerViewModel focusTimerViewModel)
		{
			EscPopup popup = new EscPopup
			{
				PlacementTarget = (sender as UIElement),
				StaysOpen = false,
				Placement = PlacementMode.MousePoint
			};
			List<CustomMenuItemViewModel> list = new List<CustomMenuItemViewModel>
			{
				new CustomMenuItemViewModel("Archive", Utils.GetString(focusTimerViewModel.Archive ? "Restore" : "Archive"), Utils.GetGeometry(focusTimerViewModel.Archive ? "Ic.Habit.Unarchive" : "Ic.Habit.Archive")),
				new CustomMenuItemViewModel("Delete", Utils.GetString("Delete"), Utils.GetGeometry("Ic.ProjectList.Delete"))
				{
					IsDelete = true
				}
			};
			if (!focusTimerViewModel.Archive)
			{
				list.Insert(0, new CustomMenuItemViewModel("Edit", Utils.GetString("Edit"), Utils.GetGeometry("Ic.TaskOM.Edit")));
				list.Insert(1, new CustomMenuItemViewModel("AddRecord", Utils.GetString("AddRecord"), Utils.GetGeometry("Ic.ProjectList.Add"))
				{
					ImageMargin = new Thickness(11.0, 0.0, 2.0, 0.0)
				});
			}
			CustomMenuList customMenuList = new CustomMenuList(list, popup);
			customMenuList.Operated += OnActionSelected;
			customMenuList.Show();
		}
	}

	private async void OnActionSelected(object sender, object e)
	{
		object dataContext = base.DataContext;
		if (!(dataContext is FocusTimerViewModel model))
		{
			return;
		}
		switch (e.ToString())
		{
		case "Edit":
		{
			TimerModel timerModel = await TimerDao.GetTimerById(model.Id);
			if (timerModel != null)
			{
				AddTimerWindow addTimerWindow = new AddTimerWindow(timerModel);
				addTimerWindow.Owner = Window.GetWindow(this);
				addTimerWindow.ShowDialog();
			}
			break;
		}
		case "AddRecord":
		{
			TimerModel timer = await TimerDao.GetTimerById(model.Id);
			if (timer != null)
			{
				string text = await TimerService.GetObjTitle(timer.ObjId, timer.ObjType);
				if (text != null && timer.Name != text)
				{
					timer.Name = text;
					await TimerService.UpdateName(timer.Id, text);
				}
				new AddFocusRecordWindow(timer).ShowDialog();
				UserActCollectUtils.AddClickEvent("timer", "timer_detail", "add_record");
			}
			break;
		}
		case "Archive":
			if (model.Archive)
			{
				if (!(await ProChecker.CheckTimerLimit(Window.GetWindow(this))))
				{
					break;
				}
			}
			else if (new CustomerDialog("", Utils.GetString("ArchiveTimerMessage"), Utils.GetString("Archive"), Utils.GetString("Cancel"), Window.GetWindow(this)).ShowDialog() != true)
			{
				break;
			}
			TimerService.ChangeArchiveStatus(model.Id);
			break;
		case "Delete":
			if (new CustomerDialog("", Utils.GetString("DeleteTimerMessage"), Utils.GetString("Delete"), Utils.GetString("Cancel"), Window.GetWindow(this)).ShowDialog() == true)
			{
				TimerService.DeleteTimer(model.Id);
			}
			break;
		}
	}

	private void OnMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext is FocusTimerViewModel { Archive: false })
		{
			_mouseDownPoint = e.GetPosition(this);
		}
	}

	private void OnMouseMove(object sender, MouseEventArgs e)
	{
		if (e.LeftButton == MouseButtonState.Pressed && _mouseDownPoint != default(Point))
		{
			if ((e.GetPosition(this) - _mouseDownPoint).Length > 4.0)
			{
				_mouseDownPoint = default(Point);
				Utils.FindParent<FocusTimerListView>(this)?.TryDragItem(base.DataContext as FocusTimerViewModel, e);
			}
		}
		else
		{
			_mouseDownPoint = default(Point);
		}
	}

	private void OnMouseLeave(object sender, MouseEventArgs e)
	{
		if (base.DataContext is FocusTimerViewModel focusTimerViewModel)
		{
			SetItemBackground(focusTimerViewModel.Selected);
		}
	}

	private void OnMouseEnter(object sender, MouseEventArgs e)
	{
		if (base.DataContext is FocusTimerViewModel { Selected: false } focusTimerViewModel)
		{
			SetItemBackground(focusTimerViewModel.Selected);
		}
	}

	private async void OnOptionIconClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		if (base.DataContext is FocusTimerViewModel focusTimerViewModel && await TickFocusManager.TryStartFocusTimer(focusTimerViewModel.Id, Window.GetWindow(this)))
		{
			Utils.FindParent<MainFocusControl>(this)?.ShowClockPanel();
			UserActCollectUtils.AddClickEvent("focus", "start_from_tab", "timer_list");
			UserActCollectUtils.AddClickEvent("focus", "start_from", "tab");
		}
	}

	private void OnItemClick(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext is FocusTimerViewModel model)
		{
			Utils.FindParent<FocusTimerListView>(this)?.OnItemClick(model);
		}
	}
}
