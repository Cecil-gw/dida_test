using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Emoji.Wpf;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.MarkDown;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Search;
using ticktick_WPF.Views.Widget;

namespace ticktick_WPF.Views.Filter;

public class FilterEditDialog : Window, IOkCancelWindow, IComponentConnector
{
	private readonly FilterViewModel _viewModel;

	private bool _toastValid;

	public bool Saved;

	internal AnimateGroupTitle GroupTitle;

	internal Grid InputGrid;

	internal EmojiEditor FilterNameText;

	internal Grid EmojiSelectGrid;

	internal Grid IconGrid;

	internal Path ProjectPath;

	internal Path SetEmojiIcon;

	internal EmjTextBlock EmojiText;

	internal EscPopup EmojiSelectPopup;

	internal EmojiSelectControl EmojiSelector;

	internal TextBlock AddFilterNameRepeatTextBlock;

	internal NormalFilterControl NormalFilterControl;

	internal AdvancedFilterControl AdvancedFilterControl;

	internal Button SaveButton;

	internal Border ToastBorder;

	internal TextBlock ToastText;

	private bool _contentLoaded;

	public event EventHandler<FilterModel> FilterSaved;

	public event EventHandler<FilterViewModel> OnSave;

	public FilterEditDialog(FilterModel filter = null, bool toastValid = false)
	{
		EmojiSearchHelper.Init();
		InitializeComponent();
		_viewModel = ((filter != null) ? new FilterViewModel(filter) : new FilterViewModel());
		InitDataContext();
		CheckEmojiStart(_viewModel);
		_toastValid = toastValid;
		base.Loaded += OnLoaded;
		FilterNameText.TextChanged += OnTextChanged;
		FilterNameText.EnterUp += OnTextKeyUp;
		SaveButton.IsEnabled = !string.IsNullOrEmpty(EmojiText.Text.Trim() + _viewModel.Name);
		FilterNameText.Text = _viewModel.Name;
		GroupTitle.SetSelectedIndex((_viewModel.Mode != FilterMode.Normal) ? 1 : 0);
	}

	public FilterEditDialog(SearchFilterModel searchFilterViewModel)
	{
		EmojiSearchHelper.Init();
		InitializeComponent();
		GroupTitle.Visibility = Visibility.Collapsed;
		FilterNameText.Margin = new Thickness(20.0, 0.0, 20.0, 12.0);
		_viewModel = new FilterViewModel(searchFilterViewModel);
		InitDataContext();
		base.Loaded += OnLoaded;
		FilterNameText.TextChanged += OnTextChanged;
		FilterNameText.EnterUp += OnTextKeyUp;
		SaveButton.IsEnabled = !string.IsNullOrEmpty(EmojiText.Text.Trim() + _viewModel.Name);
		FilterNameText.Text = _viewModel.Name;
		GroupTitle.SetSelectedIndex((_viewModel.Mode != FilterMode.Normal) ? 1 : 0);
	}

	private void OnTextChanged(object sender, EventArgs e)
	{
		_viewModel.Name = EmojiText.Text.Trim() + FilterNameText.Text.Trim();
		SaveButton.IsEnabled = !string.IsNullOrEmpty(_viewModel.Name);
	}

	private void OnTextKeyUp(object sender, EventArgs e)
	{
		SaveBtnClick(null, null);
	}

