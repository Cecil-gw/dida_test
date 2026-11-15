using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Shapes;
using Emoji.Wpf;
using ticktick_WPF.Util;
using ticktick_WPF.Views.MarkDown;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Time;

namespace ticktick_WPF.Views.Calendar;

public class CalendarTimeZoneConfigItem : UserControl, IComponentConnector
{
	private Point _mouseDownPoint;

	internal CalendarTimeZoneConfigItem Root;

	internal Grid Container;

	internal Border DragButton;

	internal TimeZoneSelector TimeZoneSelector;

	internal Grid InputGrid;

	internal EmojiEditor LabelText;

	internal Grid EmojiSelectGrid;

	internal Path SetEmojiIcon;

	internal EmjTextBlock EmojiText;

	private bool _contentLoaded;

	public event EventHandler<CalendarTimeZoneConfigItemViewModel> Delete;

	public event EventHandler TimeZoneChanged;

	public CalendarTimeZoneConfigItem()
	{
		InitializeComponent();
		base.DataContextChanged += OnDataBind;
		TimeZoneSelector.BottomBorder.SetResourceReference(Control.BackgroundProperty, "PopupBackground");
	}

	private void OnDataBind(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (base.DataContext is CalendarTimeZoneConfigItemViewModel calendarTimeZoneConfigItemViewModel)
		{
			TimeZoneSelector.SetTimeZone(calendarTimeZoneConfigItemViewModel.TimeZone);
			TimeZoneSelector.SetEnable(calendarTimeZoneConfigItemViewModel.Editable);
			DragButton.Opacity = (calendarTimeZoneConfigItemViewModel.Editable ? 1 : 0);
			DragButton.IsHitTestVisible = calendarTimeZoneConfigItemViewModel.Editable;
			LabelText.Text = calendarTimeZoneConfigItemViewModel.Label;
			LabelText.Tag = Utils.GetString(calendarTimeZoneConfigItemViewModel.IsLocal ? "CurrentTimeZone" : "Label");
			SetEmoji(calendarTimeZoneConfigItemViewModel.Emoji);
			if (calendarTimeZoneConfigItemViewModel.IsLocal)
			{
				TimeZoneSelector.ToolTip = Utils.GetString("CurrentTimeZone");
			}
		}
	}

	private void SetEmoji(string emoji)
	{
		EmojiText.Text = emoji ?? string.Empty;
		SetEmojiIcon.Visibility = ((!string.IsNullOrEmpty(emoji)) ? Visibility.Collapsed : Visibility.Visible);
		LabelText.MaxLength = 64 - EmojiText.Text.Length;
	}

	private async void ShowEmojiSelector(object sender, MouseButtonEventArgs e)
	{
		EmojiSelectControl emj = new EmojiSelectControl
		{
			Width = 310.0
		};
		emj.GetItems(focus: false);
		emj.SetCanReset(!string.IsNullOrEmpty(EmojiText.Text));
		await Task.Delay(120);
		EscPopup popup = new EscPopup
		{
			StaysOpen = false,
			HorizontalOffset = -5.0,
			VerticalOffset = -4.0,
			PlacementTarget = InputGrid,
			Placement = PlacementMode.Bottom
		};
		emj.EmojiSelected += delegate(string emoji, bool b)
		{
			SetEmoji(emoji);
			if (b)
			{
				popup.IsOpen = false;
			}
			if (base.DataContext is CalendarTimeZoneConfigItemViewModel calendarTimeZoneConfigItemViewModel)
			{
				calendarTimeZoneConfigItemViewModel.Emoji = emoji;
			}
		};
		ContentControl contentControl = new ContentControl();
		contentControl.SetResourceReference(FrameworkElement.StyleProperty, "PopupContentStyle");
		contentControl.Content = emj;
		popup.Child = contentControl;
		popup.IsOpen = true;
	}

	private void DeleteClick(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext is CalendarTimeZoneConfigItemViewModel e2)
		{
			this.Delete?.Invoke(this, e2);
		}
	}

	private void OnTimeZoneChanged(object sender, EventArgs e)
	{
		if (base.DataContext is CalendarTimeZoneConfigItemViewModel calendarTimeZoneConfigItemViewModel)
		{
			calendarTimeZoneConfigItemViewModel.CanDrag = true;
			calendarTimeZoneConfigItemViewModel.TimeZone = TimeZoneSelector.SelectedTimeZone;
			this.TimeZoneChanged?.Invoke(this, null);
		}
	}

	private void OnDragMouseMove(object sender, MouseEventArgs e)
	{
		if (e.LeftButton == MouseButtonState.Pressed && _mouseDownPoint != default(Point))
		{
			if ((e.GetPosition(this) - _mouseDownPoint).Length > 2.0)
			{
				_mouseDownPoint = default(Point);
				Utils.FindParent<CalendarTimeZoneList>(this)?.TryDragItem(base.DataContext as CalendarTimeZoneConfigItemViewModel, e);
			}
		}
		else
		{
			_mouseDownPoint = default(Point);
		}
	}

	private void OnDragMouseDown(object sender, MouseButtonEventArgs e)
	{
		_mouseDownPoint = e.GetPosition(DragButton);
	}

	private void OnNoteChanged(object sender, EventArgs e)
	{
		if (base.DataContext is CalendarTimeZoneConfigItemViewModel calendarTimeZoneConfigItemViewModel)
		{
			calendarTimeZoneConfigItemViewModel.Label = LabelText.Text.Trim();
		}
	}

	private void OnTimeZoneClick(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext is CalendarTimeZoneConfigItemViewModel { Editable: false, TimeZone: not null })
		{
			Utils.FindParent<CalendarTimeZoneConfigWindow>(this)?.Toast(Utils.GetString("CannotChangeCurrent"));
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/calendar/calendartimezoneconfigitem.xaml", UriKind.Relative);
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
			Root = (CalendarTimeZoneConfigItem)target;
			break;
		case 2:
			Container = (Grid)target;
			break;
		case 3:
			DragButton = (Border)target;
			DragButton.MouseMove += OnDragMouseMove;
			DragButton.MouseLeftButtonDown += OnDragMouseDown;
			break;
		case 4:
			TimeZoneSelector = (TimeZoneSelector)target;
			break;
		case 5:
			InputGrid = (Grid)target;
			break;
		case 6:
			LabelText = (EmojiEditor)target;
			break;
		case 7:
			EmojiSelectGrid = (Grid)target;
			EmojiSelectGrid.MouseLeftButtonUp += ShowEmojiSelector;
			break;
		case 8:
			SetEmojiIcon = (Path)target;
			break;
		case 9:
			EmojiText = (EmjTextBlock)target;
			break;
		case 10:
			((Border)target).MouseLeftButtonUp += DeleteClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
