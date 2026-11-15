using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.MarkDown;

public class StyleMenu : UserControl, IComponentConnector, IStyleConnector
{
	public static readonly DependencyProperty FilterContentProperty = DependencyProperty.Register("FilterContent", typeof(string), typeof(StyleMenu));

	public static readonly DependencyProperty EmptyVisibilityProperty = DependencyProperty.Register("EmptyVisibility", typeof(Visibility), typeof(StyleMenu));

	public static readonly DependencyProperty MoreVisibilityProperty = DependencyProperty.Register("MoreVisibility", typeof(Visibility), typeof(StyleMenu));

	public bool FromSplash;

	private bool _addSubtaskEnabled = true;

	private int _maxHeadingLevel;

	private int _itemCount;

	private bool _firstHover = true;

	internal ItemsControl StyleItems;

	private bool _contentLoaded;

	public Visibility MoreVisibility
	{
		get
		{
			return (Visibility)GetValue(MoreVisibilityProperty);
		}
		set
		{
			SetValue(MoreVisibilityProperty, value);
			if (value == Visibility.Collapsed)
			{
				RemoveSecondLevelItems();
			}
		}
	}

	public bool AddSubtaskEnabled
	{
		get
		{
			return _addSubtaskEnabled;
		}
		set
		{
			_addSubtaskEnabled = value;
			OnAddSubtaskEnabledChanged(value);
		}
	}

	public int MatchCount { get; set; }

	public string FilterContent
	{
		get
		{
			return (string)GetValue(FilterContentProperty);
		}
		set
		{
			SetValue(FilterContentProperty, value);
			FilterContentChanged();
		}
	}

	public Visibility EmptyVisibility
	{
		get
		{
			return (Visibility)GetValue(EmptyVisibilityProperty);
		}
		set
		{
			SetValue(EmptyVisibilityProperty, value);
		}
	}

	public int MaxHeadingLevel
	{
		get
		{
			return _maxHeadingLevel;
		}
		set
		{
			_maxHeadingLevel = value;
			NotifyMaxHeadingLevel(value);
		}
	}

	public event EventHandler<string> EditorStyleAction;

	public event EventHandler TryDismiss;

	private void OnAddSubtaskEnabledChanged(bool visible)
	{
		SetActionEnabled("Subtask", visible);
	}

	public void Move(bool forward)
	{
		_firstHover = true;
		int num = 0;
		List<StyleMenuItemViewModel> list = (from it in GetItemsData()
			where it.IsVisible
			select it).ToList();
		List<StyleMenuItemViewModel> list2 = list.Where((StyleMenuItemViewModel it) => it.IsSelected).ToList();
		if (list2.Any())
		{
			num = list.IndexOf(list2.First());
			list2.ForEach(delegate(StyleMenuItemViewModel it)
			{
				it.IsSelected = false;
			});
			if (!forward)
			{
				num++;
				if (num >= list.Count)
				{
					num = 0;
				}
			}
			else
			{
				num--;
				if (num < 0)
				{
					num = list.Count - 1;
				}
			}
		}
		if (num >= 0 && num < list.Count)
		{
			list[num].IsSelected = true;
		}
	}

	public void SelectIndex()
	{
		StyleMenuItemViewModel styleMenuItemViewModel = GetItemsData()?.FirstOrDefault((StyleMenuItemViewModel it) => it.IsSelected && it.IsVisible);
		if (styleMenuItemViewModel != null)
		{
			NotifyAction(null, styleMenuItemViewModel.Action);
		}
	}

	private void NotifyMaxHeadingLevel(int level)
	{
		SetActionVisible("Heading4", visible: false);
		SetActionVisible("Heading5", visible: false);
		SetActionVisible("Heading6", visible: false);
		if (level >= 3)
		{
			SetActionVisible("Heading4");
		}
		if (level >= 4)
		{
			SetActionVisible("Heading5");
		}
		if (level >= 5)
		{
			SetActionVisible("Heading6");
		}
	}

