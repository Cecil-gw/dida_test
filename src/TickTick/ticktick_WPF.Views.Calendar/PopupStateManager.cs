using System;
using System.Threading.Tasks;
using System.Windows;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Calendar;

public static class PopupStateManager
{
	private const int CheckDelay = 150;

	private static bool _addPopupOpen;

	private static bool _viewPopupOpen;

	private static bool _loadMorePopupOpen;

	private static bool _selection;

	private static bool _dragPopupOpen;

	private static bool _showLog = true;

	private static bool _viewPopupClose = true;

	private static DateTime _monthSelectionSuppressedUntil = DateTime.MinValue;

	public static bool CanOpenTimePopup = true;

	public static UIElement LastTarget { get; set; }

	public static void Reset()
	{
		_addPopupOpen = false;
		_viewPopupOpen = false;
		_viewPopupClose = true;
		_loadMorePopupOpen = false;
		_dragPopupOpen = false;
		_selection = false;
		_monthSelectionSuppressedUntil = DateTime.MinValue;
	}

	public static void OnAddPopupOpened()
	{
		d("add_open");
		_addPopupOpen = true;
	}

	public static async void OnAddPopupClosed(bool needWait = true)
	{
		if (!needWait)
		{
			await Task.Delay(150);
		}
		else
		{
			await Task.Delay(200);
		}
		d("add_close");
		_viewPopupOpen = false;
		_addPopupOpen = false;
	}

	public static bool CanShowDragPopup()
	{
		return !_dragPopupOpen;
	}

	public static async void OnViewPopupOpened()
	{
		_viewPopupOpen = true;
		_viewPopupClose = false;
	}

	public static async void OnViewPopupClosed(bool needWait = true)
	{
		_viewPopupClose = true;
		if (needWait)
		{
			await Task.Delay(200);
		}
		_viewPopupOpen = !_viewPopupClose;
	}

	public static void OnLoadMorePopupOpened()
	{
		_loadMorePopupOpen = true;
		d("load_open");
	}

	public static async void OnLoadMorePopupClosed()
	{
		await Task.Delay(150);
		_loadMorePopupOpen = false;
		d("load_close");
	}

	public static bool CanShowAddPopup()
	{
		LogState();
		if (!_addPopupOpen && !_viewPopupOpen)
		{
			return !_loadMorePopupOpen;
		}
		return false;
	}

	public static bool CanShowDetailPopup()
	{
		LogState();
		if (!_addPopupOpen)
		{
			return !_selection;
		}
		return false;
	}

	public static bool CanStartSelection()
	{
		if (!_addPopupOpen && !_viewPopupOpen)
		{
			return !_loadMorePopupOpen;
		}
		return false;
	}

	public static bool CanStartMonthSelection()
	{
		if (CanStartSelection())
		{
			return DateTime.Now > _monthSelectionSuppressedUntil;
		}
		return false;
	}

	public static void SuppressMonthSelection(int milliseconds = 300)
	{
		_monthSelectionSuppressedUntil = DateTime.Now.AddMilliseconds(milliseconds);
	}

	public static bool CanShowLoadMorePopup()
	{
		LogState();
		if (!_addPopupOpen && !_viewPopupOpen)
		{
			return !_loadMorePopupOpen;
		}
		return false;
	}

	public static void StartSelection()
	{
		_selection = true;
	}

	public static bool CheckAddPopup()
	{
		return _addPopupOpen;
	}

	public static async void StopSelection()
	{
		await Task.Delay(150);
		_selection = false;
	}

	public static bool IsInSelection()
	{
		return _selection;
	}

	private static void d(string msg)
	{
		if (_showLog)
		{
			Log.d(msg);
		}
	}

	private static void LogState()
	{
	}

	public static bool IsViewPopOpened()
	{
		return _viewPopupOpen;
	}

	public static bool IsInAdd()
	{
		return _addPopupOpen;
	}

	public static async void SetCanOpenTimePopup(bool canOpen, bool delay = false)
	{
		if (delay)
		{
			await Task.Delay(100);
		}
		CanOpenTimePopup = canOpen;
	}

	public static async Task ClearLastTarget(UIElement target)
	{
		await Task.Delay(240);
		if (LastTarget == target)
		{
			LastTarget = null;
		}
	}
}
