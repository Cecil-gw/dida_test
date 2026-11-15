using System;

namespace ticktick_WPF.Views.CountDown.Views;

public class CountDownSelectedEventArgs : EventArgs
{
	public CountDownItem SelectedItem { get; private set; }

	public CountDownSelectedEventArgs(CountDownItem selectedItem)
	{
		SelectedItem = selectedItem;
	}
}
