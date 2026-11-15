using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace ticktick_WPF.Views.Config;

public class IntegrationItemsControl : UserControl, IComponentConnector
{
	internal TextBlock Title;

	internal ItemsControl Items;

	private bool _contentLoaded;

	public IntegrationItemsControl(string title, List<SubscribeTypeItemViewModel> items)
	{
		InitializeComponent();
		Items.SetResourceReference(ItemsControl.ItemTemplateProperty, "TypeItemTemplate");
		Title.Text = title;
		Items.ItemsSource = items;
	}

	public void SetItems(List<SubscribeTypeItemViewModel> items)
	{
		Items.ItemsSource = items;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/config/integrationitemscontrol.xaml", UriKind.Relative);
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
			Title = (TextBlock)target;
			break;
		case 2:
			Items = (ItemsControl)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
