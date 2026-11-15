using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

namespace ticktick_WPF.Views;

public class MessageInfoWindow : Window, IComponentConnector
{
	internal TextBlock TitleTextBlock;

	internal TextBlock ContentTextBlock;

	internal Button OkButton;

	internal Button CancelButton;

	internal Border CloseButton;

	private bool _contentLoaded;

	public static MessageInfoWindow Window { get; set; }

	public Action OnOkAction { get; set; }

	public Action OnCancelAction { get; set; }

	public MessageInfoWindow()
	{
		InitializeComponent();
	}

	public MessageInfoWindow(string title, string content, string okButtonText, string cancelButtonText, Window owner = null, bool showClose = true, Action onOkAction = null, Action onCancelAction = null)
	{
		InitializeComponent();
		TitleTextBlock.Text = title;
		base.Title = (string.IsNullOrEmpty(title) ? "  " : title);
		TitleTextBlock.Visibility = (string.IsNullOrEmpty(title) ? Visibility.Collapsed : Visibility.Visible);
		ContentTextBlock.Text = content;
		OkButton.Content = okButtonText;
		CancelButton.Content = cancelButtonText;
		if (string.IsNullOrEmpty(cancelButtonText))
		{
			CancelButton.Visibility = Visibility.Collapsed;
		}
		if (!showClose)
		{
			CloseButton.Visibility = Visibility.Collapsed;
		}
		if (owner != null)
		{
			base.WindowStartupLocation = WindowStartupLocation.CenterOwner;
			if (owner.Activate())
			{
				base.Owner = owner;
			}
		}
		else
		{
			base.WindowStartupLocation = WindowStartupLocation.CenterScreen;
		}
		OnOkAction = onOkAction;
		OnCancelAction = onCancelAction;
		Window = this;
	}

	private async void OnCancelClick(object sender, RoutedEventArgs e)
	{
		Cancel();
	}

	protected virtual void Cancel()
	{
		OnCancelAction?.Invoke();
		Close();
	}

	protected virtual void OnOkClick(object sender, RoutedEventArgs e)
	{
		OnOk();
	}

	protected virtual void OnOk()
	{
		OnOkAction?.Invoke();
		Close();
	}

	protected virtual async void OnCloseClick(object sender, MouseButtonEventArgs e)
	{
		Cancel();
	}

	private void Window_Closing(object sender, CancelEventArgs e)
	{
		Window = null;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/messageinfowindow.xaml", UriKind.Relative);
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
			((MessageInfoWindow)target).Closing += Window_Closing;
			break;
		case 2:
			TitleTextBlock = (TextBlock)target;
			break;
		case 3:
			ContentTextBlock = (TextBlock)target;
			break;
		case 4:
			OkButton = (Button)target;
			OkButton.Click += OnOkClick;
			break;
		case 5:
			CancelButton = (Button)target;
			CancelButton.Click += OnCancelClick;
			break;
		case 6:
			CloseButton = (Border)target;
			CloseButton.MouseLeftButtonUp += OnCloseClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
