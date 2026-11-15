using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Widget;

public class WidgetBaseUserControl : UserControl
{
	internal bool IsLocked { get; set; }

	private WidgetMorePopup _morePopup { get; set; }

	internal WidgetSettings _currentWidgetSettings { get; set; }

	internal WidgetWindow _parentWindow { get; set; }

	internal WidgetViewModel Model { get; set; }

	public WidgetBaseUserControl()
	{
	}

	public WidgetBaseUserControl(WidgetViewModel model)
	{
		Model = model;
	}

	internal virtual bool IsEditing()
	{
		return false;
	}

	internal virtual void Save()
	{
		Model.Save();
	}

	internal WidgetWindow GetParentWindow()
	{
		_parentWindow = _parentWindow ?? Utils.FindParent<WidgetWindow>(this);
		return _parentWindow;
	}

	internal void OnDragBarMouseDown(object sender, MouseButtonEventArgs e)
	{
		GetParentWindow()?.OnDragBarMouseDown(sender, e);
	}

	internal void OnDragMove(object sender, MouseEventArgs e)
	{
		GetParentWindow()?.OnDragMove(sender, e);
	}

	internal void OnMoreClick(FrameworkElement target)
	{
		if (_morePopup == null)
		{
			_morePopup = new WidgetMorePopup();
			_morePopup.SetPlaceTarget(target);
			_morePopup.MoreAction += OnMoreAction;
		}
		_morePopup.Show(new Point(-5.0, -10.0), Model.WidgetType);
	}

	internal virtual void OnMoreAction(object sender, WidgetMoreAction e)
	{
		switch (e)
		{
		case WidgetMoreAction.Setting:
			AddSettingEvents();
			break;
		case WidgetMoreAction.Lock:
			LockWidget(isLocked: true);
			_currentWidgetSettings?.Close();
			break;
		case WidgetMoreAction.Exit:
			CloseWidget();
			break;
		}
	}

	internal virtual void LockWidget(bool isLocked)
	{
		if (Model != null && IsLocked != isLocked)
		{
			GetParentWindow().ResizeMode = ((!isLocked) ? ResizeMode.CanResize : ResizeMode.NoResize);
			Model.IsLocked = isLocked;
			Model.Save();
			IsLocked = isLocked;
		}
	}

	public void OnUnlockWidgetClick(object sender, RoutedEventArgs e)
	{
		LockWidget(isLocked: false);
	}

	internal virtual void AddSettingEvents()
	{
		WidgetSettings widgetSettings = (_currentWidgetSettings = WidgetConfigHelper.TryShowSettings(Model, GetParentWindow()));
		if (widgetSettings != null)
		{
			widgetSettings.Closed -= OnSettingsClosed;
			widgetSettings.Closed += OnSettingsClosed;
			widgetSettings.DisplayOptionChanged -= OnDisplayOptionChanged;
			widgetSettings.DisplayOptionChanged += OnDisplayOptionChanged;
			widgetSettings.OpacityChanged -= OnOpacityChanged;
			widgetSettings.OpacityChanged += OnOpacityChanged;
		}
	}

	internal virtual void RemoveSettingEvent()
	{
		WidgetSettings widgetSettings = WidgetConfigHelper.TryGetSettings(Model);
		if (widgetSettings != null)
		{
			widgetSettings.Closed -= OnSettingsClosed;
			widgetSettings.DisplayOptionChanged -= OnDisplayOptionChanged;
			widgetSettings.OpacityChanged -= OnOpacityChanged;
		}
	}

	internal async void OnDisplayOptionChanged(object sender, string option)
	{
		if (option != null)
		{
			switch (option)
			{
			case "top":
			case "embed":
			case "bottom":
				GetParentWindow()?.SetTopMost();
				break;
			case "light":
			case "dark":
			case "auto":
				GetParentWindow()?.SetTheme(option);
				OnThemeChange(option);
				break;
			}
		}
	}

	private void OnOpacityChanged(object sender, float opacity)
	{
		OnOpacityChange(opacity);
		GetParentWindow()?.OnOpacityChange(opacity);
	}

	private async void OnSettingsClosed(object sender, EventArgs e)
	{
		await Model.Save();
		RemoveSettingEvent();
	}

	internal virtual void OnThemeChange(string themeKey)
	{
	}

	internal virtual void OnOpacityChange(float opacity)
	{
	}

	internal virtual void CloseWidget()
	{
	}
}
