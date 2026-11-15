using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Emoji.Wpf;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.Habit;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Pomo.FocusDetail;

public class FocusDetailTitle : DockPanel
{
	private EmjTextBlock _title;

	private HoverIconButton _linkIcon;

	private FocusObjIcon _iconBorder;

	private PomoFilterControl _focusFilter;

	public FocusDetailTitle()
	{
		base.Loaded += OnLoaded;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		base.Loaded -= OnLoaded;
		InitIcon();
		InitLinkIcon();
		InitTitle();
	}

	private void InitLinkIcon()
	{
		_linkIcon = new HoverIconButton
		{
			IconData = Utils.GetGeometry("Ic.Focus.LinkToTask"),
			IsImage = false,
			VerticalAlignment = VerticalAlignment.Center
		};
		_linkIcon.MouseLeftButtonUp += OnLinkClick;
		_linkIcon.SetValue(DockPanel.DockProperty, Dock.Right);
		base.Children.Add(_linkIcon);
	}

	private void OnLinkClick(object sender, MouseButtonEventArgs e)
	{
		bool flag = !object.Equals(sender, _linkIcon);
		if (_focusFilter == null)
		{
			EscPopup popup = new EscPopup
			{
				StaysOpen = false
			};
			_focusFilter = new PomoFilterControl(popup)
			{
				DataContext = TickFocusManager.Config.FocusVModel
			};
		}
		Popup parentPopup = _focusFilter.GetParentPopup();
		parentPopup.PlacementTarget = (flag ? _title : (sender as UIElement));
		parentPopup.Placement = (flag ? PlacementMode.Bottom : PlacementMode.Left);
		parentPopup.HorizontalOffset = (flag ? (-5) : 30);
		parentPopup.VerticalOffset = (flag ? (-5) : 30);
		parentPopup.IsOpen = true;
	}

	private void InitIcon()
	{
		_iconBorder = new FocusObjIcon(30.0)
		{
			Cursor = Cursors.Hand
		};
		_iconBorder.MouseLeftButtonUp += OnTitleClick;
		base.Children.Add(_iconBorder);
		_iconBorder.SetValue(DockPanel.DockProperty, Dock.Left);
		SetIcon();
		SetFocusIcon();
	}

	private void InitTitle()
	{
		_title = new EmjTextBlock
		{
			FontSize = 18.0,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(12.0, -1.0, 0.0, 0.0),
			TextTrimming = TextTrimming.CharacterEllipsis,
			TextWrapping = TextWrapping.Wrap,
			MaxHeight = 24.0,
			ClipToBounds = true,
			Cursor = Cursors.Hand,
			Background = Brushes.Transparent,
			HorizontalAlignment = HorizontalAlignment.Left
		};
		_title.MouseEnter += delegate
		{
			UpdateTitleColor(mouseOver: true);
		};
		_title.MouseLeave += delegate
		{
			UpdateTitleColor(mouseOver: false);
		};
		_title.MouseLeftButtonUp += OnTitleClick;
		UpdateTitleColor(mouseOver: false);
		base.Children.Add(_title);
		SetTitle();
	}

	private async void OnTitleClick(object sender, MouseButtonEventArgs e)
	{
		string objId = TickFocusManager.Config.FocusVModel.ObjId;
		if (!string.IsNullOrEmpty(objId))
		{
			if (TickFocusManager.Config.FocusVModel.IsHabit)
			{
				HabitModel habit = await HabitDao.GetHabitById(objId);
				if (habit != null)
				{
					new HabitCheckInWindow(habit, (await HabitCheckInDao.GetHabitCheckInsByHabitIdAndStamp(habit.Id, DateUtils.GetDateNum(DateTime.Today).ToString()))?.CheckStatus ?? 0, DateTime.Today, App.Window).Show(this, 0.0, 0.0, byMouse: true);
				}
			}
			else
			{
				if (!TaskCache.ExistTask(objId))
				{
					return;
				}
				TaskDetailPopup taskDetailPopup = new TaskDetailPopup();
				taskDetailPopup.DependentWindow = null;
				taskDetailPopup.Show(objId, null, new TaskWindowDisplayArgs(_title, 30.0));
			}
			string actCType = TickFocusManager.GetActCType();
			UserActCollectUtils.AddClickEvent("focus", actCType, "select_task_task_detail");
		}
		else
		{
			OnLinkClick(sender, e);
		}
	}

	public void SetTitle()
	{
		if (_title != null)
		{
			string focusId = TickFocusManager.Config.FocusVModel.FocusId;
			_title.Text = (string.IsNullOrWhiteSpace(focusId) ? Utils.GetString(TickFocusManager.IsPomo ? "PomoTimer2" : "Timing") : TickFocusManager.Config.FocusVModel.Title);
			UpdateTitleColor(mouseOver: false);
			UpdateIconColor();
		}
	}

	private void UpdateTitleColor(bool mouseOver)
	{
		if (mouseOver)
		{
			_title.SetResourceReference(TextBlock.ForegroundProperty, "PrimaryColor");
		}
		else if (string.IsNullOrEmpty(TickFocusManager.Config?.FocusVModel?.FocusId))
		{
			_title.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity40");
		}
		else
		{
			_title.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity100");
		}
	}

	private void UpdateIconColor()
	{
		if (string.IsNullOrEmpty(TickFocusManager.Config?.FocusVModel?.FocusId))
		{
			_linkIcon.ImageOpacity = 0.4;
		}
		else
		{
			_linkIcon.ImageOpacity = 1.0;
		}
	}

	public void SetIcon()
	{
		_iconBorder?.SetIcon();
	}

	public void SetFocusIcon()
	{
		_iconBorder?.SetFocusIcon(TickFocusManager.Config.FocusVModel.FocusId, TickFocusManager.Config.FocusVModel.Type);
	}
}
