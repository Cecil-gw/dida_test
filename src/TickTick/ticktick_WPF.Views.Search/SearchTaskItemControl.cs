using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using Emoji.Wpf;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Views.TaskList;

namespace ticktick_WPF.Views.Search;

public class SearchTaskItemControl : UserControl, IComponentConnector
{
	internal TaskCheckIcon CheckIcon;

	internal Border TaskSelectIcon;

	internal TaskTitleBox Title;

	internal EmjTextBlock ProjectTitle;

	internal TextBlock ContentText;

	internal CommentSearchLabel CommentText;

	private bool _contentLoaded;

	public SearchTaskItemControl()
	{
		InitializeComponent();
		base.DataContextChanged += OnDataChanged;
		Title.SetupPreSearchRender();
		Title.IsHitTestVisible = false;
		CommentText.CommentText.FontSize = 12.0;
	}

	private void OnDataChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (base.DataContext is SearchTaskItemViewModel searchTaskItemViewModel)
		{
			string text = searchTaskItemViewModel.SourceModel.Title;
			if (string.IsNullOrEmpty(text))
			{
				text = Utils.GetString("NoTitle");
			}
			Title.SetText(text);
			if (searchTaskItemViewModel.IsNormal)
			{
				CheckIcon.SetSize(17);
				CheckIcon.SetIconColor(searchTaskItemViewModel.SourceModel.Type, searchTaskItemViewModel.SourceModel.Priority, searchTaskItemViewModel.SourceModel.Status);
				CheckIcon.Cursor = ((!searchTaskItemViewModel.SourceModel.Editable) ? Cursors.No : ((searchTaskItemViewModel.SourceModel.IsEvent || searchTaskItemViewModel.SourceModel.IsNote || searchTaskItemViewModel.SourceModel.IsCourse) ? Cursors.Arrow : Cursors.Hand));
			}
			else if (searchTaskItemViewModel.IsTask)
			{
				TaskSelectIcon.BorderBrush = Utils.GetPriorityColor(searchTaskItemViewModel.SourceModel.Priority);
			}
			if (!string.IsNullOrWhiteSpace(searchTaskItemViewModel.Content))
			{
				ContentText.Visibility = Visibility.Visible;
				SetContent(searchTaskItemViewModel.Content, ContentText);
				ContentText.Margin = new Thickness(47.0, 1.0, 130.0, string.IsNullOrWhiteSpace(searchTaskItemViewModel.CommentStr) ? 4 : 0);
			}
			if (!string.IsNullOrWhiteSpace(searchTaskItemViewModel.CommentStr))
			{
				CommentText.Visibility = Visibility.Visible;
				SetContent(searchTaskItemViewModel.CommentStr, CommentText.CommentText);
			}
		}
	}

	private void SetContent(string content, TextBlock block)
	{
		block.Inlines.Clear();
		string text = content;
		text = ((text.Length > 300) ? text.Substring(0, 299) : text);
		text = text.Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " ")
			.Replace("    ", " ");
		MatchCollection matchCollection = SearchHelper.PreSearchRegex.Matches(text.ToLower());
		if (matchCollection.Count > 0)
		{
			for (int i = 0; i < matchCollection.Count; i++)
			{
				int num = ((i > 0) ? (matchCollection[i - 1].Index + matchCollection[i - 1].Length) : 0);
				string text2 = text.Substring(num, matchCollection[i].Index - num);
				block.Inlines.Add(text2);
				block.Inlines.Add(new Run(text.Substring(matchCollection[i].Index, matchCollection[i].Length))
				{
					Background = SearchHelper.GetSearchHighlightColor()
				});
			}
			int num2 = matchCollection[matchCollection.Count - 1].Index + matchCollection[matchCollection.Count - 1].Length;
			string text3 = text.Substring(num2, text.Length - num2);
			block.Inlines.Add(text3);
		}
		else
		{
			block.Inlines.Add(text);
		}
	}

	private async void OnCheckIconClick(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext is SearchTaskItemViewModel searchTaskItemViewModel && CheckIcon.Cursor == Cursors.Hand)
		{
			e.Handled = true;
			if (!searchTaskItemViewModel.TogglingStatus)
			{
				searchTaskItemViewModel.TogglingStatus = true;
				searchTaskItemViewModel.Status = ((searchTaskItemViewModel.SourceModel.Status == 0) ? 2 : 0);
				searchTaskItemViewModel.ResetIcon();
				await TaskService.SetTaskStatus(searchTaskItemViewModel.SourceModel.Id, searchTaskItemViewModel.Status, needUndo: true, App.Window);
				Utils.FindParent<SearchInputControl>(this)?.OnItemCheckClick();
			}
		}
	}

	private void OnProjectClick(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext is SearchTaskItemViewModel searchTaskItemViewModel)
		{
			e.Handled = true;
			Utils.FindParent<SearchInputControl>(this)?.OnItemProjectClick(searchTaskItemViewModel.SourceModel.ProjectId);
		}
	}

	private void CancelParentClick(object sender, MouseButtonEventArgs e)
	{
		Utils.FindParent<SearchInputControl>(this)?.OnUnlinkParent();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/search/searchtaskitemcontrol.xaml", UriKind.Relative);
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
			CheckIcon = (TaskCheckIcon)target;
			break;
		case 2:
			TaskSelectIcon = (Border)target;
			break;
		case 3:
			Title = (TaskTitleBox)target;
			break;
		case 4:
			ProjectTitle = (EmjTextBlock)target;
			ProjectTitle.PreviewMouseLeftButtonUp += OnProjectClick;
			break;
		case 5:
			((Border)target).MouseLeftButtonUp += CancelParentClick;
			break;
		case 6:
			ContentText = (TextBlock)target;
			break;
		case 7:
			CommentText = (CommentSearchLabel)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
