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
using System.Windows.Shapes;
using Emoji.Wpf;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Sync.ClosedLoader;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.MarkDown;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Misc.ColorSelector;

namespace ticktick_WPF.Views.Tag;

public class AddOrEditTagWindow : Window, IOkCancelWindow, IComponentConnector
{
	private TagModel _originalTag;

	private readonly string _originalTagId;

	private TagModel _addTagModel;

	private bool _isSaved;

	internal Grid InputGrid;

	internal EmojiEditor TagTextBox;

	internal Grid EmojiSelectGrid;

	internal Grid IconGrid;

	internal Path TagPath;

	internal Path SetEmojiIcon;

	internal EmjTextBlock EmojiText;

	internal EscPopup EmojiSelectPopup;

	internal EmojiSelectControl EmojiSelector;

	internal TextBlock TagValidHintText;

	internal ColorSelector ColorItems;

	internal Grid TagParentPanel;

	internal CustomComboBox TagParentComboBox;

	internal Button SaveButton;

	private bool _contentLoaded;

	private TagModel _addSubParentTag { get; set; }

	public event EventHandler<TagModel> TagSaved;

	public event EventHandler Cancel;

	public AddOrEditTagWindow()
	{
		EmojiSearchHelper.Init();
		InitializeComponent();
		ColorItems.SetSelectedColor(ThemeUtil.GetRandomColor());
		SaveButton.IsEnabled = false;
		TagParentPanel.Visibility = Visibility.Visible;
		TagTextBox.EnterUp += OnEnterKeyDown;
	}

	public AddOrEditTagWindow(TagModel model, bool canEditTitle = true, bool isCreat = false)
	{
		EmojiSearchHelper.Init();
		InitializeComponent();
		TagTextBox.EnterUp += OnEnterKeyDown;
		TagTextBox.ReadOnly = !canEditTitle;
		EmojiSelectGrid.IsEnabled = canEditTitle;
		if (isCreat)
		{
			_addTagModel = model;
			SaveButton.IsEnabled = false;
		}
		else
		{
			_originalTagId = model.id;
			if (!model.IsParent())
			{
				TagParentPanel.Visibility = Visibility.Visible;
			}
		}
		ColorItems.SetSelectedColor(model.color);
	}

	public void SetAddSub(TagModel model)
	{
		_addSubParentTag = model;
	}

	private async void OnSaveClick(object sender, RoutedEventArgs e)
	{
		SaveButton.Content = Utils.GetString("Saving");
		SaveButton.IsEnabled = false;
		await SaveTag();
	}

	private async Task SaveTag()
	{
		if (TagValidHintText.Visibility != Visibility.Collapsed)
		{
			return;
		}
		List<TagModel> deferredTags = new List<TagModel>();
		string originalName = _originalTag?.name;
		string parentName = GetSelectedParentName();
		TagDao.UpdateParent(parentName);
		string label = GetFullTagLabel();
		string tagName = label?.ToLower();
		string originColor = "null";
		string color = ColorItems.GetSelectedColor();
		TagModel savedTag;
		if (_addTagModel != null)
		{
			savedTag = _addTagModel;
			savedTag.name = tagName;
			savedTag.label = label;
			savedTag.color = color;
			await TagDao.SaveNewTag(savedTag);
		}
		else if (_originalTag == null)
		{
			savedTag = await TagService.TryCreateTag(label, color, parentName, randomColor: false);
		}
		else
		{
			originColor = _originalTag.color;
			await TagService.SaveTag(_originalTag.id, _originalTag.name, label, color, parentName, notify: false);
			if (_originalTag.GetDisplayName() != label)
			{
				await Communicator.RenameTag(_originalTag.name, label);
				CompletionLoadStatusDao.OnTagRename(_originalTag.name, tagName);
				UtilLog.Info("EditTagWindow.SaveTag Rename : " + _originalTag.name + " to " + label);
				await SyncSortOrderDao.OnTagRenamed(_originalTag.name, label?.ToLower());
				if (_originalTag.name != tagName)
				{
					List<TagModel> list = (from t in CacheManager.GetTags()
						where t.parent == _originalTag.name
						select t).ToList();
					foreach (TagModel tag in list)
					{
						tag.parent = tagName;
						if (tag.status != 0)
						{
							tag.status = 1;
						}
						await TagDao.UpdateTag(tag, null, notify: false);
						deferredTags.Add(tag);
					}
				}
			}
			if (!string.IsNullOrEmpty(_originalTag.parent) && _originalTag.parent != parentName)
			{
				TagDao.CheckParent(_originalTag.parent);
			}
			SyncManager.Sync();
			savedTag = _originalTag;
			_originalTag.name = tagName;
			_originalTag.label = label;
			_originalTag.color = color;
			_originalTag.parent = parentName;
			TagModel tagModel = CacheManager.GetTagByName(tagName) ?? savedTag;
			deferredTags.Add(tagModel);
			foreach (TagModel item in deferredTags)
			{
				CacheManager.UpdateTag(item);
			}
			DataChangedNotifier.NotifyTagChanged(tagModel, originalName);
		}
		this.TagSaved?.Invoke(this, savedTag);
		_isSaved = true;
		if (originColor != color)
		{
			ColorSelector.TryAddClickEvent(color);
		}
		Close();
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		base.Owner?.Activate();
		base.OnClosing(e);
	}

