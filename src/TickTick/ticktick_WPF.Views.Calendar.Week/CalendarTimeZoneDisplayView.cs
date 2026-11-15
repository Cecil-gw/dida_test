using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Emoji.Wpf;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Calendar.Week;

public sealed class CalendarTimeZoneDisplayView : Border
{
	private readonly StackPanel _container;

	public CalendarTimeZoneDisplayView()
	{
		Border border = new Border
		{
			VerticalAlignment = VerticalAlignment.Bottom
		};
		border.SetResourceReference(FrameworkElement.StyleProperty, "HoverBorderStyle");
		_container = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			VerticalAlignment = VerticalAlignment.Bottom,
			Background = Brushes.Transparent
		};
		border.Child = _container;
		Child = border;
		base.Cursor = Cursors.Hand;
		base.MouseLeftButtonUp += OnMouseClick;
		SetTimeZones(CalendarGeoHelper.GetDisplayTimelineTimeZones(withCityName: false));
	}

	private void OnMouseClick(object sender, MouseButtonEventArgs e)
	{
		FrameworkElement frameworkElement = sender as FrameworkElement;
		EscPopup popup = new EscPopup
		{
			StaysOpen = false,
			PlacementTarget = frameworkElement,
			VerticalOffset = -5.0,
			Placement = PlacementMode.Mouse,
			HorizontalOffset = -5.0
		};
		ShowTimeZoneSettings(frameworkElement, popup);
	}

	private void ShowTimeZoneSettings(FrameworkElement sender, Popup popup)
	{
		if (!LocalSettings.Settings.EnableCalendarTimeZone)
		{
			return;
		}
		CalTimelineTimeZones calendarDisplayTimeZones = LocalSettings.Settings.CalendarDisplayTimeZones;
		List<CustomMenuItemViewModel> menuItems = new List<CustomMenuItemViewModel>();
		TimeZoneViewModel local = TimeZoneData.LocalTimeZoneModel;
		string text = calendarDisplayTimeZones?.current?.FirstOrDefault((CalTimeZone t) => t.timeZone == local.TimeZoneName)?.label;
		Path path = UiUtils.CreatePath("IcCurrentTimeZone", "BaseColorOpacity20", "Path01", 16.0);
		path.Margin = new Thickness(0.0, -2.0, 0.0, 0.0);
		path.HorizontalAlignment = HorizontalAlignment.Left;
		menuItems.Add(new CustomMenuItemViewModel("current", string.IsNullOrWhiteSpace(text) ? TimeZoneUtils.GetTimeZoneUtcName(TimeZoneInfo.Local) : text, Utils.GetGeometry("Ic.Common.Completed"))
		{
			CustomIconColor = ThemeUtil.GetColor("BaseColorOpacity20", sender),
			ShowSelected = false,
			ImageWidth = 12.0,
			UIContent = path
		});
		if (calendarDisplayTimeZones != null && calendarDisplayTimeZones.additional?.Count > 0)
		{
			foreach (CalTimeZone item2 in calendarDisplayTimeZones.additional)
			{
				TimeZoneInfo timeZoneInfo = TimeZoneUtils.GetTimeZoneInfo(item2.timeZone);
				if (timeZoneInfo != null)
				{
					bool flag = LocalSettings.Settings.ExtraSettings.HiddenCalTimeZones?.Contains(item2.timeZone) ?? false;
					menuItems.Add(new CustomMenuItemViewModel(item2.timeZone, string.IsNullOrWhiteSpace(item2.label) ? TimeZoneUtils.GetTimeZoneUtcName(timeZoneInfo) : item2.label, Utils.GetGeometry("Ic.Common.Completed"))
					{
						CustomIconColor = (flag ? Brushes.Transparent : ThemeUtil.GetColor("TextAccentColor", sender)),
						ShowSelected = false,
						ImageWidth = 12.0
					});
				}
			}
		}
		menuItems.Add(new CustomMenuItemViewModel(null));
		menuItems.Add(new CustomMenuItemViewModel("set", Utils.GetString("TimeZoneSettings"), Utils.GetImageSource("SettingsDrawingImage", this))
		{
			ShowSelected = false
		});
		CustomMenuList customMenuList = new CustomMenuList(menuItems, popup);
		customMenuList.AutoClose = false;
		customMenuList.Operated += delegate(object o, object val)
		{
			switch (val as string)
			{
			case "set":
			{
				popup.IsOpen = false;
				Window owner = Window.GetWindow(sender);
				CalendarTimeZoneConfigWindow calendarTimeZoneConfigWindow = new CalendarTimeZoneConfigWindow();
				calendarTimeZoneConfigWindow.Owner = owner;
				calendarTimeZoneConfigWindow.Closed += delegate
				{
					owner?.Activate();
				};
				calendarTimeZoneConfigWindow.ShowDialog();
				break;
			}
			default:
			{
				string item = val.ToString();
				if (LocalSettings.Settings.ExtraSettings.HiddenCalTimeZones == null)
				{
					LocalSettings.Settings.ExtraSettings.HiddenCalTimeZones = new List<string> { item };
				}
				else if (LocalSettings.Settings.ExtraSettings.HiddenCalTimeZones.Contains(item))
				{
					LocalSettings.Settings.ExtraSettings.HiddenCalTimeZones.Remove(item);
				}
				else
				{
					LocalSettings.Settings.ExtraSettings.HiddenCalTimeZones.Add(item);
				}
				List<CustomMenuItemViewModel> list = menuItems.Where((CustomMenuItemViewModel m) => m.Value == val).ToList();
				if (list.Count > 0)
				{
					SolidColorBrush color = (LocalSettings.Settings.ExtraSettings.HiddenCalTimeZones.Contains(item) ? Brushes.Transparent : ThemeUtil.GetColor("TextAccentColor", sender));
					list.ForEach(delegate(CustomMenuItemViewModel customMenuItemViewModel)
					{
						customMenuItemViewModel.CustomIconColor = color;
					});
				}
				GlobalEventManager.NotifyTimeLineTimeZonesChangedChanged(countChanged: true);
				LocalSettings.Settings.Save(force: true);
				break;
			}
			case "current":
				break;
			}
		};
		customMenuList.Show();
	}

	public void SetTimeZones(List<string> timezones)
	{
		if (!LocalSettings.Settings.EnableCalendarTimeZone || timezones == null)
		{
			_container.Children.Clear();
			base.Width = 56.0;
			return;
		}
		base.Width = 56.0 * (double)timezones.Count;
		int count = _container.Children.Count;
		if (count > timezones.Count)
		{
			_container.Children.RemoveRange(timezones.Count, count - timezones.Count);
		}
		else
		{
			int num = timezones.Count - count;
			for (int i = 0; i < num; i++)
			{
				EmjTextBlock emjTextBlock = new EmjTextBlock
				{
					Width = 46.0,
					TextWrapping = TextWrapping.NoWrap,
					FontSize = 10.0,
					LineHeight = 16.0,
					Height = 16.0,
					ClipToBounds = true,
					Margin = new Thickness(2.0, 1.0, 8.0, 0.0),
					TextAlignment = TextAlignment.Right,
					VerticalAlignment = VerticalAlignment.Bottom
				};
				emjTextBlock.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity60");
				_container.Children.Add(emjTextBlock);
			}
		}
		for (int j = 0; j < timezones.Count; j++)
		{
			if (_container.Children[j] is EmjTextBlock emjTextBlock2)
			{
				emjTextBlock2.Text = timezones[j];
			}
		}
	}
}
