using System;
using System.CodeDom.Compiler;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Cache;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.Config;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Project;

public class AddSubscribeDialog : ContentControl, ITabControl, IComponentConnector, IStyleConnector
{
	public static readonly DependencyProperty SelectedIndexProperty = DependencyProperty.Register("SelectedIndex", typeof(int), typeof(AddSubscribeDialog), new PropertyMetadata(-1, null));

	private readonly Popup _popup;

	internal AddSubscribeDialog Root;

	internal ItemsControl MainItems;

	private bool _contentLoaded;

	public int SelectedIndex
	{
		get
		{
			return (int)GetValue(SelectedIndexProperty);
		}
		set
		{
			SetValue(SelectedIndexProperty, value);
		}
	}

	public ObservableCollection<AddSubscribeItem> Items { get; set; } = new ObservableCollection<AddSubscribeItem>();

	public AddSubscribeDialog(FrameworkElement element)
	{
		InitializeComponent();
		_popup = new EscPopup
		{
			PlacementTarget = element,
			StaysOpen = false,
			AllowsTransparency = true,
			VerticalOffset = 0.0,
			HorizontalOffset = element.ActualWidth + 0.0,
			Placement = PlacementMode.Relative
		};
		_popup.Child = this;
		MainItems.ItemsSource = Items;
		string text = "pack://application:,,,/Assets/ImageSource";
		string text2 = " " + Utils.GetString("Calendar");
		Items.Add(new AddSubscribeItem
		{
			Type = "Google",
			Title = "Google" + text2,
			ImagePath = text + "/googleCal_45.png"
		});
		Items.Add(new AddSubscribeItem
		{
			Type = "Outlook",
			Title = "Outlook" + text2,
			ImagePath = text + "/outlook_45.png"
		});
		Items.Add(new AddSubscribeItem
		{
			Type = "Exchange",
			Title = "Exchange" + text2,
			ImagePath = text + "/exchange_45.png"
		});
		Items.Add(new AddSubscribeItem
		{
			Type = "iCloud",
			Title = "iCloud" + text2,
			ImagePath = text + "/iCloud_45.png"
		});
		string describe = (Utils.IsTickPackage() ? null : Utils.GetString("CalDavSummary"));
		Items.Add(new AddSubscribeItem
		{
			Type = "CalDAV",
			Title = "CalDAV",
			ImagePath = text + "/calDAV_45.png",
			Describe = describe
		});
		if (!Utils.IsWindows7())
		{
			Items.Add(new AddSubscribeItem
			{
				Type = "Holiday",
				Title = Utils.GetString("holidays"),
				ImagePath = text + "/holidayUrl_45.png"
			});
		}
		Items.Add(new AddSubscribeItem
		{
			Type = "URL",
			Title = "URL",
			ImagePath = text + "/url_45.png",
			Describe = Utils.GetString("UrlSubscribeSummary")
		});
	}

	public void Show()
	{
		_popup.IsOpen = true;
	}

