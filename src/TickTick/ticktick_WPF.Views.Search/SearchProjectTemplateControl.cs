using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using ticktick_WPF.Views.TaskList;

namespace ticktick_WPF.Views.Search;

public class SearchProjectTemplateControl : UserControl, IComponentConnector
{
	internal TaskTitleBox Title;

	private bool _contentLoaded;

	public SearchProjectTemplateControl()
	{
		InitializeComponent();
		Title.SetupPreSearchRender();
	}

	private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (base.DataContext is SearchProjectTemplateModel searchProjectTemplateModel)
		{
			Title.SetText(searchProjectTemplateModel.Name);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/search/searchprojecttemplatecontrol.xaml", UriKind.Relative);
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
			((SearchProjectTemplateControl)target).DataContextChanged += UserControl_DataContextChanged;
			break;
		case 2:
			Title = (TaskTitleBox)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