	public void SetCurrentActon(string action, bool forceShow = true)
	{
		if (forceShow && (action.StartsWith("Heading") || string.Equals(action, "Text") || action.StartsWith("_")))
		{
			SetActionVisible("Text");
		}
		foreach (StyleMenuItemViewModel itemsDatum in GetItemsData())
		{
			itemsDatum.IsChecked = itemsDatum.Action == action;
		}
	}

	public void UpdateContext(string action, int maxHeadingLevel, bool addSubtaskEnabled, bool forceShow = true)
	{
		MaxHeadingLevel = maxHeadingLevel;
		AddSubtaskEnabled = addSubtaskEnabled;
		SetCurrentActon(action, forceShow);
	}

	private void SetActionEnabled(string nextAction, bool enabled = true)
	{
		StyleMenuItemViewModel styleMenuItemViewModel = GetItemsData().FirstOrDefault((StyleMenuItemViewModel it) => it.Action == nextAction);
		if (styleMenuItemViewModel != null)
		{
			styleMenuItemViewModel.Enabled = enabled;
			styleMenuItemViewModel.IsVisible = enabled;
		}
	}

	private void SetActionVisible(string nextAction, bool visible = true)
	{
		StyleMenuItemViewModel styleMenuItemViewModel = GetItemsData().FirstOrDefault((StyleMenuItemViewModel it) => it.Action == nextAction);
		if (styleMenuItemViewModel != null)
		{
			styleMenuItemViewModel.IsVisible = visible;
		}
	}

	public StyleMenu(Visibility moreVisibility)
	{
		InitializeComponent();
		MoreVisibility = moreVisibility;
		EmptyVisibility = Visibility.Collapsed;
		base.DataContext = this;
		ObservableCollection<StyleMenuItemViewModel> source = new ObservableCollection<StyleMenuItemViewModel>
		{
			new StyleMenuItemViewModel("Text", Utils.GetString("body_text"))
			{
				OriginalVisible = false
			},
			new StyleMenuItemViewModel("Heading1", Utils.GetString("heading_1")),
			new StyleMenuItemViewModel("Heading2", Utils.GetString("heading_2")),
			new StyleMenuItemViewModel("Heading3", Utils.GetString("heading_3")),
			new StyleMenuItemViewModel("Heading4", Utils.GetString("heading_4"))
			{
				OriginalVisible = false
			},
			new StyleMenuItemViewModel("Heading5", Utils.GetString("heading_5"))
			{
				OriginalVisible = false
			},
			new StyleMenuItemViewModel("Heading6", Utils.GetString("heading_6"))
			{
				OriginalVisible = false
			},
			new StyleMenuItemViewModel("BulletList", Utils.GetString("md_bulleted_list")),
			new StyleMenuItemViewModel("NumberedList", Utils.GetString("md_numbered_list")),
			new StyleMenuItemViewModel("CheckItem", Utils.GetString("md_check_item")),
			new StyleMenuItemViewModel("Quote", Utils.GetString("quote"))
		};
		ObservableCollection<StyleMenuItemViewModel> source2 = new ObservableCollection<StyleMenuItemViewModel>
		{
			new StyleMenuItemViewModel("SplitLine", Utils.GetString("horizontal_line")),
			new StyleMenuItemViewModel("Attachment", Utils.GetString("Attachment"))
			{
				ShowSplitLine = true
			},
			new StyleMenuItemViewModel("Subtask", Utils.GetString("Subtask")),
			new StyleMenuItemViewModel("Tag", Utils.GetString("tag")),
			new StyleMenuItemViewModel("LinkTask", Utils.GetString("linked_task_note"))
		};
		List<StyleMenuItemViewModel> list = source.ToList();
		if (MoreVisibility == Visibility.Visible)
		{
			list.AddRange(source2.ToList());
		}
		StyleItems.ItemsSource = new ObservableCollection<StyleMenuItemViewModel>(list);
	}

	private void RemoveSecondLevelItems()
	{
		List<string> secondLevels = new List<string> { "SplitLine", "Attachment", "Subtask", "Tag", "LinkTask" };
		ObservableCollection<StyleMenuItemViewModel> itemsData = GetItemsData();
		if (itemsData == null)
		{
			return;
		}
		foreach (StyleMenuItemViewModel item in itemsData.Where((StyleMenuItemViewModel item) => secondLevels.Contains(item.Action)).ToList())
		{
			itemsData.Remove(item);
		}
	}

