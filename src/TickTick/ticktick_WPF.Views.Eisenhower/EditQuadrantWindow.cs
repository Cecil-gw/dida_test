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
using System.Windows.Shapes;
using Emoji.Wpf;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Framework.EmbeddedBrowser;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Filter;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Filter;
using ticktick_WPF.Views.MarkDown;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Eisenhower;

public class EditQuadrantWindow : Window, IOkCancelWindow, IComponentConnector
{
	private readonly int _level;

	private NormalFilterViewModel _normalViewModel;

	private NormalFilterViewModel _defaultViewModel;

	private readonly string _defaultName;

	private string _previousName;

	private bool _draftMode;

	private QuadrantModel _draftQuadrant;

	private Action<QuadrantModel> _saveDraft;

	private string _profileSid;

	internal TextBlock TitleText;

	internal Image ProImage;

	internal Grid InputGrid;

	internal EmojiEditor QuadrantNameText;

	internal Grid EmojiSelectGrid;

	internal Grid IconGrid;

	internal Path QuadrantIcon;

	internal Path SetEmojiIcon;

	internal EmjTextBlock EmojiText;

	internal EscPopup EmojiSelectPopup;

	internal EmojiSelectControl EmojiSelector;

	internal TextBlock ErrorText;

	internal NormalFilterControl FilterControl;

	internal TextBlock ResetText;

	internal Button SaveButton;

	internal Button HelpBtn;

	private bool _contentLoaded;

	public EditQuadrantWindow(int level)
	{
		InitializeComponent();
		TitleText.Text = Utils.GetString("EditMatrix");
		_defaultName = Utils.GetString("MatrixTitleQuadrant" + level);
		QuadrantIcon.Data = Utils.GetIcon("MatrixIconQuadrant" + level);
		QuadrantIcon.Fill = ThemeUtil.GetColor("MatrixColorQuadrant" + level);
		QuadrantNameText.TextChanged += OnTextChanged;
		QuadrantNameText.EnterUp += OnTextKeyUp;
		if (!UserDao.IsUserValid())
		{
			ProImage.Visibility = Visibility.Visible;
		}
		_level = level;
		base.Loaded += OnLoaded;
		if (!EmbeddedBrowserFactory.CanOpenEmbeddedBrowser(Environment.OSVersion.Version, InternetExplorerBrowserEmulation.IsVersion11()))
		{
			HelpBtn.Visibility = Visibility.Collapsed;
		}
	}

	public EditQuadrantWindow(int level, QuadrantModel quadrant, Action<QuadrantModel> saveDraft)
		: this(level)
	{
		_draftMode = true;
		_draftQuadrant = MatrixProfileDao.CloneQuadrant(quadrant);
		_saveDraft = saveDraft;
		TitleText.Text = Utils.GetString("EditMatrix");
	}

	public EditQuadrantWindow(int level, string profileSid, QuadrantModel quadrant)
		: this(level)
	{
		_profileSid = profileSid;
		_draftQuadrant = MatrixProfileDao.CloneQuadrant(quadrant);
		TitleText.Text = Utils.GetString("EditMatrix");
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		QuadrantModel quadrantModel = ((_draftMode || !string.IsNullOrEmpty(_profileSid)) ? _draftQuadrant : MatrixManager.GetQuadrantByLevel(_level));
		QuadrantModel quadrantModel2 = QuadrantModel.GetDefault(_level);
		quadrantModel = quadrantModel ?? quadrantModel2;
		if (Parser.GetFilterRuleType(quadrantModel.rule) != 0)
		{
			quadrantModel = quadrantModel2;
		}
		_normalViewModel = Parser.ToNormalModel(quadrantModel.rule);
		_defaultViewModel = Parser.ToNormalModel(quadrantModel2.rule);
		FilterControl.ViewModel = _normalViewModel;
		FilterControl.CheckValid = false;
		FilterControl.Init(inMatrix: true);
		base.PreviewMouseLeftButtonDown -= OnWindowPreviewMouseLeftButtonDown;
		base.PreviewMouseLeftButtonDown += OnWindowPreviewMouseLeftButtonDown;
		_previousName = (string.IsNullOrEmpty(quadrantModel.name) ? _defaultName : quadrantModel.name);
		SetQuadrantName(_previousName);
		SetDefaultEnable();
	}

	protected override async void OnActivated(EventArgs e)
	{
		base.OnActivated(e);
		if (UserDao.IsUserValid())
		{
			await Task.Delay(50);
			QuadrantNameText.FocusEnd();
		}
	}

