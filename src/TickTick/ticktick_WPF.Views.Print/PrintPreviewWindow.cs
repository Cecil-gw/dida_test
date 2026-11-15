using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using System.Printing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media.Animation;
using System.Windows.Xps.Packaging;
using TickTickUtils.Lang;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.Calendar.Month;
using ticktick_WPF.Views.Calendar.Week;

namespace ticktick_WPF.Views.Print;

public class PrintPreviewWindow : MyWindow, IComponentConnector, IStyleConnector
{
	private FlowDocument _doc;

	private bool _printCal;

	private DocumentPaginator _pt;

	private PrintPreviewViewModel _viewModel;

	private Size _pageSize = new Size(1240.0, 1754.0);

	private WeekPrint _week;

	private int _pagesPerSheet;

	private Duplexing _duplexing = Duplexing.OneSided;

	private int _copyCount = 1;

	private MonthPrint _monthView;

	private DayPrint _day;

	private DocumentPaginator _dp;

	private string _summaryText;

	private TaskDetailPrintView _detailView;

	private TaskDetailPrintViewModel _detailModel;

	internal Grid Container;

	internal Grid Preview;

	internal DocumentViewer DocViewer;

	internal Grid MessageBorder;

	internal TextBlock PrintingText;

	internal TextBlock PrinterStatusText;

	private bool _contentLoaded;

	public PrintPreviewWindow(TaskDetailPrintViewModel model)
	{
		InitializeComponent();
		int num = 1112;
		int num2 = 786;
		_detailView = new TaskDetailPrintView(model, num2, num);
		Preview.Children.Add(_detailView);
		_detailView.UpdateLayout();
		_detailModel = model;
		_printCal = false;
		_doc = _detailView.Doc;
		DocViewer.FitToWidth();
	}

	public PrintPreviewWindow(TaskListPrintViewModel model, bool isDetailList = false)
	{
		InitializeComponent();
		TaskListPrintView taskListPrintView = new TaskListPrintView(model, isDetailList);
		_doc = taskListPrintView.RichTextBox.Document;
		DocViewer.FitToWidth();
	}

	public PrintPreviewWindow(string summaryText)
	{
		int num = 1112;
		int num2 = 786;
		InitializeComponent();
		_summaryText = summaryText;
		SummaryPrint summaryPrint = new SummaryPrint(summaryText, num2, num);
		Preview.Children.Add(summaryPrint);
		_printCal = false;
		summaryPrint.UpdateLayout();
		_doc = summaryPrint.Doc;
		DocViewer.FitToWidth();
	}

	public PrintPreviewWindow(List<List<WeekEventModel>> monthRowData, DateTime startDate)
	{
		InitializeComponent();
		DocViewer.FitToWidth();
		_monthView = new MonthPrint(monthRowData, startDate);
		_doc = _monthView.Doc;
		_doc.PageWidth = 1754.0;
		_doc.PageHeight = 1240.0;
		_printCal = true;
	}

	public PrintPreviewWindow(List<WeekEventModel> allDayModels, List<CalendarTimelineDayViewModel> pointModels, DateTime startDate, int days = 7, bool showWeekends = true)
	{
		int num = 1754;
		int num2 = 1240;
		InitializeComponent();
		_week = new WeekPrint(allDayModels, pointModels, num2, num, startDate, days, showWeekends);
		_doc = _week.Doc;
		_doc.PageWidth = num2;
		_doc.PageHeight = num;
		Preview.Children.Add(_week);
		_printCal = true;
		DocViewer.FitToWidth();
	}

	public PrintPreviewWindow(List<CalendarDisplayViewModel> allDayTasks, WeekDayCellViewModel pointModels, DateTime date, bool isAll)
	{
		int num = 1754;
		int num2 = 1240;
		InitializeComponent();
		_day = new DayPrint(allDayTasks, pointModels, num2, num, date, isAll);
		_doc = _day.Doc;
		_doc.PageWidth = num2;
		_doc.PageHeight = num;
		Preview.Children.Add(_day);
		_printCal = true;
		DocViewer.FitToWidth();
	}

	private void GetPaginator()
	{
		_dp = ((IDocumentPaginatorSource)_doc).DocumentPaginator;
		_dp.PageSize = _pageSize;
		_pt = new PaginatorHeaderFooter(_dp, _printCal ? 1250 : 793);
		_pt.GetPage(100);
		_viewModel.PageCount = _pt.PageCount;
		Preview.Children.Clear();
	}

