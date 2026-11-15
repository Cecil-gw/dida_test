using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Shapes;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Twitter;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Time;

public class TimeZoneSelectControl : UserControl, IComponentConnector, IStyleConnector
{
	public static readonly DependencyProperty TabSelectedProperty = DependencyProperty.Register("TabSelected", typeof(bool), typeof(TimeZoneSelectControl), new PropertyMetadata(false, null));

	private readonly TimeZoneViewModel _localTimeZone = TimeZoneData.LocalTimeZoneModel;

	private TimeZoneViewModel _oldTimeZone = new TimeZoneViewModel();

	private static readonly TimeZoneViewModel FloatModel = TimeZoneViewModel.GetFloatModel();

	private TimeZoneViewModel _selectedTimeZone;

	private List<TimeZoneViewModel> _displayItems;

	internal TimeZoneSelectControl Root;

	internal Grid SelectedTimeZoneGrid;

	internal Border BackBorder;

	internal TextBlock SelectedTimeZone;

	internal Path DropArrow;

	internal Popup OptionPopup;

	internal ContentControl DropContent;

	internal RadioButton TimeZoneRadio;

	internal Grid SelectTimeZoneGrid;

	internal TextBlock SelectedTimeZoneText;

	internal EscPopup TimeZonePopup;

	internal TextBox SearchText;

	internal Button ClearButton;

	internal ListView TimeZoneItems;

	internal RadioButton KeepTimeRadio;

	internal EscPopup DirectTimeZonePopup;

	internal TextBox DirectSearchText;

	internal Button DirectClearButton;

	internal ListView DirectTimeZoneItems;

	private bool _contentLoaded;

	public bool TabSelected
	{
		get
		{
			return (bool)GetValue(TabSelectedProperty);
		}
		set
		{
			SetValue(TabSelectedProperty, value);
		}
	}

	private static List<TimeZoneViewModel> _models => TimeZoneData.Models;

	public bool DirectSelectTimeZone { get; set; }

	public event EventHandler<TimeZoneViewModel> OnTimeZoneChanged;

	public TimeZoneSelectControl(TimeZoneViewModel model = null)
	{
		InitializeComponent();
		TimeZoneData.GetTimeZoneModels();
		SetData(model);
	}

	public void SetData(TimeZoneViewModel model)
	{
		_selectedTimeZone = model ?? _localTimeZone;
		SelectedTimeZone.Text = _selectedTimeZone.DisplayName;
		SelectedTimeZone.ToolTip = _selectedTimeZone.DisplayName;
	}

