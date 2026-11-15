using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace ticktick_WPF.Views.Tag;

public class TagEditContent : UserControl, IComponentConnector
{
	internal Button SaveButton;

	private bool _contentLoaded;

	public TagEditContent()
	{
		InitializeComponent();
	}

	private void OnSaveClick(object sender, RoutedEventArgs e)
	{
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/tag/tageditcontent.xaml", UriKind.Relative);
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
			SaveButton = (Button)target;
			SaveButton.Click += OnSaveClick;
			break;
		case 2:
			((Button)target).Click += OnCancelClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
