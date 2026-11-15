using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;

namespace ticktick_WPF.Views.Search;

public class SearchProjectItemTemplate : UserControl, IComponentConnector
{
	internal TextBlock Title;

	private bool _contentLoaded;

	public SearchProjectItemTemplate()
	{
		InitializeComponent();
		base.DataContextChanged += OnDataChanged;
	}

	private void OnDataChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		Title.Inlines.Clear();
		if (!(base.DataContext is SearchTagAndProjectModel searchTagAndProjectModel))
		{
			return;
		}
		string text = (searchTagAndProjectModel.Name ?? string.Empty).Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " ");
		int num = 0;
		foreach (Match item in SearchHelper.PreSearchRegex.Matches(text.ToLower()))
		{
			if (item.Length != 0)
			{
				Title.Inlines.Add(new Run(text.Substring(num, item.Index - num)));
				Title.Inlines.Add(new Run(text.Substring(item.Index, item.Length))
				{
					Background = SearchHelper.GetSearchHighlightColor()
				});
				num = item.Index + item.Length;
			}
		}
		Title.Inlines.Add(new Run(text.Substring(num)));
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/search/searchprojectitemtemplate.xaml", UriKind.Relative);
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
			Title = (TextBlock)target;
		}
		else
		{
			_contentLoaded = true;
		}
	}
}
