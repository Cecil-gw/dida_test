using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Rendering;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.Util.DateParser;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.MainListView.DetailView;
using ticktick_WPF.Views.MarkDown;

namespace ticktick_WPF.Views.Detail;

public class DetailTextBox : LinkTextEditBox, ILinkMenuHost
{
	private Popup _linkMenuPopup;

	private EditorMenuLink _linkMenu;

	private LinkLineInfoModel _hoverLink;

	private int _linkMenuVersion;

	private Point _linkMenuScreenPosition;

	private Window _linkMenuOwner;

	public TitleDateParseMode DateParseMode { get; set; }

	public DateTime DateParseBaseDate { get; set; } = DateTime.Today;

	public DetailTextBox()
	{
		base.Margin = new Thickness(2.0, 2.0, 0.0, 0.0);
		SetMarkRegexText(unable: false);
		base.ContextMenuOpening += base.EditorMenuOnContextMenuOpening;
		base.TextChanged += delegate
		{
			CloseLinkMenu();
		};
		base.DataContextChanged += delegate
		{
			CloseLinkMenu();
		};
		base.IsEnabledChanged += delegate
		{
			CloseLinkMenu();
		};
		base.IsVisibleChanged += delegate
		{
			CloseLinkMenu();
		};
		base.PreviewMouseDown += delegate
		{
			CloseLinkMenu();
		};
		base.PreviewKeyDown += delegate
		{
			CloseLinkMenu();
		};
		base.MouseLeave += delegate
		{
			HideLinkMenuPopup();
		};
		base.TextArea.TextView.ScrollOffsetChanged += delegate
		{
			CloseLinkMenu();
		};
		base.SizeChanged += delegate
		{
			CloseLinkMenu();
		};
		base.Unloaded += delegate
		{
			CloseLinkMenu();
		};
	}

	private bool CanShowLinkMenu(LinkLineInfoModel link)
	{
		if (base.IsLoaded && base.IsVisible && base.IsEnabled && !base.ReadOnly && !base.IsReadOnly && base.SelectionLength == 0 && link != null && base.Document != null && link.TryResolveStartIndex(out var startIndex) && startIndex >= 0)
		{
			return startIndex < base.Document.TextLength;
		}
		return false;
	}

	public async Task TryShowLinkMenuPopup(LinkLineInfoModel linkLineInfo)
	{
		if (!CanShowLinkMenu(linkLineInfo) || Mouse.LeftButton != MouseButtonState.Released)
		{
			return;
		}
		if ((_linkMenuPopup?.IsOpen ?? false) && _hoverLink?.StartIndex == linkLineInfo.StartIndex)
		{
			_linkMenuVersion++;
			return;
		}
		CloseLinkMenu();
		int version = _linkMenuVersion;
		Point start = Mouse.GetPosition(this);
		await Task.Delay(300);
		if (version != _linkMenuVersion || !base.IsMouseOver || !CanShowLinkMenu(linkLineInfo) || Mouse.LeftButton != MouseButtonState.Released)
		{
			return;
		}
		Point position = Mouse.GetPosition(this);
		if ((position - start).LengthSquared > 10.0 || !base.TextArea.TextView.VisualLinesValid)
		{
			return;
		}
		if (_linkMenuPopup == null)
		{
			_linkMenu = new EditorMenuLink();
			_linkMenu.EditorAction += OnLinkMenuAction;
			_linkMenuPopup = new Popup
			{
				PlacementTarget = this,
				Placement = PlacementMode.Relative,
				AllowsTransparency = true,
				StaysOpen = true,
				PopupAnimation = PopupAnimation.Fade,
				Child = _linkMenu
			};
		}
		VisualLine visualLine = base.TextArea.TextView.GetVisualLine(base.Document.GetLineByOffset(linkLineInfo.StartIndex).LineNumber);
		if (visualLine != null && PresentationSource.FromVisual(this) != null)
		{
			linkLineInfo.VisualLine = visualLine;
			_linkMenu.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			_linkMenuPopup.HorizontalOffset = Math.Max(0.0, Math.Min(position.X - _linkMenu.DesiredSize.Width / 2.0, base.ActualWidth - _linkMenu.DesiredSize.Width));
			TextView textView = base.TextArea.TextView;
			int num = ((linkLineInfo.Source != LinkInfoSource.Link) ? (linkLineInfo.Text?.Length ?? 0) : (linkLineInfo.Url?.Length ?? 0));
			int offset = Math.Min(base.Document.TextLength, linkLineInfo.StartIndex + num / 2);
			TextViewPosition position2 = new TextViewPosition(base.Document.GetLocation(linkLineInfo.StartIndex));
			TextViewPosition position3 = new TextViewPosition(base.Document.GetLocation(offset));
			Point visualPosition = textView.GetVisualPosition(position2, VisualYPosition.LineTop);
			Point visualPosition2 = textView.GetVisualPosition(position3, VisualYPosition.LineTop);
			double num2 = ((visualPosition.Y < visualPosition2.Y) ? (textView.GetVisualPosition(position3, VisualYPosition.LineBottom).Y - 8.0) : (visualPosition2.Y - 50.0));
			_linkMenuPopup.VerticalOffset = base.TextArea.TextView.TranslatePoint(new Point(0.0, num2 - textView.ScrollOffset.Y), this).Y;
			_hoverLink = linkLineInfo;
			_linkMenuScreenPosition = PointToScreen(default(Point));
			_linkMenuOwner = Window.GetWindow(this);
			if (_linkMenuOwner != null)
			{
				_linkMenuOwner.Deactivated += OnLinkMenuOwnerDeactivated;
			}
			base.LayoutUpdated += OnLinkMenuLayoutUpdated;
			_linkMenuPopup.IsOpen = true;
		}
	}

