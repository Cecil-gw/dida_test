using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Detail;

namespace ticktick_WPF.Views.CheckList;

public class DragCheckItem : UserControl, IComponentConnector
{
	internal DragCheckItem ItemControl;

	internal DetailTextBox TitleBox;

	private bool _contentLoaded;

	public DragCheckItem()
	{
		InitializeComponent();
		base.DataContextChanged += OnDataBind;
	}

	private void OnDataBind(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (base.DataContext is CheckItemViewModel checkItemViewModel)
		{
			TitleBox.SetText(checkItemViewModel.Title);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/checklist/dragcheckitem.xaml", UriKind.Relative);
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
			ItemControl = (DragCheckItem)target;
			break;
		case 2:
			TitleBox = (DetailTextBox)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
