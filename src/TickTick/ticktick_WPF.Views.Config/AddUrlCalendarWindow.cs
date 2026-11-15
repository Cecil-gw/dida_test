using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.Views.MainListView;

namespace ticktick_WPF.Views.Config;

public class AddUrlCalendarWindow : Window, IComponentConnector
{
	private readonly SubscribeCalendar _parent;

	internal TextBox UrlEditText;

	internal TextBlock InvalidHintText;

	internal Button SaveButton;

	internal Button CancelButton;

	private bool _contentLoaded;

	public AddUrlCalendarWindow(SubscribeCalendar parent = null)
	{
		InitializeComponent();
		_parent = parent;
		InitData();
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	private void InitData()
	{
		try
		{
			string text = Clipboard.GetText();
			if (!string.IsNullOrEmpty(text) && (text.StartsWith("https:") || text.StartsWith("webcal:") || text.EndsWith("calendar.ics")))
			{
				UrlEditText.Text = text;
				UrlEditText.SelectAll();
			}
		}
		catch (Exception)
		{
		}
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private async void OnSaveClick(object sender, RoutedEventArgs e)
	{
		SetSaving(isSaving: true);
		InvalidHintText.Visibility = Visibility.Collapsed;
		string url = UrlEditText.Text.Trim();
		if (CacheManager.GetSubscribeCalendars().Any((CalendarSubscribeProfileModel c) => c.Url == url))
		{
			ShowInvalidHint(Utils.GetString("UrlExist"));
			SetSaving(isSaving: false);
			return;
		}
		CalendarSubscribeProfileModel calendarSubscribeProfileModel = await SubscribeCalendar(url);
		if (calendarSubscribeProfileModel != null)
		{
			_parent?.LoadData();
			SubscribeCalendarHelper.ParseUrlCalendar(calendarSubscribeProfileModel.Id, calendarSubscribeProfileModel.Url);
			ListViewContainer.ReloadProjectData();
			Close();
		}
		else
		{
			ShowInvalidHint(Utils.GetString("UrlNotValid"));
		}
		SetSaving(isSaving: false);
	}

	private void SetSaving(bool isSaving)
	{
		if (isSaving)
		{
			SaveButton.Content = Utils.GetString("Subscribing");
			SaveButton.IsEnabled = false;
			SaveButton.Opacity = 0.699999988079071;
			UrlEditText.IsReadOnly = true;
			CancelButton.IsEnabled = false;
		}
		else
		{
			SaveButton.Content = Utils.GetString("Subscribe");
			SaveButton.IsEnabled = true;
			SaveButton.Opacity = 1.0;
			UrlEditText.IsReadOnly = false;
			CancelButton.IsEnabled = true;
		}
	}

	private void ShowInvalidHint(string hint)
	{
		InvalidHintText.Text = hint;
		InvalidHintText.Visibility = Visibility.Visible;
		UrlEditText.Focus();
		UrlEditText.SelectAll();
	}

	private async Task<CalendarSubscribeProfileModel> SubscribeCalendar(string remotePath)
	{
		CalendarSubscribeProfileModel calendarSubscribeProfileModel = await SubscribeCalendarHelper.SubscribeCalendar(remotePath);
		if (calendarSubscribeProfileModel != null)
		{
			return calendarSubscribeProfileModel;
		}
		return null;
	}

	private async void OnAddWindowLoaded(object sender, RoutedEventArgs e)
	{
		await Task.Delay(100);
		UrlEditText.Focus();
	}

	private void OnInputTextChanged(object sender, TextChangedEventArgs e)
	{
		InvalidHintText.Visibility = Visibility.Collapsed;
	}

	protected override void OnClosed(EventArgs eventArgs)
	{
		try
		{
			if (base.Owner != null)
			{
				base.Owner.Activate();
			}
		}
		catch (Exception)
		{
		}
	}

	private void ClearUrlText(object sender, MouseButtonEventArgs e)
	{
		UrlEditText.Text = string.Empty;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/config/addurlcalendarwindow.xaml", UriKind.Relative);
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
			((AddUrlCalendarWindow)target).Loaded += OnAddWindowLoaded;
			break;
		case 2:
			UrlEditText = (TextBox)target;
			UrlEditText.TextChanged += OnInputTextChanged;
			break;
		case 3:
			InvalidHintText = (TextBlock)target;
			break;
		case 4:
			((Border)target).MouseLeftButtonUp += ClearUrlText;
			break;
		case 5:
			SaveButton = (Button)target;
			SaveButton.Click += OnSaveClick;
			break;
		case 6:
			CancelButton = (Button)target;
			CancelButton.Click += OnCancelClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
