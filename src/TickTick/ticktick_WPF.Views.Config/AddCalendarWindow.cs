using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Shapes;
using TickTickModels;
using ticktick_WPF.Util;
using ticktick_WPF.Views.MarkDown;

namespace ticktick_WPF.Views.Config;

public class AddCalendarWindow : Window, IComponentConnector
{
	private readonly string _connectId;

	internal TextBlock TitleText;

	internal StackPanel IntegrationGrid;

	internal EmojiEditor NameText;

	internal Button SaveButton;

	internal Polygon X;

	private bool _contentLoaded;

	public CalendarContentCalendar NewCalendar { get; set; }

	public AddCalendarWindow(string connectId)
	{
		_connectId = connectId;
		InitializeComponent();
		base.Loaded += OnLoaded;
	}

	private async void OnLoaded(object sender, RoutedEventArgs e)
	{
		await Task.Delay(240);
		NameText.FocusEnd();
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private async void OnSaveClick(object sender, RoutedEventArgs e)
	{
		SaveButton.IsEnabled = false;
		NewCalendar = await Communicator.CreateGoogleCalendar(_connectId, NameText.Text.Trim());
		Close();
	}

	private void CloseClick(object sender, MouseButtonEventArgs e)
	{
		Close();
	}

	private void OnDragMove(object sender, MouseButtonEventArgs e)
	{
		if (e.LeftButton == MouseButtonState.Pressed)
		{
			DragMove();
		}
	}

	private void OnTextChanged(object sender, EventArgs e)
	{
		SaveButton.IsEnabled = !string.IsNullOrEmpty(NameText.Text.Trim());
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/config/addcalendarwindow.xaml", UriKind.Relative);
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
			((Border)target).MouseLeftButtonDown += OnDragMove;
			break;
		case 2:
			TitleText = (TextBlock)target;
			break;
		case 3:
			IntegrationGrid = (StackPanel)target;
			break;
		case 4:
			NameText = (EmojiEditor)target;
			break;
		case 5:
			((Button)target).Click += OnCancelClick;
			break;
		case 6:
			SaveButton = (Button)target;
			SaveButton.Click += OnSaveClick;
			break;
		case 7:
			((Border)target).MouseLeftButtonUp += CloseClick;
			break;
		case 8:
			X = (Polygon)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