	private async void PrintPreviewWindow_OnLoaded(object sender, RoutedEventArgs e)
	{
		if (!string.IsNullOrEmpty(_summaryText) || _detailView != null)
		{
			await Task.Delay(200);
			while (!(_detailView?.IsMdLoaded ?? true))
			{
				await Task.Delay(200);
			}
		}
		try
		{
			_viewModel = new PrintPreviewViewModel(((IDocumentPaginatorSource)_doc).DocumentPaginator, _monthView != null, !string.IsNullOrEmpty(_summaryText));
			if (DocViewer.Template.FindName("ToolBar", DocViewer) is ContentControl contentControl)
			{
				contentControl.DataContext = _viewModel;
				if (contentControl.Template.FindName("Orientation", contentControl) is ComboBox comboBox)
				{
					comboBox.SelectionChanged -= OnOrientationChanged;
					if (_viewModel.PageOrientation == PageOrientation.Landscape)
					{
						comboBox.SelectedIndex = 1;
					}
					comboBox.SelectionChanged += OnOrientationChanged;
				}
			}
			if (_viewModel.PageSize == null)
			{
				_viewModel.PageSize = new PageMediaSize(PageMediaSizeName.ISOA4, 794.0, 1123.0);
				_viewModel.PageMediaSizes = new List<PageMediaSize> { _viewModel.PageSize };
			}
			if (_viewModel.PageSize.Width.HasValue && _viewModel.PageSize.Height.HasValue)
			{
				_pageSize = new Size((int)_viewModel.PageSize.Width.Value, (int)_viewModel.PageSize.Height.Value);
			}
			SetDocumentViewer();
			_viewModel.NeedReload += ReloadViewer;
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex?.ToString() + string.Empty);
		}
		Activate();
	}

	private void SetPrintText()
	{
		if (DocViewer.Template.FindName("ToolBar", DocViewer) is ContentControl contentControl && contentControl.Template.FindName("PrintButton", contentControl) is Button button)
		{
			if (_viewModel.CurrentPrintQueue.QueueStatus == PrintQueueStatus.None)
			{
				button.Content = (_viewModel.CurrentPrintQueue.Name.ToLower().Contains("fax") ? Utils.GetString("Send") : ((_viewModel.CurrentPrintQueue.Name.ToLower().Contains("pdf") || _viewModel.CurrentPrintQueue.Name.ToLower().Contains("xps")) ? Utils.GetString("PublicSave") : Utils.GetString("Print")));
			}
			else
			{
				button.Content = Utils.GetString("Print");
			}
		}
	}

	private async void ReloadViewer(object sender, EventArgs e)
	{
		PrintPreviewViewModel viewModel = _viewModel;
		int num;
		if (viewModel == null)
		{
			num = 1;
		}
		else
		{
			PageMediaSize pageSize = viewModel.PageSize;
			num = ((pageSize == null || !pageSize.Width.HasValue) ? 1 : 0);
		}
		if (num != 0)
		{
			return;
		}
		PrintPreviewViewModel viewModel2 = _viewModel;
		int num2;
		if (viewModel2 == null)
		{
			num2 = 1;
		}
		else
		{
			PageMediaSize pageSize2 = viewModel2.PageSize;
			num2 = ((pageSize2 == null || !pageSize2.Height.HasValue) ? 1 : 0);
		}
		if (num2 != 0)
		{
			return;
		}
		if (_viewModel.PageOrientation == PageOrientation.Portrait)
		{
			_pageSize = new Size((int)_viewModel.PageSize.Width.Value, (int)_viewModel.PageSize.Height.Value);
		}
		else if (_viewModel.PageOrientation == PageOrientation.Landscape)
		{
			_pageSize = new Size((int)_viewModel.PageSize.Height.Value, (int)_viewModel.PageSize.Width.Value);
		}
		if (_monthView != null)
		{
			double num3 = 1.5762925598991173;
			_doc.PageWidth = _pageSize.Width * num3;
			_doc.ColumnWidth = _pageSize.Width * num3;
			_doc.PageHeight = _pageSize.Height * num3;
		}
		if (_day != null)
		{
			double num4 = 1.5762925598991173;
			Preview.Children.Clear();
			_day = new DayPrint(_day.AllDayTasks, _day.PointModel, _pageSize.Width * num4, _pageSize.Height * num4, _day.Date, _day.IsAll);
			Preview.Children.Add(_day);
			_doc = _day.Doc;
			await Task.Delay(200);
		}
		if (_week != null)
		{
			double num5 = 1.5762925598991173;
			Preview.Children.Clear();
			_week = new WeekPrint(_week.AllDayModels, _week.PointModels, _pageSize.Width * num5, _pageSize.Height * num5, _week.StartDate, _week.Days, _week.ShowWeekends);
			Preview.Children.Add(_week);
			_doc = _week.Doc;
			await Task.Delay(200);
		}
		if (!string.IsNullOrEmpty(_summaryText))
		{
			Preview.Children.Clear();
			SummaryPrint summary = new SummaryPrint(_summaryText, _pageSize.Width, _pageSize.Height);
			Preview.Children.Add(summary);
			await Task.Delay(200);
			_doc = summary.Doc;
			await Task.Delay(200);
		}
		if (_detailView != null)
		{
			_ = Preview.Height;
			_ = Preview.ActualHeight;
			Preview.Children.Clear();
			_detailView = new TaskDetailPrintView(_detailModel, _pageSize.Width, _pageSize.Height);
			Preview.Children.Add(_detailView);
			while (!(_detailView?.IsMdLoaded ?? true))
			{
				await Task.Delay(200);
			}
			_doc = _detailView.Doc;
			await Task.Delay(200);
		}
		SetDocumentViewer();
	}

	private async void SetDocumentViewer()
	{
		GetPaginator();
		if (_detailModel != null && _detailModel.Kind != "CHECKLIST")
		{
			await Task.Delay(300);
		}
		try
		{
			Package package = Package.Open(new MemoryStream(), FileMode.Create, FileAccess.ReadWrite);
			Uri uri = new Uri(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Tick_Tick\\print.xps");
			PackageStore.RemovePackage(uri);
			PackageStore.AddPackage(uri, package);
			XpsDocument xpsDocument = new XpsDocument(package, CompressionOption.Fast, uri.AbsoluteUri);
			XpsDocument.CreateXpsDocumentWriter(xpsDocument).Write(_pt);
			DocViewer.Document = xpsDocument.GetFixedDocumentSequence();
			xpsDocument.Close();
		}
		catch (Exception)
		{
		}
	}

	public void PrintCommand()
	{
		try
		{
			PrintDialog obj = new PrintDialog
			{
				PrintQueue = _viewModel.CurrentPrintQueue,
				PrintTicket = 
				{
					Duplexing = _duplexing,
					CopyCount = _copyCount,
					PagesPerSheet = _pagesPerSheet,
					PageOrientation = _viewModel.PageOrientation,
					PageMediaSize = _viewModel.PageSize
				}
			};
			string message = CheckPrint(_viewModel.CurrentPrintQueue);
			if (_viewModel.CurrentPrintQueue.QueueStatus != PrintQueueStatus.None)
			{
				Toast(message);
			}
			PaginatorHeaderFooter paginatorHeaderFooter = new PaginatorHeaderFooter(_dp, _printCal ? 1250 : 793);
			paginatorHeaderFooter.GetPage(100);
			obj.PrintDocument(paginatorHeaderFooter, "TickTick");
		}
		catch (Exception ex)
		{
			try
			{
				UtilLog.Info("Print Exception : " + ex.Message);
				Toast("error");
				DocViewer.Print();
			}
			catch (Exception)
			{
			}
		}
	}

	private void Toast(string message)
	{
		PrintingText.Text = Utils.GetString("AddToPrintList");
		if (!string.IsNullOrEmpty(message))
		{
			PrinterStatusText.Text = Utils.GetString("PrinterStatus") + message;
			PrinterStatusText.Visibility = Visibility.Visible;
		}
		else
		{
			PrinterStatusText.Visibility = Visibility.Collapsed;
		}
		MessageBorder.Visibility = Visibility.Visible;
		((Storyboard)FindResource("ShowMessageStory")).Begin();
	}

	private void HideMessageStory(object sender, EventArgs e)
	{
		MessageBorder.Visibility = Visibility.Collapsed;
	}

	public void CloseCommand()
	{
		Close();
	}

	private void CancelRightOption(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
	}

	private void OnOrientationChanged(object sender, SelectionChangedEventArgs e)
	{
		if (_viewModel != null)
		{
			_viewModel.PageOrientation = ((((ComboBox)sender).SelectedIndex != 0) ? PageOrientation.Landscape : PageOrientation.Portrait);
		}
	}

	public string CheckPrint(PrintQueue pq)
	{
		switch (pq?.QueueStatus)
		{
		case PrintQueueStatus.Paused:
		case PrintQueueStatus.Error:
		case PrintQueueStatus.PendingDeletion:
		case PrintQueueStatus.PaperJam:
		case PrintQueueStatus.PaperOut:
		case PrintQueueStatus.ManualFeed:
		case PrintQueueStatus.PaperProblem:
		case PrintQueueStatus.Offline:
		case PrintQueueStatus.IOActive:
		case PrintQueueStatus.Busy:
		case PrintQueueStatus.Printing:
		case PrintQueueStatus.OutputBinFull:
		case PrintQueueStatus.NotAvailable:
		case PrintQueueStatus.Processing:
		case PrintQueueStatus.Initializing:
		case PrintQueueStatus.WarmingUp:
		case PrintQueueStatus.TonerLow:
		case PrintQueueStatus.NoToner:
		case PrintQueueStatus.UserIntervention:
		case PrintQueueStatus.OutOfMemory:
		case PrintQueueStatus.DoorOpen:
		case PrintQueueStatus.ServerUnknown:
		case PrintQueueStatus.PowerSave:
			return Utils.GetString("Printer" + pq.QueueStatus);
		case PrintQueueStatus.None:
			return "";
		case PrintQueueStatus.Waiting:
			return "";
		default:
			return "";
		}
	}

	private void OnTextInput(object sender, TextCompositionEventArgs e)
	{
		if (!char.IsDigit(e.Text, e.Text.Length - 1))
		{
			e.Handled = true;
		}
	}

	private void OnTextLostFocus(object sender, RoutedEventArgs e)
	{
		if (sender is TextBox textBox)
		{
			textBox.Text = textBox.Text ?? "";
		}
	}

	private void OnPagesPerSheetChanged(object sender, SelectionChangedEventArgs e)
	{
		switch (((ComboBox)sender).SelectedIndex)
		{
		default:
			_pagesPerSheet = 1;
			break;
		case 1:
			_pagesPerSheet = 2;
			break;
		case 2:
			_pagesPerSheet = 4;
			break;
		case 3:
			_pagesPerSheet = 6;
			break;
		case 4:
			_pagesPerSheet = 9;
			break;
		case 5:
			_pagesPerSheet = 16;
			break;
		}
	}

	private void OnDuplexingChanged(object sender, SelectionChangedEventArgs e)
	{
		int selectedIndex = ((ComboBox)sender).SelectedIndex;
		if (selectedIndex == 0 || selectedIndex != 1)
		{
			_duplexing = Duplexing.OneSided;
		}
		else
		{
			_duplexing = Duplexing.TwoSidedLongEdge;
		}
	}

	private void OnCopyCountChanged(object sender, TextChangedEventArgs e)
	{
		if (!(sender is TextBox textBox))
		{
			return;
		}
		if (int.TryParse(textBox.Text, out var result))
		{
			if (result > 0)
			{
				_copyCount = result;
				return;
			}
			_copyCount = 1;
			textBox.Text = "1";
		}
		else
		{
			_copyCount = 1;
			textBox.Text = "1";
		}
	}

	private void TryDrag(object sender, MouseButtonEventArgs e)
	{
		if (e.LeftButton == MouseButtonState.Pressed)
		{
			DragMove();
		}
	}

	private void OnCloseClick(object sender, MouseButtonEventArgs e)
	{
		Close();
	}

	private void OnQueueChanged(object sender, SelectionChangedEventArgs e)
	{
		SetPrintText();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/print/printpreviewwindow.xaml", UriKind.Relative);
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
			((Storyboard)target).Completed += HideMessageStory;
			break;
		case 2:
			Container = (Grid)target;
			break;
		case 9:
			Preview = (Grid)target;
			break;
		case 10:
			DocViewer = (DocumentViewer)target;
			DocViewer.PreviewMouseRightButtonUp += CancelRightOption;
			break;
		case 12:
			MessageBorder = (Grid)target;
			break;
		case 13:
			PrintingText = (TextBlock)target;
			break;
		case 14:
			PrinterStatusText = (TextBlock)target;
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
		case 3:
			((Border)target).MouseLeftButtonDown += TryDrag;
			break;
		case 4:
			((ComboBox)target).SelectionChanged += OnQueueChanged;
			break;
		case 5:
			((ComboBox)target).SelectionChanged += OnOrientationChanged;
			break;
		case 6:
			((ComboBox)target).SelectionChanged += OnPagesPerSheetChanged;
			break;
		case 7:
			((TextBox)target).TextChanged += OnCopyCountChanged;
			((TextBox)target).PreviewTextInput += OnTextInput;
			((TextBox)target).LostFocus += OnTextLostFocus;
			break;
		case 8:
			((ComboBox)target).SelectionChanged += OnDuplexingChanged;
			break;
		case 11:
			((Border)target).MouseLeftButtonUp += OnCloseClick;
			break;
		case 9:
		case 10:
			break;
		}
	}
}
