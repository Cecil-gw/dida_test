using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Shapes;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Time;

public class DayCellSelector : UserControl, IComponentConnector
{
	private bool _monthMode;

	private int _dayOfMonth;

	private DayOfWeek _day;

	public static readonly DependencyProperty ItemSelectedProperty = DependencyProperty.Register("ItemSelected", typeof(bool), typeof(DayCellSelector), new PropertyMetadata(false, null));

	public static readonly DependencyProperty DayTextProperty = DependencyProperty.Register("DayText", typeof(string), typeof(DayCellSelector), new PropertyMetadata(string.Empty, null));

	internal DayCellSelector RootView;

	internal Rectangle HoverBackground;

	internal Rectangle SelectedBackground;

	internal new TextBlock Content;

	internal Rectangle TabSelectedRect;

	private bool _contentLoaded;

	public bool TabSelected => TabSelectedRect.Visibility == Visibility.Visible;

	public DayOfWeek Day
	{
		get
		{
			return _day;
		}
		set
		{
			_day = value;
			SetValue(DayTextProperty, GetDayText());
		}
	}

	public bool ItemSelected
	{
		get
		{
			return (bool)GetValue(ItemSelectedProperty);
		}
		set
		{
			SetValue(ItemSelectedProperty, value);
		}
	}

	public int DayOfMonth
	{
		get
		{
			return _dayOfMonth;
		}
		set
		{
			_dayOfMonth = value;
			SetValue(DayTextProperty, GetDayText());
		}
	}

	public bool MonthMode
	{
		get
		{
			return _monthMode;
		}
		set
		{
			_monthMode = value;
			SetValue(DayTextProperty, GetDayText());
		}
	}

	public string DayText
	{
		get
		{
			return (string)GetValue(DayTextProperty);
		}
		set
		{
			SetValue(DayTextProperty, value);
		}
	}

	public event EventHandler<bool> SelectedChanged;

	private string GetDayText()
	{
		if (!MonthMode)
		{
			return DateUtils.GetShortWeekTextByWeekDay((int)Day);
		}
		if (DayOfMonth != -1)
		{
			return DayOfMonth.ToString();
		}
		return Utils.GetString("LastDay");
	}

	public DayCellSelector(double width = 27.0)
	{
		InitializeComponent();
		base.Width = width;
		HoverBackground.Width = width;
		SelectedBackground.Width = width;
		TabSelectedRect.Width = width;
	}

	private void OnCellClick(object sender, MouseButtonEventArgs e)
	{
		Select();
	}

	public void Select()
	{
		ItemSelected = !ItemSelected;
		this.SelectedChanged?.Invoke(this, ItemSelected);
	}

	public void SetTabSelected(bool tabSelect)
	{
		TabSelectedRect.Visibility = ((!tabSelect) ? Visibility.Collapsed : Visibility.Visible);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/time/daycellselector.xaml", UriKind.Relative);
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
			RootView = (DayCellSelector)target;
			break;
		case 2:
			((Grid)target).MouseLeftButtonUp += OnCellClick;
			break;
		case 3:
			HoverBackground = (Rectangle)target;
			break;
		case 4:
			SelectedBackground = (Rectangle)target;
			break;
		case 5:
			Content = (TextBlock)target;
			break;
		case 6:
			TabSelectedRect = (Rectangle)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
