using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using Newtonsoft.Json;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.MarkDown;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views;

public class EmojiSelectControl : UserControl, IComponentConnector, IStyleConnector
{
	public delegate void SelectEmoji(string emoji, bool closePopup);

	private static List<EmojiListModel> _emojiListsInJson;

	private static Dictionary<string, string> _keyEmoji = new Dictionary<string, string>();

	private static List<EmojiKeyViewModel> _emojiKeyViewModel = new List<EmojiKeyViewModel>();

	private static readonly HashSet<string> FoldedEmojiListKeys = new HashSet<string>();

	private Border _lastHoverBd;

	private List<EmojiSelectViewModel> _items;

	private int _countPerRow = 9;

	internal EmojiEditor SearchTextBox;

	internal Border EmojiHeadBorder;

	internal ItemsControl EmojiKeyList;

	internal StackPanel HeadPanel;

	internal TextBlock ResetText;

	internal ListView EmojiList;

	internal Grid SearchEmptyGrid;

	internal Popup EmojiSkinPopup;

	internal ItemsControl SkinItems;

	private bool _contentLoaded;

	public event SelectEmoji EmojiSelected;

	public EmojiSelectControl()
	{
		InitializeComponent();
		InitEmojiKey();
		GetEmojiModelsFromJson();
		EmojiList.PreviewMouseWheel += OnEmojiListMouseWheel;
		InitEmptyView();
	}

	private void InitEmptyView()
	{
		EmptyView emptyView = new EmptyView(120.0);
		emptyView.SetIconsSource(new EmptyIconsSourceModel("SearchResultEmpty"));
		SearchEmptyGrid.Children.Add(emptyView);
	}

	private void InitEmojiKey()
	{
		_keyEmoji["RecentUse"] = "IcEmojiKeyRecentUse";
		_keyEmoji["PeopleAndBody"] = "IcEmojiKeyPeopleAndBody";
		_keyEmoji["AnimalsAndNature"] = "IcEmojiKeyAnimalsAndNature";
		_keyEmoji["FoodAndDrink"] = "IcEmojiKeyFoodAndDrink";
		_keyEmoji["Activities"] = "IcEmojiKeyActivities";
		_keyEmoji["TravelAndPlaces"] = "IcEmojiKeyTravelAndPlaces";
		_keyEmoji["Objects"] = "IcEmojiKeyObjects";
		_keyEmoji["Symbols"] = "IcEmojiKeySymbols";
		_keyEmoji["Flags"] = "IcEmojiKeyFlags";
	}

	private string GetEmojiKey(string key)
	{
		if (string.IsNullOrEmpty(key) || !_keyEmoji.ContainsKey(key))
		{
			return string.Empty;
		}
		return _keyEmoji[key];
	}

	private void GetEmojiModelsFromJson()
	{
		List<EmojiListModel> emojiListsInJson = _emojiListsInJson;
		if (emojiListsInJson == null || emojiListsInJson.Count <= 0)
		{
			_emojiListsInJson = JsonConvert.DeserializeObject<List<EmojiListModel>>(IOUtils.GetFileContentInAssemblyFold("EmojiJsonWithSkin.json"));
		}
	}

