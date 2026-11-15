using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Time;

namespace ticktick_WPF.Views.Search;

public class SelectTimeSpanDialog : UserControl, IComponentConnector
{
	private readonly SelectTimeSpanViewModel _viewModel;

	private DateTime _ignoreStartDateHitAreaMouseUpUntil = DateTime.MinValue;

	private DateTime _ignoreEndDateHitAreaMouseUpUntil = DateTime.MinValue;

	private bool _ignoreNextStartDateHitAreaMouseUp;

	private bool _ignoreNextEndDateHitAreaMouseUp;

	internal Grid TimeSpanGrid;

	internal TextBox StartDate;

	internal TextBox EndDate;

	internal TextBox FocusBox;

	internal Grid StartDateHitArea;

	internal Grid EndDateHitArea;

	internal Button SaveBtn;

	internal Button CancelBtn;

	internal Popup StartDatePopup;

	internal TickDatePicker StartDateCalendar;

	internal Popup EndDatePopup;

	internal TickDatePicker EndDateCalendar;

	private bool _contentLoaded;

	public event EventHandler<SelectTimeSpanViewModel> OnSpanSelect;

	public event EventHandler Cancel;

	public event EventHandler OnEndEdit;

	public SelectTimeSpanDialog()
		: this(new SelectTimeSpanViewModel())
	{
	}

	private SelectTimeSpanDialog(SelectTimeSpanViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		base.DataContext = _viewModel;
		StartDateCalendar.SelectedDate = _viewModel.StartDate;
		EndDateCalendar.SelectedDate = _viewModel.EndDate;
		StartDateCalendar.SelectedDateChanged += delegate(object sender, DateTime date)
		{
			_viewModel.StartDate = date;
			StartDatePopup.IsOpen = false;
			CheckValidTimeSpan();
			this.OnEndEdit?.Invoke(this, null);
		};
		EndDateCalendar.SelectedDateChanged += delegate(object sender, DateTime date)
		{
			_viewModel.EndDate = date;
			EndDatePopup.IsOpen = false;
			CheckValidTimeSpan();
			this.OnEndEdit?.Invoke(this, null);
		};
		StartDatePopup.Closed += StartDatePopupClosed;
		EndDatePopup.Closed += EndDatePopupClosed;
	}

	public void SetStartDate(DateTime? startDate)
	{
		_viewModel.StartDate = startDate;
	}

	private void CheckValidTimeSpan(bool needToast = true)
	{
		if (!Utils.IsEmptyDate(_viewModel.StartDate) && !Utils.IsEmptyDate(_viewModel.EndDate) && _viewModel.StartDate.HasValue && _viewModel.EndDate.HasValue)
		{
			if (DateTime.Compare(_viewModel.StartDate.Value, _viewModel.EndDate.Value) <= 0)
			{
				SaveBtn.IsEnabled = true;
				return;
			}
			if (needToast)
			{
				Utils.Toast(Utils.GetString("InvalidTimeSpan"));
			}
		}
		SaveBtn.IsEnabled = false;
	}

	public void SetEndDate(DateTime? endDate)
	{
		_viewModel.EndDate = endDate;
		CheckValidTimeSpan(needToast: false);
	}

	public bool IsMouseOverDatePopup()
	{
		if (!StartDatePopup.IsMouseOver && !EndDatePopup.IsMouseOver && !IsMouseOverElement(StartDatePopup.Child as FrameworkElement))
		{
			return IsMouseOverElement(EndDatePopup.Child as FrameworkElement);
		}
		return true;
	}

	private static bool IsMouseOverElement(FrameworkElement element)
	{
		if (element == null || !element.IsVisible || element.ActualWidth <= 0.0 || element.ActualHeight <= 0.0)
		{
			return false;
		}
		Point position = Mouse.GetPosition(element);
		if (position.X >= 0.0 && position.Y >= 0.0 && position.X <= element.ActualWidth)
		{
			return position.Y <= element.ActualHeight;
		}
		return false;
	}

	private void SaveBtnClick(object sender, RoutedEventArgs e)
	{
		CloseDatePopups();
		this.OnSpanSelect?.Invoke(this, _viewModel);
		e.Handled = true;
	}

	private void CancelBtnClick(object sender, RoutedEventArgs e)
	{
		CloseDatePopups();
		this.Cancel?.Invoke(this, null);
		e.Handled = true;
	}

	private void StartDateGotFocus(object sender, RoutedEventArgs e)
	{
		if (!ConsumeIgnoredDateHitAreaMouseUp(ref _ignoreNextStartDateHitAreaMouseUp, ref _ignoreStartDateHitAreaMouseUpUntil, e))
		{
			e.Handled = true;
			if (_viewModel.StartDate.HasValue)
			{
				StartDateCalendar.SelectedDate = _viewModel.StartDate.Value;
			}
			EndDatePopup.IsOpen = false;
			StartDatePopup.IsOpen = !StartDatePopup.IsOpen;
		}
	}

