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
using System.Windows.Shapes;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Views.MainListView;

namespace ticktick_WPF.Views.Tag;

public class BatchSetTagControl : UserControl, IComponentConnector, IStyleConnector
{
	private TagSelectData _tagDatas;

	private TagSelectData _rawTagDatas;

	private bool isChanged;

	private bool _locationSet;

	private bool _inFilter;

	private Dictionary<string, long> _sortDict;

	private bool _canDelete;

	private readonly List<string> _needAddTags = new List<string>();

	private int _tabIndex;

	private bool _useInQuickAdd;

	internal RowDefinition SecondRow;

	internal Image SearchIcon;

	internal ScrollViewer SelectedScroller;

	internal ItemsControl SelectedTags;

	internal Line SplitLine;

	internal TagSelectionControl TagItems;

	internal StackPanel SavePanel;

	internal Button SaveButton;

	internal Button CancelButton;

	private bool _contentLoaded;

	public event EventHandler<TagSelectData> TagsSelect;

	public event EventHandler Close;

	public BatchSetTagControl()
	{
		InitializeComponent();
		TagItems.TagSelectedChanged += OnTagSelectedChanged;
	}

	public void Init(TagSelectData tags, bool firstInit, bool useInQuickAdd = false)
	{
		_rawTagDatas = tags.Clone();
		isChanged = false;
		_tagDatas = tags;
		_tabIndex = 0;
		_useInQuickAdd = useInQuickAdd;
		UiUtils.SetSaveButtonTabSelected(SaveButton, tabSelected: false);
		UiUtils.SetCancelButtonTabSelected(CancelButton, tabSelected: false);
		TagItems.SetSelectedTags(_tagDatas);
		SetSelectedTags(_tagDatas);
		if (firstInit || _sortDict == null)
		{
			_sortDict = CacheManager.GetTags().ToDictionary((TagModel t) => t.name, (TagModel t) => t.sortOrder);
		}
		if (useInQuickAdd)
		{
			SetSearchVisible(visible: false);
			SelectedTags.Visibility = Visibility.Collapsed;
			SavePanel.Visibility = Visibility.Collapsed;
			SplitLine.Visibility = Visibility.Collapsed;
		}
		else
		{
			SetSearchVisible(visible: true);
			SelectedTags.Visibility = Visibility.Visible;
			SavePanel.Visibility = Visibility.Visible;
			SplitLine.Visibility = Visibility.Visible;
		}
	}

	private void SetSelectedTags(TagSelectData tagData)
	{
		ObservableCollection<SelectedTagViewModel> observableCollection = new ObservableCollection<SelectedTagViewModel>();
		List<TagModel> tags = CacheManager.GetTags();
		foreach (string tagName in tagData.OmniSelectTags)
		{
			TagModel tagModel = tags.FirstOrDefault((TagModel t) => t.name == tagName);
			observableCollection.Add(new SelectedTagViewModel
			{
				Tag = ((tagModel == null) ? tagName : tagModel.GetDisplayName())
			});
		}
		observableCollection.Add(new SelectedTagViewModel
		{
			IsAdd = true
		});
		SelectedTags.ItemsSource = observableCollection;
		SetSearchVisible(observableCollection.Count <= 1);
	}

	private void SetSearchVisible(bool visible)
	{
		if (!_useInQuickAdd)
		{
			SearchIcon.Visibility = ((!visible) ? Visibility.Collapsed : Visibility.Visible);
			SelectedScroller.Margin = new Thickness(visible ? 32 : 8, 0.0, 12.0, 12.0);
		}
	}

	private void OnTextKeyDown(object sender, KeyEventArgs e)
	{
		_canDelete = false;
		if (e.Key == Key.Back && sender is TextBox { CaretIndex: 0 } textBox && string.IsNullOrEmpty(textBox.Text))
		{
			_canDelete = true;
		}
	}

	private async void OnKeyDown(object sender, KeyEventArgs e)
	{
		switch (e.Key)
		{
		case Key.Up:
			if (_tabIndex < 1)
			{
				TagItems.MoveUp();
			}
			break;
		case Key.Down:
			if (_tabIndex < 1)
			{
				TagItems.MoveDown();
			}
			break;
		case Key.Tab:
			HandleTab(Utils.IfShiftPressed());
			break;
		}
	}

	public void HandleTab(bool shift)
	{
		_tabIndex += 3 + ((!shift) ? 1 : (-1));
		_tabIndex %= 3;
		UiUtils.SetSaveButtonTabSelected(SaveButton, _tabIndex == 1);
		UiUtils.SetCancelButtonTabSelected(CancelButton, _tabIndex == 2);
	}