	private void ShowPopupClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		ShowPopup();
	}

	public void ShowPopup()
	{
		_oldTimeZone = _selectedTimeZone;
		if (DirectSelectTimeZone)
		{
			ShowDirectTimeZonePopup();
			return;
		}
		SelectedTimeZoneText.Text = (_selectedTimeZone.IsFloat ? _localTimeZone.DisplayName : _selectedTimeZone.DisplayName);
		SelectedTimeZoneText.IsEnabled = !_selectedTimeZone.IsFloat;
		OptionPopup.IsOpen = true;
		TimeZoneRadio.IsChecked = !_selectedTimeZone.IsFloat;
		KeepTimeRadio.IsChecked = _selectedTimeZone.IsFloat;
		SelectedTimeZoneText.Opacity = (_selectedTimeZone.IsFloat ? 0.36 : 0.85);
	}

	private void TimeZoneFloatClick(object sender, RoutedEventArgs routedEventArgs)
	{
		SelectedTimeZoneText.Text = _localTimeZone.DisplayName;
		SelectedTimeZoneText.ToolTip = _localTimeZone.DisplayName;
		_selectedTimeZone = FloatModel;
		SelectedTimeZoneText.Opacity = 0.36;
	}

	private void SelectTimeZoneCheck(object sender, RoutedEventArgs e)
	{
		if (_selectedTimeZone.IsFloat)
		{
			_selectedTimeZone = _localTimeZone;
			SelectedTimeZoneText.Text = _localTimeZone.DisplayName;
			SelectedTimeZoneText.ToolTip = _localTimeZone.DisplayName;
			SelectedTimeZoneText.Opacity = 0.85;
		}
	}

	private void SelectTimeZoneClick(object sender, MouseButtonEventArgs e)
	{
		OpenTimeZonePopup(TimeZonePopup, TimeZoneItems, SearchText, ClearButton);
	}

	private void ShowDirectTimeZonePopup()
	{
		OpenTimeZonePopup(DirectTimeZonePopup, DirectTimeZoneItems, DirectSearchText, DirectClearButton);
	}

	private void OpenTimeZonePopup(Popup popup, ListView listView, TextBox searchText, Button clearButton)
	{
		popup.IsOpen = true;
		List<TimeZoneViewModel> list = (_displayItems = GetDisplayModels());
		ItemsSourceHelper.SetItemsSource(listView, list);
		listView.ScrollIntoView(list[0]);
		searchText.Text = "";
		searchText.Focus();
		clearButton.Visibility = Visibility.Collapsed;
		SetFocus(GetHwnd(popup));
	}

	public void SetDropContentWidth(double width)
	{
		DropContent.Width = width;
	}

	private List<TimeZoneViewModel> GetDisplayModels()
	{
		List<TimeZoneViewModel> list = new List<TimeZoneViewModel>();
		List<TimeZoneViewModel> list2 = _models.ToList();
		TimeZoneViewModel timeZoneViewModel = list2.FirstOrDefault((TimeZoneViewModel f) => f.TimeZoneName == _localTimeZone.TimeZoneName);
		if (timeZoneViewModel != null)
		{
			list.Add(timeZoneViewModel);
			list2.Remove(timeZoneViewModel);
		}
		TimeZoneViewModel timeZoneViewModel2 = list2.FirstOrDefault((TimeZoneViewModel f) => f.TimeZoneName == _selectedTimeZone?.TimeZoneName);
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
		list.Add(new TimeZoneViewModel
		{
			IsSplit = true
		});
		list.AddRange(list2);
		list.ForEach(delegate(TimeZoneViewModel m)
		{
			m.Selected = m.TimeZoneName == _selectedTimeZone?.TimeZoneName;
		});
		return list;
	}

	private void KnowMoreClick(object sender, MouseButtonEventArgs e)
	{
		string text = "https://www.dida365.com";
		text = "https://help.dida365.com/faqs/6609623323343060992";
		try
		{
			Utils.TryProcessStartUrl(text);
			OptionPopup.IsOpen = false;
		}
		catch (Exception)
		{
			OptionPopup.IsOpen = false;
		}
	}

	private void OnTimeZoneSelect(object sender, MouseButtonEventArgs e)
	{
		if (sender is Border { DataContext: TimeZoneViewModel dataContext })
		{
			SelectedTimeZoneText.Text = dataContext.DisplayName;
			SelectedTimeZoneText.ToolTip = dataContext.DisplayName;
			_selectedTimeZone = dataContext;
			TimeZonePopup.IsOpen = false;
			SaveRecent(dataContext.TimeZoneName);
		}
	}

	private void OnDirectTimeZoneSelect(object sender, MouseButtonEventArgs e)
	{
		if (sender is Border { DataContext: TimeZoneViewModel dataContext })
		{
			_selectedTimeZone = dataContext;
			SelectedTimeZone.Text = dataContext.DisplayName;
			SelectedTimeZone.ToolTip = dataContext.DisplayName;
			DirectTimeZonePopup.IsOpen = false;
			SaveRecent(dataContext.TimeZoneName);
			NotifyTimeZoneChanged();
			_oldTimeZone = _selectedTimeZone;
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

	private void NotifyTimeZoneChanged()
	{
		if (_selectedTimeZone.IsFloat != _oldTimeZone.IsFloat || !_selectedTimeZone.TimeZone.Equals(_oldTimeZone.TimeZone))
		{
			this.OnTimeZoneChanged?.Invoke(null, _selectedTimeZone);
		}
	}

	private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
	{
		SearchTimeZones(SearchText, TimeZoneItems, ClearButton);
	}

	private void OnDirectSearchTextChanged(object sender, TextChangedEventArgs e)
	{
		SearchTimeZones(DirectSearchText, DirectTimeZoneItems, DirectClearButton);
	}

	private void SearchTimeZones(TextBox searchText, ListView listView, Button clearButton)
	{
		if (!string.IsNullOrEmpty(searchText.Text))
		{
			string query = searchText.Text.Trim().ToLower();
			List<TimeZoneViewModel> items = _displayItems?.Where((TimeZoneViewModel m) => m.TimeZone != null && m.DisplayName.ToLower().Contains(query) && !m.TimeZone.Equals(_selectedTimeZone.TimeZone)).ToList();
			ItemsSourceHelper.SetItemsSource(listView, items);
			clearButton.Visibility = Visibility.Visible;
		}
		else
		{
			ItemsSourceHelper.SetItemsSource(listView, _displayItems);
			clearButton.Visibility = Visibility.Collapsed;
		}
	}

	[DllImport("User32.dll")]
	public static extern IntPtr SetFocus(IntPtr hWnd);

	private IntPtr GetHwnd(Popup popup)
	{
		return ((HwndSource)PresentationSource.FromVisual(popup.Child)).Handle;
	}

	private void OnTopTimezoneClick(object sender, MouseButtonEventArgs e)
	{
		TimeZonePopup.IsOpen = false;
	}

	private void ClearText(object sender, RoutedEventArgs e)
	{
		SearchText.Text = "";
	}

	private void ClearDirectText(object sender, RoutedEventArgs e)
	{
		DirectSearchText.Text = "";
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		OptionPopup.IsOpen = false;
	}

	private void OnSaveClick(object sender, RoutedEventArgs e)
	{
		NotifyTimeZoneChanged();
		SelectedTimeZone.Text = _selectedTimeZone.DisplayName;
		SelectedTimeZone.ToolTip = _selectedTimeZone.DisplayName;
		_oldTimeZone = _selectedTimeZone;
		OptionPopup.IsOpen = false;
	}

	private void OnPopupClosed(object sender, EventArgs e)
	{
		_selectedTimeZone = _oldTimeZone;
	}

	public bool ClosePopup()
	{
		if (DirectTimeZonePopup.IsOpen)
		{
			DirectTimeZonePopup.IsOpen = false;
			return true;
		}
		if (OptionPopup.IsOpen)
		{
			OptionPopup.IsOpen = false;
			return true;
		}
		return false;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/time/timezoneselectcontrol.xaml", UriKind.Relative);
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
			Root = (TimeZoneSelectControl)target;
			break;
		case 2:
			SelectedTimeZoneGrid = (Grid)target;
			SelectedTimeZoneGrid.PreviewMouseLeftButtonUp += ShowPopupClick;
			break;
		case 3:
			BackBorder = (Border)target;
			break;
		case 4:
			SelectedTimeZone = (TextBlock)target;
			break;
		case 5:
			DropArrow = (Path)target;
			break;
		case 6:
			OptionPopup = (Popup)target;
			OptionPopup.Closed += OnPopupClosed;
			break;
		case 7:
			DropContent = (ContentControl)target;
			break;
		case 8:
			TimeZoneRadio = (RadioButton)target;
			TimeZoneRadio.Click += SelectTimeZoneCheck;
			break;
		case 9:
			SelectTimeZoneGrid = (Grid)target;
			SelectTimeZoneGrid.MouseLeftButtonUp += SelectTimeZoneClick;
			break;
		case 10:
			SelectedTimeZoneText = (TextBlock)target;
			break;
		case 11:
			TimeZonePopup = (EscPopup)target;
			break;
		case 12:
			SearchText = (TextBox)target;
			SearchText.TextChanged += OnSearchTextChanged;
			break;
		case 13:
			ClearButton = (Button)target;
			ClearButton.Click += ClearText;
			break;
		case 14:
			TimeZoneItems = (ListView)target;
			break;
		case 16:
			KeepTimeRadio = (RadioButton)target;
			KeepTimeRadio.Click += TimeZoneFloatClick;
			break;
		case 17:
			((TextBlock)target).PreviewMouseLeftButtonUp += KnowMoreClick;
			break;
		case 18:
			((Button)target).Click += OnSaveClick;
			break;
		case 19:
			((Button)target).Click += OnCancelClick;
			break;
		case 20:
			DirectTimeZonePopup = (EscPopup)target;
			break;
		case 21:
			DirectSearchText = (TextBox)target;
			DirectSearchText.TextChanged += OnDirectSearchTextChanged;
			break;
		case 22:
			DirectClearButton = (Button)target;
			DirectClearButton.Click += ClearDirectText;
			break;
		case 23:
			DirectTimeZoneItems = (ListView)target;
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
		switch (connectionId)
		{
		case 15:
			((Border)target).MouseLeftButtonUp += OnTimeZoneSelect;
			break;
		case 24:
			((Border)target).MouseLeftButtonUp += OnDirectTimeZoneSelect;
			break;
		}
	}
}
