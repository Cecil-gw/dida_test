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
using ticktick_WPF.Models;

namespace ticktick_WPF.Views.Tag;

public class TagDisplayControl : UserControl, IComponentConnector
{
	public static readonly DependencyProperty CanClickTagProperty = DependencyProperty.Register("CanClickTag", typeof(bool), typeof(TagDisplayControl), new PropertyMetadata(true, null));

	private ObservableCollection<TagLabelViewModel> _models = new ObservableCollection<TagLabelViewModel>();

	public List<string> OriginalTags = new List<string>();

	internal TagDisplayControl Root;

	internal ItemsControl TagItems;

	private bool _contentLoaded;

	public bool CanClickTag
	{
		get
		{
			return (bool)GetValue(CanClickTagProperty);
		}
		set
		{
			SetValue(CanClickTagProperty, value);
		}
	}

	public bool AddPopupOpened { get; set; }

	public event EventHandler<List<string>> TagsChanged;

	public event EventHandler<string> TagClick;

	public event EventHandler<Visibility> TagPanelVisibleChanged;

	public TagDisplayControl()
	{
		InitializeComponent();
		OriginalTags = new List<string>();
		TagItems.ItemsSource = _models;
	}

	public TagDisplayControl(IReadOnlyCollection<string> tags, bool editable)
	{
		OriginalTags = new List<string>(tags);
		InitializeComponent();
		TagItems.ItemsSource = _models;
		LoadTagData(tags, editable);
	}

	public void InitData(IReadOnlyCollection<string> tags, bool editable)
	{
		OriginalTags = new List<string>(tags);
		LoadTagData(tags, editable);
	}

	public async void LoadTagData(IReadOnlyCollection<string> tags, bool editable = true)
	{
		TagLabelViewModel tagLabelViewModel = _models.LastOrDefault((TagLabelViewModel m) => m.IsAdd);
		List<TagLabelViewModel> list = _models.ToList();
		list.Remove(tagLabelViewModel);
		list.ForEach(delegate(TagLabelViewModel m)
		{
			_models.Remove(m);
		});
		if (tags == null || tags.Count <= 0)
		{
			return;
		}
		if (tags.Count > 0)
		{
			List<TagModel> cachedTags = TagDataHelper.GetTags();
			List<TagLabelViewModel> source = (from tag in tags
				let local = cachedTags.FirstOrDefault((TagModel t) => t.name == tag)
				select (local == null) ? new TagLabelViewModel(tag) : new TagLabelViewModel(local)
				{
					SortOrder = cachedTags.IndexOf(local)
				}).ToList();
			source = source.OrderBy((TagLabelViewModel m) => m.SortOrder).ToList();
			for (int num = 0; num < source.Count; num++)
			{
				TagLabelViewModel tagLabelViewModel2 = source[num];
				tagLabelViewModel2.Editable = editable;
				_models.Insert(num, tagLabelViewModel2);
			}
		}
		if (editable)
		{
			if (tagLabelViewModel == null)
			{
				_models.Add(TagLabelViewModel.BuildAddModel());
			}
		}
		else
		{
			_models.Remove(tagLabelViewModel);
		}
	}

	public void AddNewTag(string tag)
	{
		if (!OriginalTags.Contains(tag))
		{
			OriginalTags.Add(tag);
			LoadTagData(OriginalTags);
			this.TagsChanged?.Invoke(this, OriginalTags);
		}
	}

	public bool CheckIfTagExisted(string tag)
	{
		return OriginalTags.Contains(tag.ToLower());
	}

	public void RemoveTag(string tag)
	{
		OriginalTags.Remove(tag);
		LoadTagData(OriginalTags);
		this.TagsChanged?.Invoke(this, OriginalTags);
	}

	public void NotifyTagClick(string tag)
	{
		this.TagClick?.Invoke(this, tag);
	}

	public void NotifyTagPanelVisibleChanged(Visibility visibility)
	{
		SetAddPopupOpened(visibility == Visibility.Visible);
		this.TagPanelVisibleChanged?.Invoke(this, visibility);
	}

	public void SetAddPopupOpened(bool opened)
	{
		AddPopupOpened = opened;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/tag/tagdisplaycontrol.xaml", UriKind.Relative);
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
			Root = (TagDisplayControl)target;
			break;
		case 2:
			TagItems = (ItemsControl)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