	private void OnTagAddTextChanged(object sender, string text)
	{
		if (!NameUtils.IsValidName(text))
		{
			Utils.Toast(Utils.GetString("TagNotValid"));
			if (sender is TextBox textBox)
			{
				textBox.SelectAll();
			}
		}
		else if (string.IsNullOrEmpty(text.Trim()))
		{
			TagItems.SetSelectedTags(_tagDatas);
		}
		else
		{
			TagItems.Filter(text, _tagDatas.OmniSelectTags);
		}
	}

	private void SetSaveButtonContent(bool textEmpty)
	{
		if (!textEmpty)
		{
			SaveButton.Content = Utils.GetString("OK");
			SaveButton.IsEnabled = false;
		}
		else
		{
			SaveButton.Content = Utils.GetString("PublicSave");
			SaveButton.IsEnabled = true;
		}
	}

	private void OnTagSelectedChanged(object sender, TagDisplayViewModel tag)
	{
		if (tag.Selected)
		{
			if (!_tagDatas.OmniSelectTags.Contains(tag.Tag, StringComparer.OrdinalIgnoreCase))
			{
				_tagDatas.OmniSelectTags.Add(tag.Tag);
			}
			_tagDatas.PartSelectTags.RemoveAll((string t) => string.Equals(t, tag.Tag, StringComparison.CurrentCultureIgnoreCase));
		}
		else if (tag.PartSelected)
		{
			if (!_tagDatas.PartSelectTags.Contains(tag.Tag, StringComparer.OrdinalIgnoreCase))
			{
				_tagDatas.PartSelectTags.Add(tag.Tag);
			}
			_tagDatas.OmniSelectTags.RemoveAll((string t) => string.Equals(t, tag.Tag, StringComparison.CurrentCultureIgnoreCase));
		}
		else
		{
			_tagDatas.PartSelectTags.RemoveAll((string t) => string.Equals(t, tag.Tag, StringComparison.CurrentCultureIgnoreCase));
			_tagDatas.OmniSelectTags.RemoveAll((string t) => string.Equals(t, tag.Tag, StringComparison.CurrentCultureIgnoreCase));
		}
		if (SelectedTags.ItemsSource is ObservableCollection<SelectedTagViewModel> observableCollection)
		{
			bool flag = !string.IsNullOrEmpty(observableCollection[observableCollection.Count - 1].Tag);
			SelectedTagViewModel selectedTagViewModel = observableCollection.FirstOrDefault((SelectedTagViewModel t) => !t.IsAdd && string.Equals(t.Tag, tag.FullTitle, StringComparison.CurrentCultureIgnoreCase));
			if (tag.Selected && selectedTagViewModel == null)
			{
				if (tag.IsAddTag)
				{
					observableCollection.Insert(0, new SelectedTagViewModel
					{
						Tag = tag.Tag
					});
					_needAddTags.Add(tag.Tag);
				}
				else
				{
					long num = (_sortDict.ContainsKey(tag.Tag) ? _sortDict[tag.Tag] : 0);
					int num2;
					for (num2 = 0; num2 < observableCollection.Count && !observableCollection[num2].IsAdd && (_sortDict.ContainsKey(observableCollection[num2].Tag) ? _sortDict[observableCollection[num2].Tag] : 0) <= num; num2++)
					{
					}
					observableCollection.Insert(num2, new SelectedTagViewModel
					{
						Tag = tag.FullTitle
					});
				}
			}
			else if (!tag.Selected && selectedTagViewModel != null)
			{
				observableCollection.Remove(selectedTagViewModel);
				_needAddTags.Remove(tag.Tag);
			}
			if (flag)
			{
				observableCollection.RemoveAt(observableCollection.Count - 1);
				observableCollection.Add(new SelectedTagViewModel
				{
					IsAdd = true
				});
				TagItems.SetSelectedTags(_tagDatas);
			}
			SetSearchVisible(observableCollection.Count <= 1);
		}
		if (_useInQuickAdd)
		{
			this.TagsSelect?.Invoke(this, _tagDatas);
		}
	}

	private static List<string> GetMergeSelectedTag(List<string> original, Dictionary<string, bool> selected)
	{
		if (selected == null)
		{
			return new List<string>();
		}
		foreach (KeyValuePair<string, bool> item in selected)
		{
			string key = item.Key;
			if (original.Contains(key))
			{
				if (!item.Value)
				{
					original.Remove(key);
				}
			}
			else if (item.Value)
			{
				original.Add(key);
			}
		}
		return original;
	}

