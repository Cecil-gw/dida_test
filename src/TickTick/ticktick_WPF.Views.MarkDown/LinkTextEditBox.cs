using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using CommonMark.Syntax;
using Emoji.Wpf;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.DateParser;
using ticktick_WPF.Views.Config;
using ticktick_WPF.Views.MarkDown.Colorizer;
using ticktick_WPF.Views.MarkDown.SpellCheck;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Project;
using ticktick_WPF.Views.QuickAdd;
using ticktick_WPF.Views.Search;

namespace ticktick_WPF.Views.MarkDown;

public class LinkTextEditBox : TextEditor, ILinkTextEditor, ISpellChecker, IEmojiRender, IDateParseBox
{
	private DateTokenGenerator _dateTokenGenerator;

	private static Pen _selectionBorder;

	private static SolidColorBrush _selectionBrush;

	private DeleteLineColorizer _deleteLine = new DeleteLineColorizer();

	private static readonly Regex LinkRegex;

	public static readonly DependencyProperty LineSpacingProperty;

	public static readonly DependencyProperty LinkSyntaxTreeProperty;

	public static readonly DependencyProperty RenderLinkProperty;

	public static readonly DependencyProperty AcceptReturnProperty;

	public static readonly DependencyProperty TextStatusProperty;

	public static readonly DependencyProperty EnableSpellCheckProperty;

	public static readonly DependencyProperty ReadOnlyProperty;

	public static readonly DependencyProperty IsDarkProperty;

	public readonly Dictionary<int, LinkInfo> LinkNameDict = new Dictionary<int, LinkInfo>();

	public readonly Dictionary<int, LinkInfo> LinkUrlDict = new Dictionary<int, LinkInfo>();

	public readonly Dictionary<int, string> EmojiDict = new Dictionary<int, string>();

	private bool _currentFocused;

	private InsertLinkWindow _linkWindow;

	private string _pasteContent;

	private int _previousLineCount;

	private int _parseLinkRandom;

	private SolidColorBrush _highLightColor;

	private SolidColorBrush _bracketColor;

	private int _caretBeforeKeyDown;

	protected bool ShowUrlNameOnly;

	private SearchHighlightBackgroundRender _searchRender;

	public IPaserDueDate ParsedData;

	private bool _selectionPopupOpened;

	private bool _inImeProcess;

	private SolidColorBrush _caretColor;

	private bool? _isDark;

	private string _color100 = "BaseColorOpacity100_80";

	private string _color80 = "BaseColorOpacity80_60";

	private string _color40 = "BaseColorOpacity40";

	private string _color20 = "BaseColorOpacity20";

	private string _uid;

	private string _filterContent;

	private QuickSelectionControl _selectionControl;

	private bool _canSplit;

	private bool? _canMerge;

	private int _markIndex;

	private readonly QuickSetProjectPopupState _projectPopupState = new QuickSetProjectPopupState();

	private readonly ImeCompositionRefresh _projectImeRefresh = new ImeCompositionRefresh();

	private bool _isLight;

	public Popup SelectionPopup;

	private ProjectOrGroupPopup _projectOrGroupPopup;

	public int MaxLength { get; set; } = -1;

	public double LineSpacing
	{
		get
		{
			return (double)GetValue(LineSpacingProperty);
		}
		set
		{
			SetValue(LineSpacingProperty, value);
		}
	}

	public bool IsDark
	{
		get
		{
			return (bool)GetValue(IsDarkProperty);
		}
		set
		{
			SetValue(IsDarkProperty, value);
		}
	}

	public bool AcceptReturn
	{
		get
		{
			return (bool)GetValue(AcceptReturnProperty);
		}
		set
		{
			SetValue(AcceptReturnProperty, value);
		}
	}

	public bool RenderLink
	{
		get
		{
			return (bool)GetValue(RenderLinkProperty);
		}
		set
		{
			SetValue(RenderLinkProperty, value);
		}
	}

	public int TextStatus
	{
		get
		{
			return (int)GetValue(TextStatusProperty);
		}
		set
		{
			SetValue(TextStatusProperty, value);
		}
	}

	public bool EnableSpellCheck
	{
		get
		{
			return (bool)GetValue(EnableSpellCheckProperty);
		}
		set
		{
			if (!value)
			{
				SpellCheckProvider?.Disconnect();
			}
		}
	}

	public SpellCheckProvider SpellCheckProvider { get; set; }

	public bool AutoGetUrlTitle { get; set; } = true;

	public bool SelectionPopupOpened => _selectionPopupOpened;

	public bool KeyboardFocused => base.TextArea.IsKeyboardFocused;

	public bool CurrentFocused
	{
		get
		{
			return _currentFocused;
		}
		set
		{
			_currentFocused = value;
		}
	}

	private CommonMark.Syntax.Block LinkSyntaxTree
	{
		get
		{
			return (CommonMark.Syntax.Block)GetValue(LinkSyntaxTreeProperty);
		}
		set
		{
			SetValue(LinkSyntaxTreeProperty, value);
		}
	}

	public bool ReadOnly
	{
		get
		{
			return (bool)GetValue(ReadOnlyProperty);
		}
		set
		{
			base.IsReadOnly = value;
			base.TextArea.Caret.CaretBrush = (value ? Brushes.Transparent : ThemeUtil.GetColor(_color100, this));
			if (value)
			{
				base.TextArea.ClearSelection();
			}
		}
	}

	public bool CanParseDate { get; set; }

	public bool ParsingDate
	{
		get
		{
			if (CanParseDate)
			{
				return ParsedData != null;
			}
			return false;
		}
	}

	public EscPopup SetProjectPopup { get; set; }

	public bool AllowQuickSet { get; set; } = true;

	public bool QuickSetUsedInCalendar { get; set; }

	public string QuickSetAccountId { get; set; }

	public Func<string> QuickSetAccountIdProvider { get; set; }

	public string QuickSetCalendarId { get; set; }

	public Func<string> QuickSetCalendarIdProvider { get; set; }

	public Func<string> QuickSetCalendarSelectorAccountIdProvider { get; set; }

	private string MarkRegexText { get; set; } = "([/|、|#|@|~|^|!|！|\\*])(.*)";

	public bool IsInOperation { get; set; }

	public event EventHandler<IPaserDueDate> DateParsed;

	public event EventHandler<ProjectTask> Navigate;

	public event EventHandler<double> CaretVerticalOffsetChanged;

	public event EventHandler<int> CaretChanged;

	public event EventHandler<System.Windows.Input.KeyEventArgs> KeysUp;

	public event EventHandler<System.Windows.Input.KeyEventArgs> KeysDown;

	public event EventHandler MoveUp;

	public event EventHandler MoveDown;

	public event EventHandler EscKeyUp;

	public event EventHandler<int> SplitText;

	public event EventHandler TextLostFocus;

	public event EventHandler<RoutedEventArgs> TextGotFocus;

	public event EventHandler PopOpened;

	public event EventHandler PopClosed;

	public event EventHandler LinkPopClosed;

	public event EventHandler LinkPopOpened;

	public event EventHandler SaveContent;

	public event EventHandler SelectDate;

	public event EventHandler LinkTextChange;

	public event EventHandler<List<string>> IgnoreTokenChanged;

	public event EventHandler<string> MergeText;

	public event EventHandler<QuickSetModel> QuickItemSelected;

	static LinkTextEditBox()
	{
		LinkRegex = new Regex("\\[(.*)\\]\\((.*)\\)");
		LineSpacingProperty = DependencyProperty.Register("LineSpacing", typeof(double), typeof(LinkTextEditBox), new PropertyMetadata(7.0, OnLineSpacingChanged));
		LinkSyntaxTreeProperty = DependencyProperty.Register("LinkSyntaxTree", typeof(CommonMark.Syntax.Block), typeof(LinkTextEditBox), new PropertyMetadata((object)null));
		RenderLinkProperty = DependencyProperty.Register("RenderLink", typeof(bool), typeof(LinkTextEditBox), new PropertyMetadata(true));
		AcceptReturnProperty = DependencyProperty.Register("AcceptReturn", typeof(bool), typeof(LinkTextEditBox), new PropertyMetadata(false));
		TextStatusProperty = DependencyProperty.Register("TextStatus", typeof(int), typeof(LinkTextEditBox), new PropertyMetadata(0, OnStatusChanged));
		EnableSpellCheckProperty = DependencyProperty.Register("EnableSpellCheck", typeof(bool), typeof(LinkTextEditBox), new PropertyMetadata(true, OnSpellCheckChanged));
		ReadOnlyProperty = DependencyProperty.Register("ReadOnly", typeof(bool), typeof(LinkTextEditBox), new PropertyMetadata(false, OnIsReadOnlyChangedCallback));
		IsDarkProperty = DependencyProperty.Register("IsDark", typeof(bool), typeof(LinkTextEditBox), new PropertyMetadata(false, OnIsDarkChanged));
		_selectionBorder = new Pen(Brushes.Transparent, 0.0);
	}

	public LinkTextEditBox()
	{
		_uid = Utils.GetGuid();
		base.Loaded += OnLoaded;
		base.Loaded += delegate
		{
			BindEvents();
		};
		base.Unloaded += delegate
		{
			UnbindEvents();
		};
		if (_selectionBrush == null)
		{
			_selectionBrush = new SolidColorBrush
			{
				Color = (Color)FindResource(SystemColors.HighlightColorKey),
				Opacity = 0.3
			};
		}
		base.Name = "EditBox";
		base.WordWrap = false;
		SetValue(ScrollViewer.PanningModeProperty, PanningMode.HorizontalFirst);
		base.TextArea.TextView.LineSpacing = 4.0;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		base.Loaded -= OnLoaded;
		InitEditBox();
	}

