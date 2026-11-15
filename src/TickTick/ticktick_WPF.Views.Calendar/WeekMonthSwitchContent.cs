using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Shapes;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Calendar;

public class WeekMonthSwitchContent : UserControl, ITabControl, IComponentConnector
{
	private int _multiDays;

	private int _multiWeeks;

	public static readonly DependencyProperty HoverIndexProperty = DependencyProperty.Register("HoverIndex", typeof(int), typeof(WeekMonthSwitchContent), new PropertyMetadata(-1, null));

	private readonly bool _moreMode;

	private string _mode;

	internal WeekMonthSwitchContent Root;

	internal ContentControl Container;

	internal Path DayIcon;

	internal TextBlock Day;

	internal Path WeekIcon;

	internal TextBlock Week;

	internal Path MonthIcon;

	internal TextBlock Month;

	internal Path YearIcon;

	internal TextBlock Year;

	internal Path AgendaIcon;

	internal TextBlock Agenda;

	internal Border Split;

	internal Border MultiDayGrid;

	internal Path MultiDayIcon;

	internal TextBlock MultiDay;

	internal TextBlock MultiDayTextBlock;

	internal Border SubDay;

	internal TextBlock MultiDayText;

	internal Border AddDay;

	internal Border MultiWeekGrid;

	internal Path MultiWeekIcon;

	internal TextBlock MultiWeek;

	internal TextBlock MultiWeekTextBlock;

	internal Border SubWeek;

	internal TextBlock MultiWeekText;

	internal Border AddWeek;

	private bool _contentLoaded;

	public int HoverIndex
	{
		get
		{
			return (int)GetValue(HoverIndexProperty);
		}
		set
		{
			SetValue(HoverIndexProperty, value);
		}
	}

	public event WeekMonthSwitch.SwitchModeDelegate SwitchMode;

	public WeekMonthSwitchContent(bool moreMode)
	{
		_moreMode = moreMode;
		InitializeComponent();
		if (_moreMode)
		{
			Container.Style = null;
			Split.Visibility = Visibility.Collapsed;
		}
	}

	public bool HandleTab(bool shift)
	{
		int length = Enum.GetValues(typeof(CalendarViewType)).Length;
		if (shift)
		{
			if (HoverIndex <= 0)
			{
				HoverIndex = (_moreMode ? (-1) : (length - 1));
				return !_moreMode;
			}
			HoverIndex--;
		}
		else
		{
			if (HoverIndex >= length - 1)
			{
				HoverIndex = (_moreMode ? (-1) : 0);
				return !_moreMode;
			}
			HoverIndex++;
		}
		return true;
	}

	public bool HandleEnter()
	{
		int length = Enum.GetValues(typeof(CalendarViewType)).Length;
		if (HoverIndex >= 0 && HoverIndex <= length - 1)
		{
			return true;
		}
		return false;
	}

	public bool HandleEsc()
	{
		return false;
	}

	public bool UpDownSelect(bool isUp)
	{
		return HandleTab(isUp);
	}

	public bool LeftRightSelect(bool isLeft)
	{
		return false;
	}

	public void InitSelectMode(string mode)
	{
		_mode = mode;
		_multiDays = LocalSettings.Settings.ExtraSettings.GetCalendarDefaultMultiNum(isMultiDay: true);
		_multiWeeks = LocalSettings.Settings.ExtraSettings.GetCalendarDefaultMultiNum(isMultiDay: false);
		bool flag = mode.StartsWith("D");
		bool flag2 = mode.StartsWith("W");
		if ((flag2 || flag) && int.TryParse(mode.Substring(1), out var result))
		{
			if (flag)
			{
				_multiDays = result;
			}
			else
			{
				_multiWeeks = result;
			}
		}
		SetAdjustButtonEnable();
		MultiDayText.Text = _multiDays.ToString() ?? "";
		MultiWeekText.Text = _multiWeeks.ToString() ?? "";
		MultiDayTextBlock.Text = _multiDays + " " + Utils.GetString((_multiDays > 1) ? "PublicDays" : "PublicDay");
		MultiWeekTextBlock.Text = _multiWeeks + " " + Utils.GetString("PublicWeeks");
		switch (mode)
		{
		case "2":
			SetItemSelected(DayIcon, Day);
			break;
		case "1":
			SetItemSelected(WeekIcon, Week);
			break;
		case "0":
			SetItemSelected(MonthIcon, Month);
			break;
		case "8":
			SetItemSelected(AgendaIcon, Agenda);
			break;
		case "10":
			SetItemSelected(YearIcon, Year);
			break;
		}
		if (flag)
		{
			SetItemSelected(MultiDayIcon, MultiDay);
		}
		else if (flag2)
		{
			SetItemSelected(MultiWeekIcon, MultiWeek);
		}
	}