	private void EndDateGotFocus(object sender, RoutedEventArgs e)
	{
		if (!ConsumeIgnoredDateHitAreaMouseUp(ref _ignoreNextEndDateHitAreaMouseUp, ref _ignoreEndDateHitAreaMouseUpUntil, e))
		{
			e.Handled = true;
			if (_viewModel.EndDate.HasValue)
			{
				EndDateCalendar.SelectedDate = _viewModel.EndDate.Value;
			}
			StartDatePopup.IsOpen = false;
			EndDatePopup.IsOpen = !EndDatePopup.IsOpen;
		}
	}

	private void OnKeyUp(object sender, KeyEventArgs e)
	{
		switch (e.Key)
		{
		case Key.Return:
			if (SaveBtn.IsEnabled)
			{
				CloseDatePopups();
				this.OnSpanSelect?.Invoke(this, _viewModel);
			}
			break;
		case Key.Escape:
			if (StartDatePopup.IsOpen || EndDatePopup.IsOpen)
			{
				CloseDatePopups();
				e.Handled = true;
			}
			else
			{
				this.Cancel?.Invoke(this, null);
			}
			break;
		}
	}

	private void CloseDatePopups()
	{
		StartDatePopup.IsOpen = false;
		EndDatePopup.IsOpen = false;
	}

	private void StartDatePopupClosed(object sender, EventArgs e)
	{
		IgnoreDateHitAreaMouseUpIfPressedOver(StartDateHitArea, ref _ignoreNextStartDateHitAreaMouseUp, ref _ignoreStartDateHitAreaMouseUpUntil);
	}

	private void EndDatePopupClosed(object sender, EventArgs e)
	{
		IgnoreDateHitAreaMouseUpIfPressedOver(EndDateHitArea, ref _ignoreNextEndDateHitAreaMouseUp, ref _ignoreEndDateHitAreaMouseUpUntil);
	}

	private static void IgnoreDateHitAreaMouseUpIfPressedOver(FrameworkElement hitArea, ref bool ignoreNextMouseUp, ref DateTime ignoreMouseUpUntil)
	{
		if (Mouse.LeftButton == MouseButtonState.Pressed && IsMouseOverElement(hitArea))
		{
			ignoreNextMouseUp = true;
			ignoreMouseUpUntil = DateTime.UtcNow.AddSeconds(30.0);
		}
	}

	private static bool ConsumeIgnoredDateHitAreaMouseUp(ref bool ignoreNextMouseUp, ref DateTime ignoreMouseUpUntil, RoutedEventArgs e)
	{
		if (!ignoreNextMouseUp)
		{
			return false;
		}
		if (ignoreMouseUpUntil <= DateTime.UtcNow)
		{
			ignoreNextMouseUp = false;
			return false;
		}
		ignoreNextMouseUp = false;
		ignoreMouseUpUntil = DateTime.MinValue;
		e.Handled = true;
		return true;
	}

	private void OnMouseUp(object sender, MouseButtonEventArgs e)
	{
		FocusBox.Focus();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/search/selecttimespandialog.xaml", UriKind.Relative);
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
			((SelectTimeSpanDialog)target).PreviewKeyUp += OnKeyUp;
			((SelectTimeSpanDialog)target).PreviewMouseLeftButtonUp += OnMouseUp;
			break;
		case 2:
			TimeSpanGrid = (Grid)target;
			break;
		case 3:
			StartDate = (TextBox)target;
			break;
		case 4:
			EndDate = (TextBox)target;
			break;
		case 5:
			FocusBox = (TextBox)target;
			FocusBox.PreviewKeyUp += OnKeyUp;
			break;
		case 6:
			StartDateHitArea = (Grid)target;
			StartDateHitArea.MouseLeftButtonUp += StartDateGotFocus;
			break;
		case 7:
			EndDateHitArea = (Grid)target;
			EndDateHitArea.MouseLeftButtonUp += EndDateGotFocus;
			break;
		case 8:
			SaveBtn = (Button)target;
			SaveBtn.PreviewMouseLeftButtonUp += SaveBtnClick;
			break;
		case 9:
			CancelBtn = (Button)target;
			CancelBtn.PreviewMouseLeftButtonUp += CancelBtnClick;
			break;
		case 10:
			StartDatePopup = (Popup)target;
			break;
		case 11:
			StartDateCalendar = (TickDatePicker)target;
			break;
		case 12:
			EndDatePopup = (Popup)target;
			break;
		case 13:
			EndDateCalendar = (TickDatePicker)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
