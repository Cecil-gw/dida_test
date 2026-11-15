using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Pomo;

public class PageTurnClock : UserControl, IComponentConnector
{
	private int _second;

	private double _columnWidth;

	internal ColumnDefinition HourColumn;

	internal ColumnDefinition MinuteColumn;

	internal ColumnDefinition SecondColumn;

	internal PageTurn HourPage;

	internal PageTurn MinutePage;

	internal PageTurn SecondPage;

	private bool _contentLoaded;

	public PageTurnClock()
	{
		InitializeComponent();
		HourPage.SetVisibleAlways(visible: false);
		MinutePage.SetVisibleAlways(visible: false);
	}

	public async void SetSecond(int second, bool withAnimation = true)
	{
		if (_second != second)
		{
			_second = second;
			int num = second / 60 % 60;
			int hour = second / 3600;
			int num2 = second % 3600 % 60;
			MinutePage.SetText(num, withAnimation);
			SecondPage.SetText(num2, withAnimation);
			if (hour > 0 && HourPage.Visibility != Visibility.Visible)
			{
				MinuteColumn.Width = new GridLength(_columnWidth * 1.1);
				SecondColumn.Width = MinuteColumn.Width;
				HourColumn.Width = MinuteColumn.Width;
				HourPage.Visibility = Visibility.Visible;
				HourPage.SetText(hour, withAnimation);
			}
			else if (hour == 0 && HourPage.Visibility == Visibility.Visible)
			{
				HourPage.SetText(hour, withAnimation);
				await Task.Delay(900);
				MinuteColumn.Width = new GridLength(_columnWidth * 1.25);
				SecondColumn.Width = MinuteColumn.Width;
				HourColumn.Width = new GridLength(0.0);
				HourPage.Visibility = Visibility.Collapsed;
			}
			if (HourPage.Visibility == Visibility.Visible)
			{
				HourPage.SetText(hour, withAnimation);
			}
		}
	}

	public void SetWidth(double width)
	{
		_columnWidth = width;
		if (HourPage.Visibility == Visibility.Visible)
		{
			MinuteColumn.Width = new GridLength(_columnWidth * 1.1);
			SecondColumn.Width = MinuteColumn.Width;
			HourColumn.Width = MinuteColumn.Width;
		}
		else
		{
			MinuteColumn.Width = new GridLength(width * 1.25);
			SecondColumn.Width = MinuteColumn.Width;
		}
		base.Height = width;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/pomo/pageturnclock.xaml", UriKind.Relative);
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
			HourColumn = (ColumnDefinition)target;
			break;
		case 2:
			MinuteColumn = (ColumnDefinition)target;
			break;
		case 3:
			SecondColumn = (ColumnDefinition)target;
			break;
		case 4:
			HourPage = (PageTurn)target;
			break;
		case 5:
			MinutePage = (PageTurn)target;
			break;
		case 6:
			SecondPage = (PageTurn)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
