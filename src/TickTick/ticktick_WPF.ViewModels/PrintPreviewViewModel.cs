using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
using System.Windows.Documents;
using System.Windows.Threading;

namespace ticktick_WPF.ViewModels;

public class PrintPreviewViewModel : BaseViewModel
{
	private int _pageCount;

	private DocumentPaginator _paginator;

	private PrintQueue _currentPrintQueue;

	private PageMediaSize _pageSize;

	private int _startIndex;

	private int _endIndex;

	private PrintQueueCollection _printQueues;

	private List<PageMediaSize> _pageMediaSizes;

	private PageOrientation _pageOrientation;

	private Dispatcher _dispatcher;

	public int StartIndex
	{
		get
		{
			return _startIndex;
		}
		set
		{
			if (value <= EndIndex && value >= 0)
			{
				_startIndex = value;
				OnPropertyChanged("StartIndex");
			}
		}
	}

	public int EndIndex
	{
		get
		{
			return _endIndex;
		}
		set
		{
			if (value >= StartIndex && value <= PageCount)
			{
				_endIndex = value;
				OnPropertyChanged("EndIndex");
			}
		}
	}

	public int PageCount
	{
		get
		{
			return _pageCount;
		}
		set
		{
			_pageCount = value;
			if (EndIndex == 0 || EndIndex > value)
			{
				EndIndex = value;
			}
			if (StartIndex == 0 || StartIndex > value)
			{
				StartIndex = 1;
			}
			OnPropertyChanged("PageCount");
		}
	}

	public PrintQueue CurrentPrintQueue
	{
		get
		{
			return _currentPrintQueue;
		}
		set
		{
			if (_currentPrintQueue != value)
			{
				_currentPrintQueue = value;
				OnPropertyChanged("CurrentPrintQueue");
				LoadPageMediaSize();
			}
		}
	}

	public PrintQueueCollection PrintQueues
	{
		get
		{
			return _printQueues;
		}
		set
		{
			_printQueues = value;
			OnPropertyChanged("PrintQueues");
		}
	}

	public PageMediaSize PageSize
	{
		get
		{
			return _pageSize;
		}
		set
		{
			bool flag = !object.Equals(_pageSize?.Width, value?.Width) || !object.Equals(_pageSize?.Height, value?.Height);
			if (_pageSize != value && value != null)
			{
				_pageSize = value;
				OnPropertyChanged("PageSize");
				if (_paginator != null && flag)
				{
					this.NeedReload?.Invoke(null, null);
				}
			}
		}
	}

	public List<PageMediaSize> PageMediaSizes
	{
		get
		{
			return _pageMediaSizes;
		}
		set
		{
			_pageMediaSizes = value;
			OnPropertyChanged("PageMediaSizes");
		}
	}

	public PageOrientation PageOrientation
	{
		get
		{
			return _pageOrientation;
		}
		set
		{
			_pageOrientation = value;
			OnPropertyChanged("PageOrientation");
			if (_paginator != null)
			{
				this.NeedReload?.Invoke(null, null);
			}
		}
	}

	public event EventHandler NeedReload;

	public PrintPreviewViewModel(DocumentPaginator paginator, bool isLandscape, bool pdfDefault)
	{
		_paginator = paginator;
		_dispatcher = Dispatcher.CurrentDispatcher;
		PageOrientation = (isLandscape ? PageOrientation.Landscape : PageOrientation.Portrait);
		LoadPrintQueues(pdfDefault);
	}

	private void LoadPrintQueues(bool pdfDefault)
	{
		try
		{
			LocalPrintServer localPrintServer = new LocalPrintServer();
			PrintQueues = localPrintServer.GetPrintQueues();
			if (pdfDefault)
			{
				CurrentPrintQueue = PrintQueues.FirstOrDefault((PrintQueue x) => x.FullName.ToLower().Contains("pdf")) ?? PrintQueues.FirstOrDefault();
			}
			else
			{
				CurrentPrintQueue = PrintQueues.FirstOrDefault((PrintQueue x) => x.FullName == LocalPrintServer.GetDefaultPrintQueue().FullName) ?? PrintQueues.FirstOrDefault();
			}
		}
		catch (Exception)
		{
		}
	}

	private void LoadPageMediaSize()
	{
		PageMediaSizes = CurrentPrintQueue.GetPrintCapabilities().PageMediaSizeCapability.ToList();
		PageMediaSize pageSize = PageMediaSizes.FirstOrDefault((PageMediaSize x) => x.PageMediaSizeName == PageMediaSizeName.ISOA4) ?? PageMediaSizes.FirstOrDefault();
		PageSize = pageSize;
	}
}
