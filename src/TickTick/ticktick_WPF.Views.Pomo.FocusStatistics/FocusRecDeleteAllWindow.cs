using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

namespace ticktick_WPF.Views.Pomo.FocusStatistics;

public class FocusRecDeleteAllWindow : Window, IComponentConnector
{
	internal StackPanel VerifyPasswordPanel;

	internal CheckBox ConfirmCheckbox;

	internal Border CloseButton;

	private bool _contentLoaded;

	public FocusRecDeleteAllWindow()
	{
		InitializeComponent();
	}

	private void OnCloseClick(object sender, MouseButtonEventArgs e)
	{
		base.DialogResult = false;
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		base.DialogResult = false;
	}

	private async void OnSaveClick(object sender, RoutedEventArgs e)
	{
		base.DialogResult = true;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/pomo/focusstatistics/focusrecdeleteallwindow.xaml", UriKind.Relative);
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
			VerifyPasswordPanel = (StackPanel)target;
			break;
		case 2:
			ConfirmCheckbox = (CheckBox)target;
			break;
		case 3:
			((Button)target).Click += OnSaveClick;
			break;
		case 4:
			((Button)target).Click += OnCancelClick;
			break;
		case 5:
			CloseButton = (Border)target;
			CloseButton.MouseLeftButtonUp += OnCloseClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