	public async void GetItems(bool focus = true, int countPerRow = 9)
	{
		_countPerRow = countPerRow;
		_emojiKeyViewModel.Clear();
		List<EmojiSelectViewModel> list = new List<EmojiSelectViewModel>();
		List<string> list2 = (from e in LocalSettings.Settings.RecentUsedEmojis?.Split(';')
			where !string.IsNullOrEmpty(e)
			select e).ToList();
		if (list2 != null && list2.Count > 0)
		{
			bool flag = FoldedEmojiListKeys.Contains("RecentUse");
			list.Add(new EmojiSelectViewModel
			{
				Key = "RecentUse",
				Text = Utils.GetString("RecentUse"),
				IsSection = true,
				Folded = flag
			});
			_emojiKeyViewModel.Add(new EmojiKeyViewModel
			{
				Key = "RecentUse",
				EmojiPathName = GetEmojiKey("RecentUse"),
				Text = Utils.GetString("RecentUse")
			});
			if (!flag)
			{
				EmojiSelectViewModel item = new EmojiSelectViewModel
				{
					Children = list2.Select((string emoji) => new EmojiSelectViewModel
					{
						Text = emoji
					}).ToList()
				};
				list.Add(item);
			}
		}
		List<EmojiListModel> emojiListsInJson = _emojiListsInJson;
		if (emojiListsInJson != null && emojiListsInJson.Count > 0)
		{
			foreach (EmojiListModel item2 in _emojiListsInJson)
			{
				if (item2.Items == null || item2.Items.Count == 0)
				{
					continue;
				}
				bool flag2 = FoldedEmojiListKeys.Contains(item2.Key);
				EmojiSelectViewModel emojiSelectViewModel = new EmojiSelectViewModel
				{
					Text = Utils.GetString(item2.Key),
					Key = item2.Key,
					IsSection = true,
					Folded = flag2
				};
				list.Add(emojiSelectViewModel);
				_emojiKeyViewModel.Add(new EmojiKeyViewModel
				{
					Key = emojiSelectViewModel.Key,
					EmojiPathName = GetEmojiKey(emojiSelectViewModel.Key),
					Text = Utils.GetString(item2.Key)
				});
				EmojiSelectViewModel emojiSelectViewModel2 = null;
				if (flag2)
				{
					continue;
				}
				for (int num = 0; num < item2.Items.Count; num++)
				{
					if (num % countPerRow == 0)
					{
						emojiSelectViewModel2 = new EmojiSelectViewModel
						{
							Key = emojiSelectViewModel.Key
						};
						emojiSelectViewModel2.Children = new List<EmojiSelectViewModel>();
						list.Add(emojiSelectViewModel2);
					}
					EmojiItemModel emojiItemModel = item2.Items[num];
					EmojiSelectViewModel emojiSelectViewModel3 = new EmojiSelectViewModel
					{
						Text = emojiItemModel.Key
					};
					List<string> skin = emojiItemModel.Skin;
					if (skin != null && skin.Count > 0)
					{
						emojiSelectViewModel3.Children = emojiItemModel.Skin.Select((string s) => new EmojiSelectViewModel
						{
							Text = s
						}).ToList();
					}
					emojiSelectViewModel2?.Children.Add(emojiSelectViewModel3);
				}
			}
		}
		ItemsSourceHelper.SetItemsSource(EmojiKeyList, _emojiKeyViewModel);
		ItemsSourceHelper.SetItemsSource(EmojiList, list);
		_items = list;
		SearchTextBox.TextChanged -= TrySearchEmoji;
		SearchTextBox.Text = string.Empty;
		SearchTextBox.TextChanged += TrySearchEmoji;
		if (focus)
		{
			await Task.Delay(200);
			SearchTextBox.Focus();
			await Task.Delay(140);
			if (!SearchTextBox.EditBox.TextArea.IsKeyboardFocused)
			{
				SearchTextBox.Focus();
			}
		}
	}