	private async void OnSaveClick(object sender, RoutedEventArgs e)
	{
		HashSet<string> localTags = new HashSet<string>(from t in CacheManager.GetTags()
			select t.name);
		bool flag = false;
		foreach (string omniSelectTag in _tagDatas.OmniSelectTags)
		{
			if (!localTags.Contains(omniSelectTag.ToLower()))
			{
				await TagService.TryCreateTag(omniSelectTag);
				flag = true;
			}
		}
		if (flag)
		{
			ListViewContainer.ReloadProjectData();
		}
		this.TagsSelect?.Invoke(this, _tagDatas);
		TryClose();
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		TryClose();
	}

	private void TryClose()
	{
		this.Close?.Invoke(this, null);
	}

	private void OnDeleteClick(object sender, MouseButtonEventArgs e)
	{
		if (!(sender is Border { DataContext: var dataContext }))
		{
			return;
		}
		SelectedTagViewModel model = dataContext as SelectedTagViewModel;
		if (model == null)
		{
			return;
		}
		if (SelectedTags.ItemsSource is ObservableCollection<SelectedTagViewModel> observableCollection)
		{
			observableCollection.Remove(model);
			SetSearchVisible(observableCollection.Count <= 1);
		}
		if (_tagDatas.OmniSelectTags.Contains(model.Tag.ToLower(), StringComparer.OrdinalIgnoreCase))
		{
			_tagDatas.OmniSelectTags.RemoveAll((string t) => t.Equals(model.Tag, StringComparison.OrdinalIgnoreCase));
		}
		TagItems.SetSelectedTags(_tagDatas);
	}

	private void OnKeyUp(object sender, KeyEventArgs e)
	{
		if (_canDelete && e.Key == Key.Back && sender is TextBox { CaretIndex: 0 } && SelectedTags.ItemsSource is ObservableCollection<SelectedTagViewModel> observableCollection)
		{
			SelectedTagViewModel selectedTagViewModel = observableCollection.LastOrDefault((SelectedTagViewModel t) => !t.IsAdd);
			if (selectedTagViewModel != null)
			{
				observableCollection.Remove(selectedTagViewModel);
				if (_tagDatas.OmniSelectTags.Contains(selectedTagViewModel.Tag))
				{
					_tagDatas.OmniSelectTags.Remove(selectedTagViewModel.Tag);
				}
				TagItems.SetSelectedTags(_tagDatas);
			}
			SetSearchVisible(observableCollection.Count <= 1);
		}
		switch (e.Key)
		{
		case Key.Escape:
			TryClose();
			e.Handled = true;
			break;
		case Key.Return:
			switch (_tabIndex)
			{
			case 0:
				TagItems.TrySelectFocusedTag(add: false);
				break;
			case 1:
				OnSaveClick(null, null);
				break;
			case 2:
				TryClose();
				break;
			}
			e.Handled = true;
			break;
		}
	}

	public void ClearEvent()
	{
		this.TagsSelect = null;
	}

	public void SetMaxHeight(double height)
	{
		SecondRow.MaxHeight = height;
		TagItems.MaxHeight = height;
	}

	public bool CheckChange()
	{
		if (isChanged)
		{
			return true;
		}
		isChanged = !_rawTagDatas.IsEqual(_tagDatas);
		return isChanged;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/tag/batchsettagcontrol.xaml", UriKind.Relative);
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
			((BatchSetTagControl)target).PreviewKeyDown += OnKeyDown;
			((BatchSetTagControl)target).KeyUp += OnKeyUp;
			break;
		case 3:
			SecondRow = (RowDefinition)target;
			break;
		case 4:
			SearchIcon = (Image)target;
			break;
		case 5:
			SelectedScroller = (ScrollViewer)target;
			break;
		case 6:
			SelectedTags = (ItemsControl)target;
			break;
		case 7:
			SplitLine = (Line)target;
			break;
		case 8:
			TagItems = (TagSelectionControl)target;
			break;
		case 9:
			SavePanel = (StackPanel)target;
			break;
		case 10:
			SaveButton = (Button)target;
			SaveButton.Click += OnSaveClick;
			break;
		case 11:
			CancelButton = (Button)target;
			CancelButton.Click += OnCancelClick;
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
		if (connectionId == 2)
		{
			((Border)target).MouseLeftButtonUp += OnDeleteClick;
		}
	}
}
