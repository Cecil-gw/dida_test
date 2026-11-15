using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using TickTickUtils.Lang;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Calendar;

public class CalendarDetailWindow : EscPopup, IComponentConnector
{
	public static bool PopupShowing;

	private bool _inOperate;

	private bool _skipCreateNewEventOnClose;

	private UIElement _target;

	internal CalendarDetailControl CalendarDetail;

	private bool _contentLoaded;

	public bool IsConvertingNewEventToTaskKind { get; private set; }

	public event EventHandler<bool> EventArchiveChanged;

	public event EventHandler<CalendarEventModel> EventCreated;

	public event EventHandler<CalendarDetailConvertEventArgs> ConvertNewEventToTaskKind;

	public event EventHandler<string> Disappear;

	public CalendarDetailWindow(string themeId = "")
	{
		if (!string.IsNullOrEmpty(themeId))
		{
			ThemeUtil.SetTheme(themeId, this);
		}
		InitializeComponent();
		InitEvents();
		base.InputBindings.Add(new KeyBinding(WindowCommands.EscCommand, new KeyGesture(Key.Escape))
		{
			CommandParameter = this
		});
		base.Closed += OnClosed;
	}

	public void FocusTitleEnd()
	{
		CalendarDetail.FocusTitleEnd();
	}

	private void InitEvents()
	{
		CalendarEventChangeNotifier.Deleted -= OnEventDeleted;
		CalendarEventChangeNotifier.Deleted += OnEventDeleted;
		CalendarDetail.InOperate -= OnCalendarOperate;
		CalendarDetail.InOperate += OnCalendarOperate;
		CalendarDetail.EventArchiveChanged -= OnEventArchivedChanged;
		CalendarDetail.EventArchiveChanged += OnEventArchivedChanged;
		CalendarDetail.EventCreated -= OnEventCreated;
		CalendarDetail.EventCreated += OnEventCreated;
		CalendarDetail.ConvertNewEventToTaskKind -= OnConvertNewEventToTaskKind;
		CalendarDetail.ConvertNewEventToTaskKind += OnConvertNewEventToTaskKind;
		CalendarDetail.StopOperate -= OnCalendarStopOperate;
		CalendarDetail.StopOperate += OnCalendarStopOperate;
	}

	private void OnEventCreated(object sender, CalendarEventModel e)
	{
		this.EventCreated?.Invoke(sender, e);
		base.IsOpen = false;
	}

	private void OnConvertNewEventToTaskKind(object sender, CalendarDetailConvertEventArgs e)
	{
		EventHandler<CalendarDetailConvertEventArgs> convertHandler = this.ConvertNewEventToTaskKind;
		if (convertHandler != null)
		{
			_skipCreateNewEventOnClose = true;
			IsConvertingNewEventToTaskKind = true;
			base.RestoreFocus = false;
			base.IsOpen = false;
			base.Dispatcher.BeginInvoke((Action)delegate
			{
				convertHandler(sender, e);
			});
		}
	}

	private void OnEventArchivedChanged(object sender, bool e)
	{
		this.EventArchiveChanged?.Invoke(sender, e);
	}

	private void OnCalendarStopOperate(object sender, EventArgs e)
	{
		_inOperate = false;
	}

	private void OnCalendarOperate(object sender, EventArgs e)
	{
		_inOperate = true;
	}

	private void OnEventDeleted(object sender, string eventId)
	{
		base.IsOpen = false;
	}

	public async void Show(UIElement target, double targetWidth, double addHeight, bool byMouse, CalendarDetailViewModel model, Point point = default(Point), bool useAbsolutePoint = false)
	{
		PopupStateManager.LastTarget = target;
		_target = target;
		base.PlacementTarget = target;
		CalendarDetail.DataContext = model;
		TaskPopupArgs popupLocation = PopupLocationCalculator.GetPopupLocation(_target, targetWidth, base.Width, byMouse, addHeight);
		if (useAbsolutePoint || point != default(Point))
		{
			base.Placement = (useAbsolutePoint ? PlacementMode.AbsolutePoint : PlacementMode.RelativePoint);
			base.HorizontalOffset = point.X;
			base.VerticalOffset = point.Y;
		}
		else if (!popupLocation.ByMouse)
		{
			base.Placement = (popupLocation.IsRight ? PlacementMode.Right : PlacementMode.Left);
			base.HorizontalOffset = ((!popupLocation.IsRight) ? 6 : 0);
			base.VerticalOffset = -4.0;
		}
		else
		{
			base.Placement = PlacementMode.Mouse;
		}
		PopupStateManager.OnViewPopupOpened();
		base.IsOpen = true;
		await Task.Delay(200);
		if (base.IsOpen)
		{
			PopupShowing = true;
			PopupStateManager.OnViewPopupOpened();
		}
	}

	private async void OnClosed(object sender, EventArgs e)
	{
		PopupShowing = false;
		PopupStateManager.SuppressMonthSelection();
		if (!_skipCreateNewEventOnClose)
		{
			try
			{
				await CalendarDetail.TryCreateNewEventOnLostFocusAsync();
			}
			catch (Exception ex)
			{
				UtilLog.Warn("CalendarDetailWindow.OnClosed TryCreateNewEventOnLostFocusAsync failed: " + ex);
			}
		}
		if (PopupStateManager.LastTarget == _target)
		{
			PopupStateManager.LastTarget = null;
		}
		PopupStateManager.OnViewPopupClosed(needWait: false);
		this.Disappear?.Invoke(this, null);
		CalendarEventChangeNotifier.Deleted -= OnEventDeleted;
		CalendarDetail.InOperate -= OnCalendarOperate;
		CalendarDetail.StopOperate -= OnCalendarStopOperate;
		CalendarEventChangeNotifier.Deleted -= OnEventDeleted;
		CalendarDetail.InOperate -= OnCalendarOperate;
		CalendarDetail.EventArchiveChanged -= OnEventArchivedChanged;
		CalendarDetail.EventCreated -= OnEventCreated;
		CalendarDetail.ConvertNewEventToTaskKind -= OnConvertNewEventToTaskKind;
		CalendarDetail.StopOperate -= OnCalendarStopOperate;
		this.EventArchiveChanged = null;
		this.EventCreated = null;
		this.ConvertNewEventToTaskKind = null;
		this.Disappear = null;
	}

	private void OnWindowKeyDown(object sender, KeyEventArgs e)
	{
		if (object.Equals(FocusManager.GetFocusedElement(this), this))
		{
			Key key = e.Key;
			if ((key == Key.Up || key == Key.Down) && _target is IShowTaskDetailWindow showTaskDetailWindow)
			{
				showTaskDetailWindow.OnDetailKeyUp(e.Key);
			}
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/calendar/calendardetailwindow.xaml", UriKind.Relative);
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
		if (connectionId == 1)
		{
			CalendarDetail = (CalendarDetailControl)target;
		}
		else
		{
			_contentLoaded = true;
		}
	}
}
