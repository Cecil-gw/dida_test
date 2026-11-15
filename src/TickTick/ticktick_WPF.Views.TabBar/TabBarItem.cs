using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using TickTickUtils;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Pomo;

namespace ticktick_WPF.Views.TabBar;

public class TabBarItem : Border
{
	public const int ItemHeight = 48;

	private Path _icon;

	private static TabBarItem _mouseDownItem;

	private bool _selected;

	private static DoubleAnimation _enterAnim;

	private static DoubleAnimation _leaveAnim;

	private Point _mouseDownPoint;

	private EscPopup _popup;

	private Grid _focusPanel;

	private PomoProgressBar _focusProgress;

	private bool _forceHighlight;

	private Path _relaxIcon;

	private TabBarItemViewModel Model => base.DataContext as TabBarItemViewModel;

	public event EventHandler<TabBarItemViewModel> ItemClick;

	static TabBarItem()
	{
		_enterAnim = AnimationUtils.GetDoubleAnimation(0.8, 1.0, 90);
		_leaveAnim = AnimationUtils.GetDoubleAnimation(1.0, 0.8, 90);
	}

	public TabBarItem()
	{
		base.Height = 48.0;
		base.Width = 50.0;
		base.Cursor = Cursors.Hand;
		base.BorderThickness = new Thickness(0.0);
		SetValue(ToolTipService.BetweenShowDelayProperty, 0);
		SetValue(ToolTipService.InitialShowDelayProperty, 500);
		InitView();
		base.DataContextChanged += OnDataBind;
		base.Background = Brushes.Transparent;
		base.PreviewMouseLeftButtonDown += OnItemMouseDown;
		base.MouseLeftButtonUp += OnItemMouseUp;
		base.MouseEnter += OnMouseEnter;
		base.MouseLeave += OnMouseLeave;
		base.MouseMove += OnMouseMove;
		base.MouseRightButtonUp += OnRightClick;
	}

	private void OnRightClick(object sender, MouseButtonEventArgs e)
	{
		object dataContext = base.DataContext;
		TabBarItemViewModel model = dataContext as TabBarItemViewModel;
		if (model == null || !model.CanOpenWindow())
		{
			return;
		}
		List<CustomMenuItemViewModel> list = new List<CustomMenuItemViewModel>
		{
			new CustomMenuItemViewModel("Window", Utils.GetString(model.IsWindowOpened() ? "CloseWindow" : "OpenNewWindow"), Utils.GetGeometry("Ic.Tabbar.OpenNewWindow"))
		};
		if (model.Module == "Task")
		{
			list.Insert(0, new CustomMenuItemViewModel("ExtraCal", Utils.GetString(model.IsParallelViewShow() ? "CloseShowParallel" : "ShowCalendarInList"), Utils.GetGeometry(model.IsParallelViewShow() ? "Ic.Common.Close" : "Ic.CalendarOM.SideBySide")));
		}
		if (model.Module == "Calendar")
		{
			list.Insert(0, new CustomMenuItemViewModel("ExtraTask", Utils.GetString(model.IsParallelViewShow() ? "CloseShowParallel" : "ShowListInCalendar"), Utils.GetGeometry(model.IsParallelViewShow() ? "Ic.Common.Close" : "Ic.CalendarOM.SideBySide")));
		}
		if (_popup == null)
		{
			_popup = new EscPopup
			{
				PlacementTarget = this,
				Placement = PlacementMode.Right,
				HorizontalOffset = -10.0,
				VerticalOffset = -14.0,
				StaysOpen = false
			};
		}
		CustomMenuList customMenuList = new CustomMenuList(list, _popup);
		customMenuList.Operated += delegate(object o, object args)
		{
			switch (args as string)
			{
			case "Window":
			{
				Window window = Window.GetWindow(this);
				model.OpenOrCloseWindow(window);
				break;
			}
			case "ExtraCal":
			{
				bool flag2 = model.IsParallelViewShow();
				if (!flag2)
				{
					if (!ProChecker.CheckPro(ProType.ParallelShowInTask))
					{
						break;
					}
					this.ItemClick?.Invoke(this, model);
				}
				UserActCollectUtils.AddClickEvent("split_view", "task+calendar", flag2 ? "close_from_tab" : "open");
				MainWindow.ShowCalendarInTaskChanged();
				break;
			}
			case "ExtraTask":
			{
				bool flag = model.IsParallelViewShow();
				if (!flag)
				{
					if (!ProChecker.CheckPro(ProType.ParallelShowInCalendar))
					{
						break;
					}
					this.ItemClick?.Invoke(this, model);
				}
				UserActCollectUtils.AddClickEvent("split_view", "calendar+task", flag ? "close_from_tab" : "open");
				MainWindow.ShowTaskInCalendarChanged();
				break;
			}
			}
		};
		customMenuList.Show();
	}