	protected override void OnClosed(EventArgs e)
	{
		if (!_isSaved)
		{
			this.Cancel?.Invoke(null, null);
		}
		base.OnClosed(e);
	}

	private void OnWindowLoaded(object sender, RoutedEventArgs e)
	{
		if (_addSubParentTag == null)
		{
			Load();
		}
		else
		{
			LoadAddSub();
		}
	}

	private async Task Load()
	{
		base.Title = Utils.GetString(string.IsNullOrEmpty(_originalTagId) ? "AddTag" : "EditTag");
		if (!string.IsNullOrEmpty(_originalTagId))
		{
			_originalTag = await TagDao.GetTagById(_originalTagId);
			if (_originalTag != null)
			{
				SetTagTextFromModel(_originalTag);
			}
		}
		if (_addTagModel != null)
		{
			SetTagTextFromModel(_addTagModel);
		}
		if (TagParentPanel.Visibility == Visibility.Visible)
		{
			ObservableCollection<ComboBoxViewModel> observableCollection = new ObservableCollection<ComboBoxViewModel>(from t in CacheManager.GetTags()
				where !t.IsChild() && t.name != _originalTag?.name
				orderby t.GetTagType(), t.sortOrder
				select new ComboBoxViewModel(t.name, t.GetDisplayName(), 32.0)
				{
					Selected = t.name.Equals(_originalTag?.parent, StringComparison.OrdinalIgnoreCase)
				});
			observableCollection.Insert(0, new ComboBoxViewModel(string.Empty, Utils.GetString("none"), 32.0)
			{
				Selected = string.IsNullOrEmpty(_originalTag?.parent)
			});
			TagParentComboBox.Init(observableCollection, null);
		}
		TagTextBox.FocusEnd();
	}

	private async Task LoadAddSub()
	{
		base.Title = Utils.GetString("AddTag");
		List<ComboBoxViewModel> list = (from t in CacheManager.GetTags()
			where t == _addSubParentTag || (!t.IsChild() && t.name != _originalTag?.name)
			orderby t.GetTagType(), t.sortOrder
			select new ComboBoxViewModel(t.name, t.GetDisplayName(), 32.0)
			{
				Selected = t.name.Equals(_originalTag?.parent, StringComparison.OrdinalIgnoreCase)
			}).ToList();
		list.Insert(0, new ComboBoxViewModel(string.Empty, Utils.GetString("none"), 32.0)
		{
			Selected = false
		});
		ObservableCollection<ComboBoxViewModel> observableCollection = new ObservableCollection<ComboBoxViewModel>(list);
		ComboBoxViewModel comboBoxViewModel = observableCollection.FirstOrDefault((ComboBoxViewModel x) => string.Equals(x.Value as string, _addSubParentTag.name, StringComparison.OrdinalIgnoreCase));
		if (comboBoxViewModel != null)
		{
			comboBoxViewModel.Selected = true;
		}
		TagParentComboBox.Init(observableCollection, comboBoxViewModel);
		TagTextBox.FocusEnd();
	}

	private void OnTagTextChanged(object sender, EventArgs e)
	{
		CheckTagValid();
		SaveButton.IsEnabled = !TagValidHintText.IsVisible;
	}

	private void CheckTagValid()
	{
		string fullTagLabel = GetFullTagLabel();
		SaveButton.IsEnabled = !string.IsNullOrEmpty(fullTagLabel);
		if (!NameUtils.IsValidName(fullTagLabel))
		{
			TagValidHintText.Text = Utils.GetString("TagNotValid");
			TagValidHintText.Visibility = Visibility.Visible;
		}
		else if (CheckIfTagExist(fullTagLabel))
		{
			TagValidHintText.Text = Utils.GetString("TagExisted");
			TagValidHintText.Visibility = Visibility.Visible;
		}
		else if (string.IsNullOrEmpty(fullTagLabel))
		{
			TagValidHintText.Text = Utils.GetString("TagCannotBeEmpty");
			TagValidHintText.Visibility = Visibility.Visible;
		}
		else
		{
			TagValidHintText.Visibility = Visibility.Collapsed;
		}
	}

