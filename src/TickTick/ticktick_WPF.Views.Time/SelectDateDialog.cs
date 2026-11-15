using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Time;

public class SelectDateDialog : UserControl, IComponentConnector
{
	private bool _isStart;

	private DurationModel _model;

	private readonly Popup _popup;

	private TickDatePicker _calendar;

	internal Grid CalendarGrid;

	private bool _contentLoaded;

	public bool IsOpen => _popup.IsOpen;

	public DateTime? SelectStart => _calendar.SelectStart;

	public DateTime? SelectEnd => _calendar.SelectEnd;

	public event EventHandler<DateTime> SelectDate;

	public event EventHandler<DateTime> SelectDateAndSave;

	public SelectDateDialog()
		: this(new EscPopup
		{
			StaysOpen = true,
			Placement = PlacementMode.MousePoint,
			VerticalOffset = -20.0,
			HorizontalOffset = -20.0
		})
	{
	}

	private SelectDateDialog(Popup popup)
		: this(popup, new DurationModel
		{
			SelectedDate = DateTime.Now.Date
		}, isStart: true)
	{
	}

	public SelectDateDialog(Popup popup, DurationModel model, bool isStart, DateTime? maxDate = null, DateTime? minDate = null, bool isChooseDate = false)
	{
		_popup = popup;
		_model = model;
		_isStart = isStart;
		InitializeComponent();
		InitCalendar(maxDate, isStart ? ((DateTime?)null) : minDate, isChooseDate);
	}

	private void InitCalendar(DateTime? maxDate = null, DateTime? minDate = null, bool isChooseDate = false)
	{
		_calendar = new TickDatePicker(_model.SelectedDate ?? DateTime.Now.Date, _model.SelectionStart, _model.SelectionEnd, _isStart, isMonthMode: false, maxDate, minDate)
		{
			IsChooseDate = isChooseDate
		};
		CalendarGrid.Children.Add(_calendar);
		_calendar.SelectedDateChanged -= OnDateSelect;
		_calendar.SelectedDateChanged += OnDateSelect;
		_calendar.DateSelected -= OnDateSelectAndSave;
		_calendar.DateSelected += OnDateSelectAndSave;
	}

	private void OnDateSelectAndSave(object sender, DateTime selectedDate)
	{
		_popup.IsOpen = false;
		this.SelectDateAndSave?.Invoke(this, selectedDate);
	}

	private void OnDateSelect(object sender, DateTime selectedDate)
	{
		_popup.IsOpen = false;
		this.SelectDate?.Invoke(this, selectedDate);
	}

	public void Show()
	{
		_popup.Child = this;
		_popup.IsOpen = true;
		_popup.Focus();
	}

	public void SetData(DurationModel model, bool isStart, DateTime? maxDate = null, DateTime? minDate = null)
	{
		_model = model;
		_isStart = isStart;
		if (_calendar == null)
		{
			InitCalendar(maxDate, minDate);
		}
		else
		{
			_calendar.SetData(_model.SelectedDate ?? DateTime.Now.Date, _model.SelectionStart, _model.SelectionEnd, _isStart, isMonthMode: false, maxDate, minDate);
		}
	}

	public void HandleLeftRight(bool isLeft)
	{
		_calendar?.MoveTabSelectDate((!isLeft) ? 1 : (-1));
	}

	public void HandleUpDown(bool isUp)
	{
		_calendar?.MoveTabSelectDate(isUp ? (-7) : 7);
	}

	public void SetCurrentTab(bool inTab)
	{
		if (inTab)
		{
			_calendar?.ClearTabSelected();
			_calendar?.TabSelectCurrent();
		}
		else
		{
			_calendar?.ClearTabSelected();
		}
	}

	public void EnterSelect()
	{
		_calendar?.SelectTabItem();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/time/selectdatedialog.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 1)
		{
			CalendarGrid = (Grid)target;
		}
		else
		{
			_contentLoaded = true;
		}
	}
}
