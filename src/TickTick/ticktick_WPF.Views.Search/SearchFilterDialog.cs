using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Filter;

namespace ticktick_WPF.Views.Search;

public class SearchFilterDialog : Window, IOkCancelWindow, IComponentConnector
{
	internal NormalFilterControl NormalFilterControl;

	private bool _contentLoaded;

	public SearchFilterDialog()
	{
		InitializeComponent();
		NormalFilterControl.ViewModel = new NormalFilterViewModel();
		NormalFilterControl.SetUseInSearchFilter();
	}

	public SearchFilterDialog(SearchFilterModel searchModel)
	{
		InitializeComponent();
		NormalFilterControl.ViewModel = new NormalFilterViewModel();
		NormalFilterControl.SetUseInSearchFilter();
	}

	public void OnCancel()
	{
		Close();
	}

	public void Ok()
	{
	}

	private void OnSearchClick(object sender, RoutedEventArgs e)
	{
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/search/searchfilterdialog.xaml", UriKind.Relative);
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
			NormalFilterControl = (NormalFilterControl)target;
			break;
		case 2:
			((Button)target).Click += OnSearchClick;
			break;
		case 3:
			((Button)target).Click += OnCancelClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
