using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Shapes;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Twitter;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Time;

public class TimeZoneSelector : UserControl, IComponentConnector, IStyleConnector
{
	private List<TimeZoneViewModel> _displayItems;

	private readonly TimeZoneViewModel _localTimeZone = TimeZoneData.LocalTimeZoneModel;

	internal Grid SelectTimeZoneGrid;

	internal Border BottomBorder;

	internal Border ClickBorder;

	internal Path ArrowPath;

	internal TextBlock SelectedTimeZoneText;

	internal EscPopup TimeZonePopup;

	internal TextBox SearchText;

	internal Button ClearButton;

	internal ListView TimeZoneItems;

	private bool _contentLoaded;

	private static List<TimeZoneViewModel> _models => TimeZoneData.Models;

	public TimeZoneViewModel SelectedTimeZone { get; private set; }

	public event EventHandler OnTimeZoneChanged;

	public TimeZoneSelector()
	{
		TimeZoneData.GetTimeZoneModels();
		InitializeComponent();
		SetTimeZone(null);
	}

	public void SetTimeZone(TimeZoneViewModel model)
	{
		SelectedTimeZone = model;
		if (model == null)
		{
			SelectedTimeZoneText.Text = Utils.GetString("SelectTimeZone");
			SelectedTimeZoneText.SetResourceReference(Control.ForegroundProperty, "BaseColorOpacity60");
		}
		else
		{
			SelectedTimeZoneText.Text = SelectedTimeZone.DisplayName;
			SelectedTimeZoneText.SetResourceReference(Control.ForegroundProperty, "BaseColorOpacity100");
		}
	}

	private async void SelectTimeZoneClick(object sender, MouseButtonEventArgs e)
	{
		await Task.Delay(120);
		TimeZonePopup.IsOpen = true;
		List<TimeZoneViewModel> list = (_displayItems = GetDisplayModels());
		ItemsSourceHelper.SetItemsSource(TimeZoneItems, list);
		TimeZoneItems.ScrollIntoView(list[0]);
		SearchText.Text = "";
		SearchText.Focus();
		HwndHelper.SetFocus(TimeZonePopup);
	}

	private List<TimeZoneViewModel> GetDisplayModels(List<string> exceptTimeZones = null)
	{
		List<TimeZoneViewModel> list = new List<TimeZoneViewModel>();
		List<TimeZoneViewModel> list2 = _models.Where((TimeZoneViewModel m) => exceptTimeZones == null || !exceptTimeZones.Contains(m.TimeZoneName)).ToList();
		TimeZoneViewModel timeZoneViewModel = list2.FirstOrDefault((TimeZoneViewModel f) => f.TimeZoneName == _localTimeZone.TimeZoneName);
		if (timeZoneViewModel != null)
		{
			list.Add(timeZoneViewModel);
			list2.Remove(timeZoneViewModel);
		}
		TimeZoneViewModel timeZoneViewModel2 = list2.FirstOrDefault((TimeZoneViewModel f) => f.TimeZoneName == SelectedTimeZone?.TimeZoneName);
		if (timeZoneViewModel2 != null)
		{
			list.Add(timeZoneViewModel2);
			list2.Remove(timeZoneViewModel2);
		}
		foreach (string re in LocalSettings.Settings.ExtraSettings.RecentTimezone?.Split(';').ToList() ?? new List<string>())
		{
			if (list.Count < 4)
			{
				TimeZoneViewModel timeZoneViewModel3 = list2.FirstOrDefault((TimeZoneViewModel f) => f.TimeZoneName == re);
				if (timeZoneViewModel3 != null)
				{
					list.Add(timeZoneViewModel3);
					list2.Remove(timeZoneViewModel3);
				}
			}
		}
		if (list.Count > 0)
		{
			list.Add(new TimeZoneViewModel
			{
				IsSplit = true
			});
		}
		list.AddRange(list2);
		list.ForEach(delegate(TimeZoneViewModel m)
		{
			m.Selected = SelectedTimeZone != null && m.TimeZoneName == SelectedTimeZone?.TimeZoneName;
		});
		return list;
	}

	private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
	{
		if (!string.IsNullOrEmpty(SearchText.Text))
		{
			List<TimeZoneViewModel> items = _displayItems?.Where((TimeZoneViewModel m) => m.TimeZone != null && m.DisplayName.ToLower().Contains(SearchText.Text.Trim().ToLower()) && !m.TimeZone.Equals(SelectedTimeZone?.TimeZone)).ToList();
			ItemsSourceHelper.SetItemsSource(TimeZoneItems, items);
			ClearButton.Visibility = Visibility.Visible;
		}
		else
		{
			ItemsSourceHelper.SetItemsSource(TimeZoneItems, _displayItems);
			ClearButton.Visibility = Visibility.Collapsed;
		}
	}

	private void ClearText(object sender, RoutedEventArgs e)
	{
		SearchText.Text = "";
	}

	private void OnTimeZoneSelect(object sender, MouseButtonEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: TimeZoneViewModel dataContext })
		{
			SetTimeZone(dataContext);
			TimeZonePopup.IsOpen = false;
			SaveRecent(dataContext.TimeZoneName);
			this.OnTimeZoneChanged?.Invoke(this, null);
		}
	}

	private void SaveRecent(string timeZone)
	{
		List<string> list = LocalSettings.Settings.ExtraSettings.RecentTimezone?.Split(';').ToList() ?? new List<string>();
		if (list.Contains(timeZone))
		{
			list.Remove(timeZone);
		}
		list.Insert(0, timeZone);
		if (list.Count > 4)
		{
			list.RemoveRange(4, list.Count - 4);
		}
		LocalSettings.Settings.ExtraSettings.RecentTimezone = list.Join(";");
	}

	public void SetEnable(bool enable)
	{
		ClickBorder.IsHitTestVisible = enable;
		ArrowPath.Opacity = (enable ? 1 : 0);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/time/timezoneselector.xaml", UriKind.Relative);
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
			SelectTimeZoneGrid = (Grid)target;
			break;
		case 2:
			BottomBorder = (Border)target;
			break;
		case 3:
			ClickBorder = (Border)target;
			ClickBorder.MouseLeftButtonUp += SelectTimeZoneClick;
			break;
		case 4:
			ArrowPath = (Path)target;
			break;
		case 5:
			SelectedTimeZoneText = (TextBlock)target;
			break;
		case 6:
			TimeZonePopup = (EscPopup)target;
			break;
		case 7:
			SearchText = (TextBox)target;
			SearchText.TextChanged += OnSearchTextChanged;
			break;
		case 8:
			ClearButton = (Button)target;
			ClearButton.Click += ClearText;
			break;
		case 9:
			TimeZoneItems = (ListView)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IStyleConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 10)
		{
			((Border)target).MouseLeftButtonUp += OnTimeZoneSelect;
		}
	}
}