	private void SetQuadrantName(string name)
	{
		string emojiIcon = EmojiHelper.GetEmojiIcon(name);
		if (!string.IsNullOrEmpty(emojiIcon) && name.StartsWith(emojiIcon))
		{
			QuadrantNameText.Text = name.Remove(0, emojiIcon.Length);
			SetEmoji(emojiIcon);
		}
		else
		{
			QuadrantNameText.Text = name;
		}
	}

	private void SetDefaultEnable()
	{
		ResetText.IsEnabled = !CheckIsDefault();
	}

	private bool CheckIsDefault()
	{
		if (EmojiText.Text + QuadrantNameText.Text.Trim() != _defaultName)
		{
			return false;
		}
		if (_normalViewModel.TaskTypes.Count == 1)
		{
			return false;
		}
		if (_normalViewModel.Projects.Any() || _normalViewModel.Groups.Any() || _normalViewModel.Tags.Any())
		{
			return false;
		}
		if (_normalViewModel.Assignees.Any())
		{
			return false;
		}
		if (_normalViewModel.DueDates.Count != _defaultViewModel.DueDates.Count || !_normalViewModel.DueDates.All(_defaultViewModel.DueDates.Contains))
		{
			return false;
		}
		if (_normalViewModel.Priorities.Count == _defaultViewModel.Priorities.Count)
		{
			return _normalViewModel.Priorities.All(_defaultViewModel.Priorities.Contains);
		}
		return false;
	}

	private void OnTextKeyUp(object sender, EventArgs e)
	{
		OnSave();
	}

	private void OnTextChanged(object sender, EventArgs e)
	{
		CheckNameValid();
		SetDefaultEnable();
	}

	private void CheckNameValid()
	{
		string text = EmojiText.Text + QuadrantNameText.Text.Trim();
		string text2 = string.Empty;
		if (string.IsNullOrEmpty(text))
		{
			text2 = Utils.GetString("MatrixNameEmpty");
		}
		else if (!NameUtils.IsValidNameNoCheckSharp(text, checkSpace: false))
		{
			text2 = Utils.GetString("ListNameCantContain");
		}
		ErrorText.Text = text2;
		ErrorText.Visibility = (string.IsNullOrEmpty(text2) ? Visibility.Collapsed : Visibility.Visible);
		SaveButton.IsEnabled = string.IsNullOrEmpty(text2);
	}

	private void SaveBtnClick(object sender, RoutedEventArgs e)
	{
		OnSave();
	}

	private async void OnSave()
	{
		if (!_draftMode && !ProChecker.CheckPro(ProType.Matrix, this))
		{
			return;
		}
		string text = EmojiText.Text + QuadrantNameText.Text.Trim();
		text = ((text == _previousName) ? null : ((text == _defaultName) ? string.Empty : text));
		string rule = _normalViewModel.ToRule(isSave: false);
		if (_draftMode)
		{
			QuadrantModel quadrantModel = MatrixProfileDao.CloneQuadrant(_draftQuadrant ?? QuadrantModel.GetDefault(_level));
			quadrantModel.rule = rule;
			if (text != null)
			{
				quadrantModel.name = ((text == string.Empty && string.IsNullOrWhiteSpace(_draftQuadrant?.name)) ? null : ((text == string.Empty) ? _defaultName : text));
			}
			_saveDraft?.Invoke(quadrantModel);
		}
		else
		{
			try
			{
				if (!string.IsNullOrEmpty(_profileSid) && !string.Equals(LocalSettings.Settings.MatrixActiveViewId, _profileSid, StringComparison.Ordinal))
				{
					await MatrixProfileDao.UpdateProfileQuadrantAsync(_profileSid, _level, rule, null, text);
				}
				else
				{
					LocalSettings.Settings.SaveMatrixQuadrant(_level, rule, null, text);
					DataChangedNotifier.NotifyMatrixQuadrantChanged(_level);
				}
			}
			catch (Exception ex)
			{
				UtilLog.Warn("EditQuadrantWindow.OnSave " + ex.Message);
				(Utils.GetToastWindow() ?? App.Window)?.TryToastString(null, Utils.GetString("OperateFailed"));
				return;
			}
		}
		Close();
	}

	private void CancelBtnClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void ResetClick(object sender, MouseButtonEventArgs e)
	{
		_normalViewModel = Parser.ToNormalModel(MatrixManager.GetDefaultQuadrantRule(MatrixType.Simple, _level));
		ResetText.IsEnabled = false;
		FilterControl.ViewModel = _normalViewModel;
		FilterControl.CheckValid = false;
		FilterControl.Init(inMatrix: true);
		EmojiText.Text = string.Empty;
		QuadrantNameText.Text = _defaultName;
		IconGrid.Visibility = Visibility.Visible;
		QuadrantNameText.FocusEnd();
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		base.PreviewMouseLeftButtonDown -= OnWindowPreviewMouseLeftButtonDown;
		base.OnClosing(e);
		base.Owner?.Activate();
	}