	private async void OnLoaded(object sender, RoutedEventArgs e)
	{
		Task.Run(delegate
		{
			AvatarHelper.GetAllProjectAvatars();
		});
		await Task.Delay(100);
		Activate();
		FilterNameText.FocusEnd();
		if (_toastValid)
		{
			IsFilterNameValid();
		}
		base.Loaded -= OnLoaded;
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	private void InitDataContext()
	{
		NormalFilterControl.ViewModel = _viewModel.NormalViewModel;
		NormalFilterControl.Init();
		AdvancedFilterControl.ViewModel = _viewModel.AdvancedViewModel;
		AdvancedFilterControl.ExistNote = LocalSettings.Settings.IsNoteEnabled;
		base.DataContext = _viewModel;
		base.Title = ((_viewModel.EditType == Constants.ModelEditType.Edit) ? Utils.GetString("EditFilter") : Utils.GetString("AddFilter"));
	}

	private void CancelBtnClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private async void SaveBtnClick(object sender, RoutedEventArgs e)
	{
		if (await IsFilterNameValid() && CheckAdvancedRule())
		{
			SaveData();
		}
	}

	private async Task<bool> IsFilterNameValid()
	{
		string text = EmojiText.Text.Trim() + FilterNameText.Text.Trim();
		string message = string.Empty;
		if (string.IsNullOrEmpty(text))
		{
			message = Utils.GetString("FilterNameIsEmpty");
		}
		else if (text.StartsWith("#"))
		{
			message = Utils.GetString("ListNameBeginError");
		}
		else if (!NameUtils.IsValidNameNoCheckSharp(text, checkSpace: false))
		{
			message = Utils.GetString("ListNameCantContain");
		}
		else if (await CheckFilterExist(text))
		{
			message = Utils.GetString("DuplicateFilterName");
		}
		bool num = string.IsNullOrEmpty(message);
		if (!num)
		{
			AddFilterNameRepeatTextBlock.Text = message;
			AddFilterNameRepeatTextBlock.Visibility = Visibility.Visible;
		}
		else
		{
			AddFilterNameRepeatTextBlock.Visibility = Visibility.Collapsed;
		}
		return num;
	}

	private async Task<bool> CheckFilterExist(string name)
	{
		FilterModel filterModel = (await FilterDao.GetAllFilters()).FirstOrDefault((FilterModel f) => f.name == name);
		if (_viewModel.EditType == Constants.ModelEditType.New)
		{
			if (filterModel != null)
			{
				return true;
			}
		}
		else if (filterModel != null && filterModel.id != _viewModel.FilterModel.id)
		{
			return true;
		}
		return false;
	}

	private bool CheckAdvancedRule()
	{
		if (_viewModel.Mode == FilterMode.Normal)
		{
			return true;
		}
		if (AdvancedFilterControl.ViewModel.ConditionValid())
		{
			if (!AdvancedFilterControl.ViewModel.CheckKeywordValid())
			{
				Toast(Utils.GetString("InvalidFilterRule"));
				return false;
			}
			return true;
		}
		Toast(Utils.GetString("InvalidFilterRule"));
		return false;
	}

	public void Toast(string toastText)
	{
		ToastBorder.Visibility = Visibility.Visible;
		ToastText.Text = toastText;
		Storyboard storyboard = (Storyboard)FindResource("ShowToast");
		ToastBorder.BeginStoryboard(storyboard);
	}

	private void OnToasted(object sender, EventArgs e)
	{
		ToastBorder.Visibility = Visibility.Collapsed;
	}

	private async void SaveData()
	{
		_viewModel.Name = EmojiText.Text.Trim() + FilterNameText.Text.Trim();
		FilterModel filter = await _viewModel.Save();
		UtilLog.Info($"FilterDialog.FilterSave : {_viewModel.FilterModel.id},isAdd {_viewModel.EditType == Constants.ModelEditType.New}");
		Saved = true;
		ProjectWidgetsHelper.OnProjectChanged(new FilterProjectIdentity(filter));
		this.FilterSaved?.Invoke(null, _viewModel.FilterModel);
		Close();
	}

	private void ShowExamples(object sender, MouseButtonEventArgs e)
	{
		UserActCollectUtils.AddClickEvent("edit_filter", "action", "example");
		Utils.TryProcessStartUrl(Utils.IsDida() ? "https://help.dida365.com/articles/6950655961044353024#%E9%AB%98%E7%BA%A7%E7%AD%9B%E9%80%89" : "https://help.ticktick.com/articles/7055782240994721792");
	}

	private void ShowPreview(object sender, MouseButtonEventArgs e)
	{
		UserActCollectUtils.AddClickEvent("edit_filter", "action", "preview");
		_viewModel.Name = EmojiText.Text.Trim() + FilterNameText.Text.Trim();
		if (CheckAdvancedRule())
		{
			FilterPreviewWindow obj = new FilterPreviewWindow(_viewModel.GetFilterModel())
			{
				Owner = base.Owner
			};
			Close();
			obj.ShowDialog();
		}
	}

	public void OnCancel()
	{
		Close();
	}

	public async void Ok()
	{
		_viewModel.Name = EmojiText.Text.Trim() + FilterNameText.Text.Trim();
		if (await IsFilterNameValid() && CheckAdvancedRule())
		{
			SaveData();
		}
	}

	private void OnSelectedTitleChanged(object sender, GroupTitleViewModel e)
	{
		if (_viewModel == null)
		{
			return;
		}
		if ("advanced".Equals(e.Title))
		{
			_viewModel.Mode = FilterMode.Advanced;
			if (_viewModel.AdvancedViewModel.CardList.Count == 0)
			{
				AdvancedFilterControl.CardList = FilterConditionProvider.BuildInitData();
			}
		}
		else
		{
			_viewModel.Mode = FilterMode.Normal;
		}
	}

	private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (!FilterNameText.IsMouseOver)
		{
			FilterNameText.TabIndex = -1;
			Keyboard.ClearFocus();
		}
	}