	private void OnActionClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: AddSubscribeItem dataContext })
		{
			OnAction(dataContext.Type);
			_popup.IsOpen = false;
		}
	}

	private void OnAction(string tag)
	{
		if (tag != "Google" && (!ProChecker.CheckPro(ProType.SubscribeCalendar) || !LimitCache.CheckCalendarCount(tag)))
		{
			return;
		}
		UserActCollectUtils.AddClickEvent("project_list_ui", "event", "add");
		if (tag == null)
		{
			return;
		}
		switch (tag.Length)
		{
		case 6:
			switch (tag[0])
			{
			case 'G':
				if (tag == "Google")
				{
					LinkGoogleCalendarWindow linkGoogleCalendarWindow = new LinkGoogleCalendarWindow();
					linkGoogleCalendarWindow.Owner = Window.GetWindow(_popup.PlacementTarget);
					linkGoogleCalendarWindow.ShowDialog();
				}
				break;
			case 'C':
				if (tag == "CalDAV")
				{
					BindAccountWindow bindAccountWindow2 = new BindAccountWindow(null, Constants.BindAccountType.CalDAV);
					bindAccountWindow2.Owner = Window.GetWindow(_popup.PlacementTarget);
					bindAccountWindow2.Show();
				}
				break;
			case 'i':
				if (tag == "iCloud")
				{
					BindICloudWindow bindICloudWindow = new BindICloudWindow(null);
					bindICloudWindow.Owner = Window.GetWindow(_popup.PlacementTarget);
					bindICloudWindow.Topmost = false;
					bindICloudWindow.Show();
				}
				break;
			}
			break;
		case 7:
			switch (tag[0])
			{
			case 'O':
				if (tag == "Outlook")
				{
					new BindOutlookWindow().ShowDialog();
				}
				break;
			case 'H':
				if (tag == "Holiday")
				{
					OpenHolidaySubscribeWebview();
				}
				break;
			}
			break;
		case 8:
			if (tag == "Exchange")
			{
				BindAccountWindow bindAccountWindow = new BindAccountWindow(null, Constants.BindAccountType.Exchange);
				bindAccountWindow.Owner = Window.GetWindow(_popup.PlacementTarget);
				bindAccountWindow.Show();
			}
			break;
		case 3:
			if (tag == "URL")
			{
				AddUrlCalendarWindow addUrlCalendarWindow = new AddUrlCalendarWindow();
				addUrlCalendarWindow.Owner = Window.GetWindow(_popup.PlacementTarget);
				addUrlCalendarWindow.Show();
			}
			break;
		case 4:
		case 5:
			break;
		}
	}

	private void OpenHolidaySubscribeWebview()
	{
		string url = "/webview/holidayCalendar?lang=" + Utils.GetLanguage().ToLower() + "&dark=" + ThemeUtil.IsDark().ToString().ToLower() + "&hl=" + Utils.GetLanguage();
		NavigateWebBrowserWindow webBrowserWindow = new NavigateWebBrowserWindow(url, addLang: true, FinishHolidaySubsAction)
		{
			Title = Utils.GetString("AddHolidays"),
			Width = 455.0,
			Height = 560.0,
			Owner = Window.GetWindow(this)
		};
		webBrowserWindow.ToastAction = delegate(string message)
		{
			webBrowserWindow.ShowToastPopup(message);
		};
		webBrowserWindow.ShowDialog();
	}

	private void FinishHolidaySubsAction(string data)
	{
		SubscribeCalendarHelper.OnHolidaySubscribeSelect(data, Utils.Toast);
	}

	public bool HandleTab(bool shift)
	{
		UpDownSelect(shift);
		return true;
	}

	public bool HandleEnter()
	{
		if (SelectedIndex >= 0 && Items.Count > SelectedIndex)
		{
			string type = Items[SelectedIndex].Type;
			OnAction(type);
			_popup.IsOpen = false;
		}
		return true;
	}

	public bool HandleEsc()
	{
		return false;
	}

	public bool UpDownSelect(bool isUp)
	{
		if (SelectedIndex < 0)
		{
			SelectedIndex = (isUp ? (Items.Count - 1) : 0);
			for (int i = 0; i < Items.Count; i++)
			{
				Items[i].IsHighlight = false;
			}
			return true;
		}
		int selectedIndex = SelectedIndex;
		selectedIndex += (isUp ? (Items.Count - 1) : (Items.Count + 1));
		selectedIndex = (SelectedIndex = selectedIndex % Items.Count);
		for (int j = 0; j < Items.Count; j++)
		{
			Items[j].IsHighlight = j == selectedIndex;
		}
		return true;
	}

	public bool LeftRightSelect(bool isLeft)
	{
		return true;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/project/addsubscribedialog.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			Root = (AddSubscribeDialog)target;
			break;
		case 2:
			MainItems = (ItemsControl)target;
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
		if (connectionId == 3)
		{
			((Border)target).MouseLeftButtonUp += OnActionClick;
		}
	}
}