	public StyleMenu()
		: this(Visibility.Collapsed)
	{
	}

	public void Reset()
	{
		_firstHover = true;
		foreach (StyleMenuItemViewModel itemsDatum in GetItemsData())
		{
			itemsDatum.IsVisible = itemsDatum.OriginalVisible;
			itemsDatum.IsSelected = false;
			itemsDatum.ShowSplitLine = itemsDatum.Action == "Attachment" && itemsDatum.IsVisible;
		}
	}

	private ObservableCollection<StyleMenuItemViewModel> GetItemsData()
	{
		return StyleItems.ItemsSource as ObservableCollection<StyleMenuItemViewModel>;
	}

	private void FilterContentChanged()
	{
		MatchCount = 0;
		foreach (StyleMenuItemViewModel itemsDatum in GetItemsData())
		{
			if (itemsDatum.Enabled)
			{
				string text = itemsDatum.Text.ToLower();
				string text2 = FilterContent.ToLower();
				bool flag = text.Contains(text2);
				if (Utils.IsZhCn() && !flag)
				{
					flag = PinyinUtils.ToPinyin(itemsDatum.Text).Contains(text2);
				}
				if (flag)
				{
					MatchCount++;
				}
				itemsDatum.IsVisible = flag;
				itemsDatum.ShowSplitLine = false;
			}
		}
		EmptyVisibility = ((MatchCount != 0 || string.IsNullOrEmpty(FilterContent)) ? Visibility.Collapsed : Visibility.Visible);
	}

	private void OnStyleClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is FrameworkElement { Tag: string tag })
		{
			e.Handled = true;
			NotifyAction(sender, tag);
		}
	}

	private void NotifyAction(object sender, string action)
	{
		UserActCollectUtils.AddClickEvent("task_detail", (MoreVisibility == Visibility.Collapsed) ? "edit_menu" : "slash_om", MarkDownAction.TryGetLabel(action));
		this.EditorStyleAction?.Invoke(sender, action);
	}

	private async void OnItemMouseMove(object sender, MouseEventArgs e)
	{
		if (_firstHover)
		{
			await Task.Delay(120);
			_firstHover = false;
			return;
		}
		(from it in GetItemsData()
			where it.IsSelected
			select it).ToList().ForEach(delegate(StyleMenuItemViewModel it)
		{
			it.IsSelected = false;
		});
		if (sender is FrameworkElement { DataContext: StyleMenuItemViewModel dataContext })
		{
			dataContext.IsSelected = true;
		}
	}

	private void OnItemMouseLeave(object sender, MouseEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: StyleMenuItemViewModel dataContext })
		{
			dataContext.IsSelected = false;
		}
	}

	public void SelectFirst()
	{
		ObservableCollection<StyleMenuItemViewModel> itemsData = GetItemsData();
		if (itemsData == null)
		{
			return;
		}
		StyleMenuItemViewModel checkedItem = itemsData.FirstOrDefault((StyleMenuItemViewModel it) => it.IsVisible && it.IsChecked);
		if (checkedItem != null)
		{
			itemsData.Where((StyleMenuItemViewModel it) => it.IsSelected && it != checkedItem).ToList().ForEach(delegate(StyleMenuItemViewModel it)
			{
				it.IsSelected = false;
			});
			checkedItem.IsSelected = true;
		}
		else if (itemsData.FirstOrDefault((StyleMenuItemViewModel it) => it.IsVisible && it.IsSelected) == null)
		{
			StyleMenuItemViewModel styleMenuItemViewModel = itemsData.FirstOrDefault((StyleMenuItemViewModel it) => it.IsVisible);
			if (styleMenuItemViewModel != null)
			{
				styleMenuItemViewModel.IsSelected = true;
			}
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/markdown/stylemenu.xaml", UriKind.Relative);
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
			StyleItems = (ItemsControl)target;
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
			((Grid)target).MouseMove += OnItemMouseMove;
			((Grid)target).MouseLeave += OnItemMouseLeave;
			((Grid)target).MouseLeftButtonUp += OnStyleClick;
		}
	}
}