	private bool CheckIfTagExist(string text)
	{
		return TagDataHelper.CheckIfTagExisted(_originalTagId, text);
	}

	private void OnEnterKeyDown(object sender, EventArgs e)
	{
		if (SaveButton.IsEnabled)
		{
			OnSaveClick(null, null);
		}
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	public void OnCancel()
	{
		if (!TagParentComboBox.IsOpen && !EmojiSelectPopup.IsOpen)
		{
			Close();
		}
	}

	public async void Ok()
	{
		if (!TagParentComboBox.IsOpen && !EmojiSelectPopup.IsOpen && SaveButton.IsEnabled)
		{
			SaveButton.Content = Utils.GetString("Saving");
			SaveButton.IsEnabled = false;
			await SaveTag();
		}
	}

	private async void OnActivated(object sender, EventArgs e)
	{
		await Task.Delay(10);
		TagTextBox.FocusEnd();
	}

	private void ShowEmojiSelector(object sender, MouseButtonEventArgs e)
	{
		EmojiSelector.GetItems();
		EmojiSelector.SetCanReset(!string.IsNullOrEmpty(EmojiText.Text));
		EmojiSelectPopup.IsOpen = true;
	}

	private void SetTagTextFromModel(TagModel tag)
	{
		string displayName = tag.GetDisplayName();
		string displayEmoji = tag.GetDisplayEmoji();
		SetTagEmoji(displayEmoji);
		TagTextBox.Text = ((!string.IsNullOrEmpty(displayEmoji) && displayName.StartsWith(displayEmoji)) ? displayName.Remove(0, displayEmoji.Length) : displayName);
	}

	private void SetTagEmoji(string emoji)
	{
		EmojiText.Text = emoji ?? string.Empty;
		IconGrid.Visibility = ((!string.IsNullOrEmpty(emoji)) ? Visibility.Collapsed : Visibility.Visible);
		TagTextBox.MaxLength = 64 - EmojiText.Text.Length;
	}

	private void OnEmojiSelected(string emoji, bool closePopup)
	{
		SetTagEmoji(emoji);
		if (closePopup)
		{
			EmojiSelectPopup.IsOpen = false;
		}
		CheckTagValid();
		SaveButton.IsEnabled = !TagValidHintText.IsVisible;
	}

	private string GetFullTagLabel()
	{
		return (EmojiText.Text.Trim() + TagTextBox.Text).Trim();
	}

	private string GetSelectedParentName()
	{
		if (TagParentPanel.Visibility == Visibility.Collapsed)
		{
			return string.Empty;
		}
		object obj;
		if (TagParentComboBox.SelectedIndex != 0)
		{
			obj = (TagParentComboBox.SelectedItem?.Value as string)?.ToLower();
			if (obj == null)
			{
				return string.Empty;
			}
		}
		else
		{
			obj = string.Empty;
		}
		return (string)obj;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/tag/addoredittagwindow.xaml", UriKind.Relative);
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
			((AddOrEditTagWindow)target).Loaded += OnWindowLoaded;
			((AddOrEditTagWindow)target).Activated += OnActivated;
			break;
		case 2:
			InputGrid = (Grid)target;
			break;
		case 3:
			TagTextBox = (EmojiEditor)target;
			break;
		case 4:
			EmojiSelectGrid = (Grid)target;
			EmojiSelectGrid.MouseLeftButtonUp += ShowEmojiSelector;
			break;
		case 5:
			IconGrid = (Grid)target;
			break;
		case 6:
			TagPath = (Path)target;
			break;
		case 7:
			SetEmojiIcon = (Path)target;
			break;
		case 8:
			EmojiText = (EmjTextBlock)target;
			break;
		case 9:
			EmojiSelectPopup = (EscPopup)target;
			break;
		case 10:
			EmojiSelector = (EmojiSelectControl)target;
			break;
		case 11:
			TagValidHintText = (TextBlock)target;
			break;
		case 12:
			ColorItems = (ColorSelector)target;
			break;
		case 13:
			TagParentPanel = (Grid)target;
			break;
		case 14:
			TagParentComboBox = (CustomComboBox)target;
			break;
		case 15:
			((Button)target).Click += OnCancelClick;
			break;
		case 16:
			SaveButton = (Button)target;
			SaveButton.Click += OnSaveClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
