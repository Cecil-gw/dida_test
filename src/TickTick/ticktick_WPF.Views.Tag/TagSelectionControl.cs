using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Models;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.MainListView;
using ticktick_WPF.Views.QuickAdd;

namespace ticktick_WPF.Views.Tag;

public class TagSelectionControl : UserControl, IQuickInput, IComponentConnector, IStyleConnector
{
	public static readonly DependencyProperty BatchModeProperty = DependencyProperty.Register("BatchMode", typeof(bool), typeof(TagSelectionControl), new PropertyMetadata(false, OnBatchModeChanged));

	public static readonly DependencyProperty SearchModeProperty = DependencyProperty.Register("SearchMode", typeof(bool), typeof(TagSelectionControl), new PropertyMetadata(false, null));

	public static readonly DependencyProperty MaxCountProperty = DependencyProperty.Register("MaxCount", typeof(int), typeof(TagSelectionControl), new PropertyMetadata(8, null));

	private bool _inFilter;

	private bool _showFullTagTitle = true;

	private TagSelectData _originalSelectedTags = new TagSelectData();

	private TagSelectData _selectedData = new TagSelectData();

	internal TagSelectionControl RootView;

	internal ListView TagItems;

	internal StackPanel EmptyPanel;

	private bool _contentLoaded;

	public bool BatchMode
	{
		get
		{
			return (bool)GetValue(BatchModeProperty);
		}
		set
		{
			SetValue(BatchModeProperty, value);
		}
	}

	public bool SearchMode
	{
		private get
		{
			return (bool)GetValue(SearchModeProperty);
		}
		set
		{
			SetValue(SearchModeProperty, value);
		}
	}

	public int MaxCount
	{
		private get
		{
			return (int)GetValue(MaxCountProperty);
		}
		set
		{
			SetValue(MaxCountProperty, value);
		}
	}

	private ObservableCollection<TagDisplayViewModel> DisplayTags => (ObservableCollection<TagDisplayViewModel>)TagItems.ItemsSource;

	public bool CanCreateTag { private get; set; } = true;

	public event EventHandler<int> TagsCountChanged;

	public event EventHandler<string> TagSelected;

	public event EventHandler<TagDisplayViewModel> TagSelectedChanged;

	public TagSelectionControl()
	{
		InitializeComponent();
		AttachMouseWheelHandler();
		base.MaxHeight = 36 * MaxCount;
	}

	public TagSelectionControl(List<string> tags)
	{
		InitializeComponent();
		AttachMouseWheelHandler();
		base.MaxHeight = 36 * MaxCount;
	}

