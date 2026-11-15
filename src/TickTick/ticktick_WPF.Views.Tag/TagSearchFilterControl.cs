using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace ticktick_WPF.Views.Tag;

public class TagSearchFilterControl : UserControl, IComponentConnector
{
	internal TagSelectionControl TagItems;

	private bool _contentLoaded;

	public event EventHandler Cancel;

	public event EventHandler<List<string>> Save;

	public TagSearchFilterControl(List<string> tags)
	{
		InitializeComponent();
		TagItems.SetSelectedTags(new TagSelectData
		{
			OmniSelectTags = (TagSerializer.NormalizeNames(tags) ?? new List<string>())
		});
	}

	private void OnSaveClick(object sender, RoutedEventArgs e)
	{
		List<string> list = TagItems.GetSelectedTags();
		if (list.Contains("*tag"))
		{
			list = new List<string>();
		}
		this.Save?.Invoke(this, list);
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		this.Cancel?.Invoke(this, null);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/tag/tagsearchfiltercontrol.xaml", UriKind.Relative);
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
			TagItems = (TagSelectionControl)target;
			break;
		case 2:
			((Button)target).Click += OnSaveClick;
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
