using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using ticktick_WPF.Dal;
using ticktick_WPF.Framework.Collections;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Misc.ColorSelector;

namespace ticktick_WPF.Views.Config;

public class EditHolidaySubsWindow : Window, IComponentConnector
{
	internal Grid Container;

	internal Border ExpireIcon;

	internal ColorSelector ColorItems;

	internal CustomComboBox StatusComboBox;

	internal Path More;

	private bool _contentLoaded;

	public EditHolidaySubsWindow(SubscribeCalendarViewModel model)
	{
		InitializeComponent();
		base.DataContext = model;
		if (model.SubType == SubscribeCalendarType.Url)
		{
			ColorItems.SetSelectedColor(model.Color);
		}
		StatusComboBox.Init(new ExtObservableCollection<ComboBoxViewModel>
		{
			new ComboBoxViewModel("show", Utils.GetString("ShowInSmartlistsAndCalendar"), 32.0)
			{
				Selected = (model.ShowStatus == CalendarSubsShowType.show)
			},
			new ComboBoxViewModel("calendar", Utils.GetString("ShowOnlyInCalendar"), 32.0)
			{
				Selected = (model.ShowStatus == CalendarSubsShowType.calendar)
			},
			new ComboBoxViewModel("hidden", Utils.GetString("Hide"), 32.0)
			{
				Selected = (model.ShowStatus == CalendarSubsShowType.hidden)
			}
		}, null);
	}

	private async void OnSaveClick(object sender, RoutedEventArgs e)
	{
		if (base.DataContext != null)
		{
			object dataContext = base.DataContext;
			if (dataContext is SubscribeCalendarViewModel model)
			{
				string showStatus = ((CalendarSubsShowType)StatusComboBox.SelectedIndex/*cast due to .constrained prefix*/).ToString();
				string selectedColor = ColorItems.GetSelectedColor();
				if (selectedColor != model.Color)
				{
					ColorSelector.TryAddClickEvent(selectedColor);
				}
				model.ShowStatus = (CalendarSubsShowType)StatusComboBox.SelectedIndex;
				model.Color = selectedColor;
				if (await CalendarSubscribeProfileDao.UpdateHolidaySubscribe(model))
				{
					Communicator.CalendarUrlSetVisible(model.CalendarId, showStatus);
				}
			}
		}
		Close();
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void MoreClick(object sender, MouseButtonEventArgs e)
	{
		ShowMorePopup(More);
	}

	private void CloseClick(object sender, MouseButtonEventArgs e)
	{
		Close();
	}

	private void OnExpireIconClick(object sender, MouseButtonEventArgs e)
	{
		ShowMorePopup(ExpireIcon, right: true);
	}

	private void ShowMorePopup(FrameworkElement target, bool right = false)
	{
		object dataContext = base.DataContext;
		SubscribeCalendarViewModel model = dataContext as SubscribeCalendarViewModel;
		if (model == null)
		{
			return;
		}
		EscPopup popup = new EscPopup
		{
			StaysOpen = false,
			PlacementTarget = target,
			VerticalOffset = 12.0,
			Placement = (right ? PlacementMode.Right : PlacementMode.Left),
			HorizontalOffset = (right ? (-32) : 32)
		};
		CustomMenuList customMenuList = new CustomMenuList(new List<CustomMenuItemViewModel>
		{
			new CustomMenuItemViewModel((object)"Muted", Utils.GetString("Muted"), (Geometry)null, "")
			{
				Selected = LocalSettings.Settings.ExtraSettings.DoNotDisturbInCalendar
			},
			new CustomMenuItemViewModel((object)"Unsubscribe", Utils.GetString("Unsubscribe"), (Geometry)null, "")
		}, popup);
		customMenuList.Operated += async delegate(object o, object o1)
		{
			string text = o1.ToString();
			if (!(text == "Unsubscribe"))
			{
				if (text == "Muted")
				{
					bool doNotDisturbInCalendar = LocalSettings.Settings.ExtraSettings.DoNotDisturbInCalendar;
					bool? flag = new CustomerDialog(Utils.GetString(doNotDisturbInCalendar ? "ComfirmCloseNotDisturb" : "ComfirmOpenNotDisturb"), Utils.GetString(doNotDisturbInCalendar ? "ComfirmCloseNotDisturbDesc" : "ComfirmOpenNotDisturbDesc"), Utils.GetString(doNotDisturbInCalendar ? "Close" : "Enable"), Utils.GetString("Cancel"), this).ShowDialog();
					if (flag.HasValue && flag.Value)
					{
						LocalSettings.Settings.ExtraSettings.DoNotDisturbInCalendar = !LocalSettings.Settings.ExtraSettings.DoNotDisturbInCalendar;
						LocalSettings.Settings.Save(force: true);
						UiUtils.Toast(Container, Utils.GetString(LocalSettings.Settings.ExtraSettings.DoNotDisturbInCalendar ? "Enabled" : "Disabled"));
					}
				}
			}
			else
			{
				bool? flag2 = new CustomerDialog(Utils.GetString("Unsubscribe"), Utils.GetString("CancelSubscribeMessage"), MessageBoxButton.OKCancel, this).ShowDialog();
				if (flag2.HasValue && flag2.Value)
				{
					await SubscribeCalendarHelper.UnsubscribeCalendar(model.CalendarId);
					DataChangedNotifier.NotifyCalendarChanged(50);
					Close();
				}
			}
		};
		customMenuList.Show();
	}

	private void OnDragMove(object sender, MouseButtonEventArgs e)
	{
		if (e.LeftButton == MouseButtonState.Pressed)
		{
			DragMove();
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/config/editholidaysubswindow.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	internal Delegate _CreateDelegate(Type delegateType, string handler)
	{
		return Delegate.CreateDelegate(delegateType, this, handler);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			Container = (Grid)target;
			break;
		case 2:
			((Border)target).MouseLeftButtonDown += OnDragMove;
			break;
		case 3:
			ExpireIcon = (Border)target;
			ExpireIcon.MouseLeftButtonUp += OnExpireIconClick;
			break;
		case 4:
			ColorItems = (ColorSelector)target;
			break;
		case 5:
			StatusComboBox = (CustomComboBox)target;
			break;
		case 6:
			((Button)target).Click += OnSaveClick;
			break;
		case 7:
			((Button)target).Click += OnCancelClick;
			break;
		case 8:
			((Border)target).MouseLeftButtonUp += MoreClick;
			break;
		case 9:
			More = (Path)target;
			break;
		case 10:
			((Border)target).MouseLeftButtonUp += CloseClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