	private static void OnLineSpacingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is LinkTextEditBox linkTextEditBox && e.NewValue is double num)
		{
			linkTextEditBox.LineSpacing = num;
			linkTextEditBox.TextArea.TextView.LineSpacing = (linkTextEditBox.WordWrap ? num : 4.0);
		}
	}

	private static void OnIsDarkChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is LinkTextEditBox linkTextEditBox)
		{
			linkTextEditBox.SetThemeColor(e.NewValue as bool? == true);
		}
	}

	private void SetThemeColor(bool isDark)
	{
		if (_isDark != isDark)
		{
			_isDark = isDark;
			SetTextHighLight();
			base.TextArea.TextView.Redraw();
		}
	}

	private static void OnIsReadOnlyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		if (sender is LinkTextEditBox linkTextEditBox && e.NewValue != null)
		{
			linkTextEditBox.ReadOnly = (bool)e.NewValue;
		}
	}

	private static void OnSpellCheckChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		if (sender is LinkTextEditBox linkTextEditBox && e.NewValue is bool enableSpellCheck)
		{
			linkTextEditBox.EnableSpellCheck = enableSpellCheck;
		}
	}

	private static void OnStatusChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		if (sender is LinkTextEditBox linkTextEditBox)
		{
			linkTextEditBox.SetTextForeground();
			linkTextEditBox.SetTextHighLight();
			linkTextEditBox.SetDeleteLine();
		}
	}

	public void SetWordWrap(bool wrap)
	{
		if (base.WordWrap != wrap)
		{
			base.WordWrap = wrap;
			SetValue(ScrollViewer.PanningModeProperty, (!wrap) ? PanningMode.HorizontalFirst : PanningMode.None);
			base.TextArea.TextView.LineSpacing = (wrap ? LineSpacing : 4.0);
		}
	}

	public void SetText(string val)
	{
		base.TextChanged -= OnEditBoxTextChanged;
		LinkNameDict.Clear();
		LinkUrlDict.Clear();
		EmojiDict.Clear();
		base.Text = val;
		OnTextSet();
		base.TextChanged += OnEditBoxTextChanged;
	}

	public void SetTextOffset(string text, bool restoreOffset, bool clearUndo = false)
	{
		base.TextChanged -= OnEditBoxTextChanged;
		SetTextAndOffset(text, restoreOffset, clearUndo);
		OnTextSet();
		base.TextChanged += OnEditBoxTextChanged;
	}

	private void OnTextSet()
	{
		_parseLinkRandom = -1;
		LinkSyntaxTree = AbstractSyntaxTree.GenerateAbstractSyntaxTree(base.Text);
		LogLinkPositions();
		LogEmojiPosition();
		SpellCheckProvider?.SetTextChanged(change: true, delay: false);
		_searchRender?.SetTextChanged(b: true);
		base.TextArea.TextView.Redraw();
	}

	private void LogEmojiPosition()
	{
		EmojiDict.Clear();
		List<KeyValuePair<int, int>> source = LinkNameDict.Select((KeyValuePair<int, LinkInfo> kv) => new KeyValuePair<int, int>(kv.Key, kv.Key + kv.Value.Link.Length)).ToList();
		foreach (Match m in EmojiData.MatchOne2.Matches(base.Text))
		{
			if (!source.Any((KeyValuePair<int, int> range) => m.Index >= range.Key && m.Index < range.Value))
			{
				EmojiDict[m.Index] = m.Value;
			}
		}
	}

	public Dictionary<int, string> GetEmojiDict()
	{
		return EmojiDict;
	}

	private void InitEditBox()
	{
		SetupGenerators();
		_dateTokenGenerator = new DateTokenGenerator(this);
		base.TextArea.TextView.ElementGenerators.Add(_dateTokenGenerator);
		SetTextHighLight();
		base.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
		base.Background = Brushes.Transparent;
		base.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
		base.LostFocus += OnLostFocus;
		base.VerticalAlignment = VerticalAlignment.Center;
		base.GotFocus += OnFocused;
		base.MouseWheel += OnMouseWheel;
		base.VerticalContentAlignment = VerticalAlignment.Center;
		base.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Stretch;
		base.PreviewKeyDown += OnKeyDown;
		base.PreviewKeyUp += OnKeyUp;
		SetTextForeground();
		base.Options.IndentationSize = 4;
		base.Options.EnableHyperlinks = false;
		base.Options.ConvertTabsToSpaces = true;
		base.Options.AllowScrollBelowDocument = true;
		base.Options.EnableEmailHyperlinks = false;
		base.Options.InheritWordWrapIndentation = false;
		base.TextArea.FocusVisualStyle = null;
		base.TextArea.SelectionForeground = null;
		base.TextArea.SelectionCornerRadius = 0.0;
		base.TextArea.VerticalAlignment = VerticalAlignment.Center;
		base.TextArea.VerticalContentAlignment = VerticalAlignment.Bottom;
		base.TextArea.SelectionBorder = _selectionBorder;
		base.TextArea.SelectionBrush = _selectionBrush;
		base.TextArea.TextView.LineSpacing = (base.WordWrap ? LineSpacing : 4.0);
		System.Windows.DataObject.RemovePastingHandler(this, OnPaste);
		System.Windows.DataObject.AddPastingHandler(this, OnPaste);
	}

	private void BindEvents()
	{
		base.TextChanged -= OnEditBoxTextChanged;
		base.TextChanged += OnEditBoxTextChanged;
		base.TextArea.Caret.PositionChanged += NotifyCaretChanged;
		DataChangedNotifier.ThemeModeChanged += OnAppThemeChanged;
		DataChangedNotifier.ShowCompleteLineChanged += OnShowCompleteLineChanged;
		LocalSettings.SpellCheckChanged += OnSpellCheckChanged;
	}

	private void OnShowCompleteLineChanged(object sender, EventArgs e)
	{
		SetDeleteLine();
	}

	private void UnbindEvents()
	{
		_projectImeRefresh.Cancel();
		base.TextArea.Caret.PositionChanged -= NotifyCaretChanged;
		base.TextChanged -= OnEditBoxTextChanged;
		DataChangedNotifier.ThemeModeChanged -= OnAppThemeChanged;
		DataChangedNotifier.ShowCompleteLineChanged -= OnShowCompleteLineChanged;
		LocalSettings.SpellCheckChanged -= OnSpellCheckChanged;
		DelayActionHandlerCenter.RemoveAction(_uid);
	}

	private void TryParseDate()
	{
		QuickSelectionControl selectionControl = _selectionControl;
		if ((selectionControl != null && selectionControl.PopupOpened()) || !LocalSettings.Settings.DateParsing || _dateTokenGenerator == null)
		{
			return;
		}
		string text = base.Text;
		if (_dateTokenGenerator.GetIgnoreTokens().Any())
		{
			text = _dateTokenGenerator.GetIgnoreTokens().Aggregate(text, (string current, string token) => current.Replace(token, string.Empty));
		}
		ParsedData = HandleParsedDate(DateParser.Parse(text, GetDateParseBaseDate()), text);
		if (ParsedData != null && ParsedData.GetRecognizeStrings().Any())
		{
			List<string> list = ParsedData.GetRecognizeStrings()?.Select((string str) => str.TrimEnd()).ToList() ?? new List<string>();
			list.RemoveAll((string t) => t.EndsWith("："));
			_dateTokenGenerator.AddTokens(list, text);
		}
		else
		{
			ParsedData = null;
			_dateTokenGenerator.ClearTokens();
		}
		base.TextArea.TextView.Redraw();
		this.DateParsed?.Invoke(this, ParsedData);
	}

	protected virtual IPaserDueDate HandleParsedDate(IPaserDueDate parsedData, string content)
	{
		return parsedData;
	}

	protected virtual DateTime? GetDateParseBaseDate()
	{
		return null;
	}

	private void OnSpellCheckChanged(object sender, EventArgs e)
	{
		if (EnableSpellCheck)
		{
			if (LocalSettings.Settings.SpellCheckEnable)
			{
				SpellCheckProvider?.Initialize(this);
			}
			else
			{
				SpellCheckProvider?.Disconnect();
			}
			base.TextChanged -= OnEditBoxTextChanged;
			SpellCheckProvider?.ForceRefresh();
			base.TextChanged += OnEditBoxTextChanged;
		}
	}

	private void NotifyTextChanged(object sender, EventArgs e)
	{
		Utils.RunOnUiThread(base.Dispatcher, delegate
		{
			this.LinkTextChange?.Invoke(sender, e);
		});
	}

	private void OnAppThemeChanged(object sender, EventArgs e)
	{
		SetTextHighLight();
		base.TextArea.TextView.Redraw();
	}

	public void SetLightTheme()
	{
		SetTextHighLight();
	}

	public void SetBaseColor(string color100 = "BaseColorOpacity100_80", string color80 = "BaseColorOpacity80_60", string color40 = "BaseColorOpacity40", string color20 = "BaseColorOpacity20")
	{
		_color100 = color100;
		_color80 = color80;
		_color40 = color40;
		_color20 = color20;
	}

	private void SetTextHighLight()
	{
		if (TextStatus == 0 || TextStatus == 8060)
		{
			_highLightColor = ThemeUtil.GetColor("TextAccentColor", this);
		}
		else
		{
			_highLightColor = ThemeUtil.GetColor(_color40, this);
		}
		base.TextArea.TextView.LinkTextForegroundBrush = _highLightColor;
		_bracketColor = ThemeUtil.GetColor(_color20, this);
		_caretColor = ThemeUtil.GetColor(_color100, this);
		base.TextArea.Caret.CaretBrush = (base.IsReadOnly ? Brushes.Transparent : _caretColor);
	}

	public void SetTextForeground()
	{
		switch (TextStatus)
		{
		case 0:
			SetResourceReference(System.Windows.Controls.Control.ForegroundProperty, _color100);
			break;
		case 8060:
			SetResourceReference(System.Windows.Controls.Control.ForegroundProperty, _color80);
			break;
		default:
			SetResourceReference(System.Windows.Controls.Control.ForegroundProperty, _color40);
			break;
		}
	}

	private void SetDeleteLine()
	{
		if (LocalSettings.Settings.ExtraSettings.ShowCompleteLine == 1 && (TextStatus == 2 || TextStatus == 1 || TextStatus == -1))
		{
			base.TextArea.TextView.LineTransformers.Remove(_deleteLine);
			base.TextArea.TextView.LineTransformers.Add(_deleteLine);
		}
		else
		{
			base.TextArea.TextView.LineTransformers.Remove(_deleteLine);
		}
	}

	public void UnRegisterCaretChanged()
	{
		base.TextArea.Caret.PositionChanged -= NotifyCaretChanged;
	}

	public void RegisterCaretChanged()
	{
		base.TextArea.Caret.PositionChanged -= NotifyCaretChanged;
		base.TextArea.Caret.PositionChanged += NotifyCaretChanged;
	}

	public TextEditor GetEditBox()
	{
		return this;
	}

	public Brush GetBracketColor()
	{
		return _bracketColor;
	}

	private void NotifyCaretChanged(object sender, EventArgs e)
	{
		double e2 = base.TextArea.TextView.GetVisualPosition(new TextViewPosition(base.TextArea.Caret.Position.Line, Math.Max(1, base.TextArea.Caret.Column)), VisualYPosition.LineTop).Y + 21.0;
		if (CurrentFocused)
		{
			this.CaretVerticalOffsetChanged?.Invoke(this, e2);
		}
		this.CaretChanged?.Invoke(this, base.CaretOffset);
		bool flag = false;
		foreach (KeyValuePair<int, LinkInfo> item in LinkNameDict)
		{
			int valueOrDefault = (item.Value?.Link?.Length).GetValueOrDefault();
			if (valueOrDefault > 0)
			{
				int key = item.Key;
				int num = key + valueOrDefault;
				if (key < base.CaretOffset && base.CaretOffset < num)
				{
					flag = true;
					break;
				}
			}
		}
		base.TextArea.Caret.CaretBrush = (base.IsReadOnly ? Brushes.Transparent : (flag ? _highLightColor : _caretColor));
	}

	private int GetRemoveLinkLength(int caret, bool onlyUrl = false, bool behindIndex = false)
	{
		int result = -1;
		if (LinkUrlDict.Any())
		{
			int num = (behindIndex ? base.Text.IndexOf(")", caret, StringComparison.Ordinal) : base.Text.LastIndexOf("[", caret, StringComparison.Ordinal));
			if (num >= 0)
			{
				string text = (behindIndex ? base.Text.Substring(caret, num - caret + 1) : base.Text.Substring(num, caret - num));
				foreach (KeyValuePair<int, LinkInfo> item in LinkNameDict.ToList())
				{
					string text2 = "[" + item.Value.Link + "](" + item.Value.Url + ")";
					if (text == text2)
					{
						result = (onlyUrl ? item.Value.Url.Length : text.Length);
						break;
					}
				}
			}
		}
		return result;
	}

	protected virtual void SetupGenerators()
	{
		base.TextArea.TextView.ElementGenerators.Add(new EmojiGenerator(this));
		base.TextArea.TextView.ElementGenerators.Add(new CustomLinkElementGenerator(this, !RenderLink));
		if (RenderLink)
		{
			base.TextArea.TextView.ElementGenerators.Add(new LinkNameGenerator(this));
			base.TextArea.TextView.ElementGenerators.Add(new LinkUrlGenerator(this));
			base.TextArea.TextView.ElementGenerators.Add(new HideLinkBracketGenerator(this, LinkNameDict, isLeft: true));
			base.TextArea.TextView.ElementGenerators.Add(new HideLinkBracketGenerator(this, LinkNameDict, isLeft: false));
		}
		if (EnableSpellCheck)
		{
			if (SpellingService.CommonSpellingService == null)
			{
				SpellingService.CommonSpellingService = new SpellingService();
			}
			SpellCheckProvider = new SpellCheckProvider(SpellingService.CommonSpellingService);
			if (LocalSettings.Settings.SpellCheckEnable)
			{
				SpellCheckProvider.Initialize(this);
			}
		}
	}

	public void SetupSearchRender(bool isTitle, bool inSearchDetail)
	{
		if (_searchRender == null)
		{
			_searchRender = new SearchHighlightBackgroundRender(this, LinkUrlDict, isTitle, inSearchDetail);
			base.TextArea.TextView.BackgroundRenderers.Add(_searchRender);
		}
	}

	public void SetupPreSearchRender()
	{
		_searchRender = new SearchHighlightBackgroundRender(this, LinkUrlDict, isTitle: true, inDetail: false, inPreSearch: true);
		base.TextArea.TextView.BackgroundRenderers.Add(_searchRender);
	}

	public double GetFirstSearchIndex()
	{
		int num = _searchRender?.GetFirstIndex() ?? (-1);
		if (num >= 0 && num < base.Text.Length)
		{
			TextViewPosition position = new TextViewPosition(base.Document.GetLocation(num));
			return base.TextArea.TextView.GetVisualPosition(position, VisualYPosition.LineTop).Y + 21.0;
		}
		return -1.0;
	}

	public void ShowInsertLink(LinkLineInfoModel model)
	{
		if (ReadOnly)
		{
			return;
		}
		if (string.IsNullOrEmpty(model.Url))
		{
			string text = TaskUtils.TryGetClipboardText();
			if (!string.IsNullOrEmpty(text))
			{
				if (new Regex("(https?:\\/\\/(?:www\\.|(?!www))[a-zA-Z0-9][a-zA-Z0-9-]+[a-zA-Z0-9]?\\.[^\\s]{2,}|www\\.[a-zA-Z0-9][a-zA-Z0-9-]+[a-zA-Z0-9]\\.[^\\s]{2,}|https?:\\/\\/(?:www\\.|(?!www))[a-zA-Z0-9]\\.[^\\s]{2,}|www\\.[a-zA-Z0-9]\\.[^\\s]{2,})").IsMatch(text))
				{
					model.Url = text;
				}
				ProjectTask projectTask = TaskUtils.ParseTaskUrl(text);
				if (projectTask != null)
				{
					model.Url = text.Replace(" " + projectTask.Title, string.Empty).TrimEnd();
				}
			}
		}
		else
		{
			model.IsNew = false;
		}
		if (!KeyboardFocused)
		{
			FocusEnd();
		}
		InsertLink(model);
	}

	public void ShowInsertLinkForCurrentContext()
	{
		LinkLineInfoModel linkLineInfoModel = TryGetCurrentLinkLineInfo();
		if (linkLineInfoModel == null)
		{
			linkLineInfoModel = new LinkLineInfoModel
			{
				Text = base.SelectedText
			};
		}
		ShowInsertLink(linkLineInfoModel);
	}

	private LinkLineInfoModel TryGetCurrentLinkLineInfo()
	{
		return MarkdownLinkContextHelper.TryCreateCurrentLinkLineInfo(base.SelectionStart, base.SelectionLength, base.CaretOffset, LinkNameDict, LinkUrlDict, (int offset) => base.Document?.GetLineByOffset(Math.Max(0, Math.Min(offset, base.Text?.Length ?? 0)))?.LineNumber, (int lineNumber) => base.TextArea.TextView.GetVisualLine(lineNumber));
	}

	private async void InsertLink(LinkLineInfoModel model)
	{
		if (string.IsNullOrEmpty(model.Text) || !model.Text.Contains("\n"))
		{
			this.LinkPopOpened?.Invoke(this, null);
			Point startPosition = GetStartPosition(model.VisualLine, 320, 210);
			_linkWindow = new InsertLinkWindow(this, model)
			{
				Owner = Window.GetWindow(this)
			};
			_linkWindow.Closed -= OnLinkClosed;
			_linkWindow.Closed += OnLinkClosed;
			_linkWindow.TryShowDialog(startPosition.X + 10.0, startPosition.Y + 10.0);
		}
	}

	private void OnLinkClosed(object sender, EventArgs e)
	{
		this.LinkPopClosed?.Invoke(sender, e);
	}

	private void OnEditBoxTextChanged(object sender, EventArgs eventArgs)
	{
		HandleOnTextChanged();
		DelayActionHandlerCenter.TryDoAction(_uid, NotifyTextChanged, 50);
	}

	private void HandleOnTextChanged()
	{
		try
		{
			if (!SearchHelper.DetailChanged)
			{
				SearchHighlightBackgroundRender searchRender = _searchRender;
				if (searchRender != null && searchRender.InDetail)
				{
					SearchHelper.DetailChanged = true;
				}
			}
			if (CanParseDate)
			{
				TryParseDate();
			}
			LinkSyntaxTree = AbstractSyntaxTree.GenerateAbstractSyntaxTree(base.Text);
			LogLinkPositions();
			LogEmojiPosition();
			SpellCheckProvider?.SetTextChanged(change: true);
			_searchRender?.SetTextChanged(b: true);
			base.TextArea.TextView.Redraw();
			TryShowQuickSetPopup();
		}
		catch (Exception)
		{
		}
	}

	private void LogLinkPositions()
	{
		LinkNameDict.Clear();
		LinkUrlDict.Clear();
		CollectLinkPositions(base.Document.Text, LinkSyntaxTree, LinkNameDict, LinkUrlDict);
	}

	private static void CollectLinkPositions(string document, CommonMark.Syntax.Block syntaxTree, IDictionary<int, LinkInfo> linkNames, IDictionary<int, LinkInfo> linkUrls)
	{
		int length = document.Length;
		foreach (CommonMark.Syntax.Block item in AbstractSyntaxTree.EnumerateSpanningBlocks(syntaxTree, 0, length))
		{
			foreach (CommonMark.Syntax.Inline item2 in AbstractSyntaxTree.EnumerateInlines(item.InlineContent))
			{
				if (item2.Tag != InlineTag.Link || item2.SourcePosition < 0 || item2.SourceLength < 0 || item2.SourcePosition + item2.SourceLength > document.Length)
				{
					continue;
				}
				string text = document.Substring(item2.SourcePosition, item2.SourceLength);
				if (text.Contains("["))
				{
					int num = text.IndexOf("[", StringComparison.Ordinal);
					int num2 = item2.SourcePosition + num;
					if (num2 >= 0 && num2 + item2.SourceLength <= document.Length)
					{
						text = document.Substring(num2, item2.SourceLength);
					}
				}
				Match match = LinkRegex.Match(text);
				if (match.Success)
				{
					string value = match.Groups[1].Value;
					string value2 = match.Groups[2].Value;
					if (!string.IsNullOrEmpty(value2))
					{
						LinkInfo value3 = new LinkInfo(value, value2);
						linkNames.Add(item2.SourcePosition + match.Groups[1].Index, value3);
						linkUrls.Add(item2.SourcePosition + match.Groups[2].Index, value3);
					}
				}
			}
		}
	}

	private static string GetPasteText(System.Windows.IDataObject dataObject, int availableLength)
	{
		string result = dataObject.GetData(System.Windows.DataFormats.UnicodeText, autoConvert: true) as string;
		try
		{
			string text = dataObject.GetData("TickTickMarkdown", autoConvert: false) as string;
			if (!string.IsNullOrEmpty(text))
			{
				string text2 = MarkdownClipboardHtmlRenderer.CreatePlainText(text, preserveLinks: true);
				if (availableLength < 0 || text2.Length <= availableLength)
				{
					return text2;
				}
			}
		}
		catch (Exception)
		{
		}
		return result;
	}

	protected virtual async void OnPaste(object sender, DataObjectPastingEventArgs arg)
	{
		try
		{
			MoveCaretAfterHiddenLink();
			string pasteText = GetPasteText(arg.SourceDataObject, (MaxLength < 0) ? (-1) : Math.Max(0, MaxLength - base.Text.Length + base.SelectionLength));
			ProjectTask projectTask = TaskUtils.ParseTaskUrl(pasteText) ?? TaskUtils.ParseTaskUrlWithoutTitle(pasteText);
			if (projectTask != null && pasteText != null && !pasteText.StartsWith("[") && !pasteText.EndsWith(")"))
			{
				pasteText = (AcceptReturn ? pasteText : pasteText.Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " "));
				string text = pasteText.Replace(" " + projectTask.Title, string.Empty);
				arg.CancelCommand();
				await TryPasteLinkTask(projectTask, text);
			}
			else
			{
				if (pasteText == null)
				{
					return;
				}
				pasteText = (AcceptReturn ? pasteText : pasteText.Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " "));
				arg.CancelCommand();
				if (MaxLength >= 0 && base.Text.Length + pasteText.Length - base.SelectionLength > MaxLength)
				{
					if (!base.WordWrap)
					{
						pasteText = pasteText.Replace("\n", " ").Replace("\r", " ");
					}
					pasteText = pasteText.Substring(0, MaxLength - base.Text.Length + base.SelectionLength);
					InsertText(pasteText);
				}
				else
				{
					if (!base.WordWrap)
					{
						pasteText = pasteText.Replace("\n", " ").Replace("\r", " ");
					}
					InsertText(pasteText);
				}
			}
		}
		catch (Exception)
		{
		}
	}

	private void InsertText(string text)
	{
		int num = base.CaretOffset;
		if (base.SelectionLength > 0)
		{
			base.Document.Remove(base.SelectionStart, base.SelectionLength);
			num = base.SelectionStart;
		}
		base.Document.Insert(num, text);
		base.CaretOffset = num + text.Length;
	}

	protected override void OnPreviewTextInput(TextCompositionEventArgs e)
	{
		MoveCaretAfterHiddenLink();
		if (MaxLength >= 0)
		{
			string text = e.Text;
			if (base.Text.Length >= MaxLength)
			{
				e.Handled = true;
				Utils.Toast(Utils.GetString("CharactersNumberLimit"));
			}
			else if (base.Text.Length + text.Length - base.SelectionLength > MaxLength)
			{
				e.Handled = true;
				text = text.Substring(0, Math.Max(0, MaxLength - base.Text.Length + base.SelectionLength));
				if (text.Length == 0)
				{
					Utils.Toast(Utils.GetString("CharactersNumberLimit"));
					return;
				}
				InsertText(text);
			}
		}
		base.OnPreviewTextInput(e);
	}

	private async Task TryPasteLinkTask(ProjectTask extra, string text)
	{
		string text2 = extra.Title;
		if (string.IsNullOrEmpty(text2))
		{
			TaskModel taskModel = await TaskDao.GetThinTaskById(extra.TaskId);
			text2 = ((taskModel == null || string.IsNullOrEmpty(taskModel.title)) ? Utils.GetString("MyTask") : taskModel.title);
		}
		base.Document.Insert(base.CaretOffset, "[" + text2 + "](" + text + ")");
	}

	private Point GetStartPosition(VisualLine line = null, int width = 245, int height = 140)
	{
		TextView textView = base.TextArea.TextView;
		TextViewPosition position = new TextViewPosition(base.Document.GetLocation(base.CaretOffset));
		if (line != null)
		{
			position = new TextViewPosition(base.Document.GetLocation(line.StartOffset));
		}
		Point visualPosition = textView.GetVisualPosition(position, VisualYPosition.LineBottom);
		Point visualPosition2 = textView.GetVisualPosition(position, VisualYPosition.LineBottom);
		Point point = textView.PointToScreen(visualPosition - textView.ScrollOffset);
		Point point2 = textView.PointToScreen(visualPosition2 - textView.ScrollOffset);
		Size size = new Size(width, height).TransformToDevice(textView);
		Rect rect = new Rect(point, size);
		Rect rect2 = Screen.GetWorkingArea(point.ToSystemDrawing()).ToWpf();
		if (!rect2.Contains(rect))
		{
			if (rect.Left < rect2.Left)
			{
				rect.X = rect2.Left;
			}
			else if (rect.Right > rect2.Right)
			{
				rect.X = rect2.Right - rect.Width;
			}
			if (rect.Bottom > rect2.Bottom)
			{
				rect.Y = point2.Y - rect.Height;
			}
			if (rect.Y < rect2.Top)
			{
				rect.Y = rect2.Top;
			}
		}
		rect = rect.TransformFromDevice(textView);
		return new Point(rect.X - 20.0, rect.Y - 20.0);
	}

	protected void OnPopupOpenChanged(bool open)
	{
		if (open)
		{
			this.PopOpened?.Invoke(this, null);
		}
		else
		{
			this.PopClosed?.Invoke(this, null);
		}
	}

	protected void EditorMenuOnContextMenuOpening(object sender, ContextMenuEventArgs context)
	{
		if (!ReadOnly)
		{
			AddEditorContextMenu(context);
			OnPopupOpenChanged(open: true);
		}
	}

	public void AddEditorContextMenu(ContextMenuEventArgs context)
	{
		System.Windows.Controls.ContextMenu contextMenu = new System.Windows.Controls.ContextMenu
		{
			Width = 230.0
		};
		contextMenu.SetResourceReference(FrameworkElement.StyleProperty, "ContextMenuStyle");
		contextMenu.SetResourceReference(System.Windows.Controls.Control.BackgroundProperty, "PopupBackground");
		contextMenu.Closed += delegate
		{
			OnPopupOpenChanged(open: false);
		};
		contextMenu.PlacementTarget = this;
		if (EnableSpellCheck && !ReadOnly)
		{
			SpellCheckSuggestion.SpellCheckSuggestions(this, contextMenu);
			SpellCheckSuggestion.AddSpellCheckLanguageMenu(contextMenu);
		}
		contextMenu.Items.Add(new EditMenuItem
		{
			Title = Utils.GetString("Cut"),
			Shortcut = "Ctrl+X",
			Command = ApplicationCommands.Cut,
			CommandTarget = base.TextArea
		});
		contextMenu.Items.Add(new EditMenuItem
		{
			Title = Utils.GetString("Copy"),
			Command = ApplicationCommands.Copy,
			CommandTarget = base.TextArea,
			Shortcut = "Ctrl+C"
		});
		EditMenuItem editMenuItem = new EditMenuItem
		{
			Title = Utils.GetString("Paste"),
			Command = ApplicationCommands.Paste,
			CommandTarget = base.TextArea,
			Shortcut = "Ctrl+V"
		};
		try
		{
			System.Windows.IDataObject dataObject = System.Windows.Clipboard.GetDataObject();
			editMenuItem.CommandParameter = dataObject;
		}
		catch (Exception ex)
		{
			UtilLog.Error("Clipboard.GetDataObject: " + ex.Message);
		}
		contextMenu.Items.Add(editMenuItem);
		Separator separator = new Separator();
		separator.SetResourceReference(FrameworkElement.StyleProperty, "MenuSeparatorStyle");
		contextMenu.Items.Add(separator);
		contextMenu.Items.Add(new EditMenuItem
		{
			Title = Utils.GetString("Undo"),
			Command = ApplicationCommands.Undo,
			CommandTarget = base.TextArea,
			Shortcut = "Ctrl+Z"
		});
		contextMenu.Items.Add(new EditMenuItem
		{
			Title = Utils.GetString("Redo"),
			Command = ApplicationCommands.Redo,
			CommandTarget = base.TextArea,
			Shortcut = "Ctrl+Shift+Z"
		});
		((FrameworkElement)context.Source).ContextMenu = contextMenu;
		contextMenu.IsOpen = true;
	}

	public string GetDisplayText()
	{
		return GetDisplayText(base.Text, LinkNameDict);
	}

	public static string GetPlainTextWithLinks(string text)
	{
		text = text ?? string.Empty;
		Dictionary<int, LinkInfo> dictionary = new Dictionary<int, LinkInfo>();
		Dictionary<int, LinkInfo> linkUrls = new Dictionary<int, LinkInfo>();
		CollectLinkPositions(text, AbstractSyntaxTree.GenerateAbstractSyntaxTree(text), dictionary, linkUrls);
		return GetDisplayText(text, dictionary);
	}

	private static string GetDisplayText(string text, IDictionary<int, LinkInfo> links)
	{
		string text2 = text ?? string.Empty;
		foreach (KeyValuePair<int, LinkInfo> item in links.OrderByDescending((KeyValuePair<int, LinkInfo> pair) => pair.Key))
		{
			int num = item.Key - 1;
			string text3 = "[" + item.Value.Link + "](" + item.Value.Url + ")";
			if (num >= 0 && num + text3.Length <= text2.Length && string.Compare(text2, num, text3, 0, text3.Length, StringComparison.Ordinal) == 0)
			{
				text2 = text2.Remove(num, text3.Length).Insert(num, item.Value.Link);
			}
		}
		return text2;
	}

	public Dictionary<int, LinkInfo> GetLinkNameDict()
	{
		return LinkNameDict;
	}

	public Dictionary<int, LinkInfo> GetLinkUrlDict()
	{
		return LinkUrlDict;
	}

	public void NavigateTask(string projectId, string taskId)
	{
		this.Navigate?.Invoke(this, new ProjectTask
		{
			ProjectId = projectId,
			TaskId = taskId
		});
	}

	public void ResetText(string text)
	{
		UnRegisterCaretChanged();
		base.Text = text;
		OnEditBoxTextChanged(null, null);
		RegisterCaretChanged();
	}

	public Brush GetHighLightColor()
	{
		return _highLightColor;
	}

	public int GetPageLastIndex(double height)
	{
		TextView textView = base.TextArea.TextView;
		DocumentLine documentLine = null;
		foreach (DocumentLine line in base.Document.Lines)
		{
			TextViewPosition position = new TextViewPosition(base.Document.GetLocation(line.Offset));
			textView.GetVisualPosition(position, VisualYPosition.LineBottom);
			TextViewPosition position2 = new TextViewPosition(base.Document.GetLocation(line.Offset));
			if (textView.GetVisualPosition(position2, VisualYPosition.LineBottom).Y > height)
			{
				documentLine = line;
				break;
			}
		}
		if (documentLine != null)
		{
			int interval = ((documentLine.Length > 1000) ? 1000 : ((documentLine.Length > 100) ? 100 : 10));
			return GetPageLastIndex(height, documentLine.Offset, documentLine.EndOffset, interval);
		}
		return base.Text.Length;
	}

	private int GetPageLastIndex(double height, int start, int end, int interval)
	{
		TextView textView = base.TextArea.TextView;
		if (start == end)
		{
			return end;
		}
		for (int i = start; i <= end; i += interval)
		{
			TextViewPosition position = new TextViewPosition(base.Document.GetLocation(i));
			Point visualPosition = textView.GetVisualPosition(position, VisualYPosition.LineBottom);
			TextViewPosition position2 = new TextViewPosition(base.Document.GetLocation(i + interval));
			Point visualPosition2 = textView.GetVisualPosition(position2, VisualYPosition.LineBottom);
			if (visualPosition.Y < height && visualPosition2.Y > height)
			{
				if (interval == 1)
				{
					return i;
				}
				return GetPageLastIndex(height, i, Math.Min(end, i + interval), interval / 10);
			}
			if (visualPosition.Y > height)
			{
				return i;
			}
		}
		return end;
	}

	private void OnKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
	{
		if (ReadOnly)
		{
			return;
		}
		if (_inImeProcess)
		{
			this.KeysUp?.Invoke(sender, e);
			_canMerge = null;
			_canSplit = false;
			_caretBeforeKeyDown = 0;
			_inImeProcess = false;
			return;
		}
		switch (e.Key)
		{
		case Key.Up:
			if (IsQuickPopupOpen())
			{
				_selectionControl.Move(forward: true);
				e.Handled = true;
				return;
			}
			if (IsProjectPopupOpen())
			{
				_projectOrGroupPopup.UpDownSelect(isUp: true);
				e.Handled = true;
				return;
			}
			if (!base.WordWrap || (_caretBeforeKeyDown == base.CaretOffset && !Utils.IfShiftPressed()))
			{
				this.MoveUp?.Invoke(sender, null);
			}
			break;
		case Key.Down:
			if (IsQuickPopupOpen())
			{
				_selectionControl.Move(forward: false);
				e.Handled = true;
				return;
			}
			if (IsProjectPopupOpen())
			{
				_projectOrGroupPopup.UpDownSelect(isUp: false);
				e.Handled = true;
				return;
			}
			if (!base.WordWrap || (_caretBeforeKeyDown == base.CaretOffset && !Utils.IfShiftPressed()))
			{
				this.MoveDown?.Invoke(sender, null);
			}
			break;
		case Key.Left:
			if (IsProjectPopupOpen())
			{
				_projectOrGroupPopup.LeftRightSelect(isLeft: true);
				e.Handled = true;
				return;
			}
			break;
		case Key.Right:
			if (IsProjectPopupOpen())
			{
				_projectOrGroupPopup.LeftRightSelect(isLeft: false);
				e.Handled = true;
				return;
			}
			break;
		case Key.Return:
			if (IsQuickPopupOpen())
			{
				_selectionControl.TrySelectItem();
				e.Handled = true;
				return;
			}
			if (IsProjectPopupOpen())
			{
				_projectOrGroupPopup.EnterSelect();
				e.Handled = true;
				return;
			}
			break;
		case Key.Back:
			if (_canMerge == true)
			{
				this.MergeText?.Invoke(this, base.Text);
			}
			break;
		}
		this.KeysUp?.Invoke(sender, e);
		_canMerge = null;
		_canSplit = false;
		if (_caretBeforeKeyDown == -1)
		{
			_caretBeforeKeyDown = 0;
		}
	}

	private void ClearFocus()
	{
		Keyboard.ClearFocus();
		Window window = Window.GetWindow(this);
		if (window != null && window.IsActive)
		{
			FocusManager.SetFocusedElement(window, window);
			Keyboard.Focus(window);
		}
	}

	private void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
	{
		if (ReadOnly)
		{
			return;
		}
		_inImeProcess = false;
		switch (e.Key)
		{
		case Key.Left:
			if (IsQuickPopupOpen() || IsProjectPopupOpen())
			{
				e.Handled = true;
			}
			if (ShowUrlNameOnly && TryMoveOverLinkBracket(isRight: false))
			{
				e.Handled = true;
			}
			break;
		case Key.Right:
			if (IsQuickPopupOpen() || IsProjectPopupOpen())
			{
				e.Handled = true;
			}
			if (ShowUrlNameOnly && TryMoveOverLinkBracket(isRight: true))
			{
				e.Handled = true;
			}
			break;
		case Key.Tab:
			e.Handled = true;
			break;
		case Key.Return:
			if (!AcceptReturn || IsQuickPopupOpen() || IsProjectPopupOpen())
			{
				e.Handled = true;
			}
			if (!Utils.IfCtrlPressed() && !IsQuickPopupOpen() && !IsProjectPopupOpen())
			{
				MoveCaretAfterHiddenLink();
				int caretOffset = base.CaretOffset;
				this.SplitText?.Invoke(this, caretOffset);
			}
			break;
		case Key.Back:
		{
			if (!_canMerge.HasValue)
			{
				_canMerge = base.CaretOffset == 0 && base.SelectedText.Length == 0;
			}
			if (!ShowUrlNameOnly)
			{
				int caretOffset2 = base.CaretOffset;
				int removeLinkLength = GetRemoveLinkLength(caretOffset2);
				if (removeLinkLength > 0)
				{
					base.Document.Remove(caretOffset2 - removeLinkLength, removeLinkLength);
					this.LinkTextChange?.Invoke(this, null);
					e.Handled = true;
				}
			}
			else if (TryDeleteLinkName(backDelete: true))
			{
				e.Handled = true;
			}
			List<string> list = _dateTokenGenerator?.GetValidTokens();
			if (list == null || list.Count <= 0)
			{
				break;
			}
			foreach (string item in list)
			{
				int num = base.Text.LastIndexOf(item, base.CaretOffset, StringComparison.Ordinal);
				if (num >= 0 && num == base.CaretOffset - item.Length)
				{
					_dateTokenGenerator.AddIgnoreToken(item);
					this.IgnoreTokenChanged?.Invoke(this, _dateTokenGenerator.GetIgnoreTokens());
					TryParseDate();
					e.Handled = true;
				}
			}
			break;
		}
		case Key.Delete:
			if (ShowUrlNameOnly && TryDeleteLinkName(backDelete: false))
			{
				e.Handled = true;
			}
			break;
		case Key.Up:
		case Key.Down:
			if (IsQuickPopupOpen() || IsProjectPopupOpen())
			{
				e.Handled = true;
			}
			break;
		case Key.Escape:
			_projectImeRefresh.Cancel();
			if (IsQuickPopupOpen())
			{
				_selectionControl?.ClosePopup();
				e.Handled = true;
				return;
			}
			if (IsProjectPopupOpen())
			{
				TryHideProjectPopup();
				e.Handled = true;
				return;
			}
			this.EscKeyUp?.Invoke(this, e);
			break;
		}
		if (base.SelectionLength > 0 && !Utils.IfShiftPressed())
		{
			switch (e.Key)
			{
			case Key.Left:
			case Key.Up:
				base.SelectionLength = 0;
				e.Handled = true;
				break;
			case Key.Right:
			case Key.Down:
			{
				int selectionStart = base.SelectionStart + base.SelectionLength;
				base.SelectionLength = 0;
				base.SelectionStart = selectionStart;
				e.Handled = true;
				break;
			}
			}
			_caretBeforeKeyDown = -1;
		}
		else if (_caretBeforeKeyDown != -1 && (e.Key == Key.Up || e.Key == Key.Down))
		{
			_caretBeforeKeyDown = base.CaretOffset;
		}
		_inImeProcess = e.ImeProcessedKey != Key.None;
		if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.D && LocalSettings.Settings.ShortCutModel.SetDate == "Ctrl+D")
		{
			e.Handled = true;
			this.SelectDate?.Invoke(this, null);
		}
		this.KeysDown?.Invoke(this, e);
	}

	private void MoveCaretAfterHiddenLink()
	{
		if (RenderLink && base.SelectionLength <= 0)
		{
			int caretOffsetAfterHiddenLink = GetCaretOffsetAfterHiddenLink(base.Text, base.CaretOffset, LinkNameDict);
			if (caretOffsetAfterHiddenLink != base.CaretOffset)
			{
				base.CaretOffset = caretOffsetAfterHiddenLink;
			}
		}
	}

	private static int GetCaretOffsetAfterHiddenLink(string text, int caretOffset, IDictionary<int, LinkInfo> linkNameDict)
	{
		if (string.IsNullOrEmpty(text) || linkNameDict == null || caretOffset < 0 || caretOffset > text.Length)
		{
			return caretOffset;
		}
		foreach (KeyValuePair<int, LinkInfo> item in linkNameDict)
		{
			LinkInfo value = item.Value;
			if (value == null || string.IsNullOrEmpty(value.Link) || string.IsNullOrEmpty(value.Url))
			{
				continue;
			}
			int num = item.Key + value.Link.Length;
			if (caretOffset == num)
			{
				int num2 = item.Key - 1;
				string text2 = "[" + value.Link + "](" + value.Url + ")";
				if (num2 >= 0 && num2 + text2.Length <= text.Length && string.Compare(text, num2, text2, 0, text2.Length, StringComparison.Ordinal) == 0)
				{
					return num2 + text2.Length;
				}
			}
		}
		return caretOffset;
	}

	private bool TryMoveOverLinkBracket(bool isRight)
	{
		int caretOffsetAcrossTitleLink = GetCaretOffsetAcrossTitleLink(base.Text, base.CaretOffset, LinkNameDict, isRight);
		if (caretOffsetAcrossTitleLink == base.CaretOffset)
		{
			return false;
		}
		base.CaretOffset = caretOffsetAcrossTitleLink;
		return true;
	}

	private static bool IsCurrentTitleLink(string text, int nameOffset, LinkInfo link)
	{
		if (string.IsNullOrEmpty(link?.Link) || nameOffset <= 0)
		{
			return false;
		}
		string text2 = "[" + link.Link + "](" + link.Url + ")";
		int num = nameOffset - 1;
		if (num <= text.Length - text2.Length)
		{
			return string.CompareOrdinal(text, num, text2, 0, text2.Length) == 0;
		}
		return false;
	}

	private static int GetTitleLinkBoundaryLength(string label, bool fromEnd)
	{
		TitleLinkTextElement titleLinkTextElement = new TitleLinkTextElement(label, null);
		int nextCaretPosition = titleLinkTextElement.GetNextCaretPosition(fromEnd ? titleLinkTextElement.VisualLength : 0, (!fromEnd) ? LogicalDirection.Forward : LogicalDirection.Backward, CaretPositioningMode.Normal);
		int relativeOffset = titleLinkTextElement.GetRelativeOffset(nextCaretPosition);
		if (!fromEnd)
		{
			return relativeOffset;
		}
		return label.Length - relativeOffset;
	}

	internal static int GetCaretOffsetAcrossTitleLink(string text, int caret, IDictionary<int, LinkInfo> links, bool isRight)
	{
		foreach (KeyValuePair<int, LinkInfo> link in links)
		{
			if (IsCurrentTitleLink(text, link.Key, link.Value))
			{
				int num = link.Key - 1;
				int num2 = link.Key + link.Value.Link.Length + link.Value.Url.Length + 3;
				int num3 = link.Key + GetTitleLinkBoundaryLength(link.Value.Link, fromEnd: false);
				int num4 = link.Key + link.Value.Link.Length - GetTitleLinkBoundaryLength(link.Value.Link, fromEnd: true);
				if (isRight && caret == num)
				{
					return num3;
				}
				if (isRight && caret == num4)
				{
					return num2;
				}
				if (!isRight && caret == num3)
				{
					return num;
				}
				if (!isRight && caret == num2)
				{
					return num4;
				}
			}
		}
		return caret;
	}

	internal static bool TryGetTitleLinkBoundaryDeletion(string text, int caret, IDictionary<int, LinkInfo> links, bool backDelete, out int offset, out int length)
	{
		offset = (length = 0);
		foreach (KeyValuePair<int, LinkInfo> link in links)
		{
			if (!IsCurrentTitleLink(text, link.Key, link.Value))
			{
				continue;
			}
			int num = link.Key - 1;
			int num2 = link.Key + link.Value.Link.Length + link.Value.Url.Length + 3;
			if (caret == (backDelete ? num2 : num))
			{
				length = GetTitleLinkBoundaryLength(link.Value.Link, backDelete);
				if (length == link.Value.Link.Length)
				{
					offset = num;
					length = num2 - num;
				}
				else
				{
					offset = (backDelete ? (link.Key + link.Value.Link.Length - length) : link.Key);
				}
				return true;
			}
		}
		return false;
	}

	private bool TryDeleteLinkName(bool backDelete)
	{
		if (base.SelectionLength == 0)
		{
			if (!TryGetTitleLinkBoundaryDeletion(base.Text, base.CaretOffset, LinkNameDict, backDelete, out var offset, out var length))
			{
				return false;
			}
			base.Document.Remove(offset, length);
			this.LinkTextChange?.Invoke(this, null);
			return true;
		}
		int caretOffset = base.CaretOffset;
		if (LinkUrlDict == null || !LinkUrlDict.Any())
		{
			return false;
		}
		int num = (backDelete ? base.Text.LastIndexOf("[", caretOffset, StringComparison.Ordinal) : base.Text.IndexOf(")", caretOffset, StringComparison.Ordinal));
		if (num < 0)
		{
			return false;
		}
		int num2 = (backDelete ? base.Text.IndexOf(")", num, StringComparison.Ordinal) : base.Text.LastIndexOf("[", num, StringComparison.Ordinal));
		string text = (backDelete ? base.Text.Substring(num, Math.Max(0, num2 - num + 1)) : ((num2 >= 0) ? base.Text.Substring(num2, Math.Max(0, num - num2 + 1)) : string.Empty));
		foreach (KeyValuePair<int, LinkInfo> item in LinkNameDict.ToList())
		{
			string text2 = "[" + item.Value.Link + "](" + item.Value.Url + ")";
			if (!(text == text2))
			{
				continue;
			}
			if (item.Value.Link.Length <= 1)
			{
				if (base.SelectedText.Length > 0 || num2 + (backDelete ? 1 : 0) != caretOffset)
				{
					return false;
				}
				base.Document.Remove(backDelete ? num : num2, Math.Abs(num - num2) + 1);
				this.LinkTextChange?.Invoke(this, null);
				return true;
			}
			if (num2 + (backDelete ? 1 : 0) != caretOffset)
			{
				if (base.SelectedText.Length > 0 && text2.Contains(base.SelectedText) && base.SelectedText.Contains("(" + item.Value.Url + ")"))
				{
					base.SelectionLength = base.SelectedText.Replace("(" + item.Value.Url + ")", "").Length - 1;
				}
				return false;
			}
			if (base.SelectedText.Length > 0)
			{
				return false;
			}
			base.Document.Remove(backDelete ? (caretOffset - (item.Value.Url.Length + 4)) : (caretOffset + 1), 1);
			this.LinkTextChange?.Invoke(this, null);
			return true;
		}
		return false;
	}

	private void OnFocused(object sender, RoutedEventArgs e)
	{
		_currentFocused = true;
		_parseLinkRandom = -1;
		this.TextGotFocus?.Invoke(sender, e);
	}

	private void OnLostFocus(object sender, RoutedEventArgs e)
	{
		TryPasteLink();
	}

	public bool TryPasteLink()
	{
		string text = base.Text;
		if (base.TextArea.IsKeyboardFocused)
		{
			return false;
		}
		_currentFocused = false;
		this.TextLostFocus?.Invoke(this, null);
		if (!base.IsMouseOver)
		{
			Select(Math.Min(base.CaretOffset, base.Text.Length), 0);
		}
		return false;
	}

	public void FocusText(int caret = -1)
	{
		Focus();
		if (caret >= 0 && caret < base.Text.Length)
		{
			base.CaretOffset = caret;
		}
	}

	public void FocusEnd()
	{
		Focus();
		base.CaretOffset = base.Text.Length;
	}

	public void FocusFirst()
	{
		Focus();
		base.CaretOffset = 0;
	}

	public void CorrectSpellingError(string correct, TextSegment errorSegment)
	{
		base.CaretOffset = errorSegment.StartOffset;
		base.Document.Replace(errorSegment, correct);
	}

	public void AddErrorToDict(string error, TextSegment errorSegment)
	{
		SpellCheckProvider?.Add(error);
		base.CaretOffset = errorSegment.StartOffset;
		SpellCheckProvider?.ForceRefresh();
	}

	private void OnMouseWheel(object sender, MouseWheelEventArgs e)
	{
		e.Handled = true;
	}

	public void SetCanParseDate(bool canParseDate)
	{
		if (CanParseDate == canParseDate)
		{
			return;
		}
		CanParseDate = canParseDate;
		List<string> list = _dateTokenGenerator?.GetValidTokens();
		if (list != null && list.Count > 0 && LocalSettings.Settings.RemoveTimeText)
		{
			string text = base.Text;
			text = list.Aggregate(text, (string current, string token) => current.Replace(token, string.Empty));
			SetText(text);
		}
		_dateTokenGenerator?.Reset();
		ParsedData = null;
		if (!canParseDate)
		{
			base.TextArea.TextView.Redraw();
		}
	}

	public void ClearParseDate()
	{
		if (CanParseDate)
		{
			CanParseDate = false;
			_dateTokenGenerator?.Reset();
			ParsedData = null;
			base.TextArea.TextView.Redraw();
		}
	}

	public async void ForceRender()
	{
		this.IgnoreTokenChanged?.Invoke(this, _dateTokenGenerator.GetIgnoreTokens());
		TryParseDate();
	}

	public double GetFontSize()
	{
		return base.TextArea.FontSize;
	}

	public string GetColor()
	{
		return ((Color)FindResource("ColorPrimary")/*cast due to .constrained prefix*/).ToString();
	}

	public void RemoveTokenText(ref string text)
	{
	}

	public string GetParsedText()
	{
		string text = base.Text.Replace("\r\n", string.Empty);
		if (CanParseDate && LocalSettings.Settings.RemoveTimeText)
		{
			List<string> validTokens = _dateTokenGenerator.GetValidTokens();
			if (validTokens.Any() && LocalSettings.Settings.RemoveTimeText)
			{
				text = validTokens.Aggregate(text, (string current, string token) => current.Replace(token, string.Empty));
			}
		}
		return text;
	}

	private void OnSelectionPopupOpened(object sender, EventArgs e)
	{
		_selectionPopupOpened = true;
		this.PopOpened?.Invoke(sender, null);
	}

	private void OnSelectionPopupClosed(object sender, EventArgs e)
	{
		_selectionPopupOpened = false;
		this.PopClosed?.Invoke(sender, null);
	}

	public void SetIgnoreToken(List<string> tokens)
	{
		_dateTokenGenerator?.SetIgnoreToken(tokens);
		TryParseDate();
	}

	private void InitPopup()
	{
		if (SelectionPopup == null)
		{
			SelectionPopup = new Popup
			{
				StaysOpen = false,
				AllowsTransparency = true,
				Placement = PlacementMode.Relative,
				PopupAnimation = PopupAnimation.Fade
			};
			SelectionPopup.PlacementTarget = this;
			SelectionPopup.Opened += OnSelectionPopupOpened;
			SelectionPopup.Closed += OnSelectionPopupClosed;
		}
	}

	private async void TryShowQuickSetPopup()
	{
		_projectImeRefresh.Cancel();
		if (!AllowQuickSet)
		{
			return;
		}
		string text = TryGetMark();
		if (string.IsNullOrEmpty(text))
		{
			_projectPopupState.ClearSuppressedTrigger();
			if (SelectionPopup != null)
			{
				SelectionPopup.IsOpen = false;
			}
			if (SetProjectPopup != null)
			{
				SetProjectPopup.IsOpen = false;
			}
			return;
		}
		if ((text == "^" || text == "~") && !_filterContent.EndsWith(" "))
		{
			if (base.TextArea.IsImeCompositionActive)
			{
				RefreshProjectPopupAfterComposition();
			}
			if (!_projectPopupState.IsTriggerSuppressed(_markIndex, text, _filterContent))
			{
				await ShowProjectPopup(text);
			}
			return;
		}
		_projectPopupState.ClearSuppressedTrigger();
		InitPopup();
		if (_selectionControl == null)
		{
			_selectionControl = new QuickSelectionControl(SelectionPopup);
			SelectionPopup.Child = _selectionControl;
			_selectionControl.ItemSelected += OnQuickItemSelect;
		}
		bool flag = IsUsedInCalendar();
		string calId = ((flag && (text == "^" || text == "~")) ? GetAccountId() : string.Empty);
		string projectId = (((!flag && text == "@") || text == "^" || text == "~") ? GetProjectId() : null);
		int priority = ((text == "!" || text == "！") ? GetPriority() : (-1));
		string assignee = ((text == "@") ? GetAssignee() : null);
		List<string> tags = ((text == "#" || text == "＃") ? GetTags() : null);
		bool flag2 = IsQuickSetNote();
		if (_selectionControl.SetSelectionItems(text, _filterContent, flag, calId, projectId, priority, tags, assignee, !flag2, withAttachment: false, ShowTemplateQuickSetItem(), flag2))
		{
			Point popupOffset = GetPopupOffset();
			_selectionControl.TryShowPopup(text, _filterContent, new Point(popupOffset.X + 10.0, popupOffset.Y - 12.0));
		}
		else
		{
			_selectionControl.ClosePopup();
		}
	}

	protected virtual bool IsQuickSetNote()
	{
		return false;
	}

	protected virtual bool ShowTemplateQuickSetItem()
	{
		return true;
	}

	private async void RefreshProjectPopupAfterComposition()
	{
		await _projectImeRefresh.RefreshAfterCompositionAsync(() => base.TextArea.IsImeCompositionActive, () => base.IsLoaded && base.TextArea.IsKeyboardFocused && AllowQuickSet, TryShowQuickSetPopup);
	}

	public void CloseQuickSetPopup()
	{
		_projectImeRefresh.Cancel();
		_selectionControl?.ClosePopup();
	}

	private async void OnQuickItemSelect(object sender, QuickSetModel e)
	{
		AllowQuickSet = false;
		RemoveQuickSetText(e);
		UpdateLayout();
		UtilLog.Info("AfterRemoveQuickSetText " + base.Text);
		this.QuickItemSelected?.Invoke(sender, e);
		AllowQuickSet = true;
	}

	private Point GetPopupOffset()
	{
		TextView textView = base.TextArea.TextView;
		TextViewPosition position = new TextViewPosition(base.Document.GetLocation(base.CaretOffset));
		return textView.GetVisualPosition(position, VisualYPosition.LineBottom);
	}

	protected virtual string GetProjectId()
	{
		return null;
	}

	protected virtual string GetColumnId()
	{
		return null;
	}

	protected virtual string GetAssignee()
	{
		return null;
	}

	protected virtual int GetPriority()
	{
		return -1;
	}

	protected virtual List<string> GetTags()
	{
		return null;
	}

	protected virtual string GetAccountId()
	{
		string text = QuickSetAccountIdProvider?.Invoke();
		string text2;
		if (string.IsNullOrEmpty(text))
		{
			text2 = QuickSetAccountId;
			if (text2 == null)
			{
				return "";
			}
		}
		else
		{
			text2 = text;
		}
		return text2;
	}

	protected virtual string GetCalendarId()
	{
		string text = QuickSetCalendarIdProvider?.Invoke();
		string text2;
		if (string.IsNullOrEmpty(text))
		{
			text2 = QuickSetCalendarId;
			if (text2 == null)
			{
				return "";
			}
		}
		else
		{
			text2 = text;
		}
		return text2;
	}

	protected virtual string GetCalendarSelectorAccountId()
	{
		if (QuickSetCalendarSelectorAccountIdProvider == null)
		{
			return GetAccountId();
		}
		return QuickSetCalendarSelectorAccountIdProvider() ?? string.Empty;
	}

	protected virtual bool IsUsedInCalendar()
	{
		return QuickSetUsedInCalendar;
	}

	public void RemoveQuickSetText(QuickSetModel e)
	{
		int num = Math.Min(base.Text.Length, Math.Max(0, _markIndex));
		base.Document.Remove(num, Math.Min(_filterContent.Length + 1, base.Text.Length - num));
		if (!string.IsNullOrEmpty(e.Tag) && LocalSettings.Settings.KeepTagsInText)
		{
			TagModel tagByName = CacheManager.GetTagByName(e.Tag.ToLower());
			if (tagByName != null)
			{
				base.Document.Insert(num, "#" + tagByName.GetDisplayName());
				num += tagByName.GetDisplayName().Length + 1;
			}
		}
		Focus();
		base.TextArea.Focus();
		base.CaretOffset = Math.Min(base.Text.Length, num);
	}

	private string TryGetMark()
	{
		if (string.IsNullOrEmpty(MarkRegexText))
		{
			_filterContent = string.Empty;
			_markIndex = -1;
			return string.Empty;
		}
		DocumentLine lineByOffset = base.Document.GetLineByOffset(base.CaretOffset);
		string text = string.Empty;
		if (lineByOffset != null)
		{
			if (lineByOffset.Offset < base.CaretOffset)
			{
				text = base.Text.Substring(lineByOffset.Offset, base.CaretOffset - lineByOffset.Offset);
			}
			if (!string.IsNullOrEmpty(text))
			{
				string[] array = text.Split(' ');
				if (array.Length != 0)
				{
					text = array.Last();
				}
				Match match = new Regex(MarkRegexText).Match(text);
				if (match.Success && match.Index == 0 && match.Groups[1].Value != "|")
				{
					int num = 0;
					for (int i = 0; i < array.Length - 1; i++)
					{
						num += array[i].Length + 1;
					}
					string value = match.Groups[1].Value;
					_filterContent = match.Groups[2].Value;
					_markIndex = lineByOffset.Offset + num;
					return value;
				}
			}
		}
		_filterContent = string.Empty;
		_markIndex = -1;
		return string.Empty;
	}

	public void SetMarkRegexText(string markRegexText)
	{
		MarkRegexText = markRegexText ?? string.Empty;
	}

	public void SetMarkRegexText(bool unable, bool inDetail = true, bool isNote = false)
	{
		if (unable)
		{
			MarkRegexText = "";
		}
		else if (inDetail)
		{
			MarkRegexText = (isNote ? "([#|@|~|^|\\*])(.*)" : "([#|@|~|^|!|！|\\*])(.*)");
		}
		else
		{
			MarkRegexText = "([/|、|#|@|~|^|!|！|\\*])(.*)";
		}
	}

	private bool IsQuickPopupOpen()
	{
		if (SelectionPopup != null)
		{
			return SelectionPopup.IsOpen;
		}
		return false;
	}

	private bool IsProjectPopupOpen()
	{
		if (SetProjectPopup != null && SetProjectPopup.IsOpen)
		{
			return !_projectPopupState.IsHidden;
		}
		return false;
	}

	public async Task ShowProjectPopup(string mark)
	{
		ProjectShowExtra extra = new ProjectShowExtra
		{
			TriggerMark = mark,
			FilterContent = _filterContent,
			Position = GetPopupOffset()
		};
		if (SetProjectPopup == null)
		{
			SetProjectPopup = new EscPopup
			{
				StaysOpen = false,
				NeedFocus = false,
				RestoreFocus = false,
				Placement = PlacementMode.Bottom,
				HorizontalOffset = -18.0,
				VerticalOffset = -5.0
			};
			SetProjectPopup.Opened += OnSelectionPopupOpened;
			SetProjectPopup.Closed += OnSelectionPopupClosed;
		}
		if (!SetProjectPopup.IsOpen)
		{
			_projectPopupState.Restore(SetProjectPopup);
			ShowSelectProjectPopup(enter: true, this, extra, ignoreOperation: true, fromCommand: true);
			await Task.Delay(10);
			if (_filterContent == extra.FilterContent)
			{
				_projectOrGroupPopup?.PerformSearch(extra.FilterContent);
			}
			await Task.Delay(100);
			if (!base.TextArea.IsImeCompositionActive)
			{
				Focus();
				Keyboard.Focus(base.TextArea);
			}
		}
		else
		{
			ProjectOrGroupPopup projectOrGroupPopup = _projectOrGroupPopup;
			if (projectOrGroupPopup != null && projectOrGroupPopup.PerformSearch(extra.FilterContent) > 0)
			{
				_projectPopupState.Restore(SetProjectPopup);
				IsInOperation = true;
				_projectOrGroupPopup.HoverSelectFirst(0);
			}
		}
		_selectionControl?.ClosePopup();
	}

	public virtual void ShowSelectProjectPopup(bool enter = false, UIElement target = null, ProjectShowExtra extra = null, bool ignoreOperation = false, bool fromCommand = false)
	{
		if (IsInOperation && !ignoreOperation)
		{
			return;
		}
		if (IsUsedInCalendar())
		{
			ShowSelectCalendarPopup(enter, target, extra);
			return;
		}
		string projectId = GetProjectId();
		_projectOrGroupPopup = new ProjectOrGroupPopup(SetProjectPopup, new ProjectExtra
		{
			ProjectIds = (string.IsNullOrEmpty(projectId) ? new List<string>() : new List<string> { projectId })
		}, new ProjectSelectorExtra
		{
			showAll = false,
			batchMode = false,
			canSelectGroup = false,
			onlyShowPermission = true,
			ShowColumn = true,
			ColumnId = GetColumnId(),
			IgnoreMiniHeight = true
		});
		_projectOrGroupPopup.NoSearchResult -= OnNoSearchResult;
		_projectOrGroupPopup.NoSearchResult += OnNoSearchResult;
		SetProjectPopup.PlacementTarget = target;
		if (extra != null)
		{
			SetProjectPopup.HorizontalOffset = extra.Position.X - 16.0;
			SetProjectPopup.VerticalOffset = extra.Position.Y - 32.0;
			if (!string.IsNullOrEmpty(extra.FilterContent) && _projectOrGroupPopup.PerformSearch(extra.FilterContent) <= 0)
			{
				return;
			}
		}
		else
		{
			SetProjectPopup.HorizontalOffset = -18.0;
			SetProjectPopup.VerticalOffset = -5.0;
		}
		_projectOrGroupPopup.ItemSelect += delegate(object sender, SelectableItemViewModel e)
		{
			OnProjectSelect(sender, e, fromCommand);
		};
		_projectOrGroupPopup.Closed -= NotifyProjectPopupClosed;
		_projectOrGroupPopup.Closed += NotifyProjectPopupClosed;
		_projectOrGroupPopup.Show();
		if (enter)
		{
			_projectOrGroupPopup.HoverSelectFirst();
		}
		DelayNotifyInOperation();
	}

	private void ShowSelectCalendarPopup(bool enter = false, UIElement target = null, ProjectShowExtra extra = null)
	{
		_projectOrGroupPopup = new ProjectOrGroupPopup(SetProjectPopup, new ProjectExtra
		{
			SubscribeCalendars = (string.IsNullOrEmpty(GetCalendarId()) ? new List<string>() : new List<string> { GetCalendarId() })
		}, new ProjectSelectorExtra
		{
			isCalendarMode = true,
			batchMode = false,
			accountId = GetCalendarSelectorAccountId()
		});
		SetProjectPopup.PlacementTarget = target;
		if (extra != null)
		{
			SetProjectPopup.HorizontalOffset = extra.Position.X - 16.0;
			SetProjectPopup.VerticalOffset = extra.Position.Y - 32.0;
			if (!string.IsNullOrEmpty(extra.FilterContent) && _projectOrGroupPopup.PerformSearch(extra.FilterContent) <= 0)
			{
				return;
			}
		}
		else
		{
			SetProjectPopup.HorizontalOffset = -18.0;
			SetProjectPopup.VerticalOffset = -5.0;
		}
		_projectOrGroupPopup.ItemSelect += OnCalendarSelect;
		_projectOrGroupPopup.Closed -= NotifyProjectPopupClosed;
		_projectOrGroupPopup.Closed += NotifyProjectPopupClosed;
		_projectOrGroupPopup.Show();
		if (enter)
		{
			_projectOrGroupPopup.HoverSelectFirst();
		}
		DelayNotifyInOperation();
	}

	private void OnCalendarSelect(object sender, SelectableItemViewModel e)
	{
		if (e is ticktick_WPF.Views.Misc.SubscribeCalendarViewModel subscribeCalendarViewModel)
		{
			IsInOperation = false;
			SetProjectPopup.IsOpen = false;
			AllowQuickSet = false;
			QuickSetModel e2 = new QuickSetModel
			{
				Type = QuickSetType.CalId,
				CalId = subscribeCalendarViewModel.Id,
				Title = subscribeCalendarViewModel.Title
			};
			RemoveQuickSetText(e2);
			UpdateLayout();
			UtilLog.Info("AfterRemoveQuickSetText " + base.Text);
			this.QuickItemSelected?.Invoke(this, e2);
			AllowQuickSet = true;
		}
	}

	public void OnProjectSelect(object sender, SelectableItemViewModel e, bool removeMark = false)
	{
		(string, string, string) projectAndColumnId = e.GetProjectAndColumnId();
		string projectId = projectAndColumnId.Item1;
		string item = projectAndColumnId.Item2;
		string item2 = projectAndColumnId.Item3;
		ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == projectId);
		if (projectModel != null)
		{
			OnProjectSelect(projectModel, item2, removeMark);
			AllowQuickSet = false;
			QuickSetModel e2 = new QuickSetModel
			{
				Type = QuickSetType.Project,
				Project = projectModel,
				ColumnId = item,
				ColumnName = item2
			};
			RemoveQuickSetText(e2);
			UpdateLayout();
			UtilLog.Info("AfterRemoveQuickSetText " + base.Text);
			this.QuickItemSelected?.Invoke(this, e2);
			AllowQuickSet = true;
		}
	}

	private async void OnProjectSelect(ProjectModel project, string columnName = null, bool removeMark = false)
	{
		IsInOperation = false;
		UserActCollectUtils.AddClickEvent("add_task", "action_btn", "list");
		SetProjectPopup.IsOpen = false;
	}

	private void NotifyProjectPopupClosed(object sender, EventArgs eventArgs)
	{
		IsInOperation = false;
	}

	private void OnNoSearchResult(object sender, EventArgs e)
	{
		if (base.TextArea.IsImeCompositionActive)
		{
			_projectPopupState.SuppressTrigger(_markIndex, base.Text, _filterContent);
			_projectPopupState.HideUntil(SetProjectPopup, () => base.TextArea.IsImeCompositionActive, delegate
			{
				IsInOperation = false;
			});
		}
		else
		{
			SetProjectPopup.IsOpen = false;
		}
	}

	private async void DelayNotifyInOperation()
	{
		IsInOperation = true;
		await Task.Delay(150);
		IsInOperation = true;
	}

	private async Task TryHideProjectPopup()
	{
		if (SetProjectPopup.IsOpen)
		{
			await Task.Delay(100);
			bool flag = _projectOrGroupPopup != null && _projectOrGroupPopup.ChildMouseOver();
			if (!SetProjectPopup.IsMouseOver && !flag)
			{
				_projectOrGroupPopup?.ClosePopup();
				SetProjectPopup.IsOpen = false;
			}
		}
	}
}