	private void OnSwitchClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is StackPanel { DataContext: EmojiSelectViewModel dataContext })
		{
			if (dataContext.Folded)
			{
				FoldedEmojiListKeys.Remove(dataContext.Key);
			}
			else
			{
				FoldedEmojiListKeys.Add(dataContext.Key);
			}
			GetItems(focus: false);
		}
	}

	private void OnEmojiClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is Border { DataContext: EmojiSelectViewModel dataContext })
		{
			this.EmojiSelected?.Invoke(dataContext.Text, closePopup: true);
			SetRecentSelected(dataContext.Text);
		}
	}

	private void SetRecentSelected(string emoji)
	{
		List<string> list = (from e in LocalSettings.Settings.RecentUsedEmojis?.Split(';')
			where !string.IsNullOrEmpty(e)
			select e).ToList();
		if (list != null && list.Count > 0)
		{
			list.Remove(emoji);
			if (list != null && list.Count == 12)
			{
				list.Remove(list[11]);
			}
			if (list.Count > 0)
			{
				string text = string.Join(";", list);
				LocalSettings.Settings.RecentUsedEmojis = emoji + ";" + text;
			}
			else
			{
				LocalSettings.Settings.RecentUsedEmojis = emoji;
			}
		}
		else
		{
			LocalSettings.Settings.RecentUsedEmojis = emoji;
		}
	}

	private void OnEmojiMouseMove(object sender, MouseEventArgs e)
	{
		if (!(sender is Border border) || (_lastHoverBd != null && _lastHoverBd.Equals(border)) || !(border.DataContext is EmojiSelectViewModel emojiSelectViewModel) || EmojiSkinPopup.IsMouseOver)
		{
			return;
		}
		_lastHoverBd = border;
		List<EmojiSelectViewModel> children = emojiSelectViewModel.Children;
		if (children != null && children.Count > 0)
		{
			if (border.TranslatePoint(new Point(0.0, 0.0), EmojiList).Y < 30.0)
			{
				EmojiSkinPopup.VerticalOffset = 35.0;
			}
			else
			{
				EmojiSkinPopup.VerticalOffset = -30.0;
			}
			SkinItems.ItemsSource = emojiSelectViewModel.Children;
			EmojiSkinPopup.PlacementTarget = border;
			EmojiSkinPopup.IsOpen = false;
			EmojiSkinPopup.IsOpen = true;
		}
		else
		{
			EmojiSkinPopup.IsOpen = false;
		}
	}

	private void OnResetClick(object sender, MouseButtonEventArgs e)
	{
		this.EmojiSelected?.Invoke(string.Empty, closePopup: true);
		SetCanReset(enable: false);
	}

	public void SetActionPanelEnabled(bool enabled)
	{
		HeadPanel.Opacity = (enabled ? 1 : 0);
	}

	public void SetCanReset(bool enable)
	{
		ResetText.IsEnabled = enable;
		ResetText.Opacity = (enable ? 1.0 : 0.4);
	}

	private void OnRandomClick(object sender, MouseButtonEventArgs e)
	{
		List<EmojiListModel> emojiListsInJson = _emojiListsInJson;
		if (emojiListsInJson != null && emojiListsInJson.Count > 0)
		{
			int index = new Random().Next(0, _emojiListsInJson.Count);
			EmojiListModel emojiListModel = _emojiListsInJson[index];
			int index2 = new Random().Next(0, emojiListModel.Items.Count);
			EmojiItemModel emojiItemModel = emojiListModel.Items[index2];
			SetCanReset(enable: true);
			this.EmojiSelected?.Invoke(emojiItemModel.Key, closePopup: false);
		}
	}

	private ScrollViewer GetScrollViewer()
	{
		if (EmojiList.Template.FindName("ScrollViewer", EmojiList) is ScrollViewer result)
		{
			return result;
		}
		return null;
	}

	private void OnEmojiKeyItemMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		EmojiList.PreviewMouseWheel -= OnEmojiListMouseWheel;
		OnItemMouseUp();
		EmojiList.PreviewMouseWheel += OnEmojiListMouseWheel;
		void OnItemMouseUp()
		{
			if (sender is Grid { DataContext: EmojiKeyViewModel dataContext })
			{
				foreach (EmojiKeyViewModel item in _emojiKeyViewModel)
				{
					if (item.Key.Equals(dataContext.Key) && item.IsSelected)
					{
						return;
					}
					item.IsSelected = false;
				}
				dataContext.IsSelected = true;
				int num = 0;
				foreach (object item2 in (IEnumerable)EmojiList.Items)
				{
					if (item2 is EmojiSelectViewModel emojiSelectViewModel)
					{
						if (emojiSelectViewModel.IsSection && emojiSelectViewModel.Key != null && emojiSelectViewModel.Key.Equals(dataContext.Key))
						{
							ScrollViewer scrollViewer = GetScrollViewer();
							scrollViewer?.ScrollToVerticalOffset(Math.Min(num, scrollViewer.ScrollableHeight));
							EmojiList.ScrollIntoView(emojiSelectViewModel);
							break;
						}
						num = ((!emojiSelectViewModel.IsSection) ? (num + 38) : (num + 28));
					}
				}
			}
		}
	}

	private void OnEmojiListMouseWheel(object sender, MouseWheelEventArgs e)
	{
		ListBoxItem mousePointItem = Utils.GetMousePointItem<ListBoxItem>(new Point(200.0, 15.0), EmojiList);
		ListBoxItem mousePointItem2 = Utils.GetMousePointItem<ListBoxItem>(new Point(200.0, EmojiList.ActualHeight - 5.0), EmojiList);
		ListBoxItem listBoxItem = ((mousePointItem == mousePointItem2) ? mousePointItem : mousePointItem2);
		if (listBoxItem == null || !(listBoxItem.DataContext is EmojiSelectViewModel { Key: not null, Key: var key }))
		{
			return;
		}
		foreach (EmojiKeyViewModel item in EmojiKeyList.ItemsSource)
		{
			item.IsSelected = false;
			if (item.Key.Equals(key))
			{
				item.IsSelected = true;
			}
		}
	}

	private void OnEmojiListMouseMove(object sender, MouseEventArgs e)
	{
		if (e.GetPosition(EmojiList).Y < 0.0 && !EmojiSkinPopup.IsMouseOver)
		{
			EmojiSkinPopup.IsOpen = false;
		}
	}

	private void TrySearchEmoji(object sender, EventArgs e)
	{
		DelayActionHandlerCenter.TryDoAction("SearchEmoji", delegate
		{
			base.Dispatcher.Invoke(SearchEmoji);
		});
	}

	private void SearchEmoji()
	{
		string text = SearchTextBox.Text;
		if (string.IsNullOrWhiteSpace(text))
		{
			EmojiHeadBorder.Visibility = Visibility.Visible;
			HeadPanel.Visibility = Visibility.Visible;
			SearchEmptyGrid.Visibility = Visibility.Collapsed;
			ItemsSourceHelper.SetItemsSource(EmojiList, _items);
			return;
		}
		if (EmojiHeadBorder.Visibility == Visibility.Visible)
		{
			EmojiHeadBorder.Visibility = Visibility.Collapsed;
			HeadPanel.Visibility = Visibility.Collapsed;
		}
		List<string> matchedEmoji = EmojiSearchHelper.GetMatchedEmoji(text.ToLower());
		matchedEmoji.Remove("\ud83d\uddfe");
		matchedEmoji.Remove("\ud83c\udf8c");
		List<EmojiSelectViewModel> list = new List<EmojiSelectViewModel>();
		if (matchedEmoji.Count > 0)
		{
			EmojiSelectViewModel emojiSelectViewModel = null;
			for (int i = 0; i < matchedEmoji.Count; i++)
			{
				if (i % _countPerRow == 0)
				{
					emojiSelectViewModel = new EmojiSelectViewModel
					{
						Key = string.Empty,
						Children = new List<EmojiSelectViewModel>()
					};
					list.Add(emojiSelectViewModel);
				}
				string text2 = matchedEmoji[i];
				EmojiSelectViewModel item = new EmojiSelectViewModel
				{
					Text = text2
				};
				emojiSelectViewModel?.Children.Add(item);
			}
		}
		ItemsSourceHelper.SetItemsSource(EmojiList, list);
		SearchEmptyGrid.Visibility = ((list.Count != 0) ? Visibility.Collapsed : Visibility.Visible);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/emojiselectcontrol.xaml", UriKind.Relative);
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
		case 4:
			((Grid)target).MouseMove += OnEmojiListMouseMove;
			break;
		case 5:
			SearchTextBox = (EmojiEditor)target;
			break;
		case 6:
			EmojiHeadBorder = (Border)target;
			break;
		case 7:
			EmojiKeyList = (ItemsControl)target;
			break;
		case 8:
			HeadPanel = (StackPanel)target;
			break;
		case 9:
			((TextBlock)target).MouseLeftButtonUp += OnRandomClick;
			break;
		case 10:
			ResetText = (TextBlock)target;
			ResetText.MouseLeftButtonUp += OnResetClick;
			break;
		case 11:
			EmojiList = (ListView)target;
			break;
		case 12:
			SearchEmptyGrid = (Grid)target;
			break;
		case 13:
			EmojiSkinPopup = (Popup)target;
			break;
		case 14:
			SkinItems = (ItemsControl)target;
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
		case 1:
			((Border)target).MouseLeftButtonUp += OnEmojiClick;
			((Border)target).MouseMove += OnEmojiMouseMove;
			break;
		case 2:
			((StackPanel)target).MouseLeftButtonUp += OnSwitchClick;
			break;
		case 3:
			((Grid)target).MouseLeftButtonUp += OnEmojiKeyItemMouseLeftButtonUp;
			break;
		}
	}
}
