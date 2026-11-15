using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.MainListView.DetailView.GoogleEventInfoBlock;

public class TaskGoogleEventInfoPanel : UserControl, IComponentConnector
{
	private sealed class AttendeeDisplayItem
	{
		public string Name { get; set; }

		public string Domain { get; set; }

		public string Suffix { get; set; }

		public string FullText { get; set; }
	}

	private const int MaxVisibleAttendees = 5;

	private TaskGoogleEventInfo _model;

	internal Grid MeetingRow;

	internal TextBlock MeetingValue;

	internal Border MeetingButton;

	internal Grid AttendeesRow;

	internal ItemsControl AttendeesItems;

	internal StackPanel LocationSection;

	internal TextBlock LocationValue;

	internal Border OpenButton;

	private bool _contentLoaded;

	public TaskGoogleEventInfoPanel()
	{
		InitializeComponent();
	}

	public void SetModel(TaskGoogleEventInfo model)
	{
		_model = model;
		if (model == null)
		{
			MeetingRow.Visibility = Visibility.Collapsed;
			AttendeesRow.Visibility = Visibility.Collapsed;
			LocationSection.Visibility = Visibility.Collapsed;
			OpenButton.Visibility = Visibility.Collapsed;
			AttendeesItems.ItemsSource = null;
			return;
		}
		bool flag = model.meeting != null && (!string.IsNullOrWhiteSpace(model.meeting.name) || !string.IsNullOrWhiteSpace(model.meeting.url));
		MeetingRow.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
		if (flag)
		{
			MeetingValue.Text = (string.IsNullOrWhiteSpace(model.meeting.name) ? model.meeting.url : model.meeting.name);
			MeetingButton.Visibility = (string.IsNullOrWhiteSpace(model.meeting.url) ? Visibility.Collapsed : Visibility.Visible);
			MeetingButton.ToolTip = model.meeting.name;
		}
		List<AttendeeDisplayItem> visibleAttendees = GetVisibleAttendees(model.attendees);
		AttendeesRow.Visibility = ((visibleAttendees.Count <= 0) ? Visibility.Collapsed : Visibility.Visible);
		AttendeesItems.ItemsSource = visibleAttendees;
		LocationSection.Visibility = (string.IsNullOrWhiteSpace(model.location) ? Visibility.Collapsed : Visibility.Visible);
		LocationSection.Margin = ((flag || visibleAttendees.Count > 0) ? new Thickness(0.0, 14.0, 0.0, 0.0) : new Thickness(0.0));
		LocationValue.Text = model.location;
		OpenButton.Visibility = (string.IsNullOrWhiteSpace(model.htmlLink) ? Visibility.Collapsed : Visibility.Visible);
	}

	public double GetHeight()
	{
		if (base.Visibility != Visibility.Visible)
		{
			return 0.0;
		}
		return base.ActualHeight + 26.0;
	}

	private void OnMeetingButtonClick(object sender, MouseButtonEventArgs eventArgs)
	{
		Utils.TryProcessStartUrl(_model?.meeting?.url);
	}

	private void OnOpenButtonClick(object sender, MouseButtonEventArgs eventArgs)
	{
		Utils.TryProcessStartUrl(_model?.htmlLink);
	}

	private static List<AttendeeDisplayItem> GetVisibleAttendees(IEnumerable<TaskGoogleEventAttendee> attendees)
	{
		List<TaskGoogleEventAttendee> obj = (from attendee in attendees?.Where((TaskGoogleEventAttendee attendee) => attendee != null && (!string.IsNullOrWhiteSpace(attendee.displayName) || !string.IsNullOrWhiteSpace(attendee.email)))
			orderby GetResponseStatusOrder(attendee.responseStatus)
			select attendee).ToList() ?? new List<TaskGoogleEventAttendee>();
		List<AttendeeDisplayItem> list = (from attendee in obj.Take(5)
			select BuildAttendeeDisplayItem(string.IsNullOrWhiteSpace(attendee.displayName) ? attendee.email : attendee.displayName)).ToList();
		if (obj.Count > 5 && list.Count > 0)
		{
			list[list.Count - 1].Suffix = "···";
		}
		return list;
	}

	private static AttendeeDisplayItem BuildAttendeeDisplayItem(string text)
	{
		int num = text?.LastIndexOf('@') ?? (-1);
		if (num <= 0)
		{
			return new AttendeeDisplayItem
			{
				Name = text,
				FullText = text
			};
		}
		return new AttendeeDisplayItem
		{
			Name = text.Substring(0, num),
			Domain = text.Substring(num),
			FullText = text
		};
	}

	private static int GetResponseStatusOrder(string responseStatus)
	{
		return responseStatus?.ToLowerInvariant() switch
		{
			"accepted" => 0, 
			"declined" => 1, 
			"tentative" => 2, 
			"needsaction" => 3, 
			_ => 4, 
		};
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/mainlistview/detailview/googleeventinfoblock/taskgoogleeventinfopanel.xaml", UriKind.Relative);
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
			MeetingRow = (Grid)target;
			break;
		case 2:
			MeetingValue = (TextBlock)target;
			break;
		case 3:
			MeetingButton = (Border)target;
			MeetingButton.MouseLeftButtonUp += OnMeetingButtonClick;
			break;
		case 4:
			AttendeesRow = (Grid)target;
			break;
		case 5:
			AttendeesItems = (ItemsControl)target;
			break;
		case 6:
			LocationSection = (StackPanel)target;
			break;
		case 7:
			LocationValue = (TextBlock)target;
			break;
		case 8:
			OpenButton = (Border)target;
			OpenButton.MouseLeftButtonUp += OnOpenButtonClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