	private void ShowEmojiSelector(object sender, MouseButtonEventArgs e)
	{
		EmojiSelector.GetItems();
		EmojiSelector.SetCanReset(!string.IsNullOrEmpty(EmojiText.Text));
		EmojiSelectPopup.IsOpen = true;
	}

	private void CheckEmojiStart(FilterViewModel model)
	{
		string emojiIcon = EmojiHelper.GetEmojiIcon(model.Name);
		if (!string.IsNullOrEmpty(emojiIcon) && model.Name.StartsWith(emojiIcon))
		{
			model.Name = model.Name.Remove(0, emojiIcon.Length);
			SetEmoji(emojiIcon);
		}
	}

	private void SetEmoji(string emoji)
	{
		EmojiText.Text = emoji ?? string.Empty;
		IconGrid.Visibility = ((!string.IsNullOrEmpty(emoji)) ? Visibility.Collapsed : Visibility.Visible);
		FilterNameText.MaxLength = 64 - EmojiText.Text.Length;
	}

	private void OnEmojiSelected(string emoji, bool closePopup)
	{
		SetEmoji(emoji);
		if (closePopup)
		{
			EmojiSelectPopup.IsOpen = false;
		}
		_viewModel.Name = EmojiText.Text.Trim() + FilterNameText.Text.Trim();
		SaveButton.IsEnabled = !string.IsNullOrEmpty(_viewModel.Name);
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		base.Owner?.Activate();
		base.OnClosing(e);
	}

	private void OnFilterNameTextChanged(object sender, EventArgs e)
	{
		IsFilterNameValid();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/filter/filtereditdialog.xaml", UriKind.Relative);
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
			((FilterEditDialog)target).PreviewMouseLeftButtonDown += OnPreviewMouseDown;
			break;
		case 2:
			((Storyboard)target).Completed += OnToasted;
			break;
		case 3:
			GroupTitle = (AnimateGroupTitle)target;
			break;
		case 4:
			InputGrid = (Grid)target;
			break;
		case 5:
			FilterNameText = (EmojiEditor)target;
			break;
		case 6:
			EmojiSelectGrid = (Grid)target;
			EmojiSelectGrid.MouseLeftButtonUp += ShowEmojiSelector;
			break;
		case 7:
			IconGrid = (Grid)target;
			break;
		case 8:
			ProjectPath = (Path)target;
			break;
		case 9:
			SetEmojiIcon = (Path)target;
			break;
		case 10:
			EmojiText = (EmjTextBlock)target;
			break;
		case 11:
			EmojiSelectPopup = (EscPopup)target;
			break;
		case 12:
			EmojiSelector = (EmojiSelectControl)target;
			break;
		case 13:
			AddFilterNameRepeatTextBlock = (TextBlock)target;
			break;
		case 14:
			NormalFilterControl = (NormalFilterControl)target;
			break;
		case 15:
			AdvancedFilterControl = (AdvancedFilterControl)target;
			break;
		case 16:
			((TextBlock)target).MouseLeftButtonUp += ShowExamples;
			break;
		case 17:
			SaveButton = (Button)target;
			SaveButton.Click += SaveBtnClick;
			break;
		case 18:
			((Button)target).Click += CancelBtnClick;
			break;
		case 19:
			((TextBlock)target).MouseLeftButtonUp += ShowPreview;
			break;
		case 20:
			ToastBorder = (Border)target;
			break;
		case 21:
			ToastText = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