	private void SetAdjustButtonEnable()
	{
		SubDay.Cursor = ((_multiDays > 1) ? Cursors.Hand : Cursors.No);
		SubDay.Opacity = ((_multiDays > 1) ? 1.0 : 0.4);
		AddDay.Cursor = ((_multiDays < 14) ? Cursors.Hand : Cursors.No);
		AddDay.Opacity = ((_multiDays < 14) ? 1.0 : 0.4);
		SubWeek.Cursor = ((_multiWeeks > 2) ? Cursors.Hand : Cursors.No);
		SubWeek.Opacity = ((_multiWeeks > 2) ? 1.0 : 0.4);
		AddWeek.Cursor = ((_multiWeeks < 6) ? Cursors.Hand : Cursors.No);
		AddWeek.Opacity = ((_multiWeeks < 6) ? 1.0 : 0.4);
	}

	private void SetItemSelected(Path icon, TextBlock text)
	{
		DayIcon.Visibility = Visibility.Collapsed;
		WeekIcon.Visibility = Visibility.Collapsed;
		MonthIcon.Visibility = Visibility.Collapsed;
		YearIcon.Visibility = Visibility.Collapsed;
		MultiDayIcon.Visibility = Visibility.Collapsed;
		MultiWeekIcon.Visibility = Visibility.Collapsed;
		AgendaIcon.Visibility = Visibility.Collapsed;
		Day.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity100_80");
		Week.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity100_80");
		Month.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity100_80");
		Year.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity100_80");
		MultiDay.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity100_80");
		MultiWeek.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity100_80");
		Agenda.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity100_80");
		icon.Visibility = Visibility.Visible;
		text.SetResourceReference(TextBlock.ForegroundProperty, "TextAccentColor");
	}

	private void OnItemClick(object sender, MouseButtonEventArgs e)
	{
		string text = _mode;
		string text2 = text;
		if (sender is FrameworkElement frameworkElement)
		{
			switch (frameworkElement.Tag as string)
			{
			case "Agenda":
				text = "8";
				SetItemSelected(AgendaIcon, Agenda);
				break;
			case "Day":
				text = "2";
				SetItemSelected(DayIcon, Day);
				break;
			case "Week":
				text = "1";
				SetItemSelected(WeekIcon, Week);
				break;
			case "Month":
				text = "0";
				SetItemSelected(MonthIcon, Month);
				break;
			case "MultiDay":
				text = "D" + _multiDays;
				SetItemSelected(MultiDayIcon, MultiDay);
				break;
			case "MultiWeek":
				text = "W" + _multiWeeks;
				SetItemSelected(MultiWeekIcon, MultiWeek);
				break;
			case "Year":
				text = "10";
				SetItemSelected(YearIcon, Year);
				break;
			}
		}
		if (text2 != text)
		{
			_mode = text;
			this.SwitchMode?.Invoke(text2, text, !SubDay.IsMouseOver && !AddDay.IsMouseOver && !SubWeek.IsMouseOver && !AddWeek.IsMouseOver);
		}
	}

