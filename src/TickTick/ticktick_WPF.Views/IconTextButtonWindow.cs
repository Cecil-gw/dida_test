using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views;

public class IconTextButtonWindow : Window, IComponentConnector
{
	internal Image MainImage;

	internal TextBlock MainText;

	internal Button MainButton;

	internal Border CloseButton;

	private bool _contentLoaded;

	private Action buttonEvent { get; set; }

	public IconTextButtonWindow()
	{
		InitializeComponent();
	}

	public IconTextButtonWindow(string title, string imageKey, string content, string buttonText, Action action)
	{
		InitializeComponent();
		base.Title = title;
		MainImage.Source = Utils.GetImageSource(imageKey);
		MainText.Text = content;
		MainButton.Content = buttonText;
		buttonEvent = action;
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	private void OnCloseClick(object sender, MouseButtonEventArgs e)
	{
		Close();
	}

	private void MainButton_Click(object sender, RoutedEventArgs e)
	{
		Close();
		buttonEvent?.Invoke();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/icontextbuttonwindow.xaml", UriKind.Relative);
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
			MainImage = (Image)target;
			break;
		case 2:
			MainText = (TextBlock)target;
			break;
		case 3:
			MainButton = (Button)target;
			MainButton.Click += MainButton_Click;
			break;
		case 4:
			CloseButton = (Border)target;
			CloseButton.MouseLeftButtonUp += OnCloseClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