	public async Task HideLinkMenuPopup()
	{
		int version = ++_linkMenuVersion;
		await Task.Delay(150);
		if (version == _linkMenuVersion)
		{
			EditorMenuLink linkMenu = _linkMenu;
			if (linkMenu == null || !linkMenu.IsMouseOver)
			{
				CloseLinkMenu();
			}
		}
	}

	internal void CloseLinkMenu()
	{
		_linkMenuVersion++;
		_hoverLink = null;
		if (_linkMenuPopup != null)
		{
			_linkMenuPopup.IsOpen = false;
		}
		base.LayoutUpdated -= OnLinkMenuLayoutUpdated;
		if (_linkMenuOwner != null)
		{
			_linkMenuOwner.Deactivated -= OnLinkMenuOwnerDeactivated;
		}
		_linkMenuOwner = null;
	}

	private void OnLinkMenuOwnerDeactivated(object sender, EventArgs e)
	{
		CloseLinkMenu();
	}

	private void OnLinkMenuLayoutUpdated(object sender, EventArgs e)
	{
		if (!CanShowLinkMenu(_hoverLink) || PresentationSource.FromVisual(this) == null || (PointToScreen(default(Point)) - _linkMenuScreenPosition).LengthSquared > 1.0)
		{
			CloseLinkMenu();
		}
	}

	private void OnLinkMenuAction(object sender, string action)
	{
		LinkLineInfoModel hoverLink = _hoverLink;
		bool flag = CanShowLinkMenu(hoverLink);
		CloseLinkMenu();
		if (action == "Edit" && flag)
		{
			ShowInsertLink(hoverLink);
		}
		else if (action == "Copy" && !string.IsNullOrEmpty(hoverLink?.Url))
		{
			try
			{
				Clipboard.SetText(hoverLink.Url);
				Utils.FindParent<IToastShowWindow>(this)?.TryToastString(null, Utils.GetString("Copied"));
			}
			catch (ExternalException)
			{
			}
		}
	}

	private TaskBaseViewModel GetTaskBaseModel()
	{
		if (Utils.FindParent<TaskDetailView>(this)?.DataContext is TaskDetailViewModel taskDetailViewModel)
		{
			return taskDetailViewModel.SourceViewModel;
		}
		return null;
	}

	protected override string GetProjectId()
	{
		return GetTaskBaseModel()?.ProjectId;
	}

	protected override string GetColumnId()
	{
		return GetTaskBaseModel()?.ColumnId;
	}

	protected override string GetAssignee()
	{
		return GetTaskBaseModel()?.Assignee;
	}

	protected override int GetPriority()
	{
		return GetTaskBaseModel()?.Priority ?? (-1);
	}

	protected override List<string> GetTags()
	{
		return GetTaskBaseModel()?.Tags?.ToList();
	}

	protected override IPaserDueDate HandleParsedDate(IPaserDueDate parsedData, string content)
	{
		if (DateParseMode != TitleDateParseMode.TimeOnlyOnCurrentDate)
		{
			return parsedData;
		}
		return FilterCalendarTimeOnlyDateParse(parsedData, content);
	}

	protected override DateTime? GetDateParseBaseDate()
	{
		if (DateParseMode != TitleDateParseMode.TimeOnlyOnCurrentDate)
		{
			return null;
		}
		return DateParseBaseDate;
	}

	private IPaserDueDate FilterCalendarTimeOnlyDateParse(IPaserDueDate parsedData, string content)
	{
		if (parsedData == null)
		{
			return null;
		}
		TimeData timeData = parsedData.ToTimeData(addDefaultReminder: true);
		if (timeData == null || !timeData.StartDate.HasValue || timeData.IsAllDay == true)
		{
			return null;
		}
		if (timeData.StartDate.Value.Date != DateParseBaseDate.Date)
		{
			return null;
		}
		DateTime dateTime = DateParseBaseDate.Date.Add(timeData.StartDate.Value.TimeOfDay);
		DateTime? dueDate = null;
		if (timeData.DueDate.HasValue)
		{
			TimeSpan timeSpan = timeData.DueDate.Value - timeData.StartDate.Value;
			dueDate = ((timeSpan >= TimeSpan.Zero) ? dateTime.Add(timeSpan) : dateTime);
		}
		List<string> list = (from token in parsedData.GetRecognizeStrings()?.Where((string token) => !string.IsNullOrWhiteSpace(token))
			select token.Trim()).ToList() ?? new List<string>();
		if (!list.Any())
		{
			return null;
		}
		return new ParseDueDate
		{
			repeatFlag = string.Empty,
			startDate = dateTime,
			dueDate = dueDate,
			reminder = timeData.Reminders?.Select((TaskReminderModel reminder) => reminder.trigger).ToList(),
			recognizeStrings = list,
			isAllDay = false,
			Text = content
		};
	}
}
