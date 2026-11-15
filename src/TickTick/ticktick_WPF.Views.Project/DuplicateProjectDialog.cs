using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Project;

public class DuplicateProjectDialog : Window, IOkCancelWindow, IComponentConnector
{
	public DuplicateProjectEnum Option;

	internal RadioButton UncompletedRadio;

	internal RadioButton KeepCompletedRadio;

	internal RadioButton ResetStatusRadio;

	internal Button OkButton;

	internal Button CancelButton;

	private bool _contentLoaded;

	public DuplicateProjectDialog()
	{
		InitializeComponent();
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	private void OnOkClick(object sender, RoutedEventArgs e)
	{
		if (UncompletedRadio.IsChecked == true)
		{
			Option = DuplicateProjectEnum.OnlyUncompleted;
		}
		if (KeepCompletedRadio.IsChecked == true)
		{
			Option = DuplicateProjectEnum.KeepCompleted;
		}
		if (ResetStatusRadio.IsChecked == true)
		{
			Option = DuplicateProjectEnum.Default;
		}
		Close();
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	public void OnCancel()
	{
		Close();
	}

	public void Ok()
	{
		OnOkClick(null, null);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/project/duplicateprojectdialog.xaml", UriKind.Relative);
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
			UncompletedRadio = (RadioButton)target;
			break;
		case 2:
			KeepCompletedRadio = (RadioButton)target;
			break;
		case 3:
			ResetStatusRadio = (RadioButton)target;
			break;
		case 4:
			OkButton = (Button)target;
			OkButton.Click += OnOkClick;
			break;
		case 5:
			CancelButton = (Button)target;
			CancelButton.Click += OnCancelClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