	private static void OnBatchModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is TagSelectionControl tagSelectionControl)
		{
			tagSelectionControl._showFullTagTitle = !(bool)e.NewValue;
		}
	}

	public bool Filter(string content, List<string> selected = null)
	{
		content = content?.Trim() ?? "";
		if (!_inFilter && !string.IsNullOrEmpty(content))
		{
			_selectedData = (BatchMode ? GetSelectedData() : new TagSelectData());
			_inFilter = true;
		}
		if (string.IsNullOrEmpty(content))
		{
			SetSelectedTags(_selectedData);
		}
		if (!NameUtils.IsValidName(content))
		{
			TagItems.ItemsSource = new ObservableCollection<TagDisplayViewModel>();
			TagItems.Visibility = Visibility.Collapsed;
			this.TagsCountChanged?.Invoke(this, 0);
		}
		else
		{
			string text = content.Trim();
			string text2 = text.ToLower();
			ObservableCollection<TagDisplayViewModel> displayTags = GetDisplayTags(text);
			if ((selected == null || !selected.Contains(text)) && text.Length <= 64 && !string.IsNullOrEmpty(text2) && !IsTagMatched(text2, displayTags) && CanCreateTag)
			{
				displayTags.Add(TagDisplayViewModel.BuildAddTag(content));
			}
			if (displayTags.Count > 0)
			{
				displayTags[0].Focused = true;
			}
			if (BatchMode)
			{
				EmptyPanel.Visibility = ((displayTags.Count != 0) ? Visibility.Collapsed : Visibility.Visible);
			}
			TagItems.ItemsSource = displayTags;
			TagItems.Visibility = ((displayTags.Count <= 0) ? Visibility.Collapsed : Visibility.Visible);
			this.TagsCountChanged?.Invoke(this, displayTags.Count);
		}
		if (TagItems.ItemsSource != null && TagItems.ItemsSource is ObservableCollection<TagDisplayViewModel> source)
		{
			return source.Any();
		}
		return false;
	}

	public bool IsTag()
	{
		return true;
	}

	public void Move(bool forward)
	{
		if (forward)
		{
			MoveUp();
		}
		else
		{
			MoveDown();
		}
	}

	public void TrySelectItem(bool exactly = false)
	{
		TrySelectFocusedTag();
	}

	public void SetTags(List<string> tags)
	{
	}

	public List<string> GetSelectedTags()
	{
		return (from tag in DisplayTags
			where tag.Selected
			select tag.Tag).Distinct().ToList();
	}

	public TagSelectData GetSelectedData()
	{
		return new TagSelectData
		{
			OmniSelectTags = ((from tag in DisplayTags?.Where((TagDisplayViewModel tag) => tag.Selected)
				select tag.Tag).Distinct().ToList() ?? new List<string>()),
			PartSelectTags = ((from tag in DisplayTags?.Where((TagDisplayViewModel tag) => tag.PartSelected)
				select tag.Tag).Distinct().ToList() ?? new List<string>())
		};
	}

	public Dictionary<string, bool> GetTagSelectedDictionary()
	{
		Dictionary<string, bool> dictionary = new Dictionary<string, bool>();
		if (DisplayTags != null && DisplayTags.Count > 0)
		{
			foreach (TagDisplayViewModel displayTag in DisplayTags)
			{
				if (!dictionary.ContainsKey(displayTag.Tag))
				{
					dictionary.Add(displayTag.Tag, displayTag.Selected);
				}
			}
		}
		return dictionary;
	}

	public void SetSelectedTags(TagSelectData tags)
	{
		_originalSelectedTags = tags ?? new TagSelectData();
		InitTagData(_originalSelectedTags);
		_inFilter = false;
	}

	private static bool IsTagMatched(string prefix, IEnumerable<TagDisplayViewModel> tags)
	{
		return tags.FirstOrDefault((TagDisplayViewModel tag) => tag.Tag == prefix) != null;
	}

	private ObservableCollection<TagDisplayViewModel> GetDisplayTags(string content)
	{
		List<TagModel> tags = TagDataHelper.GetTags();
		ObservableCollection<TagDisplayViewModel> observableCollection = new ObservableCollection<TagDisplayViewModel>();
		TagModel tagModel = null;
		foreach (TagModel item in tags)
		{
			if (item.name == content.ToLower())
			{
				tagModel = item;
			}
			else if (string.IsNullOrEmpty(content) || IsTagCandidate(item, content))
			{
				observableCollection.Add(new TagDisplayViewModel(item, _selectedData.OmniSelectTags.Contains(item.name), _selectedData.PartSelectTags.Contains(item.name), _showFullTagTitle));
			}
		}
		observableCollection = new ObservableCollection<TagDisplayViewModel>(from tag in observableCollection
			orderby tag.Selected descending, tag.PartSelected descending
			select tag);
		if (tagModel != null)
		{
			observableCollection.Insert(0, new TagDisplayViewModel(tagModel, !BatchMode || _selectedData.OmniSelectTags.Contains(tagModel.name), _selectedData.PartSelectTags.Contains(tagModel.name), _showFullTagTitle));
		}
		return observableCollection;
	}

	private static bool IsTagCandidate(TagModel tag, string text)
	{
		string text2 = text.ToLower();
		Pinyin pinyin = PinyinUtils.ToPinyin(tag.name);
		if (tag.name.Contains(text2) || pinyin.Contains(text2) || (!string.IsNullOrEmpty(tag.pinyin) && tag.pinyin.Contains(text2)) || (!string.IsNullOrEmpty(tag.inits) && tag.inits.Contains(text2)))
		{
			return true;
		}
		return false;
	}

	public void MoveUp()
	{
		if (!TryFocusFirstTag())
		{
			MoveNext(GetNextIndex(up: true));
		}
	}

	private int GetNextIndex(bool up)
	{
		int focusedIndex = GetFocusedIndex();
		int num = focusedIndex;
		if (focusedIndex == -1)
		{
			return -1;
		}
		do
		{
			num += ((!up) ? 1 : (-1));
			if (num < 0)
			{
				num = DisplayTags.Count - 1;
			}
			if (num > DisplayTags.Count - 1)
			{
				num = 0;
			}
			if (DisplayTags.Count > num && num >= 0 && !DisplayTags[num].Collapsed)
			{
				return num;
			}
		}
		while (num != focusedIndex);
		return focusedIndex;
	}

	public void MoveDown()
	{
		if (!TryFocusFirstTag())
		{
			MoveNext(GetNextIndex(up: false));
		}
	}

	private void MoveNext(int nextIndex)
	{
		if (nextIndex >= 0 && nextIndex <= DisplayTags.Count - 1)
		{
			ClearCurrenFocused();
			TagDisplayViewModel tagDisplayViewModel = DisplayTags[nextIndex];
			tagDisplayViewModel.Focused = true;
			TagItems.ScrollIntoView(tagDisplayViewModel);
		}
	}

	private void ClearCurrenFocused()
	{
		TagDisplayViewModel focusedTag = GetFocusedTag();
		if (focusedTag != null)
		{
			focusedTag.Focused = false;
		}
	}

	private bool TryFocusFirstTag()
	{
		if (GetFocusedTag() == null && DisplayTags.Count > 0)
		{
			DisplayTags[0].Focused = true;
			return true;
		}
		return false;
	}

	private TagDisplayViewModel GetFocusedTag()
	{
		return DisplayTags?.FirstOrDefault((TagDisplayViewModel tag) => tag.Focused);
	}

	private void AttachMouseWheelHandler()
	{
		TagItems.AddHandler(UIElement.PreviewMouseWheelEvent, new MouseWheelEventHandler(OnTagItemsPreviewMouseWheel), handledEventsToo: true);
	}

	private void OnTagItemsPreviewMouseWheel(object sender, MouseWheelEventArgs e)
	{
		TagItems.ApplyTemplate();
		ScrollViewer scrollViewer = (TagItems.Template.FindName("ScrollViewer", TagItems) as ScrollViewer) ?? Utils.FindSingleVisualChildren<ScrollViewer>(TagItems);
		if (scrollViewer is SmoothScrollViewer smoothScrollViewer)
		{
			smoothScrollViewer.HandleSmoothMouseWheel(e);
			return;
		}
		scrollViewer?.ScrollToVerticalOffset(scrollViewer.VerticalOffset - (double)e.Delta / 2.0);
		e.Handled = true;
	}

	private int GetFocusedIndex()
	{
		TagDisplayViewModel focusedTag = GetFocusedTag();
		if (focusedTag != null && DisplayTags != null && DisplayTags.Any())
		{
			return DisplayTags.IndexOf(focusedTag);
		}
		return -1;
	}

	public async void TrySelectFocusedTag(bool add = true)
	{
		TagDisplayViewModel focused = GetFocusedTag();
		if (focused == null)
		{
			return;
		}
		string tag = ((!focused.IsAddTag) ? (focused.FullTitle ?? focused.Title).Trim() : focused.Tag.Trim());
		if (NameUtils.IsValidName(tag))
		{
			if (focused.IsAddTag && add)
			{
				await TagService.TryCreateTag(tag);
				ListViewContainer.ReloadProjectData();
			}
			this.TagSelected?.Invoke(this, tag);
		}
		focused.Selected = !focused.Selected;
		focused.PartSelected = false;
		OnItemSelectedChanged(focused);
	}

	private void OnItemClick(object sender, MouseButtonEventArgs e)
	{
		if (!BatchMode)
		{
			TrySelectFocusedTag();
		}
		else
		{
			ToggleItemSelected(sender);
		}
	}

	private void ToggleItemSelected(object sender)
	{
		if (!(sender is Grid grid))
		{
			return;
		}
		object dataContext = grid.DataContext;
		TagDisplayViewModel model = dataContext as TagDisplayViewModel;
		if (model == null)
		{
			return;
		}
		if (model.PartSelected)
		{
			model.PartSelected = false;
			model.Selected = true;
		}
		else
		{
			model.Selected = !model.Selected;
		}
		if (_inFilter && _selectedData != null)
		{
			_selectedData.PartSelectTags.RemoveAll((string t) => string.Equals(t, model.Tag, StringComparison.CurrentCultureIgnoreCase));
			if (model.Selected)
			{
				if (!_selectedData.OmniSelectTags.Contains(model.Tag, StringComparer.OrdinalIgnoreCase))
				{
					_selectedData.OmniSelectTags.Add(model.Tag);
				}
			}
			else
			{
				_selectedData.OmniSelectTags.RemoveAll((string t) => string.Equals(t, model.Tag, StringComparison.CurrentCultureIgnoreCase));
			}
		}
		if (SearchMode)
		{
			if (model.Selected)
			{
				if (model.IsAllTag)
				{
					foreach (TagDisplayViewModel displayTag in DisplayTags)
					{
						if (!displayTag.IsAllTag)
						{
							displayTag.Selected = false;
						}
					}
				}
				else
				{
					SetAllTagItemSelected(selected: false);
				}
			}
			else if (DisplayTags.Count((TagDisplayViewModel tag) => tag.Selected) == 0)
			{
				SetAllTagItemSelected(selected: true);
			}
		}
		OnItemSelectedChanged(model);
	}

	private void OnItemSelectedChanged(TagDisplayViewModel model)
	{
		if (SearchMode)
		{
			if (model.IsParent)
			{
				List<TagDisplayViewModel> children = model.Children;
				if (children != null && children.Count > 0)
				{
					model.Children?.ForEach(delegate(TagDisplayViewModel c)
					{
						c.Selected = model.Selected;
					});
					model.ChildSelected = model.Selected;
				}
			}
			if (string.IsNullOrEmpty(model.Parent))
			{
				return;
			}
			TagDisplayViewModel tagDisplayViewModel = DisplayTags.FirstOrDefault((TagDisplayViewModel t) => t.Tag == model.Parent);
			if (tagDisplayViewModel != null)
			{
				tagDisplayViewModel.Selected = false;
				tagDisplayViewModel.ChildSelected = model.Selected || (tagDisplayViewModel.Children?.Any((TagDisplayViewModel c) => c.Selected || c.PartSelected) ?? false);
			}
			return;
		}
		DisplayTags.Where((TagDisplayViewModel t) => t.Tag == model.Tag).ToList().ForEach(delegate(TagDisplayViewModel m)
		{
			m.Selected = model.Selected;
			m.PartSelected = model.PartSelected;
		});
		if (!string.IsNullOrEmpty(model.Parent))
		{
			DisplayTags.Where((TagDisplayViewModel t) => t.Tag == model.Parent).ToList().ForEach(delegate(TagDisplayViewModel m)
			{
				m.ChildSelected = m.Children?.Any((TagDisplayViewModel c) => c.Selected || c.PartSelected) ?? false;
			});
		}
		this.TagSelectedChanged?.Invoke(this, model);
	}

	private void SetAllTagItemSelected(bool selected)
	{
		SetTagItemSelected("*tag", selected);
	}

	private void SetTagItemSelected(string tagName, bool selected)
	{
		TagDisplayViewModel tagDisplayViewModel = DisplayTags.FirstOrDefault((TagDisplayViewModel item) => string.Equals(item.Tag, tagName, StringComparison.OrdinalIgnoreCase));
		if (tagDisplayViewModel != null)
		{
			tagDisplayViewModel.Selected = selected;
		}
	}

	private void OnControlLoaded(object sender, RoutedEventArgs e)
	{
		if (DisplayTags == null || !DisplayTags.Any())
		{
			InitTagData(_originalSelectedTags ?? new TagSelectData());
		}
	}

	private void InitTagData(TagSelectData tagData)
	{
		List<TagModel> tags = TagDataHelper.GetTags();
		List<TagDisplayViewModel> list = new List<TagDisplayViewModel>();
		ObservableCollection<TagDisplayViewModel> observableCollection = new ObservableCollection<TagDisplayViewModel>();
		if (tags != null && tags.Any())
		{
			List<TagModel> source = tags.Where((TagModel t) => t.IsChild()).ToList();
			foreach (TagModel tag in tags.Where((TagModel tagModel) => !tagModel.IsChild()))
			{
				TagDisplayViewModel model = new TagDisplayViewModel(tag, tagData.OmniSelectTags.Contains(tag.name, StringComparer.OrdinalIgnoreCase), tagData.PartSelectTags.Contains(tag.name, StringComparer.OrdinalIgnoreCase), _showFullTagTitle)
				{
					IsParent = tag.IsParent()
				};
				if (model.IsParent)
				{
					model.IsOpen = !tag.collapsed;
					model.Children = (from t in source
						where t.parent == model.Tag
						select new TagDisplayViewModel(t, tagData.OmniSelectTags.Contains(t.name, StringComparer.OrdinalIgnoreCase), tagData.PartSelectTags.Contains(t.name, StringComparer.OrdinalIgnoreCase), _showFullTagTitle)
						{
							IsSubTag = true,
							Collapsed = (BatchMode && tag.collapsed),
							Parent = tag.name
						}).ToList();
					model.ChildSelected = (model.Children?.Any((TagDisplayViewModel c) => c.Selected || c.PartSelected)).Value;
				}
				list.Add(model);
			}
			if (SearchMode)
			{
				TagDisplayViewModel tagDisplayViewModel = TagDisplayViewModel.BuildAllTag();
				tagDisplayViewModel.Title = Utils.GetString("All");
				if (tagData.OmniSelectTags.Count > 0)
				{
					tagDisplayViewModel.Selected = false;
				}
				TagDisplayViewModel tagDisplayViewModel2 = TagDisplayViewModel.BuildWithTag();
				if (tagData.OmniSelectTags.Contains("*withtags"))
				{
					tagDisplayViewModel2.Selected = true;
				}
				TagDisplayViewModel tagDisplayViewModel3 = TagDisplayViewModel.BuildNoTag();
				if (tagData.OmniSelectTags.Contains("!tag"))
				{
					tagDisplayViewModel3.Selected = true;
				}
				observableCollection.Add(tagDisplayViewModel);
				observableCollection.Add(new TagDisplayViewModel(split: true));
				observableCollection.Add(tagDisplayViewModel2);
				observableCollection.Add(tagDisplayViewModel3);
			}
			foreach (TagDisplayViewModel item in list.Where((TagDisplayViewModel tagDisplayViewModel4) => string.IsNullOrEmpty(tagDisplayViewModel4.Parent)))
			{
				if (item.IsParent)
				{
					List<TagDisplayViewModel> children = item.Children;
					if (children != null && children.Count > 0)
					{
						observableCollection.Add(item);
						item.Children.ForEach(observableCollection.Add);
						continue;
					}
				}
				observableCollection.Add(item);
			}
		}
		if (BatchMode)
		{
			EmptyPanel.Visibility = ((observableCollection.Count != 0) ? Visibility.Collapsed : Visibility.Visible);
		}
		TagItems.ItemsSource = observableCollection;
		if (observableCollection.Count > 0)
		{
			observableCollection[0].Focused = true;
			TagItems.ScrollIntoView(observableCollection[0]);
		}
		this.TagsCountChanged?.Invoke(this, observableCollection.Count);
	}

	private void OnItemEnter(object sender, MouseEventArgs e)
	{
		if (sender is Grid grid)
		{
			ClearCurrenFocused();
			if (grid.DataContext is TagDisplayViewModel { Focused: false } tagDisplayViewModel)
			{
				tagDisplayViewModel.Focused = true;
			}
		}
	}

	private void OnCollapsedClick(object sender, MouseButtonEventArgs e)
	{
		if (!(sender is Grid { DataContext: var dataContext }))
		{
			return;
		}
		TagDisplayViewModel model = dataContext as TagDisplayViewModel;
		if (model != null)
		{
			model.IsOpen = !model.IsOpen;
			model.Children?.ForEach(delegate(TagDisplayViewModel m)
			{
				m.Collapsed = !model.IsOpen;
			});
			e.Handled = true;
		}
	}

	public void OnTagAdded(string tag)
	{
		_selectedData.OmniSelectTags.Add(tag);
	}

	private void OnCollapsedMouseDown(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
	}

	private void OnBringIntoView(object sender, RequestBringIntoViewEventArgs e)
	{
		e.Handled = true;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/tag/tagselectioncontrol.xaml", UriKind.Relative);
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
			RootView = (TagSelectionControl)target;
			RootView.Loaded += OnControlLoaded;
			break;
		case 2:
			TagItems = (ListView)target;
			break;
		case 6:
			EmptyPanel = (StackPanel)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IStyleConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 3:
			((Grid)target).RequestBringIntoView += OnBringIntoView;
			break;
		case 4:
			((Grid)target).MouseEnter += OnItemEnter;
			((Grid)target).MouseLeftButtonUp += OnItemClick;
			break;
		case 5:
			((Grid)target).PreviewMouseLeftButtonUp += OnCollapsedClick;
			break;
		}
	}
}
