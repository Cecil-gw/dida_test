using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

namespace ticktick_WPF.Views.QuickAdd;

public class QuickInputItems : UserControl, IQuickInput, IComponentConnector, IStyleConnector
{
	internal ListView Items;

	private bool _contentLoaded;

	public QuickInputItems()
	{
		InitializeComponent();
	}

	public virtual void Move(bool forward)
	{
	}

	public virtual void TrySelectItem(bool exactly = false)
	{
	}

	public virtual bool Filter(string key, List<string> selected = null)
	{
		return true;
	}

	public virtual bool IsTag()
	{
		return false;
	}

	protected virtual void OnItemClick(object sender, MouseButtonEventArgs e)
	{
	}

	protected virtual void OnItemEnter(object sender, MouseEventArgs e)
	{
	}

	public virtual bool TrySelectIteWithSpace(string content)
	{
		return false;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/quickadd/quickinputitems.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 1)
		{
			Items = (ListView)target;
		}
		else
		{
			_contentLoaded = true;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IStyleConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 2)
		{
			((Grid)target).MouseEnter += OnItemEnter;
			((Grid)target).MouseLeftButtonUp += OnItemClick;
		}
	}
}