	private void OnChangeNumClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is FrameworkElement frameworkElement)
		{
			switch (frameworkElement.Tag as string)
			{
			case "+Day":
				if (_multiDays >= 14)
				{
					return;
				}
				_multiDays = Math.Min(14, _multiDays + 1);
				LocalSettings.Settings.ExtraSettings.SetCalendarDefaultMultiNum(isMultiDay: true, _multiDays);
				break;
			case "-Day":
				if (_multiDays <= 1)
				{
					return;
				}
				_multiDays = Math.Max(1, _multiDays - 1);
				LocalSettings.Settings.ExtraSettings.SetCalendarDefaultMultiNum(isMultiDay: true, _multiDays);
				break;
			case "+Week":
				if (_multiWeeks >= 6)
				{
					return;
				}
				_multiWeeks = Math.Min(6, _multiWeeks + 1);
				LocalSettings.Settings.ExtraSettings.SetCalendarDefaultMultiNum(isMultiDay: false, _multiWeeks);
				break;
			case "-Week":
				if (_multiWeeks <= 2)
				{
					return;
				}
				_multiWeeks = Math.Max(2, _multiWeeks - 1);
				LocalSettings.Settings.ExtraSettings.SetCalendarDefaultMultiNum(isMultiDay: false, _multiWeeks);
				break;
			}
		}
		MultiDayText.Text = _multiDays.ToString() ?? "";
		MultiWeekText.Text = _multiWeeks.ToString() ?? "";
		MultiDayTextBlock.Text = _multiDays + " " + Utils.GetString((_multiDays > 1) ? "PublicDays" : "PublicDay");
		MultiWeekTextBlock.Text = _multiWeeks + " " + Utils.GetString("PublicWeeks");
		SetAdjustButtonEnable();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/calendar/weekmonthswitchcontent.xaml", UriKind.Relative);
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
			Root = (WeekMonthSwitchContent)target;
			break;
		case 2:
			Container = (ContentControl)target;
			break;
		case 3:
			((Border)target).MouseLeftButtonUp += OnItemClick;
			break;
		case 4:
			DayIcon = (Path)target;
			break;
		case 5:
			Day = (TextBlock)target;
			break;
		case 6:
			((Border)target).MouseLeftButtonUp += OnItemClick;
			break;
		case 7:
			WeekIcon = (Path)target;
			break;
		case 8:
			Week = (TextBlock)target;
			break;
		case 9:
			((Border)target).MouseLeftButtonUp += OnItemClick;
			break;
		case 10:
			MonthIcon = (Path)target;
			break;
		case 11:
			Month = (TextBlock)target;
			break;
		case 12:
			((Border)target).MouseLeftButtonUp += OnItemClick;
			break;
		case 13:
			YearIcon = (Path)target;
			break;
		case 14:
			Year = (TextBlock)target;
			break;
		case 15:
			((Border)target).MouseLeftButtonUp += OnItemClick;
			break;
		case 16:
			AgendaIcon = (Path)target;
			break;
		case 17:
			Agenda = (TextBlock)target;
			break;
		case 18:
			Split = (Border)target;
			break;
		case 19:
			MultiDayGrid = (Border)target;
			MultiDayGrid.MouseLeftButtonUp += OnItemClick;
			break;
		case 20:
			MultiDayIcon = (Path)target;
			break;
		case 21:
			MultiDay = (TextBlock)target;
			break;
		case 22:
			MultiDayTextBlock = (TextBlock)target;
			break;
		case 23:
			SubDay = (Border)target;
			SubDay.PreviewMouseLeftButtonUp += OnChangeNumClick;
			break;
		case 24:
			MultiDayText = (TextBlock)target;
			break;
		case 25:
			AddDay = (Border)target;
			AddDay.PreviewMouseLeftButtonUp += OnChangeNumClick;
			break;
		case 26:
			MultiWeekGrid = (Border)target;
			MultiWeekGrid.MouseLeftButtonUp += OnItemClick;
			break;
		case 27:
			MultiWeekIcon = (Path)target;
			break;
		case 28:
			MultiWeek = (TextBlock)target;
			break;
		case 29:
			MultiWeekTextBlock = (TextBlock)target;
			break;
		case 30:
			SubWeek = (Border)target;
			SubWeek.PreviewMouseLeftButtonUp += OnChangeNumClick;
			break;
		case 31:
			MultiWeekText = (TextBlock)target;
			break;
		case 32:
			AddWeek = (Border)target;
			AddWeek.PreviewMouseLeftButtonUp += OnChangeNumClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