	private void OnMouseMove(object sender, MouseEventArgs e)
	{
		if (e.LeftButton == MouseButtonState.Pressed && _mouseDownItem == this)
		{
			Point position = e.GetPosition(this);
			if ((_mouseDownPoint - position).Length > 4.0)
			{
				_mouseDownItem = null;
				Utils.FindParent<LeftMenuBar>(this)?.TryDragModel(base.DataContext as TabBarItemViewModel);
			}
		}
	}

	private void OnMouseLeave(object sender, MouseEventArgs e)
	{
		if (!_selected && !_forceHighlight)
		{
			BeginAnimation(UIElement.OpacityProperty, _leaveAnim);
		}
	}

	private void OnMouseEnter(object sender, MouseEventArgs e)
	{
		if (!_selected && !_forceHighlight)
		{
			BeginAnimation(UIElement.OpacityProperty, _enterAnim);
		}
	}

	private void OnItemMouseDown(object sender, MouseButtonEventArgs e)
	{
		_mouseDownPoint = e.GetPosition(this);
		_mouseDownItem = this;
	}

	private void OnItemMouseUp(object sender, MouseButtonEventArgs e)
	{
		if (_mouseDownItem != this)
		{
			_mouseDownItem = null;
			return;
		}
		this.ItemClick?.Invoke(this, base.DataContext as TabBarItemViewModel);
		_mouseDownItem = null;
	}

	private void InitView()
	{
		ToolTip toolTip = new ToolTip
		{
			Placement = PlacementMode.Right,
			VerticalOffset = 10.0
		};
		toolTip.SetBinding(ContentControl.ContentProperty, "ToolTip");
		base.ToolTip = toolTip;
		_icon = new Path
		{
			Height = 28.0,
			Stretch = Stretch.Uniform,
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center
		};
		_icon.SetBinding(Path.DataProperty, "Icon");
		_icon.SetResourceReference(Shape.FillProperty, "LeftBarUnSelectedColor");
		Child = _icon;
	}

	private void OnDataBind(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (e.OldValue is TabBarItemViewModel source)
		{
			PropertyChangedEventManager.RemoveHandler(source, OnPropertyChanged, "");
		}
		if (e.NewValue is TabBarItemViewModel tabBarItemViewModel)
		{
			base.Visibility = ((!tabBarItemViewModel.Show) ? Visibility.Collapsed : Visibility.Visible);
			PropertyChangedEventManager.AddHandler(tabBarItemViewModel, OnPropertyChanged, "");
			Utils.FindParent<ContentPresenter>(this)?.SetValue(Canvas.TopProperty, (double)tabBarItemViewModel.SortOrder * 48.0);
			SetValue(Panel.ZIndexProperty, tabBarItemViewModel.Show ? 10 : 0);
			if (tabBarItemViewModel.Module == "Pomo")
			{
				InitPomoIcon();
			}
			SetStyle(tabBarItemViewModel.Selected);
		}
	}

	private void InitPomoIcon()
	{
		if (_focusPanel == null)
		{
			_focusProgress = new PomoProgressBar
			{
				TimingSize = 2,
				Height = 22.0,
				Width = 22.0,
				Angle = 0.0,
				StrokeThickness = 3.0,
				IsStrokeMode = TickFocusManager.IsPomo
			};
			_focusProgress.SetResourceReference(PomoProgressBar.UnderColorProperty, "PomoTabBarUnderColor");
			_focusProgress.SetResourceReference(PomoProgressBar.TopColorProperty, "LeftBarSelectedIconColor");
			_relaxIcon = new Path
			{
				Height = 10.0,
				Width = 10.0,
				Margin = new Thickness(0.0),
				Data = Utils.GetGeometry("Ic.Tabbar.PomoPlay"),
				Stretch = Stretch.Uniform,
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Center
			};
			_relaxIcon.SetResourceReference(Shape.FillProperty, "PomoTabBarRelaxColor");
			TickFocusManager.StatusChanged += OnFocusStatusChanged;
			TickFocusManager.TypeChanged += OnFocusTypeChanged;
			TickFocusManager.CurrentSecondChanged += OnFocusSecondChanged;
			_focusPanel = new Grid
			{
				Width = 22.0,
				Height = 22.0,
				UseLayoutRounding = true,
				SnapsToDevicePixels = true,
				Children = 
				{
					(UIElement)_focusProgress,
					(UIElement)_relaxIcon
				}
			};
		}
	}

	private void OnFocusTypeChanged()
	{
		base.Dispatcher.Invoke(delegate
		{
			_focusProgress.IsStrokeMode = TickFocusManager.IsPomo;
			_focusProgress.Reset();
		});
	}

	private void OnFocusSecondChanged()
	{
		base.Dispatcher.Invoke(delegate
		{
			_focusProgress.Angle = TickFocusManager.Config.GetDisplayAngle(next: false);
			if (!_focusProgress.IsStrokeMode && TickFocusManager.Config.CurrentSeconds >= 30.0)
			{
				_focusProgress.HideLeftMask();
			}
		});
	}