	private void OnNotifyInvalid(object sender, EventArgs e)
	{
	}

	public void OnCancel()
	{
		if (!FilterControl.PopupOpened && !EmojiSelectPopup.IsOpen)
		{
			Close();
		}
	}

	public void Ok()
	{
		if (!FilterControl.PopupOpened && !EmojiSelectPopup.IsOpen)
		{
			OnSave();
		}
	}

	private void OnRuleChanged(object sender, EventArgs e)
	{
		SetDefaultEnable();
	}

	private void ShowEmojiSelector(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		if (EmojiSelectPopup.IsOpen)
		{
			EmojiSelectPopup.IsOpen = false;
			return;
		}
		EmojiSelector.GetItems();
		EmojiSelector.SetCanReset(!string.IsNullOrEmpty(EmojiText.Text));
		EmojiSelectPopup.IsOpen = true;
	}

	private void OnWindowPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (EmojiSelectPopup.IsOpen && !EmojiSelectGrid.IsMouseOver && !(EmojiSelectPopup.Child is FrameworkElement { IsMouseOver: not false }))
		{
			EmojiSelectPopup.IsOpen = false;
		}
	}

	private void OnEmojiSelected(string emoji, bool closePopup)
	{
		SetEmoji(emoji);
		if (closePopup)
		{
			EmojiSelectPopup.IsOpen = false;
		}
		CheckNameValid();
		SetDefaultEnable();
	}

	private void SetEmoji(string emoji)
	{
		EmojiText.Text = emoji ?? string.Empty;
		IconGrid.Visibility = ((!string.IsNullOrEmpty(emoji)) ? Visibility.Collapsed : Visibility.Visible);
		QuadrantNameText.MaxLength = 64 - EmojiText.Text.Length;
	}

	private void OnInputMouseDown(object sender, MouseButtonEventArgs e)
	{
	}

	private void ShowExamples(object sender, MouseButtonEventArgs e)
	{
		ShowExampleWindow();
	}

	private async void ShowExampleWindow()
	{
		NavigateWebBrowserWindow navigateWebBrowserWindow = new NavigateWebBrowserWindow("/webview/matrixRule", addLang: true, SetDefaultMatrixRule);
		navigateWebBrowserWindow.Owner = this;
		navigateWebBrowserWindow.Title = Utils.GetString("HowToCustomizeRules");
		navigateWebBrowserWindow.Show();
		navigateWebBrowserWindow.Topmost = true;
	}

	private void SetDefaultMatrixRule(string type)
	{
		if (_draftMode || !string.IsNullOrEmpty(_profileSid))
		{
			_normalViewModel = Parser.ToNormalModel(MatrixManager.GetDefaultQuadrantRule((!(type == "simple")) ? MatrixType.Complex : MatrixType.Simple, _level));
			FilterControl.ViewModel = _normalViewModel;
			FilterControl.Init(inMatrix: true);
		}
		else
		{
			MatrixManager.ResetMatrixRule(type == "simple");
			Close();
		}
	}

	private void HelpBtnClick(object sender, RoutedEventArgs e)
	{
		ShowExampleWindow();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/eisenhower/editquadrantwindow.xaml", UriKind.Relative);
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
			TitleText = (TextBlock)target;
			break;
		case 2:
			ProImage = (Image)target;
			break;
		case 3:
			InputGrid = (Grid)target;
			break;
		case 4:
			QuadrantNameText = (EmojiEditor)target;
			break;
		case 5:
			EmojiSelectGrid = (Grid)target;
			EmojiSelectGrid.MouseLeftButtonUp += ShowEmojiSelector;
			break;
		case 6:
			IconGrid = (Grid)target;
			break;
		case 7:
			QuadrantIcon = (Path)target;
			break;
		case 8:
			SetEmojiIcon = (Path)target;
			break;
		case 9:
			EmojiText = (EmjTextBlock)target;
			break;
		case 10:
			EmojiSelectPopup = (EscPopup)target;
			break;
		case 11:
			EmojiSelector = (EmojiSelectControl)target;
			break;
		case 12:
			ErrorText = (TextBlock)target;
			break;
		case 13:
			FilterControl = (NormalFilterControl)target;
			break;
		case 14:
			ResetText = (TextBlock)target;
			ResetText.MouseLeftButtonUp += ResetClick;
			break;
		case 15:
			SaveButton = (Button)target;
			SaveButton.Click += SaveBtnClick;
			break;
		case 16:
			((Button)target).Click += CancelBtnClick;
			break;
		case 17:
			HelpBtn = (Button)target;
			HelpBtn.Click += HelpBtnClick;
			break;
		case 18:
			((Button)target).Click += CancelBtnClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
