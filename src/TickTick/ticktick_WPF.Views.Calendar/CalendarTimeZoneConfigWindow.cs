using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Shapes;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Calendar;

public class CalendarTimeZoneConfigWindow : Window, IComponentConnector
{
	internal CalendarTimeZoneList Items;

	internal StackPanel AddButton;

	internal Path AddPath;

	internal Button SaveButton;

	internal Grid ToastGrid;

	private bool _contentLoaded;

	public CalendarTimeZoneConfigWindow()
	{
		InitializeComponent();
		List<CalendarTimeZoneConfigItemViewModel> list = CalendarTimeZoneConfigItemViewModel.BuildViewModels();
		ItemsSourceHelper.SetItemsSource(Items.ItemList, list);
		if (list.Count >= 5)
		{
			AddButton.Visibility = Visibility.Collapsed;
		}
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	private async void OnAddClick(object sender, MouseButtonEventArgs e)
	{
		if (Items.ItemList.ItemsSource is ObservableCollection<CalendarTimeZoneConfigItemViewModel> { Count: <5 } observableCollection)
		{
			if (observableCollection.All((CalendarTimeZoneConfigItemViewModel i) => i.TimeZone != null))
			{
				if (observableCollection.Count >= 4)
				{
					AddButton.Visibility = Visibility.Collapsed;
				}
				observableCollection.Add(new CalendarTimeZoneConfigItemViewModel());
				AddPath.SetResourceReference(Shape.FillProperty, "BaseColorOpacity40");
				await Task.Delay(100);
				AddButton.Cursor = Cursors.No;
			}
		}
		else
		{
			Toast(Utils.GetString("MaximumReached"));
		}
	}

	public void Toast(string toast)
	{
		WindowToastHelper.ToastString(ToastGrid, toast, 400.0);
	}

	private void OnTimeZoneChanged(object sender, EventArgs eventArgs)
	{
		if (Items.ItemList.ItemsSource is ObservableCollection<CalendarTimeZoneConfigItemViewModel> { Count: <5 } observableCollection)
		{
			if (observableCollection.All((CalendarTimeZoneConfigItemViewModel i) => i.TimeZone != null))
			{
				AddPath.SetResourceReference(Shape.FillProperty, "TextAccentColor");
				AddButton.Cursor = Cursors.Hand;
			}
			AddButton.Visibility = Visibility.Visible;
		}
	}

	private void OnItemDelete(object sender, CalendarTimeZoneConfigItemViewModel e)
	{
		if (Items.ItemList.ItemsSource is ObservableCollection<CalendarTimeZoneConfigItemViewModel> observableCollection)
		{
			observableCollection.Remove(e);
			if (observableCollection.All((CalendarTimeZoneConfigItemViewModel i) => i.TimeZone != null))
			{
				AddPath.SetResourceReference(Shape.FillProperty, "TextAccentColor");
				AddButton.Cursor = Cursors.Hand;
			}
			AddButton.Visibility = Visibility.Visible;
		}
	}

	public List<string> GetSelectedTimeZones()
	{
		List<string> list = new List<string>();
		if (Items.ItemList.ItemsSource is ObservableCollection<CalendarTimeZoneConfigItemViewModel> observableCollection)
		{
			foreach (CalendarTimeZoneConfigItemViewModel item in observableCollection)
			{
				if (item.TimeZone != null)
				{
					list.Add(item.TimeZone.TimeZoneName);
				}
			}
		}
		return list;
	}

	private void OnSaveClick(object sender, RoutedEventArgs e)
	{
		if (Items.ItemList.ItemsSource is ObservableCollection<CalendarTimeZoneConfigItemViewModel> observableCollection)
		{
			CalTimelineTimeZones calTimelineTimeZones = new CalTimelineTimeZones
			{
				current = new List<CalTimeZone>(),
				additional = new List<CalTimeZone>()
			};
			for (int i = 0; i < observableCollection.Count; i++)
			{
				CalendarTimeZoneConfigItemViewModel calendarTimeZoneConfigItemViewModel = observableCollection[i];
				if (calendarTimeZoneConfigItemViewModel.TimeZone != null)
				{
					CalTimeZone item = new CalTimeZone
					{
						timeZone = calendarTimeZoneConfigItemViewModel.TimeZone.TimeZoneName,
						label = calendarTimeZoneConfigItemViewModel.Emoji + calendarTimeZoneConfigItemViewModel.Label
					};
					if (i == 0)
					{
						calTimelineTimeZones.current.Add(item);
					}
					else
					{
						calTimelineTimeZones.additional.Add(item);
					}
				}
			}
			LocalSettings.Settings.SaveCalendarTimeZones(calTimelineTimeZones);
		}
		Close();
		GlobalEventManager.NotifyTimeLineTimeZonesChangedChanged(countChanged: true);
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/calendar/calendartimezoneconfigwindow.xaml", UriKind.Relative);
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
			Items = (CalendarTimeZoneList)target;
			break;
		case 2:
			AddButton = (StackPanel)target;
			AddButton.MouseLeftButtonUp += OnAddClick;
			break;
		case 3:
			AddPath = (Path)target;
			break;
		case 4:
			SaveButton = (Button)target;
			SaveButton.Click += OnSaveClick;
			break;
		case 5:
			((Button)target).Click += OnCancelClick;
			break;
		case 6:
			ToastGrid = (Grid)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
