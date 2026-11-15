using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using Emoji.Wpf;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Calendar;

public class CalendarEventReminderWindow : MyWindow, IComponentConnector
{
	private ReminderModel _reminder;

	internal Image WindowIcon;

	internal Border TitleBorder;

	internal EmjTextBlock TitleText;

	internal Grid Buttons;

	private bool _contentLoaded;

	public CalendarEventReminderWindow(ReminderModel reminder, bool locked)
	{
		InitializeComponent();
		_reminder = reminder;
		base.DataContext = new CalendarReminderViewModel
		{
			Id = reminder.EventId,
			Summary = (locked ? Utils.GetString("ReminderWhenLock") : reminder.Title),
			Description = (locked ? string.Empty : reminder.Content)
		};
		base.SourceInitialized += OnWindowSourceInitialized;
		if (locked)
		{
			TitleBorder.SetValue(Grid.RowSpanProperty, 2);
			TitleText.Margin = new Thickness(10.0, 20.0, 10.0, 20.0);
			Buttons.Visibility = Visibility.Collapsed;
		}
		base.Left = SystemParameters.WorkArea.Width - base.Width;
		base.Top = SystemParameters.WorkArea.Height - 130.0 - 100.0;
		SetWindowIcon();
		GlobalEventManager.RemindHandled += OnRemindHandled;
	}

	private void OnRemindHandled(object sender, RemindMessage e)
	{
	}

	private void OnWindowSourceInitialized(object sender, EventArgs e)
	{
		IntPtr handle = new WindowInteropHelper(this).Handle;
		if (handle != IntPtr.Zero)
		{
			NativeUtils.ShowWindow(handle, 4);
		}
	}

	private void SetWindowIcon()
	{
		WindowIcon.Source = AppIconUtils.GetIconImage();
	}

	private void OnCloseClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private async void OnViewDetailClick(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext != null && base.DataContext is CalendarReminderViewModel calendarReminderViewModel)
		{
			CalendarEventModel calendarEventModel = await CalendarEventDao.GetEventByEventId(calendarReminderViewModel.Id);
			if (calendarEventModel != null)
			{
				App.NavigateEvent(calendarEventModel.Id);
			}
		}
		Close();
	}

	private async void OnViewButtonClick(object sender, RoutedEventArgs e)
	{
		if (base.DataContext != null && base.DataContext is CalendarReminderViewModel calendarReminderViewModel)
		{
			CalendarEventModel calendarEventModel = await CalendarEventDao.GetEventByEventId(calendarReminderViewModel.Id);
			if (calendarEventModel != null)
			{
				App.NavigateEvent(calendarEventModel.Id);
			}
		}
		Close();
	}

	private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (Mouse.LeftButton == MouseButtonState.Pressed)
		{
			DragMove();
		}
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		Communicator.TryNotifyCloseReminder(new RemindMessage(_reminder));
		GlobalEventManager.RemindHandled -= OnRemindHandled;
		base.OnClosing(e);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/calendar/calendareventreminderwindow.xaml", UriKind.Relative);
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
			WindowIcon = (Image)target;
			break;
		case 2:
			((Button)target).Click += OnCloseClick;
			break;
		case 3:
			((Grid)target).PreviewMouseDown += OnPreviewMouseDown;
			break;
		case 4:
			TitleBorder = (Border)target;
			TitleBorder.MouseLeftButtonUp += OnViewDetailClick;
			break;
		case 5:
			TitleText = (EmjTextBlock)target;
			break;
		case 6:
			((ScrollViewer)target).MouseLeftButtonUp += OnViewDetailClick;
			break;
		case 7:
			Buttons = (Grid)target;
			break;
		case 8:
			((Button)target).Click += OnViewButtonClick;
			break;
		case 9:
			((Button)target).Click += OnCloseClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