	private void OnFocusStatusChanged()
	{
		base.Dispatcher.Invoke(delegate
		{
			_forceHighlight = TickFocusManager.Status != PomoStatus.WaitingWork || TickFocusManager.Config.FromRelax;
			SetStyle(_selected);
			_focusProgress.SetResourceReference(PomoProgressBar.TopColorProperty, TickFocusManager.InRelax ? "PomoTabBarRelaxColor" : "LeftBarSelectedIconColor");
			_relaxIcon.Visibility = ((!TickFocusManager.InRelax && TickFocusManager.Status != PomoStatus.WaitingWork) ? Visibility.Collapsed : Visibility.Visible);
			_relaxIcon.SetResourceReference(Shape.FillProperty, TickFocusManager.InRelax ? "PomoTabBarRelaxColor" : "PrimaryColor");
		});
	}

	private void SetStyle(bool selected)
	{
		_selected = selected;
		if (selected || _forceHighlight)
		{
			BeginAnimation(UIElement.OpacityProperty, null);
			base.Opacity = 1.0;
			_icon.SetResourceReference(Shape.FillProperty, "LeftBarSelectedIconColor");
		}
		else
		{
			base.Opacity = 0.8;
			_icon.SetResourceReference(Shape.FillProperty, "LeftBarUnSelectedColor");
		}
		if (_selected || _focusPanel == null)
		{
			Child = _icon;
		}
		else if (TickFocusManager.Status == PomoStatus.WaitingWork && !TickFocusManager.Config.FromRelax)
		{
			Child = _icon;
			_focusProgress.Reset();
		}
		else
		{
			Child = _focusPanel;
		}
	}

	private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (!(sender is TabBarItemViewModel tabBarItemViewModel))
		{
			return;
		}
		switch (e.PropertyName)
		{
		case "Selected":
			SetStyle(tabBarItemViewModel.Selected);
			break;
		case "Dragging":
			BeginAnimation(UIElement.OpacityProperty, null);
			if (tabBarItemViewModel.Dragging)
			{
				base.Opacity = 0.0;
			}
			else
			{
				base.Opacity = (tabBarItemViewModel.Selected ? 1.0 : 0.8);
			}
			break;
		case "SortOrder":
		{
			ContentPresenter contentPresenter = Utils.FindParent<ContentPresenter>(this);
			if (!tabBarItemViewModel.Dragging)
			{
				DoubleAnimation doubleAnimation = AnimationUtils.GetDoubleAnimation((double)contentPresenter.GetValue(Canvas.TopProperty), (double)tabBarItemViewModel.SortOrder * 48.0, 150);
				doubleAnimation.EasingFunction = new CubicEase
				{
					EasingMode = EasingMode.EaseOut
				};
				contentPresenter.BeginAnimation(Canvas.TopProperty, doubleAnimation);
			}
			else
			{
				contentPresenter.BeginAnimation(Canvas.TopProperty, null);
				contentPresenter.SetValue(Canvas.TopProperty, (double)tabBarItemViewModel.SortOrder * 48.0);
			}
			break;
		}
		case "Show":
		{
			double num = (tabBarItemViewModel.Selected ? 1.0 : 0.8);
			BeginAnimation(UIElement.OpacityProperty, AnimationUtils.GetDoubleAnimation(tabBarItemViewModel.Show ? 0.0 : num, tabBarItemViewModel.Show ? num : 0.0, 80));
			SetValue(Panel.ZIndexProperty, tabBarItemViewModel.Show ? 10 : 0);
			base.Visibility = ((!tabBarItemViewModel.Show) ? Visibility.Collapsed : Visibility.Visible);
			break;
		}
		case "GuideText":
			if (!string.IsNullOrEmpty(tabBarItemViewModel.GuideText))
			{
				ShowPopupText(tabBarItemViewModel.GuideText);
			}
			break;
		}
	}

	private async Task ShowPopupText(string text)
	{
		Popup popup = new Popup
		{
			AllowsTransparency = true,
			StaysOpen = true
		};
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal
		};
		stackPanel.Children.Add(new Path
		{
			Data = Geometry.Parse("M 0,6 6,12 6,0 0,6z"),
			Fill = ThemeUtil.GetColor("ToolTipColor"),
			Width = 6.0,
			Height = 12.0,
			VerticalAlignment = VerticalAlignment.Center
		});
		Border element = new Border
		{
			Background = ThemeUtil.GetColor("ToolTipColor"),
			CornerRadius = new CornerRadius(2.0),
			Child = new TextBlock
			{
				Text = text,
				Foreground = Brushes.White,
				FontSize = 12.0,
				Padding = new Thickness(6.0)
			}
		};
		stackPanel.Children.Add(element);
		popup.Child = stackPanel;
		popup.PlacementTarget = this;
		popup.Placement = PlacementMode.Right;
		popup.IsOpen = true;
		popup.HorizontalOffset = -5.0;
		popup.VerticalOffset = 8.0;
		for (int i = 0; i < 2; i++)
		{
			await Task.Delay(2000);
			popup.IsOpen = false;
			popup.IsOpen = true;
		}
		popup.IsOpen = false;
	}
}
