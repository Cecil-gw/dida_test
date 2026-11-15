using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using Emoji.Wpf;
using ticktick_WPF.Models;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CustomControl;

namespace ticktick_WPF.Views.Tag;

public class MergeTagWindow : Window, IOkCancelWindow, IComponentConnector
{
	public string MergeTag;

	private readonly TagModel _tag;

	internal EmjTextBlock TagHint;

	internal CustomComboBox TagComobox;

	internal Button SaveButton;

	private bool _contentLoaded;

	public MergeTagWindow(TagModel tag)
	{
		string displayName = tag.GetDisplayName();
		_tag = tag;
		InitializeComponent();
		TagHint.Text = string.Format(Utils.GetString("MergeTagHint"), displayName);
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	private async void OnSaveClick(object sender, RoutedEventArgs e)
	{
		SaveButton.Content = Utils.GetString("Merging");
		SaveButton.IsEnabled = false;
		await TagService.MergeTag(_tag.name, MergeTag);
		Close();
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void OnWindowLoaded(object sender, RoutedEventArgs e)
	{
		List<TagModel> source = (from t in TagDataHelper.GetTags()
			where t.name != _tag.name
			select t).ToList();
		if (source.Any((TagModel t) => t.parent == _tag.name))
		{
			source = source.Where((TagModel t) => !t.IsChild()).ToList();
		}
		ObservableCollection<ComboBoxViewModel> observableCollection = new ObservableCollection<ComboBoxViewModel>(from t in source.Select((TagModel t) => t.GetDisplayName()).ToList()
			select new ComboBoxViewModel(t, t, 32.0));
		TagComobox.Init(observableCollection, observableCollection[0]);
		TagComobox.ItemSelected += OnTagSelected;
		TagComobox.SelectedText.Text = Utils.GetString("SelectTag");
	}

	private void OnTagSelected(object sender, ComboBoxViewModel e)
	{
		MergeTag = e.Title;
		SaveButton.IsEnabled = true;
	}

	public void OnCancel()
	{
		if (!TagComobox.IsOpen)
		{
			Close();
		}
	}

	public async void Ok()
	{
		if (!TagComobox.IsOpen && SaveButton.IsEnabled)
		{
			SaveButton.Content = Utils.GetString("Merging");
			SaveButton.IsEnabled = false;
			await TagService.MergeTag(_tag.name, MergeTag);
			Close();
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/tag/mergetagwindow.xaml", UriKind.Relative);
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
			((MergeTagWindow)target).Loaded += OnWindowLoaded;
			break;
		case 2:
			TagHint = (EmjTextBlock)target;
			break;
		case 3:
			TagComobox = (CustomComboBox)target;
			break;
		case 4:
			SaveButton = (Button)target;
			SaveButton.Click += OnSaveClick;
			break;
		case 5:
			((Button)target).Click += OnCancelClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
