using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.TextFormatting;
using System.Windows.Shapes;
using System.Windows.Threading;
using Emoji.Wpf;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Framework.Markdown;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Attachment;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Config;
using ticktick_WPF.Views.MainListView.DetailView;
using ticktick_WPF.Views.MarkDown.Colorizer;
using ticktick_WPF.Views.MarkDown.SpellCheck;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Project;
using ticktick_WPF.Views.QuickAdd;
using ticktick_WPF.Views.Search;

namespace ticktick_WPF.Views.MarkDown;

public class MarkDownEditor : System.Windows.Controls.UserControl, ILinkTextEditor, ILinkMenuHost, ISpellChecker, IEmojiRender, IComponentConnector
{
	private sealed class PersistedMarkdownLinkMatch
	{
		public int Index { get; }

		public int Length { get; }

		public int TextIndex { get; }

		public int TextLength { get; }

		public int UrlIndex { get; }

		public int UrlLength { get; }

		public string Text { get; }

		public string Url { get; }

		public PersistedMarkdownLinkMatch(int index, int length, int textIndex, int textLength, int urlIndex, int urlLength, string text, string url)
		{
			Index = index;
			Length = length;
			TextIndex = textIndex;
			TextLength = textLength;
			UrlIndex = urlIndex;
			UrlLength = urlLength;
			Text = text;
			Url = url;
		}
	}

	private sealed class PendingPlainParagraphInput
	{
		public string DisplayTextBeforeInput { get; set; }

		public string PersistedTextBeforeInput { get; set; }

		public int[] DisplayToPersistedOffsets { get; set; }

		public int StartOffset { get; set; }

		public string InsertedText { get; set; }

		public int LineIndex { get; set; }

		public List<SemanticBlockPersistenceStamp> SemanticBlocks { get; set; }
	}

	private sealed class IncrementalPersistenceState
	{
		public string DisplayText { get; set; }

		public string PersistedText { get; set; }

		public int[] DisplayToPersistedOffsets { get; set; }

		public List<SemanticBlockPersistenceStamp> SemanticBlocks { get; set; }
	}

	private sealed class SemanticBlockPersistenceStamp
	{
		public MarkdownBlockKind Kind { get; set; }

		public int StartLine { get; set; }

		public int EndLine { get; set; }

		public int Indent { get; set; }

		public int? Level { get; set; }

		public MarkdownListKind? ListKind { get; set; }

		public bool? Checked { get; set; }

		public bool? Folded { get; set; }

		public bool? SoftTab { get; set; }

		public string CustomOrder { get; set; }

		public string Language { get; set; }

		public string CodeText { get; set; }

		public string CodeBlockAttachmentIdentity { get; set; }

		public MarkdownFileBlockKind? FileKind { get; set; }

		public string Source { get; set; }

		public string AltText { get; set; }

		public string Caption { get; set; }
	}

	private sealed class CodeBlockAttachmentPersistenceExpansion
	{
		public string DisplayText { get; set; }

		public MarkdownSemanticDocument SemanticDocument { get; set; }

		public IReadOnlyDictionary<int, MarkdownBlockNode> SemanticBlocksByLine { get; set; }

		public int[] SourceToExpandedOffsetMap { get; set; }
	}

	private sealed class PersistedInlineMark
	{
		public int SourcePosition { get; set; }

		public int SourceLength { get; set; }

		public string Identity { get; set; }

		public int Order { get; set; }

		public string OpeningDelimiter { get; set; }

		public string ClosingDelimiter { get; set; }
	}

	private sealed class PreparedEditorTextState
	{
		public string DisplayText { get; set; }

		public string MetadataSourceText { get; set; }

		public string MetadataDisplayText { get; set; }

		public List<MarkdownInlineDisplaySegment> SemanticMarks { get; } = new List<MarkdownInlineDisplaySegment>();

		public MarkdownSemanticDocument SemanticDocument { get; set; }

		public int[] RawToDisplayOffsetMap { get; set; }
	}

	private sealed class EditorHistoryState
	{
		public string PersistedText { get; set; }

		public string DisplayText { get; set; }

		public MarkdownSemanticDocument SemanticDocument { get; set; }

		public List<MarkdownInlineDisplaySegment> TypedSemanticMarks { get; } = new List<MarkdownInlineDisplaySegment>();

		public int CaretOffset { get; set; }
	}

	private sealed class InlineDisplayRange
	{
		public int Start { get; set; }

		public int End { get; set; }

		public int ContentStart { get; set; }

		public int ContentEnd { get; set; }

		public List<MarkdownMark> Marks { get; } = new List<MarkdownMark>();
	}

	private sealed class OffsetRange
	{
		public int Start { get; set; }

		public int End { get; set; }
	}

	private sealed class CodeBlockAttachmentDisplayTransform
	{
		public string DisplayText { get; set; }

		public MarkdownSemanticDocument SemanticDocument { get; set; }

		public int[] SourceToDisplayOffsetMap { get; set; }
	}

	private sealed class DisplayLineSpan
	{
		public int Start { get; set; }

		public int ContentEnd { get; set; }

		public int End { get; set; }
	}

	private class TextRegion
	{
		public int Start { get; set; }

		public int End { get; set; }
	}

	private sealed class InternalSelectionDragState
	{
		public int SelectionStart { get; set; }

		public int SelectionLength { get; set; }

		public string DisplayText { get; set; }

		public string PersistedText { get; set; }

		public int PersistedSelectionStart { get; set; }

		public int PersistedSelectionEnd { get; set; }

		public bool DragStarted { get; set; }
	}

	private sealed class EditorScrollSnapshot
	{
		public Vector EditorScrollOffset { get; set; }

		public ScrollViewer ParentScrollViewer { get; set; }

		public double ParentHorizontalOffset { get; set; }

		public double ParentVerticalOffset { get; set; }
	}

	private sealed class HeadingFoldExpansionScrollAnchor
	{
		public int LineIndex { get; set; }

		public ScrollViewer ParentScrollViewer { get; set; }

		public double ViewportTop { get; set; }
	}

	private sealed class SelectedMarkdownClipboardPayload
	{
		public string InternalMarkdown { get; set; } = string.Empty;

		public string ExternalMarkdown { get; set; } = string.Empty;

		public string PlainText { get; set; } = string.Empty;
	}

	private sealed class ElementGeneratorRegistration
	{
		private readonly Func<MarkDownEditor, VisualLineElementGenerator> _create;

		public string Name { get; }

		public ElementGeneratorRegistration(string name, Func<MarkDownEditor, VisualLineElementGenerator> create)
		{
			Name = name;
			_create = create;
		}

		public VisualLineElementGenerator Create(MarkDownEditor editor)
		{
			return _create(editor);
		}
	}

	private sealed class AttachmentDeletionCaretUndoAction : IUndoableOperation
	{
		private readonly WeakReference<TextEditor> editor;

		private readonly int offset;

		public AttachmentDeletionCaretUndoAction(TextEditor editor, int offset)
		{
			this.editor = new WeakReference<TextEditor>(editor);
			this.offset = offset;
		}

		public void Undo()
		{
		}

		public void Redo()
		{
			if (editor.TryGetTarget(out var target))
			{
				target.Select(Math.Min(offset, target.Document.TextLength), 0);
			}
		}
	}

	private struct DisplaySelectionSegment
	{
		public readonly int Start;

		public readonly int End;

		public DisplaySelectionSegment(int start, int end)
		{
			Start = start;
			End = end;
		}
	}

	private sealed class LinePrefixOperation
	{
		public int RemoveOffset { get; }

		public int RemoveLength { get; }

		public string InsertText { get; }

		public LinePrefixOperation(int removeLength, string insertText, int removeOffset = 0)
		{
			RemoveLength = removeLength;
			InsertText = insertText ?? string.Empty;
			RemoveOffset = Math.Max(0, removeOffset);
		}
	}

	private sealed class LineMutation
	{
		public int LineOffset { get; set; }

		public int StartOffset { get; set; }

		public int RemoveLength { get; set; }

		public string InsertText { get; set; }
	}

	private sealed class DocumentMutation
	{
		public int StartOffset { get; set; }

		public int RemoveLength { get; set; }

		public string InsertText { get; set; }
	}

	private struct ActiveRange
	{
		public int Start { get; }

		public int End { get; }

		public ActiveRange(int start, int end)
		{
			Start = start;
			End = end;
		}
	}

	private static readonly Regex FutureLinkRegex = new Regex("\\[(.*)\\]\\((.*)\\)");

	private static readonly Regex WrappedAttachmentLinkRegex = new Regex("!\\[\\[(?<kind>image|file)\\]\\((?<inner>[^)\\r\\n]+)\\)\\]\\((?<outer>[^)\\r\\n]+)\\)", RegexOptions.IgnoreCase);

	private static readonly Regex BlockMetadataCommentRegex = new Regex("<!--\\s*(?<json>\\{.*?\\})\\s*-->", RegexOptions.Singleline);

	private static readonly Regex EditingOrderedListPattern = new Regex("^[ \\t]*[ ]{0,3}(\\d+)\\. ", RegexOptions.Compiled);

	private static readonly Regex EditingBlockQuotePattern = new Regex("^[ \\t]*(?:[ ]{0,3}>) ", RegexOptions.Compiled);

	public static readonly Regex AttachmentRegex = new Regex("!\\[((?:\\\\.|[^\\]\\r\\n])*)\\]\\((?<url>(?>(?:\\\\.|[^()\\\\\\r\\n]+|(?<depth>\\()|(?<-depth>\\)))*))(?(depth)(?!))\\)");

	public static readonly DependencyProperty ReadOnlyProperty = DependencyProperty.Register("ReadOnly", typeof(bool), typeof(MarkDownEditor), new PropertyMetadata(false, OnReadOnlyChanged));

	public static readonly DependencyProperty EnableSpellCheckProperty = DependencyProperty.Register("EnableSpellCheck", typeof(bool), typeof(MarkDownEditor), new PropertyMetadata(true, OnSpellCheckChanged));

	public static readonly DependencyProperty ImageModeProperty = DependencyProperty.Register("ImageMode", typeof(int), typeof(MarkDownEditor), new PropertyMetadata(0, OnImageModeChanged));

	public static readonly DependencyProperty IsDarkProperty = DependencyProperty.Register("IsDark", typeof(bool), typeof(MarkDownEditor), new PropertyMetadata(false, OnIsDarkChanged));

	public static readonly DependencyProperty StyleEnabledProperty = DependencyProperty.Register("StyleEnabled", typeof(bool), typeof(MarkDownEditor), new PropertyMetadata(true));

	public static readonly DependencyProperty EditorEnabledProperty = DependencyProperty.Register("EditorEnabled", typeof(bool), typeof(MarkDownEditor), new PropertyMetadata(true));

	public static readonly DependencyProperty LineSpacingProperty = DependencyProperty.Register("LineSpacing", typeof(double), typeof(MarkDownEditor), new PropertyMetadata(6.0));

	public static readonly DependencyProperty BindingTextProperty = DependencyProperty.Register("BindingText", typeof(string), typeof(MarkDownEditor), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnBindingTextChanged));

	private HighlightColorizer _colorizer;

	private readonly DelayActionHandler _textChangeNotifyHandler = new DelayActionHandler(100);

	private readonly DelayActionHandler _redrawNotifyHandler = new DelayActionHandler(200);

	private readonly DelayActionHandler _textChangeHandler = new DelayActionHandler(150);

	private const int LargeDocumentTextLengthThreshold = 4096;

	private const int LargeDocumentRefreshIntervalMilliseconds = 250;

	private const int IncrementalParagraphReconciliationIntervalMilliseconds = 350;

	private bool _requiresImmediateDerivedMetadataRefresh;

	public readonly Dictionary<string, List<Point>> CodeDict = new Dictionary<string, List<Point>>();

	public readonly Dictionary<int, LinkInfo> LinkNameDict = new Dictionary<int, LinkInfo>();

	public readonly Dictionary<int, LinkInfo> LinkUrlDict = new Dictionary<int, LinkInfo>();

	public readonly List<MarkdownInlineDisplaySegment> SemanticMarkList = new List<MarkdownInlineDisplaySegment>();

	public readonly Dictionary<int, AttachmentInfo> AttachmentDict = new Dictionary<int, AttachmentInfo>();

	public readonly HashSet<int> AttachmentEndSet = new HashSet<int>();

	public Dictionary<int, string> EmojiDict = new Dictionary<int, string>();

	private BlockBackgroundRenderer _backgroundRenderer;

	private FoldingManager _headingFoldingManager;

	private int _headingFoldSectionSyncSuppressionVersion;

	private DispatcherTimer _headingFoldSectionSyncSuppressionTimer;

	private bool _allowHeadingFoldSectionSyncDuringSuppressionOnce;

	private readonly Dictionary<MarkdownBlockNode, bool?> _searchHeadingDisplayFoldedPreviousValues = new Dictionary<MarkdownBlockNode, bool?>();

	private readonly HashSet<MarkdownBlockNode> _searchExpansionSuppressedHeadings = new HashSet<MarkdownBlockNode>();

	private string _searchHeadingExpansionKey;

	private readonly Dictionary<string, IVisualLineTransformer> _codeColorizers = new Dictionary<string, IVisualLineTransformer>(StringComparer.CurrentCultureIgnoreCase);

	private bool _currentFocused;

	private bool _hasPendingLocalDocumentChanges;

	private volatile string _scheduledTextChangeTaskId;

	private int _lastInsertOffset;

	private int _lastInsertSelectionLength;

	private int _lastInsertActiveOffset;

	private const bool AllowEditingMarkdownParsing = false;

	private const bool AllowInlineShortcutConsumption = true;

	private InsertLinkWindow _linkWindow;

	private bool _previewCaretChangedRedrew;

	private string _pasteContent;

	private int _previousLineCount;

	private int _caretBeforeMoveUp;

	private bool _suppressBindingTextToEditorSync;

	private const int HeadingFoldSectionSyncSuppressionMilliseconds = 1500;

	private bool _suppressEditBoxTextChanged;

	private bool _suppressExternalTextChanged;

	private bool _deferBindingTextSyncToSemanticNotification;

	private bool _suppressSemanticSelectionNormalization;

	private bool _suppressParentCaretForCodeBlockAttachment;

	private bool _suppressParentCaretForCodeBlockObjectSelection;

	private HashSet<string> _objectSelectedCodeBlockKeys = new HashSet<string>();

	private CodeBlockSelectionLayer _codeBlockSelectionLayer;

	private CodeBlockAttachmentElementGenerator _codeBlockAttachmentElementGenerator;

	private ImageElementGenerator _imageElementGenerator;

	private bool _codeBlockAttachmentPointerActionPending;

	private bool _codeBlockAttachmentPointerActionClearScheduled;

	private int _codeBlockAttachmentPointerActionMouseDownTimestamp = -1;

	private const double CodeBlockLanguageControlWidth = 112.0;

	private const double CodeBlockLanguageBadgeFontSize = 13.0;

	private const double CodeBlockLanguageBadgeHeight = 20.0;

	private const string CodeBlockLanguageBadgeBrushKey = "BaseColorOpacity60";

	private const double CodeBlockLanguageEditorHeight = 20.0;

	private const double CodeBlockLanguageEditorBackgroundOpacity = 0.3;

	private const string CodeBlockLanguageDropdownIconBrushKey = "BaseColorOpacity40";

	private const double CodeBlockCopyButtonMinWidth = 58.0;

	private const double CodeBlockCopyButtonHeight = 20.0;

	private const double CodeBlockCopyButtonFontSize = 13.0;

	private const double CodeBlockCopyIconSize = 16.0;

	private const double CodeBlockCopyIconTextSpacing = 4.0;

	private const string CodeBlockCopyButtonBrushKey = "BaseColorOpacity40";

	private readonly List<Border> _codeBlockLanguageBadges = new List<Border>();

	private readonly List<Border> _codeBlockCopyButtons = new List<Border>();

	private CodeBlockLanguageEditorControl _codeBlockLanguageEditorControl;

	private Window _codeBlockLanguageDropdownOwnerWindow;

	private MarkdownBlockNode _codeBlockLanguageOverlayBlock;

	private MarkdownBlockNode _editingCodeBlockLanguageBlock;

	private MarkdownBlockNode _codeBlockLanguageFocusRestoreBlock;

	private bool _restoreCodeBlockEditorFocusAfterLanguageEdit;

	private bool _closingCodeBlockLanguageEditor;

	private bool _codeBlockLanguageEditorOpening;

	private bool _suppressCodeBlockLanguageDropdownRefresh;

	private readonly HashSet<MarkdownMarkKind> _activeTypingMarks = new HashSet<MarkdownMarkKind>();

	private readonly Dictionary<MarkdownMarkKind, int> _activeTypingMarkOffsets = new Dictionary<MarkdownMarkKind, int>();

	private readonly Dictionary<MarkdownMarkKind, int> _suppressedTypingMarks = new Dictionary<MarkdownMarkKind, int>();

	private string _activeTypingHighlightColor;

	private int _activeTypingHighlightOffset = -1;

	private int _trackedRawMarkdownLinkUrlStart = -1;

	private int _trackedRawMarkdownLinkUrlEnd = -1;

	private int _trackedRawMarkdownLinkTextLength = -1;

	private readonly List<MarkdownInlineDisplaySegment> _backspaceRetainedTypingMarks = new List<MarkdownInlineDisplaySegment>();

	private int _backspaceRetainedTypingMarkOffset = -1;

	private readonly List<MarkdownInlineDisplaySegment> _typedSemanticMarks = new List<MarkdownInlineDisplaySegment>();

	private bool _needsTypedSemanticMarkRedraw;

	private bool _suppressSemanticHistory;

	private readonly List<EditorHistoryState> _semanticUndoHistory = new List<EditorHistoryState>();

	private readonly List<EditorHistoryState> _semanticRedoHistory = new List<EditorHistoryState>();

	private const string RawUncheckedTaskListMarker = "- [ ] ";

	private const string RawCheckedTaskListMarker = "- [x] ";

	private const string RawCheckedTaskListUpperMarker = "- [X] ";

	private const string DisplayUncheckedTaskListMarker = "\u200b\u200c\u200d \u200e\u200f";

	private const string DisplayCheckedTaskListMarker = "\u200b\u200c\u200d\u2060\u200e\u200f";

	private const string RawBlockQuoteMarker = "> ";

	private const string DisplayBlockQuoteMarker = "\u2061\u2062";

	private const string RawBulletDashMarker = "- ";

	private const string RawBulletAsteriskMarker = "* ";

	private const string RawBulletPlusMarker = "+ ";

	private const string DisplayBulletDashMarker = "\u2063\u2064";

	private const string DisplayBulletAsteriskMarker = "\u2064\u2063";

	private const string DisplayBulletPlusMarker = "\u2061\u2064";

	private const char DisplayOrderedListDot = '\u206a';

	private const char DisplayOrderedListSpace = ' ';

	private const char DisplayHeadingMarker = '\u200b';

	private const char DisplayHeadingSpace = ' ';

	private const string RawHorizontalRuleMarker = "---";

	private const string DisplayHorizontalRuleMarker = "\u200c\u200d\u2060";

	private const string RawCodeFenceMarker = "```";

	private const string DisplayParagraphIndentMarker = "\u2065";

	private const string DisplayCaretAnchor = "\u00a0";

	private const string InternalSelectionDragDataFormat = "TickTick.MarkDownEditor.InternalSelectionDrag";

	private Brush _highLightColor;

	private Brush _bracketColor;

	private Brush _textColor;

	private SearchHighlightBackgroundRender _searchRender;

	private bool _inPrint;

	private string _previewText;

	private string _editingTaskId;

	private bool _inImeProcess;

	private bool? _isDark;

	private string _editorForegroundResourceKey;

	private bool _placeholderOpacitySuppressionActive;

	private double _placeholderOpacityBeforeSuppression = 1.0;

	private DispatcherOperation _placeholderOpacityRestoreOperation;

	private MarkdownSemanticDocument _seedSemanticDocumentForRefresh;

	private bool _suppressNextListAdjust;

	private PendingPlainParagraphInput _pendingPlainParagraphInput;

	private IncrementalPersistenceState _incrementalPersistenceState;

	private string _color100 = "BaseColorOpacity100_80";

	private string _color20 = "BaseColorOpacity20";

	private System.Windows.Controls.ToolTip _orderEditTooltip;

	private const int MarginWidth = 24;

	internal const double EmbeddedContentRightPadding = 7.0;

	internal const char CodeBlockAttachmentPlaceholderChar = '\ue000';

	internal const double CodeBlockOutsideRightHitWidth = 48.0;

	private string _lastText = string.Empty;

	private MarkdownSemanticDocument _semanticDocument;

	private Dictionary<int, MarkdownBlockNode> _semanticBlocksByLine = new Dictionary<int, MarkdownBlockNode>();

	private bool _leftPressed;

	private int? _semanticSelectionAnchorOffset;

	private bool _semanticSelectionDragHandled;

	private bool _semanticSelectionMouseUpRestoreScheduled;

	private int? _semanticSelectionLastActiveOffset;

	private int? _semanticSelectionLastStartOffset;

	private int? _semanticSelectionLastEndOffset;

	private bool _pendingFinalHeadingFoldExpansionAfterMouseUp;

	private bool _suppressNextEditorMouseUpAfterHeadingFoldToggle;

	private int? _handledHeadingFoldMouseUpTimestamp;

	private bool _suppressCaretVerticalOffsetNotification;

	private bool _suppressCaretVerticalOffsetNotificationForCurrentMouseGesture;

	private bool _internalSelectionDropCaretRestorePending;

	private bool _codeBlockParagraphExitCaretPlacementPending;

	private bool _caretVerticalOffsetNotificationAfterMouseUpScheduled;

	private const double SelectionEdgeAutoScrollZone = 48.0;

	private const double SelectionEdgeAutoScrollMinimumStep = 1.0;

	private const double SelectionEdgeAutoScrollMaximumStep = 14.0;

	private const double SelectionViewportBottomHitTestInset = 0.0001;

	private DispatcherTimer _selectionEdgeAutoScrollTimer;

	private ScrollViewer _selectionEdgeAutoScrollViewer;

	private bool _selectionEdgeSelectionUpdatePending;

	private int? _selectionViewportClampMouseMoveTimestamp;

	private int? _selectionViewportClampMouseUpTimestamp;

	private int? _wholeSelectionAnchorStartOffset;

	private int? _wholeSelectionAnchorEndOffset;

	private readonly string _internalSelectionDragOwnerId = Guid.NewGuid().ToString("N");

	private InternalSelectionDragState _internalSelectionDragState;

	private HeadingFoldExpansionScrollAnchor _pendingHeadingFoldExpansionScrollAnchor;

	private const string InternalMarkdownClipboardFormat = "TickTickMarkdown";

	private const string PlainTextClipboardFormat = "TickTickPlainText";

	private static readonly Regex ExternalTaskListMarkerRegex = new Regex("^([ \\t]*(?:>[ \\t]*)*)[-*+] \\[[ xX]\\] ", RegexOptions.Compiled);

	private static readonly Regex ColoredHighlightOpeningDelimiterRegex = new Regex("(?<!\\\\)==\\{#(?:[A-Fa-f0-9]{6}|[A-Fa-f0-9]{8})\\}", RegexOptions.Compiled);

	private int? _lastCaretPreviewRedrawLineNumber;

	private static readonly ElementGeneratorRegistration[] ElementGeneratorRegistrations = new ElementGeneratorRegistration[19]
	{
		new ElementGeneratorRegistration("HeadingElementGenerator", (MarkDownEditor editor) => new HeadingElementGenerator(editor)),
		new ElementGeneratorRegistration("InlineCodeLineHeightElementGenerator", (MarkDownEditor editor) => new InlineCodeLineHeightElementGenerator(editor)),
		new ElementGeneratorRegistration("CheckElementGenerator", (MarkDownEditor editor) => new CheckElementGenerator(editor)),
		new ElementGeneratorRegistration("UnCheckElementGenerator", (MarkDownEditor editor) => new UnCheckElementGenerator(editor)),
		new ElementGeneratorRegistration("ParagraphIndentElementGenerator", (MarkDownEditor editor) => new ParagraphIndentElementGenerator(editor)),
		new ElementGeneratorRegistration("ListElementGenerator", (MarkDownEditor editor) => new ListElementGenerator(editor)),
		new ElementGeneratorRegistration("NumberedListElementGenerator", (MarkDownEditor editor) => new NumberedListElementGenerator(editor)),
		new ElementGeneratorRegistration("QuoteElementGenerator", (MarkDownEditor editor) => new QuoteElementGenerator(editor)),
		new ElementGeneratorRegistration("SplitLineElementGenerator", (MarkDownEditor editor) => new SplitLineElementGenerator(editor, editor.EditBox)),
		new ElementGeneratorRegistration("CodeBlockAttachmentElementGenerator", (MarkDownEditor editor) => new CodeBlockAttachmentElementGenerator(editor)),
		new ElementGeneratorRegistration("CodeBlockFenceElementGenerator", (MarkDownEditor editor) => new CodeBlockFenceElementGenerator(editor)),
		new ElementGeneratorRegistration("CodeBlockContentElementGenerator", (MarkDownEditor editor) => new CodeBlockContentElementGenerator(editor)),
		new ElementGeneratorRegistration("CustomLinkElementGenerator", (MarkDownEditor editor) => new CustomLinkElementGenerator(editor)),
		new ElementGeneratorRegistration("LinkNameGenerator", (MarkDownEditor editor) => new LinkNameGenerator(editor)),
		new ElementGeneratorRegistration("AutoHideSymbolGenerator", (MarkDownEditor editor) => new AutoHideSymbolGenerator(editor, isLeft: true)),
		new ElementGeneratorRegistration("AutoHideSymbolGenerator", (MarkDownEditor editor) => new AutoHideSymbolGenerator(editor, isLeft: false)),
		new ElementGeneratorRegistration("ImageElementGenerator", (MarkDownEditor editor) => new ImageElementGenerator(editor)),
		new ElementGeneratorRegistration("EmojiGenerator", (MarkDownEditor editor) => new EmojiGenerator(editor)),
		new ElementGeneratorRegistration("FixedTabElementGenerator", (MarkDownEditor editor) => new FixedTabElementGenerator())
	};

	private HashSet<int> _pendingOrderedListRedrawLineIndices;

	private bool _pendingOrderedListRedrawScheduled;

	private OrderExtra _editingOrderExtra;

	private const string PastedUrlMarkdownCharacters = "!*~[]`_={}()";

	private bool _retainSelectionForToolbarInteraction;

	private string _filterContent;

	private QuickSelectionControl _selectionControl;

	private int _markIndex;

	private readonly QuickSetProjectPopupState _projectPopupState = new QuickSetProjectPopupState();

	private readonly ImeCompositionRefresh _projectImeRefresh = new ImeCompositionRefresh();

	public Popup SelectionPopup;

	private Popup _customEditorPopup;

	private Window _customEditorPopupOwnerWindow;

	private Popup _styleMenuPopup;

	private Popup _editOrderPopup;

	private Popup _linkEditorPopup;

	private Window _linkEditorPopupOwnerWindow;

	public bool InImmersiveMode;

	private int _linkEditorPopupShowVersion;

	private bool _hasLinkEditorPopupPlacementTargetScreenPosition;

	private Point _linkEditorPopupPlacementTargetScreenPosition;

	private bool _customEditorMenuIsOpen;

	private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

	private ProjectOrGroupPopup _projectOrGroupPopup;

	internal MarkDownEditor Root;

	internal TextEditor EditBox;

	internal Canvas CodeBlockLanguageOverlay;

	private bool _contentLoaded;

	public bool EditorEnabled
	{
		get
		{
			return (bool)GetValue(EditorEnabledProperty);
		}
		set
		{
			SetValue(EditorEnabledProperty, value);
		}
	}

	public bool StyleEnabled
	{
		get
		{
			return (bool)GetValue(StyleEnabledProperty);
		}
		set
		{
			SetValue(StyleEnabledProperty, value);
			SetupMargin();
		}
	}

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

	public string BindingText
	{
		get
		{
			return (string)GetValue(BindingTextProperty);
		}
		set
		{
			SetValue(BindingTextProperty, value);
		}
	}

	public Constants.DetailMode DetailMode { get; set; }

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

	public bool ReadOnly
	{
		get
		{
			return (bool)GetValue(ReadOnlyProperty);
		}
		set
		{
			SetValue(ReadOnlyProperty, value);
		}
	}

	public int ImageMode
	{
		get
		{
			return (int)GetValue(ImageModeProperty);
		}
		set
		{
			SetCurrentValue(ImageModeProperty, value);
			EditBox.TextArea.TextView.Redraw();
		}
	}

	public bool AllowTab { get; set; } = true;

	public int MaxLength { get; set; } = -1;

	public bool EscapePlainInlineSyntaxOnPersistence { get; set; }

	public SpellCheckProvider SpellCheckProvider { get; set; }

	public bool InPrint => _inPrint;

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

	public bool KeyboardFocused => EditBox.IsKeyboardFocusWithin;

	public bool IsApplyingPreparedState => _suppressExternalTextChanged;

	public bool HasPendingLocalDocumentChanges => _hasPendingLocalDocumentChanges;

	public string Text
	{
		get
		{
			return EditBox.Text;
		}
		set
		{
			HideLinkMenuPopup();
			string text = value ?? string.Empty;
			string text2 = MarkdownSemanticConverter.RepairMalformedHeadingLinks(NormalizeWrappedAttachmentLinkSyntax(text));
			PreparedEditorTextState preparedEditorTextState = PrepareEditorTextState(text2);
			if (EditBox.Text == preparedEditorTextState.DisplayText && string.Equals(GetPersistedText(), text2, StringComparison.Ordinal))
			{
				_imageElementGenerator?.DiagnosticTaskContentLoaded();
				if (!string.Equals(text, text2, StringComparison.Ordinal))
				{
					UpdateBindingTextFromEditor(text2);
				}
				ClearPendingLocalDocumentChanges();
			}
			else
			{
				ApplyPreparedEditorState(preparedEditorTextState, text2, restoreOffset: false, resetHistory: true);
				ClearPendingLocalDocumentChanges();
			}
		}
	}

	internal double SelectionViewportBottomInset { get; set; }

	private HashSet<int> PendingOrderedListRedrawLineIndices => _pendingOrderedListRedrawLineIndices ?? (_pendingOrderedListRedrawLineIndices = new HashSet<int>());

	internal bool IsSearchRenderInDetail => _searchRender?.InDetail ?? false;

	public EscPopup SetProjectPopup { get; set; }

	public EditorMenuEx EditorMenu { get; } = new EditorMenuEx();

	public EditorMenuLink EditorMenuLink { get; } = new EditorMenuLink();

	public StyleMenu StyleMenu { get; } = new StyleMenu(Visibility.Visible);

	private LinkLineInfoModel _linkLineInfo { get; set; }

	public bool DisableLinkEditorMenu { get; set; }

	public bool QuickSetUsedInCalendar { get; set; }

	public string QuickSetAccountId { get; set; }

	public Func<string> QuickSetAccountIdProvider { get; set; }

	public string QuickSetCalendarId { get; set; }

	public Func<string> QuickSetCalendarIdProvider { get; set; }

	public Func<string> QuickSetCalendarSelectorAccountIdProvider { get; set; }

	private string MarkRegexText { get; set; } = "([/|、|#|@|~|^|!|！|\\*])(.*)";

	public bool IsInOperation { get; set; }

	public event EventHandler<ProjectTask> Navigate;

	public event EventHandler<double> CaretVerticalOffsetChanged;

	public event EventHandler QuickPopupOpened;

	public event EventHandler QuickPopupClosed;

	public event EventHandler EditorStateChanged;

	public new event EventHandler<System.Windows.Input.KeyEventArgs> KeyUp;

	public new event EventHandler<System.Windows.Input.KeyEventArgs> KeyDown;

	public event EventHandler MoveUp;

	public event EventHandler MoveDown;

	public event EventHandler EscKeyUp;

	public event EventHandler LinkPopupOpened;

	public event EventHandler LinkPopupClosed;

	public event EventHandler SaveContent;

	public event EventHandler EnterImmersive;

	public event EventHandler SelectDate;

	public event EventHandler TextChanged;

	public event EventHandler<string> TaskIdChanged;

	public event EventHandler<QuickSetModel> QuickItemSelected;

	public event EventHandler<bool> PopupOpenChanged;

	public static bool IsUnescapedAttachmentMatch(string text, Match match)
	{
		if (string.IsNullOrEmpty(text) || match == null || !match.Success)
		{
			return false;
		}
		int num = 0;
		int num2 = match.Index - 1;
		while (num2 >= 0 && text[num2] == '\\')
		{
			num++;
			num2--;
		}
		return num % 2 == 0;
	}

	public static bool IsInternalAttachmentMatch(string text, Match match)
	{
		if (!IsUnescapedAttachmentMatch(text, match) || match.Groups.Count < 3)
		{
			return false;
		}
		string value = match.Groups[1].Value;
		string value2 = match.Groups[2].Value;
		if (!string.IsNullOrWhiteSpace(value2) && (string.Equals(value, "image", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "file", StringComparison.OrdinalIgnoreCase)))
		{
			return !Uri.IsWellFormedUriString(value2, UriKind.Absolute);
		}
		return false;
	}

	public static IReadOnlyCollection<string> GetReferencedInternalAttachmentIds(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return (IReadOnlyCollection<string>)(object)Array.Empty<string>();
		}
		string text2 = NormalizeLegacyPersistedTextForParsing(text);
		var source = (from block in MarkdownSemanticConverter.Parse(text2).Blocks
			where block != null && block.Kind == MarkdownBlockKind.CodeBlock
			select new { block.StartLine, block.EndLine }).ToList();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (Match item in AttachmentRegex.Matches(text2))
		{
			if (!IsInternalAttachmentMatch(text2, item) || IsMatchInsideInlineCode(text2, item))
			{
				continue;
			}
			int lineIndex = GetLineIndex(text2, item.Index);
			if (!source.Any(range => lineIndex >= range.StartLine && lineIndex <= range.EndLine))
			{
				string internalAttachmentId = AttachmentDao.GetInternalAttachmentId(item.Groups[2].Value);
				if (!string.IsNullOrEmpty(internalAttachmentId))
				{
					hashSet.Add(internalAttachmentId);
				}
			}
		}
		return hashSet.ToList();
	}

	internal static IEnumerable<Match> GetDiagnosticPersistedImageMatches(string text)
	{
		text = text ?? string.Empty;
		List<MarkdownBlockNode> codeRanges = ((text.IndexOf('`') < 0) ? new List<MarkdownBlockNode>() : MarkdownSemanticConverter.Parse(text).Blocks.Where((MarkdownBlockNode block) => block.Kind == MarkdownBlockKind.CodeBlock).ToList());
		foreach (Match item in AttachmentRegex.Matches(text))
		{
			if (!(item.Groups[1].Value != "image") && IsRenderableAttachmentMatch(text, item) && !IsMatchInsideInlineCode(text, item))
			{
				int line = GetLineIndex(text, item.Index);
				if (!codeRanges.Any((MarkdownBlockNode block) => line >= block.StartLine && line <= block.EndLine))
				{
					yield return item;
				}
			}
		}
	}

	private static bool IsMatchInsideInlineCode(string text, Match match)
	{
		int num = match.Index;
		while (num > 0 && text[num - 1] != '\r' && text[num - 1] != '\n')
		{
			num--;
		}
		int i;
		for (i = match.Index + match.Length; i < text.Length && text[i] != '\r' && text[i] != '\n'; i++)
		{
		}
		int num2 = num;
		while (num2 < i)
		{
			int num3 = text.IndexOf('`', num2, i - num2);
			if (num3 < 0)
			{
				return false;
			}
			int num4 = 0;
			int num5 = num3 - 1;
			while (num5 >= num && text[num5] == '\\')
			{
				num4++;
				num5--;
			}
			if (num4 % 2 != 0)
			{
				num2 = num3 + 1;
				continue;
			}
			int num6 = text.IndexOf('`', num3 + 1, i - num3 - 1);
			if (num6 < 0)
			{
				return false;
			}
			if (num6 == num3 + 1)
			{
				num2 = num3 + 1;
				continue;
			}
			if (match.Index < num6 + 1 && match.Index + match.Length > num3)
			{
				return true;
			}
			num2 = num6 + 1;
		}
		return false;
	}

	private static int GetLineIndex(string text, int offset)
	{
		int num = 0;
		for (int i = 0; i < offset && i < text.Length; i++)
		{
			if (text[i] == '\r')
			{
				num++;
				if (i + 1 < offset && text[i + 1] == '\n')
				{
					i++;
				}
			}
			else if (text[i] == '\n')
			{
				num++;
			}
		}
		return num;
	}

	public static bool IsRenderableAttachmentMatch(string text, Match match)
	{
		string attachmentKind;
		return TryGetRenderableAttachmentKind(text, match, out attachmentKind);
	}

	private static bool TryGetRenderableAttachmentKind(string text, Match match, out string attachmentKind)
	{
		attachmentKind = null;
		if (!IsUnescapedAttachmentMatch(text, match) || match.Groups.Count < 3)
		{
			return false;
		}
		string value = match.Groups[1].Value;
		string value2 = match.Groups[2].Value;
		if (string.IsNullOrWhiteSpace(value2))
		{
			return false;
		}
		if (Uri.IsWellFormedUriString(value2, UriKind.Absolute))
		{
			if (AttachmentProvider.GetFileType(System.IO.Path.GetFileName(new Uri(value2, UriKind.Absolute).AbsolutePath)) == Constants.AttachmentKind.IMAGE)
			{
				attachmentKind = "image";
				return true;
			}
			if (string.Equals(value, "file", StringComparison.OrdinalIgnoreCase))
			{
				attachmentKind = "file";
				return true;
			}
			return false;
		}
		if (string.Equals(value, "image", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "file", StringComparison.OrdinalIgnoreCase))
		{
			attachmentKind = (string.Equals(value, "file", StringComparison.OrdinalIgnoreCase) ? "file" : "image");
			return true;
		}
		return false;
	}

	private static IEnumerable<PersistedMarkdownLinkMatch> EnumeratePersistedMarkdownLinks(string text, bool includeImages = false, bool singleLine = false)
	{
		if (string.IsNullOrEmpty(text))
		{
			yield break;
		}
		int[] singleLineUrlEnds = (singleLine ? BuildSingleLineLinkUrlEnds(text) : null);
		for (int i = 0; i < text.Length; i++)
		{
			if (text[i] == '\\' && i + 1 < text.Length)
			{
				i++;
			}
			else
			{
				if (text[i] != '[' || (!includeImages && i > 0 && text[i - 1] == '!'))
				{
					continue;
				}
				int num = MarkdownSemanticConverter.FindInlineLinkTextEnd(text, i);
				if (num > i + 1 && num + 1 < text.Length && text[num + 1] == '(')
				{
					int num2 = num + 2;
					int urlEnd = (singleLine ? singleLineUrlEnds[num2 - 1] : FindRawMarkdownLinkUrlEnd(text, num2));
					if (urlEnd > num2)
					{
						int num3 = i + 1;
						yield return new PersistedMarkdownLinkMatch(i, urlEnd + 1 - i, num3, num - num3, num2, urlEnd - num2, text.Substring(num3, num - num3), text.Substring(num2, urlEnd - num2));
						i = urlEnd;
					}
				}
			}
		}
	}

	private static int[] BuildSingleLineLinkUrlEnds(string text)
	{
		int[] array = new int[text.Length];
		Stack<int> stack = new Stack<int>();
		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			switch (c)
			{
			case '\n':
			case '\r':
				stack.Clear();
				continue;
			case '\\':
				if (i + 1 < text.Length)
				{
					if (text[i + 1] != '\r' && text[i + 1] != '\n')
					{
						i++;
					}
					continue;
				}
				break;
			}
			switch (c)
			{
			case '(':
				stack.Push(i);
				break;
			case ')':
				if (stack.Count > 0)
				{
					array[stack.Pop()] = i;
				}
				break;
			}
		}
		return array;
	}

	private static void OnBindingTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		MarkDownEditor markDownEditor = (MarkDownEditor)d;
		if (!markDownEditor._suppressBindingTextToEditorSync)
		{
			string text = (string)e.NewValue;
			if (!string.Equals(text ?? string.Empty, markDownEditor.GetPersistedText(), StringComparison.Ordinal))
			{
				markDownEditor.Text = text;
			}
		}
	}

	private static void OnReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		((MarkDownEditor)d).ApplyReadOnlyState((bool)e.NewValue);
	}

	private static void OnIsDarkChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is MarkDownEditor markDownEditor)
		{
			markDownEditor.SetThemeColor(e.NewValue as bool? == true);
		}
	}

	private void SetThemeColor(bool isDark)
	{
		if (_isDark != isDark)
		{
			_isDark = isDark;
			OnAppThemeChanged(null, null);
		}
	}

	private static void OnImageModeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		if (sender is MarkDownEditor markDownEditor && e.NewValue is int imageMode)
		{
			markDownEditor.ImageMode = imageMode;
		}
	}

	private static void OnSpellCheckChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		if (sender is MarkDownEditor markDownEditor && e.NewValue is bool enableSpellCheck)
		{
			markDownEditor.EnableSpellCheck = enableSpellCheck;
		}
	}

	private void ApplyReadOnlyState(bool value)
	{
		ClearHeadingDisplayFoldOverrides();
		EditBox.IsReadOnly = value;
		EditBox.TextArea.Caret.CaretBrush = ResolveParentCaretBrush(_textColor);
		EditBox.TextArea.TextView.Redraw();
	}

	private Brush ResolveParentCaretBrush(Brush activeBrush)
	{
		object obj;
		if (!ShouldSuppressParentCaret(EditBox?.IsReadOnly ?? false, _suppressParentCaretForCodeBlockAttachment, _suppressParentCaretForCodeBlockObjectSelection))
		{
			obj = activeBrush;
			if (obj == null)
			{
				obj = _textColor;
				if (obj == null)
				{
					return Brushes.Black;
				}
			}
		}
		else
		{
			obj = Brushes.Transparent;
		}
		return (Brush)obj;
	}

	private static bool ShouldSuppressParentCaret(bool isReadOnly, bool nestedCodeEditorFocused, bool codeBlockObjectSelected)
	{
		return isReadOnly || nestedCodeEditorFocused || codeBlockObjectSelected;
	}

	internal bool IsCodeBlockAttachment(MarkdownBlockNode block)
	{
		if (block == null || block.Kind != MarkdownBlockKind.CodeBlock || block.StartLine < 0 || block.EndLine != block.StartLine || EditBox?.Document == null || block.StartLine + 1 > EditBox.Document.LineCount)
		{
			return false;
		}
		DocumentLine lineByNumber = EditBox.Document.GetLineByNumber(block.StartLine + 1);
		if (lineByNumber != null && lineByNumber.Length == 1)
		{
			return EditBox.Document.GetText(lineByNumber) == '\ue000'.ToString();
		}
		return false;
	}

	public MarkDownEditor()
	{
		InitializeComponent();
		Setup();
		SetupMargin(24.0);
		base.Loaded += OnLoaded;
		base.Unloaded += delegate
		{
			UnbindEvents();
		};
	}

	public MarkDownEditor(bool inPrint)
	{
		InitializeComponent();
		Setup();
		base.Loaded += OnLoaded;
		base.Unloaded += delegate
		{
			UnbindEvents();
		};
		_inPrint = inPrint;
	}

	private void Setup()
	{
		_highLightColor = ThemeUtil.GetColor("TextAccentColor");
		_bracketColor = ThemeUtil.GetColor(_color20, this);
		_textColor = ThemeUtil.GetColor(_color100);
		_colorizer = new HighlightColorizer(this);
		EditBox.Tag = this;
		EditBox.TextArea.TextView.ParagraphTopMargin = 8.0;
		EditBox.TextArea.TextView.HeadingLevelProvider = GetSemanticHeadingLevelForDocumentLine;
		EditBox.TextArea.TextView.SelectionRightPaddingProvider = GetSelectionRightPaddingForVisualLine;
		RefreshHeadingFoldingPlaceholderStyle();
		_headingFoldingManager = FoldingManager.Install(EditBox.TextArea);
		SetupCodeBlockLanguageOverlay();
		SetupSyntaxHighlighting();
		SetupSpellCheck();
		SetupGenerators();
		SetupEvents();
	}

	private async void OnLoaded(object sender, RoutedEventArgs e)
	{
		BindEvents();
	}

	public bool TryAcknowledgePendingLocalDocumentChanges(string persistedText)
	{
		if (!_hasPendingLocalDocumentChanges || !string.Equals(GetPersistedText(), persistedText ?? string.Empty, StringComparison.Ordinal))
		{
			return false;
		}
		_hasPendingLocalDocumentChanges = false;
		return true;
	}

	private void MarkLocalDocumentChangePending()
	{
		if (!_suppressExternalTextChanged)
		{
			_hasPendingLocalDocumentChanges = true;
		}
	}

	private void ClearPendingLocalDocumentChanges()
	{
		_hasPendingLocalDocumentChanges = false;
	}

	internal void CancelScheduledTextChangedNotification()
	{
		_textChangeNotifyHandler.CancelAction();
	}

	internal bool TryGetPersistedTextForSave(out string persistedText)
	{
		_textChangeHandler.CancelAction();
		HandleOnTextChanged();
		persistedText = GetPersistedText() ?? string.Empty;
		_imageElementGenerator?.QueuePersistedAttachmentCheck(persistedText);
		_imageElementGenerator?.FlushPendingAttachmentCheck();
		if (string.IsNullOrEmpty(persistedText))
		{
			return string.IsNullOrEmpty(Text);
		}
		return true;
	}

	public string GetPersistedText()
	{
		return BuildPersistedText(null);
	}

	public int GetPersistedOffset(int displayOffset)
	{
		string text = EditBox.Text ?? string.Empty;
		int num = Math.Max(0, Math.Min(displayOffset, text.Length));
		int[] displayOffsetMap = new int[text.Length + 1];
		BuildPersistedText(ref displayOffsetMap);
		if (displayOffsetMap == null || displayOffsetMap.Length == 0)
		{
			return num;
		}
		return displayOffsetMap[Math.Max(0, Math.Min(num, displayOffsetMap.Length - 1))];
	}

	public int GetPersistedOffsetAfterLineBlockMetadata(int displayOffset)
	{
		int persistedOffset = GetPersistedOffset(displayOffset);
		string text = EditBox.Text ?? string.Empty;
		if (EditBox?.Document == null || text.Length == 0)
		{
			return persistedOffset;
		}
		int num = Math.Max(0, Math.Min(displayOffset, text.Length));
		DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(num);
		if (lineByOffset == null || num != lineByOffset.EndOffset)
		{
			return persistedOffset;
		}
		if (!TryGetSemanticBlockAtLine(lineByOffset.LineNumber - 1, out var block))
		{
			return persistedOffset;
		}
		string trailingBlockMetadataComment = GetTrailingBlockMetadataComment(block);
		return persistedOffset + trailingBlockMetadataComment.Length;
	}

	private static string GetTrailingBlockMetadataComment(MarkdownBlockNode block)
	{
		if (block == null)
		{
			return string.Empty;
		}
		if (block.Kind == MarkdownBlockKind.Heading && block.Folded == true)
		{
			return BuildHeadingFoldedMetadataComment();
		}
		if (block.Kind == MarkdownBlockKind.Paragraph && block.Indent > 0)
		{
			return BuildParagraphIndentMetadataComment(block.Indent);
		}
		return string.Empty;
	}

	private string BuildPersistedText(int[] displayOffsetMap)
	{
		return BuildPersistedText(ref displayOffsetMap);
	}

	private string BuildPersistedText(ref int[] displayOffsetMap)
	{
		string text = EditBox.Text ?? string.Empty;
		int textLength = EditBox?.Document?.TextLength ?? text.Length;
		List<MarkdownInlineDisplaySegment> semanticMarks = (from mark in (SemanticMarkList ?? new List<MarkdownInlineDisplaySegment>()).Concat(_typedSemanticMarks ?? new List<MarkdownInlineDisplaySegment>())
			where IsValidSemanticDisplayMark(mark, textLength)
			orderby mark.SourcePosition, mark.SourceLength
			select mark).ToList();
		return BuildPersistedText(text, semanticMarks, _typedSemanticMarks, _semanticDocument, _semanticBlocksByLine, ref displayOffsetMap);
	}

	private string BuildPersistedTextForHistoryState(string displayText, IReadOnlyList<MarkdownInlineDisplaySegment> typedSemanticMarks, MarkdownSemanticDocument semanticDocument)
	{
		int[] displayOffsetMap = null;
		string text = displayText ?? string.Empty;
		int textLength = text.Length;
		List<MarkdownInlineDisplaySegment> semanticMarks = (from mark in typedSemanticMarks ?? new List<MarkdownInlineDisplaySegment>()
			where IsValidSemanticDisplayMark(mark, textLength)
			orderby mark.SourcePosition, mark.SourceLength
			select mark).ToList();
		return BuildPersistedText(text, semanticMarks, typedSemanticMarks, semanticDocument, BuildSemanticBlockIndex(semanticDocument), ref displayOffsetMap);
	}

	private string BuildPersistedText(string displayText, IReadOnlyList<MarkdownInlineDisplaySegment> semanticMarks, IReadOnlyList<MarkdownInlineDisplaySegment> typedSemanticMarks, MarkdownSemanticDocument semanticDocument, IReadOnlyDictionary<int, MarkdownBlockNode> semanticBlocksByLine, ref int[] displayOffsetMap)
	{
		displayText = NormalizeDisplayTaskMarkersForPersistence(displayText ?? string.Empty);
		semanticMarks = semanticMarks ?? new List<MarkdownInlineDisplaySegment>();
		typedSemanticMarks = typedSemanticMarks ?? new List<MarkdownInlineDisplaySegment>();
		if (displayOffsetMap != null || typedSemanticMarks.Count > 0 || semanticMarks.Any((MarkdownInlineDisplaySegment mark) => mark != null && mark.SourceLength > 0))
		{
			displayOffsetMap = new int[displayText.Length + 1];
		}
		CodeBlockAttachmentPersistenceExpansion expansion;
		bool num = TryExpandCodeBlockAttachmentsForPersistence(displayText, semanticDocument, out expansion);
		string text = (num ? ApplyCodeBlockAttachmentPersistenceExpansion(expansion, ref displayOffsetMap) : ApplySemanticBlockMarkersForPersistenceExpandedCore(displayText, semanticDocument, semanticBlocksByLine, codeBlockContentAlreadyEscaped: false, ref displayOffsetMap));
		MarkdownSemanticDocument semanticDocument2 = semanticDocument;
		IReadOnlyDictionary<int, MarkdownBlockNode> semanticBlocksByLine2 = semanticBlocksByLine;
		if (num)
		{
			semanticDocument2 = expansion.SemanticDocument;
			semanticBlocksByLine2 = expansion.SemanticBlocksByLine;
		}
		List<MarkdownInlineDisplaySegment> marks = semanticMarks.Where((MarkdownInlineDisplaySegment mark) => mark != null && mark.SourceLength > 0 && MarkdownInlineSemanticMetadata.CanSerialize(mark) && !string.IsNullOrEmpty(MarkdownInlineSemanticMetadata.GetOpeningDelimiter(mark)) && !string.IsNullOrEmpty(MarkdownInlineSemanticMetadata.GetClosingDelimiter(mark))).ToList();
		marks = SplitSerializedMarksAtDisplayLineBreaks(marks, displayText);
		List<MarkdownInlineDisplaySegment> list = semanticMarks.Where((MarkdownInlineDisplaySegment mark) => mark != null && mark.Kind == MarkdownMarkKind.LiteralInlineSyntax && mark.SourceLength > 0).ToList();
		if (marks.Count == 0 && list.Count == 0)
		{
			text = EscapePlainInlineSyntaxForPersistence(text, semanticDocument2, semanticBlocksByLine2, EscapePlainInlineSyntaxOnPersistence, ref displayOffsetMap);
			return MarkdownSemanticConverter.RepairMalformedHeadingLinks(text);
		}
		List<PersistedInlineMark> list2 = CreatePersistedInlineMarks(marks);
		SortedSet<int> sortedSet = new SortedSet<int> { 0, displayText.Length };
		foreach (PersistedInlineMark item in list2)
		{
			if (item != null && item.SourceLength > 0)
			{
				int num2 = Math.Max(0, Math.Min(item.SourcePosition, displayText.Length));
				int num3 = Math.Max(num2, Math.Min(item.SourcePosition + item.SourceLength, displayText.Length));
				if (num3 > num2)
				{
					sortedSet.Add(num2);
					sortedSet.Add(num3);
				}
			}
		}
		foreach (MarkdownInlineDisplaySegment item2 in list)
		{
			int num4 = Math.Max(0, Math.Min(item2.SourcePosition, displayText.Length));
			int num5 = Math.Max(num4, Math.Min(item2.SourcePosition + item2.SourceLength, displayText.Length));
			if (num5 > num4)
			{
				sortedSet.Add(num4);
				sortedSet.Add(num5);
			}
		}
		List<int> list3 = sortedSet.ToList();
		StringBuilder stringBuilder = new StringBuilder();
		List<PersistedInlineMark> list4 = new List<PersistedInlineMark>();
		int[] array = displayOffsetMap?.ToArray();
		string text2 = text;
		int[] displayOffsetMap2 = BuildIdentityOffsetMap(text.Length);
		text2 = EscapePlainInlineSyntaxForPersistence(text, semanticDocument2, semanticBlocksByLine2, EscapePlainInlineSyntaxOnPersistence, ref displayOffsetMap2);
		int num6 = 0;
		if (displayOffsetMap != null && displayOffsetMap.Length != 0)
		{
			displayOffsetMap[0] = 0;
		}
		for (int num7 = 0; num7 < list3.Count - 1; num7++)
		{
			int start = list3[num7];
			int end = list3[num7 + 1];
			if (end <= start)
			{
				continue;
			}
			int val = ((array != null && start < array.Length) ? array[start] : start);
			int val2 = ((array != null && end < array.Length) ? array[end] : end);
			val = Math.Max(0, Math.Min(val, text.Length));
			val2 = Math.Max(val, Math.Min(val2, text.Length));
			if (val < num6)
			{
				val = Math.Min(num6, text.Length);
				val2 = Math.Max(val, val2);
			}
			string text3 = text.Substring(val, val2 - val);
			List<PersistedInlineMark> list5 = (from mark in list2
				where mark.SourcePosition < end && mark.SourcePosition + mark.SourceLength > start
				group mark by mark.Identity into @group
				select @group.First() into mark
				orderby mark.SourcePosition, mark.SourcePosition + mark.SourceLength descending, mark.Order
				select mark).ThenBy((PersistedInlineMark mark) => mark.Identity, StringComparer.Ordinal).ToList();
			string text4 = string.Empty;
			if (val > num6)
			{
				text4 = text.Substring(num6, val - num6);
			}
			int num8 = ((text4.Length <= 0) ? GetCommonPersistencePrefixLength(list4, list5) : 0);
			for (int num9 = list4.Count - 1; num9 >= num8; num9--)
			{
				string closingDelimiter = list4[num9].ClosingDelimiter;
				if (!string.IsNullOrEmpty(closingDelimiter))
				{
					stringBuilder.Append(closingDelimiter);
				}
			}
			if (text4.Length > 0)
			{
				stringBuilder.Append(text4);
			}
			for (int num10 = num8; num10 < list5.Count; num10++)
			{
				string openingDelimiter = list5[num10].OpeningDelimiter;
				if (!string.IsNullOrEmpty(openingDelimiter))
				{
					stringBuilder.Append(openingDelimiter);
				}
			}
			if (displayOffsetMap != null)
			{
				displayOffsetMap[start] = stringBuilder.Length;
			}
			bool flag = list5.Count > 0;
			bool flag2 = list.Any((MarkdownInlineDisplaySegment mark) => mark.SourcePosition < end && mark.SourcePosition + mark.SourceLength > start);
			string value = text3;
			int[] rawToEscapedOffsetMap = null;
			if (flag)
			{
				value = EscapeSemanticMarkedTextForPersistence(text3, out rawToEscapedOffsetMap, list5.Any((PersistedInlineMark mark) => mark.OpeningDelimiter == "["));
			}
			string text5 = text3;
			int[] rawToEscapedOffsetMap2 = null;
			if (flag2)
			{
				text5 = MarkdownSemanticConverter.EscapePlainTextForPersistence(text3, out rawToEscapedOffsetMap2);
				if (start == 0 || displayText[start - 1] == '\n')
				{
					text5 = EscapePlainBlockPrefixForPersistenceLine(text5, rawToEscapedOffsetMap2);
				}
			}
			if (displayOffsetMap != null)
			{
				int length = stringBuilder.Length;
				int num11 = ((list5.Count == 0) ? TranslateRawOffsetUsingMap(val, displayOffsetMap2) : val);
				for (int num12 = start + 1; num12 < end; num12++)
				{
					int num13 = ((array != null && num12 < array.Length) ? array[num12] : num12);
					int val3 = ((flag2 && rawToEscapedOffsetMap2 != null) ? TranslateRawOffsetUsingMap(num13 - val, rawToEscapedOffsetMap2) : ((list5.Count != 0) ? ((!flag || rawToEscapedOffsetMap == null) ? (num13 - val) : TranslateRawOffsetUsingMap(num13 - val, rawToEscapedOffsetMap)) : (TranslateRawOffsetUsingMap(num13, displayOffsetMap2) - num11)));
					displayOffsetMap[num12] = length + Math.Max(0, val3);
				}
			}
			if (flag2 && rawToEscapedOffsetMap2 != null)
			{
				stringBuilder.Append(text5);
			}
			else if (flag)
			{
				stringBuilder.Append(value);
			}
			else
			{
				stringBuilder.Append(GetEscapedPlainPersistenceSlice(text2, displayOffsetMap2, val, val2));
			}
			num6 = Math.Max(num6, val2);
			list4 = list5;
		}
		for (int num14 = list4.Count - 1; num14 >= 0; num14--)
		{
			string closingDelimiter2 = list4[num14].ClosingDelimiter;
			if (!string.IsNullOrEmpty(closingDelimiter2))
			{
				stringBuilder.Append(closingDelimiter2);
			}
		}
		if (num6 < text.Length)
		{
			stringBuilder.Append(GetEscapedPlainPersistenceSlice(text2, displayOffsetMap2, num6, text.Length));
		}
		if (displayOffsetMap != null && displayOffsetMap.Length != 0)
		{
			displayOffsetMap[displayText.Length] = stringBuilder.Length;
		}
		return MarkdownSemanticConverter.RepairMalformedHeadingLinks(stringBuilder.ToString());
	}

	private static List<PersistedInlineMark> CreatePersistedInlineMarks(IEnumerable<MarkdownInlineDisplaySegment> marks)
	{
		List<MarkdownInlineDisplaySegment> list = (marks ?? Enumerable.Empty<MarkdownInlineDisplaySegment>()).Where((MarkdownInlineDisplaySegment mark) => mark != null && mark.SourceLength > 0 && MarkdownInlineSemanticMetadata.CanSerialize(mark) && !string.IsNullOrEmpty(MarkdownInlineSemanticMetadata.GetOpeningDelimiter(mark)) && !string.IsNullOrEmpty(MarkdownInlineSemanticMetadata.GetClosingDelimiter(mark))).ToList();
		if (HasCrossingPersistedMarkRanges(list))
		{
			return CreateAtomizedPersistedInlineMarks(list);
		}
		HashSet<MarkdownInlineDisplaySegment> consumedMarks = new HashSet<MarkdownInlineDisplaySegment>();
		List<PersistedInlineMark> list2 = new List<PersistedInlineMark>();
		foreach (var item in from mark in list
			group mark by new { mark.SourcePosition, mark.SourceLength })
		{
			List<MarkdownInlineDisplaySegment> list3 = item.Where((MarkdownInlineDisplaySegment mark) => mark.Kind == MarkdownMarkKind.Strong).ToList();
			List<MarkdownInlineDisplaySegment> list4 = item.Where((MarkdownInlineDisplaySegment mark) => mark.Kind == MarkdownMarkKind.Emphasis).ToList();
			if (list3.Count > 0 && list4.Count > 0 && CanUseCombinedPersistedDelimiter(list, item.Key.SourcePosition, item.Key.SourceLength, MarkdownMarkKind.Strong, MarkdownMarkKind.Emphasis))
			{
				list2.Add(CreatePersistedInlineMark(item.Key.SourcePosition, item.Key.SourceLength, "strong+emphasis", 20, "***", "***"));
				foreach (MarkdownInlineDisplaySegment item2 in list3.Concat(list4))
				{
					consumedMarks.Add(item2);
				}
			}
			List<MarkdownInlineDisplaySegment> list5 = item.Where((MarkdownInlineDisplaySegment mark) => mark.Kind == MarkdownMarkKind.Strikethrough).ToList();
			List<MarkdownInlineDisplaySegment> list6 = item.Where((MarkdownInlineDisplaySegment mark) => mark.Kind == MarkdownMarkKind.Underline).ToList();
			if (list5.Count <= 0 || list6.Count <= 0 || !CanUseCombinedPersistedDelimiter(list, item.Key.SourcePosition, item.Key.SourceLength, MarkdownMarkKind.Strikethrough, MarkdownMarkKind.Underline))
			{
				continue;
			}
			list2.Add(CreatePersistedInlineMark(item.Key.SourcePosition, item.Key.SourceLength, "strikethrough+underline", 10, "~~~", "~~~"));
			foreach (MarkdownInlineDisplaySegment item3 in list5.Concat(list6))
			{
				consumedMarks.Add(item3);
			}
		}
		foreach (MarkdownInlineDisplaySegment item4 in list.Where((MarkdownInlineDisplaySegment mark) => !consumedMarks.Contains(mark)))
		{
			list2.Add(CreatePersistedInlineMark(item4));
		}
		return (from mark in list2
			orderby mark.SourcePosition, mark.SourceLength, mark.Order
			select mark).ThenBy((PersistedInlineMark mark) => mark.Identity, StringComparer.Ordinal).ToList();
	}

	private static bool HasCrossingPersistedMarkRanges(IReadOnlyList<MarkdownInlineDisplaySegment> marks)
	{
		if (marks == null)
		{
			return false;
		}
		for (int i = 0; i < marks.Count; i++)
		{
			MarkdownInlineDisplaySegment markdownInlineDisplaySegment = marks[i];
			if (markdownInlineDisplaySegment == null || markdownInlineDisplaySegment.SourceLength <= 0)
			{
				continue;
			}
			int num = Math.Max(0, markdownInlineDisplaySegment.SourcePosition);
			int num2 = num + markdownInlineDisplaySegment.SourceLength;
			for (int j = i + 1; j < marks.Count; j++)
			{
				MarkdownInlineDisplaySegment markdownInlineDisplaySegment2 = marks[j];
				if (markdownInlineDisplaySegment2 != null && markdownInlineDisplaySegment2.SourceLength > 0)
				{
					int num3 = Math.Max(0, markdownInlineDisplaySegment2.SourcePosition);
					int num4 = num3 + markdownInlineDisplaySegment2.SourceLength;
					if ((num < num3 && num3 < num2 && num2 < num4) || (num3 < num && num < num4 && num4 < num2))
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	private static List<PersistedInlineMark> CreateAtomizedPersistedInlineMarks(IReadOnlyList<MarkdownInlineDisplaySegment> sourceMarks)
	{
		List<int> list = (from offset in sourceMarks.Where((MarkdownInlineDisplaySegment mark) => mark != null && mark.SourceLength > 0).SelectMany((MarkdownInlineDisplaySegment mark) => new int[2]
			{
				Math.Max(0, mark.SourcePosition),
				Math.Max(0, mark.SourcePosition) + mark.SourceLength
			}).Distinct()
			orderby offset
			select offset).ToList();
		List<PersistedInlineMark> list2 = new List<PersistedInlineMark>();
		for (int num = 0; num + 1 < list.Count; num++)
		{
			int start = list[num];
			int end = list[num + 1];
			if (end <= start)
			{
				continue;
			}
			List<MarkdownInlineDisplaySegment> list3 = sourceMarks.Where((MarkdownInlineDisplaySegment mark) => mark != null && Math.Max(0, mark.SourcePosition) <= start && Math.Max(0, mark.SourcePosition) + mark.SourceLength >= end).ToList();
			if (list3.Count == 0)
			{
				continue;
			}
			HashSet<MarkdownInlineDisplaySegment> consumedMarks = new HashSet<MarkdownInlineDisplaySegment>();
			List<MarkdownInlineDisplaySegment> list4 = list3.Where((MarkdownInlineDisplaySegment mark) => mark.Kind == MarkdownMarkKind.Strong).ToList();
			List<MarkdownInlineDisplaySegment> list5 = list3.Where((MarkdownInlineDisplaySegment mark) => mark.Kind == MarkdownMarkKind.Emphasis).ToList();
			if (list4.Count > 0 && list5.Count > 0)
			{
				list2.Add(CreateAtomizedPersistedInlineMark(start, end - start, "strong+emphasis", 20, "***", "***"));
				foreach (MarkdownInlineDisplaySegment item in list4.Concat(list5))
				{
					consumedMarks.Add(item);
				}
			}
			List<MarkdownInlineDisplaySegment> list6 = list3.Where((MarkdownInlineDisplaySegment mark) => mark.Kind == MarkdownMarkKind.Strikethrough).ToList();
			List<MarkdownInlineDisplaySegment> list7 = list3.Where((MarkdownInlineDisplaySegment mark) => mark.Kind == MarkdownMarkKind.Underline).ToList();
			if (list6.Count > 0 && list7.Count > 0)
			{
				list2.Add(CreateAtomizedPersistedInlineMark(start, end - start, "strikethrough+underline", 10, "~~~", "~~~"));
				foreach (MarkdownInlineDisplaySegment item2 in list6.Concat(list7))
				{
					consumedMarks.Add(item2);
				}
			}
			foreach (MarkdownInlineDisplaySegment item3 in list3.Where((MarkdownInlineDisplaySegment mark) => !consumedMarks.Contains(mark)))
			{
				PersistedInlineMark persistedInlineMark = CreatePersistedInlineMark(item3);
				persistedInlineMark.SourcePosition = start;
				persistedInlineMark.SourceLength = end - start;
				PersistedInlineMark persistedInlineMark2 = persistedInlineMark;
				persistedInlineMark2.Identity = persistedInlineMark2.Identity + "@" + start + "+" + (end - start);
				list2.Add(persistedInlineMark);
			}
		}
		return (from mark in list2
			orderby mark.SourcePosition, mark.SourceLength, mark.Order
			select mark).ThenBy((PersistedInlineMark mark) => mark.Identity, StringComparer.Ordinal).ToList();
	}

	private static PersistedInlineMark CreateAtomizedPersistedInlineMark(int sourcePosition, int sourceLength, string identity, int order, string openingDelimiter, string closingDelimiter)
	{
		PersistedInlineMark persistedInlineMark = CreatePersistedInlineMark(sourcePosition, sourceLength, identity, order, openingDelimiter, closingDelimiter);
		PersistedInlineMark persistedInlineMark2 = persistedInlineMark;
		persistedInlineMark2.Identity = persistedInlineMark2.Identity + "@" + sourcePosition + "+" + sourceLength;
		return persistedInlineMark;
	}

	private static bool CanUseCombinedPersistedDelimiter(IEnumerable<MarkdownInlineDisplaySegment> marks, int sourcePosition, int sourceLength, MarkdownMarkKind firstKind, MarkdownMarkKind secondKind)
	{
		int start = Math.Max(0, sourcePosition);
		int end = start + Math.Max(0, sourceLength);
		return !(marks ?? Enumerable.Empty<MarkdownInlineDisplaySegment>()).Any(delegate(MarkdownInlineDisplaySegment mark)
		{
			if (mark == null || (mark.Kind != firstKind && mark.Kind != secondKind) || (mark.SourcePosition == sourcePosition && mark.SourceLength == sourceLength))
			{
				return false;
			}
			int num = Math.Max(0, mark.SourcePosition);
			int num2 = num + Math.Max(0, mark.SourceLength);
			return num <= end && num2 >= start;
		});
	}

	private static PersistedInlineMark CreatePersistedInlineMark(MarkdownInlineDisplaySegment mark)
	{
		return CreatePersistedInlineMark(mark.SourcePosition, mark.SourceLength, MarkdownInlineSemanticMetadata.GetPersistenceIdentity(mark), MarkdownInlineSemanticMetadata.GetPersistenceOrder(mark.Kind), MarkdownInlineSemanticMetadata.GetOpeningDelimiter(mark), MarkdownInlineSemanticMetadata.GetClosingDelimiter(mark));
	}

	private static PersistedInlineMark CreatePersistedInlineMark(int sourcePosition, int sourceLength, string identity, int order, string openingDelimiter, string closingDelimiter)
	{
		return new PersistedInlineMark
		{
			SourcePosition = sourcePosition,
			SourceLength = sourceLength,
			Identity = (identity ?? string.Empty),
			Order = order,
			OpeningDelimiter = openingDelimiter,
			ClosingDelimiter = closingDelimiter
		};
	}

	private static List<MarkdownInlineDisplaySegment> SplitSerializedMarksAtDisplayLineBreaks(IReadOnlyList<MarkdownInlineDisplaySegment> marks, string displayText)
	{
		if (marks == null || marks.Count == 0)
		{
			return new List<MarkdownInlineDisplaySegment>();
		}
		displayText = displayText ?? string.Empty;
		if (displayText.Length == 0)
		{
			return marks.ToList();
		}
		List<MarkdownInlineDisplaySegment> list = new List<MarkdownInlineDisplaySegment>();
		foreach (MarkdownInlineDisplaySegment mark in marks)
		{
			if (!ShouldSplitSerializedMarkAtDisplayLineBreaks(mark))
			{
				list.Add(mark);
				continue;
			}
			int num = Math.Max(0, Math.Min(mark.SourcePosition, displayText.Length));
			int num2 = Math.Max(num, Math.Min(mark.SourcePosition + mark.SourceLength, displayText.Length));
			if (num2 <= num)
			{
				continue;
			}
			bool flag = false;
			int num3 = 0;
			while (num3 <= displayText.Length)
			{
				int i;
				for (i = num3; i < displayText.Length && displayText[i] != '\r' && displayText[i] != '\n'; i++)
				{
				}
				int num4 = Math.Max(num, num3);
				int num5 = Math.Min(num2, i);
				if (num5 > num4 && HasNonWhitespace(displayText, num4, num5))
				{
					list.Add(CopySemanticMarkForPersistenceSegment(mark, num4, num5));
					flag = true;
				}
				if (i >= displayText.Length || i >= num2)
				{
					break;
				}
				num3 = i + 1;
				if (displayText[i] == '\r' && num3 < displayText.Length && displayText[num3] == '\n')
				{
					num3++;
				}
			}
			if (!flag && !RangeContainsLineBreak(displayText, num, num2))
			{
				list.Add(mark);
			}
		}
		return (from mark in list
			orderby mark.SourcePosition, mark.SourceLength, MarkdownInlineSemanticMetadata.GetPersistenceOrder(mark.Kind)
			select mark).ToList();
	}

	private static bool ShouldSplitSerializedMarkAtDisplayLineBreaks(MarkdownInlineDisplaySegment mark)
	{
		if (mark != null)
		{
			return mark.Kind != MarkdownMarkKind.Link;
		}
		return false;
	}

	private static bool RangeContainsLineBreak(string text, int start, int end)
	{
		text = text ?? string.Empty;
		int num = Math.Max(0, Math.Min(start, text.Length));
		int num2 = Math.Max(num, Math.Min(end, text.Length));
		for (int i = num; i < num2; i++)
		{
			if (text[i] == '\r' || text[i] == '\n')
			{
				return true;
			}
		}
		return false;
	}

	private static MarkdownInlineDisplaySegment CopySemanticMarkForPersistenceSegment(MarkdownInlineDisplaySegment mark, int start, int end)
	{
		return new MarkdownInlineDisplaySegment
		{
			Kind = mark.Kind,
			SourcePosition = start,
			SourceLength = Math.Max(0, end - start),
			OpeningDelimiterLength = 0,
			ClosingDelimiterLength = 0,
			Color = mark.Color,
			LinkText = mark.LinkText,
			Url = mark.Url,
			UrlLength = mark.UrlLength,
			UrlPosition = mark.UrlPosition
		};
	}

	private static string GetEscapedPlainPersistenceSlice(string escapedText, int[] rawToEscapedOffsetMap, int start, int end)
	{
		escapedText = escapedText ?? string.Empty;
		if (rawToEscapedOffsetMap == null || rawToEscapedOffsetMap.Length == 0)
		{
			return string.Empty;
		}
		int val = rawToEscapedOffsetMap.Length - 1;
		start = Math.Max(0, Math.Min(start, val));
		end = Math.Max(start, Math.Min(end, val));
		int num = Math.Max(0, Math.Min(TranslateRawOffsetUsingMap(start, rawToEscapedOffsetMap), escapedText.Length));
		int num2 = Math.Max(num, Math.Min(TranslateRawOffsetUsingMap(end, rawToEscapedOffsetMap), escapedText.Length));
		return escapedText.Substring(num, num2 - num);
	}

	private static string EscapeSemanticMarkedTextForPersistence(string text, out int[] rawToEscapedOffsetMap, bool escapeLinkLabel = false)
	{
		string text2 = MarkdownSemanticConverter.EscapeSemanticTextForPersistence(text, out rawToEscapedOffsetMap);
		if (!escapeLinkLabel || text2.IndexOf('[') < 0)
		{
			return text2;
		}
		StringBuilder stringBuilder = new StringBuilder(text2.Length);
		int[] array = new int[text2.Length + 1];
		int num = 0;
		for (int i = 0; i < text2.Length; i++)
		{
			array[i] = stringBuilder.Length;
			char c = text2[i];
			if (c == '[' && num % 2 == 0)
			{
				stringBuilder.Append('\\');
			}
			stringBuilder.Append(c);
			num = ((c == '\\') ? (num + 1) : 0);
		}
		array[text2.Length] = stringBuilder.Length;
		for (int j = 0; j < rawToEscapedOffsetMap.Length; j++)
		{
			rawToEscapedOffsetMap[j] = array[rawToEscapedOffsetMap[j]];
		}
		return stringBuilder.ToString();
	}

	private string ApplySemanticBlockMarkersForPersistence(string text, ref int[] displayOffsetMap)
	{
		return ApplySemanticBlockMarkersForPersistenceCore(text, _semanticDocument, _semanticBlocksByLine, ref displayOffsetMap);
	}

	private string ApplySemanticBlockMarkersForPersistenceCore(string text, MarkdownSemanticDocument semanticDocument, IReadOnlyDictionary<int, MarkdownBlockNode> semanticBlocksByLine, ref int[] displayOffsetMap)
	{
		if (TryExpandCodeBlockAttachmentsForPersistence(text, semanticDocument, out var expansion))
		{
			return ApplyCodeBlockAttachmentPersistenceExpansion(expansion, ref displayOffsetMap);
		}
		return ApplySemanticBlockMarkersForPersistenceExpandedCore(text, semanticDocument, semanticBlocksByLine, codeBlockContentAlreadyEscaped: false, ref displayOffsetMap);
	}

	private string ApplyCodeBlockAttachmentPersistenceExpansion(CodeBlockAttachmentPersistenceExpansion expansion, ref int[] displayOffsetMap)
	{
		int[] displayOffsetMap2 = ((displayOffsetMap == null) ? null : new int[expansion.DisplayText.Length + 1]);
		string result = ApplySemanticBlockMarkersForPersistenceExpandedCore(expansion.DisplayText, expansion.SemanticDocument, expansion.SemanticBlocksByLine, codeBlockContentAlreadyEscaped: true, ref displayOffsetMap2);
		if (displayOffsetMap == null)
		{
			return result;
		}
		int[] array = new int[displayOffsetMap.Length];
		for (int i = 0; i < array.Length; i++)
		{
			int val = expansion.SourceToExpandedOffsetMap[Math.Max(0, Math.Min(i, expansion.SourceToExpandedOffsetMap.Length - 1))];
			array[i] = displayOffsetMap2[Math.Max(0, Math.Min(val, displayOffsetMap2.Length - 1))];
		}
		displayOffsetMap = array;
		return result;
	}

	private string ApplySemanticBlockMarkersForPersistenceExpandedCore(string text, MarkdownSemanticDocument semanticDocument, IReadOnlyDictionary<int, MarkdownBlockNode> semanticBlocksByLine, bool codeBlockContentAlreadyEscaped, ref int[] displayOffsetMap)
	{
		text = text ?? string.Empty;
		if ((semanticBlocksByLine == null || semanticBlocksByLine.Count <= 0) && (semanticDocument?.Blocks == null || semanticDocument.Blocks.Count <= 0) && !ContainsInternalBlockMetadataComment(text))
		{
			if (displayOffsetMap != null)
			{
				int[] array = new int[text.Length + 1];
				int num = 0;
				array[0] = 0;
				for (int i = 0; i < text.Length; i++)
				{
					if (text[i] != '\u00a0' && text[i] != '\u2065')
					{
						num++;
					}
					array[i + 1] = num;
				}
				displayOffsetMap = array;
			}
			return text;
		}
		StringBuilder stringBuilder = new StringBuilder(text.Length + 32);
		int num2 = 0;
		int num3 = 0;
		int[] array2 = ((displayOffsetMap != null) ? new int[text.Length + 1] : null);
		int num4 = 0;
		int j = 0;
		bool flag = false;
		while (j <= text.Length)
		{
			int num5 = j;
			for (; j < text.Length && text[j] != '\r' && text[j] != '\n'; j++)
			{
			}
			string text2 = text.Substring(num5, j - num5);
			int num6 = 0;
			if (j < text.Length)
			{
				num6 = ((text[j] != '\r' || j + 1 >= text.Length || text[j + 1] != '\n') ? 1 : 2);
			}
			if (array2 != null)
			{
				array2[Math.Min(num2, array2.Length - 1)] = num3;
			}
			MarkdownBlockNode markdownBlockNode = null;
			bool flag2 = false;
			if (TryGetSemanticBlockAtLine(semanticBlocksByLine, semanticDocument, num4, out var block))
			{
				markdownBlockNode = block;
				int val = text2.Length - text2.TrimStart().Length;
				flag2 = block.Kind == MarkdownBlockKind.CodeBlock && IsSemanticCodeBlockContentLine(text2, block, num4);
				if (block.Kind == MarkdownBlockKind.Paragraph && block.Indent > 0 && text2.Length > 0 && text2[0] == '\u2065')
				{
					if (array2 != null)
					{
						num2++;
						array2[Math.Min(num2, array2.Length - 1)] = num3;
					}
					else
					{
						num2++;
					}
					text2 = text2.Substring(1);
				}
				int num7 = ((!(block.Kind == MarkdownBlockKind.Paragraph || flag2)) ? Math.Max(0, block.Indent) : 0);
				string persistedBlockIndentText = GetPersistedBlockIndentText(block, num7);
				if (block.Kind == MarkdownBlockKind.BlockQuote)
				{
					stringBuilder.Append("> ");
					stringBuilder.Append(persistedBlockIndentText);
					num3 += "> ".Length + persistedBlockIndentText.Length;
				}
				else if (persistedBlockIndentText.Length > 0)
				{
					stringBuilder.Append(persistedBlockIndentText);
					num3 += persistedBlockIndentText.Length;
				}
				int num8 = Math.Min(val, Math.Max(0, num7));
				if (!flag2 && num8 > 0)
				{
					if (array2 != null)
					{
						for (int k = 0; k < num8; k++)
						{
							num2++;
							array2[Math.Min(num2, array2.Length - 1)] = num3;
						}
					}
					else
					{
						num2 += num8;
					}
					text2 = text2.Substring(Math.Min(num8, text2.Length));
				}
				if (block.Kind == MarkdownBlockKind.Heading && block.Level.HasValue)
				{
					string text3 = new string('#', Math.Max(1, Math.Min(6, block.Level ?? 1))) + " ";
					stringBuilder.Append(text3);
					num3 += text3.Length;
				}
				else if (block.Kind == MarkdownBlockKind.List && block.ListKind == MarkdownListKind.Task)
				{
					string text4 = ((block.Checked == true) ? "- [x] " : "- [ ] ");
					stringBuilder.Append(text4);
					num3 += text4.Length;
				}
				else if (block.Kind == MarkdownBlockKind.List && block.ListKind == MarkdownListKind.Bullet)
				{
					stringBuilder.Append("- ");
					num3 += "- ".Length;
				}
				else if (block.Kind == MarkdownBlockKind.List && block.ListKind == MarkdownListKind.Numbered)
				{
					string text5 = (string.IsNullOrEmpty(block.CustomOrder) ? "1" : block.CustomOrder) + ". ";
					stringBuilder.Append(text5);
					num3 += text5.Length;
				}
				if (array2 != null)
				{
					array2[Math.Min(num2, array2.Length - 1)] = num3;
				}
			}
			List<OffsetRange> displayBlockMetadataCommentRangesToSkip = GetDisplayBlockMetadataCommentRangesToSkip(text2, markdownBlockNode);
			if (markdownBlockNode == null && (flag || TryGetEditingCodeFenceInfo(text2, out var indent, out var language)))
			{
				displayBlockMetadataCommentRangesToSkip.Clear();
			}
			int num9 = 0;
			int l = 0;
			while (l < text2.Length)
			{
				if (num9 < displayBlockMetadataCommentRangesToSkip.Count && l == displayBlockMetadataCommentRangesToSkip[num9].Start)
				{
					for (int num10 = Math.Max(l, Math.Min(displayBlockMetadataCommentRangesToSkip[num9].End, text2.Length)); l < num10; l++)
					{
						num2++;
						if (array2 != null)
						{
							array2[Math.Min(num2, array2.Length - 1)] = num3;
						}
					}
					num9++;
					continue;
				}
				char c = text2[l];
				l++;
				num2++;
				if (c == '\u00a0' || c == '\u2065')
				{
					if (array2 != null)
					{
						array2[Math.Min(num2, array2.Length - 1)] = num3;
					}
					continue;
				}
				if (flag2 && !codeBlockContentAlreadyEscaped && (c == '\\' || c == '`'))
				{
					stringBuilder.Append('\\');
					num3++;
				}
				stringBuilder.Append(c);
				num3++;
				if (array2 != null)
				{
					array2[Math.Min(num2, array2.Length - 1)] = num3;
				}
			}
			MarkdownBlockNode block3;
			if (TryGetSemanticBlockAtLine(semanticBlocksByLine, semanticDocument, num4, out var block2) && block2 != null && block2.Kind == MarkdownBlockKind.Paragraph && block2.Indent > 0)
			{
				stringBuilder.Append(BuildParagraphIndentMetadataComment(block2.Indent));
				num3 = stringBuilder.Length;
			}
			else if (TryGetSemanticBlockAtLine(semanticBlocksByLine, semanticDocument, num4, out block3) && block3 != null && block3.Kind == MarkdownBlockKind.Heading && block3.Folded == true)
			{
				stringBuilder.Append(BuildHeadingFoldedMetadataComment());
				num3 = stringBuilder.Length;
			}
			if (num6 > 0)
			{
				stringBuilder.Append('\n');
				num3++;
				for (int m = 0; m < num6; m++)
				{
					num2++;
					if (array2 != null)
					{
						array2[Math.Min(num2, array2.Length - 1)] = num3;
					}
				}
			}
			if (j >= text.Length)
			{
				break;
			}
			if (markdownBlockNode == null)
			{
				if (flag)
				{
					if (TryGetEditingCodeFenceClosingInfo(text2, out indent))
					{
						flag = false;
					}
				}
				else if (TryGetEditingCodeFenceInfo(text2, out indent, out language))
				{
					flag = true;
				}
			}
			j += num6;
			num4++;
		}
		displayOffsetMap = array2;
		return stringBuilder.ToString();
	}

	private static bool TryExpandCodeBlockAttachmentsForPersistence(string displayText, MarkdownSemanticDocument sourceDocument, out CodeBlockAttachmentPersistenceExpansion expansion)
	{
		expansion = null;
		if (string.IsNullOrEmpty(displayText) || sourceDocument?.Blocks == null)
		{
			return false;
		}
		List<DisplayLineSpan> lines = GetDisplayLineSpans(displayText);
		Dictionary<int, MarkdownBlockNode> dictionary = (from block in sourceDocument.Blocks
			where block != null && block.Kind == MarkdownBlockKind.CodeBlock && block.StartLine >= 0 && block.StartLine == block.EndLine && block.StartLine < lines.Count
			where GetDisplayLineText(displayText, lines[block.StartLine]) == '\ue000'.ToString()
			select block).ToDictionary((MarkdownBlockNode block) => block.StartLine, (MarkdownBlockNode block) => block);
		if (dictionary.Count == 0)
		{
			return false;
		}
		int[] array = new int[displayText.Length + 1];
		int[] array2 = new int[lines.Count];
		Dictionary<int, int> dictionary2 = new Dictionary<int, int>();
		StringBuilder stringBuilder = new StringBuilder(displayText.Length + dictionary.Count * 32);
		int num = 0;
		for (int num2 = 0; num2 < lines.Count; num2++)
		{
			DisplayLineSpan displayLineSpan = lines[num2];
			array2[num2] = num;
			if (!dictionary.TryGetValue(num2, out var value))
			{
				AppendMappedDisplayRange(stringBuilder, displayText, displayLineSpan.Start, displayLineSpan.End, array);
				if (displayLineSpan.End > displayLineSpan.ContentEnd)
				{
					num++;
				}
				continue;
			}
			int length = stringBuilder.Length;
			array[displayLineSpan.Start] = length;
			string text = BuildEditableCodeBlockDisplayText(value);
			stringBuilder.Append(text);
			array[displayLineSpan.ContentEnd] = stringBuilder.Length;
			AppendMappedDisplayRange(stringBuilder, displayText, displayLineSpan.ContentEnd, displayLineSpan.End, array);
			int num3 = (dictionary2[num2] = GetDisplayLineSpans(text).Count);
			num += Math.Max(0, num3 - 1);
			if (displayLineSpan.End > displayLineSpan.ContentEnd)
			{
				num++;
			}
		}
		array[displayText.Length] = stringBuilder.Length;
		MarkdownSemanticDocument markdownSemanticDocument = CloneSemanticDocument(sourceDocument);
		foreach (MarkdownBlockNode block in markdownSemanticDocument.Blocks)
		{
			if (block != null && block.StartLine >= 0 && block.StartLine < array2.Length)
			{
				int startLine = block.StartLine;
				int num4 = Math.Max(startLine, Math.Min(block.EndLine, array2.Length - 1));
				int num5 = (block.StartLine = array2[startLine]);
				block.EndLine = ((block.Kind == MarkdownBlockKind.CodeBlock && startLine == num4) ? (num5 + Math.Max(0, ((!dictionary2.TryGetValue(startLine, out var value2)) ? 1 : value2) - 1)) : array2[num4]);
			}
		}
		expansion = new CodeBlockAttachmentPersistenceExpansion
		{
			DisplayText = stringBuilder.ToString(),
			SemanticDocument = markdownSemanticDocument,
			SemanticBlocksByLine = BuildSemanticBlockIndex(markdownSemanticDocument),
			SourceToExpandedOffsetMap = array
		};
		return true;
	}

	private static string GetDisplayLineText(string text, DisplayLineSpan line)
	{
		if (text == null || line == null || line.Start < 0 || line.ContentEnd < line.Start || line.ContentEnd > text.Length)
		{
			return string.Empty;
		}
		return text.Substring(line.Start, line.ContentEnd - line.Start);
	}

	private static string BuildEditableCodeBlockDisplayText(MarkdownBlockNode block)
	{
		string text = new string(' ', Math.Max(0, block?.Indent ?? 0));
		string text2 = text + "```" + (block?.Language ?? string.Empty);
		string text3 = EscapeCodeBlockAttachmentBodyForPersistence(block?.CodeText);
		string text4 = text + "```";
		if (!string.IsNullOrEmpty(text3))
		{
			return text2 + "\n" + text3 + "\n" + text4;
		}
		return text2 + "\n" + text4;
	}

	private static string EscapeCodeBlockAttachmentBodyForPersistence(string codeText)
	{
		if (string.IsNullOrEmpty(codeText))
		{
			return codeText ?? string.Empty;
		}
		StringBuilder stringBuilder = new StringBuilder(codeText.Length);
		foreach (char c in codeText)
		{
			if (c == '\\' || c == '`')
			{
				stringBuilder.Append('\\');
			}
			stringBuilder.Append(c);
		}
		return stringBuilder.ToString();
	}

	private static string EscapePlainInlineSyntaxForPersistence(string persistedText, MarkdownSemanticDocument semanticDocument, IReadOnlyDictionary<int, MarkdownBlockNode> semanticBlocksByLine, bool escapeAllPlainInlineCharacters, ref int[] displayOffsetMap)
	{
		if (string.IsNullOrEmpty(persistedText))
		{
			return persistedText ?? string.Empty;
		}
		StringBuilder stringBuilder = new StringBuilder(persistedText.Length);
		int[] array = new int[persistedText.Length + 1];
		int num = 0;
		int i = 0;
		while (i <= persistedText.Length)
		{
			int num2 = i;
			for (; i < persistedText.Length && persistedText[i] != '\r' && persistedText[i] != '\n'; i++)
			{
			}
			string text = persistedText.Substring(num2, i - num2);
			int num3 = i;
			string text2 = string.Empty;
			if (i < persistedText.Length)
			{
				if (persistedText[i] == '\r' && i + 1 < persistedText.Length && persistedText[i + 1] == '\n')
				{
					text2 = "\r\n";
					i += 2;
				}
				else
				{
					text2 = persistedText[i].ToString();
					i++;
				}
			}
			int length = stringBuilder.Length;
			int[] lineToEscapedOffsetMap;
			string value = EscapePlainInlineSyntaxForPersistenceLine(text, semanticDocument, semanticBlocksByLine, num, escapeAllPlainInlineCharacters, out lineToEscapedOffsetMap);
			for (int j = 0; j <= text.Length; j++)
			{
				array[num2 + j] = length + lineToEscapedOffsetMap[Math.Min(j, lineToEscapedOffsetMap.Length - 1)];
			}
			stringBuilder.Append(value);
			int length2 = stringBuilder.Length;
			for (int k = 1; k <= text2.Length; k++)
			{
				array[num3 + k] = length2 + k;
			}
			stringBuilder.Append(text2);
			if (i >= persistedText.Length)
			{
				break;
			}
			num++;
		}
		array[persistedText.Length] = stringBuilder.Length;
		if (displayOffsetMap != null && displayOffsetMap.Length != 0)
		{
			for (int l = 0; l < displayOffsetMap.Length; l++)
			{
				int num4 = Math.Max(0, Math.Min(displayOffsetMap[l], array.Length - 1));
				displayOffsetMap[l] = array[num4];
			}
		}
		return stringBuilder.ToString();
	}

	private static string EscapePlainInlineSyntaxForPersistenceLine(string lineText, MarkdownSemanticDocument semanticDocument, IReadOnlyDictionary<int, MarkdownBlockNode> semanticBlocksByLine, int lineIndex, bool escapeAllPlainInlineCharacters, out int[] lineToEscapedOffsetMap)
	{
		lineText = lineText ?? string.Empty;
		if (lineText.Length == 0)
		{
			lineToEscapedOffsetMap = new int[1];
			return lineText;
		}
		MarkdownBlockNode block;
		bool num = TryGetSemanticBlockAtLine(semanticBlocksByLine, semanticDocument, lineIndex, out block);
		Match match = AttachmentRegex.Match(lineText);
		string attachmentKind;
		bool flag = match.Success && match.Index == 0 && match.Length == lineText.Length && TryGetRenderableAttachmentKind(lineText, match, out attachmentKind);
		if (num && ((block != null && block.Kind == MarkdownBlockKind.CodeBlock) || (block != null && block.Kind == MarkdownBlockKind.HorizontalRule) || (block != null && block.Kind == MarkdownBlockKind.FileBlock && flag)))
		{
			lineToEscapedOffsetMap = BuildIdentityOffsetMap(lineText.Length);
			return lineText;
		}
		if (flag)
		{
			lineToEscapedOffsetMap = BuildIdentityOffsetMap(lineText.Length);
			return lineText;
		}
		int persistedInlineContentStart = GetPersistedInlineContentStart(lineText, block);
		List<OffsetRange> plainInlineEscapeProtectedRanges = GetPlainInlineEscapeProtectedRanges(lineText, block, persistedInlineContentStart);
		string text = ((plainInlineEscapeProtectedRanges.Count != 0) ? EscapePlainInlineSyntaxTextForPersistenceOutsideRanges(lineText, plainInlineEscapeProtectedRanges, escapeAllPlainInlineCharacters, out lineToEscapedOffsetMap) : EscapePlainInlineSyntaxTextForPersistence(lineText, escapeAllPlainInlineCharacters, out lineToEscapedOffsetMap));
		if (persistedInlineContentStart <= 0 && escapeAllPlainInlineCharacters)
		{
			return EscapePlainBlockPrefixForPersistenceLine(text, lineToEscapedOffsetMap, plainInlineEscapeProtectedRanges);
		}
		return text;
	}

	private static string EscapePlainBlockPrefixForPersistenceLine(string lineText, int[] rawToEscapedOffsetMap, IReadOnlyList<OffsetRange> protectedRanges = null)
	{
		lineText = lineText ?? string.Empty;
		int num = FindPlainBlockPrefixEscapeIndex(lineText);
		if (num < 0)
		{
			return lineText;
		}
		if (protectedRanges != null && rawToEscapedOffsetMap != null)
		{
			foreach (OffsetRange protectedRange in protectedRanges)
			{
				if (num >= rawToEscapedOffsetMap[protectedRange.Start] && num < rawToEscapedOffsetMap[protectedRange.End])
				{
					return lineText;
				}
			}
		}
		if (rawToEscapedOffsetMap != null)
		{
			for (int i = 0; i < rawToEscapedOffsetMap.Length; i++)
			{
				if (rawToEscapedOffsetMap[i] > num)
				{
					rawToEscapedOffsetMap[i]++;
				}
			}
		}
		return lineText.Insert(num, "\\");
	}

	private static int FindPlainBlockPrefixEscapeIndex(string lineText)
	{
		if (string.IsNullOrEmpty(lineText))
		{
			return -1;
		}
		int i = 0;
		while (i < lineText.Length)
		{
			if (lineText[i] == '\t')
			{
				i++;
				continue;
			}
			if (i + 4 > lineText.Length || lineText[i] != ' ' || lineText[i + 1] != ' ' || lineText[i + 2] != ' ' || lineText[i + 3] != ' ')
			{
				break;
			}
			i += 4;
		}
		for (; i < lineText.Length && IsEditorOnlyLeadingCharacter(lineText[i]); i++)
		{
		}
		int num = 0;
		while (i < lineText.Length && num < 3 && (lineText[i] == ' ' || lineText[i] == '\u00a0'))
		{
			i++;
			num++;
		}
		if (i >= lineText.Length)
		{
			return -1;
		}
		if (lineText[i] == '!' && i + 1 < lineText.Length && lineText[i + 1] == '[')
		{
			return i + 1;
		}
		if (lineText[i] == '`' && i + 2 < lineText.Length && lineText[i + 1] == '`' && lineText[i + 2] == '`')
		{
			return i;
		}
		switch (lineText[i])
		{
		case '#':
		case '-':
		case '>':
			return i;
		case '*':
		case '+':
			if (i + 1 >= lineText.Length || lineText[i + 1] != ' ')
			{
				return -1;
			}
			return i;
		default:
			if (!char.IsDigit(lineText[i]))
			{
				return -1;
			}
			for (; i < lineText.Length && char.IsDigit(lineText[i]); i++)
			{
			}
			if (i < lineText.Length && lineText[i] == '.' && (i + 1 == lineText.Length || lineText[i + 1] == ' '))
			{
				return i;
			}
			return -1;
		}
	}

	private static bool IsEditorOnlyLeadingCharacter(char ch)
	{
		if (ch != '\u200b' && ch != '\u200c' && ch != '\u200d' && ch != '\u200e' && ch != '\u200f' && ch != '\u2060' && ch != '\u2061' && ch != '\u2062' && ch != '\u2063' && ch != '\u2064')
		{
			return ch == '\u206a';
		}
		return true;
	}

	private static List<OffsetRange> GetPlainInlineEscapeProtectedRanges(string lineText, MarkdownBlockNode block, int contentStart)
	{
		List<OffsetRange> list = new List<OffsetRange>();
		if (string.IsNullOrEmpty(lineText))
		{
			return list;
		}
		contentStart = Math.Max(0, Math.Min(contentStart, lineText.Length));
		if (contentStart > 0)
		{
			list.Add(new OffsetRange
			{
				Start = 0,
				End = contentStart
			});
		}
		list.AddRange(GetCompletedRawMarkdownAttachmentRangesToSkip(lineText, contentStart));
		list.AddRange(GetDisplayBlockMetadataCommentRangesToSkip(lineText, block));
		list.AddRange(GetCompletedRawMarkdownLinkRangesToSkip(lineText, contentStart));
		return NormalizeOffsetRanges(list, lineText.Length);
	}

	private static IEnumerable<OffsetRange> GetCompletedRawMarkdownAttachmentRangesToSkip(string lineText, int contentStart)
	{
		foreach (Match item in AttachmentRegex.Matches(lineText ?? string.Empty))
		{
			if (item.Success && item.Index + item.Length > contentStart && TryGetRenderableAttachmentKind(lineText, item, out var _))
			{
				yield return new OffsetRange
				{
					Start = item.Index,
					End = item.Index + item.Length
				};
			}
		}
	}

	private static IEnumerable<OffsetRange> GetCompletedRawMarkdownLinkRangesToSkip(string lineText, int contentStart)
	{
		if (string.IsNullOrEmpty(lineText))
		{
			yield break;
		}
		foreach (PersistedMarkdownLinkMatch item in EnumeratePersistedMarkdownLinks(lineText))
		{
			if (item.Index >= contentStart && !IsLinkMatchInsideMarkdownImageSyntax(lineText, item.Index) && IsAutoConvertibleRawMarkdownLinkUrl(item.Url))
			{
				yield return new OffsetRange
				{
					Start = item.Index,
					End = item.Index + item.Length
				};
			}
		}
	}

	private static string EscapePlainInlineSyntaxTextForPersistenceOutsideRanges(string text, IReadOnlyList<OffsetRange> protectedRanges, bool escapeAllPlainInlineCharacters, out int[] rawToEscapedOffsetMap)
	{
		text = text ?? string.Empty;
		List<OffsetRange> list = NormalizeOffsetRanges(protectedRanges, text.Length);
		if (list.Count == 0)
		{
			return EscapePlainInlineSyntaxTextForPersistence(text, escapeAllPlainInlineCharacters, out rawToEscapedOffsetMap);
		}
		StringBuilder stringBuilder = new StringBuilder(text.Length);
		rawToEscapedOffsetMap = new int[text.Length + 1];
		int start = 0;
		foreach (OffsetRange item in list)
		{
			AppendEscapedPlainInlineSyntaxSegment(text, start, item.Start, stringBuilder, rawToEscapedOffsetMap, escapeAllPlainInlineCharacters);
			for (int i = 0; i <= item.End - item.Start; i++)
			{
				rawToEscapedOffsetMap[item.Start + i] = stringBuilder.Length + i;
			}
			stringBuilder.Append(text.Substring(item.Start, item.End - item.Start));
			start = item.End;
		}
		AppendEscapedPlainInlineSyntaxSegment(text, start, text.Length, stringBuilder, rawToEscapedOffsetMap, escapeAllPlainInlineCharacters);
		rawToEscapedOffsetMap[text.Length] = stringBuilder.Length;
		return stringBuilder.ToString();
	}

	private static List<OffsetRange> NormalizeOffsetRanges(IReadOnlyList<OffsetRange> ranges, int textLength)
	{
		textLength = Math.Max(0, textLength);
		if (ranges == null || ranges.Count == 0)
		{
			return new List<OffsetRange>();
		}
		List<OffsetRange> list = (from range in ranges
			where range != null
			select new OffsetRange
			{
				Start = Math.Max(0, Math.Min(range.Start, textLength)),
				End = Math.Max(0, Math.Min(range.End, textLength))
			} into range
			where range.End > range.Start
			orderby range.Start, range.End
			select range).ToList();
		if (list.Count <= 1)
		{
			return list;
		}
		List<OffsetRange> list2 = new List<OffsetRange>();
		foreach (OffsetRange item in list)
		{
			OffsetRange offsetRange = ((list2.Count > 0) ? list2[list2.Count - 1] : null);
			if (offsetRange == null || item.Start > offsetRange.End)
			{
				list2.Add(item);
			}
			else
			{
				offsetRange.End = Math.Max(offsetRange.End, item.End);
			}
		}
		return list2;
	}

	private static string EscapePlainInlineSyntaxTextForPersistence(string text, bool escapeAllPlainInlineCharacters, out int[] rawToEscapedOffsetMap)
	{
		text = text ?? string.Empty;
		if (text.Length == 0)
		{
			rawToEscapedOffsetMap = new int[1];
			return string.Empty;
		}
		List<Match> list = (from Match match in WrappedAttachmentLinkRegex.Matches(text)
			where match.Success && match.Length > 0
			orderby match.Index
			select match).ToList();
		if (list.Count == 0)
		{
			if (!escapeAllPlainInlineCharacters)
			{
				return MarkdownSemanticConverter.EscapeCompletedInlineSyntaxForPersistence(text, out rawToEscapedOffsetMap);
			}
			return MarkdownSemanticConverter.EscapePlainTextForPersistence(text, out rawToEscapedOffsetMap);
		}
		StringBuilder stringBuilder = new StringBuilder(text.Length);
		rawToEscapedOffsetMap = new int[text.Length + 1];
		int num = 0;
		foreach (Match item in list)
		{
			if (item.Index >= num)
			{
				AppendEscapedPlainInlineSyntaxSegment(text, num, item.Index, stringBuilder, rawToEscapedOffsetMap, escapeAllPlainInlineCharacters);
				for (int num2 = 0; num2 <= item.Length; num2++)
				{
					rawToEscapedOffsetMap[item.Index + num2] = stringBuilder.Length + num2;
				}
				stringBuilder.Append(item.Value);
				num = item.Index + item.Length;
			}
		}
		AppendEscapedPlainInlineSyntaxSegment(text, num, text.Length, stringBuilder, rawToEscapedOffsetMap, escapeAllPlainInlineCharacters);
		rawToEscapedOffsetMap[text.Length] = stringBuilder.Length;
		return stringBuilder.ToString();
	}

	private static void AppendEscapedPlainInlineSyntaxSegment(string text, int start, int end, StringBuilder builder, int[] rawToEscapedOffsetMap, bool escapeAllPlainInlineCharacters)
	{
		start = Math.Max(0, Math.Min(start, text?.Length ?? 0));
		end = Math.Max(start, Math.Min(end, text?.Length ?? 0));
		if (end <= start)
		{
			if (rawToEscapedOffsetMap != null && start < rawToEscapedOffsetMap.Length)
			{
				rawToEscapedOffsetMap[start] = builder.Length;
			}
			return;
		}
		string text2 = text.Substring(start, end - start);
		int[] rawToEscapedOffsetMap2;
		string value = (escapeAllPlainInlineCharacters ? MarkdownSemanticConverter.EscapePlainTextForPersistence(text2, out rawToEscapedOffsetMap2) : MarkdownSemanticConverter.EscapeCompletedInlineSyntaxForPersistence(text2, out rawToEscapedOffsetMap2));
		for (int i = 0; i < rawToEscapedOffsetMap2.Length; i++)
		{
			rawToEscapedOffsetMap[start + i] = builder.Length + rawToEscapedOffsetMap2[i];
		}
		builder.Append(value);
	}

	private static int[] BuildIdentityOffsetMap(int length)
	{
		int[] array = new int[Math.Max(0, length) + 1];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = i;
		}
		return array;
	}

	private static string GetPersistedBlockIndentText(MarkdownBlockNode block, int indentColumns)
	{
		indentColumns = Math.Max(0, indentColumns);
		if (indentColumns <= 0)
		{
			return string.Empty;
		}
		if ((block != null && block.Kind == MarkdownBlockKind.List) || (block != null && block.Kind == MarkdownBlockKind.BlockQuote))
		{
			int count = Math.Max(1, (indentColumns + 4 - 1) / 4);
			return new string('\t', count);
		}
		return new string(' ', indentColumns);
	}

	private static int GetPersistedInlineContentStart(string lineText, MarkdownBlockNode block)
	{
		if (string.IsNullOrEmpty(lineText) || block == null)
		{
			return 0;
		}
		int num = lineText.Length - lineText.TrimStart().Length;
		switch (block.Kind)
		{
		case MarkdownBlockKind.Heading:
			if (block.Level.HasValue)
			{
				return num + Math.Max(1, Math.Min(6, block.Level.Value)) + 1;
			}
			break;
		case MarkdownBlockKind.BlockQuote:
			return Math.Min(lineText.Length, num + "> ".Length);
		case MarkdownBlockKind.List:
			switch (block.ListKind)
			{
			case MarkdownListKind.Task:
				return Math.Min(lineText.Length, num + "- [ ] ".Length);
			case MarkdownListKind.Numbered:
			{
				int num2 = Math.Max(1, (block.CustomOrder ?? "1").Length);
				return Math.Min(lineText.Length, num + num2 + 2);
			}
			default:
				return Math.Min(lineText.Length, num + "- ".Length);
			}
		}
		return 0;
	}

	private static bool TryGetSemanticBlockAtLine(IReadOnlyDictionary<int, MarkdownBlockNode> semanticBlocksByLine, MarkdownSemanticDocument semanticDocument, int lineIndex, out MarkdownBlockNode block)
	{
		block = null;
		if (lineIndex < 0)
		{
			return false;
		}
		if (semanticBlocksByLine != null && semanticBlocksByLine.TryGetValue(lineIndex + 1, out var value) && value != null)
		{
			block = value;
			return true;
		}
		block = semanticDocument?.Blocks?.FirstOrDefault((MarkdownBlockNode candidate) => candidate != null && candidate.StartLine <= lineIndex && candidate.EndLine >= lineIndex);
		return block != null;
	}

	private static int GetCommonPersistencePrefixLength(IReadOnlyList<PersistedInlineMark> previousMarks, IReadOnlyList<PersistedInlineMark> currentMarks)
	{
		int num = Math.Min(previousMarks?.Count ?? 0, currentMarks?.Count ?? 0);
		int i;
		for (i = 0; i < num && string.Equals(previousMarks[i].Identity, currentMarks[i].Identity, StringComparison.Ordinal); i++)
		{
		}
		return i;
	}

	private PreparedEditorTextState PrepareEditorTextState(string text)
	{
		PreparedEditorTextState preparedEditorTextState = PrepareEditorTextStateCore(text, repairMisplacedBlockMetadata: false, collapseCodeBlockAttachments: true, parseEmphasis: true);
		AddLiteralInlineSyntaxMarks(preparedEditorTextState);
		return preparedEditorTextState;
	}

	private PreparedEditorTextState PrepareEditorTextState(string text, bool repairMisplacedBlockMetadata)
	{
		PreparedEditorTextState preparedEditorTextState = PrepareEditorTextStateCore(text, repairMisplacedBlockMetadata, collapseCodeBlockAttachments: true, parseEmphasis: true);
		AddLiteralInlineSyntaxMarks(preparedEditorTextState);
		return preparedEditorTextState;
	}

	private PreparedEditorTextState PreparePastedEditorTextState(string text)
	{
		PreparedEditorTextState preparedEditorTextState = PrepareEditorTextStateCore(text, repairMisplacedBlockMetadata: false, collapseCodeBlockAttachments: true, parseEmphasis: false);
		AddLiteralInlineSyntaxMarks(preparedEditorTextState);
		return preparedEditorTextState;
	}

	private PreparedEditorTextState PrepareExportedEditorTextState(string text)
	{
		string text2 = string.Join("\n", (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n')
			.Select(MarkdownSemanticConverter.EscapePlainTextLineForPersistence));
		return PrepareEditorTextState(text2, repairMisplacedBlockMetadata: false);
	}

	private PreparedEditorTextState PrepareStructuredPastedEditorTextState(string text)
	{
		PreparedEditorTextState preparedEditorTextState = PrepareEditorTextStateCore(text, repairMisplacedBlockMetadata: false, collapseCodeBlockAttachments: true, parseEmphasis: true);
		AddLiteralInlineSyntaxMarks(preparedEditorTextState);
		return preparedEditorTextState;
	}

	private void AddLiteralInlineSyntaxMarks(PreparedEditorTextState prepared)
	{
		string text = prepared?.DisplayText ?? string.Empty;
		if (text.Length == 0)
		{
			return;
		}
		string text2 = prepared.MetadataSourceText ?? string.Empty;
		int[] rawToDisplayOffsetMap = prepared.RawToDisplayOffsetMap;
		int num = 0;
		while (rawToDisplayOffsetMap != null && num + 1 < text2.Length)
		{
			if (text2[num] == '\\' && MarkdownSemanticConverter.IsEscapableLiteralChar(text2[num + 1]))
			{
				if (text2[num + 1] != '[' && text2[num + 1] != ']' && text2[num + 1] != '!' && text2[num + 1] != '`')
				{
					num++;
				}
				else
				{
					int start = TranslateRawOffsetUsingMap(num, rawToDisplayOffsetMap);
					int end = TranslateRawOffsetUsingMap(num + 2, rawToDisplayOffsetMap);
					if (start == TranslateRawOffsetUsingMap(num + 1, rawToDisplayOffsetMap) && end == start + 1 && start >= 0 && end <= text.Length && text[start] == text2[num + 1] && !prepared.SemanticMarks.Any((MarkdownInlineDisplaySegment mark) => mark.Kind == MarkdownMarkKind.Code && mark.SourcePosition <= start && mark.SourcePosition + mark.SourceLength >= end))
					{
						prepared.SemanticMarks.Add(new MarkdownInlineDisplaySegment
						{
							Kind = MarkdownMarkKind.LiteralInlineSyntax,
							SourcePosition = start,
							SourceLength = 1,
							LinkText = text.Substring(start, 1)
						});
					}
					num++;
				}
			}
			num++;
		}
		IEnumerable<MarkdownBlockNode> enumerable = MarkdownSemanticConverter.Parse(text, parseEmphasis: true)?.Blocks;
		foreach (MarkdownBlockNode item in enumerable ?? Enumerable.Empty<MarkdownBlockNode>())
		{
			if (item == null || item.Kind == MarkdownBlockKind.CodeBlock || !TryGetSemanticInlineBaseOffset(item, text, out var baseOffset))
			{
				continue;
			}
			foreach (MarkdownInlineNode inline in item.Inlines)
			{
				if (inline != null && inline.SourceLength > 0 && inline.Marks.Any((MarkdownMark mark) => mark != null && (mark.Kind == MarkdownMarkKind.Strong || mark.Kind == MarkdownMarkKind.Emphasis)))
				{
					int start2 = baseOffset + inline.SourceStart;
					int end2 = start2 + inline.SourceLength;
					if (start2 >= 0 && end2 <= text.Length && end2 > start2 && !prepared.SemanticMarks.Any((MarkdownInlineDisplaySegment mark) => mark != null && mark.Kind == MarkdownMarkKind.Code && mark.SourcePosition <= start2 && mark.SourcePosition + mark.SourceLength >= end2))
					{
						prepared.SemanticMarks.Add(new MarkdownInlineDisplaySegment
						{
							Kind = MarkdownMarkKind.LiteralInlineSyntax,
							SourcePosition = start2,
							SourceLength = end2 - start2,
							OpeningDelimiterLength = 0,
							ClosingDelimiterLength = 0,
							LinkText = text.Substring(start2, end2 - start2)
						});
					}
				}
			}
		}
		MergeAdjacentSemanticMarks(prepared.SemanticMarks, text);
	}

	private PreparedEditorTextState PrepareEditorTextStateCore(string text, bool repairMisplacedBlockMetadata, bool collapseCodeBlockAttachments, bool parseEmphasis)
	{
		string rawText = NormalizeLegacyPersistedTextForParsing(text);
		if (repairMisplacedBlockMetadata)
		{
			rawText = NormalizeMisplacedBlockMetadataComments(rawText);
		}
		List<MarkdownInlineDisplaySegment> list = MarkdownInlineSemanticMetadata.Extract(ref rawText);
		PreparedEditorTextState preparedEditorTextState = new PreparedEditorTextState
		{
			DisplayText = rawText,
			MetadataSourceText = rawText
		};
		if (string.IsNullOrEmpty(rawText))
		{
			if (list.Count > 0)
			{
				preparedEditorTextState.SemanticMarks.AddRange(list);
			}
			preparedEditorTextState.MetadataDisplayText = preparedEditorTextState.DisplayText;
			return preparedEditorTextState;
		}
		MarkdownSemanticDocument markdownSemanticDocument = (preparedEditorTextState.SemanticDocument = MarkdownSemanticConverter.Parse(rawText, parseEmphasis));
		if (markdownSemanticDocument?.Blocks == null || markdownSemanticDocument.Blocks.Count == 0)
		{
			if (list.Count > 0)
			{
				preparedEditorTextState.SemanticMarks.AddRange(list);
			}
			preparedEditorTextState.MetadataDisplayText = preparedEditorTextState.DisplayText;
			return preparedEditorTextState;
		}
		int[] rawToVisibleOffsetMap;
		string rawText2 = HideBlockMetadataCommentsForDisplay(rawText, markdownSemanticDocument, out rawToVisibleOffsetMap);
		string displayText = (preparedEditorTextState.DisplayText = ApplyTaskListDisplayMarkers(rawText2, markdownSemanticDocument, out var rawToDisplayOffsetMap));
		preparedEditorTextState.RawToDisplayOffsetMap = ComposeOffsetMaps(rawToVisibleOffsetMap, rawToDisplayOffsetMap);
		displayText = (preparedEditorTextState.DisplayText = RemoveEscapedInlineSyntaxForDisplay(rawText, markdownSemanticDocument, displayText, preparedEditorTextState.RawToDisplayOffsetMap, out var rawToUnescapedDisplayOffsetMap));
		preparedEditorTextState.RawToDisplayOffsetMap = rawToUnescapedDisplayOffsetMap;
		MarkdownSemanticDocument markdownSemanticDocument3 = markdownSemanticDocument;
		CodeBlockAttachmentDisplayTransform codeBlockAttachmentDisplayTransform = (collapseCodeBlockAttachments ? CollapseCodeBlocksForAttachmentDisplay(displayText, markdownSemanticDocument) : null);
		if (codeBlockAttachmentDisplayTransform != null)
		{
			displayText = codeBlockAttachmentDisplayTransform.DisplayText;
			markdownSemanticDocument = codeBlockAttachmentDisplayTransform.SemanticDocument;
			preparedEditorTextState.DisplayText = displayText;
			preparedEditorTextState.SemanticDocument = markdownSemanticDocument;
			preparedEditorTextState.RawToDisplayOffsetMap = ComposeOffsetMaps(preparedEditorTextState.RawToDisplayOffsetMap, codeBlockAttachmentDisplayTransform.SourceToDisplayOffsetMap);
		}
		List<InlineDisplayRange> list2 = new List<InlineDisplayRange>();
		foreach (MarkdownBlockNode block in markdownSemanticDocument3.Blocks)
		{
			if (block == null || block.Kind == MarkdownBlockKind.CodeBlock || !TryGetSemanticInlineBaseOffset(block, rawText, out var baseOffset))
			{
				continue;
			}
			foreach (MarkdownInlineNode inline in block.Inlines)
			{
				if (TryAddInlineDisplayRange(list2, inline, baseOffset, rawText.Length, preparedEditorTextState.RawToDisplayOffsetMap, displayText.Length, out var rawContentStart, out var rawContentEnd))
				{
					AddNestedInlineDisplayRanges(list2, inline, rawContentStart, rawContentEnd, preparedEditorTextState.RawToDisplayOffsetMap, displayText.Length, parseEmphasis);
				}
			}
		}
		if (list2.Count == 0)
		{
			if (list.Count > 0)
			{
				AddTranslatedMetadataMarks(preparedEditorTextState, list);
			}
			preparedEditorTextState.MetadataDisplayText = preparedEditorTextState.DisplayText;
			return preparedEditorTextState;
		}
		List<OffsetRange> list3 = (from range in list2.SelectMany((InlineDisplayRange range) => new OffsetRange[2]
			{
				new OffsetRange
				{
					Start = range.Start,
					End = range.ContentStart
				},
				new OffsetRange
				{
					Start = range.ContentEnd,
					End = range.End
				}
			})
			where range.End > range.Start
			orderby range.Start
			select range).ToList();
		StringBuilder stringBuilder = new StringBuilder(displayText.Length);
		int num = 0;
		int num2 = 0;
		while (num2 < displayText.Length)
		{
			if (num < list3.Count && num2 == list3[num].Start)
			{
				num2 = list3[num].End;
				num++;
			}
			else
			{
				stringBuilder.Append(displayText[num2]);
				num2++;
			}
		}
		string text4 = stringBuilder.ToString();
		if (preparedEditorTextState.RawToDisplayOffsetMap != null && preparedEditorTextState.RawToDisplayOffsetMap.Length != 0)
		{
			int[] array = new int[rawText.Length + 1];
			for (int num3 = 0; num3 <= rawText.Length; num3++)
			{
				int rawOffset = TranslateRawOffsetUsingMap(num3, preparedEditorTextState.RawToDisplayOffsetMap);
				array[num3] = Math.Max(0, Math.Min(TranslateRawOffsetToDisplayOffset(rawOffset, list3), text4.Length));
			}
			preparedEditorTextState.RawToDisplayOffsetMap = array;
		}
		preparedEditorTextState.DisplayText = text4;
		foreach (InlineDisplayRange item in list2)
		{
			int num4 = TranslateRawOffsetToDisplayOffset(item.ContentStart, list3);
			int num5 = TranslateRawOffsetToDisplayOffset(item.ContentEnd, list3);
			if (num4 < 0 || num5 > text4.Length || num5 <= num4)
			{
				continue;
			}
			foreach (MarkdownMark mark in item.Marks)
			{
				preparedEditorTextState.SemanticMarks.Add(new MarkdownInlineDisplaySegment
				{
					Kind = mark.Kind,
					SourcePosition = num4,
					SourceLength = num5 - num4,
					OpeningDelimiterLength = 0,
					ClosingDelimiterLength = 0,
					Color = mark.Color,
					LinkText = text4.Substring(num4, num5 - num4),
					Url = mark.Url
				});
			}
		}
		MergeAdjacentSemanticMarks(preparedEditorTextState.SemanticMarks, text4);
		if (list.Count > 0)
		{
			AddTranslatedMetadataMarks(preparedEditorTextState, list);
		}
		preparedEditorTextState.MetadataDisplayText = preparedEditorTextState.DisplayText;
		return preparedEditorTextState;
	}

	private static void AddTranslatedMetadataMarks(PreparedEditorTextState prepared, IEnumerable<MarkdownInlineDisplaySegment> metadataMarks)
	{
		if (prepared == null || metadataMarks == null)
		{
			return;
		}
		string text = prepared.DisplayText ?? string.Empty;
		int[] rawToDisplayOffsetMap = prepared.RawToDisplayOffsetMap;
		foreach (MarkdownInlineDisplaySegment metadataMark in metadataMarks)
		{
			if (metadataMark != null && metadataMark.SourceLength > 0)
			{
				long num = Math.Min(2147483647L, (long)metadataMark.SourcePosition + (long)Math.Max(0, metadataMark.SourceLength));
				int val = metadataMark.SourcePosition;
				int val2 = (int)num;
				if (rawToDisplayOffsetMap != null && rawToDisplayOffsetMap.Length != 0)
				{
					int num2 = Math.Max(0, Math.Min(metadataMark.SourcePosition, rawToDisplayOffsetMap.Length - 1));
					int rawOffset = (int)Math.Max(num2, Math.Min(num, (long)rawToDisplayOffsetMap.Length - 1L));
					val = TranslateRawOffsetUsingMap(num2, rawToDisplayOffsetMap);
					val2 = TranslateRawOffsetUsingMap(rawOffset, rawToDisplayOffsetMap);
				}
				val = Math.Max(0, Math.Min(val, text.Length));
				val2 = Math.Max(val, Math.Min(val2, text.Length));
				if (val2 > val)
				{
					prepared.SemanticMarks.Add(new MarkdownInlineDisplaySegment
					{
						Kind = metadataMark.Kind,
						SourcePosition = val,
						SourceLength = val2 - val,
						OpeningDelimiterLength = 0,
						ClosingDelimiterLength = 0,
						Color = metadataMark.Color,
						LinkText = text.Substring(val, val2 - val),
						Url = metadataMark.Url,
						UrlPosition = metadataMark.UrlPosition,
						UrlLength = metadataMark.UrlLength
					});
				}
			}
		}
	}

	private static string RepairLegacyEscapedCodeBlockAttachmentSave(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text ?? string.Empty;
		}
		List<string> list = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n')
			.ToList();
		bool flag = false;
		for (int i = 0; i < list.Count; i++)
		{
			if (!TryGetEditingCodeFenceInfo(list[i], out var indent, out var _))
			{
				continue;
			}
			int num = -1;
			int num2 = -1;
			for (int j = i + 1; j < list.Count; j++)
			{
				if (TryGetEditingCodeFenceClosingInfo(list[j], out indent))
				{
					num = j;
					break;
				}
				if (num2 < 0 && IsRepeatedlyEscapedCodeFenceLine(list[j]))
				{
					num2 = j;
				}
			}
			if (num >= 0)
			{
				i = num;
			}
			else if (num2 >= 0)
			{
				string text2 = list[num2] ?? string.Empty;
				int length = text2.Length - text2.TrimStart().Length;
				list[num2] = text2.Substring(0, length) + "```";
				flag = true;
				int num3 = num2 + 1;
				while (num3 < list.Count && IsRepeatedlyEscapedCodeFenceLine(list[num3]))
				{
					list.RemoveAt(num3);
				}
				i = num2;
			}
		}
		if (!flag)
		{
			return text;
		}
		return string.Join("\n", list);
	}

	private static string NormalizeLegacyPersistedTextForParsing(string text)
	{
		return MarkdownSemanticConverter.RepairMalformedHeadingLinks(NormalizeWrappedAttachmentLinkSyntax(NormalizeLegacyDisplayPlaceholders(RepairLegacyEscapedCodeBlockAttachmentSave(text ?? string.Empty))));
	}

	private static bool IsRepeatedlyEscapedCodeFenceLine(string lineText)
	{
		string text = (lineText ?? string.Empty).Trim();
		if (text.Length == 0 || text.Any((char character) => character != '\\' && character != '`'))
		{
			return false;
		}
		for (int num = 0; num < 8; num++)
		{
			if (string.Equals(text, "```", StringComparison.Ordinal))
			{
				return num > 0;
			}
			StringBuilder stringBuilder = new StringBuilder(text.Length);
			bool flag = false;
			for (int num2 = 0; num2 < text.Length; num2++)
			{
				if (text[num2] == '\\' && num2 + 1 < text.Length && (text[num2 + 1] == '\\' || text[num2 + 1] == '`'))
				{
					stringBuilder.Append(text[num2 + 1]);
					num2++;
					flag = true;
				}
				else
				{
					stringBuilder.Append(text[num2]);
				}
			}
			if (!flag)
			{
				return false;
			}
			text = stringBuilder.ToString();
		}
		return string.Equals(text, "```", StringComparison.Ordinal);
	}

	private static CodeBlockAttachmentDisplayTransform CollapseCodeBlocksForAttachmentDisplay(string displayText, MarkdownSemanticDocument sourceDocument)
	{
		if (string.IsNullOrEmpty(displayText) || sourceDocument?.Blocks == null)
		{
			return null;
		}
		Dictionary<int, MarkdownBlockNode> dictionary = sourceDocument.Blocks.Where((MarkdownBlockNode block) => block != null && block.Kind == MarkdownBlockKind.CodeBlock && block.EndLine >= block.StartLine).ToDictionary((MarkdownBlockNode block) => block.StartLine, (MarkdownBlockNode block) => block);
		if (dictionary.Count == 0)
		{
			return null;
		}
		List<DisplayLineSpan> lines = GetDisplayLineSpans(displayText);
		if (lines.Count == 0 || dictionary.Any((KeyValuePair<int, MarkdownBlockNode> pair) => pair.Key < 0 || pair.Key >= lines.Count || pair.Value.EndLine >= lines.Count))
		{
			return null;
		}
		int[] array = new int[displayText.Length + 1];
		int[] array2 = new int[lines.Count];
		StringBuilder stringBuilder = new StringBuilder(displayText.Length);
		int num = 0;
		int num2 = 0;
		while (num < lines.Count)
		{
			DisplayLineSpan displayLineSpan = lines[num];
			array2[num] = num2;
			if (!dictionary.TryGetValue(num, out var value))
			{
				AppendMappedDisplayRange(stringBuilder, displayText, displayLineSpan.Start, displayLineSpan.End, array);
				num++;
				if (displayLineSpan.End > displayLineSpan.ContentEnd)
				{
					num2++;
				}
				continue;
			}
			int num3 = Math.Max(num, Math.Min(value.EndLine, lines.Count - 1));
			DisplayLineSpan displayLineSpan2 = lines[num3];
			int length = stringBuilder.Length;
			for (int num4 = displayLineSpan.Start; num4 <= displayLineSpan2.ContentEnd; num4++)
			{
				array[num4] = length;
			}
			stringBuilder.Append('\ue000');
			int length2 = stringBuilder.Length;
			array[displayLineSpan2.ContentEnd] = length2;
			for (int num5 = num; num5 <= num3; num5++)
			{
				array2[num5] = num2;
			}
			AppendMappedDisplayRange(stringBuilder, displayText, displayLineSpan2.ContentEnd, displayLineSpan2.End, array);
			num = num3 + 1;
			if (displayLineSpan2.End > displayLineSpan2.ContentEnd)
			{
				num2++;
			}
		}
		array[displayText.Length] = stringBuilder.Length;
		MarkdownSemanticDocument markdownSemanticDocument = CloneSemanticDocument(sourceDocument);
		foreach (MarkdownBlockNode block in markdownSemanticDocument.Blocks)
		{
			if (block != null && block.StartLine >= 0 && block.StartLine < array2.Length)
			{
				int startLine = block.StartLine;
				int num6 = Math.Max(startLine, Math.Min(block.EndLine, array2.Length - 1));
				int num7 = (block.StartLine = array2[startLine]);
				block.EndLine = ((block.Kind == MarkdownBlockKind.CodeBlock) ? num7 : array2[num6]);
			}
		}
		return new CodeBlockAttachmentDisplayTransform
		{
			DisplayText = stringBuilder.ToString(),
			SemanticDocument = markdownSemanticDocument,
			SourceToDisplayOffsetMap = array
		};
	}

	private static List<DisplayLineSpan> GetDisplayLineSpans(string text)
	{
		List<DisplayLineSpan> list = new List<DisplayLineSpan>();
		text = text ?? string.Empty;
		int i = 0;
		while (i <= text.Length)
		{
			int start = i;
			for (; i < text.Length && text[i] != '\r' && text[i] != '\n'; i++)
			{
			}
			int num = i;
			if (i < text.Length && text[i] == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
			{
				i += 2;
			}
			else if (i < text.Length)
			{
				i++;
			}
			list.Add(new DisplayLineSpan
			{
				Start = start,
				ContentEnd = num,
				End = i
			});
			if (i >= text.Length)
			{
				if (i == text.Length && num < i)
				{
					list.Add(new DisplayLineSpan
					{
						Start = i,
						ContentEnd = i,
						End = i
					});
				}
				break;
			}
		}
		return list;
	}

	private static void AppendMappedDisplayRange(StringBuilder builder, string source, int startOffset, int endOffset, int[] sourceToDisplayOffsetMap)
	{
		int num = Math.Max(0, Math.Min(startOffset, source?.Length ?? 0));
		int num2 = Math.Max(num, Math.Min(endOffset, source?.Length ?? 0));
		for (int i = num; i < num2; i++)
		{
			sourceToDisplayOffsetMap[i] = builder.Length;
			builder.Append(source[i]);
			sourceToDisplayOffsetMap[i + 1] = builder.Length;
		}
	}

	private static void MergeAdjacentSemanticMarks(List<MarkdownInlineDisplaySegment> marks, string displayText)
	{
		if (marks == null || marks.Count < 2)
		{
			return;
		}
		List<MarkdownInlineDisplaySegment> list = new List<MarkdownInlineDisplaySegment>();
		foreach (MarkdownInlineDisplaySegment mark in marks.OrderBy((MarkdownInlineDisplaySegment markdownInlineDisplaySegment2) => markdownInlineDisplaySegment2?.SourcePosition ?? int.MaxValue))
		{
			if (mark != null && mark.SourceLength > 0)
			{
				MarkdownInlineDisplaySegment markdownInlineDisplaySegment = list.LastOrDefault((MarkdownInlineDisplaySegment candidate) => candidate.Kind == mark.Kind && string.Equals(candidate.Color, mark.Color, StringComparison.Ordinal) && string.Equals(candidate.Url, mark.Url, StringComparison.Ordinal) && mark.SourcePosition <= candidate.SourcePosition + candidate.SourceLength);
				if (markdownInlineDisplaySegment == null)
				{
					list.Add(mark);
					continue;
				}
				int num = Math.Max(markdownInlineDisplaySegment.SourcePosition + markdownInlineDisplaySegment.SourceLength, mark.SourcePosition + mark.SourceLength);
				markdownInlineDisplaySegment.SourceLength = num - markdownInlineDisplaySegment.SourcePosition;
				markdownInlineDisplaySegment.LinkText = ((markdownInlineDisplaySegment.SourcePosition >= 0 && num <= (displayText?.Length ?? 0)) ? displayText.Substring(markdownInlineDisplaySegment.SourcePosition, markdownInlineDisplaySegment.SourceLength) : markdownInlineDisplaySegment.LinkText);
			}
		}
		marks.Clear();
		marks.AddRange(list);
	}

	private static string RemoveEscapedInlineSyntaxForDisplay(string rawText, MarkdownSemanticDocument document, string displayText, int[] rawToDisplayOffsetMap, out int[] rawToUnescapedDisplayOffsetMap)
	{
		rawText = rawText ?? string.Empty;
		displayText = displayText ?? string.Empty;
		rawToUnescapedDisplayOffsetMap = rawToDisplayOffsetMap ?? BuildIdentityOffsetMap(rawText.Length);
		if (rawText.Length == 0 || displayText.Length == 0)
		{
			return displayText;
		}
		List<OffsetRange> escapedInlineSyntaxDisplayRemovalRanges = GetEscapedInlineSyntaxDisplayRemovalRanges(rawText, document, rawToUnescapedDisplayOffsetMap, displayText);
		if (escapedInlineSyntaxDisplayRemovalRanges.Count == 0)
		{
			return displayText;
		}
		StringBuilder stringBuilder = new StringBuilder(displayText.Length);
		int num = 0;
		int num2 = 0;
		while (num2 < displayText.Length)
		{
			if (num < escapedInlineSyntaxDisplayRemovalRanges.Count && num2 == escapedInlineSyntaxDisplayRemovalRanges[num].Start)
			{
				num2 = escapedInlineSyntaxDisplayRemovalRanges[num].End;
				num++;
			}
			else
			{
				stringBuilder.Append(displayText[num2]);
				num2++;
			}
		}
		string text = stringBuilder.ToString();
		int[] array = new int[rawText.Length + 1];
		for (int i = 0; i <= rawText.Length; i++)
		{
			int rawOffset = TranslateRawOffsetUsingMap(i, rawToUnescapedDisplayOffsetMap);
			array[i] = Math.Max(0, Math.Min(TranslateRawOffsetToDisplayOffset(rawOffset, escapedInlineSyntaxDisplayRemovalRanges), text.Length));
		}
		rawToUnescapedDisplayOffsetMap = array;
		return text;
	}

	private static List<OffsetRange> GetEscapedInlineSyntaxDisplayRemovalRanges(string rawText, MarkdownSemanticDocument document, int[] rawToDisplayOffsetMap, string displayText)
	{
		List<OffsetRange> list = new List<OffsetRange>();
		int num = displayText?.Length ?? 0;
		int i = 0;
		int lineIndex = 0;
		while (i <= rawText.Length)
		{
			int num2 = i;
			for (; i < rawText.Length && rawText[i] != '\r' && rawText[i] != '\n'; i++)
			{
			}
			int num3 = i;
			MarkdownBlockNode obj = document?.Blocks?.FirstOrDefault((MarkdownBlockNode currentBlock) => currentBlock != null && currentBlock.StartLine <= lineIndex && currentBlock.EndLine >= lineIndex);
			if (obj != null && obj.Kind == MarkdownBlockKind.CodeBlock)
			{
				for (int num4 = num2; num4 + 1 < num3; num4++)
				{
					if (rawText[num4] == '\\' && (rawText[num4 + 1] == '\\' || rawText[num4 + 1] == '`'))
					{
						int num5 = TranslateRawOffsetUsingMap(num4, rawToDisplayOffsetMap);
						if (num5 < 0 || num5 >= num || displayText[num5] != '\\')
						{
							num4++;
							continue;
						}
						list.Add(new OffsetRange
						{
							Start = num5,
							End = num5 + 1
						});
						num4++;
					}
				}
			}
			else
			{
				for (int num6 = num2; num6 + 1 < num3; num6++)
				{
					if (rawText[num6] == '\\' && MarkdownSemanticConverter.IsEscapableLiteralChar(rawText[num6 + 1]))
					{
						int num7 = TranslateRawOffsetUsingMap(num6, rawToDisplayOffsetMap);
						if (num7 < 0 || num7 >= num || displayText[num7] != '\\')
						{
							num6++;
							continue;
						}
						list.Add(new OffsetRange
						{
							Start = num7,
							End = num7 + 1
						});
						num6++;
					}
				}
			}
			if (i < rawText.Length)
			{
				if (rawText[i] == '\r')
				{
					i++;
					if (i < rawText.Length && rawText[i] == '\n')
					{
						i++;
					}
				}
				else
				{
					i++;
				}
			}
			lineIndex++;
			if (i >= rawText.Length)
			{
				break;
			}
		}
		return (from range in list
			where range.End > range.Start
			orderby range.Start
			select range).ToList();
	}

	private static bool TryAddInlineDisplayRange(ICollection<InlineDisplayRange> inlineRanges, MarkdownInlineNode inline, int baseOffset, int rawTextLength, int[] rawToDisplayOffsetMap, int displayTextLength, out int rawContentStart, out int rawContentEnd)
	{
		rawContentStart = 0;
		rawContentEnd = 0;
		if (inline == null || inline.Marks.Count == 0 || inline.SourceLength <= 0 || inline.OpeningDelimiterLength <= 0 || inline.ClosingDelimiterLength <= 0)
		{
			return false;
		}
		List<MarkdownMark> list = inline.Marks.Where((MarkdownMark mark) => mark != null && IsPersistedInlineShortcutMark(mark.Kind)).ToList();
		if (list.Count == 0)
		{
			return false;
		}
		int num = baseOffset + inline.SourceStart;
		int num2 = num + inline.SourceLength;
		rawContentStart = num + inline.OpeningDelimiterLength;
		rawContentEnd = num2 - inline.ClosingDelimiterLength;
		if (num < 0 || num2 > rawTextLength || rawContentStart >= rawContentEnd)
		{
			return false;
		}
		int num3 = TranslateRawOffsetUsingMap(num, rawToDisplayOffsetMap);
		int num4 = TranslateRawOffsetUsingMap(num2, rawToDisplayOffsetMap);
		int num5 = TranslateRawOffsetUsingMap(rawContentStart, rawToDisplayOffsetMap);
		int num6 = TranslateRawOffsetUsingMap(rawContentEnd, rawToDisplayOffsetMap);
		if (num3 < 0 || num4 > displayTextLength || num5 >= num6)
		{
			return false;
		}
		InlineDisplayRange inlineDisplayRange = new InlineDisplayRange
		{
			Start = num3,
			End = num4,
			ContentStart = num5,
			ContentEnd = num6
		};
		inlineDisplayRange.Marks.AddRange(list);
		inlineRanges.Add(inlineDisplayRange);
		return true;
	}

	private static void AddNestedInlineDisplayRanges(ICollection<InlineDisplayRange> inlineRanges, MarkdownInlineNode parentInline, int rawContentStart, int rawContentEnd, int[] rawToDisplayOffsetMap, int displayTextLength, bool parseEmphasis)
	{
		if (!CanParseNestedInlineMarks(parentInline) || rawContentEnd <= rawContentStart)
		{
			return;
		}
		string text = parentInline.RawText ?? parentInline.Text ?? string.Empty;
		if (text.Length == 0 || text.IndexOfAny(new char[2] { '\r', '\n' }) >= 0 || text.IndexOfAny(new char[7] { '*', '_', '~', '`', '[', '=', ':' }) < 0)
		{
			return;
		}
		foreach (MarkdownInlineNode item in MarkdownSemanticConverter.ParseInlineNodes(text, parseEmphasis))
		{
			if (TryAddInlineDisplayRange(inlineRanges, item, rawContentStart, rawContentEnd, rawToDisplayOffsetMap, displayTextLength, out var rawContentStart2, out var rawContentEnd2))
			{
				AddNestedInlineDisplayRanges(inlineRanges, item, rawContentStart2, rawContentEnd2, rawToDisplayOffsetMap, displayTextLength, parseEmphasis);
			}
		}
	}

	private static bool CanParseNestedInlineMarks(MarkdownInlineNode inline)
	{
		if (inline?.Marks != null)
		{
			return inline.Marks.All((MarkdownMark mark) => mark != null && mark.Kind != MarkdownMarkKind.Code);
		}
		return false;
	}

	private static string NormalizeWrappedAttachmentLinkSyntax(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text ?? string.Empty;
		}
		string text2 = text;
		for (int i = 0; i < 8; i++)
		{
			bool changed = false;
			text2 = WrappedAttachmentLinkRegex.Replace(text2, delegate(Match match)
			{
				string value = match.Groups["inner"].Value;
				string value2 = match.Groups["outer"].Value;
				if (!string.Equals(value, value2, StringComparison.Ordinal))
				{
					return match.Value;
				}
				changed = true;
				return "![" + match.Groups["kind"].Value.ToLowerInvariant() + "](" + value + ")";
			});
			if (!changed)
			{
				return text2;
			}
		}
		return text2;
	}

	private static string NormalizeMisplacedBlockMetadataComments(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text ?? string.Empty;
		}
		StringBuilder stringBuilder = new StringBuilder(text.Length);
		int i = 0;
		bool flag = false;
		while (i <= text.Length)
		{
			int num = i;
			for (; i < text.Length && text[i] != '\r' && text[i] != '\n'; i++)
			{
			}
			string text2 = text.Substring(num, i - num);
			int num2 = i;
			if (i < text.Length)
			{
				if (text[i] == '\r')
				{
					i++;
					if (i < text.Length && text[i] == '\n')
					{
						i++;
					}
				}
				else
				{
					i++;
				}
			}
			stringBuilder.Append(flag ? text2 : RewriteMisplacedHeadingFoldMetadataCommentsInLine(text2));
			stringBuilder.Append(text.Substring(num2, i - num2));
			int indent;
			string language;
			if (flag)
			{
				if (TryGetEditingCodeFenceClosingInfo(text2, out indent))
				{
					flag = false;
				}
			}
			else if (TryGetEditingCodeFenceInfo(text2, out indent, out language))
			{
				flag = true;
			}
			if (i >= text.Length)
			{
				break;
			}
		}
		return stringBuilder.ToString();
	}

	private static string RewriteMisplacedHeadingFoldMetadataCommentsInLine(string lineText)
	{
		if (string.IsNullOrEmpty(lineText))
		{
			return lineText ?? string.Empty;
		}
		List<Match> list = (from Match match in BlockMetadataCommentRegex.Matches(lineText)
			where match.Success && IsHeadingFoldedMetadataJson(match.Groups["json"].Value)
			select match).ToList();
		if (list.Count == 0)
		{
			return lineText;
		}
		StringBuilder stringBuilder = new StringBuilder(lineText.Length);
		int num = 0;
		foreach (Match item in list)
		{
			stringBuilder.Append(lineText.Substring(num, item.Index - num));
			num = item.Index + item.Length;
		}
		stringBuilder.Append(lineText.Substring(num));
		if (!LooksLikeRawHeadingLine(stringBuilder.ToString()))
		{
			return lineText;
		}
		foreach (Match item2 in list)
		{
			stringBuilder.Append(item2.Value);
		}
		return stringBuilder.ToString();
	}

	private static List<OffsetRange> GetDisplayBlockMetadataCommentRangesToSkip(string lineText, MarkdownBlockNode semanticBlock)
	{
		List<OffsetRange> list = new List<OffsetRange>();
		if (string.IsNullOrEmpty(lineText))
		{
			return list;
		}
		foreach (Match item in BlockMetadataCommentRegex.Matches(lineText))
		{
			if (item.Success && IsDisplayBlockMetadataCommentForSemanticBlock(item.Groups["json"].Value, semanticBlock))
			{
				list.Add(new OffsetRange
				{
					Start = item.Index,
					End = item.Index + item.Length
				});
			}
		}
		return list;
	}

	private static bool IsHeadingFoldedMetadataJson(string json)
	{
		if (!string.IsNullOrWhiteSpace(json))
		{
			return Regex.IsMatch(json, "^\\s*\\{\\s*\"folded\"\\s*:\\s*true\\s*\\}\\s*$", RegexOptions.IgnoreCase);
		}
		return false;
	}

	private static bool TryGetParagraphIndentMetadataJson(string json, out int indent)
	{
		indent = 0;
		if (string.IsNullOrWhiteSpace(json))
		{
			return false;
		}
		Match match = Regex.Match(json, "^\\s*\\{\\s*\"indent\"\\s*:\\s*(\\d+)\\s*\\}\\s*$", RegexOptions.IgnoreCase);
		if (match.Success)
		{
			return int.TryParse(match.Groups[1].Value, out indent);
		}
		return false;
	}

	private static bool IsInternalBlockMetadataJson(string json)
	{
		int indent;
		if (!IsHeadingFoldedMetadataJson(json))
		{
			return TryGetParagraphIndentMetadataJson(json, out indent);
		}
		return true;
	}

	private static bool ContainsInternalBlockMetadataComment(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		foreach (Match item in BlockMetadataCommentRegex.Matches(text))
		{
			if (item.Success && IsInternalBlockMetadataJson(item.Groups["json"].Value))
			{
				return true;
			}
		}
		return false;
	}

	private static bool LooksLikeRawHeadingLine(string lineText)
	{
		return Regex.IsMatch(lineText ?? string.Empty, "^\\s{0,40}#{1,6}\\s+");
	}

	private static bool IsDisplayBlockMetadataCommentForSemanticBlock(string json, MarkdownBlockNode semanticBlock)
	{
		if (!IsInternalBlockMetadataJson(json))
		{
			return false;
		}
		if (semanticBlock != null && semanticBlock.Kind == MarkdownBlockKind.CodeBlock)
		{
			return false;
		}
		if (semanticBlock == null)
		{
			return true;
		}
		if (semanticBlock.Kind == MarkdownBlockKind.Heading && semanticBlock.Folded == true)
		{
			return IsHeadingFoldedMetadataJson(json);
		}
		if (semanticBlock.Kind == MarkdownBlockKind.Paragraph && semanticBlock.Indent > 0 && TryGetParagraphIndentMetadataJson(json, out var indent))
		{
			return indent == semanticBlock.Indent;
		}
		return true;
	}

	private static int TranslateRawOffsetToDisplayOffset(int rawOffset, IList<OffsetRange> removalRanges)
	{
		int num = 0;
		foreach (OffsetRange removalRange in removalRanges)
		{
			if (removalRange.End <= rawOffset)
			{
				num += removalRange.End - removalRange.Start;
				continue;
			}
			if (removalRange.Start < rawOffset)
			{
				num += Math.Max(0, rawOffset - removalRange.Start);
			}
			break;
		}
		return Math.Max(0, rawOffset - num);
	}

	private static string RemoveOffsetRanges(string text, IList<OffsetRange> removalRanges)
	{
		text = text ?? string.Empty;
		if (string.IsNullOrEmpty(text) || removalRanges == null || removalRanges.Count == 0)
		{
			return text;
		}
		StringBuilder stringBuilder = new StringBuilder(text.Length);
		int num = 0;
		foreach (OffsetRange removalRange in removalRanges)
		{
			int num2 = Math.Max(num, Math.Min(removalRange.Start, text.Length));
			int num3 = Math.Max(num2, Math.Min(removalRange.End, text.Length));
			if (num2 > num)
			{
				stringBuilder.Append(text.Substring(num, num2 - num));
			}
			num = num3;
		}
		if (num < text.Length)
		{
			stringBuilder.Append(text.Substring(num));
		}
		return stringBuilder.ToString();
	}

	private static int TranslateRawOffsetUsingMap(int rawOffset, int[] rawToDisplayOffsetMap)
	{
		if (rawToDisplayOffsetMap == null || rawToDisplayOffsetMap.Length == 0)
		{
			return Math.Max(0, rawOffset);
		}
		return rawToDisplayOffsetMap[Math.Max(0, Math.Min(rawOffset, rawToDisplayOffsetMap.Length - 1))];
	}

	private string ApplyTaskListDisplayMarkers(string rawText, MarkdownSemanticDocument document, out int[] rawToDisplayOffsetMap)
	{
		rawText = rawText ?? string.Empty;
		rawToDisplayOffsetMap = new int[rawText.Length + 1];
		if (string.IsNullOrEmpty(rawText) || document?.Blocks == null || document.Blocks.Count == 0)
		{
			for (int i = 0; i <= rawText.Length; i++)
			{
				rawToDisplayOffsetMap[i] = i;
			}
			return rawText;
		}
		StringBuilder stringBuilder = new StringBuilder(rawText.Length);
		int j = 0;
		int lineIndex = 0;
		while (j <= rawText.Length)
		{
			int num = j;
			for (; j < rawText.Length && rawText[j] != '\r' && rawText[j] != '\n'; j++)
			{
			}
			int num2 = j;
			string text = rawText.Substring(num, num2 - num);
			int num3 = j;
			if (j < rawText.Length)
			{
				if (rawText[j] == '\r')
				{
					j++;
					if (j < rawText.Length && rawText[j] == '\n')
					{
						j++;
					}
				}
				else
				{
					j++;
				}
			}
			string text2 = rawText.Substring(num3, j - num3);
			rawToDisplayOffsetMap[num] = stringBuilder.Length;
			int markerOffset = 0;
			int markerLength = 0;
			string displayMarker = string.Empty;
			MarkdownBlockNode markdownBlockNode = document.Blocks.FirstOrDefault((MarkdownBlockNode currentBlock) => currentBlock != null && currentBlock.StartLine <= lineIndex && currentBlock.EndLine >= lineIndex);
			bool flag = false;
			if (markdownBlockNode != null)
			{
				flag = TryGetDisplayBlockPrefixReplacement(text, markdownBlockNode, out markerOffset, out markerLength, out displayMarker);
			}
			if (!flag && (markdownBlockNode == null || markdownBlockNode.Kind != MarkdownBlockKind.CodeBlock))
			{
				MarkdownBlockNode markdownBlockNode2 = ConvertSyntaxInfoToBlockNode(MarkdownBlockSyntax.Parse(text));
				if (markdownBlockNode2 != null)
				{
					flag = TryGetDisplayBlockPrefixReplacement(text, markdownBlockNode2, out markerOffset, out markerLength, out displayMarker);
				}
			}
			markerOffset = Math.Max(0, Math.Min(markerOffset, text.Length));
			markerLength = Math.Max(0, Math.Min(markerLength, Math.Max(0, text.Length - markerOffset)));
			string value = ((markerLength > 0 || !string.IsNullOrEmpty(displayMarker)) ? (text.Substring(0, markerOffset) + displayMarker + text.Substring(markerOffset + markerLength)) : text);
			int length = stringBuilder.Length;
			for (int num4 = 0; num4 <= text.Length; num4++)
			{
				int num5 = ((markerLength == 0 && !string.IsNullOrEmpty(displayMarker)) ? ((num4 < markerOffset) ? (length + num4) : (length + displayMarker.Length + num4)) : ((num4 > markerOffset) ? ((num4 > markerOffset + markerLength) ? (length + markerOffset + displayMarker.Length + (num4 - markerOffset - markerLength)) : (length + markerOffset + displayMarker.Length)) : (length + num4)));
				rawToDisplayOffsetMap[num + num4] = num5;
			}
			stringBuilder.Append(value);
			for (int num6 = 1; num6 <= text2.Length; num6++)
			{
				rawToDisplayOffsetMap[num2 + num6] = stringBuilder.Length + num6;
			}
			stringBuilder.Append(text2);
			lineIndex++;
			if (j >= rawText.Length)
			{
				break;
			}
		}
		rawToDisplayOffsetMap[rawText.Length] = stringBuilder.Length;
		return stringBuilder.ToString();
	}

	private static string HideBlockMetadataCommentsForDisplay(string rawText, MarkdownSemanticDocument document, out int[] rawToVisibleOffsetMap)
	{
		rawText = rawText ?? string.Empty;
		rawToVisibleOffsetMap = new int[rawText.Length + 1];
		if (string.IsNullOrEmpty(rawText))
		{
			rawToVisibleOffsetMap[0] = 0;
			return string.Empty;
		}
		StringBuilder stringBuilder = new StringBuilder(rawText.Length);
		int i = 0;
		int lineIndex = 0;
		while (i <= rawText.Length)
		{
			int num = i;
			for (; i < rawText.Length && rawText[i] != '\r' && rawText[i] != '\n'; i++)
			{
			}
			int num2 = i;
			string text = rawText.Substring(num, num2 - num);
			int num3 = i;
			if (i < rawText.Length)
			{
				if (rawText[i] == '\r')
				{
					i++;
					if (i < rawText.Length && rawText[i] == '\n')
					{
						i++;
					}
				}
				else
				{
					i++;
				}
			}
			string text2 = rawText.Substring(num3, i - num3);
			MarkdownBlockNode semanticBlock = document?.Blocks?.FirstOrDefault((MarkdownBlockNode currentBlock) => currentBlock != null && currentBlock.StartLine <= lineIndex && currentBlock.EndLine >= lineIndex);
			List<OffsetRange> displayBlockMetadataCommentRangesToSkip = GetDisplayBlockMetadataCommentRangesToSkip(text, semanticBlock);
			string value = RemoveOffsetRanges(text, displayBlockMetadataCommentRangesToSkip);
			for (int num4 = 0; num4 <= text.Length; num4++)
			{
				int num5 = stringBuilder.Length + TranslateRawOffsetToDisplayOffset(num4, displayBlockMetadataCommentRangesToSkip);
				rawToVisibleOffsetMap[num + num4] = num5;
			}
			stringBuilder.Append(value);
			for (int num6 = 1; num6 <= text2.Length; num6++)
			{
				rawToVisibleOffsetMap[num2 + num6] = stringBuilder.Length + num6;
			}
			stringBuilder.Append(text2);
			lineIndex++;
			if (i >= rawText.Length)
			{
				break;
			}
		}
		rawToVisibleOffsetMap[rawText.Length] = stringBuilder.Length;
		return stringBuilder.ToString();
	}

	private static int[] ComposeOffsetMaps(int[] rawToVisibleOffsetMap, int[] visibleToDisplayOffsetMap)
	{
		if (rawToVisibleOffsetMap == null || rawToVisibleOffsetMap.Length == 0)
		{
			return visibleToDisplayOffsetMap;
		}
		if (visibleToDisplayOffsetMap == null || visibleToDisplayOffsetMap.Length == 0)
		{
			return rawToVisibleOffsetMap;
		}
		int[] array = new int[rawToVisibleOffsetMap.Length];
		for (int i = 0; i < rawToVisibleOffsetMap.Length; i++)
		{
			int num = Math.Max(0, Math.Min(rawToVisibleOffsetMap[i], visibleToDisplayOffsetMap.Length - 1));
			array[i] = visibleToDisplayOffsetMap[num];
		}
		return array;
	}

	private static string BuildParagraphIndentMetadataComment(int indent)
	{
		return "<!-- {\"indent\":" + Math.Max(0, indent) + "} -->";
	}

	private static string BuildHeadingFoldedMetadataComment()
	{
		return "<!-- {\"folded\":true} -->";
	}

	private static bool StartsWithParagraphIndentMarker(string lineText)
	{
		if (!string.IsNullOrEmpty(lineText))
		{
			return lineText[0] == '\u2065';
		}
		return false;
	}

	private static bool StartsWithLiteralTab(string lineText)
	{
		for (int i = 0; i < (lineText?.Length ?? 0); i++)
		{
			switch (lineText[i])
			{
			case '\t':
				return true;
			default:
				return false;
			case '\u00a0':
			case '\u2065':
				break;
			}
		}
		return false;
	}

	private static int AdjustCaretOffsetForParagraphIndentMutation(int caretOffset, int mutationOffset, bool insertingMarker)
	{
		if (insertingMarker)
		{
			if (caretOffset < mutationOffset)
			{
				return caretOffset;
			}
			return caretOffset + 1;
		}
		if (caretOffset <= mutationOffset)
		{
			return caretOffset;
		}
		return caretOffset - 1;
	}

	private bool TrySyncParagraphIndentMarkersWithSemanticState(int desiredCaretOffset, out int adjustedCaretOffset, bool startExternalImageDownloads = true)
	{
		adjustedCaretOffset = Math.Max(0, Math.Min(desiredCaretOffset, (EditBox?.Text?.Length).GetValueOrDefault()));
		if (EditBox?.Document == null)
		{
			return false;
		}
		List<DocumentMutation> list = new List<DocumentMutation>();
		for (int i = 1; i <= EditBox.Document.LineCount; i++)
		{
			DocumentLine lineByNumber = EditBox.Document.GetLineByNumber(i);
			if (lineByNumber == null)
			{
				continue;
			}
			int lineIndex = Math.Max(0, lineByNumber.LineNumber - 1);
			string text = EditBox.Document.GetText(lineByNumber);
			bool flag = StartsWithLiteralTab(text);
			MarkdownBlockNode block;
			bool flag2 = TryGetSemanticBlockAtLine(lineIndex, out block) && block != null && block.Kind == MarkdownBlockKind.Paragraph && block.Indent > 0 && !flag;
			bool flag3 = false;
			for (int j = 0; j < (text?.Length ?? 0); j++)
			{
				if (text[j] == '\u2065')
				{
					if (flag2 && j == 0 && !flag3)
					{
						flag3 = true;
						continue;
					}
					list.Add(new DocumentMutation
					{
						StartOffset = lineByNumber.Offset + j,
						RemoveLength = 1,
						InsertText = string.Empty
					});
					adjustedCaretOffset = AdjustCaretOffsetForParagraphIndentMutation(adjustedCaretOffset, lineByNumber.Offset + j, insertingMarker: false);
				}
			}
			if (flag2 && !flag3)
			{
				list.Add(new DocumentMutation
				{
					StartOffset = lineByNumber.Offset,
					RemoveLength = 0,
					InsertText = "\u2065"
				});
				adjustedCaretOffset = AdjustCaretOffsetForParagraphIndentMutation(adjustedCaretOffset, lineByNumber.Offset, insertingMarker: true);
			}
		}
		if (list.Count == 0)
		{
			adjustedCaretOffset = Math.Max(0, Math.Min(adjustedCaretOffset, EditBox.Text.Length));
			return false;
		}
		using (EditBox.Document.RunUpdate())
		{
			ApplyDocumentMutations(list);
		}
		RefreshEditingDerivedMetadataWithoutMarkdownParsing(startExternalImageDownloads);
		adjustedCaretOffset = Math.Max(0, Math.Min(adjustedCaretOffset, EditBox.Text.Length));
		return true;
	}

	private static bool TryGetDisplayBlockPrefixReplacement(string lineText, MarkdownBlockNode block, out int markerOffset, out int markerLength, out string displayMarker)
	{
		markerOffset = 0;
		markerLength = 0;
		displayMarker = string.Empty;
		if (block == null)
		{
			return false;
		}
		switch (block.Kind)
		{
		case MarkdownBlockKind.BlockQuote:
			if (!MarkdownBlockSyntax.TryGetBlockQuoteDisplayPrefixLength(lineText, out markerLength))
			{
				return false;
			}
			markerOffset = 0;
			displayMarker = string.Empty;
			return true;
		case MarkdownBlockKind.Heading:
		{
			int num2 = lineText.Length - lineText.TrimStart().Length;
			int num3 = Math.Max(1, Math.Min(6, block.Level ?? 1));
			markerOffset = num2;
			markerLength = num3 + 1;
			displayMarker = string.Empty;
			return true;
		}
		case MarkdownBlockKind.List:
		{
			int num4 = lineText.Length - lineText.TrimStart().Length;
			markerOffset = 0;
			markerLength = num4;
			switch (block.ListKind)
			{
			case MarkdownListKind.Task:
				markerLength += "- [ ] ".Length;
				displayMarker = string.Empty;
				return true;
			case MarkdownListKind.Bullet:
				markerLength += 2;
				displayMarker = string.Empty;
				return true;
			case MarkdownListKind.Numbered:
				markerLength += Math.Max(1, (block.CustomOrder ?? "1").Length) + 2;
				displayMarker = string.Empty;
				return true;
			}
			break;
		}
		case MarkdownBlockKind.Paragraph:
			if (block.Indent > 0 && !StartsWithLiteralTab(lineText))
			{
				markerOffset = 0;
				markerLength = 0;
				displayMarker = "\u2065";
				return true;
			}
			break;
		case MarkdownBlockKind.HorizontalRule:
		{
			int num = (markerOffset = lineText.Length - lineText.TrimStart().Length);
			markerLength = lineText.Length - num;
			displayMarker = "\u200c\u200d\u2060";
			return true;
		}
		case MarkdownBlockKind.CodeBlock:
			return false;
		}
		return false;
	}

	private static bool IsPersistedInlineShortcutMark(MarkdownMarkKind kind)
	{
		if (kind != MarkdownMarkKind.Strong && kind != MarkdownMarkKind.Emphasis && kind != MarkdownMarkKind.Underline && kind != MarkdownMarkKind.Strikethrough && kind != MarkdownMarkKind.Code && kind != MarkdownMarkKind.Highlight)
		{
			return kind == MarkdownMarkKind.Link;
		}
		return true;
	}

	private void ResetTransientSemanticEditingState()
	{
		ClearAllActiveTypingMarks();
		_suppressedTypingMarks.Clear();
		ClearBackspaceRetainedTypingMarks();
		_typedSemanticMarks.Clear();
		_needsTypedSemanticMarkRedraw = false;
	}

	private void ClearSemanticStyleStateForEmptyText()
	{
		_activeTypingMarks?.Clear();
		_activeTypingMarkOffsets?.Clear();
		_suppressedTypingMarks?.Clear();
		_activeTypingHighlightColor = null;
		_activeTypingHighlightOffset = -1;
		_backspaceRetainedTypingMarks?.Clear();
		_backspaceRetainedTypingMarkOffset = -1;
		_typedSemanticMarks?.Clear();
		SemanticMarkList?.Clear();
		_needsTypedSemanticMarkRedraw = false;
	}

	private bool TryClearSemanticStyleStateForRepeatedEmptyDeletion()
	{
		if (!string.IsNullOrEmpty(EditBox?.Text))
		{
			return false;
		}
		TextDocument textDocument = EditBox?.Document;
		if (textDocument != null)
		{
			int offset = Math.Max(0, Math.Min(EditBox.CaretOffset, textDocument.TextLength));
			DocumentLine lineByOffset = textDocument.GetLineByOffset(offset);
			if (lineByOffset != null && TryGetSemanticBlockAtLine(Math.Max(0, lineByOffset.LineNumber - 1), out var block) && !CanClearInlineStyleStateForRepeatedEmptyDeletion(block))
			{
				return false;
			}
		}
		ClearSemanticStyleStateForEmptyText();
		return true;
	}

	private static bool CanClearInlineStyleStateForRepeatedEmptyDeletion(MarkdownBlockNode block)
	{
		if (block != null)
		{
			return block.Kind == MarkdownBlockKind.Paragraph;
		}
		return true;
	}

	private void ResetSemanticHistoryToCurrentState()
	{
		_semanticUndoHistory.Clear();
		_semanticRedoHistory.Clear();
		CaptureSemanticHistorySnapshot(clearRedo: false);
	}

	private void ApplyPreparedEditorState(PreparedEditorTextState prepared, string persistedText, bool restoreOffset, bool resetHistory, int? explicitCaretOffset = null)
	{
		if (prepared == null)
		{
			prepared = new PreparedEditorTextState();
		}
		persistedText = persistedText ?? string.Empty;
		ResetTransientSemanticEditingState();
		_typedSemanticMarks.AddRange(prepared.SemanticMarks);
		SemanticMarkList.Clear();
		LinkNameDict.Clear();
		LinkUrlDict.Clear();
		AttachmentDict.Clear();
		AttachmentEndSet.Clear();
		CodeDict.Clear();
		EmojiDict.Clear();
		_semanticDocument = null;
		_semanticBlocksByLine = null;
		UnRegisterCaretChanged();
		UnRegisterInputHandler();
		EditBox.TextChanged -= EditBoxOnTextChanged;
		ClearHeadingFoldingsBeforeTextReset();
		bool suppressed = ContainsDisplayBlockPlaceholder(prepared.DisplayText) && BeginPlaceholderOpacitySuppression();
		bool suppressExternalTextChanged = _suppressExternalTextChanged;
		_suppressExternalTextChanged = true;
		try
		{
			if (restoreOffset)
			{
				EditBox.SetTextAndOffset(prepared.DisplayText, restoreOffset: true);
			}
			else
			{
				EditBox.Text = prepared.DisplayText;
			}
			AfterTextSet();
			_seedSemanticDocumentForRefresh = prepared.SemanticDocument;
			RefreshEditingStateWithoutMarkdownParsing(EditBox.Text);
			int num = explicitCaretOffset ?? EditBox.CaretOffset;
			int adjustedCaretOffset;
			bool flag = TrySyncParagraphIndentMarkersWithSemanticState(num, out adjustedCaretOffset, startExternalImageDownloads: false);
			if (explicitCaretOffset.HasValue || flag)
			{
				int val = (flag ? adjustedCaretOffset : num);
				EditBox.CaretOffset = Math.Max(0, Math.Min(val, EditBox.Text.Length));
			}
			RebuildPreparedLinkAndAttachmentState(prepared);
			_imageElementGenerator?.DiagnosticTaskContentLoaded();
			UpdateBindingTextFromEditor(persistedText);
			RequestSafeRedraw();
			RefreshCodeBlockLanguageOverlayAfterSemanticRefresh();
		}
		finally
		{
			EditBox.TextChanged += EditBoxOnTextChanged;
			RegisterInputHandler();
			RegisterCaretChanged();
			_suppressExternalTextChanged = suppressExternalTextChanged;
			EndPlaceholderOpacitySuppression(suppressed);
		}
		if (resetHistory)
		{
			ResetSemanticHistoryToCurrentState();
		}
	}

	private void RebuildPreparedLinkAndAttachmentState(PreparedEditorTextState prepared)
	{
		if (prepared != null && prepared.MetadataSourceText != null)
		{
			string sourceDisplayText = prepared.MetadataDisplayText ?? prepared.DisplayText ?? string.Empty;
			string text = EditBox?.Text ?? string.Empty;
			int[] array = BuildDisplayOnlyMarkerOffsetMap(sourceDisplayText, text);
			if (array != null)
			{
				string metadataSourceText = prepared.MetadataSourceText;
				int[] persistedToDisplayOffsets = ComposeOffsetMaps(prepared.RawToDisplayOffsetMap, array);
				IReadOnlyCollection<string> referencedInternalAttachmentIds = GetReferencedInternalAttachmentIds(metadataSourceText);
				RebuildEditingLinkAndAttachmentState(metadataSourceText, persistedToDisplayOffsets, startExternalImageDownloads: false);
				SchedulePreparedAttachmentMetadataDiagnostics(referencedInternalAttachmentIds, text);
			}
		}
	}

	internal static int[] BuildDisplayOnlyMarkerOffsetMap(string sourceDisplayText, string currentDisplayText)
	{
		sourceDisplayText = sourceDisplayText ?? string.Empty;
		currentDisplayText = currentDisplayText ?? string.Empty;
		int[] array = new int[sourceDisplayText.Length + 1];
		int num = 0;
		int i = 0;
		char c = '\u2065';
		while (true)
		{
			if (i < currentDisplayText.Length && currentDisplayText[i] == c && (num >= sourceDisplayText.Length || sourceDisplayText[num] != c))
			{
				i++;
				continue;
			}
			array[num] = i;
			if (num >= sourceDisplayText.Length)
			{
				for (; i < currentDisplayText.Length && currentDisplayText[i] == c; i++)
				{
				}
				array[num] = i;
				if (i != currentDisplayText.Length)
				{
					return null;
				}
				return array;
			}
			if (i >= currentDisplayText.Length)
			{
				while (num < sourceDisplayText.Length && sourceDisplayText[num] == c)
				{
					num++;
					array[num] = i;
				}
				if (num != sourceDisplayText.Length)
				{
					return null;
				}
				return array;
			}
			if (sourceDisplayText[num] == currentDisplayText[i])
			{
				num++;
				i++;
				continue;
			}
			if (sourceDisplayText[num] == c)
			{
				num++;
				array[num] = i;
				continue;
			}
			if (currentDisplayText[i] != c)
			{
				break;
			}
			i = (array[num] = i + 1);
		}
		return null;
	}

	private void SchedulePreparedAttachmentMetadataDiagnostics(IReadOnlyCollection<string> expectedInternalAttachmentIds, string expectedDisplayText)
	{
		if (string.IsNullOrEmpty(_editingTaskId) || expectedInternalAttachmentIds == null || expectedInternalAttachmentIds.Count == 0)
		{
			return;
		}
		string taskId = _editingTaskId;
		List<string> expectedAttachmentIds = expectedInternalAttachmentIds.ToList();
		base.Dispatcher?.BeginInvoke((Action)delegate
		{
			if (!string.Equals(_editingTaskId, taskId, StringComparison.Ordinal) || !string.Equals(EditBox?.Text ?? string.Empty, expectedDisplayText ?? string.Empty, StringComparison.Ordinal))
			{
				return;
			}
			string displayText = EditBox?.Text ?? string.Empty;
			foreach (string attachmentId in expectedAttachmentIds)
			{
				if (!AttachmentDict.Values.Any(delegate(AttachmentInfo actual)
				{
					if (actual == null || !string.Equals(AttachmentDao.GetInternalAttachmentId(actual.Url), attachmentId, StringComparison.OrdinalIgnoreCase))
					{
						return false;
					}
					return actual.Offset >= 0 && actual.Value != null && actual.Offset + actual.Value.Length <= displayText.Length && string.CompareOrdinal(displayText, actual.Offset, actual.Value, 0, actual.Value.Length) == 0;
				}))
				{
					AttachmentRenderDiagnostic.Warn("editor-state", taskId, attachmentId, "metadata_missing_after_apply", null, $"metadataCount:{AttachmentDict.Count}, textLength:{displayText.Length}");
				}
			}
		}, DispatcherPriority.ContextIdle);
	}

	private void ClearHeadingFoldingsBeforeTextReset()
	{
		_searchHeadingDisplayFoldedPreviousValues.Clear();
		_searchExpansionSuppressedHeadings.Clear();
		_searchHeadingExpansionKey = null;
		if (_headingFoldingManager != null)
		{
			_headingFoldingManager.Clear();
		}
	}

	public void ReapplyPersistedEditorStatePreservingHistory(string persistedText, int caretOffset)
	{
		if (EditBox?.Document != null)
		{
			string text = persistedText ?? string.Empty;
			PreparedEditorTextState prepared = PrepareEditorTextState(text);
			ApplyPreparedEditorState(prepared, text, restoreOffset: false, resetHistory: false, caretOffset);
			RequestImmediateRedraw();
		}
	}

	private static bool ContainsDisplayBlockPlaceholder(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		if (!ContainsSuppressibleDisplayBlockPlaceholder(text) && text.IndexOf('\ue000') < 0)
		{
			return text.IndexOf("\u200c\u200d\u2060", StringComparison.Ordinal) >= 0;
		}
		return true;
	}

	private static bool ContainsSuppressibleDisplayBlockPlaceholder(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		if (text.IndexOf("\u200b\u200c\u200d \u200e\u200f", StringComparison.Ordinal) < 0 && text.IndexOf("\u200b\u200c\u200d\u2060\u200e\u200f", StringComparison.Ordinal) < 0 && text.IndexOf("\u2061\u2062", StringComparison.Ordinal) < 0 && text.IndexOf("\u2063\u2064", StringComparison.Ordinal) < 0 && text.IndexOf("\u2064\u2063", StringComparison.Ordinal) < 0 && text.IndexOf("\u2061\u2064", StringComparison.Ordinal) < 0 && text.IndexOf("\u206a ", StringComparison.Ordinal) < 0 && text.IndexOf("\u200b ", StringComparison.Ordinal) < 0)
		{
			return text.IndexOf("\u2065", StringComparison.Ordinal) >= 0;
		}
		return true;
	}

	private bool TryBeginPlaceholderMutationSuppression(IEnumerable<DocumentMutation> mutations)
	{
		if (mutations == null || EditBox == null)
		{
			return false;
		}
		foreach (DocumentMutation mutation in mutations)
		{
			if (mutation != null && ContainsSuppressibleDisplayBlockPlaceholder(mutation.InsertText))
			{
				return BeginPlaceholderOpacitySuppression();
			}
		}
		return false;
	}

	private bool BeginPlaceholderOpacitySuppression()
	{
		if (EditBox == null)
		{
			return false;
		}
		if (_placeholderOpacityRestoreOperation != null && _placeholderOpacityRestoreOperation.Status == DispatcherOperationStatus.Pending)
		{
			_placeholderOpacityRestoreOperation.Abort();
		}
		_placeholderOpacityRestoreOperation = null;
		if (!_placeholderOpacitySuppressionActive)
		{
			_placeholderOpacityBeforeSuppression = ((EditBox.Opacity > 0.01) ? EditBox.Opacity : 1.0);
			_placeholderOpacitySuppressionActive = true;
		}
		EditBox.Opacity = 0.0;
		return true;
	}

	private void EndPlaceholderOpacitySuppression(bool suppressed)
	{
		TextEditor editBox = EditBox;
		if (!suppressed || editBox == null)
		{
			return;
		}
		if (_placeholderOpacityRestoreOperation != null && _placeholderOpacityRestoreOperation.Status == DispatcherOperationStatus.Pending)
		{
			_placeholderOpacityRestoreOperation.Abort();
		}
		double restoreOpacity = ((_placeholderOpacityBeforeSuppression > 0.01) ? _placeholderOpacityBeforeSuppression : 1.0);
		Dispatcher dispatcher = editBox.Dispatcher;
		if (dispatcher == null || dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
		{
			editBox.Opacity = restoreOpacity;
			_placeholderOpacitySuppressionActive = false;
			_placeholderOpacityRestoreOperation = null;
			RequestSafeRedraw();
			return;
		}
		_placeholderOpacityRestoreOperation = dispatcher.BeginInvoke((Action)delegate
		{
			editBox.Opacity = restoreOpacity;
			_placeholderOpacitySuppressionActive = false;
			_placeholderOpacityRestoreOperation = null;
			RequestSafeRedraw();
		}, DispatcherPriority.Render);
	}

	private void CaptureSemanticHistorySnapshot(bool clearRedo = true)
	{
		CaptureSemanticHistorySnapshotCore(clearRedo, null);
	}

	private void CaptureSemanticHistorySnapshotWithPersistedText(string persistedText)
	{
		CaptureSemanticHistorySnapshotCore(clearRedo: true, persistedText);
	}

	private void CaptureSemanticHistorySnapshotCore(bool clearRedo, string persistedText)
	{
		if (!_suppressSemanticHistory)
		{
			EditorHistoryState editorHistoryState = new EditorHistoryState
			{
				PersistedText = (persistedText ?? GetPersistedText()),
				DisplayText = (EditBox?.Text ?? string.Empty),
				SemanticDocument = CloneSemanticDocument(_semanticDocument),
				CaretOffset = Math.Max(0, Math.Min(EditBox?.CaretOffset ?? 0, (EditBox?.Text?.Length).GetValueOrDefault()))
			};
			editorHistoryState.TypedSemanticMarks.AddRange(CloneDisplaySegments(_typedSemanticMarks));
			AddSemanticHistorySnapshot(editorHistoryState, clearRedo);
		}
	}

	private void AddSemanticHistorySnapshot(EditorHistoryState snapshot, bool clearRedo = true)
	{
		if (snapshot == null)
		{
			return;
		}
		EditorHistoryState editorHistoryState = _semanticUndoHistory.LastOrDefault();
		if (editorHistoryState == null || !string.Equals(editorHistoryState.PersistedText, snapshot.PersistedText, StringComparison.Ordinal) || !string.Equals(editorHistoryState.DisplayText, snapshot.DisplayText, StringComparison.Ordinal) || editorHistoryState.CaretOffset != snapshot.CaretOffset)
		{
			_semanticUndoHistory.Add(snapshot);
			if (_semanticUndoHistory.Count > 200)
			{
				_semanticUndoHistory.RemoveAt(0);
			}
			if (clearRedo)
			{
				_semanticRedoHistory.Clear();
			}
		}
	}

	private bool TryPerformSemanticUndo()
	{
		if (_semanticUndoHistory.Count <= 1)
		{
			return false;
		}
		EditorHistoryState editorHistoryState = new EditorHistoryState
		{
			PersistedText = GetPersistedText(),
			DisplayText = (EditBox?.Text ?? string.Empty),
			SemanticDocument = CloneSemanticDocument(_semanticDocument),
			CaretOffset = Math.Max(0, Math.Min(EditBox?.CaretOffset ?? 0, (EditBox?.Text?.Length).GetValueOrDefault()))
		};
		editorHistoryState.TypedSemanticMarks.AddRange(CloneDisplaySegments(_typedSemanticMarks));
		_semanticRedoHistory.Add(editorHistoryState);
		_semanticUndoHistory.RemoveAt(_semanticUndoHistory.Count - 1);
		return ApplySemanticHistoryState(_semanticUndoHistory.LastOrDefault());
	}

	private bool TryPerformSemanticRedo()
	{
		if (_semanticRedoHistory.Count == 0)
		{
			return false;
		}
		EditorHistoryState editorHistoryState = _semanticRedoHistory.Last();
		_semanticRedoHistory.RemoveAt(_semanticRedoHistory.Count - 1);
		_semanticUndoHistory.Add(CloneHistoryState(editorHistoryState));
		return ApplySemanticHistoryState(editorHistoryState);
	}

	private bool ApplySemanticHistoryState(EditorHistoryState state)
	{
		if (state == null)
		{
			return false;
		}
		_suppressSemanticHistory = true;
		try
		{
			PreparedEditorTextState preparedEditorTextState = PrepareEditorTextState(state.PersistedText ?? string.Empty);
			preparedEditorTextState.DisplayText = state.DisplayText ?? preparedEditorTextState.DisplayText ?? string.Empty;
			preparedEditorTextState.SemanticDocument = CloneSemanticDocument(state.SemanticDocument);
			preparedEditorTextState.SemanticMarks.Clear();
			preparedEditorTextState.SemanticMarks.AddRange(CloneDisplaySegments(state.TypedSemanticMarks));
			ApplyPreparedEditorState(preparedEditorTextState, state.PersistedText ?? string.Empty, restoreOffset: false, resetHistory: false, state.CaretOffset);
			RefreshOpenMenuContexts();
			RequestSafeRedraw();
			NotifyTextChanged(this, EventArgs.Empty);
		}
		finally
		{
			_suppressSemanticHistory = false;
		}
		return true;
	}

	private List<TextRegion> GetChangedRegions(string oldText, string newText)
	{
		List<TextRegion> list = new List<TextRegion>();
		List<Diff> list2 = new diff_match_patch().diff_main(oldText, newText);
		int num = 0;
		foreach (Diff item in list2)
		{
			if (item.operation != Operation.EQUAL)
			{
				list.Add(new TextRegion
				{
					Start = Math.Max(0, num - 50),
					End = Math.Min(newText.Length, num + item.text.Length + 50)
				});
			}
			if (item.operation != Operation.DELETE)
			{
				num += item.text.Length;
			}
		}
		return MergeRegions(list);
	}

	private List<TextRegion> MergeRegions(List<TextRegion> regions)
	{
		if (regions.Count <= 1)
		{
			return regions;
		}
		regions.Sort((TextRegion a, TextRegion b) => a.Start.CompareTo(b.Start));
		List<TextRegion> list = new List<TextRegion>();
		TextRegion textRegion = regions[0];
		for (int num = 1; num < regions.Count; num++)
		{
			if (regions[num].Start <= textRegion.End)
			{
				textRegion.End = Math.Max(textRegion.End, regions[num].End);
				continue;
			}
			list.Add(textRegion);
			textRegion = regions[num];
		}
		list.Add(textRegion);
		return list;
	}

	private static string ExpandReverseDelimiterCluster(string text, int insertOffset, string insertedText)
	{
		if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(insertedText) || insertOffset < 0 || insertOffset >= text.Length)
		{
			return insertedText;
		}
		char delimiterChar = insertedText[0];
		if (insertedText.Any((char ch) => ch != delimiterChar))
		{
			return insertedText;
		}
		int num;
		for (num = insertedText.Length; insertOffset + num < text.Length && num < 3 && text[insertOffset + num] == delimiterChar; num++)
		{
		}
		return text.Substring(insertOffset, num);
	}

	private void RefreshEditingSemanticStateWithoutMarkdownParsing(string text)
	{
		string text2 = text ?? string.Empty;
		if (_seedSemanticDocumentForRefresh != null)
		{
			_semanticDocument = CloneSemanticDocument(_seedSemanticDocumentForRefresh);
			SyncCodeBlocksFromDisplayText(_semanticDocument, text2);
		}
		else
		{
			_semanticDocument = BuildEditingSemanticDocument(text2);
		}
		SyncEditingParagraphInlineText(_semanticDocument, text2);
		_semanticBlocksByLine = BuildSemanticBlockIndex(_semanticDocument);
		_seedSemanticDocumentForRefresh = null;
		UpdateHeadingFoldings();
	}

	private MarkdownSemanticDocument BuildEditingSemanticDocument(string text)
	{
		MarkdownSemanticDocument markdownSemanticDocument = new MarkdownSemanticDocument();
		string[] array = (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
		MarkdownBlockNode markdownBlockNode = null;
		for (int i = 0; i < array.Length; i++)
		{
			string text2 = array[i] ?? string.Empty;
			int num = text2.Length - text2.TrimStart().Length;
			string text3 = text2.TrimStart();
			int num2 = ((!IsLiteralTabOnlySemanticLine(text2) && (StartsWithParagraphIndentMarker(text2) || num > 0 || string.IsNullOrEmpty(text3) || ShouldPreserveStructuredBlockIndentForLine(i))) ? GetPreservedSemanticIndentForLine(i, num) : 0);
			int closingLineIndex;
			int level;
			bool? folded;
			bool isChecked;
			string numberText;
			int indent;
			string language;
			MarkdownBlockNode block2;
			if (markdownBlockNode == null && TryGetPreservedSemanticCodeBlockStartingAtLine(i, array.Length, array, out var block))
			{
				markdownSemanticDocument.Blocks.Add(block);
				i = block.EndLine;
			}
			else if (markdownBlockNode != null)
			{
				markdownBlockNode.EndLine = i;
				if (!IsLiteralEditingCodeFence(i, text2) && TryGetEditingCodeFenceClosingInfo(text2, out closingLineIndex))
				{
					markdownBlockNode = null;
					continue;
				}
				string text4 = markdownBlockNode.CodeText ?? string.Empty;
				markdownBlockNode.CodeText = (string.IsNullOrEmpty(text4) ? text2 : (text4 + "\n" + text2));
			}
			else if (TryGetSeedHeadingForLine(i, out level, out folded))
			{
				markdownSemanticDocument.Blocks.Add(new MarkdownBlockNode
				{
					Kind = MarkdownBlockKind.Heading,
					StartLine = i,
					EndLine = i,
					Indent = num2,
					Level = level,
					Folded = folded
				});
			}
			else if (TryGetSeedBlockQuoteForLine(i))
			{
				markdownSemanticDocument.Blocks.Add(new MarkdownBlockNode
				{
					Kind = MarkdownBlockKind.BlockQuote,
					StartLine = i,
					EndLine = i,
					Indent = num2
				});
			}
			else if (TryGetSeedBulletListForLine(i))
			{
				markdownSemanticDocument.Blocks.Add(new MarkdownBlockNode
				{
					Kind = MarkdownBlockKind.List,
					StartLine = i,
					EndLine = i,
					Indent = num2,
					ListKind = MarkdownListKind.Bullet
				});
			}
			else if (TryGetSeedTaskListForLine(i, out isChecked))
			{
				markdownSemanticDocument.Blocks.Add(new MarkdownBlockNode
				{
					Kind = MarkdownBlockKind.List,
					StartLine = i,
					EndLine = i,
					Indent = num2,
					ListKind = MarkdownListKind.Task,
					Checked = isChecked
				});
			}
			else if (TryGetSeedOrderedListForLine(i, out numberText))
			{
				markdownSemanticDocument.Blocks.Add(new MarkdownBlockNode
				{
					Kind = MarkdownBlockKind.List,
					StartLine = i,
					EndLine = i,
					Indent = num2,
					ListKind = MarkdownListKind.Numbered,
					CustomOrder = numberText
				});
			}
			else if (!IsLiteralEditingCodeFence(i, text2) && TryGetEditingCodeFenceInfo(text2, out indent, out language))
			{
				if (!TryFindEditingCodeFenceClosingLine(array, i + 1, out closingLineIndex))
				{
					markdownSemanticDocument.Blocks.Add(CreateEditingParagraphBlock(i, num2, text2));
					continue;
				}
				markdownBlockNode = new MarkdownBlockNode
				{
					Kind = MarkdownBlockKind.CodeBlock,
					StartLine = i,
					EndLine = i,
					Indent = Math.Max(num2, indent),
					Language = (string.IsNullOrWhiteSpace(language) ? null : language),
					CodeText = string.Empty
				};
				markdownSemanticDocument.Blocks.Add(markdownBlockNode);
			}
			else if (MarkdownBlockSyntax.IsSplitLine(text2) || string.Equals(text3, "\u200c\u200d\u2060", StringComparison.Ordinal))
			{
				markdownSemanticDocument.Blocks.Add(new MarkdownBlockNode
				{
					Kind = MarkdownBlockKind.HorizontalRule,
					StartLine = i,
					EndLine = i,
					Indent = num2
				});
			}
			else if (TryCreateEditingFileBlock(i, text2, out block2))
			{
				markdownSemanticDocument.Blocks.Add(block2);
			}
			else
			{
				markdownSemanticDocument.Blocks.Add(CreateEditingParagraphBlock(i, num2, text2));
			}
		}
		return markdownSemanticDocument;
	}

	private static bool TryCreateEditingFileBlock(int lineIndex, string lineText, out MarkdownBlockNode block)
	{
		block = null;
		if (lineIndex < 0 || string.IsNullOrEmpty(lineText) || (!lineText.StartsWith("![image](", StringComparison.Ordinal) && !lineText.StartsWith("![file](", StringComparison.Ordinal)))
		{
			return false;
		}
		MarkdownBlockNode markdownBlockNode = MarkdownSemanticConverter.Parse(lineText)?.Blocks?.FirstOrDefault();
		if (markdownBlockNode == null || markdownBlockNode.Kind != MarkdownBlockKind.FileBlock)
		{
			return false;
		}
		block = new MarkdownBlockNode
		{
			Kind = MarkdownBlockKind.FileBlock,
			StartLine = lineIndex,
			EndLine = lineIndex,
			Indent = markdownBlockNode.Indent,
			FileKind = markdownBlockNode.FileKind,
			Source = markdownBlockNode.Source,
			AltText = markdownBlockNode.AltText,
			Caption = markdownBlockNode.Caption
		};
		return true;
	}

	private static MarkdownBlockNode CreateEditingParagraphBlock(int lineIndex, int indent, string lineText)
	{
		MarkdownBlockNode markdownBlockNode = new MarkdownBlockNode
		{
			Kind = MarkdownBlockKind.Paragraph,
			StartLine = lineIndex,
			EndLine = lineIndex,
			Indent = indent
		};
		if (!IsEffectivelyEmptySemanticLine(lineText))
		{
			markdownBlockNode.Inlines.Add(new MarkdownInlineNode
			{
				Text = (lineText ?? string.Empty),
				SourceStart = 0,
				SourceLength = (lineText?.Length ?? 0)
			});
		}
		return markdownBlockNode;
	}

	private static void SyncEditingParagraphInlineText(MarkdownSemanticDocument document, string text)
	{
		if (document?.Blocks == null)
		{
			return;
		}
		string[] array = (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
		foreach (MarkdownBlockNode block in document.Blocks)
		{
			if (block == null || block.Kind != MarkdownBlockKind.Paragraph)
			{
				continue;
			}
			block.Inlines.Clear();
			int num = Math.Max(0, block.StartLine);
			int num2 = Math.Min(array.Length - 1, Math.Max(num, block.EndLine));
			for (int i = num; i <= num2; i++)
			{
				string text2 = array[i] ?? string.Empty;
				if (IsLiteralTabOnlySemanticLine(text2))
				{
					block.Indent = 0;
				}
				if (!IsEffectivelyEmptySemanticLine(text2))
				{
					block.Inlines.Add(new MarkdownInlineNode
					{
						Text = text2,
						SourceStart = 0,
						SourceLength = text2.Length
					});
				}
			}
		}
	}

	private bool TryFindEditingCodeFenceClosingLine(string[] lines, int startLineIndex, out int closingLineIndex)
	{
		closingLineIndex = -1;
		if (lines == null || startLineIndex < 0)
		{
			return false;
		}
		for (int i = startLineIndex; i < lines.Length; i++)
		{
			if (IsPreservedSemanticCodeBlockLine(i))
			{
				return false;
			}
			if (!IsLiteralEditingCodeFence(i, lines[i] ?? string.Empty) && TryGetEditingCodeFenceClosingInfo(lines[i] ?? string.Empty, out var _))
			{
				closingLineIndex = i;
				return true;
			}
		}
		return false;
	}

	private bool IsLiteralEditingCodeFence(int lineIndex, string lineText)
	{
		TextDocument textDocument = EditBox?.Document;
		if (textDocument == null || lineIndex < 0 || lineIndex >= textDocument.LineCount)
		{
			return false;
		}
		int num = (lineText ?? string.Empty).IndexOf("```", StringComparison.Ordinal);
		if (num < 0)
		{
			return false;
		}
		int start = textDocument.GetLineByNumber(lineIndex + 1).Offset + num;
		int end = start + "```".Length;
		return SemanticMarkList.Concat(_typedSemanticMarks).Any((MarkdownInlineDisplaySegment mark) => mark != null && mark.Kind == MarkdownMarkKind.LiteralInlineSyntax && mark.SourceLength > 0 && mark.SourcePosition < end && mark.SourcePosition + mark.SourceLength > start);
	}

	private static bool TryGetEditingCodeFenceClosingInfo(string lineText, out int indent)
	{
		if (TryGetEditingCodeFenceInfo(lineText, out indent, out var language) && string.IsNullOrWhiteSpace(language))
		{
			return true;
		}
		indent = 0;
		return false;
	}

	private static bool TryGetEditingCodeFenceInfo(string lineText, out int indent, out string language)
	{
		indent = 0;
		language = string.Empty;
		string text = NormalizeDisplayTaskMarkerLine(lineText ?? string.Empty);
		MarkdownBlockSyntaxInfo markdownBlockSyntaxInfo = MarkdownBlockSyntax.Parse(text);
		if (markdownBlockSyntaxInfo.Kind != MarkdownBlockSyntaxKind.CodeFence)
		{
			return false;
		}
		indent = ConvertIndentLevelsToWhitespaceCount(markdownBlockSyntaxInfo.Indent);
		string text2 = text.TrimStart();
		language = ((text2.Length > "```".Length) ? text2.Substring("```".Length).Trim() : string.Empty);
		return true;
	}

	private bool TryGetPreservedSemanticCodeBlockStartingAtLine(int lineIndex, int lineCount, string[] lines, out MarkdownBlockNode block)
	{
		block = null;
		if (lineIndex < 0 || lineCount <= 0 || !TryGetPreservedSemanticBlockForLine(lineIndex, out var block2) || block2 == null || block2.Kind != MarkdownBlockKind.CodeBlock || block2.StartLine != lineIndex)
		{
			return false;
		}
		int num = Math.Max(0, Math.Min(block2.StartLine, lineCount - 1));
		if (num != lineIndex)
		{
			return false;
		}
		int endLineIndex = Math.Max(num, Math.Min(block2.EndLine, lineCount - 1));
		block = CloneSemanticBlockAsLineRange(block2, num, endLineIndex);
		SyncCodeBlockFromDisplayLines(block, lines);
		return true;
	}

	private bool IsPreservedSemanticCodeBlockLine(int lineIndex)
	{
		if (TryGetPreservedSemanticBlockForLine(lineIndex, out var block))
		{
			if (block == null)
			{
				return false;
			}
			return block.Kind == MarkdownBlockKind.CodeBlock;
		}
		return false;
	}

	private bool TryGetSeedHeadingLevelForLine(int lineIndex, out int level)
	{
		bool? folded;
		return TryGetSeedHeadingForLine(lineIndex, out level, out folded);
	}

	private bool ShouldPreserveStructuredBlockIndentForLine(int lineIndex)
	{
		if (lineIndex < 0)
		{
			return false;
		}
		if (TryGetPreservedSemanticBlockForLine(lineIndex, out var block) && block != null && block.Indent > 0 && block.Kind != MarkdownBlockKind.Paragraph)
		{
			return true;
		}
		return false;
	}

	private bool TryGetPreservedSemanticBlockForLine(int lineIndex, out MarkdownBlockNode block)
	{
		block = null;
		if (lineIndex < 0)
		{
			return false;
		}
		MarkdownBlockNode markdownBlockNode = _seedSemanticDocumentForRefresh?.Blocks?.FirstOrDefault((MarkdownBlockNode candidate) => candidate != null && candidate.StartLine <= lineIndex && candidate.EndLine >= lineIndex);
		if (markdownBlockNode != null)
		{
			block = markdownBlockNode;
			return true;
		}
		if (_semanticBlocksByLine != null && _semanticBlocksByLine.TryGetValue(lineIndex + 1, out var value) && value != null)
		{
			block = value;
			return true;
		}
		block = _semanticDocument?.Blocks?.FirstOrDefault((MarkdownBlockNode candidate) => candidate != null && candidate.StartLine <= lineIndex && candidate.EndLine >= lineIndex);
		if (block != null)
		{
			return true;
		}
		return false;
	}

	private bool TryGetSeedHeadingForLine(int lineIndex, out int level, out bool? folded)
	{
		level = 0;
		folded = null;
		if (lineIndex < 0)
		{
			return false;
		}
		List<MarkdownBlockNode> list = _seedSemanticDocumentForRefresh?.Blocks;
		if (list != null)
		{
			MarkdownBlockNode markdownBlockNode = list.FirstOrDefault((MarkdownBlockNode block) => block != null && block.Kind == MarkdownBlockKind.Heading && block.Level.HasValue && block.StartLine <= lineIndex && block.EndLine >= lineIndex);
			if (markdownBlockNode != null)
			{
				level = Math.Max(1, Math.Min(6, markdownBlockNode.Level ?? 1));
				folded = markdownBlockNode.Folded;
				return true;
			}
		}
		if (_semanticBlocksByLine != null && _semanticBlocksByLine.TryGetValue(lineIndex + 1, out var value) && value != null && value.Kind == MarkdownBlockKind.Heading && value.Level.HasValue)
		{
			level = Math.Max(1, Math.Min(6, value.Level ?? 1));
			folded = value.Folded;
			return true;
		}
		return false;
	}

	private bool TryGetSemanticHeadingLevelForLine(int lineIndex, out int level)
	{
		level = 0;
		if (lineIndex < 0)
		{
			return false;
		}
		if (_semanticBlocksByLine != null && _semanticBlocksByLine.TryGetValue(lineIndex + 1, out var value) && value != null && value.Kind == MarkdownBlockKind.Heading && value.Level.HasValue)
		{
			level = Math.Max(1, Math.Min(6, value.Level ?? 1));
			return true;
		}
		return false;
	}

	private int? GetSemanticHeadingLevelForDocumentLine(DocumentLine line)
	{
		if (line == null)
		{
			return null;
		}
		int lineIndex = line.LineNumber - 1;
		if (!TryGetSemanticBlockAtLine(lineIndex, out var block) || block == null)
		{
			return 0;
		}
		return (block.Kind == MarkdownBlockKind.Heading && block.Level.HasValue) ? Math.Max(1, Math.Min(6, block.Level.Value)) : 0;
	}

	private bool TryGetSemanticBlockAtLine(int lineIndex, out MarkdownBlockNode block)
	{
		block = null;
		if (lineIndex < 0)
		{
			return false;
		}
		if (_semanticBlocksByLine != null && _semanticBlocksByLine.TryGetValue(lineIndex + 1, out var value) && value != null)
		{
			block = value;
			return true;
		}
		block = _semanticDocument?.Blocks?.FirstOrDefault((MarkdownBlockNode candidate) => candidate != null && candidate.StartLine <= lineIndex && candidate.EndLine >= lineIndex);
		if (block != null)
		{
			return true;
		}
		return false;
	}

	private bool TryGetSeedBlockQuoteForLine(int lineIndex)
	{
		if (lineIndex < 0)
		{
			return false;
		}
		List<MarkdownBlockNode> list = _seedSemanticDocumentForRefresh?.Blocks;
		if (list != null && list.Any((MarkdownBlockNode block) => block != null && block.Kind == MarkdownBlockKind.BlockQuote && block.StartLine <= lineIndex && block.EndLine >= lineIndex))
		{
			return true;
		}
		if (_semanticBlocksByLine != null && _semanticBlocksByLine.TryGetValue(lineIndex + 1, out var value) && value != null && value.Kind == MarkdownBlockKind.BlockQuote)
		{
			return true;
		}
		return false;
	}

	private bool TryGetSemanticBlockQuoteForLine(int lineIndex)
	{
		if (TryGetSemanticBlockAtLine(lineIndex, out var block))
		{
			return block.Kind == MarkdownBlockKind.BlockQuote;
		}
		return false;
	}

	private bool TryGetSeedBulletListForLine(int lineIndex)
	{
		if (lineIndex < 0)
		{
			return false;
		}
		List<MarkdownBlockNode> list = _seedSemanticDocumentForRefresh?.Blocks;
		if (list != null && list.Any((MarkdownBlockNode block) => block != null && block.Kind == MarkdownBlockKind.List && block.ListKind == MarkdownListKind.Bullet && block.StartLine <= lineIndex && block.EndLine >= lineIndex))
		{
			return true;
		}
		if (_semanticBlocksByLine != null && _semanticBlocksByLine.TryGetValue(lineIndex + 1, out var value) && value != null && value.Kind == MarkdownBlockKind.List && value.ListKind == MarkdownListKind.Bullet)
		{
			return true;
		}
		return false;
	}

	private bool TryGetSeedOrderedListForLine(int lineIndex, out string numberText)
	{
		numberText = string.Empty;
		if (lineIndex < 0)
		{
			return false;
		}
		MarkdownBlockNode markdownBlockNode = (_seedSemanticDocumentForRefresh?.Blocks)?.FirstOrDefault((MarkdownBlockNode block) => block != null && block.Kind == MarkdownBlockKind.List && block.ListKind == MarkdownListKind.Numbered && block.StartLine <= lineIndex && block.EndLine >= lineIndex);
		if (markdownBlockNode == null)
		{
			if (_semanticBlocksByLine != null && _semanticBlocksByLine.TryGetValue(lineIndex + 1, out var value) && value != null && value.Kind == MarkdownBlockKind.List && value.ListKind == MarkdownListKind.Numbered)
			{
				numberText = (string.IsNullOrEmpty(value.CustomOrder) ? "1" : value.CustomOrder);
				return true;
			}
			return false;
		}
		numberText = (string.IsNullOrEmpty(markdownBlockNode.CustomOrder) ? "1" : markdownBlockNode.CustomOrder);
		return true;
	}

	private bool TryGetSeedTaskListForLine(int lineIndex, out bool isChecked)
	{
		isChecked = false;
		if (lineIndex < 0)
		{
			return false;
		}
		MarkdownBlockNode markdownBlockNode = (_seedSemanticDocumentForRefresh?.Blocks)?.FirstOrDefault((MarkdownBlockNode block) => block != null && block.Kind == MarkdownBlockKind.List && block.ListKind == MarkdownListKind.Task && block.StartLine <= lineIndex && block.EndLine >= lineIndex);
		if (markdownBlockNode == null)
		{
			if (_semanticBlocksByLine != null && _semanticBlocksByLine.TryGetValue(lineIndex + 1, out var value) && value != null && value.Kind == MarkdownBlockKind.List && value.ListKind == MarkdownListKind.Task)
			{
				isChecked = value.Checked == true;
				return true;
			}
			return false;
		}
		isChecked = markdownBlockNode.Checked == true;
		return true;
	}

	private bool TryGetSemanticBulletListForLine(int lineIndex)
	{
		if (TryGetSemanticBlockAtLine(lineIndex, out var block) && block.Kind == MarkdownBlockKind.List)
		{
			return block.ListKind == MarkdownListKind.Bullet;
		}
		return false;
	}

	private bool TryGetSemanticOrderedListForLine(int lineIndex, out string numberText)
	{
		numberText = string.Empty;
		if (!TryGetSemanticBlockAtLine(lineIndex, out var block) || block.Kind != MarkdownBlockKind.List || block.ListKind != MarkdownListKind.Numbered)
		{
			return false;
		}
		numberText = (string.IsNullOrEmpty(block.CustomOrder) ? "1" : block.CustomOrder);
		return true;
	}

	private bool TryGetSemanticTaskListForLine(int lineIndex, out bool isChecked)
	{
		isChecked = false;
		if (!TryGetSemanticBlockAtLine(lineIndex, out var block) || block.Kind != MarkdownBlockKind.List || block.ListKind != MarkdownListKind.Task)
		{
			return false;
		}
		isChecked = block.Checked == true;
		return true;
	}

	private int GetSemanticIndentForLineOrZero(int lineIndex)
	{
		if (TryGetSemanticBlockAtLine(lineIndex, out var block) && block != null)
		{
			return Math.Max(0, block.Indent);
		}
		return 0;
	}

	private int GetPreservedSemanticIndentForLine(int lineIndex, int fallbackIndent)
	{
		if (lineIndex < 0)
		{
			return Math.Max(0, fallbackIndent);
		}
		int num = -1;
		if (_seedSemanticDocumentForRefresh?.Blocks != null)
		{
			num = (from block in _seedSemanticDocumentForRefresh.Blocks.Where((MarkdownBlockNode block) => block != null && block.StartLine <= lineIndex && block.EndLine >= lineIndex)
				select Math.Max(0, block.Indent)).DefaultIfEmpty(-1).Max();
		}
		if (num >= 0)
		{
			return num;
		}
		int num2 = -1;
		if (_semanticDocument?.Blocks != null)
		{
			num2 = (from block in _semanticDocument.Blocks.Where((MarkdownBlockNode block) => block != null && block.StartLine <= lineIndex && block.EndLine >= lineIndex)
				select Math.Max(0, block.Indent)).DefaultIfEmpty(-1).Max();
		}
		if (num2 >= 0)
		{
			return num2;
		}
		return Math.Max(0, fallbackIndent);
	}

	private static bool IsLiteralTabOnlySemanticLine(string lineText)
	{
		bool result = false;
		string text = lineText ?? string.Empty;
		for (int i = 0; i < text.Length; i++)
		{
			switch (text[i])
			{
			case '\t':
				result = true;
				break;
			default:
				return false;
			case '\u00a0':
			case '\u2065':
				break;
			}
		}
		return result;
	}

	private static bool IsEffectivelyEmptySemanticLine(string lineText)
	{
		if (string.IsNullOrEmpty(lineText))
		{
			return true;
		}
		foreach (char c in lineText)
		{
			if (c != '\u00a0' && c != '\u2065' && c != '\u2066' && "\u200c\u200d\u2060".IndexOf(c) < 0 && !char.IsWhiteSpace(c))
			{
				return false;
			}
		}
		return true;
	}

	private static bool TryGetDisplayCaretAnchorOffset(string lineText, out int anchorOffset)
	{
		anchorOffset = -1;
		if (string.IsNullOrEmpty(lineText))
		{
			return false;
		}
		anchorOffset = lineText.IndexOf('\u00a0');
		return anchorOffset >= 0;
	}

	private static int GetCaretAnchorInsertionOffsetForEmptySemanticLine(string lineText)
	{
		string text = lineText ?? string.Empty;
		int i;
		for (i = 0; i < text.Length && text[i] != '\u00a0' && text[i] != '\u2065' && char.IsWhiteSpace(text[i]); i++)
		{
		}
		return i;
	}

	private void SetSemanticBlockQuoteForLine(int lineIndex, bool enabled, int? indent = null)
	{
		if (lineIndex < 0)
		{
			return;
		}
		_semanticDocument = _semanticDocument ?? new MarkdownSemanticDocument();
		MarkdownBlockNode markdownBlockNode = _semanticDocument.Blocks.FirstOrDefault((MarkdownBlockNode block) => block != null && block.StartLine <= lineIndex && block.EndLine >= lineIndex);
		if (enabled)
		{
			if (markdownBlockNode != null)
			{
				markdownBlockNode.Kind = MarkdownBlockKind.BlockQuote;
				markdownBlockNode.Level = null;
				markdownBlockNode.Folded = null;
				markdownBlockNode.ListKind = null;
				markdownBlockNode.Checked = null;
				markdownBlockNode.CustomOrder = null;
				markdownBlockNode.Language = null;
				markdownBlockNode.CodeText = null;
			}
			else
			{
				_semanticDocument.Blocks.Add(new MarkdownBlockNode
				{
					Kind = MarkdownBlockKind.BlockQuote,
					StartLine = lineIndex,
					EndLine = lineIndex,
					Indent = indent.GetValueOrDefault()
				});
			}
			if (markdownBlockNode != null)
			{
				markdownBlockNode.Indent = indent ?? markdownBlockNode.Indent;
			}
		}
		else if (markdownBlockNode != null)
		{
			markdownBlockNode.Kind = MarkdownBlockKind.Paragraph;
			markdownBlockNode.Level = null;
			markdownBlockNode.Folded = null;
			markdownBlockNode.ListKind = null;
			markdownBlockNode.Checked = null;
			markdownBlockNode.CustomOrder = null;
			markdownBlockNode.Language = null;
			markdownBlockNode.CodeText = null;
		}
		else
		{
			_semanticDocument.Blocks.Add(new MarkdownBlockNode
			{
				Kind = MarkdownBlockKind.Paragraph,
				StartLine = lineIndex,
				EndLine = lineIndex,
				Indent = indent.GetValueOrDefault()
			});
		}
		ExpandFoldedHeadingsThatWouldHideLine(lineIndex);
		_semanticBlocksByLine = BuildSemanticBlockIndex(_semanticDocument);
		UpdateHeadingFoldings();
	}

	private void SetSemanticFileBlockForLine(int lineIndex, MarkdownBlockNode fileBlock)
	{
		if (lineIndex >= 0 && fileBlock != null)
		{
			_semanticDocument = _semanticDocument ?? new MarkdownSemanticDocument();
			MarkdownBlockNode markdownBlockNode = _semanticDocument.Blocks.FirstOrDefault((MarkdownBlockNode block) => block != null && block.StartLine <= lineIndex && block.EndLine >= lineIndex);
			if (markdownBlockNode == null)
			{
				markdownBlockNode = new MarkdownBlockNode();
				_semanticDocument.Blocks.Add(markdownBlockNode);
			}
			markdownBlockNode.Kind = MarkdownBlockKind.FileBlock;
			markdownBlockNode.StartLine = lineIndex;
			markdownBlockNode.EndLine = lineIndex;
			markdownBlockNode.Indent = Math.Max(0, fileBlock.Indent);
			markdownBlockNode.FileKind = fileBlock.FileKind;
			markdownBlockNode.Source = fileBlock.Source;
			markdownBlockNode.AltText = fileBlock.AltText;
			markdownBlockNode.Caption = fileBlock.Caption;
			markdownBlockNode.Level = null;
			markdownBlockNode.Folded = false;
			markdownBlockNode.ListKind = null;
			markdownBlockNode.Checked = null;
			markdownBlockNode.CustomOrder = null;
			markdownBlockNode.Language = null;
			markdownBlockNode.CodeText = null;
			markdownBlockNode.Inlines.Clear();
			ExpandFoldedHeadingsThatWouldHideLine(lineIndex);
			_semanticBlocksByLine = BuildSemanticBlockIndex(_semanticDocument);
			UpdateHeadingFoldings();
		}
	}

	private void SetSemanticBulletListForLine(int lineIndex, bool enabled, int? indent = null)
	{
		if (lineIndex < 0)
		{
			return;
		}
		_semanticDocument = _semanticDocument ?? new MarkdownSemanticDocument();
		MarkdownBlockNode markdownBlockNode = _semanticDocument.Blocks.FirstOrDefault((MarkdownBlockNode block) => block != null && block.StartLine <= lineIndex && block.EndLine >= lineIndex);
		if (enabled)
		{
			if (markdownBlockNode != null)
			{
				markdownBlockNode.Kind = MarkdownBlockKind.List;
				markdownBlockNode.ListKind = MarkdownListKind.Bullet;
				markdownBlockNode.Level = null;
				markdownBlockNode.Folded = null;
				markdownBlockNode.Checked = null;
				markdownBlockNode.CustomOrder = null;
				markdownBlockNode.Language = null;
				markdownBlockNode.CodeText = null;
			}
			else
			{
				_semanticDocument.Blocks.Add(new MarkdownBlockNode
				{
					Kind = MarkdownBlockKind.List,
					ListKind = MarkdownListKind.Bullet,
					StartLine = lineIndex,
					EndLine = lineIndex,
					Indent = indent.GetValueOrDefault()
				});
			}
			if (markdownBlockNode != null)
			{
				markdownBlockNode.Indent = indent ?? markdownBlockNode.Indent;
			}
		}
		else if (markdownBlockNode != null)
		{
			markdownBlockNode.Kind = MarkdownBlockKind.Paragraph;
			markdownBlockNode.ListKind = null;
			markdownBlockNode.Level = null;
			markdownBlockNode.Folded = null;
			markdownBlockNode.Checked = null;
			markdownBlockNode.CustomOrder = null;
			markdownBlockNode.Language = null;
			markdownBlockNode.CodeText = null;
		}
		else
		{
			_semanticDocument.Blocks.Add(new MarkdownBlockNode
			{
				Kind = MarkdownBlockKind.Paragraph,
				StartLine = lineIndex,
				EndLine = lineIndex,
				Indent = indent.GetValueOrDefault()
			});
		}
		ExpandFoldedHeadingsThatWouldHideLine(lineIndex);
		_semanticBlocksByLine = BuildSemanticBlockIndex(_semanticDocument);
		UpdateHeadingFoldings();
	}

	private void SetSemanticTaskListForLine(int lineIndex, bool enabled, bool isChecked = false, int? indent = null)
	{
		if (lineIndex < 0)
		{
			return;
		}
		_semanticDocument = _semanticDocument ?? new MarkdownSemanticDocument();
		MarkdownBlockNode markdownBlockNode = _semanticDocument.Blocks.FirstOrDefault((MarkdownBlockNode block) => block != null && block.StartLine <= lineIndex && block.EndLine >= lineIndex);
		if (enabled)
		{
			if (markdownBlockNode != null)
			{
				markdownBlockNode.Kind = MarkdownBlockKind.List;
				markdownBlockNode.ListKind = MarkdownListKind.Task;
				markdownBlockNode.Checked = isChecked;
				markdownBlockNode.Level = null;
				markdownBlockNode.Folded = null;
				markdownBlockNode.CustomOrder = null;
				markdownBlockNode.Language = null;
				markdownBlockNode.CodeText = null;
			}
			else
			{
				_semanticDocument.Blocks.Add(new MarkdownBlockNode
				{
					Kind = MarkdownBlockKind.List,
					ListKind = MarkdownListKind.Task,
					Checked = isChecked,
					StartLine = lineIndex,
					EndLine = lineIndex,
					Indent = indent.GetValueOrDefault()
				});
			}
			if (markdownBlockNode != null)
			{
				markdownBlockNode.Indent = indent ?? markdownBlockNode.Indent;
			}
		}
		else if (markdownBlockNode != null)
		{
			markdownBlockNode.Kind = MarkdownBlockKind.Paragraph;
			markdownBlockNode.ListKind = null;
			markdownBlockNode.Checked = null;
			markdownBlockNode.Level = null;
			markdownBlockNode.Folded = null;
			markdownBlockNode.CustomOrder = null;
			markdownBlockNode.Language = null;
			markdownBlockNode.CodeText = null;
		}
		else
		{
			_semanticDocument.Blocks.Add(new MarkdownBlockNode
			{
				Kind = MarkdownBlockKind.Paragraph,
				StartLine = lineIndex,
				EndLine = lineIndex,
				Indent = indent.GetValueOrDefault()
			});
		}
		ExpandFoldedHeadingsThatWouldHideLine(lineIndex);
		_semanticBlocksByLine = BuildSemanticBlockIndex(_semanticDocument);
		UpdateHeadingFoldings();
	}

	private void SetSemanticOrderedListForLine(int lineIndex, bool enabled, string numberText = "1", int? indent = null)
	{
		if (lineIndex < 0)
		{
			return;
		}
		_semanticDocument = _semanticDocument ?? new MarkdownSemanticDocument();
		MarkdownBlockNode markdownBlockNode = _semanticDocument.Blocks.FirstOrDefault((MarkdownBlockNode block) => block != null && block.StartLine <= lineIndex && block.EndLine >= lineIndex);
		if (enabled)
		{
			if (markdownBlockNode != null)
			{
				markdownBlockNode.Kind = MarkdownBlockKind.List;
				markdownBlockNode.ListKind = MarkdownListKind.Numbered;
				markdownBlockNode.CustomOrder = (string.IsNullOrEmpty(numberText) ? "1" : numberText);
				markdownBlockNode.Checked = null;
				markdownBlockNode.Level = null;
				markdownBlockNode.Folded = null;
				markdownBlockNode.Language = null;
				markdownBlockNode.CodeText = null;
			}
			else
			{
				_semanticDocument.Blocks.Add(new MarkdownBlockNode
				{
					Kind = MarkdownBlockKind.List,
					ListKind = MarkdownListKind.Numbered,
					CustomOrder = (string.IsNullOrEmpty(numberText) ? "1" : numberText),
					StartLine = lineIndex,
					EndLine = lineIndex,
					Indent = indent.GetValueOrDefault()
				});
			}
			if (markdownBlockNode != null)
			{
				markdownBlockNode.Indent = indent ?? markdownBlockNode.Indent;
			}
		}
		else if (markdownBlockNode != null)
		{
			markdownBlockNode.Kind = MarkdownBlockKind.Paragraph;
			markdownBlockNode.ListKind = null;
			markdownBlockNode.CustomOrder = null;
			markdownBlockNode.Checked = null;
			markdownBlockNode.Level = null;
			markdownBlockNode.Folded = null;
			markdownBlockNode.Language = null;
			markdownBlockNode.CodeText = null;
		}
		else
		{
			_semanticDocument.Blocks.Add(new MarkdownBlockNode
			{
				Kind = MarkdownBlockKind.Paragraph,
				StartLine = lineIndex,
				EndLine = lineIndex,
				Indent = indent.GetValueOrDefault()
			});
		}
		ExpandFoldedHeadingsThatWouldHideLine(lineIndex);
		_semanticBlocksByLine = BuildSemanticBlockIndex(_semanticDocument);
		UpdateHeadingFoldings();
	}

	private void SetSemanticCodeBlockForLine(int lineIndex, bool enabled, string language = null, string codeText = null, int? indent = null)
	{
		if (lineIndex < 0)
		{
			return;
		}
		_semanticDocument = _semanticDocument ?? new MarkdownSemanticDocument();
		MarkdownBlockNode markdownBlockNode = _semanticDocument.Blocks.FirstOrDefault((MarkdownBlockNode block) => block != null && block.StartLine <= lineIndex && block.EndLine >= lineIndex);
		if (enabled)
		{
			if (markdownBlockNode != null)
			{
				markdownBlockNode.Kind = MarkdownBlockKind.CodeBlock;
				markdownBlockNode.Level = null;
				markdownBlockNode.Folded = null;
				markdownBlockNode.ListKind = null;
				markdownBlockNode.Checked = null;
				markdownBlockNode.CustomOrder = null;
				markdownBlockNode.Language = (string.IsNullOrWhiteSpace(language) ? null : language);
				markdownBlockNode.CodeText = codeText ?? string.Empty;
			}
			else
			{
				_semanticDocument.Blocks.Add(new MarkdownBlockNode
				{
					Kind = MarkdownBlockKind.CodeBlock,
					StartLine = lineIndex,
					EndLine = lineIndex,
					Indent = indent.GetValueOrDefault(),
					Language = (string.IsNullOrWhiteSpace(language) ? null : language),
					CodeText = (codeText ?? string.Empty)
				});
			}
			if (markdownBlockNode != null)
			{
				markdownBlockNode.Indent = indent ?? markdownBlockNode.Indent;
			}
		}
		else if (markdownBlockNode != null)
		{
			markdownBlockNode.Kind = MarkdownBlockKind.Paragraph;
			markdownBlockNode.Level = null;
			markdownBlockNode.Folded = null;
			markdownBlockNode.ListKind = null;
			markdownBlockNode.Checked = null;
			markdownBlockNode.CustomOrder = null;
			markdownBlockNode.Language = null;
			markdownBlockNode.CodeText = null;
		}
		else
		{
			_semanticDocument.Blocks.Add(new MarkdownBlockNode
			{
				Kind = MarkdownBlockKind.Paragraph,
				StartLine = lineIndex,
				EndLine = lineIndex,
				Indent = indent.GetValueOrDefault()
			});
		}
		ExpandFoldedHeadingsThatWouldHideLine(lineIndex);
		_semanticBlocksByLine = BuildSemanticBlockIndex(_semanticDocument);
		UpdateHeadingFoldings();
	}

	private void SetSemanticCodeBlockForLineRange(int startLineIndex, int endLineIndex, string language = null, string codeText = null, int? indent = null)
	{
		if (startLineIndex < 0 || endLineIndex < startLineIndex)
		{
			return;
		}
		_semanticDocument = _semanticDocument ?? new MarkdownSemanticDocument();
		List<MarkdownBlockNode> list = new List<MarkdownBlockNode>();
		foreach (MarkdownBlockNode item in _semanticDocument.Blocks.ToList())
		{
			if (item != null && item.EndLine >= startLineIndex && item.StartLine <= endLineIndex)
			{
				_semanticDocument.Blocks.Remove(item);
				if (item.StartLine < startLineIndex)
				{
					list.Add(CloneSemanticBlockAsLineRange(item, item.StartLine, startLineIndex - 1));
				}
				if (item.EndLine > endLineIndex)
				{
					list.Add(CloneSemanticBlockAsLineRange(item, endLineIndex + 1, item.EndLine));
				}
			}
		}
		foreach (MarkdownBlockNode item2 in list.Where((MarkdownBlockNode block) => block != null))
		{
			_semanticDocument.Blocks.Add(item2);
		}
		_semanticDocument.Blocks.Add(new MarkdownBlockNode
		{
			Kind = MarkdownBlockKind.CodeBlock,
			StartLine = startLineIndex,
			EndLine = endLineIndex,
			Indent = indent.GetValueOrDefault(),
			Language = (string.IsNullOrWhiteSpace(language) ? null : language),
			CodeText = (codeText ?? string.Empty)
		});
		_semanticDocument.Blocks.Sort(delegate(MarkdownBlockNode left, MarkdownBlockNode right)
		{
			if (left == null && right == null)
			{
				return 0;
			}
			if (left == null)
			{
				return 1;
			}
			if (right == null)
			{
				return -1;
			}
			int num = left.StartLine.CompareTo(right.StartLine);
			return (num == 0) ? left.EndLine.CompareTo(right.EndLine) : num;
		});
		ExpandFoldedHeadingsThatWouldHideLines(startLineIndex, endLineIndex);
		_semanticBlocksByLine = BuildSemanticBlockIndex(_semanticDocument);
		UpdateHeadingFoldings();
	}

	private void SetSemanticHeadingLevelForLine(int lineIndex, int? level)
	{
		if (lineIndex < 0)
		{
			return;
		}
		_semanticDocument = _semanticDocument ?? new MarkdownSemanticDocument();
		MarkdownBlockNode markdownBlockNode = _semanticDocument.Blocks.FirstOrDefault((MarkdownBlockNode block) => block != null && block.StartLine <= lineIndex && block.EndLine >= lineIndex);
		if (level.HasValue)
		{
			if (markdownBlockNode != null)
			{
				markdownBlockNode.Kind = MarkdownBlockKind.Heading;
				markdownBlockNode.Level = Math.Max(1, Math.Min(6, level.Value));
				markdownBlockNode.Folded = markdownBlockNode.Folded == true;
				markdownBlockNode.ListKind = null;
				markdownBlockNode.Checked = null;
				markdownBlockNode.CustomOrder = null;
				markdownBlockNode.Language = null;
				markdownBlockNode.CodeText = null;
			}
			else
			{
				_semanticDocument.Blocks.Add(new MarkdownBlockNode
				{
					Kind = MarkdownBlockKind.Heading,
					StartLine = lineIndex,
					EndLine = lineIndex,
					Level = Math.Max(1, Math.Min(6, level.Value)),
					Indent = 0,
					Folded = false
				});
			}
		}
		else if (markdownBlockNode != null)
		{
			markdownBlockNode.Kind = MarkdownBlockKind.Paragraph;
			markdownBlockNode.Level = null;
			markdownBlockNode.Folded = null;
			markdownBlockNode.ListKind = null;
			markdownBlockNode.Checked = null;
			markdownBlockNode.CustomOrder = null;
			markdownBlockNode.Language = null;
			markdownBlockNode.CodeText = null;
		}
		else
		{
			_semanticDocument.Blocks.Add(new MarkdownBlockNode
			{
				Kind = MarkdownBlockKind.Paragraph,
				StartLine = lineIndex,
				EndLine = lineIndex,
				Indent = 0
			});
		}
		ExpandFoldedHeadingsThatWouldHideLine(lineIndex);
		_semanticBlocksByLine = BuildSemanticBlockIndex(_semanticDocument);
		UpdateHeadingFoldings();
	}

	private void ExpandFoldedHeadingsThatWouldHideLine(int lineIndex)
	{
		ExpandFoldedHeadingsThatWouldHideLines(lineIndex, lineIndex);
	}

	private void ExpandFoldedHeadingsThatWouldHideLines(int startLineIndex, int endLineIndex)
	{
		if (startLineIndex < 0 || endLineIndex < startLineIndex || EditBox?.Document == null || _semanticDocument?.Blocks == null || startLineIndex >= EditBox.Document.LineCount)
		{
			return;
		}
		endLineIndex = Math.Min(endLineIndex, EditBox.Document.LineCount - 1);
		int headingFoldExpansionScrollAnchorLine = GetHeadingFoldExpansionScrollAnchorLine(startLineIndex, endLineIndex);
		foreach (MarkdownHeadingFoldRange foldedRange in MarkdownHeadingFoldHelper.GetFoldedRanges(_semanticDocument, EditBox.Document.LineCount))
		{
			if (foldedRange != null && foldedRange.Heading?.Folded == true && foldedRange.StartLineIndex <= endLineIndex && startLineIndex <= foldedRange.EndLineIndex)
			{
				CaptureHeadingFoldExpansionScrollAnchor(headingFoldExpansionScrollAnchorLine);
				foldedRange.Heading.DisplayFolded = null;
				foldedRange.Heading.Folded = false;
			}
		}
	}

	private int GetHeadingFoldExpansionScrollAnchorLine(int startLineIndex, int endLineIndex)
	{
		TextDocument textDocument = EditBox?.Document;
		if (textDocument == null)
		{
			return startLineIndex;
		}
		int offset = Math.Max(0, Math.Min(EditBox.CaretOffset, textDocument.TextLength));
		int num = Math.Max(0, textDocument.GetLineByOffset(offset).LineNumber - 1);
		if (num < startLineIndex || num > endLineIndex)
		{
			return startLineIndex;
		}
		return num;
	}

	private void CaptureHeadingFoldExpansionScrollAnchor(int lineIndex)
	{
		if (_pendingHeadingFoldExpansionScrollAnchor != null || _headingFoldingManager == null || EditBox?.Document == null || lineIndex < 0 || lineIndex >= EditBox.Document.LineCount)
		{
			return;
		}
		TextView textView = EditBox.TextArea?.TextView;
		if (textView == null)
		{
			return;
		}
		try
		{
			textView.EnsureVisualLines();
			TextViewPosition position = new TextViewPosition(lineIndex + 1, 1);
			Point point = textView.GetVisualPosition(position, VisualYPosition.LineTop) - textView.ScrollOffset;
			ScrollViewer scrollViewer = Utils.FindParent<ScrollViewer>(this);
			double viewportTop = ((scrollViewer == null) ? point.Y : textView.TranslatePoint(point, scrollViewer).Y);
			_pendingHeadingFoldExpansionScrollAnchor = new HeadingFoldExpansionScrollAnchor
			{
				LineIndex = lineIndex,
				ParentScrollViewer = scrollViewer,
				ViewportTop = viewportTop
			};
		}
		catch (InvalidOperationException)
		{
		}
	}

	private void RestoreHeadingFoldExpansionScrollAnchor()
	{
		HeadingFoldExpansionScrollAnchor anchor = _pendingHeadingFoldExpansionScrollAnchor;
		_pendingHeadingFoldExpansionScrollAnchor = null;
		if (anchor == null)
		{
			return;
		}
		Action action = delegate
		{
			if (EditBox?.Document != null && anchor.LineIndex >= 0 && anchor.LineIndex < EditBox.Document.LineCount)
			{
				TextView textView = EditBox.TextArea?.TextView;
				if (textView != null)
				{
					try
					{
						textView.EnsureVisualLines();
						TextViewPosition position = new TextViewPosition(anchor.LineIndex + 1, 1);
						Point point = textView.GetVisualPosition(position, VisualYPosition.LineTop) - textView.ScrollOffset;
						double num = ((anchor.ParentScrollViewer == null) ? point.Y : textView.TranslatePoint(point, anchor.ParentScrollViewer).Y) - anchor.ViewportTop;
						if (!double.IsNaN(num) && !double.IsInfinity(num) && !(Math.Abs(num) < 0.5))
						{
							if (anchor.ParentScrollViewer != null)
							{
								double offset = Math.Max(0.0, Math.Min(anchor.ParentScrollViewer.ScrollableHeight, anchor.ParentScrollViewer.VerticalOffset + num));
								anchor.ParentScrollViewer.ScrollToVerticalOffset(offset);
							}
							else
							{
								EditBox.ScrollToVerticalOffset(Math.Max(0.0, textView.VerticalOffset + num));
							}
						}
					}
					catch (InvalidOperationException)
					{
					}
				}
			}
		};
		if (base.Dispatcher != null && !base.Dispatcher.HasShutdownStarted && !base.Dispatcher.HasShutdownFinished)
		{
			base.Dispatcher.BeginInvoke(action, DispatcherPriority.Render);
			base.Dispatcher.BeginInvoke(action, DispatcherPriority.ContextIdle);
		}
		else
		{
			action();
		}
	}

	private static Dictionary<int, MarkdownBlockNode> BuildSemanticBlockIndex(MarkdownSemanticDocument document)
	{
		Dictionary<int, MarkdownBlockNode> dictionary = new Dictionary<int, MarkdownBlockNode>();
		if (document == null)
		{
			return dictionary;
		}
		foreach (MarkdownBlockNode block in document.Blocks)
		{
			for (int i = block.StartLine; i <= block.EndLine; i++)
			{
				dictionary[i + 1] = block;
			}
		}
		return dictionary;
	}

	private void UpdateHeadingFoldings()
	{
		if (EditBox?.Document == null || _headingFoldingManager == null)
		{
			return;
		}
		RefreshSearchHeadingExpansionState();
		List<NewFolding> list = new List<NewFolding>();
		foreach (MarkdownHeadingFoldRange displayFoldedRange in MarkdownHeadingFoldHelper.GetDisplayFoldedRanges(_semanticDocument, EditBox.Document.LineCount))
		{
			if (TryGetHeadingFoldOffsets(displayFoldedRange, out var startOffset, out var endOffset))
			{
				list.Add(new NewFolding(startOffset, endOffset)
				{
					Name = "•••",
					DefaultClosed = true
				});
			}
		}
		list.Sort((NewFolding left, NewFolding right) => left.StartOffset.CompareTo(right.StartOffset));
		_headingFoldingManager.UpdateFoldings(list, -1);
		foreach (FoldingSection allFolding in _headingFoldingManager.AllFoldings)
		{
			allFolding.IsFolded = true;
		}
		RestoreHeadingFoldExpansionScrollAnchor();
		if (IsSemanticBlockHiddenByHeadingFold(_editingCodeBlockLanguageBlock))
		{
			CommitCodeBlockLanguageEdit(apply: true, restoreEditorFocus: false);
		}
		if (IsSemanticBlockHiddenByHeadingFold(_codeBlockLanguageOverlayBlock))
		{
			HideCodeBlockLanguageOverlay();
		}
	}

	private void RefreshSearchHeadingExpansionState()
	{
		string text = (IsSearchHeadingExpansionActive() ? SearchHelper.SearchKey : null);
		if (!string.Equals(_searchHeadingExpansionKey, text, StringComparison.Ordinal))
		{
			RestoreSearchHeadingDisplayFoldOverrides();
			_searchHeadingExpansionKey = text;
			_searchExpansionSuppressedHeadings.Clear();
		}
		if (string.IsNullOrEmpty(text) || _searchRender == null || EditBox?.Document == null || _semanticDocument?.Blocks == null)
		{
			return;
		}
		RebindSearchHeadingDisplayFoldOverridesIfDocumentChanged();
		HashSet<int> hashSet = new HashSet<int>();
		foreach (int documentMatchIndex in _searchRender.GetDocumentMatchIndices(includeHiddenOffsets: true))
		{
			int offset = Math.Max(0, Math.Min(documentMatchIndex, EditBox.Document.TextLength));
			DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(offset);
			if (lineByOffset != null)
			{
				hashSet.Add(lineByOffset.LineNumber - 1);
			}
		}
		foreach (MarkdownBlockNode block in _semanticDocument.Blocks)
		{
			if (block != null && block.Kind == MarkdownBlockKind.CodeBlock && _searchRender.GetFirstMatchIndex(block.CodeText ?? string.Empty).HasValue)
			{
				hashSet.Add(block.StartLine);
			}
		}
		if (hashSet.Count == 0)
		{
			RestoreSearchHeadingDisplayFoldOverrides();
			return;
		}
		HashSet<MarkdownBlockNode> hashSet2 = new HashSet<MarkdownBlockNode>(from range in MarkdownHeadingFoldHelper.GetDisplayFoldedRangesContainingMatches(_semanticDocument, EditBox.Document.LineCount, hashSet, _searchExpansionSuppressedHeadings)
			where range?.Heading != null
			select range.Heading);
		foreach (MarkdownBlockNode item in _searchHeadingDisplayFoldedPreviousValues.Keys.ToList())
		{
			if (!hashSet2.Contains(item))
			{
				RestoreSearchHeadingDisplayFoldOverride(item);
			}
		}
		foreach (MarkdownBlockNode item2 in hashSet2)
		{
			if (!_searchHeadingDisplayFoldedPreviousValues.ContainsKey(item2))
			{
				_searchHeadingDisplayFoldedPreviousValues[item2] = item2.DisplayFolded;
			}
			item2.DisplayFolded = false;
		}
	}

	private void RebindSearchHeadingDisplayFoldOverridesIfDocumentChanged()
	{
		if (_searchHeadingDisplayFoldedPreviousValues.Count == 0 || _semanticDocument?.Blocks == null)
		{
			return;
		}
		HashSet<MarkdownBlockNode> currentHeadings = new HashSet<MarkdownBlockNode>(_semanticDocument.Blocks.Where((MarkdownBlockNode block) => block != null && block.Kind == MarkdownBlockKind.Heading));
		if (_searchHeadingDisplayFoldedPreviousValues.Keys.All(currentHeadings.Contains))
		{
			return;
		}
		_searchHeadingDisplayFoldedPreviousValues.Clear();
		_searchExpansionSuppressedHeadings.RemoveWhere((MarkdownBlockNode heading) => !currentHeadings.Contains(heading));
		foreach (MarkdownBlockNode item in currentHeadings)
		{
			if (item.Folded == true && item.DisplayFolded == false)
			{
				item.DisplayFolded = null;
			}
		}
	}

	private void RestoreSearchHeadingDisplayFoldOverrides()
	{
		foreach (MarkdownBlockNode item in _searchHeadingDisplayFoldedPreviousValues.Keys.ToList())
		{
			RestoreSearchHeadingDisplayFoldOverride(item);
		}
	}

	private void RestoreSearchHeadingDisplayFoldOverride(MarkdownBlockNode heading)
	{
		if (heading != null && _searchHeadingDisplayFoldedPreviousValues.TryGetValue(heading, out var value))
		{
			heading.DisplayFolded = value;
			_searchHeadingDisplayFoldedPreviousValues.Remove(heading);
		}
	}

	private void PromoteSearchHeadingDisplayFoldOverridesForEditing()
	{
		if (ReadOnly || _searchHeadingDisplayFoldedPreviousValues.Count == 0)
		{
			return;
		}
		Dictionary<int, bool> dictionary = new Dictionary<int, bool>();
		foreach (MarkdownBlockNode item in _searchHeadingDisplayFoldedPreviousValues.Keys.ToList())
		{
			if (item != null && item.Kind == MarkdownBlockKind.Heading && item.DisplayFolded.HasValue)
			{
				bool value = item.DisplayFolded.Value;
				item.Folded = value;
				item.DisplayFolded = null;
				dictionary[item.StartLine] = value;
			}
		}
		if (_seedSemanticDocumentForRefresh?.Blocks != null)
		{
			foreach (MarkdownBlockNode block in _seedSemanticDocumentForRefresh.Blocks)
			{
				if (block != null && block.Kind == MarkdownBlockKind.Heading && dictionary.TryGetValue(block.StartLine, out var value2))
				{
					block.Folded = value2;
					block.DisplayFolded = null;
				}
			}
		}
		_searchHeadingDisplayFoldedPreviousValues.Clear();
		_searchExpansionSuppressedHeadings.Clear();
		_searchHeadingExpansionKey = null;
	}

	private bool IsSearchHeadingExpansionActive()
	{
		SearchHighlightBackgroundRender searchRender = _searchRender;
		if (searchRender != null && searchRender.InDetail && SearchHelper.InSearch && !SearchHelper.DetailChanged)
		{
			return !string.IsNullOrEmpty(SearchHelper.SearchKey);
		}
		return false;
	}

	private void SuppressHeadingFoldSectionSyncForCurrentInput()
	{
		RestartHeadingFoldSectionSyncSuppressionTimer(++_headingFoldSectionSyncSuppressionVersion);
	}

	private void RestartHeadingFoldSectionSyncSuppressionTimer(int version)
	{
		Dispatcher dispatcher = base.Dispatcher;
		if (dispatcher != null && !dispatcher.HasShutdownStarted && !dispatcher.HasShutdownFinished)
		{
			if (_headingFoldSectionSyncSuppressionTimer == null)
			{
				_headingFoldSectionSyncSuppressionTimer = new DispatcherTimer(DispatcherPriority.Background, dispatcher);
				_headingFoldSectionSyncSuppressionTimer.Tick += OnHeadingFoldSectionSyncSuppressionTimerTick;
			}
			_headingFoldSectionSyncSuppressionTimer.Tag = version;
			_headingFoldSectionSyncSuppressionTimer.Stop();
			_headingFoldSectionSyncSuppressionTimer.Interval = TimeSpan.FromMilliseconds(1500.0);
			_headingFoldSectionSyncSuppressionTimer.Start();
		}
	}

	private void OnHeadingFoldSectionSyncSuppressionTimerTick(object sender, EventArgs e)
	{
		_headingFoldSectionSyncSuppressionTimer?.Stop();
		int num = ((sender is DispatcherTimer { Tag: var tag } && tag is int num2) ? num2 : _headingFoldSectionSyncSuppressionVersion);
		if (_headingFoldSectionSyncSuppressionVersion == num)
		{
			_headingFoldSectionSyncSuppressionVersion = 0;
		}
	}

	private bool TryGetHeadingFoldOffsets(MarkdownHeadingFoldRange range, out int startOffset, out int endOffset)
	{
		startOffset = -1;
		endOffset = -1;
		if (range == null || EditBox?.Document == null)
		{
			return false;
		}
		TextDocument document = EditBox.Document;
		if (range.HeadingLineIndex + 1 < 1 || range.EndLineIndex + 1 > document.LineCount)
		{
			return false;
		}
		DocumentLine lineByNumber = document.GetLineByNumber(range.HeadingLineIndex + 1);
		DocumentLine lineByNumber2 = document.GetLineByNumber(range.EndLineIndex + 1);
		if (lineByNumber == null || lineByNumber2 == null)
		{
			return false;
		}
		startOffset = Math.Max(0, Math.Min(lineByNumber.EndOffset, document.TextLength));
		endOffset = Math.Max(startOffset, Math.Min(lineByNumber2.EndOffset, document.TextLength));
		return endOffset > startOffset;
	}

	private void SyncHeadingFoldStatesFromFoldingSections()
	{
		if (_headingFoldingManager == null || EditBox?.Document == null || _semanticDocument?.Blocks == null)
		{
			return;
		}
		bool allowHeadingFoldSectionSyncDuringSuppressionOnce = _allowHeadingFoldSectionSyncDuringSuppressionOnce;
		_allowHeadingFoldSectionSyncDuringSuppressionOnce = false;
		if (_headingFoldSectionSyncSuppressionVersion > 0 && !allowHeadingFoldSectionSyncDuringSuppressionOnce)
		{
			UpdateHeadingFoldings();
			return;
		}
		if (allowHeadingFoldSectionSyncDuringSuppressionOnce)
		{
			_headingFoldSectionSyncSuppressionTimer?.Stop();
			_headingFoldSectionSyncSuppressionVersion = 0;
		}
		string persistedText = GetPersistedText();
		bool flag = false;
		bool flag2 = false;
		foreach (FoldingSection item in _headingFoldingManager.AllFoldings.ToList())
		{
			if (!TryGetHeadingLineIndexFromFolding(item, out var lineIndex) || !TryGetSemanticBlockAtLine(lineIndex, out var block) || block == null || block.Kind != MarkdownBlockKind.Heading)
			{
				continue;
			}
			if (ReadOnly)
			{
				bool isFolded = item.IsFolded;
				if ((block.DisplayFolded ?? (block.Folded == true)) != isFolded)
				{
					_searchHeadingDisplayFoldedPreviousValues.Remove(block);
					block.DisplayFolded = isFolded;
					if (isFolded && IsSearchHeadingExpansionActive())
					{
						_searchExpansionSuppressedHeadings.Add(block);
					}
					else
					{
						_searchExpansionSuppressedHeadings.Remove(block);
					}
					flag2 = true;
				}
				continue;
			}
			bool isFolded2 = item.IsFolded;
			if (block.DisplayFolded.HasValue)
			{
				_searchHeadingDisplayFoldedPreviousValues.Remove(block);
				block.DisplayFolded = null;
				flag2 = true;
			}
			if (isFolded2 && IsSearchHeadingExpansionActive())
			{
				_searchExpansionSuppressedHeadings.Add(block);
			}
			else
			{
				_searchExpansionSuppressedHeadings.Remove(block);
			}
			if (block.Folded == true != isFolded2)
			{
				SetHeadingFoldedStateAtLine(lineIndex, isFolded2);
				flag = true;
			}
		}
		if (flag || flag2)
		{
			UpdateHeadingFoldings();
			RequestSafeRedraw();
			InvalidMargin();
			if (flag)
			{
				NotifySemanticTextChangedIfNeeded(persistedText);
			}
		}
	}

	private bool TryGetHeadingLineIndexFromFolding(FoldingSection folding, out int lineIndex)
	{
		lineIndex = -1;
		if (folding == null || EditBox?.Document == null)
		{
			return false;
		}
		TextDocument document = EditBox.Document;
		int offset = Math.Max(0, Math.Min(folding.StartOffset, document.TextLength));
		DocumentLine lineByOffset = document.GetLineByOffset(offset);
		if (lineByOffset == null)
		{
			return false;
		}
		lineIndex = Math.Max(0, lineByOffset.LineNumber - 1);
		return true;
	}

	private bool TryGetFoldedHeadingAtCaretBoundary(int caretOffset, out FoldingSection folding, out MarkdownBlockNode block, out MarkdownHeadingFoldRange range)
	{
		folding = null;
		block = null;
		range = null;
		if (_headingFoldingManager == null || EditBox?.Document == null || _semanticDocument?.Blocks == null)
		{
			return false;
		}
		TextDocument document = EditBox.Document;
		int num = Math.Max(0, Math.Min(caretOffset, document.TextLength));
		foreach (FoldingSection allFolding in _headingFoldingManager.AllFoldings)
		{
			if (allFolding != null && allFolding.IsFolded && (num == allFolding.StartOffset || num == allFolding.EndOffset) && TryGetHeadingLineIndexFromFolding(allFolding, out var lineIndex) && TryGetSemanticBlockAtLine(lineIndex, out var block2) && block2 != null && block2.Kind == MarkdownBlockKind.Heading && block2.Folded == true && MarkdownHeadingFoldHelper.TryGetFoldRange(_semanticDocument.Blocks, block2, document.LineCount, out var range2))
			{
				folding = allFolding;
				block = block2;
				range = range2;
				return true;
			}
		}
		return false;
	}

	private static bool ShouldDeferFinalHeadingFoldExpansion(bool deferWhileMouseDown, MouseButtonState leftButtonState)
	{
		if (deferWhileMouseDown)
		{
			return leftButtonState == MouseButtonState.Pressed;
		}
		return false;
	}

	private void PlaceCaretAfterHeadingFoldExpansion(int caretOffset)
	{
		if (EditBox?.Document != null)
		{
			CancelNativeMouseSelectionForHeadingFoldClick();
			FocusEditBox();
			Keyboard.Focus(EditBox.TextArea);
			int caretOffset2 = Math.Max(0, Math.Min(caretOffset, EditBox.Document.TextLength));
			EditBox.SelectionLength = 0;
			EditBox.CaretOffset = caretOffset2;
			EditBox.TextArea?.Caret.Show();
		}
	}

	private bool TryExpandFinalHeadingFoldAtCaret(bool deferWhileMouseDown = true)
	{
		if (ReadOnly || EditBox?.Document == null)
		{
			return false;
		}
		if (!TryGetFoldedHeadingAtCaretBoundary(EditBox.CaretOffset, out var folding, out var _, out var range) || EditBox.CaretOffset != folding.EndOffset)
		{
			return false;
		}
		MarkdownSemanticDocument semanticDocument = _semanticDocument;
		if (semanticDocument != null && semanticDocument.Blocks?.Any((MarkdownBlockNode markdownBlockNode) => markdownBlockNode != null && markdownBlockNode.StartLine > range.EndLineIndex) == true)
		{
			return false;
		}
		if (ShouldDeferFinalHeadingFoldExpansion(deferWhileMouseDown, Mouse.LeftButton))
		{
			_pendingFinalHeadingFoldExpansionAfterMouseUp = true;
			CancelNativeMouseSelectionForHeadingFoldClick();
			RequestSafeRedraw();
			InvalidMargin();
			return true;
		}
		string persistedText = GetPersistedText();
		SetHeadingFoldedStateAtLine(range.HeadingLineIndex, folded: false);
		folding.IsFolded = false;
		UpdateHeadingFoldings();
		int textLength = EditBox.Document.TextLength;
		PlaceCaretAfterHeadingFoldExpansion(textLength);
		RequestSafeRedraw();
		InvalidMargin();
		NotifySemanticTextChangedIfNeeded(persistedText);
		return true;
	}

	public bool TryExpandFoldedHeadingAtInsertionOffset(int insertionOffset)
	{
		if (ReadOnly || EditBox?.Document == null)
		{
			return false;
		}
		int caretOffset = Math.Max(0, Math.Min(insertionOffset, EditBox.Document.TextLength));
		if (!TryGetFoldedHeadingAtCaretBoundary(caretOffset, out var folding, out var _, out var range))
		{
			return false;
		}
		string persistedText = GetPersistedText();
		SetHeadingFoldedStateAtLine(range.HeadingLineIndex, folded: false);
		folding.IsFolded = false;
		UpdateHeadingFoldings();
		PlaceCaretAfterHeadingFoldExpansion(caretOffset);
		RequestSafeRedraw();
		InvalidMargin();
		NotifySemanticTextChangedIfNeeded(persistedText);
		return true;
	}

	public bool TryClearHeadingFoldStateAtLine(int lineIndex, bool notifyTextChanged = true)
	{
		if (ReadOnly || lineIndex < 0 || !TryGetSemanticBlockAtLine(lineIndex, out var block) || block == null || block.Kind != MarkdownBlockKind.Heading || block.Folded != true)
		{
			return false;
		}
		string persistedText = GetPersistedText();
		SetHeadingFoldedStateAtLine(lineIndex, folded: false);
		UpdateHeadingFoldings();
		RequestSafeRedraw();
		InvalidMargin();
		if (notifyTextChanged)
		{
			NotifySemanticTextChangedIfNeeded(persistedText);
		}
		return true;
	}

	public bool ToggleHeadingFoldAtLine(int lineIndex)
	{
		return ToggleHeadingFoldAtLine(lineIndex, ReadOnly);
	}

	private bool ToggleHeadingFoldAtLine(int lineIndex, bool isReadOnly)
	{
		if (!TryGetFoldableHeadingFoldStateAtLine(lineIndex, out var folded, out var _))
		{
			return false;
		}
		if (!TryGetSemanticBlockAtLine(lineIndex, out var block) || block == null || block.Kind != MarkdownBlockKind.Heading)
		{
			return false;
		}
		if (!folded)
		{
			MoveCaretOutsideHeadingFoldBeforeCollapse(block);
		}
		if (isReadOnly)
		{
			_searchHeadingDisplayFoldedPreviousValues.Remove(block);
			block.DisplayFolded = !folded;
			if (!folded && IsSearchHeadingExpansionActive())
			{
				_searchExpansionSuppressedHeadings.Add(block);
			}
			else
			{
				_searchExpansionSuppressedHeadings.Remove(block);
			}
			UpdateHeadingFoldings();
			RequestSafeRedraw();
			InvalidMargin();
			return true;
		}
		string persistedText = GetPersistedText();
		_searchHeadingDisplayFoldedPreviousValues.Remove(block);
		block.DisplayFolded = null;
		if (!folded && IsSearchHeadingExpansionActive())
		{
			_searchExpansionSuppressedHeadings.Add(block);
		}
		else
		{
			_searchExpansionSuppressedHeadings.Remove(block);
		}
		block.Folded = !folded;
		SuppressHeadingFoldSectionSyncForCurrentInput();
		UpdateHeadingFoldings();
		RequestSafeRedraw();
		InvalidMargin();
		NotifySemanticTextChangedIfNeeded(persistedText);
		return true;
	}

	private void MoveCaretOutsideHeadingFoldBeforeCollapse(MarkdownBlockNode heading)
	{
		if (heading != null && EditBox?.Document != null && _semanticDocument?.Blocks != null && MarkdownHeadingFoldHelper.TryGetFoldRange(_semanticDocument.Blocks, heading, EditBox.Document.LineCount, out var range) && TryGetHeadingFoldOffsets(range, out var startOffset, out var endOffset))
		{
			int num = Math.Max(0, Math.Min(EditBox.CaretOffset, EditBox.Document.TextLength));
			if (num > startOffset && num <= endOffset)
			{
				CancelNativeMouseSelectionForHeadingFoldClick();
				EditBox.CaretOffset = startOffset;
			}
		}
	}

	private void ClearHeadingDisplayFoldOverrides()
	{
		if (_semanticDocument?.Blocks != null)
		{
			foreach (MarkdownBlockNode block in _semanticDocument.Blocks)
			{
				if (block != null && block.Kind == MarkdownBlockKind.Heading)
				{
					block.DisplayFolded = null;
				}
			}
		}
		_searchHeadingDisplayFoldedPreviousValues.Clear();
		_searchExpansionSuppressedHeadings.Clear();
		_searchHeadingExpansionKey = null;
		UpdateHeadingFoldings();
		RequestSafeRedraw();
		InvalidMargin();
	}

	public bool TryGetHeadingFoldStateAtLine(int lineIndex, out bool folded, out int level)
	{
		folded = false;
		level = 0;
		if (lineIndex < 0 || !TryGetSemanticBlockAtLine(lineIndex, out var block) || block == null || block.Kind != MarkdownBlockKind.Heading || !block.Level.HasValue)
		{
			return false;
		}
		folded = block.DisplayFolded ?? (block.Folded == true);
		level = Math.Max(1, Math.Min(6, block.Level.Value));
		return true;
	}

	public bool TryGetInteractiveHeadingFoldStateAtLine(int lineIndex, out bool folded, out int level)
	{
		if (!TryGetFoldableHeadingFoldStateAtLine(lineIndex, out folded, out level))
		{
			return false;
		}
		return true;
	}

	public bool TryGetFoldableHeadingFoldStateAtLine(int lineIndex, out bool folded, out int level)
	{
		if (!TryGetHeadingFoldStateAtLine(lineIndex, out folded, out level) || EditBox?.Document == null || _semanticDocument?.Blocks == null || !TryGetSemanticBlockAtLine(lineIndex, out var block) || block == null || block.Kind != MarkdownBlockKind.Heading)
		{
			folded = false;
			level = 0;
			return false;
		}
		if (MarkdownHeadingFoldHelper.TryGetFoldRange(_semanticDocument.Blocks, block, EditBox.Document.LineCount, out var _))
		{
			return true;
		}
		folded = false;
		level = 0;
		return false;
	}

	public bool IsSemanticBlockHiddenByHeadingFold(MarkdownBlockNode block)
	{
		if (block != null)
		{
			return IsSemanticLineHiddenByHeadingFold(block.StartLine);
		}
		return false;
	}

	public bool IsDocumentOffsetHiddenByHeadingFold(int offset)
	{
		if (EditBox?.Document == null)
		{
			return false;
		}
		TextDocument document = EditBox.Document;
		int offset2 = Math.Max(0, Math.Min(offset, document.TextLength));
		DocumentLine lineByOffset = document.GetLineByOffset(offset2);
		if (lineByOffset != null)
		{
			return IsSemanticLineHiddenByHeadingFold(lineByOffset.LineNumber - 1);
		}
		return false;
	}

	public bool IsSemanticLineHiddenByHeadingFold(int lineIndex)
	{
		if (lineIndex < 0 || _headingFoldingManager == null || EditBox?.Document == null)
		{
			return false;
		}
		TextDocument document = EditBox.Document;
		int num = lineIndex + 1;
		if (num < 1 || num > document.LineCount)
		{
			return false;
		}
		DocumentLine lineByNumber = document.GetLineByNumber(num);
		int num2 = Math.Max(0, Math.Min(lineByNumber.Offset, document.TextLength));
		foreach (FoldingSection allFolding in _headingFoldingManager.AllFoldings)
		{
			if (allFolding != null && allFolding.IsFolded && num2 >= allFolding.StartOffset && num2 <= allFolding.EndOffset && (num2 < allFolding.EndOffset || IsTerminalEmptyListLineIncludedInHeadingFold(allFolding, lineByNumber, lineIndex)))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsTerminalEmptyListLineIncludedInHeadingFold(FoldingSection folding, DocumentLine line, int lineIndex)
	{
		if (folding == null || line == null || line.Length != 0 || line.DelimiterLength != 0 || line.Offset != EditBox.Document.TextLength || !TryGetSemanticBlockAtLine(lineIndex, out var block) || block == null || block.Kind != MarkdownBlockKind.List || !TryGetHeadingLineIndexFromFolding(folding, out var lineIndex2) || !TryGetSemanticBlockAtLine(lineIndex2, out var block2) || block2 == null || block2.Kind != MarkdownBlockKind.Heading || !MarkdownHeadingFoldHelper.TryGetFoldRange(_semanticDocument.Blocks, block2, EditBox.Document.LineCount, out var range))
		{
			return false;
		}
		if (lineIndex >= range.StartLineIndex)
		{
			return lineIndex <= range.EndLineIndex;
		}
		return false;
	}

	public bool TryGetSemanticBlockAtOffset(int offset, out MarkdownBlockNode block)
	{
		block = null;
		if (EditBox?.Document == null || _semanticBlocksByLine == null || _semanticBlocksByLine.Count == 0)
		{
			return false;
		}
		int offset2 = Math.Max(0, Math.Min(offset, EditBox.Document.TextLength));
		int lineNumber = EditBox.Document.GetLineByOffset(offset2).LineNumber;
		return _semanticBlocksByLine.TryGetValue(lineNumber, out block);
	}

	public IList<MarkdownBlockNode> GetSemanticBlocks()
	{
		return _semanticDocument?.Blocks ?? new List<MarkdownBlockNode>();
	}

	private void AfterTextSet()
	{
		SpellCheckProvider?.SetTextChanged(change: true, delay: false);
		_searchRender?.SetTextChanged(b: true);
		InvalidMargin();
		_previousLineCount = EditBox.LineCount;
	}

	private void LogEmojiPosition(string text)
	{
		EmojiDict.Clear();
		MatchCollection matchCollection = EmojiData.MatchOne2.Matches(text);
		List<KeyValuePair<int, int>> list = new List<KeyValuePair<int, int>>();
		foreach (KeyValuePair<int, LinkInfo> item in LinkNameDict)
		{
			list.Add(new KeyValuePair<int, int>(item.Key, item.Key + item.Value.Link.Length));
		}
		EmojiData.MatchOne2.Matches(text);
		foreach (Match m in matchCollection)
		{
			if (!list.Any((KeyValuePair<int, int> pair) => m.Index >= pair.Key && m.Index <= pair.Value))
			{
				EmojiDict[m.Index] = m.Value;
			}
		}
	}

	public Dictionary<int, string> GetEmojiDict()
	{
		return EmojiDict;
	}

	internal void NotifyCodeBlockAttachmentCaretVerticalOffset(double offset)
	{
		if (!double.IsNaN(offset) && !double.IsInfinity(offset))
		{
			this.CaretVerticalOffsetChanged?.Invoke(this, offset);
		}
	}

	public void SetupMargin(double width = 22.0, bool inSticky = false)
	{
		EditBox.TextArea.LeftMargins.Clear();
		if (StyleEnabled)
		{
			EditBox.TextArea.LeftMargins.Add(new MarkDownTagMargin(EditBox.TextArea, this, width, inSticky));
		}
	}

	private void SetupEvents()
	{
		EditBox.TextChanged -= EditBoxOnTextChanged;
		EditBox.TextChanged += EditBoxOnTextChanged;
		EditBox.Document.Changing -= OnEditDocumentChanging;
		EditBox.Document.Changing += OnEditDocumentChanging;
		EditBox.Document.Changed -= OnEditDocumentChanged;
		EditBox.Document.Changed += OnEditDocumentChanged;
		EditBox.TextArea.TextEntering -= OnImeCompositionTextEntering;
		EditBox.TextArea.TextEntering += OnImeCompositionTextEntering;
		EditBox.CommandBindings.Insert(0, new CommandBinding(ApplicationCommands.Copy, OnCopyCommandExecuted, OnCopyCommandCanExecute));
		EditBox.CommandBindings.Insert(0, new CommandBinding(ApplicationCommands.Cut, OnCutCommandExecuted, OnCutCommandCanExecute));
		EditBox.CommandBindings.Add(new CommandBinding(ApplicationCommands.Undo, OnUndoCommandExecuted, OnUndoCommandCanExecute));
		EditBox.CommandBindings.Add(new CommandBinding(ApplicationCommands.Redo, OnRedoCommandExecuted, OnRedoCommandCanExecute));
		EditBox.TextArea.CommandBindings.Insert(0, new CommandBinding(ApplicationCommands.Copy, OnCopyCommandExecuted, OnCopyCommandCanExecute));
		EditBox.TextArea.CommandBindings.Insert(0, new CommandBinding(ApplicationCommands.Cut, OnCutCommandExecuted, OnCutCommandCanExecute));
		CommandManager.RemovePreviewExecutedHandler(EditBox, OnPreviewCopyCommandExecuted);
		CommandManager.AddPreviewExecutedHandler(EditBox, OnPreviewCopyCommandExecuted);
		CommandManager.RemovePreviewExecutedHandler(EditBox.TextArea, OnPreviewCopyCommandExecuted);
		CommandManager.AddPreviewExecutedHandler(EditBox.TextArea, OnPreviewCopyCommandExecuted);
		EditBox.Options.IndentationSize = 4;
		EditBox.Options.EnableHyperlinks = false;
		EditBox.Options.ConvertTabsToSpaces = true;
		EditBox.Options.AllowScrollBelowDocument = true;
		EditBox.Options.EnableEmailHyperlinks = false;
		EditBox.Options.InheritWordWrapIndentation = false;
		EditBox.Options.EnableTextDragDrop = true;
		EditBox.AllowDrop = true;
		EditBox.TextArea.AllowDrop = true;
		EditBox.TextArea.TextView.AllowDrop = true;
		EditBox.PreviewKeyDown -= OnEditorKeyDown;
		EditBox.PreviewKeyDown += OnEditorKeyDown;
		EditBox.PreviewKeyUp -= OnEditKeyUp;
		EditBox.PreviewKeyUp += OnEditKeyUp;
		EditBox.GotFocus -= OnEditorGotFocus;
		EditBox.GotFocus += OnEditorGotFocus;
		EditBox.LostFocus -= OnEditorLostFocus;
		EditBox.LostFocus += OnEditorLostFocus;
		EditBox.PreviewMouseLeftButtonUp -= OnEditorMouseUp;
		EditBox.TextArea.PreviewMouseLeftButtonUp -= OnEditorMouseUp;
		EditBox.RemoveHandler(UIElement.PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler(OnEditorMouseUp));
		EditBox.AddHandler(UIElement.PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler(OnEditorMouseUp), handledEventsToo: true);
		EditBox.TextArea.RemoveHandler(UIElement.PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler(OnEditorMouseUp));
		EditBox.TextArea.AddHandler(UIElement.PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler(OnEditorMouseUp), handledEventsToo: true);
		EditBox.TextArea.SelectionChanged -= OnSelectionChanged;
		EditBox.TextArea.SelectionChanged += OnSelectionChanged;
		EditBox.TextArea.TextView.MouseLeave -= OnTextViewLeave;
		EditBox.TextArea.TextView.MouseLeave += OnTextViewLeave;
		EditBox.TextArea.TextView.ScrollOffsetChanged -= OnCodeBlockLanguageOverlayScrollChanged;
		EditBox.TextArea.TextView.ScrollOffsetChanged += OnCodeBlockLanguageOverlayScrollChanged;
		EditBox.TextArea.TextView.ScrollOffsetChanged -= OnTextViewScrollOffsetChanged;
		EditBox.TextArea.TextView.ScrollOffsetChanged += OnTextViewScrollOffsetChanged;
		EditBox.TextArea.TextView.VisualLinesChanged -= OnCodeBlockLanguageOverlayLayoutChanged;
		EditBox.TextArea.TextView.VisualLinesChanged += OnCodeBlockLanguageOverlayLayoutChanged;
		EditBox.TextArea.TextView.PreviewMouseDown -= OnTextViewDown;
		EditBox.TextArea.TextView.RemoveHandler(UIElement.PreviewMouseDownEvent, new MouseButtonEventHandler(OnTextViewDown));
		EditBox.TextArea.TextView.AddHandler(UIElement.PreviewMouseDownEvent, new MouseButtonEventHandler(OnTextViewDown), handledEventsToo: true);
		EditBox.TextArea.TextView.RemoveHandler(UIElement.MouseDownEvent, new MouseButtonEventHandler(OnTextViewMouseDownAfterFolding));
		EditBox.TextArea.TextView.AddHandler(UIElement.MouseDownEvent, new MouseButtonEventHandler(OnTextViewMouseDownAfterFolding), handledEventsToo: true);
		EditBox.PreviewMouseLeftButtonDown -= OnEditorMouseDown;
		EditBox.TextArea.PreviewMouseLeftButtonDown -= OnEditorMouseDown;
		EditBox.RemoveHandler(UIElement.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(OnEditorMouseDown));
		EditBox.AddHandler(UIElement.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(OnEditorMouseDown), handledEventsToo: true);
		EditBox.TextArea.RemoveHandler(UIElement.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(OnEditorMouseDown));
		EditBox.TextArea.AddHandler(UIElement.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(OnEditorMouseDown), handledEventsToo: true);
		EditBox.PreviewMouseMove -= OnEditorMouseMove;
		EditBox.TextArea.PreviewMouseMove -= OnEditorMouseMove;
		EditBox.RemoveHandler(UIElement.PreviewMouseMoveEvent, new System.Windows.Input.MouseEventHandler(OnEditorMouseMove));
		EditBox.AddHandler(UIElement.PreviewMouseMoveEvent, new System.Windows.Input.MouseEventHandler(OnEditorMouseMove), handledEventsToo: true);
		EditBox.TextArea.RemoveHandler(UIElement.PreviewMouseMoveEvent, new System.Windows.Input.MouseEventHandler(OnEditorMouseMove));
		EditBox.TextArea.AddHandler(UIElement.PreviewMouseMoveEvent, new System.Windows.Input.MouseEventHandler(OnEditorMouseMove), handledEventsToo: true);
		EditBox.TextArea.TextView.RemoveHandler(UIElement.PreviewMouseMoveEvent, new System.Windows.Input.MouseEventHandler(OnEditorMouseMove));
		EditBox.TextArea.TextView.AddHandler(UIElement.PreviewMouseMoveEvent, new System.Windows.Input.MouseEventHandler(OnEditorMouseMove), handledEventsToo: true);
		EditBox.RemoveHandler(UIElement.PreviewDragOverEvent, new System.Windows.DragEventHandler(OnEditorPreviewDragOver));
		EditBox.AddHandler(UIElement.PreviewDragOverEvent, new System.Windows.DragEventHandler(OnEditorPreviewDragOver), handledEventsToo: true);
		EditBox.TextArea.RemoveHandler(UIElement.PreviewDragOverEvent, new System.Windows.DragEventHandler(OnEditorPreviewDragOver));
		EditBox.TextArea.AddHandler(UIElement.PreviewDragOverEvent, new System.Windows.DragEventHandler(OnEditorPreviewDragOver), handledEventsToo: true);
		EditBox.TextArea.TextView.RemoveHandler(UIElement.PreviewDragOverEvent, new System.Windows.DragEventHandler(OnEditorPreviewDragOver));
		EditBox.TextArea.TextView.AddHandler(UIElement.PreviewDragOverEvent, new System.Windows.DragEventHandler(OnEditorPreviewDragOver), handledEventsToo: true);
		EditBox.RemoveHandler(UIElement.PreviewDropEvent, new System.Windows.DragEventHandler(OnEditorPreviewDrop));
		EditBox.AddHandler(UIElement.PreviewDropEvent, new System.Windows.DragEventHandler(OnEditorPreviewDrop), handledEventsToo: true);
		EditBox.TextArea.RemoveHandler(UIElement.PreviewDropEvent, new System.Windows.DragEventHandler(OnEditorPreviewDrop));
		EditBox.TextArea.AddHandler(UIElement.PreviewDropEvent, new System.Windows.DragEventHandler(OnEditorPreviewDrop), handledEventsToo: true);
		EditBox.TextArea.TextView.RemoveHandler(UIElement.PreviewDropEvent, new System.Windows.DragEventHandler(OnEditorPreviewDrop));
		EditBox.TextArea.TextView.AddHandler(UIElement.PreviewDropEvent, new System.Windows.DragEventHandler(OnEditorPreviewDrop), handledEventsToo: true);
		EditBox.TextArea.Caret.PositionChanged -= OnCaretPositionChanged;
		EditBox.TextArea.Caret.PositionChanged += OnCaretPositionChanged;
		EditBox.TextArea.Caret.CaretRectangleTransform = TransformParentCaretRectangleAtAttachmentEnd;
		EditBox.TextArea.TextView.LinkTextForegroundBrush = ThemeUtil.GetColor("TextAccentColor");
		EditBox.TextArea.TextView.LinkTextUnderline = false;
		System.Windows.DataObject.RemovePastingHandler(EditBox, OnPaste);
		System.Windows.DataObject.AddPastingHandler(EditBox, OnPaste);
		System.Windows.DataObject.RemoveCopyingHandler(EditBox.TextArea, OnEditorDataObjectCopying);
		System.Windows.DataObject.AddCopyingHandler(EditBox.TextArea, OnEditorDataObjectCopying);
	}

	private void OnParentWindowDeactivated(object sender, EventArgs e)
	{
		StopSelectionEdgeAutoScroll();
		HideLinkMenuPopup();
	}

	private void OnParentWindowStateChanged(object sender, EventArgs e)
	{
		StopSelectionEdgeAutoScroll();
		HideLinkMenuPopup();
	}

	private void OnParentWindowLocationChanged(object sender, EventArgs e)
	{
		HideLinkMenuPopup();
	}

	private void OnParentWindowClosing(object sender, CancelEventArgs e)
	{
		StopSelectionEdgeAutoScroll();
		HideLinkMenuPopup();
	}

	private void OnEditorVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (!base.IsVisible)
		{
			StopSelectionEdgeAutoScroll();
			HideLinkMenuPopup();
		}
	}

	private void BindLinkEditorPopupOwnerEvents()
	{
		base.IsVisibleChanged -= OnEditorVisibleChanged;
		base.IsVisibleChanged += OnEditorVisibleChanged;
		Window window = Window.GetWindow(this);
		if (_linkEditorPopupOwnerWindow != window)
		{
			UnbindLinkEditorPopupOwnerEvents();
			_linkEditorPopupOwnerWindow = window;
			if (_linkEditorPopupOwnerWindow != null)
			{
				_linkEditorPopupOwnerWindow.Deactivated += OnParentWindowDeactivated;
				_linkEditorPopupOwnerWindow.StateChanged += OnParentWindowStateChanged;
				_linkEditorPopupOwnerWindow.LocationChanged += OnParentWindowLocationChanged;
				_linkEditorPopupOwnerWindow.Closing += OnParentWindowClosing;
			}
		}
	}

	private void UnbindLinkEditorPopupOwnerEvents()
	{
		base.IsVisibleChanged -= OnEditorVisibleChanged;
		if (_linkEditorPopupOwnerWindow != null)
		{
			_linkEditorPopupOwnerWindow.Deactivated -= OnParentWindowDeactivated;
			_linkEditorPopupOwnerWindow.StateChanged -= OnParentWindowStateChanged;
			_linkEditorPopupOwnerWindow.LocationChanged -= OnParentWindowLocationChanged;
			_linkEditorPopupOwnerWindow.Closing -= OnParentWindowClosing;
			_linkEditorPopupOwnerWindow = null;
		}
	}

	private void OnTextViewScrollOffsetChanged(object sender, EventArgs e)
	{
		HideLinkMenuPopup();
	}

	private void ClearSemanticSelectionDragState()
	{
		StopSelectionEdgeAutoScroll();
		_semanticSelectionAnchorOffset = null;
		_semanticSelectionDragHandled = false;
		_semanticSelectionMouseUpRestoreScheduled = false;
		_semanticSelectionLastActiveOffset = null;
		_semanticSelectionLastStartOffset = null;
		_semanticSelectionLastEndOffset = null;
		_wholeSelectionAnchorStartOffset = null;
		_wholeSelectionAnchorEndOffset = null;
		_pendingFinalHeadingFoldExpansionAfterMouseUp = false;
		InternalSelectionDragState internalSelectionDragState = _internalSelectionDragState;
		if (internalSelectionDragState == null || !internalSelectionDragState.DragStarted)
		{
			_internalSelectionDragState = null;
		}
	}

	private void OnTextViewDown(object sender, MouseButtonEventArgs e)
	{
		if (e.LeftButton == MouseButtonState.Pressed && !CodeBlockAttachmentControl.TryFindFromOriginalSource(e.OriginalSource, out var _))
		{
			_leftPressed = true;
			Point position = e.GetPosition(EditBox.TextArea.TextView);
			_semanticSelectionAnchorOffset = TryGetDocumentOffsetFromTextViewPoint(position) ?? TryGetNativeSelectionHitOffsetFromTextViewPoint(position) ?? TryGetDocumentBoundaryOffsetFromTextViewPoint(position);
			_semanticSelectionDragHandled = false;
			_semanticSelectionMouseUpRestoreScheduled = false;
			_semanticSelectionLastActiveOffset = null;
			_semanticSelectionLastStartOffset = null;
			_semanticSelectionLastEndOffset = null;
			ResetSelectionViewportClampGestureState();
			CaptureInternalSelectionDragState(e);
			CancelTryShowEditorMenu();
			ScheduleCodeBlockContentNormalization();
		}
	}

	private void OnTextViewMouseDownAfterFolding(object sender, MouseButtonEventArgs e)
	{
		if (e.ChangedButton == MouseButton.Left && !IsCodeBlockAttachmentEditorInputSource(e?.OriginalSource))
		{
			_allowHeadingFoldSectionSyncDuringSuppressionOnce = true;
			base.Dispatcher?.BeginInvoke(new Action(SyncHeadingFoldStatesFromFoldingSections), DispatcherPriority.Background);
		}
	}

	private void OnTextViewLeave(object sender, System.Windows.Input.MouseEventArgs e)
	{
		Point position = Mouse.GetPosition(EditBox);
		if (_leftPressed)
		{
			_leftPressed = false;
		}
		else if (position.X > 24.0 && EditBox.TextArea.LeftMargins.Any())
		{
			EditBox.TextArea.LeftMargins[0].InvalidateVisual();
		}
	}

	private void SetupCodeBlockLanguageOverlay()
	{
		if (CodeBlockLanguageOverlay != null && _codeBlockLanguageEditorControl == null)
		{
			_codeBlockLanguageEditorControl = new CodeBlockLanguageEditorControl
			{
				MinWidth = 112.0,
				MaxWidth = 112.0,
				Height = 20.0,
				Visibility = Visibility.Collapsed
			};
			_codeBlockLanguageEditorControl.Editor.PreviewKeyDown += OnCodeBlockLanguageEditorPreviewKeyDown;
			_codeBlockLanguageEditorControl.Editor.LostKeyboardFocus += OnCodeBlockLanguageEditorLostKeyboardFocus;
			_codeBlockLanguageEditorControl.Editor.TextChanged += OnCodeBlockLanguageEditorTextChanged;
			_codeBlockLanguageEditorControl.LanguageSelected += OnCodeBlockLanguageSelected;
			_codeBlockLanguageEditorControl.DropdownClosed += OnCodeBlockLanguageDropdownClosed;
			RefreshCodeBlockLanguageOverlayTheme();
			System.Windows.Controls.Panel.SetZIndex(CodeBlockLanguageOverlay, 10);
			System.Windows.Controls.Panel.SetZIndex(_codeBlockLanguageEditorControl, 2);
			CodeBlockLanguageOverlay.Children.Add(_codeBlockLanguageEditorControl);
			ScheduleCodeBlockLanguageOverlayLayoutRefresh();
		}
	}

	private void RefreshCodeBlockLanguageOverlayTheme()
	{
		if (_codeBlockLanguageEditorControl == null)
		{
			return;
		}
		SolidColorBrush color = ThemeUtil.GetColor(_color100, this);
		SolidColorBrush color2 = ThemeUtil.GetColor("PopupBackground", this);
		Brush editorBackground = CloneBrushWithOpacity(color2, 0.3);
		SolidColorBrush color3 = ThemeUtil.GetColor("BaseColorOpacity0_10", this);
		foreach (Border codeBlockLanguageBadge in _codeBlockLanguageBadges)
		{
			ApplyCodeBlockLanguageBadgeTheme(codeBlockLanguageBadge);
		}
		foreach (Border codeBlockCopyButton in _codeBlockCopyButtons)
		{
			ApplyCodeBlockCopyButtonTheme(codeBlockCopyButton);
		}
		_codeBlockLanguageEditorControl.ApplyTheme(editorBackground, color, color2, color3, ThemeUtil.GetColor("BaseColorOpacity40", this));
	}

	private Border CreateCodeBlockLanguageBadge(MarkdownBlockNode block)
	{
		TextBlock child = new TextBlock
		{
			Text = GetDisplayCodeBlockLanguage(block),
			FontSize = 13.0,
			VerticalAlignment = VerticalAlignment.Center,
			TextTrimming = TextTrimming.CharacterEllipsis
		};
		Border border = new Border
		{
			CornerRadius = new CornerRadius(4.0),
			Padding = new Thickness(8.0, 2.0, 8.0, 2.0),
			MinWidth = 112.0,
			MaxWidth = 112.0,
			Height = 20.0,
			Cursor = System.Windows.Input.Cursors.Hand,
			DataContext = block,
			Child = child
		};
		border.MouseEnter += OnCodeBlockLanguageBadgeMouseEnter;
		border.PreviewMouseLeftButtonDown += OnCodeBlockLanguageBadgeMouseLeftButtonDown;
		border.PreviewMouseLeftButtonUp += OnCodeBlockLanguageBadgeMouseLeftButtonUp;
		border.MouseLeave += OnCodeBlockLanguageBadgeMouseLeave;
		ApplyCodeBlockLanguageBadgeTheme(border);
		return border;
	}

	private void ApplyCodeBlockLanguageBadgeTheme(Border badge)
	{
		if (badge != null)
		{
			badge.Background = Brushes.Transparent;
			badge.BorderBrush = ThemeUtil.GetColor("BaseColorOpacity0_10", this);
			badge.Tag = CloneBrushWithOpacity(ThemeUtil.GetColor("CodeBackground", this), 0.88);
			if (badge.Child is TextBlock textBlock)
			{
				textBlock.Foreground = ThemeUtil.GetColor("BaseColorOpacity60", this);
			}
		}
	}

	private Border CreateCodeBlockCopyButton(MarkdownBlockNode block)
	{
		System.Windows.Shapes.Path element = new System.Windows.Shapes.Path
		{
			Data = ((TryFindResource("Ic.MarkdownEditor.Copy") ?? System.Windows.Application.Current?.TryFindResource("Ic.MarkdownEditor.Copy")) as Geometry),
			Width = 16.0,
			Height = 16.0,
			Stretch = Stretch.Uniform,
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
			IsHitTestVisible = false
		};
		TextBlock element2 = new TextBlock
		{
			FontSize = 13.0,
			VerticalAlignment = VerticalAlignment.Center,
			Text = Utils.GetString("Copy"),
			Margin = new Thickness(4.0, 0.0, 0.0, 0.0)
		};
		StackPanel stackPanel = new StackPanel
		{
			Orientation = System.Windows.Controls.Orientation.Horizontal,
			HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			IsHitTestVisible = false
		};
		stackPanel.Children.Add(element);
		stackPanel.Children.Add(element2);
		Border border = new Border
		{
			CornerRadius = new CornerRadius(4.0),
			Padding = new Thickness(6.0, 2.0, 6.0, 2.0),
			MinWidth = 58.0,
			Height = 20.0,
			Cursor = System.Windows.Input.Cursors.Hand,
			DataContext = block,
			Child = stackPanel
		};
		border.MouseEnter += OnCodeBlockCopyButtonMouseEnter;
		border.PreviewMouseLeftButtonDown += OnCodeBlockCopyButtonMouseLeftButtonDown;
		border.PreviewMouseLeftButtonUp += OnCodeBlockCopyButtonMouseLeftButtonUp;
		border.MouseLeave += OnCodeBlockCopyButtonMouseLeave;
		ApplyCodeBlockCopyButtonTheme(border);
		return border;
	}

	private void ApplyCodeBlockCopyButtonTheme(Border copyButton)
	{
		if (copyButton == null)
		{
			return;
		}
		copyButton.Background = Brushes.Transparent;
		copyButton.BorderBrush = ThemeUtil.GetColor("BaseColorOpacity0_10", this);
		copyButton.Tag = CloneBrushWithOpacity(ThemeUtil.GetColor("CodeBackground", this), 0.88);
		SolidColorBrush color = ThemeUtil.GetColor("BaseColorOpacity40", this);
		if (!(copyButton.Child is StackPanel stackPanel))
		{
			return;
		}
		foreach (object child in stackPanel.Children)
		{
			if (child is TextBlock textBlock)
			{
				textBlock.Foreground = color;
			}
			else if (child is System.Windows.Shapes.Path path)
			{
				path.Fill = color;
			}
		}
	}

	private void RefreshHeadingFoldingPlaceholderStyle()
	{
		FoldingElementGenerator.PlaceholderStyle = new FoldingElementPlaceholderStyle
		{
			LeadingGap = 6.0,
			DotBrush = ThemeUtil.GetColor("BaseColorOpacity40", this),
			BlockBackgroundBrush = ThemeUtil.GetColor("BaseColorOpacity6", this),
			HoverDotBrush = ThemeUtil.GetColor("BaseColorOpacity60", this),
			HoverBlockBackgroundBrush = ThemeUtil.GetColor("BaseColorOpacity10", this)
		};
	}

	private static Brush CloneBrushWithOpacity(Brush brush, double opacity)
	{
		Brush brush2 = brush?.CloneCurrentValue();
		if (brush2 == null)
		{
			return Brushes.Transparent;
		}
		brush2.Opacity = Math.Max(0.0, Math.Min(1.0, opacity));
		return brush2;
	}

	private void RefreshCodeBlockLanguageOverlayAfterSemanticRefresh()
	{
		if (CodeBlockLanguageOverlay == null)
		{
			return;
		}
		CodeBlockLanguageEditorControl codeBlockLanguageEditorControl = _codeBlockLanguageEditorControl;
		if (codeBlockLanguageEditorControl != null && codeBlockLanguageEditorControl.Visibility == Visibility.Visible)
		{
			if (TryResolveCurrentCodeBlockOverlayBlock(_editingCodeBlockLanguageBlock, out var block))
			{
				_editingCodeBlockLanguageBlock = block;
				_codeBlockLanguageOverlayBlock = block;
			}
			else
			{
				HideCodeBlockLanguageEditorWithoutApply();
				_codeBlockLanguageOverlayBlock = null;
			}
		}
		else if (_codeBlockLanguageOverlayBlock != null)
		{
			_codeBlockLanguageOverlayBlock = (TryResolveCurrentCodeBlockOverlayBlock(_codeBlockLanguageOverlayBlock, out var block2) ? block2 : null);
		}
		ScheduleCodeBlockLanguageOverlayLayoutRefresh();
	}

	private void ScheduleCodeBlockLanguageOverlayLayoutRefresh()
	{
		base.Dispatcher?.BeginInvoke((Action)delegate
		{
			OnCodeBlockLanguageOverlayLayoutChanged(EditBox?.TextArea?.TextView, EventArgs.Empty);
		}, DispatcherPriority.Render);
	}

	private void HideCodeBlockLanguageEditorWithoutApply()
	{
		_codeBlockLanguageEditorOpening = false;
		_editingCodeBlockLanguageBlock = null;
		_codeBlockLanguageOverlayBlock = null;
		HideCodeBlockLanguageDropdown();
		if (_codeBlockLanguageEditorControl != null)
		{
			_codeBlockLanguageEditorControl.Visibility = Visibility.Collapsed;
		}
		DetachCodeBlockLanguageEditorDismissHandlers();
	}

	private bool TryResolveCurrentCodeBlockOverlayBlock(MarkdownBlockNode candidate, out MarkdownBlockNode block)
	{
		block = null;
		MarkdownBlockNode markdownBlockNode = candidate;
		if (markdownBlockNode == null || markdownBlockNode.Kind != MarkdownBlockKind.CodeBlock)
		{
			return false;
		}
		IList<MarkdownBlockNode> semanticBlocks = GetSemanticBlocks();
		if (semanticBlocks.Contains(candidate))
		{
			block = candidate;
			return true;
		}
		if (TryGetSemanticBlockAtLine(candidate.StartLine, out var block2) && IsMatchingCodeBlockOverlayTarget(candidate, block2))
		{
			block = block2;
			return true;
		}
		block = semanticBlocks.FirstOrDefault((MarkdownBlockNode currentBlock) => IsMatchingCodeBlockOverlayTarget(candidate, currentBlock));
		return block != null;
	}

	private static bool IsMatchingCodeBlockOverlayTarget(MarkdownBlockNode candidate, MarkdownBlockNode currentBlock)
	{
		if (candidate != null && currentBlock != null && currentBlock.Kind == MarkdownBlockKind.CodeBlock && string.Equals(candidate.Language ?? string.Empty, currentBlock.Language ?? string.Empty, StringComparison.Ordinal))
		{
			return string.Equals(candidate.CodeText ?? string.Empty, currentBlock.CodeText ?? string.Empty, StringComparison.Ordinal);
		}
		return false;
	}

	private IEnumerable<MarkdownBlockNode> GetVisibleCodeBlockLanguageBlocks()
	{
		foreach (MarkdownBlockNode semanticBlock in GetSemanticBlocks())
		{
			if (semanticBlock != null && semanticBlock.Kind == MarkdownBlockKind.CodeBlock && TryGetCodeBlockHeaderRectInTextView(semanticBlock, out var _))
			{
				yield return semanticBlock;
			}
		}
	}

	private void RefreshPersistentCodeBlockHeaders()
	{
		if (CodeBlockLanguageOverlay == null)
		{
			return;
		}
		ClearPersistentCodeBlockHeaders();
		CodeBlockLanguageEditorControl codeBlockLanguageEditorControl = _codeBlockLanguageEditorControl;
		if (codeBlockLanguageEditorControl != null && codeBlockLanguageEditorControl.Visibility == Visibility.Visible)
		{
			if (!TryResolveCurrentCodeBlockOverlayBlock(_editingCodeBlockLanguageBlock, out var block))
			{
				HideCodeBlockLanguageEditorWithoutApply();
			}
			else
			{
				_editingCodeBlockLanguageBlock = block;
				_codeBlockLanguageOverlayBlock = block;
				if (!TryPlaceCodeBlockLanguageControl(_editingCodeBlockLanguageBlock, _codeBlockLanguageEditorControl, 112.0, 20.0))
				{
					CommitCodeBlockLanguageEdit(apply: true, restoreEditorFocus: false);
					return;
				}
			}
		}
		foreach (MarkdownBlockNode visibleCodeBlockLanguageBlock in GetVisibleCodeBlockLanguageBlocks())
		{
			CodeBlockLanguageEditorControl codeBlockLanguageEditorControl2 = _codeBlockLanguageEditorControl;
			if (codeBlockLanguageEditorControl2 == null || codeBlockLanguageEditorControl2.Visibility != Visibility.Visible || visibleCodeBlockLanguageBlock != _editingCodeBlockLanguageBlock)
			{
				Border border = CreateCodeBlockLanguageBadge(visibleCodeBlockLanguageBlock);
				if (TryPlaceCodeBlockLanguageControl(visibleCodeBlockLanguageBlock, border, 112.0, 20.0))
				{
					System.Windows.Controls.Panel.SetZIndex(border, 1);
					CodeBlockLanguageOverlay.Children.Add(border);
					_codeBlockLanguageBadges.Add(border);
				}
			}
			Border border2 = CreateCodeBlockCopyButton(visibleCodeBlockLanguageBlock);
			if (TryPlaceCodeBlockCopyControl(visibleCodeBlockLanguageBlock, border2, 58.0, 20.0))
			{
				System.Windows.Controls.Panel.SetZIndex(border2, 1);
				CodeBlockLanguageOverlay.Children.Add(border2);
				_codeBlockCopyButtons.Add(border2);
			}
		}
	}

	private void ClearPersistentCodeBlockHeaders()
	{
		if (CodeBlockLanguageOverlay == null)
		{
			_codeBlockLanguageBadges.Clear();
			_codeBlockCopyButtons.Clear();
			return;
		}
		foreach (Border codeBlockLanguageBadge in _codeBlockLanguageBadges)
		{
			CodeBlockLanguageOverlay.Children.Remove(codeBlockLanguageBadge);
		}
		foreach (Border codeBlockCopyButton in _codeBlockCopyButtons)
		{
			CodeBlockLanguageOverlay.Children.Remove(codeBlockCopyButton);
		}
		_codeBlockLanguageBadges.Clear();
		_codeBlockCopyButtons.Clear();
	}

	private static string GetDisplayCodeBlockLanguage(MarkdownBlockNode block)
	{
		return CodeBlockLanguageCatalog.GetDisplayNameOrInput(block?.Language);
	}

	private void HideCodeBlockLanguageOverlay()
	{
		CodeBlockLanguageEditorControl codeBlockLanguageEditorControl = _codeBlockLanguageEditorControl;
		if (codeBlockLanguageEditorControl == null || codeBlockLanguageEditorControl.Visibility != Visibility.Visible)
		{
			ClearPersistentCodeBlockHeaders();
			_codeBlockLanguageOverlayBlock = null;
		}
	}

	private void OnCodeBlockLanguageOverlayLayoutChanged(object sender, EventArgs e)
	{
		RefreshPersistentCodeBlockHeaders();
	}

	private void OnCodeBlockLanguageOverlayScrollChanged(object sender, EventArgs e)
	{
		RefreshPersistentCodeBlockHeaders();
	}

	private void OnCodeBlockLanguageBadgeMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
	{
		if (sender is Border border)
		{
			border.Background = (border.Tag as Brush) ?? Brushes.Transparent;
		}
	}

	private void OnCodeBlockLanguageBadgeMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
	{
		if (sender is Border border)
		{
			border.Background = Brushes.Transparent;
		}
	}

	private void OnCodeBlockCopyButtonMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
	{
		if (sender is Border border)
		{
			border.Background = (border.Tag as Brush) ?? Brushes.Transparent;
		}
	}

	private void OnCodeBlockCopyButtonMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
	{
		if (sender is Border border)
		{
			border.Background = Brushes.Transparent;
		}
	}

	private void OnCodeBlockCopyButtonMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
	}

	private void OnCodeBlockCopyButtonMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		if ((sender as FrameworkElement)?.DataContext is MarkdownBlockNode block)
		{
			CopyCodeBlockContent(block);
		}
		e.Handled = true;
	}

	private void CopyCodeBlockContent(MarkdownBlockNode block)
	{
		if (block == null || block.Kind != MarkdownBlockKind.CodeBlock)
		{
			return;
		}
		try
		{
			if (_semanticDocument != null && EditBox != null)
			{
				SyncCodeBlocksFromDisplayText(_semanticDocument, EditBox.Text);
			}
			System.Windows.Clipboard.SetText(GetCodeBlockCopyText(block) ?? string.Empty);
		}
		catch (Exception)
		{
		}
	}

	private string GetCodeBlockCopyText(MarkdownBlockNode block)
	{
		if (block == null || block.Kind != MarkdownBlockKind.CodeBlock)
		{
			return string.Empty;
		}
		if (block.CodeText != null)
		{
			return block.CodeText;
		}
		if (EditBox?.Document == null)
		{
			return string.Empty;
		}
		int num = block.StartLine + 1;
		int lastCodeBlockContentLineIndex = GetLastCodeBlockContentLineIndex(block);
		if (num > lastCodeBlockContentLineIndex)
		{
			return string.Empty;
		}
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = num; i <= lastCodeBlockContentLineIndex; i++)
		{
			DocumentLine documentLine = TryGetDocumentLineBySemanticIndex(i);
			if (documentLine == null)
			{
				continue;
			}
			string text = EditBox.Document.GetText(documentLine);
			if (IsSemanticCodeBlockContentLine(text, block, i))
			{
				if (stringBuilder.Length > 0)
				{
					stringBuilder.Append('\n');
				}
				stringBuilder.Append(NormalizeDisplayTaskMarkerLine(text));
			}
		}
		return stringBuilder.ToString();
	}

	private void OnCodeBlockLanguageBadgeMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		CaptureCodeBlockLanguageEditFocus((sender as FrameworkElement)?.DataContext as MarkdownBlockNode);
		e.Handled = true;
	}

	private void OnCodeBlockLanguageBadgeMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		MarkdownBlockNode markdownBlockNode = (sender as FrameworkElement)?.DataContext as MarkdownBlockNode;
		if (ReadOnly || markdownBlockNode == null)
		{
			e.Handled = true;
			return;
		}
		BeginCodeBlockLanguageEdit(markdownBlockNode);
		e.Handled = true;
	}

	private void BeginCodeBlockLanguageEdit(MarkdownBlockNode block)
	{
		if (_codeBlockLanguageEditorControl != null && block != null)
		{
			if (_codeBlockLanguageFocusRestoreBlock != block)
			{
				CaptureCodeBlockLanguageEditFocus(block);
			}
			_editingCodeBlockLanguageBlock = block;
			_codeBlockLanguageOverlayBlock = block;
			_suppressCodeBlockLanguageDropdownRefresh = true;
			try
			{
				_codeBlockLanguageEditorControl.Text = block.Language ?? string.Empty;
			}
			finally
			{
				_suppressCodeBlockLanguageDropdownRefresh = false;
			}
			_codeBlockLanguageEditorOpening = true;
			if (!TryPlaceCodeBlockLanguageControl(block, _codeBlockLanguageEditorControl, 112.0, 20.0))
			{
				_editingCodeBlockLanguageBlock = null;
				ClearCodeBlockLanguageEditFocusRestore();
				_codeBlockLanguageEditorOpening = false;
			}
			else
			{
				_codeBlockLanguageEditorControl.Visibility = Visibility.Visible;
				RefreshPersistentCodeBlockHeaders();
				AttachCodeBlockLanguageEditorDismissHandlers();
				FocusCodeBlockLanguageEditor();
			}
		}
	}

	private void CaptureCodeBlockLanguageEditFocus(MarkdownBlockNode block)
	{
		_codeBlockLanguageFocusRestoreBlock = block;
		_restoreCodeBlockEditorFocusAfterLanguageEdit = block != null && (_codeBlockAttachmentElementGenerator?.IsCodeEditorKeyboardFocused(block) ?? false);
	}

	private void ClearCodeBlockLanguageEditFocusRestore()
	{
		_codeBlockLanguageFocusRestoreBlock = null;
		_restoreCodeBlockEditorFocusAfterLanguageEdit = false;
	}

	private void FocusCodeBlockLanguageEditor()
	{
		base.Dispatcher?.BeginInvoke((Action)delegate
		{
			CodeBlockLanguageEditorControl codeBlockLanguageEditorControl = _codeBlockLanguageEditorControl;
			if (codeBlockLanguageEditorControl == null || codeBlockLanguageEditorControl.Visibility != Visibility.Visible)
			{
				_codeBlockLanguageEditorOpening = false;
			}
			else
			{
				_codeBlockLanguageEditorControl.FocusEditor();
				_codeBlockLanguageEditorOpening = false;
			}
		}, DispatcherPriority.Input);
	}

	private bool IsCodeBlockLanguageEditorFocusWithin(DependencyObject focus)
	{
		if (!IsSelfOrDescendant(focus, _codeBlockLanguageEditorControl))
		{
			return IsSelfOrDescendant(focus, _codeBlockLanguageEditorControl?.DropdownChild);
		}
		return true;
	}

	private void OnCodeBlockLanguageEditorPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
	{
		if (e.Key == Key.Down)
		{
			CodeBlockLanguageEditorControl codeBlockLanguageEditorControl = _codeBlockLanguageEditorControl;
			if (codeBlockLanguageEditorControl != null && codeBlockLanguageEditorControl.IsDropdownOpen)
			{
				MoveCodeBlockLanguageDropdownSelection(1);
			}
			else
			{
				ShowCodeBlockLanguageDropdown();
			}
			e.Handled = true;
		}
		else if (e.Key == Key.Up)
		{
			CodeBlockLanguageEditorControl codeBlockLanguageEditorControl2 = _codeBlockLanguageEditorControl;
			if (codeBlockLanguageEditorControl2 != null && codeBlockLanguageEditorControl2.IsDropdownOpen)
			{
				MoveCodeBlockLanguageDropdownSelection(-1);
				e.Handled = true;
			}
		}
		else if (e.Key == Key.Return)
		{
			CodeBlockLanguageEditorControl codeBlockLanguageEditorControl3 = _codeBlockLanguageEditorControl;
			if (codeBlockLanguageEditorControl3 != null && codeBlockLanguageEditorControl3.IsDropdownOpen && TryGetSelectedCodeBlockLanguageDropdownValue(out var displayName))
			{
				SelectCodeBlockLanguageDropdownValue(displayName);
			}
			else
			{
				CommitCodeBlockLanguageEdit(apply: true, restoreEditorFocus: true);
			}
			e.Handled = true;
		}
		else if (e.Key == Key.Escape)
		{
			CommitCodeBlockLanguageEdit(apply: false, restoreEditorFocus: true);
			e.Handled = true;
		}
	}

	private void OnCodeBlockLanguageEditorLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
	{
		if (_codeBlockLanguageEditorOpening)
		{
			_codeBlockLanguageEditorOpening = false;
		}
		else if (!IsCodeBlockLanguageEditorFocusWithin(e.NewFocus as DependencyObject) && !_closingCodeBlockLanguageEditor)
		{
			CommitCodeBlockLanguageEdit(apply: true, restoreEditorFocus: false);
		}
	}

	private void OnCodeBlockLanguageEditorTextChanged(object sender, TextChangedEventArgs e)
	{
		if (!_suppressCodeBlockLanguageDropdownRefresh)
		{
			CodeBlockLanguageEditorControl codeBlockLanguageEditorControl = _codeBlockLanguageEditorControl;
			if (codeBlockLanguageEditorControl != null && codeBlockLanguageEditorControl.Visibility == Visibility.Visible && _codeBlockLanguageEditorControl.IsEditorKeyboardFocusWithin)
			{
				ShowCodeBlockLanguageDropdown();
			}
		}
	}

	private void ShowCodeBlockLanguageDropdown()
	{
		CodeBlockLanguageEditorControl codeBlockLanguageEditorControl = _codeBlockLanguageEditorControl;
		if (codeBlockLanguageEditorControl != null && codeBlockLanguageEditorControl.Visibility == Visibility.Visible)
		{
			_codeBlockLanguageEditorControl.ShowDropdown();
		}
	}

	private void HideCodeBlockLanguageDropdown()
	{
		_codeBlockLanguageEditorControl?.HideDropdown();
	}

	private void SelectCodeBlockLanguageDropdownValue(string displayName)
	{
		if (_codeBlockLanguageEditorControl != null)
		{
			_suppressCodeBlockLanguageDropdownRefresh = true;
			try
			{
				_codeBlockLanguageEditorControl.Text = displayName;
				_codeBlockLanguageEditorControl.Editor.CaretIndex = _codeBlockLanguageEditorControl.Text.Length;
			}
			finally
			{
				_suppressCodeBlockLanguageDropdownRefresh = false;
			}
			CommitCodeBlockLanguageEdit(apply: true, restoreEditorFocus: true);
		}
	}

	private void MoveCodeBlockLanguageDropdownSelection(int offset)
	{
		_codeBlockLanguageEditorControl?.MoveDropdownSelection(offset);
	}

	private bool TryGetSelectedCodeBlockLanguageDropdownValue(out string displayName)
	{
		displayName = null;
		return _codeBlockLanguageEditorControl?.TryGetSelectedDropdownValue(out displayName) ?? false;
	}

	private void OnCodeBlockLanguageSelected(object sender, string displayName)
	{
		SelectCodeBlockLanguageDropdownValue(displayName);
	}

	private void OnCodeBlockLanguageDropdownClosed(object sender, EventArgs e)
	{
	}

	private void CommitCodeBlockLanguageEdit(bool apply, bool restoreEditorFocus)
	{
		if (_codeBlockLanguageEditorControl == null || _codeBlockLanguageEditorControl.Visibility != Visibility.Visible || _closingCodeBlockLanguageEditor)
		{
			return;
		}
		_closingCodeBlockLanguageEditor = true;
		try
		{
			_codeBlockLanguageEditorOpening = false;
			MarkdownBlockNode editingCodeBlockLanguageBlock = _editingCodeBlockLanguageBlock;
			MarkdownBlockNode codeBlockLanguageFocusRestoreBlock = _codeBlockLanguageFocusRestoreBlock;
			bool flag = restoreEditorFocus && _restoreCodeBlockEditorFocusAfterLanguageEdit && codeBlockLanguageFocusRestoreBlock != null;
			string language = NormalizeCodeBlockLanguageInput(_codeBlockLanguageEditorControl.Text);
			HideCodeBlockLanguageDropdown();
			_codeBlockLanguageEditorControl.Visibility = Visibility.Collapsed;
			_editingCodeBlockLanguageBlock = null;
			_codeBlockLanguageOverlayBlock = null;
			ClearCodeBlockLanguageEditFocusRestore();
			DetachCodeBlockLanguageEditorDismissHandlers();
			if (apply && editingCodeBlockLanguageBlock != null)
			{
				UpdateCodeBlockLanguage(editingCodeBlockLanguageBlock, language);
			}
			RefreshPersistentCodeBlockHeaders();
			if (restoreEditorFocus)
			{
				RestoreFocusAfterCodeBlockLanguageEdit(flag ? codeBlockLanguageFocusRestoreBlock : null);
			}
		}
		finally
		{
			_closingCodeBlockLanguageEditor = false;
		}
	}

	private void AttachCodeBlockLanguageEditorDismissHandlers()
	{
		DetachCodeBlockLanguageEditorDismissHandlers();
		_codeBlockLanguageDropdownOwnerWindow = Window.GetWindow(this);
		if (_codeBlockLanguageDropdownOwnerWindow != null)
		{
			_codeBlockLanguageDropdownOwnerWindow.PreviewMouseDown += OnCodeBlockLanguageOwnerPreviewMouseDown;
			_codeBlockLanguageDropdownOwnerWindow.PreviewMouseWheel += OnCodeBlockLanguageOwnerPreviewMouseWheel;
			_codeBlockLanguageDropdownOwnerWindow.Deactivated += OnCodeBlockLanguageOwnerDeactivated;
			_codeBlockLanguageDropdownOwnerWindow.StateChanged += OnCodeBlockLanguageOwnerStateChanged;
		}
	}

	private void DetachCodeBlockLanguageEditorDismissHandlers()
	{
		if (_codeBlockLanguageDropdownOwnerWindow != null)
		{
			_codeBlockLanguageDropdownOwnerWindow.PreviewMouseDown -= OnCodeBlockLanguageOwnerPreviewMouseDown;
			_codeBlockLanguageDropdownOwnerWindow.PreviewMouseWheel -= OnCodeBlockLanguageOwnerPreviewMouseWheel;
			_codeBlockLanguageDropdownOwnerWindow.Deactivated -= OnCodeBlockLanguageOwnerDeactivated;
			_codeBlockLanguageDropdownOwnerWindow.StateChanged -= OnCodeBlockLanguageOwnerStateChanged;
			_codeBlockLanguageDropdownOwnerWindow = null;
		}
	}

	private void OnCodeBlockLanguageOwnerPreviewMouseDown(object sender, MouseButtonEventArgs e)
	{
		CodeBlockLanguageEditorControl codeBlockLanguageEditorControl = _codeBlockLanguageEditorControl;
		if (codeBlockLanguageEditorControl == null || codeBlockLanguageEditorControl.Visibility != Visibility.Visible)
		{
			DetachCodeBlockLanguageEditorDismissHandlers();
			return;
		}
		DependencyObject source = e.OriginalSource as DependencyObject;
		if (!IsSelfOrDescendant(source, _codeBlockLanguageEditorControl) && !IsSelfOrDescendant(source, _codeBlockLanguageEditorControl?.DropdownChild))
		{
			CommitCodeBlockLanguageEdit(apply: true, restoreEditorFocus: false);
		}
	}

	private void OnCodeBlockLanguageOwnerPreviewMouseWheel(object sender, MouseWheelEventArgs e)
	{
		CodeBlockLanguageEditorControl codeBlockLanguageEditorControl = _codeBlockLanguageEditorControl;
		if (codeBlockLanguageEditorControl != null && codeBlockLanguageEditorControl.Visibility == Visibility.Visible && _codeBlockLanguageEditorControl.IsDropdownOpen && _codeBlockLanguageEditorControl.IsMouseOverDropdown())
		{
			_codeBlockLanguageEditorControl.ScrollDropdownByWheelDelta(e.Delta);
			e.Handled = true;
		}
	}

	private void OnCodeBlockLanguageOwnerDeactivated(object sender, EventArgs e)
	{
		CommitCodeBlockLanguageEdit(apply: true, restoreEditorFocus: false);
	}

	private void OnCodeBlockLanguageOwnerStateChanged(object sender, EventArgs e)
	{
		if (sender is Window { WindowState: WindowState.Minimized })
		{
			CommitCodeBlockLanguageEdit(apply: true, restoreEditorFocus: false);
		}
	}

	private static bool IsSelfOrDescendant(DependencyObject source, DependencyObject ancestor)
	{
		while (source != null)
		{
			if (source == ancestor)
			{
				return true;
			}
			source = GetParent(source);
		}
		return false;
	}

	private static DependencyObject GetParent(DependencyObject element)
	{
		if (element is FrameworkElement { Parent: not null } frameworkElement)
		{
			return frameworkElement.Parent;
		}
		if (element is FrameworkContentElement { Parent: not null } frameworkContentElement)
		{
			return frameworkContentElement.Parent;
		}
		try
		{
			return VisualTreeHelper.GetParent(element);
		}
		catch (InvalidOperationException)
		{
			return null;
		}
	}

	private static string NormalizeCodeBlockLanguageInput(string language)
	{
		return CodeBlockLanguageCatalog.NormalizeLanguageInput(language);
	}

	private void UpdateCodeBlockLanguage(MarkdownBlockNode block, string language)
	{
		if (block == null || block.Kind != MarkdownBlockKind.CodeBlock)
		{
			return;
		}
		string normalizedLanguage = (string.IsNullOrWhiteSpace(language) ? null : language);
		string persistedText = GetPersistedText();
		block.Language = normalizedLanguage;
		DocumentLine documentLine = TryGetDocumentLineBySemanticIndex(block.StartLine);
		if (documentLine == null)
		{
			RefreshEditingDerivedMetadataWithoutMarkdownParsing();
			RequestImmediateRedrawAroundCaret();
			NotifySemanticTextChangedIfNeeded(persistedText);
			return;
		}
		string text = EditBox.Document.GetText(documentLine);
		if (!TryGetEditingCodeFenceInfo(text, out var _, out var _))
		{
			RefreshEditingDerivedMetadataWithoutMarkdownParsing();
			RequestImmediateRedrawForDocumentRange(documentLine.Offset, documentLine.EndOffset);
			NotifySemanticTextChangedIfNeeded(persistedText);
			return;
		}
		int num = text.Length - text.TrimStart().Length;
		string text2 = ((num > 0) ? text.Substring(0, num) : string.Empty) + "```" + (normalizedLanguage ?? string.Empty);
		if (string.Equals(text, text2, StringComparison.Ordinal))
		{
			RefreshEditingDerivedMetadataWithoutMarkdownParsing();
			RequestImmediateRedrawForDocumentRange(documentLine.Offset, documentLine.EndOffset);
			NotifySemanticTextChangedIfNeeded(persistedText);
			return;
		}
		ApplyDocumentMutationsAndNotify(new DocumentMutation[1]
		{
			new DocumentMutation
			{
				StartOffset = documentLine.Offset,
				RemoveLength = documentLine.Length,
				InsertText = text2
			}
		}, null, delegate
		{
			if (TryGetSemanticBlockAtLine(block.StartLine, out var block2) && block2 != null && block2.Kind == MarkdownBlockKind.CodeBlock)
			{
				block2.Language = normalizedLanguage;
			}
		});
	}

	private bool TryPlaceCodeBlockLanguageControl(MarkdownBlockNode block, FrameworkElement control, double preferredWidth, double preferredHeight)
	{
		return TryPlaceCodeBlockHeaderControl(block, control, preferredWidth, preferredHeight, alignRight: false);
	}

	private bool TryPlaceCodeBlockCopyControl(MarkdownBlockNode block, FrameworkElement control, double preferredWidth, double preferredHeight)
	{
		return TryPlaceCodeBlockHeaderControl(block, control, preferredWidth, preferredHeight, alignRight: true);
	}

	private bool TryPlaceCodeBlockHeaderControl(MarkdownBlockNode block, FrameworkElement control, double preferredWidth, double preferredHeight, bool alignRight)
	{
		if (block == null || control == null || !TryGetCodeBlockHeaderRectInTextView(block, out var headerRect) || !TryTranslateTextViewRectToLanguageOverlay(headerRect, out var overlayRect))
		{
			return false;
		}
		Visibility visibility = control.Visibility;
		if (visibility == Visibility.Collapsed)
		{
			control.Visibility = Visibility.Hidden;
		}
		control.Width = double.NaN;
		control.Height = double.NaN;
		control.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
		if (visibility == Visibility.Collapsed)
		{
			control.Visibility = visibility;
		}
		double num = Math.Min(180.0, Math.Max(preferredWidth, Math.Ceiling(control.DesiredSize.Width)));
		double num2 = Math.Max(preferredHeight, Math.Ceiling(control.DesiredSize.Height));
		double num3 = (alignRight ? Math.Max(overlayRect.Left + 8.0, overlayRect.Right - num - 6.0) : (overlayRect.Left + 8.0));
		if (!alignRight && num3 + num > overlayRect.Right - 8.0)
		{
			num3 = Math.Max(overlayRect.Left + 8.0, overlayRect.Right - num - 8.0);
		}
		double length = overlayRect.Top + Math.Max(0.0, (overlayRect.Height - num2) / 2.0);
		control.Width = num;
		control.Height = num2;
		Canvas.SetLeft(control, num3);
		Canvas.SetTop(control, length);
		return true;
	}

	private bool TryTranslateTextViewRectToLanguageOverlay(Rect textViewRect, out Rect overlayRect)
	{
		overlayRect = Rect.Empty;
		if (CodeBlockLanguageOverlay == null || EditBox?.TextArea?.TextView == null)
		{
			return false;
		}
		try
		{
			GeneralTransform generalTransform = EditBox.TextArea.TextView.TransformToVisual(CodeBlockLanguageOverlay);
			Point point = generalTransform.Transform(new Point(textViewRect.Left, textViewRect.Top));
			Point point2 = generalTransform.Transform(new Point(textViewRect.Right, textViewRect.Bottom));
			overlayRect = new Rect(point, point2);
			return overlayRect.Width > 0.0 && overlayRect.Height > 0.0;
		}
		catch (InvalidOperationException)
		{
			return false;
		}
		catch (ArgumentException)
		{
			return false;
		}
		catch (VisualLinesInvalidException)
		{
			return false;
		}
	}

	private bool TryGetCodeBlockHeaderRectInTextView(MarkdownBlockNode block, out Rect headerRect)
	{
		headerRect = Rect.Empty;
		if (block == null || block.Kind != MarkdownBlockKind.CodeBlock || IsSemanticBlockHiddenByHeadingFold(block) || EditBox?.Document == null || EditBox.TextArea?.TextView == null)
		{
			return false;
		}
		DocumentLine documentLine = TryGetDocumentLineBySemanticIndex(block.StartLine);
		if (documentLine == null)
		{
			return false;
		}
		TextView textView = EditBox.TextArea.TextView;
		if (textView.GetVisualLine(documentLine.LineNumber) == null)
		{
			return false;
		}
		try
		{
			TextViewPosition position = new TextViewPosition(documentLine.LineNumber, 1);
			double num = textView.GetVisualPosition(position, VisualYPosition.LineTop).Y - textView.ScrollOffset.Y;
			double num2 = textView.GetVisualPosition(position, VisualYPosition.LineBottom).Y - textView.ScrollOffset.Y;
			double num3 = (IsCodeBlockAttachment(block) ? 24.0 : Math.Max(18.0, num2 - num));
			if (num + num3 < 0.0 || num > textView.ActualHeight)
			{
				return false;
			}
			headerRect = new Rect(0.0, num, Math.Max(0.0, textView.ActualWidth - 7.0), num3);
			return true;
		}
		catch (InvalidOperationException)
		{
			return false;
		}
		catch (ArgumentException)
		{
			return false;
		}
		catch (VisualLinesInvalidException)
		{
			return false;
		}
	}

	private void RestoreFocusAfterCodeBlockLanguageEdit(MarkdownBlockNode codeBlock)
	{
		if (codeBlock != null)
		{
			CodeBlockAttachmentElementGenerator codeBlockAttachmentElementGenerator = _codeBlockAttachmentElementGenerator;
			if (codeBlockAttachmentElementGenerator != null && codeBlockAttachmentElementGenerator.TryFocusCodeEditorPreservingCaret(codeBlock))
			{
				return;
			}
		}
		base.Dispatcher?.BeginInvoke((Action)delegate
		{
			if (codeBlock != null)
			{
				CodeBlockAttachmentElementGenerator codeBlockAttachmentElementGenerator2 = _codeBlockAttachmentElementGenerator;
				if (codeBlockAttachmentElementGenerator2 != null && codeBlockAttachmentElementGenerator2.TryFocusCodeEditorPreservingCaret(codeBlock))
				{
					return;
				}
			}
			FocusEditBox();
		}, DispatcherPriority.Loaded);
	}

	private bool TryPlaceCaretAfterCodeBlockAttachmentFromPoint(Point textViewPoint)
	{
		if (EditBox?.Document == null || EditBox.TextArea?.TextView == null)
		{
			return false;
		}
		foreach (MarkdownBlockNode semanticBlock in GetSemanticBlocks())
		{
			if (IsCodeBlockAttachment(semanticBlock) && semanticBlock != null && semanticBlock.Kind == MarkdownBlockKind.CodeBlock && TryGetCodeBlockRectInTextView(semanticBlock, out var codeBlockRect) && !(textViewPoint.Y <= codeBlockRect.Top + 24.0) && IsPointInCodeBlockOutsideRightArea(textViewPoint, codeBlockRect) && TrySetCaretAfterCodeBlockAttachment(semanticBlock))
			{
				return true;
			}
		}
		return false;
	}

	private bool TrySetCaretAfterCodeBlockAttachment(MarkdownBlockNode block)
	{
		TextDocument textDocument = EditBox?.Document;
		if (textDocument == null || block == null || block.Kind != MarkdownBlockKind.CodeBlock)
		{
			return false;
		}
		if (IsCodeBlockAttachment(block))
		{
			DocumentLine lineByNumber = textDocument.GetLineByNumber(block.StartLine + 1);
			if (lineByNumber == null)
			{
				return false;
			}
			EditBox.Focus();
			ClearObjectSelectedCodeBlocks();
			EditBox.SelectionLength = 0;
			EditBox.TextArea.Caret.Position = new TextViewPosition(textDocument.GetLocation(lineByNumber.EndOffset), lineByNumber.Length);
			EditBox.TextArea.Caret.DesiredXPos = double.NaN;
			EditBox.TextArea.Caret.Show();
			RecordLastInsertRangeFromEditor();
			RefreshOpenMenuContexts();
			return true;
		}
		DocumentLine lineByNumber2 = textDocument.GetLineByNumber(block.EndLine + 1);
		if (lineByNumber2 == null || !IsSemanticCodeBlockClosingFenceLine(textDocument.GetText(lineByNumber2), block, block.EndLine))
		{
			return false;
		}
		EditBox.Focus();
		EditBox.SelectionLength = 0;
		EditBox.CaretOffset = lineByNumber2.EndOffset;
		RecordLastInsertRangeFromEditor();
		RefreshOpenMenuContexts();
		return true;
	}

	private bool TrySetCaretBeforeCodeBlockAttachment(MarkdownBlockNode block)
	{
		TextDocument textDocument = EditBox?.Document;
		if (!IsCodeBlockAttachment(block) || textDocument == null)
		{
			return false;
		}
		DocumentLine lineByNumber = textDocument.GetLineByNumber(block.StartLine + 1);
		if (lineByNumber == null)
		{
			return false;
		}
		EditBox.Focus();
		ClearObjectSelectedCodeBlocks();
		EditBox.SelectionLength = 0;
		EditBox.TextArea.Caret.Position = new TextViewPosition(textDocument.GetLocation(lineByNumber.Offset), 0);
		EditBox.TextArea.Caret.DesiredXPos = double.NaN;
		EditBox.TextArea.Caret.Show();
		RecordLastInsertRangeFromEditor();
		RefreshOpenMenuContexts();
		return true;
	}

	internal bool TryPlaceCaretAfterCodeBlockAttachment(MarkdownBlockNode block)
	{
		return TrySetCaretAfterCodeBlockAttachment(block);
	}

	internal bool TryInsertParagraphAfterCodeBlockAttachment(MarkdownBlockNode block)
	{
		TextDocument textDocument = EditBox?.Document;
		if (!CanMutateCodeBlockAttachment(ReadOnly, block) || !IsCodeBlockAttachment(block) || textDocument == null || WouldExceedMaxLengthForContentInsertion(1))
		{
			return false;
		}
		DocumentLine lineByNumber = textDocument.GetLineByNumber(block.StartLine + 1);
		if (lineByNumber == null)
		{
			return false;
		}
		int insertOffset = lineByNumber.EndOffset;
		int insertedLineIndex = lineByNumber.LineNumber;
		EditorScrollSnapshot editorScrollSnapshot = CaptureEditorScrollSnapshot();
		_codeBlockParagraphExitCaretPlacementPending = true;
		try
		{
			RunWithoutCaretVerticalOffsetNotification(delegate
			{
				EditBox.Focus();
				ClearObjectSelectedCodeBlocks();
				EditBox.SelectionLength = 0;
				ApplyDocumentMutationsAndNotify(new DocumentMutation[1]
				{
					new DocumentMutation
					{
						StartOffset = insertOffset,
						RemoveLength = 0,
						InsertText = "\n"
					}
				}, insertOffset + 1, delegate
				{
					SetSemanticParagraphForLine(insertedLineIndex, 0);
				});
				EditBox.TextArea.Caret.DesiredXPos = double.NaN;
				EditBox.TextArea.Caret.Show();
			});
		}
		catch
		{
			_codeBlockParagraphExitCaretPlacementPending = false;
			throw;
		}
		RestoreEditorScrollSnapshot(editorScrollSnapshot);
		ScheduleCodeBlockParagraphExitCaretPlacement(editorScrollSnapshot);
		RecordLastInsertRangeFromEditor();
		RefreshOpenMenuContexts();
		return true;
	}

	private Rect TransformParentCaretRectangleAtAttachmentEnd(Rect caretRectangle)
	{
		if (caretRectangle.IsEmpty || EditBox?.Document == null || !TryGetRenderedAttachmentBottomAtCaret(EditBox.CaretOffset, out var attachmentBottom))
		{
			return caretRectangle;
		}
		return AlignCaretRectangleBottom(caretRectangle, attachmentBottom);
	}

	private bool TryGetRenderedAttachmentBottomAtCaret(int caretOffset, out double attachmentBottom)
	{
		attachmentBottom = double.NaN;
		TextDocument textDocument = EditBox?.Document;
		TextView textView = EditBox?.TextArea?.TextView;
		if (textDocument == null || textView == null)
		{
			return false;
		}
		if (AttachmentDict != null)
		{
			foreach (KeyValuePair<int, AttachmentInfo> item in AttachmentDict)
			{
				int valueOrDefault = (item.Value?.Value?.Length).GetValueOrDefault();
				if (valueOrDefault > 0 && caretOffset == item.Key + valueOrDefault)
				{
					if (_imageElementGenerator != null && _imageElementGenerator.TryGetControlBounds(item.Key, textView, out var rect))
					{
						attachmentBottom = rect.Bottom + textView.ScrollOffset.Y;
						return true;
					}
					return false;
				}
			}
		}
		if (caretOffset <= 0)
		{
			return false;
		}
		int offset = Math.Max(0, Math.Min(caretOffset - 1, textDocument.TextLength));
		DocumentLine lineByOffset = textDocument.GetLineByOffset(offset);
		int lineIndex = (lineByOffset?.LineNumber ?? 1) - 1;
		if (lineByOffset == null || caretOffset != lineByOffset.EndOffset || !TryGetSemanticBlockAtLine(lineIndex, out var block) || !IsCodeBlockAttachment(block))
		{
			return false;
		}
		if (!TryGetCodeBlockAttachmentBounds(block, out var rect2))
		{
			return false;
		}
		attachmentBottom = rect2.Bottom + textView.ScrollOffset.Y;
		return true;
	}

	internal static Rect AlignCaretRectangleBottom(Rect caretRectangle, double objectBottom)
	{
		if (caretRectangle.IsEmpty || double.IsNaN(objectBottom) || double.IsInfinity(objectBottom))
		{
			return caretRectangle;
		}
		caretRectangle.Y = objectBottom - caretRectangle.Height;
		return caretRectangle;
	}

	private void ScheduleCodeBlockParagraphExitCaretPlacement(EditorScrollSnapshot scrollSnapshot)
	{
		if (base.Dispatcher == null || !base.IsLoaded)
		{
			_codeBlockParagraphExitCaretPlacementPending = false;
			return;
		}
		base.Dispatcher.BeginInvoke((Action)delegate
		{
			try
			{
				UpdateLayout();
				TryEnsureVisualLines(EditBox?.TextArea?.TextView);
				RestoreEditorScrollSnapshot(scrollSnapshot);
			}
			finally
			{
				_codeBlockParagraphExitCaretPlacementPending = false;
			}
			NotifyCaretChanged(this, EventArgs.Empty);
		}, DispatcherPriority.ContextIdle);
	}

	internal bool TryGetCodeBlockAttachmentBounds(MarkdownBlockNode block, out Rect rect)
	{
		rect = Rect.Empty;
		if (_codeBlockAttachmentElementGenerator != null)
		{
			return _codeBlockAttachmentElementGenerator.TryGetControlBounds(block, EditBox?.TextArea?.TextView, out rect);
		}
		return false;
	}

	private bool TryFocusCodeBlockAttachment(MarkdownBlockNode block, bool moveToEnd)
	{
		if (!IsCodeBlockAttachment(block) || _codeBlockAttachmentElementGenerator == null)
		{
			return false;
		}
		if (_codeBlockAttachmentElementGenerator.TryFocusCodeEditor(block, moveToEnd))
		{
			return true;
		}
		TryEnsureVisualLines(EditBox?.TextArea?.TextView);
		UpdateLayout();
		return _codeBlockAttachmentElementGenerator.TryFocusCodeEditor(block, moveToEnd);
	}

	internal bool TryRestoreCodeBlockAttachmentFocus(MarkdownBlockNode block, int caretOffset, int selectionStart, int selectionLength, int? caretLine, int? caretColumn)
	{
		if (block == null || _codeBlockAttachmentElementGenerator == null)
		{
			return false;
		}
		MarkdownBlockNode block2 = block;
		if (!GetSemanticBlocks().Contains(block2) && (!TryGetSemanticBlockAtLine(block.StartLine, out block2) || !IsCodeBlockAttachment(block2)))
		{
			return false;
		}
		TryEnsureVisualLines(EditBox?.TextArea?.TextView);
		UpdateLayout();
		return _codeBlockAttachmentElementGenerator.TryRestoreCodeEditorFocus(block2, caretOffset, selectionStart, selectionLength, caretLine, caretColumn);
	}

	private bool TryFocusCodeBlockAttachmentAtLine(int lineIndex, bool moveToEnd)
	{
		if (TryGetSemanticBlockAtLine(lineIndex, out var block))
		{
			return TryFocusCodeBlockAttachment(block, moveToEnd);
		}
		return false;
	}

	private bool TryFocusCodeBlockAttachmentFromParentCaret(Key key)
	{
		if (EditBox?.Document == null || EditBox.SelectionLength > 0)
		{
			return false;
		}
		int num = Math.Max(0, Math.Min(EditBox.CaretOffset, EditBox.Document.TextLength));
		DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(num);
		if (lineByOffset == null)
		{
			return false;
		}
		if (TryGetCodeBlockAttachmentForDocumentLine(lineByOffset, out var block))
		{
			if (num <= lineByOffset.Offset && (key == Key.Right || key == Key.Down))
			{
				return TryFocusCodeBlockAttachment(block, moveToEnd: false);
			}
			if (num >= lineByOffset.EndOffset && (key == Key.Left || key == Key.Up))
			{
				return TryFocusCodeBlockAttachment(block, moveToEnd: true);
			}
		}
		if (((key == Key.Down && IsParentCaretOnBoundaryVisualRow(lineByOffset, firstRow: false)) || (key == Key.Right && num >= lineByOffset.EndOffset)) && TryGetCodeBlockAttachmentForDocumentLine(lineByOffset.NextLine, out var block2))
		{
			return TryFocusCodeBlockAttachment(block2, moveToEnd: false);
		}
		if (((key == Key.Up && IsParentCaretOnBoundaryVisualRow(lineByOffset, firstRow: true)) || (key == Key.Left && num <= lineByOffset.Offset)) && TryGetCodeBlockAttachmentForDocumentLine(lineByOffset.PreviousLine, out var block3))
		{
			return TryFocusCodeBlockAttachment(block3, moveToEnd: true);
		}
		return false;
	}

	private bool IsParentCaretOnBoundaryVisualRow(DocumentLine line, bool firstRow)
	{
		TextView textView = EditBox?.TextArea?.TextView;
		if (line == null || textView == null)
		{
			return false;
		}
		try
		{
			textView.EnsureVisualLines();
			VisualLine orConstructVisualLine = textView.GetOrConstructVisualLine(line);
			if (orConstructVisualLine?.TextLines == null || orConstructVisualLine.TextLines.Count == 0)
			{
				return false;
			}
			int relativeTextOffset = Math.Max(line.Offset, Math.Min(EditBox.CaretOffset, line.EndOffset)) - orConstructVisualLine.FirstDocumentLine.Offset;
			int visualColumn = orConstructVisualLine.GetVisualColumn(relativeTextOffset);
			TextLine textLine = orConstructVisualLine.GetTextLine(visualColumn, EditBox.TextArea.Caret.Position.IsAtEndOfLine);
			return firstRow ? (textLine == orConstructVisualLine.TextLines[0]) : (textLine == orConstructVisualLine.TextLines[orConstructVisualLine.TextLines.Count - 1]);
		}
		catch (InvalidOperationException)
		{
			return false;
		}
		catch (VisualLinesInvalidException)
		{
			return false;
		}
	}

	private bool TryGetCodeBlockAttachmentForDocumentLine(DocumentLine line, out MarkdownBlockNode block)
	{
		block = null;
		if (line != null && line.Length == 1 && EditBox?.Document?.GetText(line) == '\ue000'.ToString() && TryGetSemanticBlockAtLine(line.LineNumber - 1, out block))
		{
			return IsCodeBlockAttachment(block);
		}
		return false;
	}

	private void ScheduleFocusCodeBlockAttachment(int lineIndex, bool moveToEnd)
	{
		base.Dispatcher?.BeginInvoke((Action)delegate
		{
			DocumentLine documentLine = TryGetDocumentLineBySemanticIndex(lineIndex);
			if (documentLine != null)
			{
				RequestImmediateRedrawForDocumentRange(documentLine.Offset, documentLine.EndOffset);
			}
			UpdateLayout();
			TryEnsureVisualLines(EditBox?.TextArea?.TextView);
			UpdateLayout();
			if (!TryFocusCodeBlockAttachmentAtLine(lineIndex, moveToEnd))
			{
				base.Dispatcher?.BeginInvoke((Action)delegate
				{
					UpdateLayout();
					TryEnsureVisualLines(EditBox?.TextArea?.TextView);
					TryFocusCodeBlockAttachmentAtLine(lineIndex, moveToEnd);
				}, DispatcherPriority.ContextIdle);
			}
		}, DispatcherPriority.Loaded);
	}

	internal void HideParentCaretForCodeBlockAttachment()
	{
		_suppressParentCaretForCodeBlockAttachment = true;
		if (EditBox?.TextArea?.Caret != null)
		{
			EditBox.TextArea.Caret.CaretBrush = Brushes.Transparent;
			EditBox.TextArea.Caret.Hide();
		}
	}

	internal void RefreshParentCaretVisibilityAfterCodeBlockFocusChange()
	{
		base.Dispatcher?.BeginInvoke((Action)delegate
		{
			if (EditBox?.TextArea?.Caret != null)
			{
				IInputElement focusedElement = Keyboard.FocusedElement;
				if (CodeBlockAttachmentControl.TryFindFromOriginalSource(focusedElement, out var control) && control.IsSourceInsideCodeEditor(focusedElement))
				{
					HideParentCaretForCodeBlockAttachment();
				}
				else
				{
					_suppressParentCaretForCodeBlockAttachment = false;
					EditBox.TextArea.Caret.CaretBrush = ResolveParentCaretBrush(_textColor);
					if (EditBox.IsKeyboardFocusWithin)
					{
						EditBox.TextArea.Caret.Show();
					}
				}
			}
		}, DispatcherPriority.Input);
	}

	internal void UpdateCodeBlockAttachmentContent(MarkdownBlockNode block, string codeText)
	{
		if (!CanMutateCodeBlockAttachment(ReadOnly, block) || _semanticDocument?.Blocks == null)
		{
			return;
		}
		MarkdownBlockNode markdownBlockNode = (_semanticDocument.Blocks.Contains(block) ? block : _semanticDocument.Blocks.FirstOrDefault((MarkdownBlockNode candidate) => candidate != null && candidate.Kind == MarkdownBlockKind.CodeBlock && candidate.StartLine == block.StartLine && candidate.EndLine == block.EndLine));
		if (markdownBlockNode != null && IsCodeBlockAttachment(markdownBlockNode))
		{
			string text = (codeText ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
			if (!string.Equals(markdownBlockNode.CodeText ?? string.Empty, text, StringComparison.Ordinal))
			{
				markdownBlockNode.CodeText = text;
				MarkLocalDocumentChangePending();
				NotifySemanticTextChanged(GetPersistedText());
			}
		}
	}

	internal static bool CanMutateCodeBlockAttachment(bool readOnly, MarkdownBlockNode block)
	{
		if (!readOnly)
		{
			if (block == null)
			{
				return false;
			}
			return block.Kind == MarkdownBlockKind.CodeBlock;
		}
		return false;
	}

	internal int GetContentLengthForMaxLength()
	{
		long num = (EditBox?.Document?.TextLength).GetValueOrDefault();
		foreach (MarkdownBlockNode semanticBlock in GetSemanticBlocks())
		{
			if (IsCodeBlockAttachment(semanticBlock))
			{
				num += (long)(semanticBlock.CodeText?.Length ?? 0) - 1L;
			}
		}
		return (int)Math.Max(0L, Math.Min(2147483647L, num));
	}

	internal bool WouldExceedMaxLengthForContentInsertion(int insertionLength, int removalLength = 0)
	{
		if (MaxLength < 0)
		{
			return false;
		}
		return Math.Max(0L, (long)GetContentLengthForMaxLength() - (long)Math.Max(0, removalLength)) + Math.Max(0, insertionLength) > MaxLength;
	}

	private int GetSelectionLengthForMaxLength(int selectionStart, int selectionLength)
	{
		TextDocument textDocument = EditBox?.Document;
		if (textDocument == null || selectionLength <= 0)
		{
			return 0;
		}
		int num = Math.Max(0, Math.Min(selectionStart, textDocument.TextLength));
		int num2 = Math.Max(num, Math.Min(num + selectionLength, textDocument.TextLength));
		long num3 = num2 - num;
		foreach (MarkdownBlockNode semanticBlock in GetSemanticBlocks())
		{
			if (IsCodeBlockAttachment(semanticBlock))
			{
				DocumentLine lineByNumber = textDocument.GetLineByNumber(semanticBlock.StartLine + 1);
				if (lineByNumber != null && num <= lineByNumber.Offset && num2 > lineByNumber.Offset)
				{
					num3 += (long)(semanticBlock.CodeText?.Length ?? 0) - 1L;
				}
			}
		}
		return (int)Math.Max(0L, Math.Min(2147483647L, num3));
	}

	internal bool TryPerformSemanticHistoryFromCodeBlockAttachment(MarkdownBlockNode block, bool redo, int caretOffset, int selectionStart, int selectionLength)
	{
		if (ReadOnly || !IsCodeBlockAttachment(block))
		{
			return false;
		}
		int lineIndex = block.StartLine;
		string previousCodeText = block.CodeText ?? string.Empty;
		if (!(redo ? TryPerformSemanticRedo() : TryPerformSemanticUndo()))
		{
			return false;
		}
		base.Dispatcher?.BeginInvoke((Action)delegate
		{
			EditBox?.TextArea?.TextView?.Redraw();
			UpdateLayout();
			TryEnsureVisualLines(EditBox?.TextArea?.TextView);
			if (TryGetSemanticBlockAtLine(lineIndex, out var block2) && IsCodeBlockAttachment(block2))
			{
				string text = block2.CodeText ?? string.Empty;
				int length = text.Length;
				int caretOffset2 = TranslateCodeBlockOffsetAcrossHistoryChange(previousCodeText, text, caretOffset);
				int num = TranslateCodeBlockOffsetAcrossHistoryChange(previousCodeText, text, selectionStart);
				int num2 = TranslateCodeBlockOffsetAcrossHistoryChange(previousCodeText, text, Math.Max(selectionStart, selectionStart + selectionLength));
				int selectionLength2 = Math.Max(0, Math.Min(num2 - num, length - num));
				TryRestoreCodeBlockAttachmentFocus(block2, caretOffset2, num, selectionLength2, null, null);
			}
		}, DispatcherPriority.Loaded);
		return true;
	}

	internal static int TranslateCodeBlockOffsetAcrossHistoryChange(string previousText, string currentText, int offset)
	{
		previousText = previousText ?? string.Empty;
		currentText = currentText ?? string.Empty;
		int num = Math.Max(0, Math.Min(offset, previousText.Length));
		int i = 0;
		for (int num2 = Math.Min(previousText.Length, currentText.Length); i < num2 && previousText[i] == currentText[i]; i++)
		{
		}
		int j;
		for (j = 0; j < previousText.Length - i && j < currentText.Length - i && previousText[previousText.Length - j - 1] == currentText[currentText.Length - j - 1]; j++)
		{
		}
		int num3 = previousText.Length - j;
		int num4 = currentText.Length - j;
		if (num < i || (num == i && num3 > i))
		{
			return num;
		}
		if (num >= num3)
		{
			return Math.Max(0, Math.Min(currentText.Length, num + num4 - num3));
		}
		return num4;
	}

	internal bool TrySelectAllFromCodeBlockAttachment(MarkdownBlockNode block)
	{
		if (!IsCodeBlockAttachment(block) || EditBox?.Document == null)
		{
			return false;
		}
		int textLength = EditBox.Document.TextLength;
		if (textLength <= 0)
		{
			return false;
		}
		EditBox.Focus();
		ClearSemanticSelectionDragState();
		ClearObjectSelectedCodeBlocks();
		foreach (MarkdownBlockNode semanticBlock in GetSemanticBlocks())
		{
			if (IsCodeBlockAttachment(semanticBlock))
			{
				GetObjectSelectedCodeBlockKeys().Add(GetCodeBlockSelectionKey(semanticBlock));
			}
		}
		bool suppressSemanticSelectionNormalization = _suppressSemanticSelectionNormalization;
		_suppressSemanticSelectionNormalization = true;
		try
		{
			ApplyNormalizedSemanticSelection(0, textLength, textLength);
		}
		finally
		{
			_suppressSemanticSelectionNormalization = suppressSemanticSelectionNormalization;
		}
		RecordLastInsertRangeFromEditor();
		RefreshOpenMenuContexts();
		_codeBlockSelectionLayer?.InvalidateVisual();
		if (EditBox.SelectionStart == 0)
		{
			return EditBox.SelectionLength == textLength;
		}
		return false;
	}

	internal bool TryMoveCaretAboveCodeBlockAttachment(MarkdownBlockNode block)
	{
		if (!IsCodeBlockAttachment(block) || EditBox?.Document == null)
		{
			return false;
		}
		DocumentLine lineByNumber = EditBox.Document.GetLineByNumber(block.StartLine + 1);
		DocumentLine documentLine = lineByNumber?.PreviousLine;
		if (documentLine == null)
		{
			if (ReadOnly || lineByNumber == null)
			{
				return false;
			}
			if (WouldExceedMaxLengthForContentInsertion(1))
			{
				Utils.Toast(Utils.GetString("CharactersNumberLimit"));
				return false;
			}
			ApplyDocumentMutationsAndNotify(new DocumentMutation[1]
			{
				new DocumentMutation
				{
					StartOffset = 0,
					RemoveLength = 0,
					InsertText = "\n"
				}
			}, 0, delegate
			{
				SetSemanticParagraphForLine(0, 0);
			});
			EditBox.SelectionLength = 0;
			EditBox.CaretOffset = 0;
			RecordLastInsertRangeFromEditor();
			RefreshOpenMenuContexts();
			return true;
		}
		EditBox.Focus();
		EditBox.SelectionLength = 0;
		EditBox.CaretOffset = documentLine.EndOffset;
		RecordLastInsertRangeFromEditor();
		RefreshOpenMenuContexts();
		return true;
	}

	private bool TryInsertParagraphBeforeCodeBlockAttachmentAtCaret()
	{
		bool blockedByMaxLength;
		bool result = TryNormalizeCaretBeforeCodeBlockAttachment(0, out blockedByMaxLength);
		if (blockedByMaxLength)
		{
			Utils.Toast(Utils.GetString("CharactersNumberLimit"));
			return true;
		}
		return result;
	}

	private bool TryNormalizeCaretBeforeCodeBlockAttachment(int contentLengthAfterInsertedParagraph, out bool blockedByMaxLength)
	{
		blockedByMaxLength = false;
		if (EditBox?.Document == null || EditBox.SelectionLength > 0)
		{
			return false;
		}
		TextDocument document = EditBox.Document;
		int num = Math.Max(0, Math.Min(EditBox.CaretOffset, document.TextLength));
		DocumentLine lineByOffset = document.GetLineByOffset(num);
		int insertOffset = lineByOffset?.Offset ?? (-1);
		int lineIndex = Math.Max(0, (lineByOffset?.LineNumber ?? 1) - 1);
		if (lineByOffset == null || num != insertOffset || !TryGetCodeBlockAttachmentForDocumentLine(lineByOffset, out var _))
		{
			return false;
		}
		if (WouldExceedMaxLengthForContentInsertion(1 + Math.Max(0, contentLengthAfterInsertedParagraph)))
		{
			blockedByMaxLength = true;
			return false;
		}
		RunWithoutCaretVerticalOffsetNotification(delegate
		{
			EditBox.Focus();
			ClearObjectSelectedCodeBlocks();
			EditBox.SelectionLength = 0;
			ApplyDocumentMutationsAndNotify(new DocumentMutation[1]
			{
				new DocumentMutation
				{
					StartOffset = insertOffset,
					RemoveLength = 0,
					InsertText = "\n"
				}
			}, insertOffset, delegate
			{
				SetSemanticParagraphForLine(lineIndex, 0);
			});
			EditBox.SelectionLength = 0;
			EditBox.CaretOffset = insertOffset;
		});
		NotifyCaretChanged(this, EventArgs.Empty);
		RecordLastInsertRangeFromEditor();
		RefreshOpenMenuContexts();
		return true;
	}

	internal bool TryMoveCaretBelowCodeBlockAttachment(MarkdownBlockNode block)
	{
		if (!IsCodeBlockAttachment(block) || EditBox?.Document == null)
		{
			return false;
		}
		EditBox.Focus();
		EditBox.SelectionLength = 0;
		bool num = TryMoveCaretOutsideCodeBlock(block);
		if (num)
		{
			RecordLastInsertRangeFromEditor();
			RefreshOpenMenuContexts();
		}
		return num;
	}

	internal void RefreshCodeBlockAttachmentLayout()
	{
		TextDocument textDocument = EditBox?.Document;
		if (textDocument != null)
		{
			foreach (MarkdownBlockNode item in GetSemanticBlocks().Where(IsCodeBlockAttachment))
			{
				DocumentLine lineByNumber = textDocument.GetLineByNumber(item.StartLine + 1);
				if (lineByNumber != null)
				{
					RequestImmediateRedrawForDocumentRange(lineByNumber.Offset, lineByNumber.EndOffset);
				}
			}
		}
		RefreshPersistentCodeBlockHeaders();
	}

	private bool TryNormalizeCaretAfterCodeBlockAttachment(int contentLengthAfterAppendedParagraph, out bool appendedParagraph, out bool blockedByMaxLength)
	{
		appendedParagraph = false;
		blockedByMaxLength = false;
		if (EditBox?.Document == null || EditBox.SelectionLength > 0)
		{
			return false;
		}
		int caretOffset = EditBox.CaretOffset;
		foreach (MarkdownBlockNode semanticBlock in GetSemanticBlocks())
		{
			if (!IsCodeBlockAttachment(semanticBlock))
			{
				continue;
			}
			if (IsCodeBlockAttachment(semanticBlock))
			{
				DocumentLine lineByNumber = EditBox.Document.GetLineByNumber(semanticBlock.StartLine + 1);
				if (lineByNumber != null && caretOffset == lineByNumber.EndOffset)
				{
					bool flag = lineByNumber.NextLine == null;
					if (flag && WouldExceedMaxLengthForContentInsertion(1 + Math.Max(0, contentLengthAfterAppendedParagraph)))
					{
						blockedByMaxLength = true;
						return false;
					}
					bool flag2 = TryMoveCaretOutsideCodeBlock(semanticBlock);
					appendedParagraph = flag2 && flag;
					return flag2;
				}
			}
			else
			{
				DocumentLine lineByNumber2 = EditBox.Document.GetLineByNumber(semanticBlock.EndLine + 1);
				if (lineByNumber2 != null && caretOffset == lineByNumber2.EndOffset)
				{
					return TryMoveCaretOutsideCodeBlock(semanticBlock);
				}
			}
		}
		return false;
	}

	internal static bool TryEnsureVisualLines(TextView textView)
	{
		if (textView == null)
		{
			return false;
		}
		try
		{
			textView.EnsureVisualLines();
			return textView.VisualLinesValid;
		}
		catch (InvalidOperationException)
		{
			return false;
		}
		catch (VisualLinesInvalidException)
		{
			return false;
		}
	}

	private bool TrySelectCodeBlockAtCaretForDelete(bool backspace)
	{
		if (EditBox?.Document == null || EditBox.SelectionLength > 0)
		{
			return false;
		}
		int caretOffset = Math.Max(0, Math.Min(EditBox.CaretOffset, EditBox.Document.TextLength));
		foreach (MarkdownBlockNode semanticBlock in GetSemanticBlocks())
		{
			if (semanticBlock != null && semanticBlock.Kind == MarkdownBlockKind.CodeBlock && TryGetCodeBlockOuterRange(semanticBlock, out var blockStartOffset, out var blockEndOffset) && blockEndOffset > blockStartOffset && IsCaretAtCodeBlockObjectDeletionBoundary(semanticBlock, caretOffset, backspace, blockStartOffset, blockEndOffset))
			{
				return TrySelectCodeBlockObject(semanticBlock);
			}
		}
		return false;
	}

	private bool IsCaretAtCodeBlockObjectDeletionBoundary(MarkdownBlockNode block, int caretOffset, bool backspace, int selectionStart, int selectionEnd)
	{
		if (block == null || block.Kind != MarkdownBlockKind.CodeBlock)
		{
			return false;
		}
		int num = (backspace ? selectionEnd : selectionStart);
		if (caretOffset == num)
		{
			return true;
		}
		if (!backspace || !IsCodeBlockAttachment(block) || EditBox?.Document == null)
		{
			return false;
		}
		return caretOffset == EditBox.Document.GetLineByNumber(block.StartLine + 1)?.EndOffset;
	}

	internal bool TrySelectCodeBlockAttachment(MarkdownBlockNode block)
	{
		if (IsCodeBlockAttachment(block))
		{
			return TrySelectCodeBlockObject(block);
		}
		return false;
	}

	private bool TrySelectCodeBlockObject(MarkdownBlockNode block)
	{
		if (block == null || block.Kind != MarkdownBlockKind.CodeBlock || !TryGetCodeBlockOuterRange(block, out var blockStartOffset, out var blockEndOffset) || blockEndOffset <= blockStartOffset)
		{
			return false;
		}
		EditBox.Focus();
		ClearObjectSelectedCodeBlocks();
		GetObjectSelectedCodeBlockKeys().Add(GetCodeBlockSelectionKey(block));
		_suppressParentCaretForCodeBlockObjectSelection = true;
		bool suppressSemanticSelectionNormalization = _suppressSemanticSelectionNormalization;
		_suppressSemanticSelectionNormalization = true;
		try
		{
			ApplyNormalizedSemanticSelection(blockStartOffset, blockEndOffset, blockEndOffset);
		}
		finally
		{
			_suppressSemanticSelectionNormalization = suppressSemanticSelectionNormalization;
		}
		RecordLastInsertRangeFromEditor();
		RefreshOpenMenuContexts();
		_codeBlockSelectionLayer?.InvalidateVisual();
		RefreshParentCaretVisibilityForObjectSelection();
		return true;
	}

	private bool TryGetCodeBlockRectInTextView(MarkdownBlockNode block, out Rect codeBlockRect)
	{
		codeBlockRect = Rect.Empty;
		if (block == null || block.Kind != MarkdownBlockKind.CodeBlock || IsSemanticBlockHiddenByHeadingFold(block) || EditBox?.Document == null || EditBox.TextArea?.TextView == null)
		{
			return false;
		}
		DocumentLine documentLine = TryGetDocumentLineBySemanticIndex(block.StartLine);
		DocumentLine documentLine2 = TryGetDocumentLineBySemanticIndex(block.EndLine);
		if (documentLine == null || documentLine2 == null)
		{
			return false;
		}
		TextView textView = EditBox.TextArea.TextView;
		if (textView.GetVisualLine(documentLine.LineNumber) == null || textView.GetVisualLine(documentLine2.LineNumber) == null)
		{
			return false;
		}
		try
		{
			double num = textView.GetVisualPosition(new TextViewPosition(documentLine.LineNumber, 1), VisualYPosition.LineTop).Y - textView.ScrollOffset.Y;
			double num2 = textView.GetVisualPosition(new TextViewPosition(documentLine2.LineNumber, 1), VisualYPosition.LineBottom).Y - textView.ScrollOffset.Y;
			if (num2 <= num)
			{
				return false;
			}
			codeBlockRect = new Rect(0.0, num, Math.Max(0.0, textView.ActualWidth - 7.0), num2 - num);
			return true;
		}
		catch (InvalidOperationException)
		{
			return false;
		}
		catch (ArgumentException)
		{
			return false;
		}
		catch (VisualLinesInvalidException)
		{
			return false;
		}
	}

	private bool TryMoveCaretOutsideCodeBlock(MarkdownBlockNode block)
	{
		TextDocument textDocument = EditBox?.Document;
		if (block == null || block.Kind != MarkdownBlockKind.CodeBlock || textDocument == null || block.EndLine < 0 || block.EndLine >= textDocument.LineCount)
		{
			return false;
		}
		if (IsCodeBlockAttachment(block))
		{
			DocumentLine lineByNumber = textDocument.GetLineByNumber(block.StartLine + 1);
			if (lineByNumber == null)
			{
				return false;
			}
			if (lineByNumber.NextLine != null)
			{
				EditBox.CaretOffset = lineByNumber.NextLine.Offset;
				return true;
			}
			if (ReadOnly)
			{
				return TrySetCaretAfterCodeBlockAttachment(block);
			}
			return TryAppendCodeBlockExitLineAtDocumentEnd(lineByNumber.EndOffset);
		}
		DocumentLine lineByNumber2 = textDocument.GetLineByNumber(block.EndLine + 1);
		if (lineByNumber2 == null || !IsSemanticCodeBlockClosingFenceLine(textDocument.GetText(lineByNumber2), block, block.EndLine))
		{
			return false;
		}
		if (block.EndLine < textDocument.LineCount - 1)
		{
			DocumentLine lineByNumber3 = textDocument.GetLineByNumber(block.EndLine + 2);
			if (lineByNumber3 == null)
			{
				return false;
			}
			EditBox.CaretOffset = lineByNumber3.Offset;
			return true;
		}
		if (ReadOnly)
		{
			EditBox.CaretOffset = lineByNumber2.EndOffset;
			return true;
		}
		return TryAppendCodeBlockExitLineAtDocumentEnd(EditBox.CaretOffset);
	}

	private static bool IsPointInCodeBlockOutsideRightArea(Point point, Rect codeBlockRect)
	{
		if (!codeBlockRect.IsEmpty && point.X >= Math.Max(0.0, codeBlockRect.Right - 48.0) && point.Y >= codeBlockRect.Top)
		{
			return point.Y <= codeBlockRect.Bottom;
		}
		return false;
	}

	private void OnSelectionChanged(object sender, EventArgs e)
	{
		TextEditor editBox = EditBox;
		if (editBox != null && editBox.SelectionLength > 0)
		{
			ClearBackspaceRetainedTypingMarks();
		}
		if (!_suppressSemanticSelectionNormalization && !_semanticSelectionMouseUpRestoreScheduled)
		{
			if (TryRestoreSemanticDragSelectionDuringMouseDrag())
			{
				RefreshOpenMenuContexts();
				return;
			}
			NormalizeSelectionInsideCodeBlockContentIfNeeded();
		}
		if (_currentFocused)
		{
			RecordLastInsertRangeFromEditor();
		}
		if (EditBox.SelectedText.Length < 1)
		{
			TryDismissEditorMenu();
		}
		RefreshOpenMenuContexts();
		_codeBlockSelectionLayer?.InvalidateVisual();
		if (Mouse.LeftButton != MouseButtonState.Pressed && !_suppressSemanticSelectionNormalization && !_semanticSelectionDragHandled)
		{
			_semanticSelectionAnchorOffset = null;
			_semanticSelectionDragHandled = false;
			_semanticSelectionMouseUpRestoreScheduled = false;
			_semanticSelectionLastActiveOffset = null;
			_semanticSelectionLastStartOffset = null;
			_semanticSelectionLastEndOffset = null;
		}
	}

	private void CaptureInternalSelectionDragState(MouseButtonEventArgs e)
	{
		InternalSelectionDragState internalSelectionDragState = _internalSelectionDragState;
		bool flag = internalSelectionDragState != null && !internalSelectionDragState.DragStarted && e != null && e.LeftButton == MouseButtonState.Pressed;
		if (e == null || !ShouldCaptureInternalSelectionDrag(e.ClickCount) || EditBox?.Document == null || EditBox.TextArea?.TextView == null || EditBox.SelectionLength <= 0)
		{
			if (!flag)
			{
				_internalSelectionDragState = null;
			}
			return;
		}
		int num = Math.Max(0, Math.Min(EditBox.SelectionStart, EditBox.Document.TextLength));
		int num2 = Math.Max(num, Math.Min(num + EditBox.SelectionLength, EditBox.Document.TextLength));
		if (num2 <= num)
		{
			if (!flag)
			{
				_internalSelectionDragState = null;
			}
			return;
		}
		Point position = e.GetPosition(EditBox.TextArea.TextView);
		int? num3 = TryGetNativeSelectionHitOffsetFromTextViewPoint(position) ?? TryGetDocumentOffsetFromTextViewPoint(position) ?? TryGetDocumentBoundaryOffsetFromTextViewPoint(position);
		if (!num3.HasValue || num3.Value < num || num3.Value > num2)
		{
			if (!flag)
			{
				_internalSelectionDragState = null;
			}
			return;
		}
		int selectionStart = num;
		int selectionEnd = num2;
		ExpandSelectionToCodeBlockBoundariesIfNeeded(ref selectionStart, ref selectionEnd);
		ExpandSelectionToLinkBoundariesIfNeeded(ref selectionStart, ref selectionEnd);
		if (selectionEnd <= selectionStart)
		{
			if (!flag)
			{
				_internalSelectionDragState = null;
			}
			return;
		}
		string persistedText = GetPersistedText();
		if (string.IsNullOrEmpty(persistedText) || !TryGetPersistedSelectionRange(selectionStart, selectionEnd, persistedText, out var persistedStart, out var persistedEnd) || persistedEnd <= persistedStart)
		{
			if (!flag)
			{
				_internalSelectionDragState = null;
			}
			return;
		}
		_internalSelectionDragState = new InternalSelectionDragState
		{
			SelectionStart = selectionStart,
			SelectionLength = selectionEnd - selectionStart,
			DisplayText = EditBox.Document.GetText(selectionStart, selectionEnd - selectionStart),
			PersistedText = persistedText,
			PersistedSelectionStart = persistedStart,
			PersistedSelectionEnd = persistedEnd
		};
	}

	private static bool ShouldCaptureInternalSelectionDrag(int clickCount)
	{
		return clickCount == 1;
	}

	private bool TryCaptureCurrentSelectionForInternalDrag()
	{
		if (EditBox?.Document == null || EditBox.SelectionLength <= 0)
		{
			return false;
		}
		int selectionStart = Math.Max(0, Math.Min(EditBox.SelectionStart, EditBox.Document.TextLength));
		int selectionEnd = Math.Max(selectionStart, Math.Min(selectionStart + EditBox.SelectionLength, EditBox.Document.TextLength));
		ExpandSelectionToCodeBlockBoundariesIfNeeded(ref selectionStart, ref selectionEnd);
		ExpandSelectionToLinkBoundariesIfNeeded(ref selectionStart, ref selectionEnd);
		if (selectionEnd <= selectionStart)
		{
			return false;
		}
		string persistedText = GetPersistedText();
		if (string.IsNullOrEmpty(persistedText) || !TryGetPersistedSelectionRange(selectionStart, selectionEnd, persistedText, out var persistedStart, out var persistedEnd) || persistedEnd <= persistedStart)
		{
			return false;
		}
		_internalSelectionDragState = new InternalSelectionDragState
		{
			SelectionStart = selectionStart,
			SelectionLength = selectionEnd - selectionStart,
			DisplayText = EditBox.Document.GetText(selectionStart, selectionEnd - selectionStart),
			PersistedText = persistedText,
			PersistedSelectionStart = persistedStart,
			PersistedSelectionEnd = persistedEnd
		};
		return true;
	}

	private int? TryGetNativeSelectionHitOffsetFromTextViewPoint(Point textViewPoint)
	{
		if (!TryGetNativeTextViewOffsetFromPoint(textViewPoint, out var offset, out var _))
		{
			return null;
		}
		return offset;
	}

	private bool TryGetNativeTextViewOffsetFromPoint(Point textViewPoint, out int offset, out int visualColumn)
	{
		offset = 0;
		visualColumn = 0;
		if (!TryGetNativeTextViewPositionFromPoint(textViewPoint, firstTextLineOnly: false, out var position) || EditBox?.Document == null)
		{
			return false;
		}
		visualColumn = position.VisualColumn;
		offset = Math.Max(0, Math.Min(EditBox.Document.GetOffset(position.Location), EditBox.Document.TextLength));
		return true;
	}

	private bool TryGetNativeTextViewPositionFromPoint(Point textViewPoint, bool firstTextLineOnly, out TextViewPosition position)
	{
		position = default(TextViewPosition);
		if (EditBox?.Document == null || EditBox.TextArea?.TextView == null)
		{
			return false;
		}
		try
		{
			TextView textView = EditBox.TextArea.TextView;
			Point visualPosition = textViewPoint;
			if (visualPosition.Y < 0.0)
			{
				visualPosition.Y = 0.0;
			}
			if (visualPosition.Y > textView.ActualHeight)
			{
				visualPosition.Y = textView.ActualHeight;
			}
			visualPosition += textView.ScrollOffset;
			if (visualPosition.Y >= textView.DocumentHeight)
			{
				visualPosition.Y = Math.Max(0.0, textView.DocumentHeight - 0.0001);
			}
			VisualLine visualLineFromVisualTop = textView.GetVisualLineFromVisualTop(visualPosition.Y);
			if (visualLineFromVisualTop == null)
			{
				return false;
			}
			bool allowVirtualSpace = EditBox.TextArea.Selection?.EnableVirtualSpace ?? false;
			if (firstTextLineOnly)
			{
				int visualColumn = visualLineFromVisualTop.GetVisualColumn(visualLineFromVisualTop.TextLines.First(), visualPosition.X, allowVirtualSpace);
				position = visualLineFromVisualTop.GetTextViewPosition(visualColumn);
				position.IsAtEndOfLine = true;
			}
			else
			{
				position = visualLineFromVisualTop.GetTextViewPosition(visualPosition, allowVirtualSpace);
			}
			return true;
		}
		catch (InvalidOperationException)
		{
			return false;
		}
		catch (ArgumentException)
		{
			return false;
		}
	}

	private void OnEditorDataObjectCopying(object sender, DataObjectCopyingEventArgs e)
	{
		if ((!CodeBlockAttachmentControl.TryFindFromOriginalSource(e?.OriginalSource, out var control) || !control.IsSourceInsideCodeEditor(e.OriginalSource)) && e != null && e.IsDragDrop && EditBox?.TextArea?.TextView != null)
		{
			if (_internalSelectionDragState == null)
			{
				TryCaptureCurrentSelectionForInternalDrag();
			}
			if (_internalSelectionDragState != null)
			{
				_internalSelectionDragState.DragStarted = true;
				e.DataObject.SetData("TickTick.MarkDownEditor.InternalSelectionDrag", _internalSelectionDragOwnerId);
				e.DataObject.SetData(System.Windows.DataFormats.UnicodeText, _internalSelectionDragState.DisplayText ?? string.Empty);
				e.DataObject.SetData(System.Windows.DataFormats.Text, _internalSelectionDragState.DisplayText ?? string.Empty);
				e.DataObject.SetData(typeof(string), _internalSelectionDragState.DisplayText ?? string.Empty);
			}
		}
	}

	private void OnEditorPreviewDrop(object sender, System.Windows.DragEventArgs e)
	{
		if (CodeBlockAttachmentControl.TryFindFromOriginalSource(e?.OriginalSource, out var control) && control.IsSourceInsideCodeEditor(e.OriginalSource))
		{
			return;
		}
		try
		{
			if (!IsInternalSelectionDragData(e))
			{
				TryHandleExternalTextDrop(e);
				return;
			}
			if (!TryHandleInternalSelectionDrop(e))
			{
				e.Effects = System.Windows.DragDropEffects.None;
			}
			e.Handled = true;
		}
		finally
		{
			_internalSelectionDragState = null;
		}
	}

	private void OnEditorPreviewDragOver(object sender, System.Windows.DragEventArgs e)
	{
		if ((!CodeBlockAttachmentControl.TryFindFromOriginalSource(e?.OriginalSource, out var control) || !control.IsSourceInsideCodeEditor(e.OriginalSource)) && IsInternalSelectionDragData(e))
		{
			e.Effects = (TryUpdateInternalSelectionDropCaret(e, out var _) ? GetInternalSelectionDropEffect(e) : System.Windows.DragDropEffects.None);
			e.Handled = true;
		}
	}

	private bool TryUpdateInternalSelectionDropCaret(System.Windows.DragEventArgs e, out int dropOffset)
	{
		dropOffset = 0;
		if (e == null || EditBox?.Document == null || EditBox.TextArea?.TextView == null)
		{
			return false;
		}
		Point position = e.GetPosition(EditBox.TextArea.TextView);
		if (!TryGetNativeTextViewOffsetFromPoint(position, out dropOffset, out var visualColumn))
		{
			int? num = TryGetDocumentBoundaryOffsetFromTextViewPoint(position);
			if (!num.HasValue)
			{
				return false;
			}
			dropOffset = num.Value;
			visualColumn = -1;
		}
		dropOffset = Math.Max(0, Math.Min(dropOffset, EditBox.Document.TextLength));
		TextLocation location = EditBox.Document.GetLocation(dropOffset);
		RunWithoutCaretVerticalOffsetNotification(delegate
		{
			EditBox.TextArea.Caret.Position = ((visualColumn >= 0) ? new TextViewPosition(location, visualColumn) : new TextViewPosition(location));
			EditBox.TextArea.Caret.DesiredXPos = double.NaN;
			EditBox.TextArea.Caret.Show();
		});
		return true;
	}

	private bool TryHandleExternalTextDrop(System.Windows.DragEventArgs e)
	{
		if (ReadOnly || e?.Data == null || EditBox?.Document == null || EditBox.TextArea?.TextView == null)
		{
			return false;
		}
		ClipboardRichTextContent clipboardRichTextContent;
		try
		{
			clipboardRichTextContent = ClipboardRichTextHelper.Extract(e.Data);
		}
		catch (Exception)
		{
			return false;
		}
		string text = clipboardRichTextContent?.PreferredText;
		if (string.IsNullOrEmpty(text))
		{
			try
			{
				if (!TryGetDropText(e, out text) || string.IsNullOrEmpty(text))
				{
					return false;
				}
			}
			catch (Exception)
			{
				return false;
			}
			clipboardRichTextContent = new ClipboardRichTextContent
			{
				PlainText = text,
				PreferredText = text
			};
		}
		if (!TryUpdateInternalSelectionDropCaret(e, out var dropOffset))
		{
			return false;
		}
		EditBox.Select(dropOffset, 0);
		EditBox.CaretOffset = dropOffset;
		e.Effects = System.Windows.DragDropEffects.Copy;
		e.Handled = true;
		if (ShouldPastePlainTextInsideCodeBlock(dropOffset, 0))
		{
			HandlePastePlainTextAsync(GetPlainTextForCodeBlockPaste(clipboardRichTextContent));
		}
		else if (!clipboardRichTextContent.ShouldParseEmphasis)
		{
			HandlePasteTextAsync(text);
		}
		else
		{
			HandleStructuredPasteTextAsync(text);
		}
		return true;
	}

	private bool TryHandleInternalSelectionDrop(System.Windows.DragEventArgs e)
	{
		if (e != null && !ReadOnly && EditBox?.Document != null && EditBox.TextArea?.TextView != null)
		{
			InternalSelectionDragState internalSelectionDragState = _internalSelectionDragState;
			if (internalSelectionDragState != null && internalSelectionDragState.DragStarted && IsInternalSelectionDragData(e))
			{
				if (!TryGetDropText(e, out var text) || !IsSameDragDisplayText(text, _internalSelectionDragState.DisplayText))
				{
					return false;
				}
				if (!TryUpdateInternalSelectionDropCaret(e, out var dropOffset))
				{
					return false;
				}
				bool copy = GetInternalSelectionDropEffect(e) == System.Windows.DragDropEffects.Copy;
				bool num = TryMoveSelectedSemanticRangeForDrag(dropOffset, copy, _internalSelectionDragState);
				if (num)
				{
					e.Effects = System.Windows.DragDropEffects.Copy;
				}
				return num;
			}
		}
		return false;
	}

	private bool IsInternalSelectionDragData(System.Windows.DragEventArgs e)
	{
		if (e == null || e.Data?.GetDataPresent("TickTick.MarkDownEditor.InternalSelectionDrag", autoConvert: false) != true)
		{
			return false;
		}
		return string.Equals(e.Data.GetData("TickTick.MarkDownEditor.InternalSelectionDrag", autoConvert: false) as string, _internalSelectionDragOwnerId, StringComparison.Ordinal);
	}

	private static System.Windows.DragDropEffects GetInternalSelectionDropEffect(System.Windows.DragEventArgs e)
	{
		if (e == null || (e.KeyStates & DragDropKeyStates.ControlKey) != DragDropKeyStates.ControlKey)
		{
			return System.Windows.DragDropEffects.Move;
		}
		return System.Windows.DragDropEffects.Copy;
	}

	private static bool TryGetDropText(System.Windows.DragEventArgs e, out string text)
	{
		text = null;
		if (e?.Data == null)
		{
			return false;
		}
		if (e.Data.GetDataPresent(System.Windows.DataFormats.UnicodeText, autoConvert: true))
		{
			text = e.Data.GetData(System.Windows.DataFormats.UnicodeText, autoConvert: true) as string;
		}
		else if (e.Data.GetDataPresent(System.Windows.DataFormats.Text, autoConvert: true))
		{
			text = e.Data.GetData(System.Windows.DataFormats.Text, autoConvert: true) as string;
		}
		else if (e.Data.GetDataPresent(typeof(string)))
		{
			text = e.Data.GetData(typeof(string)) as string;
		}
		return text != null;
	}

	private static bool IsSameDragDisplayText(string left, string right)
	{
		return string.Equals(NormalizeDragDropText(left), NormalizeDragDropText(right), StringComparison.Ordinal);
	}

	private static string NormalizeDragDropText(string text)
	{
		return (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
	}

	private bool TryMoveSelectedSemanticRangeForDrag(int dropOffset, bool copy, InternalSelectionDragState dragState)
	{
		if (dragState == null || EditBox?.Document == null || string.IsNullOrEmpty(dragState.PersistedText))
		{
			return false;
		}
		EditorScrollSnapshot editorScrollSnapshot = CaptureEditorScrollSnapshot();
		int textLength = EditBox.Document.TextLength;
		int num = Math.Max(0, Math.Min(dragState.SelectionStart, textLength));
		int num2 = Math.Max(num, Math.Min(dragState.SelectionStart + dragState.SelectionLength, textLength));
		dropOffset = Math.Max(0, Math.Min(dropOffset, textLength));
		if (num2 <= num)
		{
			return false;
		}
		if (!copy && dropOffset >= num && dropOffset <= num2)
		{
			_internalSelectionDropCaretRestorePending = true;
			EditBox.SelectionLength = 0;
			SetInternalSelectionDropCaretOffset(dropOffset);
			RestoreEditorScrollSnapshot(editorScrollSnapshot);
			ScheduleInternalSelectionDropCaretRestore(dropOffset, editorScrollSnapshot);
			return true;
		}
		string persistedText = dragState.PersistedText;
		if (!string.Equals(GetPersistedText(), persistedText, StringComparison.Ordinal))
		{
			return false;
		}
		int num3 = Math.Max(0, Math.Min(dragState.PersistedSelectionStart, persistedText.Length));
		int num4 = Math.Max(num3, Math.Min(dragState.PersistedSelectionEnd, persistedText.Length));
		if (num4 <= num3)
		{
			return false;
		}
		int num5 = Math.Max(0, Math.Min(GetPersistedOffset(dropOffset), persistedText.Length));
		if (!copy && num5 >= num3 && num5 <= num4)
		{
			_internalSelectionDropCaretRestorePending = true;
			EditBox.SelectionLength = 0;
			SetInternalSelectionDropCaretOffset(dropOffset);
			RestoreEditorScrollSnapshot(editorScrollSnapshot);
			ScheduleInternalSelectionDropCaretRestore(dropOffset, editorScrollSnapshot);
			return true;
		}
		string text = persistedText.Substring(num3, num4 - num3);
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		string text2 = dragState.DisplayText ?? string.Empty;
		int valueOrDefault = (EditBox?.Document?.TextLength).GetValueOrDefault();
		int num6 = Math.Max(0, Math.Min(dragState.SelectionStart, valueOrDefault));
		int num7 = Math.Max(num6, Math.Min(num6 + Math.Max(0, dragState.SelectionLength), valueOrDefault));
		int num8 = num7 - num6;
		List<MarkdownInlineDisplaySegment> selectedInlineSemanticMarks = GetSelectedInlineSemanticMarks(num6, num7, valueOrDefault);
		PreparedEditorTextState preparedEditorTextState = PrepareEditorTextState(text);
		if (selectedInlineSemanticMarks.Count > 0 || !AreDisplayTextsEqual(preparedEditorTextState.DisplayText, text2) || !AreSemanticMarksEquivalent(preparedEditorTextState.SemanticMarks, selectedInlineSemanticMarks))
		{
			text = BuildSelectedSemanticMarkdown(text2, selectedInlineSemanticMarks, num6, num7);
		}
		string updatedPersistedText = persistedText;
		int val = num5;
		int num9 = dropOffset;
		if (!copy)
		{
			updatedPersistedText = updatedPersistedText.Remove(num3, num4 - num3);
			updatedPersistedText = AdjustPersistedInlineSemanticMetadataForDisplayReplacement(updatedPersistedText, num6, num8, 0);
			if (num5 > num4)
			{
				val = num5 - (num4 - num3);
			}
			if (dropOffset > num7)
			{
				num9 = Math.Max(0, dropOffset - num8);
			}
		}
		val = Math.Max(0, Math.Min(val, updatedPersistedText.Length));
		updatedPersistedText = updatedPersistedText.Insert(val, text);
		updatedPersistedText = AdjustPersistedInlineSemanticMetadataForDisplayReplacement(updatedPersistedText, num9, 0, text2.Length);
		int val2 = val + text.Length;
		PreparedEditorTextState prepared = PrepareEditorTextState(updatedPersistedText, repairMisplacedBlockMetadata: true);
		ReconcileInsertedSemanticMarkOffsets(prepared.SemanticMarks, selectedInlineSemanticMarks, num9, text2.Length);
		int explicitCaretOffset = ((prepared.RawToDisplayOffsetMap != null && prepared.RawToDisplayOffsetMap.Length != 0) ? prepared.RawToDisplayOffsetMap[Math.Max(0, Math.Min(val2, prepared.RawToDisplayOffsetMap.Length - 1))] : Math.Max(0, Math.Min(dropOffset, prepared.DisplayText?.Length ?? 0)));
		_internalSelectionDropCaretRestorePending = true;
		RunWithoutCaretVerticalOffsetNotification(delegate
		{
			ApplyPreparedEditorState(prepared, updatedPersistedText, restoreOffset: false, resetHistory: false, explicitCaretOffset);
		});
		RestoreEditorScrollSnapshot(editorScrollSnapshot);
		EditBox.SelectionLength = 0;
		SetInternalSelectionDropCaretOffset(explicitCaretOffset);
		RestoreEditorScrollSnapshot(editorScrollSnapshot);
		ScheduleInternalSelectionDropCaretRestore(explicitCaretOffset, editorScrollSnapshot);
		CaptureSemanticHistorySnapshot();
		RefreshOpenMenuContexts();
		RequestSafeRedraw();
		NotifySemanticTextChangedIfNeeded(persistedText);
		return true;
	}

	private static void ReconcileInsertedSemanticMarkOffsets(IList<MarkdownInlineDisplaySegment> parsedMarks, IEnumerable<MarkdownInlineDisplaySegment> insertedMarks, int insertDisplayOffset, int insertedDisplayLength)
	{
		if (parsedMarks == null || insertedMarks == null || insertedDisplayLength <= 0)
		{
			return;
		}
		int insertStart = Math.Max(0, insertDisplayOffset);
		int insertEnd = insertStart + insertedDisplayLength;
		foreach (MarkdownInlineDisplaySegment insertedMark in insertedMarks.Where((MarkdownInlineDisplaySegment mark) => mark != null && mark.SourceLength > 0))
		{
			int expectedPosition = insertStart + Math.Max(0, insertedMark.SourcePosition);
			MarkdownInlineDisplaySegment markdownInlineDisplaySegment = (from mark in parsedMarks
				where mark != null && mark.SourceLength == insertedMark.SourceLength && mark.SourcePosition >= insertStart && mark.SourcePosition < insertEnd && MarkdownInlineSemanticMetadata.ArePersistenceMarksEquivalent(mark, insertedMark)
				orderby Math.Abs(mark.SourcePosition - expectedPosition)
				select mark).FirstOrDefault();
			if (markdownInlineDisplaySegment != null)
			{
				markdownInlineDisplaySegment.SourcePosition = expectedPosition;
			}
		}
	}

	private EditorScrollSnapshot CaptureEditorScrollSnapshot()
	{
		ScrollViewer scrollViewer = Utils.FindParent<ScrollViewer>(this);
		return new EditorScrollSnapshot
		{
			EditorScrollOffset = (EditBox?.TextArea?.TextView?.ScrollOffset).GetValueOrDefault(),
			ParentScrollViewer = scrollViewer,
			ParentHorizontalOffset = (scrollViewer?.HorizontalOffset ?? 0.0),
			ParentVerticalOffset = (scrollViewer?.VerticalOffset ?? 0.0)
		};
	}

	private void RestoreEditorScrollSnapshot(EditorScrollSnapshot snapshot)
	{
		if (snapshot != null)
		{
			if (EditBox != null)
			{
				EditBox.ScrollToHorizontalOffset(Math.Max(0.0, snapshot.EditorScrollOffset.X));
				EditBox.ScrollToVerticalOffset(Math.Max(0.0, snapshot.EditorScrollOffset.Y));
			}
			if (snapshot.ParentScrollViewer != null)
			{
				snapshot.ParentScrollViewer.ScrollToHorizontalOffset(Math.Max(0.0, snapshot.ParentHorizontalOffset));
				snapshot.ParentScrollViewer.ScrollToVerticalOffset(Math.Max(0.0, snapshot.ParentVerticalOffset));
			}
		}
	}

	private void SetInternalSelectionDropCaretOffset(int caretOffset)
	{
		RunWithoutCaretVerticalOffsetNotification(delegate
		{
			if (EditBox?.Document != null)
			{
				EditBox.CaretOffset = Math.Max(0, Math.Min(caretOffset, EditBox.Document.TextLength));
			}
		});
	}

	private void ScheduleInternalSelectionDropCaretRestore(int caretOffset, EditorScrollSnapshot scrollSnapshotBeforeDrop)
	{
		base.Dispatcher?.BeginInvoke((Action)delegate
		{
			if (EditBox?.Document == null || EditBox.TextArea == null)
			{
				_internalSelectionDropCaretRestorePending = false;
			}
			else
			{
				int safeCaretOffset = Math.Max(0, Math.Min(caretOffset, EditBox.Document.TextLength));
				EditBox.TextArea.MouseSelectionMode = MouseSelectionMode.None;
				EditBox.TextArea.ClearSelection();
				RunWithoutCaretVerticalOffsetNotification(delegate
				{
					EditBox.CaretOffset = safeCaretOffset;
					EditBox.TextArea.Caret.Show();
				});
				RestoreEditorScrollSnapshot(scrollSnapshotBeforeDrop);
				RecordLastInsertRangeFromEditor();
				RefreshOpenMenuContexts();
				base.Dispatcher?.BeginInvoke((Action)delegate
				{
					RestoreEditorScrollSnapshot(scrollSnapshotBeforeDrop);
					_internalSelectionDropCaretRestorePending = false;
				}, DispatcherPriority.ContextIdle);
			}
		}, DispatcherPriority.ApplicationIdle);
	}

	private void RunWithoutCaretVerticalOffsetNotification(Action action)
	{
		if (action == null)
		{
			return;
		}
		bool suppressCaretVerticalOffsetNotification = _suppressCaretVerticalOffsetNotification;
		_suppressCaretVerticalOffsetNotification = true;
		try
		{
			action();
		}
		finally
		{
			_suppressCaretVerticalOffsetNotification = suppressCaretVerticalOffsetNotification;
		}
	}

	private void RecordLastInsertRangeFromEditor()
	{
		if (EditBox?.Document != null)
		{
			int textLength = EditBox.Document.TextLength;
			int num = Math.Max(0, Math.Min(EditBox.SelectionStart, textLength));
			int num2 = Math.Max(0, Math.Min(EditBox.SelectionLength, textLength - num));
			int lastInsertActiveOffset = Math.Max(0, Math.Min(EditBox.CaretOffset, textLength));
			if (num2 == 0)
			{
				num = GetInsertionOffsetOutsideLinkDisplayBoundary(num);
				lastInsertActiveOffset = num;
			}
			_lastInsertOffset = num;
			_lastInsertSelectionLength = num2;
			_lastInsertActiveOffset = lastInsertActiveOffset;
		}
	}

	private bool TryRestoreSemanticDragSelectionDuringMouseDrag()
	{
		if (Mouse.LeftButton != MouseButtonState.Pressed || !_semanticSelectionDragHandled || !_semanticSelectionLastStartOffset.HasValue || !_semanticSelectionLastEndOffset.HasValue || EditBox?.Document == null)
		{
			return false;
		}
		int selectionStart = EditBox.SelectionStart;
		int num = selectionStart + EditBox.SelectionLength;
		int num2 = Math.Max(0, Math.Min(_semanticSelectionLastStartOffset.Value, EditBox.Document.TextLength));
		int num3 = Math.Max(num2, Math.Min(_semanticSelectionLastEndOffset.Value, EditBox.Document.TextLength));
		if (selectionStart == num2 && num == num3)
		{
			return false;
		}
		RestoreLastSemanticDragSelectionIfNeeded();
		if (EditBox.SelectionStart == num2)
		{
			return EditBox.SelectionStart + EditBox.SelectionLength == num3;
		}
		return false;
	}

	private void OnEditorMouseDown(object sender, MouseButtonEventArgs e)
	{
		HideLinkMenuPopup();
		if (e.LeftButton == MouseButtonState.Pressed)
		{
			_suppressNextEditorMouseUpAfterHeadingFoldToggle = false;
			_handledHeadingFoldMouseUpTimestamp = null;
			ResetStaleCodeBlockAttachmentPointerAction(e.Timestamp);
			ClearBackspaceRetainedTypingMarks();
			_pendingFinalHeadingFoldExpansionAfterMouseUp = false;
			if (CodeBlockAttachmentControl.TryFindFromOriginalSource(e.OriginalSource, out var control))
			{
				Point position = e.GetPosition(control);
				if (position.Y >= 24.0 && position.X >= Math.Max(0.0, control.ActualWidth - 48.0) && TryPlaceCaretAfterCodeBlockAttachment(control.Block))
				{
					MarkCodeBlockAttachmentPointerActionHandled(e.Timestamp);
					ClearSemanticSelectionDragState();
					e.Handled = true;
					TryDismissEditorMenu();
				}
				else if (!control.IsSourceInsideCodeEditor(e.OriginalSource) && TrySetCaretBeforeCodeBlockAttachment(control.Block))
				{
					MarkCodeBlockAttachmentPointerActionHandled(e.Timestamp);
					ClearSemanticSelectionDragState();
					e.Handled = true;
					TryDismissEditorMenu();
				}
				return;
			}
			Point position2 = e.GetPosition(EditBox.TextArea.TextView);
			if (TryPlaceCaretAfterCodeBlockAttachmentFromPoint(position2))
			{
				MarkCodeBlockAttachmentPointerActionHandled(e.Timestamp);
				ClearSemanticSelectionDragState();
				e.Handled = true;
				TryDismissEditorMenu();
				return;
			}
			_semanticSelectionAnchorOffset = TryGetDocumentOffsetFromTextViewPoint(position2) ?? TryGetNativeSelectionHitOffsetFromTextViewPoint(position2) ?? TryGetDocumentBoundaryOffsetFromTextViewPoint(position2);
			_semanticSelectionDragHandled = false;
			_semanticSelectionMouseUpRestoreScheduled = false;
			_semanticSelectionLastActiveOffset = null;
			_semanticSelectionLastStartOffset = null;
			_semanticSelectionLastEndOffset = null;
			ResetSelectionViewportClampGestureState();
			CaptureInternalSelectionDragState(e);
		}
		TryDismissEditorMenu();
	}

	private void OnEditorMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
	{
		if (_pendingFinalHeadingFoldExpansionAfterMouseUp)
		{
			StopSelectionEdgeAutoScroll();
			CancelNativeMouseSelectionForHeadingFoldClick();
			e.Handled = true;
			return;
		}
		UpdateSelectionEdgeAutoScroll(e);
		if (e.LeftButton != MouseButtonState.Pressed)
		{
			ClearLinkHoverIfPointOutsideEditableArea(e);
		}
		if (e.LeftButton != MouseButtonState.Pressed || !_semanticSelectionAnchorOffset.HasValue || EditBox?.Document == null || EditBox.TextArea?.TextView == null)
		{
			return;
		}
		CaptureWholeSelectionAnchorIfNeeded();
		Point textViewPoint = e.GetPosition(EditBox.TextArea.TextView);
		bool flag = false;
		if (TryGetViewportConstrainedSelectionPoint(e, out var textViewPoint2, out var wasClamped))
		{
			textViewPoint = textViewPoint2;
			flag = wasClamped;
		}
		if (flag && _selectionViewportClampMouseMoveTimestamp == e.Timestamp)
		{
			e.Handled = true;
			return;
		}
		int? num = TryGetDocumentOffsetFromTextViewPoint(textViewPoint) ?? TryGetNativeSelectionHitOffsetFromTextViewPoint(textViewPoint) ?? TryGetDocumentBoundaryOffsetFromTextViewPoint(textViewPoint);
		if (!num.HasValue)
		{
			if (flag)
			{
				_selectionViewportClampMouseMoveTimestamp = e.Timestamp;
				e.Handled = true;
			}
		}
		else if (TryApplySemanticCodeBlockDragSelection(_semanticSelectionAnchorOffset.Value, num.Value))
		{
			_semanticSelectionDragHandled = true;
			_semanticSelectionLastActiveOffset = num.Value;
			e.Handled = true;
		}
		else if (flag)
		{
			ApplyAvalonEditMouseSelectionAtTextViewPoint(textViewPoint);
			_selectionViewportClampMouseMoveTimestamp = e.Timestamp;
			e.Handled = true;
		}
	}

	private void UpdateSelectionEdgeAutoScroll(System.Windows.Input.MouseEventArgs e)
	{
		if (e == null || e.LeftButton != MouseButtonState.Pressed || !_semanticSelectionAnchorOffset.HasValue || EditBox?.Document == null || EditBox.TextArea == null || !IsMouseSelectionMode(EditBox.TextArea.MouseSelectionMode, _semanticSelectionDragHandled))
		{
			StopSelectionEdgeAutoScroll();
			return;
		}
		ScrollViewer scrollViewer = Utils.FindParent<ScrollViewer>(this);
		if (scrollViewer == null || scrollViewer.ActualHeight <= 0.0 || scrollViewer.ScrollableHeight <= 0.0)
		{
			StopSelectionEdgeAutoScroll();
			return;
		}
		double y = e.GetPosition(scrollViewer).Y;
		if (Math.Abs(GetSelectionEdgeAutoScrollDelta(scrollViewer, y)) < double.Epsilon)
		{
			StopSelectionEdgeAutoScroll();
			return;
		}
		_selectionEdgeAutoScrollViewer = scrollViewer;
		if (_selectionEdgeAutoScrollTimer == null)
		{
			_selectionEdgeAutoScrollTimer = new DispatcherTimer(DispatcherPriority.Input, base.Dispatcher)
			{
				Interval = TimeSpan.FromMilliseconds(16.0)
			};
			_selectionEdgeAutoScrollTimer.Tick += OnSelectionEdgeAutoScrollTick;
		}
		if (!_selectionEdgeAutoScrollTimer.IsEnabled)
		{
			_selectionEdgeAutoScrollTimer.Start();
		}
	}

	private void OnSelectionEdgeAutoScrollTick(object sender, EventArgs e)
	{
		ScrollViewer selectionEdgeAutoScrollViewer = _selectionEdgeAutoScrollViewer;
		if (Mouse.LeftButton != MouseButtonState.Pressed || !_semanticSelectionAnchorOffset.HasValue || EditBox?.Document == null || EditBox.TextArea == null || !IsMouseSelectionMode(EditBox.TextArea.MouseSelectionMode, _semanticSelectionDragHandled) || selectionEdgeAutoScrollViewer == null)
		{
			StopSelectionEdgeAutoScroll();
			return;
		}
		double y = Mouse.GetPosition(selectionEdgeAutoScrollViewer).Y;
		double selectionEdgeAutoScrollDelta = GetSelectionEdgeAutoScrollDelta(selectionEdgeAutoScrollViewer, y);
		if (Math.Abs(selectionEdgeAutoScrollDelta) < double.Epsilon)
		{
			StopSelectionEdgeAutoScroll();
			return;
		}
		double verticalOffset = selectionEdgeAutoScrollViewer.VerticalOffset;
		double num = Math.Max(0.0, Math.Min(selectionEdgeAutoScrollViewer.ScrollableHeight, verticalOffset + selectionEdgeAutoScrollDelta));
		selectionEdgeAutoScrollViewer.ScrollToVerticalOffset(num);
		if (Math.Abs(num - verticalOffset) < double.Epsilon)
		{
			StopSelectionEdgeAutoScroll();
		}
		else
		{
			ScheduleSelectionUpdateAfterEdgeScroll();
		}
	}

	private double GetSelectionEdgeAutoScrollDelta(ScrollViewer scrollViewer, double pointerY)
	{
		double selectionViewportHeight = GetSelectionViewportHeight(scrollViewer);
		double num = CalculateSelectionEdgeScrollDelta(pointerY, selectionViewportHeight, scrollViewer.VerticalOffset, scrollViewer.ScrollableHeight);
		if (Math.Abs(num) < double.Epsilon || !TryGetDocumentVerticalBoundsInViewport(scrollViewer, out var documentTop, out var documentBottom))
		{
			return 0.0;
		}
		return LimitSelectionEdgeScrollDeltaToDocument(num, documentTop, documentBottom, 0.0, selectionViewportHeight);
	}

	private double GetSelectionViewportHeight(ScrollViewer scrollViewer)
	{
		return CalculateSelectionViewportHeight(scrollViewer?.ActualHeight ?? 0.0, SelectionViewportBottomInset);
	}

	private bool TryGetDocumentVerticalBoundsInViewport(ScrollViewer scrollViewer, out double documentTop, out double documentBottom)
	{
		documentTop = 0.0;
		documentBottom = 0.0;
		TextView textView = EditBox?.TextArea?.TextView;
		if (scrollViewer == null || textView == null || double.IsNaN(textView.DocumentHeight) || double.IsInfinity(textView.DocumentHeight) || textView.DocumentHeight <= 0.0)
		{
			return false;
		}
		try
		{
			double y = textView.ScrollOffset.Y;
			documentTop = textView.TranslatePoint(new Point(0.0, 0.0 - y), scrollViewer).Y;
			documentBottom = textView.TranslatePoint(new Point(0.0, textView.DocumentHeight - y), scrollViewer).Y;
			return !double.IsNaN(documentTop) && !double.IsInfinity(documentTop) && !double.IsNaN(documentBottom) && !double.IsInfinity(documentBottom) && documentBottom >= documentTop;
		}
		catch (InvalidOperationException)
		{
			return false;
		}
		catch (ArgumentException)
		{
			return false;
		}
	}

	private void ScheduleSelectionUpdateAfterEdgeScroll()
	{
		if (!_selectionEdgeSelectionUpdatePending && base.Dispatcher != null)
		{
			_selectionEdgeSelectionUpdatePending = true;
			base.Dispatcher.BeginInvoke((Action)delegate
			{
				_selectionEdgeSelectionUpdatePending = false;
				UpdateSelectionFromCurrentPointerAfterEdgeScroll();
			}, DispatcherPriority.Input);
		}
	}

	private void UpdateSelectionFromCurrentPointerAfterEdgeScroll()
	{
		if (Mouse.LeftButton == MouseButtonState.Pressed && _semanticSelectionAnchorOffset.HasValue && EditBox?.Document != null && EditBox.TextArea?.TextView != null)
		{
			ScrollViewer scrollViewer = _selectionEdgeAutoScrollViewer ?? Utils.FindParent<ScrollViewer>(this);
			if (scrollViewer != null && TryGetViewportConstrainedSelectionPoint(scrollViewer, Mouse.GetPosition(scrollViewer), out var textViewPoint, out var _))
			{
				ApplyMouseSelectionAtTextViewPoint(textViewPoint);
			}
		}
	}

	private bool ApplyMouseSelectionAtTextViewPoint(Point textViewPoint)
	{
		if (!_semanticSelectionAnchorOffset.HasValue)
		{
			return false;
		}
		int? num = TryGetDocumentOffsetFromTextViewPoint(textViewPoint) ?? TryGetNativeSelectionHitOffsetFromTextViewPoint(textViewPoint) ?? TryGetDocumentBoundaryOffsetFromTextViewPoint(textViewPoint);
		if (!num.HasValue)
		{
			return false;
		}
		if (TryApplySemanticCodeBlockDragSelection(_semanticSelectionAnchorOffset.Value, num.Value))
		{
			_semanticSelectionDragHandled = true;
			_semanticSelectionLastActiveOffset = num.Value;
			return true;
		}
		return ApplyAvalonEditMouseSelectionAtTextViewPoint(textViewPoint);
	}

	private bool ApplyAvalonEditMouseSelectionAtTextViewPoint(Point textViewPoint)
	{
		if (EditBox?.Document == null || EditBox.TextArea == null)
		{
			return false;
		}
		TextArea textArea = EditBox.TextArea;
		MouseSelectionMode mouseSelectionMode = textArea.MouseSelectionMode;
		switch (mouseSelectionMode)
		{
		case MouseSelectionMode.Normal:
		case MouseSelectionMode.Rectangular:
		{
			TextViewPosition position = textArea.Caret.Position;
			if (!TryGetNativeTextViewPositionFromPoint(textViewPoint, mouseSelectionMode == MouseSelectionMode.Rectangular, out var position2))
			{
				return false;
			}
			textArea.Caret.Position = position2;
			textArea.Caret.DesiredXPos = double.NaN;
			if (mouseSelectionMode == MouseSelectionMode.Normal && textArea.Selection is RectangleSelection)
			{
				textArea.Selection = Selection.Create(textArea, EditBox.Document.GetOffset(position.Location), EditBox.Document.GetOffset(position2.Location));
			}
			else if (mouseSelectionMode == MouseSelectionMode.Rectangular && !(textArea.Selection is RectangleSelection))
			{
				textArea.Selection = new RectangleSelection(textArea, position, position2);
			}
			else
			{
				textArea.Selection = textArea.Selection.StartSelectionOrSetEndpoint(position, position2);
			}
			return true;
		}
		default:
			return false;
		case MouseSelectionMode.WholeWord:
		case MouseSelectionMode.WholeLine:
		{
			if (!CaptureWholeSelectionAnchorIfNeeded() || !TryGetWholeSelectionSegmentAtTextViewPoint(textViewPoint, mouseSelectionMode == MouseSelectionMode.WholeLine, out var segmentStart, out var segmentEnd))
			{
				return false;
			}
			int value = _wholeSelectionAnchorStartOffset.Value;
			int value2 = _wholeSelectionAnchorEndOffset.Value;
			textArea.Selection = Selection.Create(textArea, Math.Min(segmentStart, value), Math.Max(segmentEnd, value2));
			textArea.Caret.Offset = ((segmentStart < value) ? segmentStart : Math.Max(segmentEnd, value2));
			return true;
		}
		}
	}

	private bool CaptureWholeSelectionAnchorIfNeeded()
	{
		if (_wholeSelectionAnchorStartOffset.HasValue && _wholeSelectionAnchorEndOffset.HasValue)
		{
			return true;
		}
		if (EditBox?.Document == null || EditBox.TextArea?.Selection?.SurroundingSegment == null)
		{
			return false;
		}
		MouseSelectionMode mouseSelectionMode = EditBox.TextArea.MouseSelectionMode;
		if (mouseSelectionMode != MouseSelectionMode.WholeWord && mouseSelectionMode != MouseSelectionMode.WholeLine)
		{
			return false;
		}
		ISegment surroundingSegment = EditBox.TextArea.Selection.SurroundingSegment;
		_wholeSelectionAnchorStartOffset = Math.Max(0, Math.Min(surroundingSegment.Offset, EditBox.Document.TextLength));
		_wholeSelectionAnchorEndOffset = Math.Max(_wholeSelectionAnchorStartOffset.Value, Math.Min(surroundingSegment.EndOffset, EditBox.Document.TextLength));
		return true;
	}

	private bool TryGetWholeSelectionSegmentAtTextViewPoint(Point textViewPoint, bool wholeLine, out int segmentStart, out int segmentEnd)
	{
		segmentStart = 0;
		segmentEnd = 0;
		if (EditBox?.Document == null || EditBox.TextArea?.TextView == null)
		{
			return false;
		}
		try
		{
			TextView textView = EditBox.TextArea.TextView;
			Point point = textViewPoint;
			point.Y = Math.Max(0.0, Math.Min(point.Y, textView.ActualHeight));
			point += textView.ScrollOffset;
			VisualLine visualLineFromVisualTop = textView.GetVisualLineFromVisualTop(point.Y);
			if (visualLineFromVisualTop == null)
			{
				return false;
			}
			if (wholeLine)
			{
				segmentStart = visualLineFromVisualTop.StartOffset;
				segmentEnd = visualLineFromVisualTop.LastDocumentLine.EndOffset;
				return true;
			}
			bool allowVirtualSpace = EditBox.TextArea.Selection?.EnableVirtualSpace ?? false;
			int visualColumn = visualLineFromVisualTop.GetVisualColumn(point, allowVirtualSpace);
			int num = visualLineFromVisualTop.GetNextCaretPosition(visualColumn + 1, LogicalDirection.Backward, CaretPositioningMode.WordStartOrSymbol, allowVirtualSpace);
			if (num < 0)
			{
				num = 0;
			}
			int num2 = visualLineFromVisualTop.GetNextCaretPosition(num, LogicalDirection.Forward, CaretPositioningMode.WordBorderOrSymbol, allowVirtualSpace);
			if (num2 < 0)
			{
				num2 = visualLineFromVisualTop.VisualLength;
			}
			segmentStart = Math.Max(0, Math.Min(visualLineFromVisualTop.GetRelativeOffset(num) + visualLineFromVisualTop.FirstDocumentLine.Offset, EditBox.Document.TextLength));
			segmentEnd = Math.Max(segmentStart, Math.Min(visualLineFromVisualTop.GetRelativeOffset(num2) + visualLineFromVisualTop.FirstDocumentLine.Offset, EditBox.Document.TextLength));
			return true;
		}
		catch (InvalidOperationException)
		{
			return false;
		}
		catch (ArgumentException)
		{
			return false;
		}
	}

	private bool TryGetViewportConstrainedSelectionPoint(System.Windows.Input.MouseEventArgs e, out Point textViewPoint, out bool wasClamped)
	{
		textViewPoint = e.GetPosition(EditBox.TextArea.TextView);
		wasClamped = false;
		ScrollViewer scrollViewer = Utils.FindParent<ScrollViewer>(this);
		if (scrollViewer != null)
		{
			return TryGetViewportConstrainedSelectionPoint(scrollViewer, e.GetPosition(scrollViewer), out textViewPoint, out wasClamped);
		}
		return true;
	}

	private bool TryGetViewportConstrainedSelectionPoint(ScrollViewer scrollViewer, Point pointerRelativeToViewport, out Point textViewPoint, out bool wasClamped)
	{
		textViewPoint = default(Point);
		wasClamped = false;
		TextView textView = EditBox?.TextArea?.TextView;
		double selectionViewportHeight = GetSelectionViewportHeight(scrollViewer);
		if (scrollViewer == null || textView == null || selectionViewportHeight <= 0.0)
		{
			return false;
		}
		double num = ClampSelectionPointerYToViewport(pointerRelativeToViewport.Y, selectionViewportHeight);
		wasClamped = Math.Abs(num - pointerRelativeToViewport.Y) > double.Epsilon;
		Point point = new Point(pointerRelativeToViewport.X, num);
		try
		{
			textViewPoint = scrollViewer.TranslatePoint(point, textView);
			return true;
		}
		catch (InvalidOperationException)
		{
			return false;
		}
		catch (ArgumentException)
		{
			return false;
		}
	}

	private void ResetSelectionViewportClampGestureState()
	{
		_selectionViewportClampMouseMoveTimestamp = null;
		_selectionViewportClampMouseUpTimestamp = null;
		_wholeSelectionAnchorStartOffset = null;
		_wholeSelectionAnchorEndOffset = null;
	}

	private void StopSelectionEdgeAutoScroll()
	{
		_selectionEdgeAutoScrollTimer?.Stop();
		_selectionEdgeAutoScrollViewer = null;
	}

	internal static bool IsMouseSelectionMode(MouseSelectionMode mouseSelectionMode, bool semanticSelectionDragHandled)
	{
		if (!semanticSelectionDragHandled && mouseSelectionMode != MouseSelectionMode.Normal && mouseSelectionMode != MouseSelectionMode.WholeWord && mouseSelectionMode != MouseSelectionMode.WholeLine)
		{
			return mouseSelectionMode == MouseSelectionMode.Rectangular;
		}
		return true;
	}

	internal static double CalculateSelectionViewportHeight(double actualHeight, double bottomInset)
	{
		if (double.IsNaN(actualHeight) || double.IsInfinity(actualHeight) || actualHeight <= 0.0)
		{
			return 0.0;
		}
		if (double.IsNaN(bottomInset) || bottomInset <= 0.0)
		{
			return actualHeight;
		}
		if (double.IsInfinity(bottomInset))
		{
			return 0.0;
		}
		return Math.Max(0.0, actualHeight - bottomInset);
	}

	internal static double LimitSelectionEdgeScrollDeltaToDocument(double scrollDelta, double documentTop, double documentBottom, double viewportTop, double viewportBottom)
	{
		if (double.IsNaN(scrollDelta) || double.IsInfinity(scrollDelta) || double.IsNaN(documentTop) || double.IsInfinity(documentTop) || double.IsNaN(documentBottom) || double.IsInfinity(documentBottom) || double.IsNaN(viewportTop) || double.IsInfinity(viewportTop) || double.IsNaN(viewportBottom) || double.IsInfinity(viewportBottom) || documentBottom < documentTop || viewportBottom < viewportTop)
		{
			return 0.0;
		}
		if (scrollDelta < 0.0)
		{
			double val = Math.Max(0.0, viewportTop - documentTop);
			return 0.0 - Math.Min(0.0 - scrollDelta, val);
		}
		if (scrollDelta > 0.0)
		{
			double val2 = Math.Max(0.0, documentBottom - viewportBottom);
			return Math.Min(scrollDelta, val2);
		}
		return 0.0;
	}

	internal static double ClampSelectionPointerYToViewport(double pointerY, double viewportHeight)
	{
		if (double.IsNaN(pointerY) || double.IsInfinity(pointerY) || double.IsNaN(viewportHeight) || double.IsInfinity(viewportHeight) || viewportHeight <= 0.0)
		{
			return pointerY;
		}
		if (pointerY < 0.0)
		{
			return 0.0;
		}
		if (pointerY >= viewportHeight)
		{
			return Math.Max(0.0, viewportHeight - 0.0001);
		}
		return pointerY;
	}

	internal static double CalculateSelectionEdgeScrollDelta(double pointerY, double viewportHeight, double verticalOffset, double scrollableHeight)
	{
		if (double.IsNaN(pointerY) || double.IsInfinity(pointerY) || double.IsNaN(viewportHeight) || double.IsInfinity(viewportHeight) || viewportHeight <= 0.0 || scrollableHeight <= 0.0)
		{
			return 0.0;
		}
		double num = Math.Min(48.0, viewportHeight / 2.0);
		double num2;
		double val;
		if (pointerY < num && verticalOffset > 0.0)
		{
			num2 = -1.0;
			val = (num - pointerY) / num;
		}
		else
		{
			if (!(pointerY > viewportHeight - num) || !(verticalOffset < scrollableHeight))
			{
				return 0.0;
			}
			num2 = 1.0;
			val = (pointerY - (viewportHeight - num)) / num;
		}
		val = Math.Max(0.0, Math.Min(1.0, val));
		double val2 = 1.0 + 13.0 * val;
		double val3 = ((num2 < 0.0) ? Math.Max(0.0, verticalOffset) : Math.Max(0.0, scrollableHeight - verticalOffset));
		return num2 * Math.Min(val2, val3);
	}

	private void ClearLinkHoverIfPointOutsideEditableArea(System.Windows.Input.MouseEventArgs e)
	{
		TextView textView = EditBox?.TextArea?.TextView;
		if (e != null && textView != null && !IsPointInsideElement(e.GetPosition(textView), textView))
		{
			ClearAllHoveredLinks();
			HideLinkMenuPopup();
		}
	}

	private bool ClearAllHoveredLinks()
	{
		TextView textView = EditBox?.TextArea?.TextView;
		if (textView == null)
		{
			return false;
		}
		bool flag = false;
		foreach (CustomLinkElementGenerator item in textView.ElementGenerators.OfType<CustomLinkElementGenerator>())
		{
			flag |= item.ClearHoveredLink();
		}
		foreach (LinkNameGenerator item2 in textView.ElementGenerators.OfType<LinkNameGenerator>())
		{
			flag |= item2.ClearHoveredLink();
		}
		return flag;
	}

	private static bool IsPointInsideElement(Point point, FrameworkElement element)
	{
		if (element != null && point.X >= 0.0 && point.Y >= 0.0 && point.X <= element.ActualWidth)
		{
			return point.Y <= element.ActualHeight;
		}
		return false;
	}

	private int? TryGetDocumentBoundaryOffsetFromTextViewPoint(Point textViewPoint)
	{
		if (EditBox?.Document == null || EditBox.TextArea?.TextView == null)
		{
			return null;
		}
		if (textViewPoint.Y < 0.0)
		{
			return 0;
		}
		if (textViewPoint.Y > EditBox.TextArea.TextView.ActualHeight)
		{
			return EditBox.Document.TextLength;
		}
		return null;
	}

	public void TryDismissStyleMenu()
	{
		if (_styleMenuPopup != null && _styleMenuPopup.IsOpen)
		{
			_styleMenuPopup.IsOpen = false;
		}
	}

	public void TryDismissEditorMenu()
	{
		EditorMenu.DismissSecondLevelPopups();
		if (_customEditorPopup != null && _customEditorPopup.IsOpen)
		{
			_customEditorPopup.IsOpen = false;
		}
	}

	internal static bool ShouldScheduleParentCaretNotificationForMouseUp(bool completedCodeBlockPointerAction, bool originatedFromCodeBlockAttachment)
	{
		if (!completedCodeBlockPointerAction)
		{
			return !originatedFromCodeBlockAttachment;
		}
		return false;
	}

	internal static bool ShouldSuppressHeadingFoldMouseUp(bool foldToggleMouseUpPending, int? handledMouseUpTimestamp, int currentMouseUpTimestamp)
	{
		if (!foldToggleMouseUpPending)
		{
			return handledMouseUpTimestamp == currentMouseUpTimestamp;
		}
		return true;
	}

	private void OnEditorMouseUp(object sender, MouseButtonEventArgs e)
	{
		if (_selectionViewportClampMouseUpTimestamp == e.Timestamp)
		{
			e.Handled = true;
			return;
		}
		StopSelectionEdgeAutoScroll();
		if (ShouldSuppressHeadingFoldMouseUp(_suppressNextEditorMouseUpAfterHeadingFoldToggle, _handledHeadingFoldMouseUpTimestamp, e.Timestamp))
		{
			_suppressNextEditorMouseUpAfterHeadingFoldToggle = false;
			_handledHeadingFoldMouseUpTimestamp = e.Timestamp;
			CancelNativeMouseSelectionForHeadingFoldClick();
			ClearSemanticSelectionDragState();
			e.Handled = true;
			return;
		}
		bool completedCodeBlockPointerAction = TryCompleteCodeBlockAttachmentPointerAction(e);
		CodeBlockAttachmentControl control;
		bool flag = CodeBlockAttachmentControl.TryFindFromOriginalSource(e.OriginalSource, out control);
		if (!ShouldScheduleParentCaretNotificationForMouseUp(completedCodeBlockPointerAction, flag))
		{
			if (flag && !control.IsSourceInsideCodeEditor(e.OriginalSource))
			{
				e.Handled = true;
			}
			return;
		}
		if (ShouldSuppressCaretVerticalOffsetNotificationForInternalSelectionClick(_internalSelectionDragState != null, _internalSelectionDragState?.DragStarted ?? false))
		{
			_suppressCaretVerticalOffsetNotificationForCurrentMouseGesture = true;
		}
		ScheduleCaretVerticalOffsetNotificationAfterMouseUp();
		if (_pendingFinalHeadingFoldExpansionAfterMouseUp)
		{
			_pendingFinalHeadingFoldExpansionAfterMouseUp = false;
			CancelNativeMouseSelectionForHeadingFoldClick();
			TryExpandFinalHeadingFoldAtCaret(deferWhileMouseDown: false);
			ClearSemanticSelectionDragState();
			e.Handled = true;
		}
		else if (_semanticSelectionMouseUpRestoreScheduled)
		{
			e.Handled = true;
		}
		else
		{
			if (TryCompleteInternalSelectionClickWithoutDrag(e))
			{
				return;
			}
			Point textViewPoint = ((EditBox?.TextArea?.TextView == null) ? default(Point) : e.GetPosition(EditBox.TextArea.TextView));
			bool flag2 = false;
			Point textViewPoint2 = default(Point);
			bool wasClamped = default(bool);
			if (_semanticSelectionAnchorOffset.HasValue && EditBox?.TextArea != null && IsMouseSelectionMode(EditBox.TextArea.MouseSelectionMode, _semanticSelectionDragHandled) && TryGetViewportConstrainedSelectionPoint(e, out textViewPoint2, out wasClamped) && wasClamped)
			{
				textViewPoint = textViewPoint2;
				flag2 = true;
				CaptureWholeSelectionAnchorIfNeeded();
				ApplyMouseSelectionAtTextViewPoint(textViewPoint);
			}
			bool flag3 = _semanticSelectionDragHandled;
			int? semanticSelectionAnchorOffset = _semanticSelectionAnchorOffset;
			int? activeOffset = null;
			int? selectionStart = null;
			int? selectionEnd = null;
			if (_semanticSelectionAnchorOffset.HasValue && EditBox?.TextArea?.TextView != null)
			{
				int? num = TryGetDocumentOffsetFromTextViewPoint(textViewPoint) ?? TryGetNativeSelectionHitOffsetFromTextViewPoint(textViewPoint) ?? TryGetDocumentBoundaryOffsetFromTextViewPoint(textViewPoint) ?? _semanticSelectionLastActiveOffset;
				activeOffset = num;
				if (num.HasValue && TryGetSemanticCodeBlockDragSelectionRange(_semanticSelectionAnchorOffset.Value, num.Value, out var selectionStart2, out var selectionEnd2))
				{
					selectionStart = selectionStart2;
					selectionEnd = selectionEnd2;
					_semanticSelectionDragHandled = true;
					_semanticSelectionLastActiveOffset = num.Value;
					_semanticSelectionLastStartOffset = selectionStart2;
					_semanticSelectionLastEndOffset = selectionEnd2;
					flag3 = true;
				}
			}
			if (flag3 || (_semanticSelectionAnchorOffset.HasValue && _semanticSelectionLastStartOffset.HasValue && _semanticSelectionLastEndOffset.HasValue))
			{
				CancelNativeMouseSelectionAfterSemanticDrag(e);
				TryRestoreCapturedSemanticDragSelection(semanticSelectionAnchorOffset, activeOffset, selectionStart, selectionEnd);
				ScheduleSemanticDragSelectionRestoreAfterMouseUp(semanticSelectionAnchorOffset, activeOffset, selectionStart, selectionEnd);
			}
			else
			{
				ScheduleCodeBlockContentNormalization();
				if (TryNormalizeCaretOutsideLinkDisplayBoundary())
				{
					RecordLastInsertRangeFromEditor();
				}
			}
			TryShowEditorMenu();
			InternalSelectionDragState internalSelectionDragState = _internalSelectionDragState;
			if (internalSelectionDragState == null || !internalSelectionDragState.DragStarted)
			{
				_internalSelectionDragState = null;
			}
			if (flag2)
			{
				_selectionViewportClampMouseUpTimestamp = e.Timestamp;
				EditBox.TextArea.MouseSelectionMode = MouseSelectionMode.None;
				EditBox.TextArea.ReleaseMouseCapture();
				EditBox.TextArea.TextView?.ReleaseMouseCapture();
				EditBox.ReleaseMouseCapture();
				e.Handled = true;
			}
		}
	}

	private void MarkCodeBlockAttachmentPointerActionHandled(int mouseDownTimestamp)
	{
		_codeBlockAttachmentPointerActionPending = true;
		_codeBlockAttachmentPointerActionMouseDownTimestamp = mouseDownTimestamp;
	}

	private void ResetStaleCodeBlockAttachmentPointerAction(int mouseDownTimestamp)
	{
		if (_codeBlockAttachmentPointerActionPending && _codeBlockAttachmentPointerActionMouseDownTimestamp != mouseDownTimestamp)
		{
			_codeBlockAttachmentPointerActionPending = false;
			_codeBlockAttachmentPointerActionClearScheduled = false;
			_codeBlockAttachmentPointerActionMouseDownTimestamp = -1;
		}
	}

	private bool TryCompleteCodeBlockAttachmentPointerAction(MouseButtonEventArgs e)
	{
		if (!_codeBlockAttachmentPointerActionPending || e == null)
		{
			return false;
		}
		EditBox?.TextArea?.ReleaseMouseCapture();
		EditBox?.TextArea?.TextView?.ReleaseMouseCapture();
		EditBox?.ReleaseMouseCapture();
		e.Handled = true;
		if (!_codeBlockAttachmentPointerActionClearScheduled)
		{
			_codeBlockAttachmentPointerActionClearScheduled = true;
			base.Dispatcher?.BeginInvoke((Action)delegate
			{
				_codeBlockAttachmentPointerActionPending = false;
				_codeBlockAttachmentPointerActionClearScheduled = false;
				_codeBlockAttachmentPointerActionMouseDownTimestamp = -1;
			}, DispatcherPriority.Input);
		}
		return true;
	}

	private bool TryCompleteInternalSelectionClickWithoutDrag(MouseButtonEventArgs e)
	{
		if (_internalSelectionDragState == null || _internalSelectionDragState.DragStarted || e == null || EditBox?.Document == null || EditBox.TextArea?.TextView == null)
		{
			return false;
		}
		Point position = e.GetPosition(EditBox.TextArea.TextView);
		int caretOffset = TryGetDocumentOffsetFromTextViewPoint(position) ?? TryGetDocumentBoundaryOffsetFromTextViewPoint(position) ?? _internalSelectionDragState.SelectionStart;
		EditBox.TextArea.MouseSelectionMode = MouseSelectionMode.None;
		EditBox.TextArea.ReleaseMouseCapture();
		EditBox.TextArea.TextView?.ReleaseMouseCapture();
		EditBox.ReleaseMouseCapture();
		RunWithoutCaretVerticalOffsetNotification(delegate
		{
			EditBox.SelectionLength = 0;
			EditBox.CaretOffset = Math.Max(0, Math.Min(caretOffset, EditBox.Document.TextLength));
			TryNormalizeCaretOutsideLinkDisplayBoundary();
		});
		RecordLastInsertRangeFromEditor();
		TryDismissEditorMenu();
		_internalSelectionDragState = null;
		e.Handled = true;
		return true;
	}

	internal static bool ShouldSuppressCaretVerticalOffsetNotificationForInternalSelectionClick(bool hasInternalSelectionDragState, bool dragStarted)
	{
		if (hasInternalSelectionDragState)
		{
			return !dragStarted;
		}
		return false;
	}

	private void CancelNativeMouseSelectionAfterSemanticDrag(MouseButtonEventArgs e)
	{
		if (EditBox?.TextArea != null)
		{
			EditBox.TextArea.MouseSelectionMode = MouseSelectionMode.None;
			EditBox.TextArea.ReleaseMouseCapture();
			EditBox.TextArea.TextView?.ReleaseMouseCapture();
			EditBox.ReleaseMouseCapture();
			e.Handled = true;
		}
	}

	private void CancelNativeMouseSelectionForHeadingFoldClick()
	{
		if (EditBox?.TextArea != null)
		{
			EditBox.TextArea.MouseSelectionMode = MouseSelectionMode.None;
			EditBox.TextArea.ClearSelection();
			EditBox.TextArea.ReleaseMouseCapture();
			EditBox.TextArea.TextView?.ReleaseMouseCapture();
			EditBox.ReleaseMouseCapture();
			EditBox.SelectionLength = 0;
		}
	}

	internal void PrepareHeadingFoldTogglePointerCompletion()
	{
		_handledHeadingFoldMouseUpTimestamp = null;
		_suppressNextEditorMouseUpAfterHeadingFoldToggle = true;
		CancelNativeMouseSelectionForHeadingFoldClick();
		ClearSemanticSelectionDragState();
	}

	private void ScheduleSemanticDragSelectionRestoreAfterMouseUp(int? anchorOffset, int? activeOffset, int? selectionStart, int? selectionEnd)
	{
		if (_semanticSelectionMouseUpRestoreScheduled)
		{
			return;
		}
		_semanticSelectionMouseUpRestoreScheduled = true;
		base.Dispatcher?.BeginInvoke((Action)delegate
		{
			base.Dispatcher?.BeginInvoke((Action)delegate
			{
				if (!TryRestoreCapturedSemanticDragSelection(anchorOffset, activeOffset, selectionStart, selectionEnd))
				{
					RestoreLastSemanticDragSelectionIfNeeded();
				}
				ClearSemanticSelectionDragState();
			}, DispatcherPriority.ApplicationIdle);
		}, DispatcherPriority.ContextIdle);
	}

	private bool TryRestoreCapturedSemanticDragSelection(int? anchorOffset, int? activeOffset, int? selectionStart, int? selectionEnd)
	{
		if (!anchorOffset.HasValue || !activeOffset.HasValue || !selectionStart.HasValue || !selectionEnd.HasValue || EditBox?.Document == null)
		{
			return false;
		}
		int num = Math.Max(0, Math.Min(selectionStart.Value, EditBox.Document.TextLength));
		int num2 = Math.Max(num, Math.Min(selectionEnd.Value, EditBox.Document.TextLength));
		if (num2 <= num)
		{
			return false;
		}
		int value = Math.Max(0, Math.Min(anchorOffset.Value, EditBox.Document.TextLength));
		int num3 = Math.Max(0, Math.Min(activeOffset.Value, EditBox.Document.TextLength));
		int? semanticSelectionAnchorOffset = _semanticSelectionAnchorOffset;
		_semanticSelectionAnchorOffset = value;
		_semanticSelectionDragHandled = true;
		_semanticSelectionLastActiveOffset = num3;
		_semanticSelectionLastStartOffset = num;
		_semanticSelectionLastEndOffset = num2;
		bool suppressSemanticSelectionNormalization = _suppressSemanticSelectionNormalization;
		_suppressSemanticSelectionNormalization = true;
		try
		{
			ApplyNormalizedSemanticSelection(num, num2, num3);
			_codeBlockSelectionLayer?.InvalidateVisual();
		}
		finally
		{
			_suppressSemanticSelectionNormalization = suppressSemanticSelectionNormalization;
			_semanticSelectionAnchorOffset = semanticSelectionAnchorOffset;
		}
		if (EditBox.SelectionStart == num)
		{
			return EditBox.SelectionStart + EditBox.SelectionLength == num2;
		}
		return false;
	}

	private void ScheduleCodeBlockContentNormalization()
	{
		base.Dispatcher?.BeginInvoke((Action)delegate
		{
			if (EditBox == null)
			{
				_semanticSelectionAnchorOffset = null;
				_semanticSelectionDragHandled = false;
				_semanticSelectionMouseUpRestoreScheduled = false;
				_semanticSelectionLastActiveOffset = null;
				_semanticSelectionLastStartOffset = null;
				_semanticSelectionLastEndOffset = null;
			}
			else
			{
				RestoreLastSemanticDragSelectionIfNeeded();
				if (ShouldNormalizeCaretIntoSemanticContent(EditBox.TextArea?.MouseSelectionMode ?? MouseSelectionMode.None, EditBox.SelectionLength))
				{
					NormalizeCaretIntoSemanticContentIfNeeded();
				}
				if (TryNormalizeCaretOutsideLinkDisplayBoundary())
				{
					RecordLastInsertRangeFromEditor();
					RefreshOpenMenuContexts();
				}
				if (!_suppressSemanticSelectionNormalization)
				{
					NormalizeSelectionInsideCodeBlockContentIfNeeded();
				}
				if (Mouse.LeftButton != MouseButtonState.Pressed)
				{
					_semanticSelectionAnchorOffset = null;
					_semanticSelectionDragHandled = false;
					_semanticSelectionMouseUpRestoreScheduled = false;
					_semanticSelectionLastActiveOffset = null;
					_semanticSelectionLastStartOffset = null;
					_semanticSelectionLastEndOffset = null;
				}
			}
		});
	}

	private void BindEvents()
	{
		_textChangeNotifyHandler.SetAction(NotifyScheduledTextChanged);
		_textChangeHandler.SetAction(HandlerTextChanged);
		_redrawNotifyHandler.SetAction(NotifyRedraw);
		if (EditBox?.TextArea?.Caret != null)
		{
			EditBox.TextArea.Caret.CaretRectangleTransform = TransformParentCaretRectangleAtAttachmentEnd;
		}
		DataChangedNotifier.ThemeModeChanged += OnAppThemeChanged;
		LocalSettings.SpellCheckChanged += OnSpellCheckChanged;
		EditorMenuLink.EditorAction += EditorMenuLink_EditorAction;
		BindLinkEditorPopupOwnerEvents();
	}

	private void UnbindEvents()
	{
		_projectImeRefresh.Cancel();
		StopSelectionEdgeAutoScroll();
		_textChangeNotifyHandler.StopAndClear();
		_textChangeHandler.StopAndClear();
		_redrawNotifyHandler.StopAndClear();
		DataChangedNotifier.ThemeModeChanged -= OnAppThemeChanged;
		LocalSettings.SpellCheckChanged -= OnSpellCheckChanged;
		EditorMenuLink.EditorAction -= EditorMenuLink_EditorAction;
		if (EditBox?.TextArea?.TextView != null)
		{
			EditBox.TextArea.TextView.ScrollOffsetChanged -= OnCodeBlockLanguageOverlayScrollChanged;
			EditBox.TextArea.TextView.VisualLinesChanged -= OnCodeBlockLanguageOverlayLayoutChanged;
		}
		if (EditBox?.TextArea?.Caret != null)
		{
			EditBox.TextArea.Caret.CaretRectangleTransform = null;
		}
		DestroyLinkMenuPopup();
		UnbindLinkEditorPopupOwnerEvents();
	}

	private void EditorMenuLink_EditorAction(object sender, string tag)
	{
		switch (tag)
		{
		case "Edit":
		{
			LinkLineInfoModel linkLineInfo = _linkLineInfo;
			HideLinkMenuPopup();
			if (linkLineInfo != null)
			{
				UserActCollectUtils.AddClickEvent("task_detail", "link_hover_om", "edit_link");
				InsertLink(linkLineInfo);
			}
			break;
		}
		case "Copy":
		{
			string text = _linkLineInfo?.Url;
			HideLinkMenuPopup();
			UserActCollectUtils.AddClickEvent("task_detail", "link_hover_om", "copy_link");
			if (!string.IsNullOrEmpty(text))
			{
				try
				{
					System.Windows.Clipboard.SetText(text);
				}
				catch (Exception)
				{
				}
				Utils.FindParent<IToastShowWindow>(this)?.TryToastString(null, Utils.GetString("Copied"));
			}
			break;
		}
		case "Leave":
			HideLinkMenuPopup();
			break;
		}
	}

	private void OnEditorLostFocus(object sender, RoutedEventArgs e)
	{
		RecordLastInsertRangeFromEditor();
		TryConvertTrackedRawMarkdownLinkUrlIfCaretLeft(force: true);
		ClearBackspaceRetainedTypingMarks();
		bool retainSelectionForToolbarInteraction = _retainSelectionForToolbarInteraction;
		ClearToolbarSelectionRetention();
		_currentFocused = false;
		ClearSemanticSelectionDragState();
		_selectionControl?.TryRemoveTaskSelector();
		if (!retainSelectionForToolbarInteraction && (_customEditorPopup == null || !_customEditorPopup.IsOpen))
		{
			EditBox.SelectionLength = 0;
		}
		else if (retainSelectionForToolbarInteraction)
		{
			RequestImmediateRedrawForSelectionOrCaret();
		}
		HideLinkMenuPopup();
	}

	private void NotifyRedraw(object sender, EventArgs e)
	{
		base.Dispatcher?.BeginInvoke((Action)delegate
		{
			try
			{
				EditBox?.TextArea?.TextView?.Redraw();
			}
			catch (ArgumentException)
			{
			}
		}, DispatcherPriority.Background);
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
			EditBox.TextChanged -= EditBoxOnTextChanged;
			SpellCheckProvider?.ForceRefresh();
			EditBox.TextChanged += EditBoxOnTextChanged;
		}
	}

	private void OnEditKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
	{
		if (CodeBlockAttachmentControl.TryFindFromOriginalSource(e.OriginalSource, out var _))
		{
			return;
		}
		if (_inImeProcess)
		{
			_inImeProcess = false;
			return;
		}
		switch (e.Key)
		{
		case Key.Up:
			if (SelectionPopup != null && SelectionPopup.IsOpen)
			{
				_selectionControl.Move(forward: true);
				e.Handled = true;
				return;
			}
			if (_styleMenuPopup != null && _styleMenuPopup.IsOpen)
			{
				StyleMenu.Move(forward: true);
				e.Handled = true;
				return;
			}
			if (IsProjectPopupOpen())
			{
				_projectOrGroupPopup.UpDownSelect(isUp: true);
				e.Handled = true;
				return;
			}
			break;
		case Key.Down:
			if (SelectionPopup != null && SelectionPopup.IsOpen)
			{
				_selectionControl.Move(forward: false);
				e.Handled = true;
				return;
			}
			if (_styleMenuPopup != null && _styleMenuPopup.IsOpen)
			{
				StyleMenu.Move(forward: false);
				e.Handled = true;
				return;
			}
			if (IsProjectPopupOpen())
			{
				_projectOrGroupPopup.UpDownSelect(isUp: false);
				e.Handled = true;
				return;
			}
			break;
		case Key.Return:
			if (SelectionPopup != null && SelectionPopup.IsOpen)
			{
				_selectionControl.TrySelectItem();
				e.Handled = true;
				return;
			}
			if (_styleMenuPopup != null && _styleMenuPopup.IsOpen)
			{
				StyleMenu.SelectIndex();
				_styleMenuPopup.IsOpen = false;
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
		case Key.LeftShift:
			TryShowEditorMenu();
			break;
		case Key.Right:
			if (IsProjectPopupOpen())
			{
				_projectOrGroupPopup.LeftRightSelect(isLeft: false);
				e.Handled = true;
				return;
			}
			TryShowEditorMenu();
			break;
		case Key.Left:
			if (IsProjectPopupOpen())
			{
				_projectOrGroupPopup.LeftRightSelect(isLeft: true);
				e.Handled = true;
				return;
			}
			break;
		}
		if (_caretBeforeMoveUp == -1)
		{
			_caretBeforeMoveUp = 0;
		}
		else if (_caretBeforeMoveUp == EditBox.CaretOffset && !Utils.IfShiftPressed())
		{
			if (e.Key == Key.Up)
			{
				this.MoveUp?.Invoke(sender, null);
			}
			if (e.Key == Key.Down)
			{
				this.MoveDown?.Invoke(sender, null);
			}
		}
		this.KeyUp?.Invoke(sender, e);
	}

	private void NotifyTextChanged(object sender, EventArgs e)
	{
		if (!_suppressExternalTextChanged)
		{
			MarkLocalDocumentChangePending();
			QueueTextChangedNotification(sender, _editingTaskId);
		}
	}

	private void NotifyScheduledTextChanged(object sender, EventArgs e)
	{
		QueueTextChangedNotification(sender, _scheduledTextChangeTaskId);
	}

	private void QueueTextChangedNotification(object sender, string taskId)
	{
		Action action = delegate
		{
			if (!_suppressExternalTextChanged && _hasPendingLocalDocumentChanges && string.Equals(taskId, _editingTaskId, StringComparison.Ordinal))
			{
				this.TextChanged?.Invoke(sender, new MarkDownEditorTextChangedEventArgs(taskId));
			}
		};
		if (base.Dispatcher == null || base.Dispatcher.CheckAccess())
		{
			action();
		}
		else
		{
			base.Dispatcher.Invoke(DispatcherPriority.Normal, action);
		}
	}

	private void OnUndoCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
	{
		e.CanExecute = _semanticUndoHistory.Count > 1;
		e.Handled = true;
	}

	private void OnPreviewCopyCommandExecuted(object sender, ExecutedRoutedEventArgs e)
	{
		if (!IsCodeBlockAttachmentEditorInputSource(e?.OriginalSource) && e.Command == ApplicationCommands.Copy)
		{
			TextEditor editBox = EditBox;
			if (editBox == null || editBox.SelectionLength > 0)
			{
				TryCopySelectionToClipboard();
				e.Handled = true;
			}
		}
	}

	private void OnCopyCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
	{
		TextEditor editBox = EditBox;
		e.CanExecute = editBox != null && editBox.SelectionLength > 0;
		e.Handled = true;
	}

	private void OnCopyCommandExecuted(object sender, ExecutedRoutedEventArgs e)
	{
		TextEditor editBox = EditBox;
		if (editBox != null && editBox.SelectionLength > 0)
		{
			TryCopySelectionToClipboard();
		}
		e.Handled = true;
	}

	private void OnCutCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
	{
		int canExecute;
		if (!ReadOnly)
		{
			TextEditor editBox = EditBox;
			canExecute = ((editBox != null && editBox.SelectionLength > 0) ? 1 : 0);
		}
		else
		{
			canExecute = 0;
		}
		e.CanExecute = (byte)canExecute != 0;
		e.Handled = true;
	}

	private void OnCutCommandExecuted(object sender, ExecutedRoutedEventArgs e)
	{
		if (!ReadOnly)
		{
			TextEditor editBox = EditBox;
			if (editBox == null || editBox.SelectionLength > 0)
			{
				TryCutSelectedSemanticRange();
				e.Handled = true;
				return;
			}
		}
		e.Handled = true;
	}

	private bool TryCutSelectedSemanticRange()
	{
		if (!ReadOnly)
		{
			TextEditor editBox = EditBox;
			if (editBox == null || editBox.SelectionLength > 0)
			{
				TryCopySelectionToClipboard();
				return TryRemoveSelectedSemanticRange();
			}
		}
		return false;
	}

	private void OnRedoCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
	{
		e.CanExecute = _semanticRedoHistory.Count > 0;
		e.Handled = true;
	}

	private void OnUndoCommandExecuted(object sender, ExecutedRoutedEventArgs e)
	{
		TryPerformSemanticUndo();
		e.Handled = true;
	}

	private void OnRedoCommandExecuted(object sender, ExecutedRoutedEventArgs e)
	{
		TryPerformSemanticRedo();
		e.Handled = true;
	}

	private bool TryCopySelectionToClipboard()
	{
		SelectedMarkdownClipboardPayload selectedMarkdownClipboardPayload = GetSelectedMarkdownClipboardPayload();
		string text = selectedMarkdownClipboardPayload.InternalMarkdown ?? string.Empty;
		string text2 = selectedMarkdownClipboardPayload.ExternalMarkdown ?? string.Empty;
		string plainText = selectedMarkdownClipboardPayload.PlainText ?? string.Empty;
		if (string.IsNullOrEmpty(text) && string.IsNullOrEmpty(text2))
		{
			return false;
		}
		try
		{
			System.Windows.Forms.DataObject dataObject = CreateSelectionClipboardDataObject();
			PopulateSelectionClipboardDataObject(dataObject, text, text2, plainText);
			System.Windows.Forms.Clipboard.SetDataObject(dataObject, copy: false, 4, 60);
			return true;
		}
		catch (Exception ex)
		{
			UtilLog.Error("Clipboard.SetDataObject: " + ex.Message);
			return false;
		}
	}

	private static void PopulateSelectionClipboardDataObject(System.Windows.Forms.DataObject dataObject, string internalMarkdown, string externalMarkdown, string plainText)
	{
		if (dataObject != null)
		{
			if (string.IsNullOrEmpty(externalMarkdown))
			{
				externalMarkdown = RemoveInlineSemanticMetadata(internalMarkdown);
			}
			if (string.IsNullOrEmpty(internalMarkdown))
			{
				internalMarkdown = externalMarkdown;
			}
			externalMarkdown = RemoveBlockSemanticMetadataForExternalCopy(externalMarkdown);
			externalMarkdown = ConvertColoredHighlightsToPortableSyntax(externalMarkdown);
			externalMarkdown = ConvertTaskListMarkersToBulletListForExternalCopy(externalMarkdown);
			string text = MarkdownClipboardHtmlRenderer.CreateHtml(RemoveBlockSemanticMetadataForExternalCopy(RemoveInlineSemanticMetadata(internalMarkdown)));
			if (!string.IsNullOrEmpty(text))
			{
				dataObject.SetText(text, System.Windows.Forms.TextDataFormat.Html);
			}
			string data = RemovePersistenceEscapesForExternalCopy(externalMarkdown);
			string text2 = plainText ?? string.Empty;
			string text3 = text2.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");
			dataObject.SetText(text3, System.Windows.Forms.TextDataFormat.UnicodeText);
			dataObject.SetText(text3, System.Windows.Forms.TextDataFormat.Text);
			dataObject.SetData(typeof(string).FullName, autoConvert: false, text3);
			dataObject.SetData("Markdown", autoConvert: false, data);
			dataObject.SetData("TickTickMarkdown", autoConvert: false, internalMarkdown);
			dataObject.SetData("TickTickPlainText", autoConvert: false, text2);
		}
	}

	private System.Windows.Forms.DataObject CreateSelectionClipboardDataObject()
	{
		System.Windows.Forms.DataObject dataObject = new System.Windows.Forms.DataObject();
		CopySelectionClipboardFormats(dataObject);
		return dataObject;
	}

	private void CopySelectionClipboardFormats(System.Windows.Forms.DataObject target)
	{
		try
		{
			if (EditBox?.TextArea?.Selection != null && !EditBox.TextArea.Selection.IsEmpty)
			{
				CopyDataObjectFormats(EditBox.TextArea.Selection.CreateDataObject(EditBox.TextArea), target);
			}
		}
		catch (Exception ex)
		{
			UtilLog.Error("Selection.CreateDataObject: " + ex.Message);
		}
	}

	private static void CopyDataObjectFormats(System.Windows.IDataObject source, System.Windows.Forms.DataObject target)
	{
		if (source == null || target == null)
		{
			return;
		}
		string[] formats = source.GetFormats(autoConvert: false);
		foreach (string text in formats)
		{
			if (string.IsNullOrEmpty(text))
			{
				continue;
			}
			try
			{
				object data = source.GetData(text, autoConvert: false);
				if (!(data is string textData))
				{
					goto IL_0080;
				}
				if (string.Equals(text, System.Windows.DataFormats.Html, StringComparison.Ordinal))
				{
					target.SetText(textData, System.Windows.Forms.TextDataFormat.Html);
					continue;
				}
				if (string.Equals(text, System.Windows.DataFormats.UnicodeText, StringComparison.Ordinal))
				{
					target.SetText(textData, System.Windows.Forms.TextDataFormat.UnicodeText);
					continue;
				}
				if (!string.Equals(text, System.Windows.DataFormats.Text, StringComparison.Ordinal))
				{
					goto IL_0080;
				}
				target.SetText(textData, System.Windows.Forms.TextDataFormat.Text);
				goto end_IL_0020;
				IL_0080:
				target.SetData(text, autoConvert: false, data);
				end_IL_0020:;
			}
			catch (Exception)
			{
			}
		}
	}

	private string GetSelectedPersistedMarkdown()
	{
		return GetSelectedMarkdownClipboardPayload().InternalMarkdown;
	}

	private SelectedMarkdownClipboardPayload GetSelectedMarkdownClipboardPayload()
	{
		if (EditBox == null || EditBox.SelectionLength <= 0)
		{
			return new SelectedMarkdownClipboardPayload();
		}
		int selectionStart = EditBox.SelectionStart;
		int selectionEnd = EditBox.SelectionStart + EditBox.SelectionLength;
		ExpandSelectionToCodeBlockBoundariesIfNeeded(ref selectionStart, ref selectionEnd);
		DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(selectionStart);
		bool flag = selectionEnd <= lineByOffset.EndOffset && (!SelectionStartsAtSemanticLineContentStart(lineByOffset, selectionStart) || !SelectionEndsAtSemanticLineContentEnd(lineByOffset, selectionEnd));
		ExpandSelectionToLinkBoundariesIfNeeded(ref selectionStart, ref selectionEnd);
		if (selectionEnd <= selectionStart)
		{
			return new SelectedMarkdownClipboardPayload();
		}
		string persistedText = GetPersistedText();
		if (string.IsNullOrEmpty(persistedText))
		{
			return new SelectedMarkdownClipboardPayload();
		}
		if (!TryGetPersistedSelectionRange(selectionStart, selectionEnd, persistedText, out var persistedStart, out var persistedEnd))
		{
			return new SelectedMarkdownClipboardPayload();
		}
		string selectedMarkdown = persistedText.Substring(persistedStart, persistedEnd - persistedStart);
		int textLength = EditBox.Document.TextLength;
		int num = Math.Max(0, Math.Min(selectionStart, textLength));
		int num2 = Math.Max(num, Math.Min(selectionEnd, textLength));
		string text = EditBox.Document.GetText(num, num2 - num);
		selectedMarkdown = PreserveBlockSemanticsForSelection(selectedMarkdown, num, num2, text);
		string externalMarkdown = RemoveInlineSemanticMetadata(selectedMarkdown);
		List<MarkdownInlineDisplaySegment> selectedInlineSemanticMarks = GetSelectedInlineSemanticMarks(num, num2, textLength);
		if (selectedInlineSemanticMarks.Count > 0 || flag)
		{
			string text2 = BuildSelectedSemanticMarkdown(text, selectedInlineSemanticMarks, num, num2, !flag);
			return CreateSelectedMarkdownClipboardPayload(text2, text2, text);
		}
		PreparedEditorTextState preparedEditorTextState = PrepareEditorTextState(selectedMarkdown);
		if (AreDisplayTextsEqual(preparedEditorTextState.DisplayText, text) && AreSemanticMarksEquivalent(preparedEditorTextState.SemanticMarks, selectedInlineSemanticMarks))
		{
			return CreateSelectedMarkdownClipboardPayload(selectedMarkdown, externalMarkdown, text);
		}
		string text3 = BuildSelectedSemanticMarkdown(text, selectedInlineSemanticMarks, num, num2);
		return CreateSelectedMarkdownClipboardPayload(text3, text3, text);
	}

	private static SelectedMarkdownClipboardPayload CreateSelectedMarkdownClipboardPayload(string internalMarkdown, string externalMarkdown, string plainText = null)
	{
		return new SelectedMarkdownClipboardPayload
		{
			InternalMarkdown = (internalMarkdown ?? string.Empty),
			ExternalMarkdown = ConvertTaskListMarkersToBulletListForExternalCopy(ConvertColoredHighlightsToPortableSyntax(externalMarkdown)),
			PlainText = CreatePlainTextClipboardText(internalMarkdown, plainText)
		};
	}

	private static bool AreDisplayTextsEqual(string left, string right)
	{
		return string.Equals(NormalizeLineEndings(left), NormalizeLineEndings(right), StringComparison.Ordinal);
	}

	private static string NormalizeLineEndings(string text)
	{
		return (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
	}

	private static string RemoveInlineSemanticMetadata(string markdown)
	{
		string rawText = markdown ?? string.Empty;
		MarkdownInlineSemanticMetadata.Extract(ref rawText);
		return rawText;
	}

	private static string RemoveBlockSemanticMetadataForExternalCopy(string markdown)
	{
		if (string.IsNullOrEmpty(markdown))
		{
			return string.Empty;
		}
		StringBuilder stringBuilder = new StringBuilder(markdown.Length);
		int i = 0;
		bool flag = false;
		char c = '\0';
		while (i <= markdown.Length)
		{
			int num = i;
			for (; i < markdown.Length && markdown[i] != '\r' && markdown[i] != '\n'; i++)
			{
			}
			string text = markdown.Substring(num, i - num);
			int num2 = i;
			if (i < markdown.Length)
			{
				if (markdown[i] == '\r')
				{
					i++;
					if (i < markdown.Length && markdown[i] == '\n')
					{
						i++;
					}
				}
				else
				{
					i++;
				}
			}
			string value = markdown.Substring(num2, i - num2);
			stringBuilder.Append(flag ? text : RemoveTrailingBlockSemanticMetadataComments(text));
			stringBuilder.Append(value);
			if (TryGetMarkdownFenceMarker(text, out var marker))
			{
				if (!flag)
				{
					flag = true;
					c = marker;
				}
				else if (marker == c)
				{
					flag = false;
					c = '\0';
				}
			}
			if (i >= markdown.Length)
			{
				break;
			}
		}
		return stringBuilder.ToString();
	}

	private static string RemoveTrailingBlockSemanticMetadataComments(string lineText)
	{
		string text = lineText ?? string.Empty;
		while (!string.IsNullOrEmpty(text))
		{
			int contentEnd;
			for (contentEnd = text.Length; contentEnd > 0 && char.IsWhiteSpace(text[contentEnd - 1]); contentEnd--)
			{
			}
			Match match = BlockMetadataCommentRegex.Matches(text).Cast<Match>().LastOrDefault((Match candidate) => candidate.Success && candidate.Index + candidate.Length == contentEnd);
			if (match == null || !IsExternalCopyBlockMetadataJson(match.Groups["json"].Value))
			{
				break;
			}
			text = text.Substring(0, match.Index) + text.Substring(contentEnd);
		}
		return text;
	}

	private static bool IsExternalCopyBlockMetadataJson(string json)
	{
		if (IsHeadingFoldedMetadataJson(json))
		{
			return true;
		}
		int indent;
		return TryGetParagraphIndentMetadataJson(json, out indent);
	}

	internal static string ConvertColoredHighlightsToPortableSyntax(string markdown)
	{
		if (string.IsNullOrEmpty(markdown))
		{
			return string.Empty;
		}
		string text = (markdown.Contains("\r\n") ? "\r\n" : (markdown.Contains("\r") ? "\r" : "\n"));
		string[] array = markdown.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
		bool flag = false;
		char c = '\0';
		for (int i = 0; i < array.Length; i++)
		{
			string text2 = array[i] ?? string.Empty;
			if (TryGetMarkdownFenceMarker(text2, out var marker))
			{
				if (!flag)
				{
					flag = true;
					c = marker;
				}
				else if (marker == c)
				{
					flag = false;
					c = '\0';
				}
			}
			else if (!flag)
			{
				array[i] = ColoredHighlightOpeningDelimiterRegex.Replace(text2, "==");
			}
		}
		string text3 = string.Join("\n", array);
		if (!(text == "\n"))
		{
			return text3.Replace("\n", text);
		}
		return text3;
	}

	private static string ConvertTaskListMarkersToBulletListForExternalCopy(string markdown)
	{
		if (string.IsNullOrEmpty(markdown))
		{
			return string.Empty;
		}
		string text = (markdown.Contains("\r\n") ? "\r\n" : (markdown.Contains("\r") ? "\r" : "\n"));
		string[] array = markdown.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
		bool flag = false;
		char c = '\0';
		for (int i = 0; i < array.Length; i++)
		{
			string text2 = array[i] ?? string.Empty;
			if (TryGetMarkdownFenceMarker(text2, out var marker))
			{
				if (!flag)
				{
					flag = true;
					c = marker;
				}
				else if (marker == c)
				{
					flag = false;
					c = '\0';
				}
			}
			else if (!flag)
			{
				array[i] = ExternalTaskListMarkerRegex.Replace(text2, "$1- ");
			}
		}
		string text3 = string.Join("\n", array);
		if (!(text == "\n"))
		{
			return text3.Replace("\n", text);
		}
		return text3;
	}

	private static string CreatePlainTextClipboardText(string markdown, string fallbackText)
	{
		if (string.IsNullOrEmpty(markdown))
		{
			return fallbackText ?? string.Empty;
		}
		string text = MarkdownClipboardHtmlRenderer.CreatePlainText(markdown);
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		return fallbackText ?? string.Empty;
	}

	private static string RemovePersistenceEscapesForExternalCopy(string markdown)
	{
		if (string.IsNullOrEmpty(markdown))
		{
			return string.Empty;
		}
		StringBuilder stringBuilder = new StringBuilder(markdown.Length);
		int i = 0;
		bool flag = false;
		char c = '\0';
		while (i <= markdown.Length)
		{
			int num = i;
			for (; i < markdown.Length && markdown[i] != '\r' && markdown[i] != '\n'; i++)
			{
			}
			string text = markdown.Substring(num, i - num);
			if (TryGetMarkdownFenceMarker(text, out var marker))
			{
				stringBuilder.Append(text);
				if (!flag)
				{
					flag = true;
					c = marker;
				}
				else if (marker == c)
				{
					flag = false;
					c = '\0';
				}
			}
			else
			{
				stringBuilder.Append(flag ? text : RemovePersistenceEscapesFromExternalLine(text));
			}
			if (i < markdown.Length)
			{
				if (markdown[i] == '\r' && i + 1 < markdown.Length && markdown[i + 1] == '\n')
				{
					stringBuilder.Append("\r\n");
					i += 2;
				}
				else
				{
					stringBuilder.Append(markdown[i]);
					i++;
				}
			}
			if (i >= markdown.Length)
			{
				break;
			}
		}
		return stringBuilder.ToString();
	}

	private static string RemovePersistenceEscapesFromExternalLine(string line)
	{
		if (string.IsNullOrEmpty(line))
		{
			return line ?? string.Empty;
		}
		StringBuilder stringBuilder = new StringBuilder(line.Length);
		bool flag = false;
		for (int i = 0; i < line.Length; i++)
		{
			char c = line[i];
			if (!flag && c == '\\' && i + 1 < line.Length && MarkdownSemanticConverter.IsEscapableLiteralChar(line[i + 1]))
			{
				stringBuilder.Append(line[i + 1]);
				i++;
				continue;
			}
			stringBuilder.Append(c);
			if (c == '`')
			{
				flag = !flag;
			}
		}
		return stringBuilder.ToString();
	}

	private static bool TryGetMarkdownFenceMarker(string line, out char marker)
	{
		marker = '\0';
		string text = RemoveLeadingBlockQuoteMarkers(line).TrimStart(' ', '\t');
		if (text.Length < 3)
		{
			return false;
		}
		char c = text[0];
		if (c != '`' && c != '~')
		{
			return false;
		}
		if (text[1] != c || text[2] != c)
		{
			return false;
		}
		marker = c;
		return true;
	}

	private static string RemoveLeadingBlockQuoteMarkers(string line)
	{
		string text = line ?? string.Empty;
		while (true)
		{
			text = text.TrimStart(' ', '\t');
			if (!text.StartsWith(">", StringComparison.Ordinal))
			{
				break;
			}
			text = text.Substring(1);
		}
		return text;
	}

	private string BuildSelectedSemanticMarkdown(string selectedDisplayText, IEnumerable<MarkdownInlineDisplaySegment> selectedMarks, int selectionStart, int selectionEnd, bool preserveBlockStyles = true)
	{
		MarkdownSemanticDocument markdownSemanticDocument = (preserveBlockStyles ? BuildSelectionSemanticDocument(selectionStart, selectionEnd, selectedDisplayText) : new MarkdownSemanticDocument());
		int[] displayOffsetMap = null;
		return BuildPersistedText(selectedDisplayText ?? string.Empty, (selectedMarks ?? Enumerable.Empty<MarkdownInlineDisplaySegment>()).ToList(), new List<MarkdownInlineDisplaySegment>(), markdownSemanticDocument, BuildSemanticBlockIndex(markdownSemanticDocument), ref displayOffsetMap);
	}

	private List<MarkdownInlineDisplaySegment> GetSelectedInlineSemanticMarks(int selectionStart, int selectionEnd, int textLength)
	{
		List<MarkdownInlineDisplaySegment> list = new List<MarkdownInlineDisplaySegment>();
		if (selectionEnd <= selectionStart)
		{
			return list;
		}
		foreach (MarkdownInlineDisplaySegment semanticMark in GetSemanticMarkList())
		{
			if (semanticMark != null && semanticMark.SourceLength > 0 && MarkdownInlineSemanticMetadata.CanSerialize(semanticMark))
			{
				int val = Math.Max(0, Math.Min(semanticMark.SourcePosition, textLength));
				int val2 = Math.Max(val, Math.Min(semanticMark.SourcePosition + semanticMark.SourceLength, textLength));
				int num = Math.Max(val, selectionStart);
				int num2 = Math.Min(val2, selectionEnd);
				if (num2 > num)
				{
					list.Add(new MarkdownInlineDisplaySegment
					{
						Kind = semanticMark.Kind,
						SourcePosition = num - selectionStart,
						SourceLength = num2 - num,
						OpeningDelimiterLength = 0,
						ClosingDelimiterLength = 0,
						Color = semanticMark.Color,
						LinkText = semanticMark.LinkText,
						Url = semanticMark.Url,
						UrlLength = semanticMark.UrlLength,
						UrlPosition = semanticMark.UrlPosition
					});
				}
			}
		}
		return (from @group in list.GroupBy(GetSemanticMarkComparisonKey)
			select @group.First() into mark
			orderby mark.SourcePosition, mark.SourceLength
			select mark).ThenBy(MarkdownInlineSemanticMetadata.GetPersistenceIdentity).ToList();
	}

	private static bool AreSemanticMarksEquivalent(IEnumerable<MarkdownInlineDisplaySegment> left, IEnumerable<MarkdownInlineDisplaySegment> right)
	{
		List<string> semanticMarkComparisonKeys = GetSemanticMarkComparisonKeys(left);
		List<string> semanticMarkComparisonKeys2 = GetSemanticMarkComparisonKeys(right);
		if (semanticMarkComparisonKeys.Count != semanticMarkComparisonKeys2.Count)
		{
			return false;
		}
		for (int i = 0; i < semanticMarkComparisonKeys.Count; i++)
		{
			if (!string.Equals(semanticMarkComparisonKeys[i], semanticMarkComparisonKeys2[i], StringComparison.Ordinal))
			{
				return false;
			}
		}
		return true;
	}

	private static List<string> GetSemanticMarkComparisonKeys(IEnumerable<MarkdownInlineDisplaySegment> marks)
	{
		return (marks ?? Enumerable.Empty<MarkdownInlineDisplaySegment>()).Where((MarkdownInlineDisplaySegment mark) => mark != null && mark.SourceLength > 0).Select(GetSemanticMarkComparisonKey).OrderBy((string key) => key, StringComparer.Ordinal)
			.ToList();
	}

	private static string GetSemanticMarkComparisonKey(MarkdownInlineDisplaySegment mark)
	{
		return Math.Max(0, mark.SourcePosition) + ":" + Math.Max(0, mark.SourceLength) + ":" + MarkdownInlineSemanticMetadata.GetPersistenceIdentity(mark);
	}

	private string PreserveBlockSemanticsForSelection(string selectedMarkdown, int selectionStart, int selectionEnd, string selectedDisplayText)
	{
		if (string.IsNullOrEmpty(selectedMarkdown) || EditBox?.Document == null || selectionEnd <= selectionStart)
		{
			return selectedMarkdown;
		}
		TextDocument document = EditBox.Document;
		int textLength = document.TextLength;
		int num = Math.Max(0, Math.Min(selectionStart, textLength));
		int num2 = Math.Max(num, Math.Min(selectionEnd, textLength));
		DocumentLine lineByOffset = document.GetLineByOffset(num);
		int val = ((num2 > num) ? (num2 - 1) : num2);
		DocumentLine lineByOffset2 = document.GetLineByOffset(Math.Max(0, Math.Min(val, textLength)));
		if (lineByOffset == null || lineByOffset2 == null || lineByOffset2.LineNumber < lineByOffset.LineNumber)
		{
			return selectedMarkdown;
		}
		if ((string.IsNullOrEmpty(selectedDisplayText) || (selectedDisplayText.IndexOf('\r') < 0 && selectedDisplayText.IndexOf('\n') < 0)) && (!SelectionStartsAtSemanticLineContentStart(lineByOffset, num) || !SelectionEndsAtSemanticLineContentEnd(lineByOffset, num2)))
		{
			return selectedMarkdown;
		}
		string[] array = selectedMarkdown.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
		bool flag = false;
		for (int i = lineByOffset.LineNumber; i <= lineByOffset2.LineNumber; i++)
		{
			int num3 = i - lineByOffset.LineNumber;
			if (num3 < 0 || num3 >= array.Length)
			{
				break;
			}
			DocumentLine lineByNumber = document.GetLineByNumber(i);
			if (lineByNumber != null && LineSelectionHasContent(lineByNumber, num, num2) && TryGetSemanticBlockAtLine(i - 1, out var block))
			{
				string semanticBlockPersistencePrefix = GetSemanticBlockPersistencePrefix(block);
				if (!string.IsNullOrEmpty(semanticBlockPersistencePrefix) && !array[num3].StartsWith(semanticBlockPersistencePrefix, StringComparison.Ordinal))
				{
					array[num3] = semanticBlockPersistencePrefix + array[num3];
					flag = true;
				}
			}
		}
		if (!flag)
		{
			return selectedMarkdown;
		}
		return string.Join("\n", array);
	}

	private MarkdownSemanticDocument BuildSelectionSemanticDocument(int selectionStart, int selectionEnd, string selectedDisplayText)
	{
		MarkdownSemanticDocument markdownSemanticDocument = new MarkdownSemanticDocument();
		if (EditBox?.Document == null || selectionEnd <= selectionStart)
		{
			return markdownSemanticDocument;
		}
		TextDocument document = EditBox.Document;
		int textLength = document.TextLength;
		int num = Math.Max(0, Math.Min(selectionStart, textLength));
		int num2 = Math.Max(num, Math.Min(selectionEnd, textLength));
		DocumentLine lineByOffset = document.GetLineByOffset(num);
		int val = ((num2 > num) ? (num2 - 1) : num2);
		DocumentLine lineByOffset2 = document.GetLineByOffset(Math.Max(0, Math.Min(val, textLength)));
		if (lineByOffset == null || lineByOffset2 == null || lineByOffset2.LineNumber < lineByOffset.LineNumber)
		{
			return markdownSemanticDocument;
		}
		if ((string.IsNullOrEmpty(selectedDisplayText) || (selectedDisplayText.IndexOf('\r') < 0 && selectedDisplayText.IndexOf('\n') < 0)) && (!SelectionStartsAtSemanticLineContentStart(lineByOffset, num) || !SelectionEndsAtSemanticLineContentEnd(lineByOffset, num2)))
		{
			return markdownSemanticDocument;
		}
		int num3 = lineByOffset.LineNumber - 1;
		int val2 = lineByOffset2.LineNumber - 1;
		object obj = _semanticDocument?.Blocks;
		if (obj == null)
		{
			IEnumerable<MarkdownBlockNode> enumerable = _semanticBlocksByLine?.Values;
			obj = (enumerable ?? Enumerable.Empty<MarkdownBlockNode>()).Where((MarkdownBlockNode block) => block != null).Distinct().ToList();
		}
		foreach (MarkdownBlockNode item in ((IEnumerable<MarkdownBlockNode>)obj).Where((MarkdownBlockNode block) => block != null))
		{
			int num4 = Math.Max(num3, item.StartLine);
			int num5 = Math.Min(val2, item.EndLine);
			if (num5 >= num4 && (item.Kind != MarkdownBlockKind.CodeBlock || (num4 == item.StartLine && num5 == item.EndLine)))
			{
				MarkdownBlockNode markdownBlockNode = CloneSemanticBlockAsLineRange(item, num4, num5);
				if (markdownBlockNode != null)
				{
					markdownBlockNode.StartLine -= num3;
					markdownBlockNode.EndLine -= num3;
					markdownSemanticDocument.Blocks.Add(markdownBlockNode);
				}
			}
		}
		return markdownSemanticDocument;
	}

	private static bool LineSelectionHasContent(DocumentLine line, int selectionStart, int selectionEnd)
	{
		if (line == null || selectionEnd <= selectionStart)
		{
			return false;
		}
		int num = Math.Max(line.Offset, selectionStart);
		return Math.Min(line.EndOffset, selectionEnd) > num;
	}

	private static string GetSemanticBlockPersistencePrefix(MarkdownBlockNode block)
	{
		if (block == null)
		{
			return string.Empty;
		}
		int num = Math.Max(0, block.Indent);
		string text = ((num > 0) ? new string(' ', num) : string.Empty);
		switch (block.Kind)
		{
		case MarkdownBlockKind.Heading:
			if (!block.Level.HasValue)
			{
				return string.Empty;
			}
			return text + new string('#', Math.Max(1, Math.Min(6, block.Level.Value))) + " ";
		case MarkdownBlockKind.BlockQuote:
			return "> " + GetPersistedBlockIndentText(block, num);
		case MarkdownBlockKind.List:
			return block.ListKind switch
			{
				MarkdownListKind.Task => text + ((block.Checked == true) ? "- [x] " : "- [ ] "), 
				MarkdownListKind.Bullet => text + "- ", 
				MarkdownListKind.Numbered => text + (string.IsNullOrEmpty(block.CustomOrder) ? "1" : block.CustomOrder) + ". ", 
				_ => string.Empty, 
			};
		default:
			return string.Empty;
		}
	}

	private bool TryGetPersistedSelectionRange(int selectionStart, int selectionEnd, string persistedText, out int persistedStart, out int persistedEnd)
	{
		persistedStart = 0;
		persistedEnd = 0;
		if (EditBox == null || string.IsNullOrEmpty(persistedText) || selectionEnd <= selectionStart)
		{
			return false;
		}
		int num = EditBox.Text?.Length ?? 0;
		int num2 = Math.Max(0, Math.Min(selectionStart, num));
		int num3 = Math.Max(num2, Math.Min(selectionEnd, num));
		if (num2 == 0 && num3 >= num)
		{
			persistedStart = 0;
			persistedEnd = persistedText.Length;
			return persistedEnd > persistedStart;
		}
		persistedStart = GetPersistedOffset(num2);
		persistedEnd = GetPersistedOffset(num3);
		if (persistedEnd < persistedStart)
		{
			int num4 = persistedEnd;
			int num5 = persistedStart;
			persistedStart = num4;
			persistedEnd = num5;
		}
		persistedStart = Math.Max(0, Math.Min(persistedStart, persistedText.Length));
		persistedEnd = Math.Max(0, Math.Min(persistedEnd, persistedText.Length));
		return persistedEnd > persistedStart;
	}

	private void ExpandSelectionToLinkBoundariesIfNeeded(ref int selectionStart, ref int selectionEnd)
	{
		if (EditBox?.Document == null || selectionEnd <= selectionStart)
		{
			return;
		}
		int textLength = EditBox.Document.TextLength;
		selectionStart = Math.Max(0, Math.Min(selectionStart, textLength));
		selectionEnd = Math.Max(selectionStart, Math.Min(selectionEnd, textLength));
		foreach (OffsetRange linkDisplayRange in GetLinkDisplayRanges())
		{
			if (linkDisplayRange != null && linkDisplayRange.End > linkDisplayRange.Start && selectionStart < linkDisplayRange.End && selectionEnd > linkDisplayRange.Start)
			{
				selectionStart = Math.Min(selectionStart, linkDisplayRange.Start);
				selectionEnd = Math.Max(selectionEnd, linkDisplayRange.End);
			}
		}
	}

	private IEnumerable<OffsetRange> GetLinkDisplayRanges()
	{
		string text = EditBox?.Text ?? string.Empty;
		int textLength = text.Length;
		HashSet<string> emitted = new HashSet<string>();
		foreach (MarkdownInlineDisplaySegment semanticMark in GetSemanticMarkList())
		{
			if (semanticMark != null && semanticMark.Kind == MarkdownMarkKind.Link && semanticMark.SourceLength > 0)
			{
				int num = Math.Max(0, Math.Min(semanticMark.SourcePosition, textLength));
				int num2 = Math.Max(num, Math.Min(semanticMark.SourcePosition + semanticMark.SourceLength, textLength));
				if (num2 > num && emitted.Add(num + ":" + num2))
				{
					yield return new OffsetRange
					{
						Start = num,
						End = num2
					};
				}
			}
		}
		foreach (PersistedMarkdownLinkMatch item in EnumeratePersistedMarkdownLinks(text))
		{
			if (item.Length > 0 && !IsLinkMatchInsideMarkdownImageSyntax(text, item.Index))
			{
				int num3 = Math.Max(0, Math.Min(item.Index, textLength));
				int num4 = Math.Max(num3, Math.Min(item.Index + item.Length, textLength));
				if (num4 > num3 && emitted.Add(num3 + ":" + num4))
				{
					yield return new OffsetRange
					{
						Start = num3,
						End = num4
					};
				}
			}
		}
	}

	private bool TryFindLinkDisplayRange(LinkLineInfoModel model, string original, out int displayStart, out int displayEnd)
	{
		displayStart = 0;
		displayEnd = 0;
		string text = EditBox?.Text ?? string.Empty;
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		int startIndex = -1;
		if (model != null && !model.TryResolveStartIndex(out startIndex))
		{
			return false;
		}
		if (!string.IsNullOrEmpty(original))
		{
			int num = 0;
			while (num <= text.Length)
			{
				int num2 = text.IndexOf(original, num, StringComparison.Ordinal);
				if (num2 < 0)
				{
					break;
				}
				int num3 = num2 + original.Length;
				if (IsLinkModelOffsetInRange(startIndex, num2, num3))
				{
					displayStart = num2;
					displayEnd = num3;
					return true;
				}
				num = num2 + Math.Max(1, original.Length);
			}
		}
		foreach (OffsetRange linkDisplayRange in GetLinkDisplayRanges())
		{
			if (linkDisplayRange != null && linkDisplayRange.End > linkDisplayRange.Start && IsLinkModelOffsetInRange(startIndex, linkDisplayRange.Start, linkDisplayRange.End))
			{
				displayStart = linkDisplayRange.Start;
				displayEnd = linkDisplayRange.End;
				return true;
			}
		}
		if (startIndex < 0 && !string.IsNullOrEmpty(original))
		{
			int num4 = text.IndexOf(original, StringComparison.Ordinal);
			if (num4 >= 0)
			{
				displayStart = num4;
				displayEnd = num4 + original.Length;
				return true;
			}
		}
		return false;
	}

	private static bool IsLinkModelOffsetInRange(int modelOffset, int start, int end)
	{
		if (modelOffset >= 0)
		{
			if (modelOffset >= start)
			{
				return modelOffset <= end;
			}
			return false;
		}
		return true;
	}

	public void UnRegisterInputHandler()
	{
		EditBox.Document.PropertyChanged -= OnEditBoxPropertyChanged;
	}

	public void RegisterInputHandler()
	{
		EditBox.Document.PropertyChanged -= OnEditBoxPropertyChanged;
		EditBox.Document.PropertyChanged += OnEditBoxPropertyChanged;
	}

	private void OnEditorGotFocus(object sender, RoutedEventArgs e)
	{
		_currentFocused = true;
		RequestSafeRedraw();
		InvalidMargin();
	}

	private void OnAppThemeChanged(object sender, EventArgs e)
	{
		_colorizer.OnThemeChanged(new Theme(this));
		_highLightColor = ThemeUtil.GetColor("TextAccentColor", this);
		_textColor = ThemeUtil.GetColor(_color100, this);
		_bracketColor = ThemeUtil.GetColor(_color20, this);
		EditBox.TextArea.Caret.CaretBrush = ResolveParentCaretBrush(_textColor);
		EditBox.TextArea.TextView.LinkTextForegroundBrush = _highLightColor;
		ApplyEditorForegroundResource();
		RefreshHeadingFoldingPlaceholderStyle();
		RefreshCodeColorizersForTheme();
		RefreshCodeBlockLanguageOverlayTheme();
		RequestSafeRedraw();
	}

	public void SetTheme(bool light = true)
	{
		ThemeUtil.SetTheme(light ? "light" : "dark", this);
		_isDark = !light;
		_colorizer.OnThemeChanged(new Theme(this));
		_highLightColor = ThemeUtil.GetColor("TextAccentColor", this);
		_bracketColor = ThemeUtil.GetColor(_color20, this);
		_textColor = ThemeUtil.GetColor(_color100, this);
		ApplyEditorForegroundResource();
		RefreshHeadingFoldingPlaceholderStyle();
		RefreshCodeColorizersForTheme();
		RefreshCodeBlockLanguageOverlayTheme();
	}

	public void SetEditorForegroundResource(string resourceKey)
	{
		_editorForegroundResourceKey = resourceKey;
		ApplyEditorForegroundResource();
	}

	private void ApplyEditorForegroundResource()
	{
		if (!string.IsNullOrEmpty(_editorForegroundResourceKey) && EditBox != null)
		{
			EditBox.SetResourceReference(System.Windows.Controls.Control.ForegroundProperty, _editorForegroundResourceKey);
			if (TryFindResource(_editorForegroundResourceKey) is Brush textColor)
			{
				_textColor = textColor;
				EditBox.TextArea.Caret.CaretBrush = ResolveParentCaretBrush(_textColor);
			}
		}
	}

	public void ClearTheme()
	{
		base.Resources.MergedDictionaries.Clear();
	}

	private void OnCaretPositionChanged(object sender, EventArgs e)
	{
		HideLinkMenuPopup();
		if (!TryExpandFinalHeadingFoldAtCaret())
		{
			MouseSelectionMode valueOrDefault = (EditBox?.TextArea?.MouseSelectionMode).GetValueOrDefault();
			int selectionLength = EditBox?.SelectionLength ?? 0;
			if (ShouldNormalizeCaretIntoSemanticContent(valueOrDefault, selectionLength))
			{
				NormalizeCaretIntoSemanticContentIfNeeded();
			}
			int num = EditBox?.CaretOffset ?? 0;
			if (TryConvertTrackedRawMarkdownLinkUrlIfCaretLeft())
			{
				num = EditBox?.CaretOffset ?? num;
			}
			ClearTypingMarkSuppressionsAwayFromOffset(num);
			ClearActiveTypingMarksAwayFromOffset(num);
			ClearBackspaceRetainedTypingMarksAwayFromOffset(num);
			InvalidMargin();
			RefreshOpenMenuContexts();
		}
	}

	private static bool ShouldNormalizeCaretIntoSemanticContent(MouseSelectionMode mouseSelectionMode, int selectionLength)
	{
		if (selectionLength <= 0)
		{
			return mouseSelectionMode == MouseSelectionMode.None;
		}
		return false;
	}

	public void UnRegisterCaretChanged()
	{
		EditBox.TextArea.Caret.PositionChanged -= NotifyCaretChanged;
	}

	public void RegisterCaretChanged()
	{
		EditBox.TextArea.Caret.PositionChanged -= NotifyCaretChanged;
		EditBox.TextArea.Caret.PositionChanged += NotifyCaretChanged;
	}

	internal void SuppressCaretVerticalOffsetNotificationForCurrentMouseGesture()
	{
		_suppressCaretVerticalOffsetNotificationForCurrentMouseGesture = true;
	}

	private void NotifyCaretChanged(object sender, EventArgs e)
	{
		int line = EditBox.TextArea.Caret.GetPosition().Line;
		if (!TryGetVisualOffset(new TextViewPosition(line, Math.Max(1, EditBox.TextArea.Caret.Column)), VisualYPosition.LineTop, out var offset))
		{
			return;
		}
		if (ShouldNotifyCaretVerticalOffset(CurrentFocused, _suppressCaretVerticalOffsetNotification, _internalSelectionDropCaretRestorePending, _codeBlockParagraphExitCaretPlacementPending, Mouse.LeftButton))
		{
			this.CaretVerticalOffsetChanged?.Invoke(this, offset);
		}
		bool flag = false;
		foreach (KeyValuePair<int, LinkInfo> item in LinkNameDict)
		{
			int valueOrDefault = (item.Value?.Link?.Length).GetValueOrDefault();
			if (valueOrDefault > 0)
			{
				int key = item.Key;
				int num = key + valueOrDefault;
				if (key < EditBox.CaretOffset && EditBox.CaretOffset < num)
				{
					flag = true;
					break;
				}
			}
		}
		DocumentLine lineByNumber = EditBox.Document.GetLineByNumber(line);
		string text = EditBox.Document.GetText(lineByNumber.Offset, lineByNumber.Length);
		Brush brush = ResolveParentCaretBrush(flag ? _highLightColor : _textColor);
		if (!brush.Equals(EditBox.TextArea.Caret.CaretBrush))
		{
			EditBox.TextArea.Caret.CaretBrush = brush;
			RequestImmediateCaretPreviewRedraw(line);
		}
		else if (!Regex.IsMatch(text, "[~|*|:]"))
		{
			if (_previewCaretChangedRedrew)
			{
				RequestImmediateCaretPreviewRedraw(line);
				_lastCaretPreviewRedrawLineNumber = null;
			}
			_previewCaretChangedRedrew = false;
		}
		else
		{
			_previewCaretChangedRedrew = true;
			RequestImmediateCaretPreviewRedraw(line);
		}
	}

	private void RequestImmediateCaretPreviewRedraw(int currentLineNumber)
	{
		TextDocument document = EditBox?.Document;
		if (document != null)
		{
			List<int> source = (from lineNumber in new int?[2] { _lastCaretPreviewRedrawLineNumber, currentLineNumber }
				where lineNumber.HasValue && lineNumber.Value >= 1 && lineNumber.Value <= document.LineCount
				select lineNumber.Value).Distinct().ToList();
			_lastCaretPreviewRedrawLineNumber = currentLineNumber;
			RequestImmediateRedrawForLines(source.Select(document.GetLineByNumber));
		}
	}

	private static bool ShouldNotifyCaretVerticalOffset(bool currentFocused, bool suppressCaretVerticalOffsetNotification, bool internalSelectionDropCaretRestorePending, bool codeBlockParagraphExitCaretPlacementPending, MouseButtonState leftButtonState)
	{
		if (currentFocused && !suppressCaretVerticalOffsetNotification && !internalSelectionDropCaretRestorePending && !codeBlockParagraphExitCaretPlacementPending)
		{
			return leftButtonState != MouseButtonState.Pressed;
		}
		return false;
	}

	private void ScheduleCaretVerticalOffsetNotificationAfterMouseUp()
	{
		if (!ShouldScheduleCaretVerticalOffsetNotificationAfterMouseUp(_suppressCaretVerticalOffsetNotificationForCurrentMouseGesture, _caretVerticalOffsetNotificationAfterMouseUpScheduled, CurrentFocused, base.Dispatcher != null))
		{
			if (_suppressCaretVerticalOffsetNotificationForCurrentMouseGesture)
			{
				base.Dispatcher?.BeginInvoke((Action)delegate
				{
					_suppressCaretVerticalOffsetNotificationForCurrentMouseGesture = false;
				}, DispatcherPriority.ContextIdle);
			}
		}
		else
		{
			_caretVerticalOffsetNotificationAfterMouseUpScheduled = true;
			base.Dispatcher.BeginInvoke((Action)delegate
			{
				_caretVerticalOffsetNotificationAfterMouseUpScheduled = false;
				NotifyCaretChanged(this, EventArgs.Empty);
			});
		}
	}

	internal static bool ShouldScheduleCaretVerticalOffsetNotificationAfterMouseUp(bool suppressForCurrentMouseGesture, bool notificationAlreadyScheduled, bool currentFocused, bool hasDispatcher)
	{
		return !suppressForCurrentMouseGesture && !notificationAlreadyScheduled && currentFocused && hasDispatcher;
	}

	public static bool IsHighlightShortcut(Key key, ModifierKeys modifiers)
	{
		if (key == Key.Y)
		{
			return modifiers == (ModifierKeys.Control | ModifierKeys.Shift);
		}
		return false;
	}

	public static bool IsSemanticUndoShortcut(Key key, ModifierKeys modifiers)
	{
		if (key == Key.Z)
		{
			return modifiers == ModifierKeys.Control;
		}
		return false;
	}

	public static bool IsSemanticRedoShortcut(Key key, ModifierKeys modifiers)
	{
		if (key != Key.Y || modifiers != ModifierKeys.Control)
		{
			if (key == Key.Z)
			{
				return modifiers == (ModifierKeys.Control | ModifierKeys.Shift);
			}
			return false;
		}
		return true;
	}

	public static bool IsCutShortcut(Key key, ModifierKeys modifiers)
	{
		if (key == Key.X)
		{
			return modifiers == ModifierKeys.Control;
		}
		return false;
	}

	public static bool IsSelectAllShortcut(Key key, ModifierKeys modifiers)
	{
		if (key == Key.A)
		{
			return modifiers == ModifierKeys.Control;
		}
		return false;
	}

	public static bool IsTextEnterKeyInput(ModifierKeys modifiers)
	{
		return (modifiers & (ModifierKeys.Alt | ModifierKeys.Control)) == 0;
	}

	public static bool IsWordDeletionShortcut(Key key, ModifierKeys modifiers)
	{
		if (key == Key.Back || key == Key.Delete)
		{
			return modifiers == ModifierKeys.Control;
		}
		return false;
	}

	public static bool IsSingleCharacterDeletionKeyInput(Key key, ModifierKeys modifiers)
	{
		switch (key)
		{
		case Key.Delete:
			return modifiers == ModifierKeys.None;
		default:
			return false;
		case Key.Back:
			if (modifiers != ModifierKeys.None)
			{
				return modifiers == ModifierKeys.Shift;
			}
			return true;
		}
	}

	private void OnEditorKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
	{
		if (IsCodeBlockAttachmentEditorInputSource(e?.OriginalSource))
		{
			return;
		}
		Key key = ((e.Key == Key.ImeProcessed) ? e.ImeProcessedKey : e.Key);
		ModifierKeys modifiers = Keyboard.Modifiers;
		if (modifiers == ModifierKeys.None && TryFocusCodeBlockAttachmentFromParentCaret(key))
		{
			e.Handled = true;
		}
		else
		{
			if (ReadOnly)
			{
				return;
			}
			if ((key == Key.Return || key == Key.Return) && IsTextEnterKeyInput(modifiers) && TryInsertParagraphBeforeCodeBlockAttachmentAtCaret())
			{
				ClearActiveInlineTypingAfterLineBreak();
				e.Handled = true;
				return;
			}
			if (key == Key.Return || key == Key.Tab)
			{
				int contentLengthAfterAppendedParagraph = ((key == Key.Tab) ? 1 : 0);
				bool appendedParagraph;
				bool blockedByMaxLength;
				bool flag = TryNormalizeCaretAfterCodeBlockAttachment(contentLengthAfterAppendedParagraph, out appendedParagraph, out blockedByMaxLength);
				if (blockedByMaxLength)
				{
					Utils.Toast(Utils.GetString("CharactersNumberLimit"));
					e.Handled = true;
					return;
				}
				if (flag && appendedParagraph && (key == Key.Return || key == Key.Return))
				{
					ClearActiveInlineTypingAfterLineBreak();
					e.Handled = true;
					return;
				}
			}
			if (IsSingleCharacterDeletionKeyInput(key, modifiers) && TrySelectCodeBlockAtCaretForDelete(key == Key.Back))
			{
				e.Handled = true;
				return;
			}
			if (IsHighlightShortcut(key, modifiers))
			{
				HighlightWithSelectedColor();
				e.Handled = true;
				return;
			}
			if (IsSelectAllShortcut(key, modifiers) && TryHandleCodeBlockSelectAllShortcut())
			{
				e.Handled = true;
				return;
			}
			if (e.Key == Key.Return || e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Tab)
			{
				NormalizeCaretIntoSemanticContentIfNeeded();
			}
			if (ShouldNormalizeLinkBoundaryBeforeKeyInput(e.Key))
			{
				TryNormalizeCaretOutsideLinkDisplayBoundary();
			}
			if (e.Key == Key.Left && TryKeepCaretInsideSemanticContent(moveToPreviousLineWhenAtStart: false))
			{
				e.Handled = true;
				return;
			}
			if (e.Key == Key.Right && TryKeepCaretBeforeSemanticBoundary())
			{
				e.Handled = true;
				return;
			}
			if (IsSemanticUndoShortcut(key, modifiers) || IsSemanticRedoShortcut(key, modifiers))
			{
				if (IsSemanticUndoShortcut(key, modifiers))
				{
					TryPerformSemanticUndo();
				}
				else
					TryPerformSemanticRedo();
				e.Handled = true;
				return;
			}
			_inImeProcess = false;
			switch (e.Key)
			{
			case Key.Up:
			case Key.Down:
				if (SelectionPopup != null && SelectionPopup.IsOpen)
				{
					e.Handled = true;
					return;
				}
				if (_styleMenuPopup != null && _styleMenuPopup.IsOpen)
				{
					e.Handled = true;
					return;
				}
				if (IsProjectPopupOpen())
				{
					e.Handled = true;
					return;
				}
				if (e.Key == Key.Up && TryMoveCaretAboveCodeBlockAtFirstContentStart())
				{
					_caretBeforeMoveUp = -1;
					e.Handled = true;
					return;
				}
				if (e.Key == Key.Down && TryMoveCaretBelowCodeBlockAtLastContentEnd())
				{
					_caretBeforeMoveUp = -1;
					e.Handled = true;
					return;
				}
				break;
			case Key.Escape:
				_projectImeRefresh.Cancel();
				if (SelectionPopup != null && SelectionPopup.IsOpen)
				{
					e.Handled = true;
					_selectionControl?.ClosePopup();
					return;
				}
				if (_styleMenuPopup != null && _styleMenuPopup.IsOpen)
				{
					e.Handled = true;
					_styleMenuPopup.IsOpen = false;
					return;
				}
				if (IsProjectPopupOpen())
				{
					TryHideProjectPopup();
					e.Handled = true;
					return;
				}
				this.EscKeyUp?.Invoke(this, null);
				break;
			case Key.Tab:
			case Key.ImeProcessed:
				if (e.Key == Key.Tab && !AllowTab && !CanHandleSemanticTabKey())
				{
					e.Handled = true;
					return;
				}
				if (AttachmentDict.ContainsKey(EditBox.CaretOffset))
				{
					ReplaceEditorText(EditBox.CaretOffset, 0, "\n", EditBox.CaretOffset);
				}
				else if (AttachmentEndSet.Contains(EditBox.CaretOffset))
				{
					ReplaceEditorText(EditBox.CaretOffset, 0, "\n", EditBox.CaretOffset + 1);
				}
				break;
			case Key.Back:
			{
				if (EditBox.SelectionLength > 0 && TryRemoveSelectedSemanticRange())
				{
					e.Handled = true;
					return;
				}
				if (TryKeepAttachmentLineBreakAtCaret(backspace: true))
				{
					e.Handled = true;
					return;
				}
				if (TrySelectAttachmentAtCaretForDelete(backspace: true))
				{
					e.Handled = true;
					return;
				}
				if (TryKeepCodeBlockStructuralBoundaryAtCaret(backspace: true))
				{
					e.Handled = true;
					return;
				}
				if (TryHandleWordDeletionShortcut(backspace: true, modifiers))
				{
					e.Handled = true;
					return;
				}
				if (!IsSingleCharacterDeletionKeyInput(Key.Back, modifiers))
				{
					break;
				}
				if (TryClearSemanticStyleStateForRepeatedEmptyDeletion())
				{
					RefreshOpenMenuContexts();
					RequestImmediateRedrawAroundCaret();
					e.Handled = true;
					return;
				}
				if (TryExitEmptyBlockQuoteLineAtCaret())
				{
					e.Handled = true;
					return;
				}
				if (TryHandleEmptyListLineDeleteAtCaret(backspace: true))
				{
					e.Handled = true;
					return;
				}
				if (TryRemoveHorizontalRuleLineAtCaret(backspace: true))
				{
					e.Handled = true;
					return;
				}
				if (TryConvertSemanticListToParagraphAtLineStart())
				{
					e.Handled = true;
					return;
				}
				if (TryConvertSemanticHeadingToParagraphAtLineStart())
				{
					e.Handled = true;
					return;
				}
				if (TryRemoveSemanticLineBreakAtCaret(backspace: true))
				{
					e.Handled = true;
					return;
				}
				if (TryRemoveEmptyCodeBlockContentLineBreakAtCaret(backspace: true))
				{
					e.Handled = true;
					return;
				}
				if (TryRemoveCodeBlockAtBoundary(backspace: true))
				{
					e.Handled = true;
					return;
				}
				if (TryRemoveEmptySemanticBlockLineAtCaret(backspace: true))
				{
					e.Handled = true;
					return;
				}
				if (TryRetreatParagraphIndentAtLineStart())
				{
					e.Handled = true;
					return;
				}
				if (TryRemoveLinkSyntaxBeforeCaretForBackspace())
				{
					e.Handled = true;
					return;
				}
				int caretOffset2 = EditBox.CaretOffset;
				if (caretOffset2 == 0)
				{
					TryRemoveLeadingEmptyLineOnDelete();
					e.Handled = true;
					return;
				}
				if (EditBox.SelectionLength > 0)
				{
					HandleSelectionBehindAttachment();
				}
				else if (caretOffset2 > 0 && caretOffset2 <= Text.Length)
				{
					DocumentLine lineByOffset2 = EditBox.TextArea.Document.GetLineByOffset(caretOffset2);
					if (lineByOffset2.Offset == EditBox.CaretOffset)
					{
						DocumentLine previousLine = lineByOffset2.PreviousLine;
						if (AttachmentDict.ContainsKey(EditBox.CaretOffset) && previousLine != null && previousLine.Length > 0)
						{
							EditBox.CaretOffset = previousLine.EndOffset;
							e.Handled = true;
						}
						else if (previousLine != null && AttachmentEndSet.Contains(previousLine.EndOffset) && lineByOffset2.Length > 0)
						{
							EditBox.CaretOffset = previousLine.EndOffset;
							e.Handled = true;
						}
					}
					else if (lineByOffset2.EndOffset == EditBox.CaretOffset && AttachmentEndSet.Contains(lineByOffset2.EndOffset))
					{
						EditBox.Select(lineByOffset2.Offset, lineByOffset2.Length);
						e.Handled = true;
					}
				}
				if (!e.Handled)
				{
					e.Handled = TryHandleDefaultCharacterDeletion(backspace: true);
				}
				break;
			}
			case Key.Return:
			{
				if (!IsTextEnterKeyInput(modifiers))
				{
					break;
				}
				if (SelectionPopup != null && SelectionPopup.IsOpen)
				{
					e.Handled = true;
					return;
				}
				if (_styleMenuPopup != null && _styleMenuPopup.IsOpen)
				{
					e.Handled = true;
					return;
				}
				if (IsProjectPopupOpen())
				{
					e.Handled = true;
					return;
				}
				_suppressNextListAdjust = true;
				if (TryHandleSemanticBlockEnter(out var lineCountChanged))
				{
					ClearActiveInlineTypingAfterLineBreak();
					if (!lineCountChanged)
					{
						_suppressNextListAdjust = false;
					}
					e.Handled = true;
					return;
				}
				_suppressNextListAdjust = false;
				string defaultEnterText = GetDefaultEnterText();
				if (TrySplitSemanticLinkAtCaretForEnter(defaultEnterText))
				{
					ClearActiveInlineTypingAfterLineBreak();
					e.Handled = true;
					return;
				}
				string defaultEnterTextForSemanticMutation = GetDefaultEnterTextForSemanticMutation(defaultEnterText);
				AdjustInlineSemanticMarksForDefaultTextMutation((EditBox.SelectionLength > 0) ? EditBox.SelectionStart : EditBox.CaretOffset, EditBox.SelectionLength, defaultEnterTextForSemanticMutation.Length);
				PreserveSemanticBlocksForDefaultTextMutation((EditBox.SelectionLength > 0) ? EditBox.SelectionStart : EditBox.CaretOffset, EditBox.SelectionLength, defaultEnterTextForSemanticMutation);
				ClearActiveInlineTypingAfterLineBreak();
				break;
			}
			case Key.Delete:
			{
				if (EditBox.SelectionLength > 0 && TryRemoveSelectedSemanticRange())
				{
					e.Handled = true;
					return;
				}
				if (TryKeepAttachmentLineBreakAtCaret(backspace: false))
				{
					e.Handled = true;
					return;
				}
				if (TrySelectAttachmentAtCaretForDelete(backspace: false))
				{
					e.Handled = true;
					return;
				}
				if (TryKeepCodeBlockStructuralBoundaryAtCaret(backspace: false))
				{
					e.Handled = true;
					return;
				}
				if (TryHandleWordDeletionShortcut(backspace: false, modifiers))
				{
					e.Handled = true;
					return;
				}
				if (!IsSingleCharacterDeletionKeyInput(Key.Delete, modifiers))
				{
					break;
				}
				if (TryClearSemanticStyleStateForRepeatedEmptyDeletion())
				{
					RefreshOpenMenuContexts();
					RequestImmediateRedrawAroundCaret();
					e.Handled = true;
					return;
				}
				if (TryExitEmptyBlockQuoteLineAtCaret())
				{
					e.Handled = true;
					return;
				}
				if (TryHandleEmptyListLineDeleteAtCaret(backspace: false))
				{
					e.Handled = true;
					return;
				}
				if (TryRemoveHorizontalRuleLineAtCaret(backspace: false))
				{
					e.Handled = true;
					return;
				}
				if (TryRemoveSemanticLineBreakAtCaret(backspace: false))
				{
					e.Handled = true;
					return;
				}
				if (TryRemoveEmptyCodeBlockContentLineBreakAtCaret(backspace: false))
				{
					e.Handled = true;
					return;
				}
				if (TryRemoveCodeBlockAtBoundary(backspace: false))
				{
					e.Handled = true;
					return;
				}
				if (TryRemoveEmptySemanticBlockLineAtCaret(backspace: false))
				{
					e.Handled = true;
					return;
				}
				int caretOffset = EditBox.CaretOffset;
				if (EditBox.SelectionLength > 0)
				{
					HandleSelectionBehindAttachment();
				}
				else if (caretOffset >= 0 && caretOffset < Text.Length)
				{
					DocumentLine lineByOffset = EditBox.TextArea.Document.GetLineByOffset(caretOffset);
					if (lineByOffset.EndOffset == EditBox.CaretOffset)
					{
						DocumentLine nextLine = lineByOffset.NextLine;
						if (nextLine != null && AttachmentDict.ContainsKey(nextLine.Offset) && lineByOffset.Length > 0)
						{
							EditBox.CaretOffset = nextLine.Offset;
							e.Handled = true;
						}
						else if (AttachmentEndSet.Contains(caretOffset) && nextLine != null && nextLine.Length > 0)
						{
							EditBox.CaretOffset = nextLine.Offset;
							e.Handled = true;
						}
					}
					else if (lineByOffset.Offset == EditBox.CaretOffset && AttachmentEndSet.Contains(lineByOffset.EndOffset))
					{
						EditBox.Select(lineByOffset.Offset, lineByOffset.Length);
						e.Handled = true;
					}
				}
				if (!e.Handled)
				{
					e.Handled = TryHandleDefaultCharacterDeletion(backspace: false);
				}
				break;
			}
			case Key.X:
				if (IsCutShortcut(key, modifiers) && EditBox.SelectionLength > 0)
				{
					TryCutSelectedSemanticRange();
					e.Handled = true;
					return;
				}
				break;
			}
			if (EditBox.SelectionLength > 0 && !Utils.IfShiftPressed())
			{
				switch (e.Key)
				{
				case Key.Left:
				case Key.Up:
					EditBox.SelectionLength = 0;
					e.Handled = true;
					break;
				case Key.Right:
				case Key.Down:
				{
					int selectionStart = EditBox.SelectionStart + EditBox.SelectionLength;
					EditBox.SelectionLength = 0;
					EditBox.SelectionStart = selectionStart;
					e.Handled = true;
					break;
				}
				}
				_caretBeforeMoveUp = -1;
			}
			else if (_caretBeforeMoveUp != -1 && (e.Key == Key.Up || e.Key == Key.Down))
			{
				_caretBeforeMoveUp = EditBox.CaretOffset;
			}
			_inImeProcess = e.ImeProcessedKey != Key.None;
			if (e.Key == Key.Tab)
			{
				if (string.IsNullOrEmpty(EditBox.SelectedText))
				{
					DocumentLine lineByOffset3 = EditBox.Document.GetLineByOffset(EditBox.CaretOffset);
					int lineIndex = Math.Max(0, lineByOffset3.LineNumber - 1);
					if (TryGetSemanticBlockAtLine(lineIndex, out var block) && ((block != null && block.Kind == MarkdownBlockKind.List) || (block != null && block.Kind == MarkdownBlockKind.BlockQuote) || (block != null && block.Kind == MarkdownBlockKind.Paragraph) || (block != null && block.Kind == MarkdownBlockKind.CodeBlock)))
					{
						if (block != null && block.Kind == MarkdownBlockKind.CodeBlock)
						{
							if (IsCodeBlockAttachment(block))
							{
								if ((Keyboard.Modifiers & ModifierKeys.Shift) == 0)
								{
									TryInsertTabBeforeCodeBlockAttachmentAtCaret();
								}
							}
							else if ((Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None)
							{
								DeIndent();
							}
							else
							{
								InsertText("    ");
							}
						}
						else if (block != null && block.Kind == MarkdownBlockKind.Paragraph && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
						{
							TryInsertLiteralTabIntoParagraph();
						}
						else if ((Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None)
						{
							OutIndentLine();
						}
						else
						{
							IndentLine();
						}
						e.Handled = true;
					}
				}
				else if (TryHandleCodeBlockSelectionTab((Keyboard.Modifiers & ModifierKeys.Shift) != 0))
				{
					e.Handled = true;
				}
				else if ((Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None)
				{
					DeIndent();
					e.Handled = true;
				}
				else
				{
					Indent();
					e.Handled = true;
				}
			}
			if (Keyboard.Modifiers == ModifierKeys.Control)
			{
				if (e.Key == Key.I)
				{
					Italic();
					e.Handled = true;
				}
				if (LocalSettings.Settings.ShortCutModel.SetDate == "Ctrl+D" && e.Key == Key.D)
				{
					e.Handled = true;
					this.SelectDate?.Invoke(this, null);
				}
			}
			if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
			{
				Window window = Window.GetWindow(this);
				char? c = ((window != null) ? KeyboardHelper.GetCharFromWpfKey(e, window) : ((char?)null));
				if (c.HasValue && KeyboardHelper.IsLikelyAltGrInput(c.Value))
				{
					return;
				}
				switch (e.Key)
				{
				case Key.D1:
				case Key.NumPad1:
					InsertHeader(1);
					e.Handled = true;
					break;
				case Key.D2:
				case Key.NumPad2:
					InsertHeader(2);
					e.Handled = true;
					break;
				case Key.D3:
				case Key.NumPad3:
					InsertHeader(3);
					e.Handled = true;
					break;
				case Key.D4:
				case Key.NumPad4:
					InsertHeader(4);
					e.Handled = true;
					break;
				case Key.D5:
				case Key.NumPad5:
					InsertHeader(5);
					e.Handled = true;
					break;
				case Key.D6:
				case Key.NumPad6:
					InsertHeader(6);
					e.Handled = true;
					break;
				case Key.D0:
				case Key.NumPad0:
					EnterImmersiveMode();
					e.Handled = true;
					break;
				case Key.D:
					ToggleCheckItem();
					e.Handled = true;
					break;
				case Key.Oem3:
					Code();
					e.Handled = true;
					break;
				}
			}
			this.KeyDown?.Invoke(this, e);
		}
	}

	private bool TryHandleDefaultCharacterDeletion(bool backspace)
	{
		Selection deletionSelection;
		ISegment[] characterDeletionSegments = EditBox.TextArea.GetCharacterDeletionSegments(backspace, out deletionSelection);
		if (characterDeletionSegments.Length == 0)
		{
			return false;
		}
		if (backspace && characterDeletionSegments.Length == 1 && !(EditBox.TextArea.Selection is RectangleSelection))
		{
			CaptureBackspaceRetainedTypingMarks(characterDeletionSegments[0].Offset, characterDeletionSegments[0].Length);
		}
		else
		{
			ClearBackspaceRetainedTypingMarks();
		}
		DocumentMutation[] mutations = characterDeletionSegments.Select((ISegment segment) => new DocumentMutation
		{
			StartOffset = segment.Offset,
			RemoveLength = segment.Length,
			InsertText = string.Empty
		}).ToArray();
		bool flag = ClearHeadingFoldingsBeforeDocumentMutationsIfNeeded(mutations);
		foreach (ISegment item in characterDeletionSegments.OrderByDescending((ISegment segment) => segment.Offset))
		{
			_needsTypedSemanticMarkRedraw |= AdjustTypedSemanticMarksForReplace(item.Offset, item.Length, 0);
			AdjustSemanticMarkListForReplace(item.Offset, item.Length, 0);
		}
		PreserveSemanticBlocksAcrossMutations(mutations);
		if (flag)
		{
			deletionSelection.ReplaceSelectionWithText(string.Empty);
			EditBox.TextArea.Caret.BringCaretToView();
			return true;
		}
		return false;
	}

	private bool TryHandleWordDeletionShortcut(bool backspace, ModifierKeys modifiers)
	{
		if (!IsWordDeletionShortcut(backspace ? Key.Back : Key.Delete, modifiers) || EditBox?.Document == null || EditBox.SelectionLength > 0)
		{
			return false;
		}
		TextDocument document = EditBox.Document;
		int num = Math.Max(0, Math.Min(EditBox.CaretOffset, document.TextLength));
		int num2 = TextUtilities.GetNextCaretPosition(document, num, (!backspace) ? LogicalDirection.Forward : LogicalDirection.Backward, CaretPositioningMode.WordStart);
		if (num2 < 0)
		{
			num2 = ((!backspace) ? document.TextLength : 0);
		}
		num2 = Math.Max(0, Math.Min(num2, document.TextLength));
		int num3 = Math.Min(num, num2);
		int num4 = Math.Max(num, num2);
		int num5 = num4 - num3;
		if (num5 <= 0)
		{
			return true;
		}
		if (IsProtectedSemanticStructureDeletionRange(num3, num4))
		{
			return true;
		}
		ClearBackspaceRetainedTypingMarks();
		ApplyDocumentMutationsAndNotify(new DocumentMutation[1]
		{
			new DocumentMutation
			{
				StartOffset = num3,
				RemoveLength = num5,
				InsertText = string.Empty
			}
		}, num3);
		return true;
	}

	private bool IsProtectedSemanticStructureDeletionRange(int startOffset, int endOffset)
	{
		if (EditBox?.Document == null || endOffset <= startOffset)
		{
			return false;
		}
		TextDocument document = EditBox.Document;
		int textLength = document.TextLength;
		int num = Math.Max(0, Math.Min(startOffset, textLength));
		int num2 = Math.Max(num, Math.Min(endOffset, textLength));
		if (num2 <= num)
		{
			return false;
		}
		DocumentLine lineByOffset = document.GetLineByOffset(num);
		int offset = Math.Max(num, Math.Min(num2, textLength));
		DocumentLine lineByOffset2 = document.GetLineByOffset(offset);
		if (lineByOffset == null || lineByOffset2 == null)
		{
			return false;
		}
		for (int i = lineByOffset.LineNumber; i <= lineByOffset2.LineNumber; i++)
		{
			DocumentLine lineByNumber = document.GetLineByNumber(i);
			if (lineByNumber == null || num >= lineByNumber.EndOffset + lineByNumber.DelimiterLength || num2 < lineByNumber.Offset)
			{
				continue;
			}
			if (AttachmentDict.ContainsKey(lineByNumber.Offset) || AttachmentEndSet.Contains(lineByNumber.EndOffset))
			{
				return true;
			}
			int lineIndex = Math.Max(0, lineByNumber.LineNumber - 1);
			if (TryGetSemanticBlockAtLine(lineIndex, out var block) && block != null && block.Kind == MarkdownBlockKind.CodeBlock)
			{
				string text = document.GetText(lineByNumber);
				if (IsSemanticCodeBlockOpeningFenceLine(text, block, lineIndex) || IsSemanticCodeBlockClosingFenceLine(text, block, lineIndex))
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool TryHandleCodeBlockSelectAllShortcut()
	{
		if (EditBox?.Document == null)
		{
			return false;
		}
		int textLength = EditBox.Document.TextLength;
		int num = Math.Max(0, Math.Min(EditBox.SelectionStart, textLength));
		int num2 = Math.Max(num, Math.Min(num + Math.Max(0, EditBox.SelectionLength), textLength));
		if (!TryGetCodeBlockForSelectAllShortcut(num, num2, out var block) || !TryGetCodeBlockContentRange(block, out var contentStartOffset, out var contentEndOffset))
		{
			return false;
		}
		bool num3 = EditBox.SelectionLength > 0 && num == contentStartOffset && num2 == contentEndOffset;
		ClearObjectSelectedCodeBlocks();
		if (num3)
		{
			EditBox.Select(0, textLength);
			return true;
		}
		EditBox.Select(contentStartOffset, Math.Max(0, contentEndOffset - contentStartOffset));
		return true;
	}

	private bool TryGetCodeBlockForSelectAllShortcut(int selectionStart, int selectionEnd, out MarkdownBlockNode block)
	{
		block = null;
		if (EditBox?.Document == null)
		{
			return false;
		}
		if (selectionEnd > selectionStart)
		{
			if (!TryGetCodeBlockAtSelectionOffset(selectionStart, treatAsSelectionEnd: false, out var block2) || !TryGetCodeBlockAtSelectionOffset(selectionEnd, treatAsSelectionEnd: true, out var block3) || !string.Equals(GetCodeBlockSelectionKey(block2), GetCodeBlockSelectionKey(block3), StringComparison.Ordinal))
			{
				return false;
			}
			block = block2;
			return true;
		}
		int offset = Math.Max(0, Math.Min(EditBox.CaretOffset, EditBox.Document.TextLength));
		return TryGetCodeBlockAtSelectionOffset(offset, treatAsSelectionEnd: false, out block);
	}

	private bool TryMoveCaretAboveCodeBlockAtFirstContentStart()
	{
		if (EditBox?.Document == null || EditBox.SelectionLength > 0)
		{
			return false;
		}
		TextDocument document = EditBox.Document;
		int num = Math.Max(0, Math.Min(EditBox.CaretOffset, document.TextLength));
		DocumentLine lineByOffset = document.GetLineByOffset(num);
		if (lineByOffset == null)
		{
			return false;
		}
		int num2 = Math.Max(0, lineByOffset.LineNumber - 1);
		if (!TryGetSemanticBlockAtLine(num2, out var block) || block == null || block.Kind != MarkdownBlockKind.CodeBlock)
		{
			return false;
		}
		if (!IsSemanticCodeBlockContentLine(document.GetText(lineByOffset), block, num2) || !TryGetCodeBlockEditableOffset(lineByOffset, num2, block, out var targetOffset) || num != targetOffset || num2 != block.StartLine + 1 || block.StartLine <= 0)
		{
			return false;
		}
		DocumentLine lineByNumber = document.GetLineByNumber(block.StartLine);
		if (lineByNumber == null)
		{
			return false;
		}
		EditBox.SelectionLength = 0;
		EditBox.CaretOffset = lineByNumber.EndOffset;
		NormalizeCaretIntoSemanticContentIfNeeded();
		return EditBox.CaretOffset != num;
	}

	private bool TryMoveCaretBelowCodeBlockAtLastContentEnd()
	{
		if (EditBox?.Document == null || EditBox.SelectionLength > 0)
		{
			return false;
		}
		TextDocument document = EditBox.Document;
		int num = Math.Max(0, Math.Min(EditBox.CaretOffset, document.TextLength));
		DocumentLine lineByOffset = document.GetLineByOffset(num);
		if (lineByOffset == null)
		{
			return false;
		}
		int num2 = Math.Max(0, lineByOffset.LineNumber - 1);
		if (!TryGetSemanticBlockAtLine(num2, out var block) || block == null || block.Kind != MarkdownBlockKind.CodeBlock)
		{
			return false;
		}
		if (!IsSemanticCodeBlockContentLine(document.GetText(lineByOffset), block, num2) || num != lineByOffset.EndOffset || num2 != block.EndLine - 1 || block.EndLine > document.LineCount - 1)
		{
			return false;
		}
		if (block.EndLine == document.LineCount - 1)
		{
			return TryAppendCodeBlockExitLineAtDocumentEnd(num);
		}
		DocumentLine lineByNumber = document.GetLineByNumber(block.EndLine + 2);
		if (lineByNumber == null)
		{
			return false;
		}
		EditBox.SelectionLength = 0;
		EditBox.CaretOffset = lineByNumber.Offset;
		NormalizeCaretIntoSemanticContentIfNeeded();
		return EditBox.CaretOffset != num;
	}

	private bool TryAppendCodeBlockExitLineAtDocumentEnd(int previousCaretOffset)
	{
		TextDocument textDocument = EditBox?.Document;
		if (ReadOnly || textDocument == null || WouldExceedMaxLengthForContentInsertion(1))
		{
			return false;
		}
		int textLength = textDocument.TextLength;
		using (textDocument.RunUpdate())
		{
			textDocument.Insert(textLength, "\n");
		}
		SetSemanticParagraphForLine(Math.Max(0, textDocument.LineCount - 1), 0);
		if (_semanticDocument != null)
		{
			_semanticBlocksByLine = BuildSemanticBlockIndex(_semanticDocument);
		}
		EditBox.SelectionLength = 0;
		EditBox.CaretOffset = textDocument.TextLength;
		NormalizeCaretIntoSemanticContentIfNeeded();
		return EditBox.CaretOffset != previousCaretOffset;
	}

	private void HandleSelectionBehindAttachment()
	{
		if (AttachmentEndSet.Contains(EditBox.SelectionStart))
		{
			DocumentLine nextLine = EditBox.TextArea.Document.GetLineByOffset(EditBox.SelectionStart).NextLine;
			if (nextLine != null)
			{
				if (EditBox.CaretOffset == EditBox.SelectionStart)
				{
					EditBox.CaretOffset = nextLine.Offset;
				}
				int num = nextLine.Offset - EditBox.SelectionStart;
				EditBox.SelectionLength = Math.Max(0, EditBox.SelectionLength - num);
				EditBox.SelectionStart = nextLine.Offset;
			}
		}
		else
		{
			if (!AttachmentDict.ContainsKey(EditBox.SelectionStart + EditBox.SelectionLength))
			{
				return;
			}
			int num2 = EditBox.SelectionStart + EditBox.SelectionLength;
			DocumentLine previousLine = EditBox.TextArea.Document.GetLineByOffset(num2).PreviousLine;
			if (previousLine != null)
			{
				if (EditBox.CaretOffset == num2)
				{
					EditBox.CaretOffset = previousLine.EndOffset;
				}
				int val = previousLine.EndOffset - EditBox.SelectionStart;
				EditBox.SelectionLength = Math.Max(0, val);
			}
		}
	}

	private bool TrySelectAttachmentAtCaretForDelete(bool backspace)
	{
		if (EditBox?.Document == null || EditBox.SelectionLength > 0)
		{
			return false;
		}
		TextDocument document = EditBox.Document;
		int caretOffset = Math.Max(0, Math.Min(EditBox.CaretOffset, document.TextLength));
		if (!TryGetAttachmentLineAtBoundary(caretOffset, backspace, out var attachmentLine))
		{
			return false;
		}
		EditBox.Select(attachmentLine.Offset, attachmentLine.Length);
		return true;
	}

	private bool TryGetAttachmentLineAtBoundary(int caretOffset, bool backspace, out DocumentLine attachmentLine)
	{
		attachmentLine = null;
		if (EditBox?.Document == null)
		{
			return false;
		}
		TextDocument document = EditBox.Document;
		caretOffset = Math.Max(0, Math.Min(caretOffset, document.TextLength));
		DocumentLine lineByOffset = document.GetLineByOffset(caretOffset);
		if (lineByOffset == null)
		{
			return false;
		}
		if (backspace)
		{
			if (caretOffset != lineByOffset.EndOffset || !AttachmentEndSet.Contains(lineByOffset.EndOffset))
			{
				return false;
			}
			attachmentLine = lineByOffset;
			return true;
		}
		if (caretOffset != lineByOffset.Offset || !AttachmentDict.ContainsKey(lineByOffset.Offset))
		{
			return false;
		}
		attachmentLine = lineByOffset;
		return true;
	}

	private bool TryKeepAttachmentLineBreakAtCaret(bool backspace)
	{
		if (EditBox?.Document == null || EditBox.SelectionLength > 0)
		{
			return false;
		}
		TextDocument document = EditBox.Document;
		int num = Math.Max(0, Math.Min(EditBox.CaretOffset, document.TextLength));
		DocumentLine lineByOffset = document.GetLineByOffset(num);
		if (lineByOffset == null)
		{
			return false;
		}
		if (backspace)
		{
			if (num != lineByOffset.Offset)
			{
				return false;
			}
			DocumentLine previousLine = lineByOffset.PreviousLine;
			if (AttachmentDict.ContainsKey(num) && previousLine != null && previousLine.Length > 0)
			{
				EditBox.SelectionLength = 0;
				EditBox.CaretOffset = previousLine.EndOffset;
				return true;
			}
			if (previousLine != null && AttachmentEndSet.Contains(previousLine.EndOffset) && lineByOffset.Length > 0)
			{
				EditBox.SelectionLength = 0;
				EditBox.CaretOffset = previousLine.EndOffset;
				return true;
			}
			return false;
		}
		if (num != lineByOffset.EndOffset)
		{
			return false;
		}
		DocumentLine nextLine = lineByOffset.NextLine;
		if (nextLine != null && AttachmentDict.ContainsKey(nextLine.Offset) && lineByOffset.Length > 0)
		{
			EditBox.SelectionLength = 0;
			EditBox.CaretOffset = nextLine.Offset;
			return true;
		}
		if (AttachmentEndSet.Contains(num) && nextLine != null && nextLine.Length > 0)
		{
			EditBox.SelectionLength = 0;
			EditBox.CaretOffset = nextLine.Offset;
			return true;
		}
		return false;
	}

	private int GetRemoveLinkLength(int caret)
	{
		if (LinkUrlDict.Any())
		{
			int num = EditBox.Text.LastIndexOf("[", caret, StringComparison.Ordinal);
			if (num >= 0)
			{
				string link = EditBox.Text.Substring(num, caret - num);
				if (LinkNameDict.Select((KeyValuePair<int, LinkInfo> info) => "[" + info.Value.Link + "](" + info.Value.Url + ")").Any((string content) => link == content))
				{
					return link.Length;
				}
			}
		}
		return -1;
	}

	private bool TryRemoveLinkSyntaxBeforeCaretForBackspace()
	{
		if (EditBox?.Document == null || EditBox.SelectionLength > 0)
		{
			return false;
		}
		int num = Math.Max(0, Math.Min(EditBox.CaretOffset, EditBox.Document.TextLength));
		int removeLinkLength = GetRemoveLinkLength(num);
		if (removeLinkLength <= 0)
		{
			return false;
		}
		int num2 = Math.Max(0, num - removeLinkLength);
		ReplaceEditorText(num2, removeLinkLength, string.Empty, num2);
		return true;
	}

	private static string[] GetElementGeneratorRegistrationOrder()
	{
		return ElementGeneratorRegistrations.Select((ElementGeneratorRegistration registration) => registration.Name).ToArray();
	}

	private void SetupGenerators()
	{
		ElementGeneratorRegistration[] elementGeneratorRegistrations = ElementGeneratorRegistrations;
		for (int i = 0; i < elementGeneratorRegistrations.Length; i++)
		{
			VisualLineElementGenerator visualLineElementGenerator = elementGeneratorRegistrations[i].Create(this);
			if (visualLineElementGenerator is CodeBlockAttachmentElementGenerator codeBlockAttachmentElementGenerator)
			{
				_codeBlockAttachmentElementGenerator = codeBlockAttachmentElementGenerator;
			}
			else if (visualLineElementGenerator is ImageElementGenerator imageElementGenerator)
			{
				_imageElementGenerator = imageElementGenerator;
			}
			EditBox.TextArea.TextView.ElementGenerators.Add(visualLineElementGenerator);
		}
		_codeBlockSelectionLayer = new CodeBlockSelectionLayer(this, EditBox.TextArea.TextView);
		EditBox.TextArea.TextView.InsertLayer(_codeBlockSelectionLayer, KnownLayer.Selection, LayerInsertionPosition.Above);
	}

	private void AddLinkBracketElementGenerator()
	{
		EditBox.TextArea.TextView.ElementGenerators.Add(new HideLinkBracketGenerator(this, LinkNameDict, isLeft: true));
		EditBox.TextArea.TextView.ElementGenerators.Add(new HideLinkBracketGenerator(this, LinkNameDict, isLeft: false));
	}

	public void Bold()
	{
		ToggleInlineMark(MarkdownMarkKind.Strong);
	}

	public void Italic()
	{
		ToggleInlineMark(MarkdownMarkKind.Emphasis);
	}

	public void Code()
	{
		ToggleInlineMark(MarkdownMarkKind.Code);
	}

	public void UnderLine()
	{
		ToggleInlineMark(MarkdownMarkKind.Underline);
	}

	public void StrokeLine()
	{
		ToggleInlineMark(MarkdownMarkKind.Strikethrough);
	}

	public void AddText(string param, bool keepCurrentLine = false, bool useLastCaretWhenUnfocused = false)
	{
		if (param == null)
		{
			return;
		}
		if (!_currentFocused)
		{
			if (useLastCaretWhenUnfocused)
			{
				InsertEditorTextAtLastUnfocusedCaret(param, keepCurrentLine);
				FocusEditBox();
				return;
			}
			string text = (string.IsNullOrEmpty(EditBox.Text) ? param : ("\r\n" + param));
			EditBox.CaretOffset = EditBox.Text.Length;
			TryExpandFoldedHeadingAtInsertionOffset(EditBox.CaretOffset);
			InsertEditorText(text);
			FocusEditBox();
		}
		else
		{
			TryExpandFoldedHeadingAtInsertionOffset(EditBox.CaretOffset);
			InsertEditorText(param);
			FocusEditBox();
		}
	}

	public void InsertTextAtCurrentCaretAndFocus(string text)
	{
		if (!string.IsNullOrEmpty(text) && EditBox?.Document != null)
		{
			int textLength = EditBox.Document.TextLength;
			bool flag = EditBox.SelectionLength > 0;
			bool num = !flag && !_currentFocused && _lastInsertSelectionLength > 0;
			int num2 = EditBox.CaretOffset;
			int removedLength = 0;
			if (num)
			{
				num2 = Math.Max(0, Math.Min(_lastInsertOffset, textLength));
				removedLength = Math.Max(0, Math.Min(_lastInsertSelectionLength, textLength - num2));
			}
			else if (flag)
			{
				num2 = EditBox.SelectionStart;
				removedLength = EditBox.SelectionLength;
			}
			TryExpandFoldedHeadingAtInsertionOffset(num2);
			List<MarkdownInlineDisplaySegment> typingMarksForInsertion = GetTypingMarksForInsertion(num2, removedLength);
			InsertTextAt(num2, removedLength, text, typingMarksForInsertion);
			FocusEditBox();
		}
	}

	public void InsertEditorText(string text)
	{
		if (!string.IsNullOrEmpty(text))
		{
			int startOffset = ((EditBox.SelectionLength > 0) ? EditBox.SelectionStart : EditBox.CaretOffset);
			int selectionLength = EditBox.SelectionLength;
			List<MarkdownInlineDisplaySegment> typingMarksForInsertion = GetTypingMarksForInsertion(startOffset, selectionLength);
			InsertText(text, typingMarksForInsertion);
		}
	}

	public void InsertPersistedEditorText(string text)
	{
		if (!string.IsNullOrEmpty(text) && EditBox?.Document != null)
		{
			int num = ((EditBox.SelectionLength > 0) ? EditBox.SelectionStart : EditBox.CaretOffset);
			int selectionLength = EditBox.SelectionLength;
			TryExpandFoldedHeadingAtInsertionOffset(num);
			if (selectionLength == 0)
			{
				num = GetInsertionOffsetOutsideLinkDisplayBoundary(num);
			}
			PreparedEditorTextState prepared = PrepareEditorTextState(text);
			InsertPreparedEditorText(prepared, num, selectionLength);
		}
	}

	private void InsertEditorTextAtLastUnfocusedCaret(string text, bool keepCurrentLine)
	{
		if (!string.IsNullOrEmpty(text) && EditBox?.Document != null)
		{
			int textLength = EditBox.Document.TextLength;
			int num = Math.Max(0, Math.Min(_lastInsertOffset, textLength));
			int num2 = Math.Max(0, Math.Min(_lastInsertSelectionLength, textLength - num));
			TryExpandFoldedHeadingAtInsertionOffset(num);
			if (num2 == 0)
			{
				num = GetInsertionOffsetOutsideLinkDisplayBoundary(num);
			}
			string text2 = text;
			if (num2 == 0 && !keepCurrentLine && ShouldPrefixLineBreakForUnfocusedInsertion(num))
			{
				text2 = "\r\n" + text2;
			}
			List<MarkdownInlineDisplaySegment> typingMarksForInsertion = GetTypingMarksForInsertion(num, num2);
			InsertTextAt(num, num2, text2, typingMarksForInsertion);
		}
	}

	private bool ShouldPrefixLineBreakForUnfocusedInsertion(int startOffset)
	{
		if (EditBox?.Document == null || EditBox.Document.TextLength == 0)
		{
			return false;
		}
		int offset = Math.Min(startOffset, EditBox.Document.TextLength - 1);
		DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(offset);
		if (lineByOffset != null)
		{
			return !string.IsNullOrEmpty(EditBox.Document.GetText(lineByOffset.Offset, lineByOffset.Length));
		}
		return false;
	}

	private void InsertPreparedEditorText(PreparedEditorTextState prepared, int startOffset, int removedLength, bool convertCompletedMarkdownLink = true)
	{
		prepared = prepared ?? new PreparedEditorTextState();
		EnsurePreparedCodeBlockAttachmentsOccupyOwnLines(prepared, startOffset, removedLength);
		string text = prepared.DisplayText ?? string.Empty;
		bool keepCurrentBlockStyleForFirstLine = ShouldKeepCurrentBlockStyleForPastedFirstLine(startOffset, removedLength, prepared);
		ReplaceEditorText(startOffset, removedLength, text, startOffset + text.Length, delegate(int insertedOffset, int insertedLength)
		{
			ApplyPreparedSemanticBlocks(prepared, insertedOffset, insertedLength, keepCurrentBlockStyleForFirstLine);
			if (prepared.SemanticMarks == null || prepared.SemanticMarks.Count == 0)
			{
				return;
			}
			foreach (MarkdownInlineDisplaySegment item in prepared.SemanticMarks.Where((MarkdownInlineDisplaySegment mark) => mark != null && mark.SourceLength > 0))
			{
				AddTypedSemanticMarkFromSegment(item, insertedOffset + item.SourcePosition, item.SourceLength);
			}
		}, deferTextChangedNotification: false, convertCompletedMarkdownLink);
	}

	private void EnsurePreparedCodeBlockAttachmentsOccupyOwnLines(PreparedEditorTextState prepared, int insertionOffset, int removedLength)
	{
		if (prepared == null || prepared.SemanticDocument?.Blocks?.Any((MarkdownBlockNode block) => block != null && block.Kind == MarkdownBlockKind.CodeBlock) != true || string.IsNullOrEmpty(prepared.DisplayText) || EditBox?.Document == null)
		{
			return;
		}
		string displayText = prepared.DisplayText;
		string value = '\ue000'.ToString();
		if (displayText.IndexOf('\ue000') < 0)
		{
			return;
		}
		TextDocument document = EditBox.Document;
		int num = Math.Max(0, Math.Min(insertionOffset, document.TextLength));
		int num2 = Math.Max(0, Math.Min(removedLength, document.TextLength - num));
		DocumentLine lineByOffset = document.GetLineByOffset(num);
		int num3 = num + num2;
		DocumentLine lineByOffset2 = document.GetLineByOffset(num3);
		if (lineByOffset == null || lineByOffset2 == null)
		{
			return;
		}
		bool flag = displayText.StartsWith(value, StringComparison.Ordinal) && num > lineByOffset.Offset;
		bool flag2 = displayText.EndsWith(value, StringComparison.Ordinal) && num3 < lineByOffset2.EndOffset;
		if (flag || flag2)
		{
			if (flag)
			{
				prepared.DisplayText = "\n" + displayText;
				ShiftPreparedEditorTextStateForLeadingLineBreak(prepared);
				displayText = prepared.DisplayText;
			}
			if (flag2)
			{
				prepared.DisplayText = displayText + "\n";
			}
		}
	}

	private static void ShiftPreparedEditorTextStateForLeadingLineBreak(PreparedEditorTextState prepared)
	{
		IEnumerable<MarkdownBlockNode> enumerable = prepared.SemanticDocument?.Blocks;
		foreach (MarkdownBlockNode item in enumerable ?? Enumerable.Empty<MarkdownBlockNode>())
		{
			if (item != null)
			{
				item.StartLine++;
				item.EndLine++;
			}
		}
		foreach (MarkdownInlineDisplaySegment item2 in prepared.SemanticMarks.Where((MarkdownInlineDisplaySegment mark) => mark != null))
		{
			item2.SourcePosition++;
			item2.UrlPosition++;
		}
		if (prepared.RawToDisplayOffsetMap == null)
		{
			return;
		}
		for (int num = 0; num < prepared.RawToDisplayOffsetMap.Length; num++)
		{
			if (prepared.RawToDisplayOffsetMap[num] >= 0)
			{
				prepared.RawToDisplayOffsetMap[num]++;
			}
		}
	}

	private bool ShouldKeepCurrentBlockStyleForPastedFirstLine(int startOffset, int removedLength, PreparedEditorTextState prepared)
	{
		string value = prepared?.DisplayText ?? string.Empty;
		if (removedLength > 0 || string.IsNullOrEmpty(value) || EditBox?.Document == null)
		{
			return false;
		}
		int num = Math.Max(0, Math.Min(startOffset, EditBox.Document.TextLength));
		DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(num);
		if (lineByOffset == null)
		{
			return false;
		}
		if (!IsEffectivelyEmptySemanticLine(EditBox.Document.GetText(lineByOffset.Offset, lineByOffset.Length)))
		{
			return SelectionEndsAtSemanticLineContentEnd(lineByOffset, num);
		}
		int lineIndex = Math.Max(0, lineByOffset.LineNumber - 1);
		if (IsInlineOnlyPreparedPaste(prepared) && TryGetSemanticBlockAtLine(lineIndex, out var block) && IsTextBearingSemanticBlock(block))
		{
			return SelectionStartsAtSemanticLineContentStart(lineByOffset, num);
		}
		return false;
	}

	private static bool IsInlineOnlyPreparedPaste(PreparedEditorTextState prepared)
	{
		if (prepared?.SemanticDocument?.Blocks == null || prepared.SemanticDocument.Blocks.Count == 0)
		{
			return false;
		}
		return prepared.SemanticDocument.Blocks.All((MarkdownBlockNode block) => block != null && block.Kind == MarkdownBlockKind.Paragraph && !block.Level.HasValue && !block.ListKind.HasValue && !block.Checked.HasValue && Math.Max(0, block.Indent) == 0 && block.SoftTab != true);
	}

	private void ApplyPreparedSemanticBlocks(PreparedEditorTextState prepared, int insertedOffset, int insertedLength, bool keepCurrentBlockStyleForFirstLine = false)
	{
		if (prepared?.SemanticDocument?.Blocks == null || EditBox?.Document == null)
		{
			return;
		}
		DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(Math.Max(0, Math.Min(insertedOffset, EditBox.Document.TextLength)));
		int val = ((insertedLength > 0) ? (insertedOffset + insertedLength - 1) : insertedOffset);
		DocumentLine lineByOffset2 = EditBox.Document.GetLineByOffset(Math.Max(0, Math.Min(val, EditBox.Document.TextLength)));
		if (lineByOffset == null || lineByOffset2 == null)
		{
			return;
		}
		int num = Math.Max(0, lineByOffset.LineNumber - 1);
		int num2 = Math.Max(num, lineByOffset2.LineNumber - 1);
		for (int i = (keepCurrentBlockStyleForFirstLine ? (num + 1) : num); i <= num2; i++)
		{
			SetSemanticParagraphForLine(i);
		}
		foreach (MarkdownBlockNode item in prepared.SemanticDocument.Blocks.Where((MarkdownBlockNode block) => block != null))
		{
			int num3 = num + Math.Max(0, item.StartLine);
			int num4 = Math.Min(num2, num + Math.Max(item.StartLine, item.EndLine));
			if (keepCurrentBlockStyleForFirstLine && num3 <= num)
			{
				num3 = num + 1;
				if (num4 < num3)
				{
					continue;
				}
			}
			if (item.Kind == MarkdownBlockKind.CodeBlock)
			{
				SetSemanticCodeBlockForLineRange(num3, num4, item.Language, item.CodeText, item.Indent);
				continue;
			}
			if (item.Kind == MarkdownBlockKind.FileBlock)
			{
				SetSemanticFileBlockForLine(num3, item);
				continue;
			}
			for (int num5 = num3; num5 <= num4; num5++)
			{
				switch (item.Kind)
				{
				case MarkdownBlockKind.Heading:
					SetSemanticHeadingLevelForLine(num5, item.Level);
					break;
				case MarkdownBlockKind.BlockQuote:
					SetSemanticBlockQuoteForLine(num5, enabled: true, item.Indent);
					break;
				case MarkdownBlockKind.HorizontalRule:
					SetSemanticHorizontalRuleForLine(num5, item.Indent);
					break;
				case MarkdownBlockKind.List:
					switch (item.ListKind)
					{
					case MarkdownListKind.Bullet:
						SetSemanticBulletListForLine(num5, enabled: true, item.Indent);
						break;
					case MarkdownListKind.Task:
						SetSemanticTaskListForLine(num5, enabled: true, item.Checked == true, item.Indent);
						break;
					case MarkdownListKind.Numbered:
						SetSemanticOrderedListForLine(num5, enabled: true, string.IsNullOrEmpty(item.CustomOrder) ? "1" : item.CustomOrder, item.Indent);
						break;
					}
					break;
				}
			}
		}
	}

	private void SetSemanticParagraphForLine(int lineIndex, int? indent = null)
	{
		if (lineIndex >= 0)
		{
			_semanticDocument = _semanticDocument ?? new MarkdownSemanticDocument();
			MarkdownBlockNode markdownBlockNode = _semanticDocument.Blocks.FirstOrDefault((MarkdownBlockNode block) => block != null && block.StartLine <= lineIndex && block.EndLine >= lineIndex);
			if (markdownBlockNode != null)
			{
				markdownBlockNode.Kind = MarkdownBlockKind.Paragraph;
				markdownBlockNode.Level = null;
				markdownBlockNode.Folded = null;
				markdownBlockNode.ListKind = null;
				markdownBlockNode.CustomOrder = null;
				markdownBlockNode.Checked = null;
				markdownBlockNode.Language = null;
				markdownBlockNode.CodeText = null;
				markdownBlockNode.Indent = indent.GetValueOrDefault();
			}
			else
			{
				_semanticDocument.Blocks.Add(new MarkdownBlockNode
				{
					Kind = MarkdownBlockKind.Paragraph,
					StartLine = lineIndex,
					EndLine = lineIndex,
					Indent = indent.GetValueOrDefault()
				});
			}
			ExpandFoldedHeadingsThatWouldHideLine(lineIndex);
			_semanticBlocksByLine = BuildSemanticBlockIndex(_semanticDocument);
			UpdateHeadingFoldings();
		}
	}

	private void SetSemanticHorizontalRuleForLine(int lineIndex, int? indent = null)
	{
		if (lineIndex >= 0)
		{
			_semanticDocument = _semanticDocument ?? new MarkdownSemanticDocument();
			MarkdownBlockNode replacement = new MarkdownBlockNode
			{
				Kind = MarkdownBlockKind.HorizontalRule,
				StartLine = lineIndex,
				EndLine = lineIndex,
				Indent = indent.GetValueOrDefault()
			};
			ReplaceSingleLineSemanticBlock(lineIndex, replacement);
			ExpandFoldedHeadingsThatWouldHideLine(lineIndex);
			UpdateHeadingFoldings();
		}
	}

	private void ReplaceSingleLineSemanticBlock(int lineIndex, MarkdownBlockNode replacement)
	{
		if (lineIndex < 0 || replacement == null)
		{
			return;
		}
		_semanticDocument = _semanticDocument ?? new MarkdownSemanticDocument();
		List<MarkdownBlockNode> list = new List<MarkdownBlockNode>();
		foreach (MarkdownBlockNode item in _semanticDocument.Blocks.ToList())
		{
			if (item == null)
			{
				continue;
			}
			if (item.EndLine < lineIndex || item.StartLine > lineIndex)
			{
				list.Add(item);
				continue;
			}
			MarkdownBlockNode markdownBlockNode = CloneSemanticBlockAsLineRange(item, item.StartLine, lineIndex - 1);
			if (markdownBlockNode != null)
			{
				list.Add(markdownBlockNode);
			}
			MarkdownBlockNode markdownBlockNode2 = CloneSemanticBlockAsLineRange(item, lineIndex + 1, item.EndLine);
			if (markdownBlockNode2 != null)
			{
				list.Add(markdownBlockNode2);
			}
		}
		list.Add(replacement);
		list.Sort(delegate(MarkdownBlockNode left, MarkdownBlockNode right)
		{
			if (left == null && right == null)
			{
				return 0;
			}
			if (left == null)
			{
				return 1;
			}
			if (right == null)
			{
				return -1;
			}
			int num = left.StartLine.CompareTo(right.StartLine);
			return (num == 0) ? left.EndLine.CompareTo(right.EndLine) : num;
		});
		_semanticDocument.Blocks.Clear();
		_semanticDocument.Blocks.AddRange(list.Where((MarkdownBlockNode block) => block != null));
		_semanticBlocksByLine = BuildSemanticBlockIndex(_semanticDocument);
	}

	public void Highlight()
	{
		ToggleInlineMark(MarkdownMarkKind.Highlight);
	}

	public void HighlightWithSelectedColor()
	{
		HighlightEx(HighlightColorMenuState.GetSelectedColor());
	}

	public void HighlightEx(string background = null, string color = null)
	{
		if (ReadOnly)
		{
			return;
		}
		TryRestoreUnfocusedInlineCommandSelection();
		if (!CaretNearAttachment())
		{
			string text = NormalizeHighlightColor(background, color);
			if (string.IsNullOrWhiteSpace(text))
			{
				ClearHighlight();
			}
			else
			{
				ToggleInlineMark(MarkdownMarkKind.Highlight, text);
			}
		}
	}

	public void ClearHighlight()
	{
		if (ReadOnly)
		{
			return;
		}
		TryRestoreUnfocusedInlineCommandSelection();
		if (CaretNearAttachment() || !CanApplyInlineSemanticAtCurrentSelection())
		{
			return;
		}
		if (EditBox.SelectionLength > 0)
		{
			int selectionStart = EditBox.SelectionStart;
			int selectionLength = EditBox.SelectionLength;
			int caretOffset = EditBox.CaretOffset;
			string persistedText = GetPersistedText();
			RemoveTypedSemanticMarks(MarkdownMarkKind.Highlight, selectionStart, selectionStart + selectionLength);
			RequestImmediateRedrawForDocumentRange(selectionStart, selectionStart + selectionLength);
			RefreshOpenMenuContexts();
			NotifySemanticTextChangedIfNeeded(persistedText);
			FocusEditBoxAndRestoreSelection(selectionStart, selectionLength, caretOffset);
		}
		else
		{
			int caretOffset2 = EditBox.CaretOffset;
			ClearBackspaceRetainedTypingMarks();
			ClearActiveTypingMark(MarkdownMarkKind.Highlight);
			if (IsInheritedInlineTypingMarkActiveAtCaret(MarkdownMarkKind.Highlight))
			{
				_suppressedTypingMarks[MarkdownMarkKind.Highlight] = caretOffset2;
			}
			else
			{
				_suppressedTypingMarks.Remove(MarkdownMarkKind.Highlight);
			}
			RequestImmediateRedrawAroundCaret();
			RefreshOpenMenuContexts();
			FocusEditBox();
		}
	}

	private void ClearActiveInlineTypingAfterLineBreak()
	{
		if ((0u | (ClearActiveTypingMark(MarkdownMarkKind.Strong) ? 1u : 0u) | (ClearActiveTypingMark(MarkdownMarkKind.Emphasis) ? 1u : 0u) | (ClearActiveTypingMark(MarkdownMarkKind.Underline) ? 1u : 0u) | (ClearActiveTypingMark(MarkdownMarkKind.Strikethrough) ? 1u : 0u) | (ClearActiveTypingMark(MarkdownMarkKind.Code) ? 1u : 0u) | (ClearActiveTypingMark(MarkdownMarkKind.Highlight) ? 1u : 0u)) != 0)
		{
			RefreshOpenMenuContexts();
		}
	}

	public void ToggleCheckItem()
	{
		Selection selection = EditBox.TextArea.Selection;
		if (selection != null && selection.StartPosition.Line > 1)
		{
			int num = Math.Min(selection.StartPosition.Line, selection.EndPosition.Line);
			int num2 = Math.Max(selection.StartPosition.Line, selection.EndPosition.Line);
			for (int i = num; i <= num2; i++)
			{
				DocumentLine lineByNumber = EditBox.Document.GetLineByNumber(i);
				if (lineByNumber != null)
				{
					ToggleLineCheckItem(lineByNumber.Offset);
				}
			}
		}
		else
		{
			ToggleLineCheckItem(EditBox.CaretOffset);
		}
	}

	internal void SetTaskListCheckedStateAtLineOffset(int lineOffset, bool isChecked)
	{
		DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(Math.Max(0, Math.Min(lineOffset, EditBox.Document.TextLength)));
		if (lineByOffset == null)
		{
			return;
		}
		int lineIndex = Math.Max(0, lineByOffset.LineNumber - 1);
		if (TryGetSemanticTaskListForLine(lineIndex, out var isChecked2))
		{
			if (isChecked2 == isChecked)
			{
				ForceImmediateBlockRenderAfterEnter();
				return;
			}
			string persistedText = GetPersistedText();
			SetSemanticTaskListForLine(lineIndex, enabled: true, isChecked);
			RefreshOpenMenuContexts();
			RequestImmediateRedrawForDocumentRange(lineByOffset.Offset, lineByOffset.EndOffset);
			NotifySemanticTextChangedIfNeeded(persistedText);
			ForceImmediateBlockRenderAfterEnter();
		}
	}

	public void TrySaveContent()
	{
		this.SaveContent?.Invoke(this, null);
	}

	private void ToggleLineCheckItem(int lineOffset)
	{
		DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(Math.Max(0, Math.Min(lineOffset, EditBox.Document.TextLength)));
		if (lineByOffset != null && !IsCodeBlockLine(lineByOffset.LineNumber - 1))
		{
			int lineIndex = Math.Max(0, lineByOffset.LineNumber - 1);
			if (TryGetSemanticTaskListForLine(lineIndex, out var isChecked) && isChecked)
			{
				SetTaskListCheckedStateAtLineOffset(lineByOffset.Offset, isChecked: false);
			}
			else if (TryGetSemanticTaskListForLine(lineIndex, out isChecked) && !isChecked)
			{
				SetTaskListCheckedStateAtLineOffset(lineByOffset.Offset, isChecked: true);
			}
		}
	}

	internal void AdjustOrderedListMarkers(int? affectedStartLine = null)
	{
		if (_semanticDocument?.Blocks == null)
		{
			return;
		}
		int num = Math.Max(0, (affectedStartLine ?? 1) - 1);
		List<MarkdownBlockNode> list = (from block in _semanticDocument.Blocks
			where block != null && block.Kind == MarkdownBlockKind.List && block.ListKind == MarkdownListKind.Numbered
			orderby block.StartLine, block.Indent
			select block).ToList();
		Dictionary<int, int> dictionary = new Dictionary<int, int>();
		int num2 = -1000;
		foreach (MarkdownBlockNode item in list)
		{
			int result = 0;
			int.TryParse(item.CustomOrder, out result);
			result = Math.Max(1, result);
			if (item.StartLine < num)
			{
				if (item.StartLine > num2 + 1)
				{
					dictionary.Clear();
				}
				num2 = item.EndLine;
				dictionary[item.Indent] = result;
				ClearDeeperOrderedListCounters(dictionary, item.Indent);
				continue;
			}
			bool flag = item.StartLine > num2 + 1;
			if (flag)
			{
				dictionary.Clear();
			}
			if (item.StartLine > num2 + 1)
			{
				num2 = item.StartLine - 1;
			}
			if (!dictionary.TryGetValue(item.Indent, out var value))
			{
				value = (flag ? (result - 1) : 0);
			}
			value++;
			dictionary[item.Indent] = value;
			string text = value.ToString();
			if (!string.Equals(item.CustomOrder, text, StringComparison.Ordinal))
			{
				item.CustomOrder = text;
				PendingOrderedListRedrawLineIndices.Add(item.StartLine);
			}
			ClearDeeperOrderedListCounters(dictionary, item.Indent);
			num2 = item.EndLine;
		}
		_semanticBlocksByLine = BuildSemanticBlockIndex(_semanticDocument);
		SchedulePendingOrderedListRedraw();
	}

	private static void ClearDeeperOrderedListCounters(Dictionary<int, int> countersByIndent, int currentIndent)
	{
		if (countersByIndent == null || countersByIndent.Count == 0)
		{
			return;
		}
		foreach (int item in countersByIndent.Keys.Where((int indent) => indent > currentIndent).ToList())
		{
			countersByIndent.Remove(item);
		}
	}

	public void InsertLine()
	{
		if (CaretNearAttachment() || ReadOnly)
		{
			return;
		}
		TryExpandFoldedHeadingAtInsertionOffset(EditBox.CaretOffset);
		DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(EditBox.CaretOffset);
		if (lineByOffset == null || IsCodeBlockLine(lineByOffset.LineNumber - 1))
		{
			return;
		}
		string text = EditBox.Document.GetText(lineByOffset.Offset, lineByOffset.Length);
		if (TryInsertHorizontalRuleInsideBlockQuote(lineByOffset, text))
		{
			return;
		}
		int lineIndex = Math.Max(0, lineByOffset.LineNumber - 1);
		MarkdownBlockNode block;
		bool flag = (!TryGetSemanticBlockAtLine(lineIndex, out block) || block == null || block.Kind != MarkdownBlockKind.List) && IsEffectivelyEmptySemanticLine(text);
		int targetLineIndex = (flag ? Math.Max(0, lineByOffset.LineNumber - 1) : Math.Max(0, lineByOffset.LineNumber));
		int num = (flag ? lineByOffset.Offset : (lineByOffset.EndOffset + lineByOffset.DelimiterLength));
		string text2 = "\u200c\u200d\u2060\n";
		int value = num + text2.Length;
		if (!flag)
		{
			if (lineByOffset.NextLine == null)
			{
				num = lineByOffset.EndOffset;
				text2 = "\n\u200c\u200d\u2060\n";
				value = num + text2.Length;
			}
			else
			{
				text2 = "\u200c\u200d\u2060\n\n";
				value = num + "\u200c\u200d\u2060".Length + 1;
			}
		}
		ApplyDocumentMutationsAndNotify(new DocumentMutation[1]
		{
			new DocumentMutation
			{
				StartOffset = num,
				RemoveLength = 0,
				InsertText = text2
			}
		}, value, delegate
		{
			SetSemanticHorizontalRuleForLine(targetLineIndex);
			SetSemanticParagraphForLine(targetLineIndex + 1, 0);
		});
		FocusEditBox();
	}

	public int GetBlockInsertionOffsetAfterCurrentListLine(int requestedOffset)
	{
		if (EditBox?.Document == null)
		{
			return Math.Max(0, requestedOffset);
		}
		int textLength = EditBox.Document.TextLength;
		int num = Math.Max(0, Math.Min(requestedOffset, textLength));
		DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(num);
		if (lineByOffset == null)
		{
			return num;
		}
		int lineIndex = Math.Max(0, lineByOffset.LineNumber - 1);
		if (!TryGetSemanticBlockAtLine(lineIndex, out var block) || block == null || block.Kind != MarkdownBlockKind.List)
		{
			return num;
		}
		return lineByOffset.EndOffset;
	}

	public int GetAttachmentInsertionOffsetOutsideCodeBlock(int requestedOffset)
	{
		if (EditBox?.Document == null)
		{
			return Math.Max(0, requestedOffset);
		}
		int textLength = EditBox.Document.TextLength;
		int num = Math.Max(0, Math.Min(requestedOffset, textLength));
		DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(num);
		if (lineByOffset == null)
		{
			return num;
		}
		int lineIndex = Math.Max(0, lineByOffset.LineNumber - 1);
		if (!TryGetSemanticBlockAtLine(lineIndex, out var block) || block == null || block.Kind != MarkdownBlockKind.CodeBlock)
		{
			return num;
		}
		return textLength;
	}

	private bool TryInsertHorizontalRuleInsideBlockQuote(DocumentLine line, string lineText)
	{
		if (line == null || EditBox?.Document == null || EditBox.SelectionLength > 0)
		{
			return false;
		}
		int lineIndex = Math.Max(0, line.LineNumber - 1);
		if (!TryGetSemanticBlockAtLine(lineIndex, out var block) || block == null || block.Kind != MarkdownBlockKind.BlockQuote)
		{
			return false;
		}
		int num = Math.Max(line.Offset, Math.Min(EditBox.CaretOffset, line.EndOffset));
		int num2 = line.Offset + GetSemanticBlockContentOffset(lineText, block);
		if (num <= num2 || num >= line.EndOffset)
		{
			return false;
		}
		int num3 = line.EndOffset - num;
		string text = EditBox.Document.GetText(num, num3);
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		int quoteIndent = block.Indent;
		string text2 = "\n\u200c\u200d\u2060\n";
		string insertText = text2 + text;
		int nextLineIndex = lineIndex + 1;
		int tailLineIndex = lineIndex + 2;
		ApplyDocumentMutationsAndNotify(new DocumentMutation[1]
		{
			new DocumentMutation
			{
				StartOffset = num,
				RemoveLength = num3,
				InsertText = insertText
			}
		}, num + text2.Length, delegate
		{
			ReplaceSingleLineSemanticBlock(lineIndex, new MarkdownBlockNode
			{
				Kind = MarkdownBlockKind.BlockQuote,
				StartLine = lineIndex,
				EndLine = lineIndex,
				Indent = quoteIndent
			});
			ReplaceSingleLineSemanticBlock(nextLineIndex, new MarkdownBlockNode
			{
				Kind = MarkdownBlockKind.HorizontalRule,
				StartLine = nextLineIndex,
				EndLine = nextLineIndex,
				Indent = 0
			});
			ReplaceSingleLineSemanticBlock(tailLineIndex, new MarkdownBlockNode
			{
				Kind = MarkdownBlockKind.Paragraph,
				StartLine = tailLineIndex,
				EndLine = tailLineIndex,
				Indent = 0
			});
			ExpandFoldedHeadingsThatWouldHideLines(lineIndex, tailLineIndex);
			UpdateHeadingFoldings();
		});
		FocusEditBox();
		return true;
	}

	public void InsertCodeBlock()
	{
		if (CaretNearAttachment() || ReadOnly)
		{
			return;
		}
		TryExpandFoldedHeadingAtInsertionOffset(EditBox.CaretOffset);
		DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(EditBox.CaretOffset);
		if (lineByOffset == null || IsCodeBlockLine(lineByOffset.LineNumber - 1))
		{
			return;
		}
		string value = EditBox.Document.GetText(lineByOffset.Offset, lineByOffset.Length).Trim();
		string text = (string.IsNullOrEmpty(value) ? "```\n\n```" : "\n```\n\n```");
		PreparedEditorTextState prepared = PrepareEditorTextState(text);
		string text2 = prepared.DisplayText ?? string.Empty;
		int num = (string.IsNullOrEmpty(value) ? lineByOffset.Offset : lineByOffset.EndOffset);
		int removedLength = (string.IsNullOrEmpty(value) ? lineByOffset.Length : 0);
		int num2 = (string.IsNullOrEmpty(value) ? 4 : 5);
		int num3 = ((prepared.RawToDisplayOffsetMap != null && prepared.RawToDisplayOffsetMap.Length > num2) ? prepared.RawToDisplayOffsetMap[num2] : text2.Length);
		ReplaceEditorText(num, removedLength, text2, num + num3, delegate(int insertedOffset, int insertedLength)
		{
			ApplyPreparedSemanticBlocks(prepared, insertedOffset, insertedLength);
			if (prepared.SemanticMarks == null || prepared.SemanticMarks.Count == 0)
			{
				return;
			}
			foreach (MarkdownInlineDisplaySegment item in prepared.SemanticMarks.Where((MarkdownInlineDisplaySegment mark) => mark != null && mark.SourceLength > 0))
			{
				AddTypedSemanticMarkFromSegment(item, insertedOffset + item.SourcePosition, item.SourceLength);
			}
		});
		FocusEditBox();
	}

	private void ToggleInlineMark(MarkdownMarkKind kind, string color = null)
	{
		if (ReadOnly)
		{
			return;
		}
		TryRestoreUnfocusedInlineCommandSelection();
		if (CaretNearAttachment() || !CanApplyInlineSemanticAtCurrentSelection())
		{
			return;
		}
		int caretOffset = EditBox.CaretOffset;
		bool flag = IsBackspaceRetainedTypingMarkActive(kind, caretOffset);
		string highlightColor;
		bool flag2 = kind == MarkdownMarkKind.Highlight && flag && TryGetBackspaceRetainedHighlightColor(caretOffset, out highlightColor) && IsSameSemanticColor(highlightColor, color);
		ClearBackspaceRetainedTypingMarks();
		if (EditBox.SelectionLength > 0)
		{
			int selectionStart = EditBox.SelectionStart;
			int selectionLength = EditBox.SelectionLength;
			int caretOffset2 = EditBox.CaretOffset;
			ApplyInlineMarkToSelection(kind, color);
			FocusEditBoxAndRestoreSelection(selectionStart, selectionLength, caretOffset2);
			return;
		}
		if (kind == MarkdownMarkKind.Highlight)
		{
			bool flag3 = IsInheritedHighlightTypingMarkActiveAtCaret(color);
			if ((IsActiveTypingMarkAtOffset(kind, EditBox.CaretOffset) && IsSameSemanticColor(_activeTypingHighlightColor, color)) || flag2)
			{
				ClearActiveTypingMark(kind);
				if (flag3)
				{
					_suppressedTypingMarks[kind] = EditBox.CaretOffset;
				}
				else
				{
					_suppressedTypingMarks.Remove(kind);
				}
			}
			else if (IsTypingMarkSuppressedAtOffset(kind, EditBox.CaretOffset))
			{
				_suppressedTypingMarks.Remove(kind);
				if (!flag3)
				{
					_activeTypingHighlightColor = color;
					SetActiveTypingMark(kind, EditBox.CaretOffset);
				}
			}
			else if (flag3)
			{
				ClearActiveTypingMark(kind);
				_suppressedTypingMarks[kind] = EditBox.CaretOffset;
			}
			else
			{
				_suppressedTypingMarks.Remove(kind);
				_activeTypingHighlightColor = color;
				SetActiveTypingMark(kind, EditBox.CaretOffset);
			}
		}
		else if (IsActiveTypingMarkAtOffset(kind, EditBox.CaretOffset) || flag)
		{
			ClearActiveTypingMark(kind);
			if (IsInheritedInlineTypingMarkActiveAtCaret(kind))
			{
				_suppressedTypingMarks[kind] = EditBox.CaretOffset;
			}
		}
		else if (IsTypingMarkSuppressedAtOffset(kind, EditBox.CaretOffset))
		{
			_suppressedTypingMarks.Remove(kind);
			if (!IsInheritedInlineTypingMarkActiveAtCaret(kind))
			{
				SetActiveTypingMark(kind, EditBox.CaretOffset);
			}
		}
		else if (IsInheritedInlineTypingMarkActiveAtCaret(kind))
		{
			_suppressedTypingMarks[kind] = EditBox.CaretOffset;
		}
		else
		{
			SetActiveTypingMark(kind, EditBox.CaretOffset);
			_suppressedTypingMarks.Remove(kind);
		}
		RequestImmediateRedrawForSelectionOrCaret();
		RefreshOpenMenuContexts();
		FocusEditBox();
	}

	private bool IsTypingMarkSuppressedAtOffset(MarkdownMarkKind kind, int offset)
	{
		if (_suppressedTypingMarks.TryGetValue(kind, out var value))
		{
			return value == offset;
		}
		return false;
	}

	private bool IsInheritedHighlightTypingMarkActiveAtCaret(string color)
	{
		if (EditBox == null)
		{
			return false;
		}
		int caretOffset = EditBox.CaretOffset;
		return GetSemanticMarkList().Any((MarkdownInlineDisplaySegment mark) => mark != null && mark.Kind == MarkdownMarkKind.Highlight && mark.SourceLength > 0 && IsSameSemanticColor(mark.Color, color) && IsInlineTypingContextMark(mark) && IsInlineTypingContextActive(mark, caretOffset, 0));
	}

	private bool IsInheritedInlineTypingMarkActiveAtCaret(MarkdownMarkKind kind)
	{
		if (EditBox == null)
		{
			return false;
		}
		int caretOffset = EditBox.CaretOffset;
		return GetSemanticMarkList().Any((MarkdownInlineDisplaySegment mark) => mark != null && mark.Kind == kind && mark.SourceLength > 0 && IsInlineTypingContextMark(mark) && IsInlineTypingContextActive(mark, caretOffset, 0));
	}

	private bool TryRestoreUnfocusedInlineCommandSelection()
	{
		if (_currentFocused || EditBox?.Document == null || EditBox.SelectionLength > 0 || _lastInsertSelectionLength <= 0)
		{
			return false;
		}
		int textLength = EditBox.Document.TextLength;
		int num = Math.Max(0, Math.Min(_lastInsertOffset, textLength));
		int num2 = Math.Max(0, Math.Min(_lastInsertSelectionLength, textLength - num));
		if (num2 <= 0)
		{
			return false;
		}
		RestoreEditorSelection(num, num2, _lastInsertActiveOffset);
		return EditBox.SelectionLength > 0;
	}

	private void FocusEditBoxAndRestoreSelection(int selectionStart, int selectionLength, int activeOffset)
	{
		FocusEditBox();
		RestoreEditorSelection(selectionStart, selectionLength, activeOffset);
		if (base.IsInitialized || base.IsLoaded)
		{
			base.Dispatcher?.BeginInvoke((Action)delegate
			{
				RestoreEditorSelection(selectionStart, selectionLength, activeOffset);
			}, DispatcherPriority.ContextIdle);
		}
	}

	private void RestoreEditorSelection(int selectionStart, int selectionLength, int activeOffset)
	{
		if (selectionLength <= 0 || EditBox?.Document == null || EditBox.TextArea == null)
		{
			return;
		}
		int textLength = EditBox.Document.TextLength;
		int num = Math.Max(0, Math.Min(selectionStart, textLength));
		int num2 = Math.Max(num, Math.Min(selectionStart + selectionLength, textLength));
		if (num2 <= num)
		{
			return;
		}
		int activeOffset2 = Math.Max(0, Math.Min(activeOffset, textLength));
		bool suppressSemanticSelectionNormalization = _suppressSemanticSelectionNormalization;
		_suppressSemanticSelectionNormalization = true;
		try
		{
			ApplyNormalizedSemanticSelection(num, num2, activeOffset2);
			_codeBlockSelectionLayer?.InvalidateVisual();
		}
		finally
		{
			_suppressSemanticSelectionNormalization = suppressSemanticSelectionNormalization;
		}
	}

	private void ApplyInlineMarkToSelection(MarkdownMarkKind kind, string color = null)
	{
		string persistedText = GetPersistedText();
		int selectionStart = EditBox.SelectionStart;
		int selectionLength = EditBox.SelectionLength;
		if (selectionLength <= 0)
		{
			return;
		}
		int num = selectionStart + selectionLength;
		if (IsRangeFullyCoveredByTypedSemanticMarks(kind, selectionStart, num, color))
		{
			RemoveTypedSemanticMarks(kind, selectionStart, num, color);
		}
		else
		{
			if (kind == MarkdownMarkKind.Highlight)
			{
				RemoveTypedSemanticMarks(kind, selectionStart, num);
			}
			AddOrExtendTypedSemanticMark(kind, selectionStart, selectionLength, color);
		}
		RequestImmediateRedrawForDocumentRange(selectionStart, num);
		RefreshOpenMenuContexts();
		NotifySemanticTextChangedIfNeeded(persistedText);
	}

	private void NotifySemanticTextChangedIfNeeded(string previousPersistedText, bool deferTextChangedNotification = false)
	{
		string persistedText = GetPersistedText();
		if ((!string.IsNullOrEmpty(persistedText) || string.IsNullOrEmpty(EditBox?.Text)) && !string.Equals(previousPersistedText, persistedText, StringComparison.Ordinal))
		{
			NotifySemanticTextChanged(persistedText, deferTextChangedNotification);
		}
	}

	private void NotifySemanticTextChanged(string currentPersistedText, bool deferTextChangedNotification = false)
	{
		if (!string.IsNullOrEmpty(currentPersistedText) || string.IsNullOrEmpty(EditBox?.Text))
		{
			UpdateBindingTextFromEditor(currentPersistedText);
			CaptureSemanticHistorySnapshotWithPersistedText(currentPersistedText);
			if (deferTextChangedNotification)
			{
				MarkLocalDocumentChangePending();
				_scheduledTextChangeTaskId = _editingTaskId;
				_textChangeNotifyHandler.TryDoAction();
			}
			else
			{
				NotifyTextChanged(this, EventArgs.Empty);
			}
		}
	}

	private void UpdateBindingTextFromEditor(string persistedText = null)
	{
		if (!base.IsInitialized && !base.IsLoaded)
		{
			return;
		}
		persistedText = persistedText ?? GetPersistedText();
		_imageElementGenerator?.QueuePersistedAttachmentCheck(persistedText);
		if ((string.IsNullOrEmpty(persistedText) && !string.IsNullOrEmpty(EditBox?.Text)) || string.Equals(BindingText, persistedText, StringComparison.Ordinal))
		{
			return;
		}
		_suppressBindingTextToEditorSync = true;
		try
		{
			BindingText = persistedText;
		}
		finally
		{
			_suppressBindingTextToEditorSync = false;
		}
	}

	private bool IsRangeFullyCoveredByTypedSemanticMarks(MarkdownMarkKind kind, int selectionStart, int selectionEnd, string color = null)
	{
		if (selectionEnd <= selectionStart)
		{
			return false;
		}
		var list = (from mark in _typedSemanticMarks
			where mark.Kind == kind && mark.SourceLength > 0 && IsSameSemanticColor(mark.Color, color)
			select new
			{
				Start = mark.SourcePosition,
				End = mark.SourcePosition + mark.SourceLength
			} into range
			where range.End > selectionStart && range.Start < selectionEnd
			orderby range.Start
			select range).ToList();
		if (list.Count == 0)
		{
			return false;
		}
		int num = selectionStart;
		foreach (var item in list)
		{
			if (item.Start > num)
			{
				return false;
			}
			num = Math.Max(num, item.End);
			if (num >= selectionEnd)
			{
				return true;
			}
		}
		return false;
	}

	private bool RemoveTypedSemanticMarks(MarkdownMarkKind kind, int selectionStart, int selectionEnd, string color = null)
	{
		bool result = false;
		List<MarkdownInlineDisplaySegment> list = new List<MarkdownInlineDisplaySegment>();
		for (int num = _typedSemanticMarks.Count - 1; num >= 0; num--)
		{
			MarkdownInlineDisplaySegment markdownInlineDisplaySegment = _typedSemanticMarks[num];
			bool flag = (kind == MarkdownMarkKind.Highlight && color == null) || IsSameSemanticColor(markdownInlineDisplaySegment.Color, color);
			if (markdownInlineDisplaySegment.Kind == kind && flag)
			{
				int sourcePosition = markdownInlineDisplaySegment.SourcePosition;
				int num2 = markdownInlineDisplaySegment.SourcePosition + markdownInlineDisplaySegment.SourceLength;
				if (selectionStart < num2 && selectionEnd > sourcePosition)
				{
					_typedSemanticMarks.RemoveAt(num);
					result = true;
					if (sourcePosition < selectionStart)
					{
						list.Add(new MarkdownInlineDisplaySegment
						{
							Kind = markdownInlineDisplaySegment.Kind,
							SourcePosition = sourcePosition,
							SourceLength = selectionStart - sourcePosition,
							OpeningDelimiterLength = 0,
							ClosingDelimiterLength = 0,
							Color = markdownInlineDisplaySegment.Color,
							LinkText = markdownInlineDisplaySegment.LinkText,
							Url = markdownInlineDisplaySegment.Url,
							UrlLength = markdownInlineDisplaySegment.UrlLength,
							UrlPosition = markdownInlineDisplaySegment.UrlPosition
						});
					}
					if (selectionEnd < num2)
					{
						list.Add(new MarkdownInlineDisplaySegment
						{
							Kind = markdownInlineDisplaySegment.Kind,
							SourcePosition = selectionEnd,
							SourceLength = num2 - selectionEnd,
							OpeningDelimiterLength = 0,
							ClosingDelimiterLength = 0,
							Color = markdownInlineDisplaySegment.Color,
							LinkText = markdownInlineDisplaySegment.LinkText,
							Url = markdownInlineDisplaySegment.Url,
							UrlLength = markdownInlineDisplaySegment.UrlLength,
							UrlPosition = markdownInlineDisplaySegment.UrlPosition
						});
					}
				}
			}
		}
		if (list.Count > 0)
		{
			_typedSemanticMarks.AddRange(list);
		}
		return result;
	}

	private void ApplyTypingMarksToInsertedText(int startOffset, int length, IEnumerable<MarkdownInlineDisplaySegment> typingMarks = null)
	{
		List<MarkdownInlineDisplaySegment> list = (from @group in (typingMarks ?? GetActiveTypingMarkSegments()).Where((MarkdownInlineDisplaySegment mark) => mark != null).GroupBy(MarkdownInlineSemanticMetadata.GetPersistenceIdentity)
			select @group.First()).ToList();
		if (length <= 0 || list.Count == 0)
		{
			return;
		}
		foreach (MarkdownInlineDisplaySegment item in list)
		{
			AddTypedSemanticMarkFromSegment(item, startOffset, length);
		}
		_needsTypedSemanticMarkRedraw = true;
	}

	private void AddTypedSemanticMarkFromSegment(MarkdownInlineDisplaySegment mark, int startOffset, int length)
	{
		if (mark != null && length > 0)
		{
			if (mark.Kind == MarkdownMarkKind.Link)
			{
				AddLinkSemanticMark(startOffset, length, mark.Url);
			}
			else
			{
				AddOrExtendTypedSemanticMark(mark.Kind, startOffset, length, mark.Color);
			}
		}
	}

	private void AddOrExtendTypedSemanticMark(MarkdownMarkKind kind, int startOffset, int length, string color = null)
	{
		if (length <= 0)
		{
			return;
		}
		int num = startOffset;
		int num2 = startOffset + length;
		if (kind == MarkdownMarkKind.Highlight)
		{
			RemoveTypedSemanticMarks(kind, num, num2);
		}
		for (int num3 = _typedSemanticMarks.Count - 1; num3 >= 0; num3--)
		{
			MarkdownInlineDisplaySegment markdownInlineDisplaySegment = _typedSemanticMarks[num3];
			if (markdownInlineDisplaySegment.Kind == kind && IsSameSemanticColor(markdownInlineDisplaySegment.Color, color))
			{
				int sourcePosition = markdownInlineDisplaySegment.SourcePosition;
				int num4 = markdownInlineDisplaySegment.SourcePosition + markdownInlineDisplaySegment.SourceLength;
				if (num4 >= num && sourcePosition <= num2)
				{
					num = Math.Min(num, sourcePosition);
					num2 = Math.Max(num2, num4);
					_typedSemanticMarks.RemoveAt(num3);
				}
			}
		}
		_typedSemanticMarks.Add(new MarkdownInlineDisplaySegment
		{
			Kind = kind,
			SourcePosition = num,
			SourceLength = num2 - num,
			OpeningDelimiterLength = 0,
			ClosingDelimiterLength = 0,
			Color = color
		});
	}

	private void AddLinkSemanticMark(int startOffset, int length, string url)
	{
		if (length > 0 && !string.IsNullOrWhiteSpace(url))
		{
			int valueOrDefault = (EditBox?.Text?.Length).GetValueOrDefault();
			int num = Math.Max(0, Math.Min(startOffset, valueOrDefault));
			int num2 = Math.Max(num, Math.Min(num + length, valueOrDefault));
			if (num2 > num)
			{
				RemoveOverlappingLinkSemanticMarks(num, num2);
				_typedSemanticMarks.Add(new MarkdownInlineDisplaySegment
				{
					Kind = MarkdownMarkKind.Link,
					SourcePosition = num,
					SourceLength = num2 - num,
					OpeningDelimiterLength = 0,
					ClosingDelimiterLength = 0,
					LinkText = EditBox?.Text?.Substring(num, num2 - num),
					Url = url
				});
				_needsTypedSemanticMarkRedraw = true;
			}
		}
	}

	private void RemoveOverlappingLinkSemanticMarks(int startOffset, int endOffset)
	{
		_typedSemanticMarks.RemoveAll((MarkdownInlineDisplaySegment mark) => mark != null && mark.Kind == MarkdownMarkKind.Link && startOffset < mark.SourcePosition + mark.SourceLength && endOffset > mark.SourcePosition);
		SemanticMarkList.RemoveAll((MarkdownInlineDisplaySegment mark) => mark != null && mark.Kind == MarkdownMarkKind.Link && startOffset < mark.SourcePosition + mark.SourceLength && endOffset > mark.SourcePosition);
	}

	private static bool IsSameSemanticColor(string left, string right)
	{
		return string.Equals(left ?? string.Empty, right ?? string.Empty, StringComparison.OrdinalIgnoreCase);
	}

	private static string NormalizeHighlightColor(string background, string color)
	{
		if (!string.IsNullOrWhiteSpace(background))
		{
			return HighlightColorMenuState.GetEditorColor(background);
		}
		if (!string.IsNullOrWhiteSpace(color))
		{
			return HighlightColorMenuState.GetEditorColor(color);
		}
		return null;
	}

	private IReadOnlyCollection<MarkdownInlineDisplaySegment> GetActiveTypingMarkSegments(int offset = -1)
	{
		int activeOffset = ((offset >= 0) ? offset : (EditBox?.CaretOffset ?? (-1)));
		return (from kind in _activeTypingMarks
			where IsActiveTypingMarkAtOffset(kind, activeOffset)
			select new MarkdownInlineDisplaySegment
			{
				Kind = kind,
				Color = ((kind == MarkdownMarkKind.Highlight) ? _activeTypingHighlightColor : null)
			}).ToList();
	}

	private bool AdjustTypedSemanticMarksForReplace(int startOffset, int removedLength, int insertedLength)
	{
		return AdjustInlineSemanticMarksForReplace(_typedSemanticMarks, startOffset, removedLength, insertedLength, IsTypingMarkSuppressedAtOffset);
	}

	private static bool AdjustInlineSemanticMarksForReplace(IList<MarkdownInlineDisplaySegment> marks, int startOffset, int removedLength, int insertedLength)
	{
		return AdjustInlineSemanticMarksForReplace(marks, startOffset, removedLength, insertedLength, null);
	}

	private static bool AdjustInlineSemanticMarksForReplace(IList<MarkdownInlineDisplaySegment> marks, int startOffset, int removedLength, int insertedLength, Func<MarkdownMarkKind, int, bool> isTypingMarkSuppressedAtOffset)
	{
		if (marks == null || marks.Count == 0)
		{
			return false;
		}
		bool result = false;
		int num = startOffset + Math.Max(0, removedLength);
		int num2 = insertedLength - removedLength;
		for (int num3 = marks.Count - 1; num3 >= 0; num3--)
		{
			MarkdownInlineDisplaySegment markdownInlineDisplaySegment = marks[num3];
			if (markdownInlineDisplaySegment == null)
			{
				marks.RemoveAt(num3);
				result = true;
				continue;
			}
			int sourcePosition = markdownInlineDisplaySegment.SourcePosition;
			int num4 = markdownInlineDisplaySegment.SourcePosition + markdownInlineDisplaySegment.SourceLength;
			if (removedLength > 0 && startOffset < num4 && num > sourcePosition)
			{
				if (startOffset <= sourcePosition && num >= num4)
				{
					marks.RemoveAt(num3);
					result = true;
					continue;
				}
				if (startOffset <= sourcePosition)
				{
					if (markdownInlineDisplaySegment.SourcePosition != startOffset + insertedLength || markdownInlineDisplaySegment.SourceLength != Math.Max(0, num4 - num))
					{
						result = true;
					}
					markdownInlineDisplaySegment.SourcePosition = startOffset + insertedLength;
					markdownInlineDisplaySegment.SourceLength = Math.Max(0, num4 - num);
				}
				else if (num >= num4)
				{
					if (markdownInlineDisplaySegment.SourceLength != Math.Max(0, startOffset - sourcePosition))
					{
						result = true;
					}
					markdownInlineDisplaySegment.SourceLength = Math.Max(0, startOffset - sourcePosition);
				}
				else
				{
					if (markdownInlineDisplaySegment.SourceLength != Math.Max(0, markdownInlineDisplaySegment.SourceLength - removedLength + insertedLength))
					{
						result = true;
					}
					markdownInlineDisplaySegment.SourceLength = Math.Max(0, markdownInlineDisplaySegment.SourceLength - removedLength + insertedLength);
				}
			}
			else if (removedLength == 0 && insertedLength > 0 && startOffset > sourcePosition && startOffset < num4)
			{
				if (isTypingMarkSuppressedAtOffset != null && isTypingMarkSuppressedAtOffset(markdownInlineDisplaySegment.Kind, startOffset) && TrySplitInlineSemanticMarkAroundInsertion(marks, num3, startOffset, insertedLength))
				{
					result = true;
					continue;
				}
				markdownInlineDisplaySegment.SourceLength += insertedLength;
				result = true;
			}
			else if (sourcePosition >= num && num2 != 0)
			{
				markdownInlineDisplaySegment.SourcePosition += num2;
				result = true;
			}
			if (markdownInlineDisplaySegment.SourceLength <= 0)
			{
				marks.RemoveAt(num3);
				result = true;
			}
		}
		return result;
	}

	private static bool TrySplitInlineSemanticMarkAroundInsertion(IList<MarkdownInlineDisplaySegment> marks, int markIndex, int insertionOffset, int insertedLength)
	{
		if (marks == null || markIndex < 0 || markIndex >= marks.Count || insertedLength <= 0)
		{
			return false;
		}
		MarkdownInlineDisplaySegment markdownInlineDisplaySegment = marks[markIndex];
		if (markdownInlineDisplaySegment == null || markdownInlineDisplaySegment.SourceLength <= 0)
		{
			return false;
		}
		int sourcePosition = markdownInlineDisplaySegment.SourcePosition;
		int num = markdownInlineDisplaySegment.SourcePosition + markdownInlineDisplaySegment.SourceLength;
		if (insertionOffset <= sourcePosition || insertionOffset >= num)
		{
			return false;
		}
		int sourceLength = insertionOffset - sourcePosition;
		int sourceLength2 = num - insertionOffset;
		markdownInlineDisplaySegment.SourceLength = sourceLength;
		marks.Add(new MarkdownInlineDisplaySegment
		{
			Kind = markdownInlineDisplaySegment.Kind,
			SourcePosition = insertionOffset + insertedLength,
			SourceLength = sourceLength2,
			OpeningDelimiterLength = 0,
			ClosingDelimiterLength = 0,
			Color = markdownInlineDisplaySegment.Color,
			LinkText = markdownInlineDisplaySegment.LinkText,
			Url = markdownInlineDisplaySegment.Url,
			UrlLength = markdownInlineDisplaySegment.UrlLength,
			UrlPosition = markdownInlineDisplaySegment.UrlPosition
		});
		return true;
	}

	private bool AdjustSemanticMarkListForReplace(int startOffset, int removedLength, int insertedLength)
	{
		if (SemanticMarkList.Count == 0)
		{
			return false;
		}
		bool result = false;
		int num = insertedLength - removedLength;
		int num2 = startOffset + removedLength;
		for (int num3 = SemanticMarkList.Count - 1; num3 >= 0; num3--)
		{
			MarkdownInlineDisplaySegment markdownInlineDisplaySegment = SemanticMarkList[num3];
			if (markdownInlineDisplaySegment == null)
			{
				SemanticMarkList.RemoveAt(num3);
				result = true;
				continue;
			}
			int sourcePosition = markdownInlineDisplaySegment.SourcePosition;
			int num4 = markdownInlineDisplaySegment.SourcePosition + markdownInlineDisplaySegment.SourceLength;
			if (removedLength > 0 && sourcePosition < num2 && num4 > startOffset)
			{
				if (startOffset <= sourcePosition && num2 >= num4)
				{
					SemanticMarkList.RemoveAt(num3);
					result = true;
					continue;
				}
				int num5 = Math.Max(sourcePosition, startOffset);
				int num6 = Math.Min(num4, num2);
				int num7 = Math.Max(0, num6 - num5);
				if (num7 > 0)
				{
					markdownInlineDisplaySegment.SourceLength = Math.Max(0, markdownInlineDisplaySegment.SourceLength - num7);
					result = true;
				}
				if (startOffset <= sourcePosition)
				{
					markdownInlineDisplaySegment.SourcePosition = Math.Max(startOffset, markdownInlineDisplaySegment.SourcePosition + num);
				}
				else if (num != 0 && num4 > num2)
				{
					markdownInlineDisplaySegment.SourceLength = Math.Max(0, markdownInlineDisplaySegment.SourceLength + num);
				}
			}
			else if (removedLength == 0 && insertedLength > 0 && startOffset > sourcePosition && startOffset < num4)
			{
				if (IsTypingMarkSuppressedAtOffset(markdownInlineDisplaySegment.Kind, startOffset) && TrySplitInlineSemanticMarkAroundInsertion(SemanticMarkList, num3, startOffset, insertedLength))
				{
					result = true;
					continue;
				}
				markdownInlineDisplaySegment.SourceLength += insertedLength;
				result = true;
			}
			else if (sourcePosition >= startOffset && num != 0)
			{
				markdownInlineDisplaySegment.SourcePosition += num;
				result = true;
			}
			if (markdownInlineDisplaySegment.SourceLength <= 0)
			{
				SemanticMarkList.RemoveAt(num3);
				result = true;
			}
		}
		return result;
	}

	public bool IsTypingMarkActive(MarkdownMarkKind kind)
	{
		int offset = EditBox?.CaretOffset ?? (-1);
		if (!IsActiveTypingMarkAtOffset(kind, offset))
		{
			return IsBackspaceRetainedTypingMarkActive(kind, offset);
		}
		return true;
	}

	public void Quote()
	{
		if (CaretNearAttachment() || ReadOnly)
		{
			return;
		}
		string persistedText = GetPersistedText();
		List<DocumentLine> selectedDocumentLines = GetSelectedDocumentLines();
		if (selectedDocumentLines.Count == 0)
		{
			DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(EditBox.CaretOffset);
			if (lineByOffset != null)
			{
				selectedDocumentLines.Add(lineByOffset);
			}
		}
		if (selectedDocumentLines.Count == 0 || ContainsCodeBlockLine(selectedDocumentLines))
		{
			return;
		}
		bool shouldRemove = selectedDocumentLines.All((DocumentLine line) => TryGetSemanticBlockQuoteForLine(line.LineNumber - 1));
		var lineInfos = selectedDocumentLines.Where((DocumentLine line) => line != null).Select(delegate(DocumentLine line)
		{
			string text = EditBox.Document.GetText(line);
			int caretAnchorInsertionOffsetForEmptySemanticLine2 = GetCaretAnchorInsertionOffsetForEmptySemanticLine(text);
			int lineIndex = Math.Max(0, line.LineNumber - 1);
			return new
			{
				Line = line,
				LineIndex = lineIndex,
				LineText = text,
				Indent = Math.Max(caretAnchorInsertionOffsetForEmptySemanticLine2, GetSemanticIndentForLineOrZero(lineIndex))
			};
		}).ToList();
		List<DocumentMutation> list = new List<DocumentMutation>();
		int? explicitCaretOffset = null;
		foreach (var item in lineInfos)
		{
			if (!IsEffectivelyEmptySemanticLine(item.LineText))
			{
				continue;
			}
			int anchorOffset2;
			if (shouldRemove)
			{
				if (TryGetDisplayCaretAnchorOffset(item.LineText, out var anchorOffset))
				{
					list.Add(new DocumentMutation
					{
						StartOffset = item.Line.Offset + anchorOffset,
						RemoveLength = "\u00a0".Length,
						InsertText = string.Empty
					});
				}
			}
			else if (!TryGetDisplayCaretAnchorOffset(item.LineText, out anchorOffset2))
			{
				int caretAnchorInsertionOffsetForEmptySemanticLine = GetCaretAnchorInsertionOffsetForEmptySemanticLine(item.LineText);
				list.Add(new DocumentMutation
				{
					StartOffset = item.Line.Offset + caretAnchorInsertionOffsetForEmptySemanticLine,
					RemoveLength = 0,
					InsertText = "\u00a0"
				});
				if (EditBox.SelectionLength == 0 && EditBox.CaretOffset >= item.Line.Offset && EditBox.CaretOffset <= item.Line.EndOffset)
				{
					explicitCaretOffset = item.Line.Offset + caretAnchorInsertionOffsetForEmptySemanticLine + "\u00a0".Length;
				}
			}
		}
		Action action = delegate
		{
			foreach (var item2 in lineInfos)
			{
				SetSemanticBlockQuoteForLine(item2.LineIndex, !shouldRemove, item2.Indent);
			}
		};
		if (list.Count > 0)
		{
			ApplyDocumentMutationsAndNotify(list, explicitCaretOffset, action);
			NotifySemanticTextChangedIfNeeded(persistedText);
			return;
		}
		action();
		SyncParagraphIndentMarkersAfterBlockStyleChange();
		RefreshOpenMenuContexts();
		RequestImmediateRedrawForLines(selectedDocumentLines);
		NotifySemanticTextChangedIfNeeded(persistedText);
	}

	private bool TryApplyEmptySemanticLineCaretAnchors(IEnumerable<DocumentLine> lines, bool shouldRemove, Action applySemantic, string previousPersistedText)
	{
		if (EditBox?.Document == null || lines == null)
		{
			return false;
		}
		List<DocumentMutation> list = new List<DocumentMutation>();
		int? explicitCaretOffset = null;
		foreach (DocumentLine item in lines.Where((DocumentLine line) => line != null))
		{
			string text = EditBox.Document.GetText(item);
			if (!IsEffectivelyEmptySemanticLine(text))
			{
				continue;
			}
			if (shouldRemove)
			{
				if (TryGetDisplayCaretAnchorOffset(text, out var anchorOffset))
				{
					list.Add(new DocumentMutation
					{
						StartOffset = item.Offset + anchorOffset,
						RemoveLength = "\u00a0".Length,
						InsertText = string.Empty
					});
				}
				continue;
			}
			if (TryGetDisplayCaretAnchorOffset(text, out var anchorOffset2))
			{
				if (EditBox.SelectionLength == 0 && EditBox.CaretOffset >= item.Offset && EditBox.CaretOffset <= item.Offset + anchorOffset2)
				{
					explicitCaretOffset = item.Offset + anchorOffset2 + "\u00a0".Length;
				}
				continue;
			}
			int caretAnchorInsertionOffsetForEmptySemanticLine = GetCaretAnchorInsertionOffsetForEmptySemanticLine(text);
			list.Add(new DocumentMutation
			{
				StartOffset = item.Offset + caretAnchorInsertionOffsetForEmptySemanticLine,
				RemoveLength = 0,
				InsertText = "\u00a0"
			});
			if (EditBox.SelectionLength == 0 && EditBox.CaretOffset >= item.Offset && EditBox.CaretOffset <= item.EndOffset)
			{
				explicitCaretOffset = item.Offset + caretAnchorInsertionOffsetForEmptySemanticLine + "\u00a0".Length;
			}
		}
		if (list.Count == 0)
		{
			if (explicitCaretOffset.HasValue)
			{
				applySemantic?.Invoke();
				EditBox.SelectionLength = 0;
				EditBox.CaretOffset = Math.Max(0, Math.Min(explicitCaretOffset.Value, EditBox.Text.Length));
				NormalizeCaretIntoSemanticContentIfNeeded();
				RefreshOpenMenuContexts();
				RequestImmediateRedrawForLines(lines);
				NotifySemanticTextChangedIfNeeded(previousPersistedText);
				return true;
			}
			return false;
		}
		ApplyDocumentMutationsAndNotify(list, explicitCaretOffset, applySemantic);
		return true;
	}

	public void RemoveStartText()
	{
		if (CaretNearAttachment() || ReadOnly)
		{
			return;
		}
		string persistedText = GetPersistedText();
		List<DocumentLine> selectedDocumentLines = GetSelectedDocumentLines();
		if (selectedDocumentLines.Count == 0)
		{
			DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(EditBox.CaretOffset);
			if (lineByOffset != null)
			{
				selectedDocumentLines.Add(lineByOffset);
			}
		}
		if (selectedDocumentLines.Count == 0 || ContainsCodeBlockLine(selectedDocumentLines))
		{
			return;
		}
		List<int> source = (from line in selectedDocumentLines
			where line != null
			select line.LineNumber).Distinct().ToList();
		bool flag = false;
		List<DocumentMutation> list = new List<DocumentMutation>();
		foreach (DocumentLine item in selectedDocumentLines)
		{
			int lineIndex = Math.Max(0, item.LineNumber - 1);
			if (TryGetSemanticHeadingLevelForLine(lineIndex, out var _))
			{
				SetSemanticHeadingLevelForLine(lineIndex, null);
				flag = true;
				continue;
			}
			if (TryGetSemanticBlockQuoteForLine(lineIndex))
			{
				SetSemanticBlockQuoteForLine(lineIndex, enabled: false);
				flag = true;
				continue;
			}
			if (TryGetSemanticBulletListForLine(lineIndex))
			{
				SetSemanticBulletListForLine(lineIndex, enabled: false);
				flag = true;
				continue;
			}
			if (TryGetSemanticTaskListForLine(lineIndex, out var _))
			{
				SetSemanticTaskListForLine(lineIndex, enabled: false);
				flag = true;
				continue;
			}
			if (TryGetSemanticOrderedListForLine(lineIndex, out var _))
			{
				SetSemanticOrderedListForLine(lineIndex, enabled: false);
				flag = true;
				continue;
			}
			string text = EditBox.Document.GetText(item);
			int removalOffset2;
			int removalLength2;
			if (TryGetSemanticBlockAtLine(lineIndex, out var block) && block != null)
			{
				if (block.Kind == MarkdownBlockKind.HorizontalRule && TryGetLineStyleRemoval(text, out var removalOffset, out var removalLength))
				{
					list.Add(new DocumentMutation
					{
						StartOffset = item.Offset + removalOffset,
						RemoveLength = removalLength,
						InsertText = string.Empty
					});
				}
			}
			else if (TryGetLineStyleRemoval(text, out removalOffset2, out removalLength2))
			{
				list.Add(new DocumentMutation
				{
					StartOffset = item.Offset + removalOffset2,
					RemoveLength = removalLength2,
					InsertText = string.Empty
				});
			}
		}
		if (list.Count > 0)
		{
			ApplyDocumentMutationsAndNotify(list, Math.Max(0, Math.Min(EditBox.CaretOffset, EditBox.Text.Length)));
		}
		if (flag || list.Count != 0)
		{
			SyncParagraphIndentMarkersAfterBlockStyleChange();
			RefreshOpenMenuContexts();
			RequestImmediateRedrawForLines(from lineNumber in source
				where lineNumber >= 1 && lineNumber <= EditBox.Document.LineCount
				select EditBox.Document.GetLineByNumber(lineNumber));
			NotifySemanticTextChangedIfNeeded(persistedText);
		}
	}

	public void InsertHeader(int level)
	{
		if (CaretNearAttachment() || ReadOnly)
		{
			return;
		}
		string persistedText = GetPersistedText();
		List<DocumentLine> selectedDocumentLines = GetSelectedDocumentLines();
		if (selectedDocumentLines.Count == 0)
		{
			DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(EditBox.CaretOffset);
			if (lineByOffset != null)
			{
				selectedDocumentLines.Add(lineByOffset);
			}
		}
		if (selectedDocumentLines.Count == 0 || ContainsCodeBlockLine(selectedDocumentLines))
		{
			return;
		}
		int level2;
		bool flag = selectedDocumentLines.All((DocumentLine line) => TryGetSemanticHeadingLevelForLine(line.LineNumber - 1, out level2) && level2 == level);
		foreach (DocumentLine item in selectedDocumentLines)
		{
			SetSemanticHeadingLevelForLine(item.LineNumber - 1, flag ? ((int?)null) : new int?(level));
		}
		SyncParagraphIndentMarkersAfterBlockStyleChange();
		RefreshOpenMenuContexts();
		RequestImmediateRedrawForLines(selectedDocumentLines);
		NotifySemanticTextChangedIfNeeded(persistedText);
	}

	public void InsertUnOrderList()
	{
		if (CaretNearAttachment() || ReadOnly)
		{
			return;
		}
		string persistedText = GetPersistedText();
		List<DocumentLine> lines = GetSelectedDocumentLines();
		if (lines.Count == 0)
		{
			DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(EditBox.CaretOffset);
			if (lineByOffset != null)
			{
				lines.Add(lineByOffset);
			}
		}
		if (lines.Count == 0 || ContainsCodeBlockLine(lines))
		{
			return;
		}
		bool shouldRemove = lines.All((DocumentLine line) => TryGetSemanticBulletListForLine(line.LineNumber - 1));
		Action action = delegate
		{
			foreach (DocumentLine item in lines)
			{
				SetSemanticBulletListForLine(item.LineNumber - 1, !shouldRemove);
			}
		};
		if (!TryApplyEmptySemanticLineCaretAnchors(lines, shouldRemove, action, persistedText))
		{
			action();
			SyncParagraphIndentMarkersAfterBlockStyleChange();
			RefreshOpenMenuContexts();
			RequestImmediateRedrawForLines(lines);
			NotifySemanticTextChangedIfNeeded(persistedText);
		}
	}

	public void InsertCheckItem()
	{
		if (CaretNearAttachment() || ReadOnly)
		{
			return;
		}
		string persistedText = GetPersistedText();
		List<DocumentLine> lines = GetSelectedDocumentLines();
		if (lines.Count == 0)
		{
			DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(EditBox.CaretOffset);
			if (lineByOffset != null)
			{
				lines.Add(lineByOffset);
			}
		}
		if (lines.Count == 0 || ContainsCodeBlockLine(lines))
		{
			return;
		}
		bool isChecked;
		bool shouldRemove = lines.All((DocumentLine line) => TryGetSemanticTaskListForLine(line.LineNumber - 1, out isChecked));
		Action action = delegate
		{
			foreach (DocumentLine item in lines)
			{
				SetSemanticTaskListForLine(item.LineNumber - 1, !shouldRemove);
			}
		};
		if (!TryApplyEmptySemanticLineCaretAnchors(lines, shouldRemove, action, persistedText))
		{
			action();
			SyncParagraphIndentMarkersAfterBlockStyleChange();
			RefreshOpenMenuContexts();
			RequestImmediateRedrawForLines(lines);
			NotifySemanticTextChangedIfNeeded(persistedText);
		}
	}

	public void InsertNumberedList()
	{
		if (CaretNearAttachment() || ReadOnly)
		{
			return;
		}
		string persistedText = GetPersistedText();
		List<DocumentLine> lines = GetSelectedDocumentLines();
		if (lines.Count == 0)
		{
			DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(EditBox.CaretOffset);
			if (lineByOffset != null)
			{
				lines.Add(lineByOffset);
			}
		}
		if (lines.Count == 0 || ContainsCodeBlockLine(lines))
		{
			return;
		}
		string numberText;
		bool shouldRemove = lines.All((DocumentLine line) => TryGetSemanticOrderedListForLine(line.LineNumber - 1, out numberText));
		Action action = delegate
		{
			int num = 1;
			foreach (DocumentLine item in lines)
			{
				if (shouldRemove)
				{
					SetSemanticOrderedListForLine(item.LineNumber - 1, enabled: false);
				}
				else
				{
					SetSemanticOrderedListForLine(item.LineNumber - 1, enabled: true, num.ToString());
					num++;
				}
			}
			if (!shouldRemove)
			{
				AdjustOrderedListMarkers(lines.Min((DocumentLine line) => line.LineNumber));
			}
		};
		if (!TryApplyEmptySemanticLineCaretAnchors(lines, shouldRemove, action, persistedText))
		{
			action();
			SyncParagraphIndentMarkersAfterBlockStyleChange();
			RefreshOpenMenuContexts();
			RequestImmediateRedrawForLines(lines);
			NotifySemanticTextChangedIfNeeded(persistedText);
		}
	}

	private void SyncParagraphIndentMarkersAfterBlockStyleChange()
	{
		if (EditBox?.Document == null)
		{
			return;
		}
		int caretOffset = EditBox.CaretOffset;
		bool suppressEditBoxTextChanged = _suppressEditBoxTextChanged;
		_suppressEditBoxTextChanged = true;
		try
		{
			if (TrySyncParagraphIndentMarkersWithSemanticState(caretOffset, out var adjustedCaretOffset) && EditBox.SelectionLength == 0)
			{
				EditBox.CaretOffset = adjustedCaretOffset;
			}
		}
		finally
		{
			_suppressEditBoxTextChanged = suppressEditBoxTextChanged;
		}
	}

	public void Indent()
	{
		if ((!string.IsNullOrEmpty(EditBox.SelectedText) && TryHandleCodeBlockSelectionTab(outdent: false)) || (!string.IsNullOrEmpty(EditBox.SelectedText) && TryAdjustSemanticIndentForSelectedLines(4)))
		{
			return;
		}
		if (string.IsNullOrEmpty(EditBox.SelectedText))
		{
			IndentLine();
			return;
		}
		ToggleLinePrefixesInSelection((string _) => new LinePrefixOperation(0, "    "));
		AdjustOrderedListMarkers(GetSelectionStartLine());
	}

	public void DeIndent()
	{
		if ((!string.IsNullOrEmpty(EditBox.SelectedText) && TryHandleCodeBlockSelectionTab(outdent: true)) || (!string.IsNullOrEmpty(EditBox.SelectedText) && TryAdjustSemanticIndentForSelectedLines(-4)))
		{
			return;
		}
		if (string.IsNullOrEmpty(EditBox.SelectedText))
		{
			OutIndentLine();
			return;
		}
		ToggleLinePrefixesInSelection((string lineText) => (!lineText.StartsWith("    ")) ? null : new LinePrefixOperation(4, string.Empty));
		AdjustOrderedListMarkers(GetSelectionStartLine());
	}

	private bool TryHandleCodeBlockSelectionTab(bool outdent)
	{
		if (EditBox?.Document == null || string.IsNullOrEmpty(EditBox.SelectedText))
		{
			return false;
		}
		List<DocumentLine> selectedDocumentLines = GetSelectedDocumentLines();
		if (selectedDocumentLines.Count == 0)
		{
			return false;
		}
		List<LineMutation> list = new List<LineMutation>();
		bool result = false;
		foreach (DocumentLine item in selectedDocumentLines)
		{
			if (item == null)
			{
				continue;
			}
			int num = Math.Max(0, item.LineNumber - 1);
			if (!TryGetSemanticBlockAtLine(num, out var block) || block == null || block.Kind != MarkdownBlockKind.CodeBlock)
			{
				return false;
			}
			if (IsCodeBlockAttachment(block))
			{
				result = true;
			}
			else if (num > block.StartLine && num < block.EndLine)
			{
				string text = EditBox.Document.GetText(item);
				int semanticBlockContentOffset = GetSemanticBlockContentOffset(text, block);
				if (TryGetCodeBlockLineIndentMutation(item.Offset, text, semanticBlockContentOffset, outdent, out var mutation))
				{
					list.Add(mutation);
				}
			}
		}
		if (list.Count == 0)
		{
			return result;
		}
		ApplyLinePrefixMutations(list);
		return true;
	}

	private bool TryAdjustSemanticIndentForSelectedLines(int delta)
	{
		if (delta == 0)
		{
			return false;
		}
		List<DocumentLine> selectedDocumentLines = GetSelectedDocumentLines();
		if (selectedDocumentLines.Count == 0)
		{
			return false;
		}
		List<int> list = new List<int>();
		foreach (DocumentLine item in selectedDocumentLines)
		{
			if (item == null)
			{
				continue;
			}
			int num = Math.Max(0, item.LineNumber - 1);
			if (!string.IsNullOrWhiteSpace(EditBox.Document.GetText(item)))
			{
				if (!TryGetSemanticBlockAtLine(num, out var block) || block == null || (block.Kind != MarkdownBlockKind.List && block.Kind != MarkdownBlockKind.BlockQuote))
				{
					return false;
				}
				list.Add(num);
			}
		}
		if (list.Count == 0)
		{
			return false;
		}
		string persistedText = GetPersistedText();
		foreach (int item2 in from index in list.Distinct()
			orderby index
			select index)
		{
			ApplySemanticIndentDelta(item2, delta, refreshVisual: false);
		}
		AdjustOrderedListMarkers(list.Min() + 1);
		RefreshOpenMenuContexts();
		RequestImmediateRedrawForLines(selectedDocumentLines);
		ForceImmediateBlockRenderAfterEnter();
		NotifySemanticTextChangedIfNeeded(persistedText);
		return true;
	}

	private bool TryInsertLiteralTabIntoParagraph()
	{
		if (EditBox?.Document == null || EditBox.SelectionLength > 0)
		{
			return false;
		}
		DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(EditBox.CaretOffset);
		if (lineByOffset == null || !TryGetSemanticBlockAtLine(Math.Max(0, lineByOffset.LineNumber - 1), out var block) || block == null || block.Kind != MarkdownBlockKind.Paragraph)
		{
			return false;
		}
		InsertText("\t");
		return true;
	}

	private bool TryInsertTabBeforeCodeBlockAttachmentAtCaret()
	{
		bool blockedByMaxLength;
		bool flag = TryNormalizeCaretBeforeCodeBlockAttachment(1, out blockedByMaxLength);
		if (blockedByMaxLength)
		{
			Utils.Toast(Utils.GetString("CharactersNumberLimit"));
			return true;
		}
		if (flag)
		{
			return TryInsertLiteralTabIntoParagraph();
		}
		return false;
	}

	private void IndentLine()
	{
		DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(EditBox.CaretOffset);
		if (lineByOffset != null)
		{
			int lineIndex = Math.Max(0, lineByOffset.LineNumber - 1);
			if (TryGetSemanticBlockAtLine(lineIndex, out var block) && block != null && block.Kind == MarkdownBlockKind.CodeBlock && IsCodeBlockAttachment(block))
			{
				TryInsertTabBeforeCodeBlockAttachmentAtCaret();
				return;
			}
			if (TryGetSemanticBlockAtLine(lineIndex, out block) && block != null && block.Kind == MarkdownBlockKind.Paragraph)
			{
				string persistedText = GetPersistedText();
				SetSemanticParagraphForLine(lineIndex, Math.Max(0, block.Indent) + 4);
				bool suppressEditBoxTextChanged = _suppressEditBoxTextChanged;
				_suppressEditBoxTextChanged = true;
				try
				{
					if (TrySyncParagraphIndentMarkersWithSemanticState(EditBox.CaretOffset, out var adjustedCaretOffset))
					{
						EditBox.SelectionLength = 0;
						EditBox.CaretOffset = adjustedCaretOffset;
					}
				}
				finally
				{
					_suppressEditBoxTextChanged = suppressEditBoxTextChanged;
				}
				RefreshOpenMenuContexts();
				RequestImmediateRedrawForDocumentRange(lineByOffset.Offset, lineByOffset.EndOffset);
				ForceImmediateBlockRenderAfterEnter();
				NotifySemanticTextChangedIfNeeded(persistedText);
				return;
			}
			if (TryGetSemanticBlockAtLine(lineIndex, out block) && (block.Kind == MarkdownBlockKind.List || block.Kind == MarkdownBlockKind.BlockQuote))
			{
				string persistedText2 = GetPersistedText();
				ApplySemanticIndentDelta(lineIndex, 4);
				NotifySemanticTextChangedIfNeeded(persistedText2);
				return;
			}
			ReplaceLinePrefix(lineByOffset.Offset, 0, "    ", EditBox.CaretOffset + 4);
		}
		AdjustOrderedListMarkers(lineByOffset?.LineNumber);
	}

	private void OutIndentLine()
	{
		DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(EditBox.CaretOffset);
		if (lineByOffset != null)
		{
			int lineIndex = Math.Max(0, lineByOffset.LineNumber - 1);
			if (TryGetSemanticBlockAtLine(lineIndex, out var block) && block != null && block.Kind == MarkdownBlockKind.Paragraph)
			{
				string persistedText = GetPersistedText();
				if (TryGetRetreatedSemanticIndent(block, out var nextIndent))
				{
					SetSemanticParagraphForLine(lineIndex, nextIndent);
				}
				bool suppressEditBoxTextChanged = _suppressEditBoxTextChanged;
				_suppressEditBoxTextChanged = true;
				try
				{
					if (TrySyncParagraphIndentMarkersWithSemanticState(EditBox.CaretOffset, out var adjustedCaretOffset))
					{
						EditBox.SelectionLength = 0;
						EditBox.CaretOffset = adjustedCaretOffset;
					}
				}
				finally
				{
					_suppressEditBoxTextChanged = suppressEditBoxTextChanged;
				}
				RefreshOpenMenuContexts();
				RequestImmediateRedrawForDocumentRange(lineByOffset.Offset, lineByOffset.EndOffset);
				ForceImmediateBlockRenderAfterEnter();
				NotifySemanticTextChangedIfNeeded(persistedText);
				return;
			}
			if (TryGetSemanticBlockAtLine(lineIndex, out block) && (block.Kind == MarkdownBlockKind.List || block.Kind == MarkdownBlockKind.BlockQuote))
			{
				string persistedText2 = GetPersistedText();
				ApplySemanticIndentDelta(lineIndex, -4);
				NotifySemanticTextChangedIfNeeded(persistedText2);
				return;
			}
			string text = EditBox.Document.GetText(lineByOffset);
			if (text.StartsWith("    "))
			{
				ReplaceLinePrefix(lineByOffset.Offset, 4, string.Empty, Math.Max(lineByOffset.Offset, EditBox.CaretOffset - 4));
			}
			else if (text.StartsWith("\t"))
			{
				ReplaceLinePrefix(lineByOffset.Offset, 1, string.Empty, Math.Max(lineByOffset.Offset, EditBox.CaretOffset - 1));
			}
		}
		AdjustOrderedListMarkers(lineByOffset?.LineNumber);
	}

	private int GetSelectionStartLine()
	{
		Selection selection = EditBox.TextArea.Selection;
		if (selection == null)
		{
			return EditBox.TextArea.Caret.Line;
		}
		int num = Math.Min(selection.StartPosition.Line, selection.EndPosition.Line);
		if (num != 0)
		{
			return num;
		}
		return EditBox.TextArea.Caret.Line;
	}

	private void ApplySemanticIndentDelta(int lineIndex, int delta, bool refreshVisual = true)
	{
		if (lineIndex < 0 || delta == 0 || !TryGetSemanticBlockAtLine(lineIndex, out var block) || block == null)
		{
			return;
		}
		int value = Math.Max(0, block.Indent + delta);
		switch (block.Kind)
		{
		case MarkdownBlockKind.BlockQuote:
			SetSemanticBlockQuoteForLine(lineIndex, enabled: true, value);
			break;
		case MarkdownBlockKind.Paragraph:
			SetSemanticParagraphForLine(lineIndex, value);
			break;
		case MarkdownBlockKind.List:
			switch (block.ListKind)
			{
			case MarkdownListKind.Task:
				SetSemanticTaskListForLine(lineIndex, enabled: true, block.Checked == true, value);
				break;
			case MarkdownListKind.Numbered:
				SetSemanticOrderedListForLine(lineIndex, enabled: true, string.IsNullOrEmpty(block.CustomOrder) ? "1" : block.CustomOrder, value);
				break;
			default:
				SetSemanticBulletListForLine(lineIndex, enabled: true, value);
				break;
			}
			AdjustOrderedListMarkers(lineIndex + 1);
			break;
		}
		if (refreshVisual)
		{
			RefreshOpenMenuContexts();
			RequestImmediateRedrawAroundCaret();
			ForceImmediateBlockRenderAfterEnter();
		}
	}

	private bool TryRetreatEmptySemanticBlockLine(int lineIndex, MarkdownBlockNode block)
	{
		if (lineIndex < 0 || block == null)
		{
			return false;
		}
		if (!TryGetRetreatedSemanticIndent(block, out var nextIndent))
		{
			return false;
		}
		switch (block.Kind)
		{
		case MarkdownBlockKind.BlockQuote:
			SetSemanticBlockQuoteForLine(lineIndex, enabled: true, nextIndent);
			break;
		case MarkdownBlockKind.Paragraph:
			SetSemanticParagraphForLine(lineIndex, nextIndent);
			break;
		case MarkdownBlockKind.List:
			switch (block.ListKind)
			{
			case MarkdownListKind.Task:
				SetSemanticTaskListForLine(lineIndex, enabled: true, block.Checked == true, nextIndent);
				break;
			case MarkdownListKind.Numbered:
				SetSemanticOrderedListForLine(lineIndex, enabled: true, string.IsNullOrEmpty(block.CustomOrder) ? "1" : block.CustomOrder, nextIndent);
				AdjustOrderedListMarkers(lineIndex + 1);
				break;
			default:
				SetSemanticBulletListForLine(lineIndex, enabled: true, nextIndent);
				break;
			}
			break;
		default:
			return false;
		}
		RefreshOpenMenuContexts();
		bool suppressEditBoxTextChanged = _suppressEditBoxTextChanged;
		_suppressEditBoxTextChanged = true;
		try
		{
			if (TrySyncParagraphIndentMarkersWithSemanticState(EditBox?.CaretOffset ?? 0, out var adjustedCaretOffset))
			{
				EditBox.SelectionLength = 0;
				EditBox.CaretOffset = adjustedCaretOffset;
			}
		}
		finally
		{
			_suppressEditBoxTextChanged = suppressEditBoxTextChanged;
		}
		ForceImmediateBlockRenderAfterEnter();
		return true;
	}

	private bool TryConvertSemanticListToParagraphAtLineStart()
	{
		if (ReadOnly || EditBox?.Document == null || EditBox.SelectionLength > 0)
		{
			return false;
		}
		int num = Math.Max(0, Math.Min(EditBox.CaretOffset, EditBox.Document.TextLength));
		DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(num);
		if (lineByOffset == null)
		{
			return false;
		}
		int lineIndex = Math.Max(0, lineByOffset.LineNumber - 1);
		if (!TryGetSemanticBlockAtLine(lineIndex, out var block) || block == null || block.Kind != MarkdownBlockKind.List)
		{
			return false;
		}
		string text = EditBox.Document.GetText(lineByOffset);
		if (IsEffectivelyEmptySemanticLine(text))
		{
			return false;
		}
		int semanticBlockContentOffset = GetSemanticBlockContentOffset(text, block);
		int num2 = Math.Max(lineByOffset.Offset, Math.Min(lineByOffset.EndOffset, lineByOffset.Offset + semanticBlockContentOffset));
		if (num > num2)
		{
			return false;
		}
		if (!IsEffectivelyEmptySemanticLine((num > lineByOffset.Offset) ? EditBox.Document.GetText(lineByOffset.Offset, num - lineByOffset.Offset) : string.Empty))
		{
			return false;
		}
		string persistedText = GetPersistedText();
		int paragraphIndent = Math.Max(0, block.Indent);
		int num3 = Math.Max(0, Math.Min(CountSemanticIndentCharacters(text, paragraphIndent), Math.Max(0, lineByOffset.Length)));
		if (num3 > 0)
		{
			ApplyDocumentMutationsAndNotify(new DocumentMutation[1]
			{
				new DocumentMutation
				{
					StartOffset = lineByOffset.Offset,
					RemoveLength = num3,
					InsertText = string.Empty
				}
			}, lineByOffset.Offset, delegate
			{
				SetSemanticParagraphForLine(lineIndex, paragraphIndent);
			});
			ForceImmediateBlockRenderAfterEnter();
			NotifySemanticTextChangedIfNeeded(persistedText);
			return true;
		}
		SetSemanticParagraphForLine(lineIndex, paragraphIndent);
		int num4 = num;
		bool suppressEditBoxTextChanged = _suppressEditBoxTextChanged;
		_suppressEditBoxTextChanged = true;
		try
		{
			if (TrySyncParagraphIndentMarkersWithSemanticState(num4, out var adjustedCaretOffset))
			{
				num4 = adjustedCaretOffset;
			}
		}
		finally
		{
			_suppressEditBoxTextChanged = suppressEditBoxTextChanged;
		}
		EditBox.SelectionLength = 0;
		EditBox.CaretOffset = Math.Max(0, Math.Min(num4, EditBox.Text.Length));
		RefreshOpenMenuContexts();
		RequestImmediateRedrawForDocumentRange(lineByOffset.Offset, lineByOffset.EndOffset);
		ForceImmediateBlockRenderAfterEnter();
		NotifySemanticTextChangedIfNeeded(persistedText);
		return true;
	}

	private bool TryConvertSemanticHeadingToParagraphAtLineStart()
	{
		if (ReadOnly || EditBox?.Document == null || EditBox.SelectionLength > 0)
		{
			return false;
		}
		int num = Math.Max(0, Math.Min(EditBox.CaretOffset, EditBox.Document.TextLength));
		DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(num);
		if (lineByOffset == null)
		{
			return false;
		}
		int lineIndex = Math.Max(0, lineByOffset.LineNumber - 1);
		if (!TryGetSemanticBlockAtLine(lineIndex, out var block) || block == null || block.Kind != MarkdownBlockKind.Heading)
		{
			return false;
		}
		string text = EditBox.Document.GetText(lineByOffset);
		if (IsEffectivelyEmptySemanticLine(text))
		{
			return false;
		}
		int semanticBlockContentOffset = GetSemanticBlockContentOffset(text, block);
		int num2 = Math.Max(lineByOffset.Offset, Math.Min(lineByOffset.EndOffset, lineByOffset.Offset + semanticBlockContentOffset));
		if (num > num2)
		{
			return false;
		}
		if (!IsEffectivelyEmptySemanticLine((num > lineByOffset.Offset) ? EditBox.Document.GetText(lineByOffset.Offset, num - lineByOffset.Offset) : string.Empty))
		{
			return false;
		}
		string persistedText = GetPersistedText();
		SetSemanticHeadingLevelForLine(lineIndex, null);
		EditBox.SelectionLength = 0;
		EditBox.CaretOffset = Math.Max(0, Math.Min(num, EditBox.Text.Length));
		RefreshOpenMenuContexts();
		RequestImmediateRedrawForDocumentRange(lineByOffset.Offset, lineByOffset.EndOffset);
		ForceImmediateBlockRenderAfterEnter();
		NotifySemanticTextChangedIfNeeded(persistedText);
		return true;
	}

	private bool TryRetreatParagraphIndentAtLineStart()
	{
		if (EditBox?.Document == null || EditBox.SelectionLength > 0)
		{
			return false;
		}
		int num = Math.Max(0, Math.Min(EditBox.CaretOffset, EditBox.Document.TextLength));
		DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(num);
		if (lineByOffset == null)
		{
			return false;
		}
		int lineIndex = Math.Max(0, lineByOffset.LineNumber - 1);
		if (!TryGetSemanticBlockAtLine(lineIndex, out var block) || block == null || block.Kind != MarkdownBlockKind.Paragraph || block.Indent <= 0)
		{
			return false;
		}
		int semanticBlockContentOffset = GetSemanticBlockContentOffset(EditBox.Document.GetText(lineByOffset), block);
		if (num != lineByOffset.Offset + semanticBlockContentOffset || !TryGetRetreatedSemanticIndent(block, out var nextIndent))
		{
			return false;
		}
		string persistedText = GetPersistedText();
		SetSemanticParagraphForLine(lineIndex, nextIndent);
		int num2 = num;
		bool suppressEditBoxTextChanged = _suppressEditBoxTextChanged;
		_suppressEditBoxTextChanged = true;
		try
		{
			if (TrySyncParagraphIndentMarkersWithSemanticState(num2, out var adjustedCaretOffset))
			{
				num2 = adjustedCaretOffset;
			}
		}
		finally
		{
			_suppressEditBoxTextChanged = suppressEditBoxTextChanged;
		}
		EditBox.SelectionLength = 0;
		EditBox.CaretOffset = Math.Max(0, Math.Min(num2, EditBox.Text.Length));
		RefreshOpenMenuContexts();
		RequestImmediateRedrawForDocumentRange(lineByOffset.Offset, lineByOffset.EndOffset);
		ForceImmediateBlockRenderAfterEnter();
		NotifySemanticTextChangedIfNeeded(persistedText);
		return true;
	}

	private static bool TryGetRetreatedSemanticIndent(MarkdownBlockNode block, out int nextIndent)
	{
		nextIndent = 0;
		if (block == null)
		{
			return false;
		}
		nextIndent = Math.Max(0, block.Indent - 4);
		if (block.Indent <= 0 || nextIndent == block.Indent)
		{
			return false;
		}
		MarkdownBlockKind kind = block.Kind;
		if (kind == MarkdownBlockKind.Paragraph || (uint)(kind - 2) <= 1u)
		{
			return true;
		}
		nextIndent = 0;
		return false;
	}

	private bool TryExitEmptyBlockQuoteLineAtCaret()
	{
		if (EditBox?.Document == null || EditBox.SelectionLength > 0)
		{
			return false;
		}
		int offset = Math.Max(0, Math.Min(EditBox.CaretOffset, EditBox.Document.TextLength));
		DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(offset);
		if (lineByOffset == null)
		{
			return false;
		}
		int lineIndex = Math.Max(0, lineByOffset.LineNumber - 1);
		if (!TryGetSemanticBlockAtLine(lineIndex, out var block) || block == null || block.Kind != MarkdownBlockKind.BlockQuote)
		{
			return false;
		}
		if (!IsEffectivelyEmptySemanticLine(EditBox.Document.GetText(lineByOffset)))
		{
			return false;
		}
		string persistedText = GetPersistedText();
		if (TryRetreatEmptySemanticBlockLine(lineIndex, block))
		{
			NotifySemanticTextChangedIfNeeded(persistedText);
			return true;
		}
		ApplyDocumentMutationsAndNotify(new DocumentMutation[1]
		{
			new DocumentMutation
			{
				StartOffset = lineByOffset.Offset,
				RemoveLength = lineByOffset.Length,
				InsertText = string.Empty
			}
		}, lineByOffset.Offset, delegate
		{
			SetSemanticBlockQuoteForLine(lineIndex, enabled: false);
		});
		NotifySemanticTextChangedIfNeeded(persistedText);
		return true;
	}

	private bool TryRemoveEmptySemanticBlockLineAtCaret(bool backspace)
	{
		if (EditBox?.Document == null || EditBox.SelectionLength > 0)
		{
			return false;
		}
		int caretOffset = EditBox.CaretOffset;
		DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(Math.Max(0, Math.Min(caretOffset, EditBox.Document.TextLength)));
		if (lineByOffset == null)
		{
			return false;
		}
		if (!TryGetSemanticBlockAtLine(Math.Max(0, lineByOffset.LineNumber - 1), out var block) || block == null)
		{
			return false;
		}
		if (!IsEffectivelyEmptySemanticLine(EditBox.Document.GetText(lineByOffset)))
		{
			return false;
		}
		int num = Math.Max(0, lineByOffset.LineNumber - 1);
		if (block.Kind == MarkdownBlockKind.List)
		{
			if (Math.Max(0, block.Indent) <= 0)
			{
				return TryExitTopLevelEmptyListLineAtCaret(lineByOffset, num, block);
			}
			return TryClearEmptyListLineAtCaret(lineByOffset, num, block);
		}
		if (backspace && num == 0 && lineByOffset.NextLine != null)
		{
			return TryRemoveLeadingEmptyLineOnDelete();
		}
		return TryRemoveEmptySemanticBlockLine(lineByOffset, num, block, backspace);
	}

	private bool TryRemoveEmptySemanticBlockLine(DocumentLine line, int lineIndex, MarkdownBlockNode block, bool backspace)
	{
		if (line == null || lineIndex < 0 || block == null || !CanRemoveEmptySemanticBlockLine(block))
		{
			return false;
		}
		int num = line.Offset;
		int num2 = line.Length;
		if (line.DelimiterLength > 0 && !backspace)
		{
			num2 += line.DelimiterLength;
		}
		else if (line.PreviousLine != null && backspace)
		{
			num = line.PreviousLine.EndOffset;
			num2 += line.Offset - line.PreviousLine.EndOffset;
		}
		string persistedText = GetPersistedText();
		int affectedLineNumber = line.LineNumber;
		RemoveSemanticLineAndShiftFollowing(lineIndex);
		ApplyDocumentMutationsAndNotify(new DocumentMutation[1]
		{
			new DocumentMutation
			{
				StartOffset = num,
				RemoveLength = num2,
				InsertText = string.Empty
			}
		}, num, delegate
		{
			if (block.Kind == MarkdownBlockKind.List && block.ListKind == MarkdownListKind.Numbered)
			{
				AdjustOrderedListMarkers(Math.Max(1, affectedLineNumber));
			}
		}, preserveSemanticBlocks: false);
		NotifySemanticTextChangedIfNeeded(persistedText);
		return true;
	}

	private bool TryHandleEmptyListLineDeleteAtCaret(bool backspace)
	{
		if (EditBox?.Document == null || EditBox.SelectionLength > 0)
		{
			return false;
		}
		int offset = Math.Max(0, Math.Min(EditBox.CaretOffset, EditBox.Document.TextLength));
		DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(offset);
		if (lineByOffset == null)
		{
			return false;
		}
		int lineIndex = Math.Max(0, lineByOffset.LineNumber - 1);
		if (!TryGetSemanticBlockAtLine(lineIndex, out var block) || block == null || block.Kind != MarkdownBlockKind.List)
		{
			return false;
		}
		if (!IsEffectivelyEmptySemanticLine(EditBox.Document.GetText(lineByOffset)))
		{
			return false;
		}
		if (Math.Max(0, block.Indent) <= 0)
		{
			return TryExitTopLevelEmptyListLineAtCaret(lineByOffset, lineIndex, block);
		}
		return TryClearEmptyListLineAtCaret(lineByOffset, lineIndex, block);
	}

	private bool TryClearEmptyListLineAtCaret(DocumentLine line, int lineIndex, MarkdownBlockNode block)
	{
		if (line == null || lineIndex < 0 || block == null || block.Kind != MarkdownBlockKind.List)
		{
			return false;
		}
		string persistedText = GetPersistedText();
		ApplyDocumentMutationsAndNotify(new DocumentMutation[1]
		{
			new DocumentMutation
			{
				StartOffset = line.Offset,
				RemoveLength = line.Length,
				InsertText = string.Empty
			}
		}, line.Offset, delegate
		{
			SetSemanticParagraphForLine(lineIndex, 0);
		});
		RequestImmediateRedrawForDocumentRange(line.Offset, line.EndOffset);
		NotifySemanticTextChangedIfNeeded(persistedText);
		return true;
	}

	private bool TryExitTopLevelEmptyListLineAtCaret(DocumentLine line, int lineIndex, MarkdownBlockNode block)
	{
		if (line == null || lineIndex < 0 || block == null || block.Kind != MarkdownBlockKind.List || Math.Max(0, block.Indent) > 0)
		{
			return false;
		}
		string persistedText = GetPersistedText();
		MarkdownListKind? listKind = block.ListKind;
		ApplyDocumentMutationsAndNotify(new DocumentMutation[1]
		{
			new DocumentMutation
			{
				StartOffset = line.Offset,
				RemoveLength = line.Length,
				InsertText = string.Empty
			}
		}, line.Offset, delegate
		{
			if (listKind.HasValue)
			{
				switch (listKind.GetValueOrDefault())
				{
				case MarkdownListKind.Task:
					SetSemanticTaskListForLine(lineIndex, enabled: false);
					return;
				case MarkdownListKind.Numbered:
					SetSemanticOrderedListForLine(lineIndex, enabled: false);
					return;
				}
			}
			SetSemanticBulletListForLine(lineIndex, enabled: false);
		});
		RequestImmediateRedrawForDocumentRange(line.Offset, line.EndOffset);
		NotifySemanticTextChangedIfNeeded(persistedText);
		return true;
	}

	private bool TryRemoveHorizontalRuleLineAtCaret(bool backspace)
	{
		if (EditBox?.Document == null || EditBox.SelectionLength > 0)
		{
			return false;
		}
		int num = Math.Max(0, Math.Min(EditBox.CaretOffset, EditBox.Document.TextLength));
		DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(num);
		if (lineByOffset == null)
		{
			return false;
		}
		if (TryRemoveHorizontalRuleLineAtOffset(lineByOffset, num, backspace))
		{
			return true;
		}
		if (backspace && num == lineByOffset.Offset && IsHorizontalRuleLine(lineByOffset.PreviousLine))
		{
			MoveCaretToOffset(lineByOffset.PreviousLine.EndOffset);
			return true;
		}
		if (!backspace && num == lineByOffset.EndOffset && IsHorizontalRuleLine(lineByOffset.NextLine))
		{
			MoveCaretToOffset(lineByOffset.NextLine.Offset);
			return true;
		}
		return false;
	}

	private bool TryRemoveHorizontalRuleLineAtOffset(DocumentLine line, int caretOffset, bool backspace)
	{
		if (line == null || !IsHorizontalRuleLine(line))
		{
			return false;
		}
		if (backspace && caretOffset <= line.Offset)
		{
			return false;
		}
		if (!backspace && caretOffset >= line.EndOffset)
		{
			return false;
		}
		int lineIndex = Math.Max(0, line.LineNumber - 1);
		int num = line.Offset;
		int removeLength = line.Length + line.DelimiterLength;
		if (line.DelimiterLength <= 0 && line.PreviousLine != null)
		{
			num = line.PreviousLine.EndOffset;
			removeLength = line.Offset - line.PreviousLine.EndOffset + line.Length;
		}
		RemoveSemanticLineAndShiftFollowing(lineIndex);
		ApplyDocumentMutationsAndNotify(new DocumentMutation[1]
		{
			new DocumentMutation
			{
				StartOffset = num,
				RemoveLength = removeLength,
				InsertText = string.Empty
			}
		}, num, null, preserveSemanticBlocks: false);
		return true;
	}

	private void MoveCaretToOffset(int offset)
	{
		if (EditBox?.Document != null)
		{
			EditBox.SelectionLength = 0;
			EditBox.CaretOffset = Math.Max(0, Math.Min(offset, EditBox.Document.TextLength));
			NormalizeCaretIntoSemanticContentIfNeeded();
		}
	}

	private bool IsHorizontalRuleLine(DocumentLine line)
	{
		if (line == null)
		{
			return false;
		}
		int lineIndex = Math.Max(0, line.LineNumber - 1);
		if (TryGetSemanticBlockAtLine(lineIndex, out var block) && block != null)
		{
			return block.Kind == MarkdownBlockKind.HorizontalRule;
		}
		string text = EditBox?.Document?.GetText(line) ?? string.Empty;
		if (!string.Equals(text.Trim(), "\u200c\u200d\u2060", StringComparison.Ordinal))
		{
			return MarkdownBlockSyntax.IsSplitLine(text);
		}
		return true;
	}

	private static bool CanRemoveEmptySemanticBlockLine(MarkdownBlockNode block)
	{
		if (block != null && block.Kind != MarkdownBlockKind.CodeBlock)
		{
			return block.Kind != MarkdownBlockKind.HorizontalRule;
		}
		return false;
	}

	private bool TryRemoveSemanticLineBreakAtCaret(bool backspace)
	{
		if (EditBox?.Document == null || EditBox.SelectionLength > 0)
		{
			return false;
		}
		int num = Math.Max(0, Math.Min(EditBox.CaretOffset, EditBox.Document.TextLength));
		DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(num);
		if (lineByOffset == null)
		{
			return false;
		}
		if (TryKeepCodeBlockStructuralBoundaryAtCaret(backspace))
		{
			return false;
		}
		int num2 = Math.Max(0, lineByOffset.LineNumber - 1);
		if (TryGetSemanticBlockAtLine(num2, out var block) && block != null && block.Kind == MarkdownBlockKind.CodeBlock)
		{
			return false;
		}
		int num3 = -1;
		int num4 = 0;
		int num5 = -1;
		if (backspace)
		{
			if (num != lineByOffset.Offset || lineByOffset.PreviousLine == null)
			{
				return false;
			}
			DocumentLine previousLine = lineByOffset.PreviousLine;
			string text = EditBox.Document.GetText(previousLine);
			int num6 = Math.Max(0, previousLine.LineNumber - 1);
			MarkdownBlockNode block2;
			bool flag = TryGetSemanticBlockAtLine(num6, out block2) && block2 != null && block2.Kind == MarkdownBlockKind.List;
			if (IsEffectivelyEmptySemanticLine(text) && !flag)
			{
				num3 = previousLine.Offset;
				num4 = lineByOffset.Offset - previousLine.Offset;
				num5 = num6;
			}
			else
			{
				num3 = previousLine.EndOffset;
				num4 = lineByOffset.Offset - previousLine.EndOffset;
				num5 = Math.Max(0, lineByOffset.LineNumber - 1);
			}
		}
		else
		{
			if (num != lineByOffset.EndOffset || lineByOffset.DelimiterLength <= 0)
			{
				return false;
			}
			if (IsEffectivelyEmptySemanticLine(EditBox.Document.GetText(lineByOffset)))
			{
				num3 = lineByOffset.Offset;
				num4 = lineByOffset.Length + lineByOffset.DelimiterLength;
				num5 = num2;
			}
			else
			{
				num3 = lineByOffset.EndOffset;
				num4 = lineByOffset.DelimiterLength;
				num5 = Math.Max(0, lineByOffset.LineNumber);
			}
		}
		if (num3 < 0 || num4 <= 0 || num5 < 0)
		{
			return false;
		}
		string persistedText = GetPersistedText();
		int value = Math.Max(1, backspace ? (lineByOffset.LineNumber - 1) : lineByOffset.LineNumber);
		RemoveSemanticLineAndShiftFollowing(num5);
		AdjustOrderedListMarkers(value);
		_seedSemanticDocumentForRefresh = CloneSemanticDocument(_semanticDocument);
		ApplyDocumentMutationsAndNotify(new DocumentMutation[1]
		{
			new DocumentMutation
			{
				StartOffset = num3,
				RemoveLength = num4,
				InsertText = string.Empty
			}
		}, num3, null, preserveSemanticBlocks: false);
		NotifySemanticTextChangedIfNeeded(persistedText);
		return true;
	}

	private bool TryKeepCodeBlockStructuralBoundaryAtCaret(bool backspace)
	{
		if (EditBox?.Document == null || EditBox.SelectionLength > 0)
		{
			return false;
		}
		int num = Math.Max(0, Math.Min(EditBox.CaretOffset, EditBox.Document.TextLength));
		DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(num);
		if (lineByOffset == null)
		{
			return false;
		}
		int num2 = Math.Max(0, lineByOffset.LineNumber - 1);
		if (!TryGetSemanticBlockAtLine(num2, out var block) || block == null || block.Kind != MarkdownBlockKind.CodeBlock)
		{
			return false;
		}
		string text = EditBox.Document.GetText(lineByOffset);
		if (block.EndLine == block.StartLine + 2 && num2 == block.StartLine + 1 && string.IsNullOrEmpty(block.CodeText))
		{
			return false;
		}
		if (backspace)
		{
			if (num2 == block.StartLine && IsSemanticCodeBlockOpeningFenceLine(text, block, num2) && num <= lineByOffset.Offset)
			{
				return true;
			}
			if (num2 == block.EndLine && IsSemanticCodeBlockClosingFenceLine(text, block, num2) && num <= lineByOffset.Offset)
			{
				return true;
			}
			if (num2 == block.StartLine + 1 && IsSemanticCodeBlockContentLine(text, block, num2) && TryGetCodeBlockEditableOffset(lineByOffset, num2, block, out var targetOffset))
			{
				return num == targetOffset;
			}
			return false;
		}
		if (num2 == block.StartLine && IsSemanticCodeBlockOpeningFenceLine(text, block, num2) && num >= lineByOffset.EndOffset)
		{
			return true;
		}
		if (num2 == block.EndLine && IsSemanticCodeBlockClosingFenceLine(text, block, num2) && num >= lineByOffset.EndOffset)
		{
			return true;
		}
		if (num2 == GetLastCodeBlockContentLineIndex(block) && IsSemanticCodeBlockContentLine(text, block, num2))
		{
			return num == lineByOffset.EndOffset;
		}
		return false;
	}

	private bool TryRemoveCodeBlockAtBoundary(bool backspace)
	{
		if (EditBox?.Document == null || EditBox.SelectionLength > 0)
		{
			return false;
		}
		int num = Math.Max(0, Math.Min(EditBox.CaretOffset, EditBox.Document.TextLength));
		DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(num);
		if (lineByOffset == null)
		{
			return false;
		}
		int lineIndex = Math.Max(0, lineByOffset.LineNumber - 1);
		if (!TryGetSemanticBlockAtLine(lineIndex, out var block) || block == null)
		{
			return TryRemoveAdjacentCodeBlockAtBoundary(lineByOffset, lineIndex, num, backspace);
		}
		bool atBoundary = IsCaretAtSemanticCodeBlockBoundary(EditBox.Document.GetText(lineByOffset), block, lineIndex, lineByOffset.Offset, lineByOffset.EndOffset, num, backspace);
		bool hasClosingFenceLine = false;
		if (block.EndLine >= 0 && block.EndLine < EditBox.Document.LineCount)
		{
			DocumentLine lineByNumber = EditBox.Document.GetLineByNumber(block.EndLine + 1);
			if (lineByNumber != null)
			{
				hasClosingFenceLine = IsSemanticCodeBlockClosingFenceLine(EditBox.Document.GetText(lineByNumber), block, block.EndLine);
			}
		}
		if (!TryGetCodeBlockDeletionLineRange(block, lineIndex, backspace, atBoundary, hasClosingFenceLine, out var startLineIndex, out var endLineIndex))
		{
			return TryRemoveAdjacentCodeBlockAtBoundary(lineByOffset, lineIndex, num, backspace);
		}
		return TryRemoveCodeBlockLineRange(startLineIndex, endLineIndex);
	}

	private bool TryRemoveEmptyCodeBlockContentLineBreakAtCaret(bool backspace)
	{
		if (EditBox?.Document == null || EditBox.SelectionLength > 0)
		{
			return false;
		}
		TextDocument document = EditBox.Document;
		int num = Math.Max(0, Math.Min(EditBox.CaretOffset, document.TextLength));
		DocumentLine lineByOffset = document.GetLineByOffset(num);
		if (lineByOffset == null)
		{
			return false;
		}
		int num2 = Math.Max(0, lineByOffset.LineNumber - 1);
		string text = document.GetText(lineByOffset);
		if (!TryGetSemanticBlockAtLine(num2, out var block) || block == null || block.Kind != MarkdownBlockKind.CodeBlock || num2 <= block.StartLine || num2 >= block.EndLine || block.EndLine - block.StartLine <= 2 || !IsEffectivelyEmptySemanticLine(text))
		{
			return false;
		}
		int num3 = -1;
		int num4 = 0;
		if (backspace)
		{
			if (num != lineByOffset.Offset || num2 <= block.StartLine + 1 || lineByOffset.PreviousLine == null)
			{
				return false;
			}
			num3 = lineByOffset.PreviousLine.EndOffset;
			num4 = lineByOffset.Offset - num3;
		}
		else
		{
			if (num != lineByOffset.EndOffset || num2 >= block.EndLine - 1 || lineByOffset.DelimiterLength <= 0)
			{
				return false;
			}
			num3 = lineByOffset.EndOffset;
			num4 = lineByOffset.DelimiterLength;
		}
		if (num3 < 0 || num4 <= 0)
		{
			return false;
		}
		string persistedText = GetPersistedText();
		ApplyDocumentMutationsAndNotify(new DocumentMutation[1]
		{
			new DocumentMutation
			{
				StartOffset = num3,
				RemoveLength = num4,
				InsertText = string.Empty
			}
		}, num3, null, preserveSemanticBlocks: true, refreshEditingState: true, notifyTextChanged: false);
		NotifySemanticTextChangedIfNeeded(persistedText);
		return true;
	}

	private bool TryRemoveAdjacentCodeBlockAtBoundary(DocumentLine line, int lineIndex, int caretOffset, bool backspace)
	{
		if (line == null)
		{
			return false;
		}
		if (backspace)
		{
			if (!TryGetAdjacentCodeBlockDeletionLineRange(GetSemanticLineSnapshotForCurrentDocument(), lineIndex, backspace, caretOffset == line.Offset, out var startLineIndex, out var endLineIndex))
			{
				return false;
			}
			return TryRemoveCodeBlockLineRange(startLineIndex, endLineIndex);
		}
		if (!TryGetAdjacentCodeBlockDeletionLineRange(GetSemanticLineSnapshotForCurrentDocument(), lineIndex, backspace, caretOffset == line.EndOffset, out var startLineIndex2, out var endLineIndex2))
		{
			return false;
		}
		return TryRemoveCodeBlockLineRange(startLineIndex2, endLineIndex2);
	}

	private bool TryRemoveCodeBlockLineRange(int startLineIndex, int endLineIndex)
	{
		if (EditBox?.Document == null)
		{
			return false;
		}
		int num = Math.Max(1, startLineIndex + 1);
		int number = Math.Max(num, Math.Min(endLineIndex + 1, EditBox.Document.LineCount));
		DocumentLine lineByNumber = EditBox.Document.GetLineByNumber(num);
		DocumentLine lineByNumber2 = EditBox.Document.GetLineByNumber(number);
		if (lineByNumber == null || lineByNumber2 == null)
		{
			return false;
		}
		int offset = lineByNumber.Offset;
		bool flag = lineByNumber2.DelimiterLength > 0 && lineByNumber2.NextLine != null;
		int num2 = Math.Min(EditBox.Document.TextLength, lineByNumber2.EndOffset + ((!flag) ? lineByNumber2.DelimiterLength : 0));
		if (num2 <= offset)
		{
			return false;
		}
		string persistedText = GetPersistedText();
		int removeLength = num2 - offset;
		int num3 = Math.Max(0, endLineIndex - startLineIndex + 1);
		int semanticRemovedLineCount = Math.Max(0, num3 - (flag ? 1 : 0));
		ApplyDocumentMutationsAndNotify(new DocumentMutation[1]
		{
			new DocumentMutation
			{
				StartOffset = offset,
				RemoveLength = removeLength,
				InsertText = string.Empty
			}
		}, offset, delegate
		{
			RemoveSemanticRangeAndShiftFollowing(startLineIndex, endLineIndex, semanticRemovedLineCount);
			_seedSemanticDocumentForRefresh = CloneSemanticDocument(_semanticDocument);
		}, preserveSemanticBlocks: false, refreshEditingState: true, notifyTextChanged: false);
		NotifySemanticTextChangedIfNeeded(persistedText);
		return true;
	}

	private IReadOnlyList<MarkdownBlockNode> GetSemanticLineSnapshotForCurrentDocument()
	{
		int valueOrDefault = (EditBox?.Document?.LineCount).GetValueOrDefault();
		if (valueOrDefault <= 0)
		{
			return new MarkdownBlockNode[0];
		}
		MarkdownBlockNode[] array = new MarkdownBlockNode[valueOrDefault];
		for (int i = 0; i < valueOrDefault; i++)
		{
			TryGetSemanticBlockAtLine(i, out array[i]);
		}
		return array;
	}

	private static bool IsCaretAtSemanticCodeBlockBoundary(string lineText, MarkdownBlockNode block, int lineIndex, int lineOffset, int lineEndOffset, int caretOffset, bool backspace)
	{
		if (block == null || block.Kind != MarkdownBlockKind.CodeBlock)
		{
			return false;
		}
		if (backspace)
		{
			if (lineIndex == block.StartLine && IsSemanticCodeBlockOpeningFenceLine(lineText, block, lineIndex))
			{
				return caretOffset == lineOffset;
			}
			if (lineIndex == block.EndLine && IsSemanticCodeBlockClosingFenceLine(lineText, block, lineIndex))
			{
				return caretOffset == lineOffset;
			}
			int semanticContentStartOffset = GetSemanticContentStartOffset(lineText, block, lineOffset, lineEndOffset);
			return caretOffset == semanticContentStartOffset;
		}
		return caretOffset == lineEndOffset;
	}

	private double GetSelectionRightPaddingForVisualLine(VisualLine visualLine)
	{
		DocumentLine documentLine = visualLine?.FirstDocumentLine;
		if (documentLine == null || EditBox?.Document == null || !TryGetSemanticBlockAtOffset(documentLine.Offset, out var block) || block == null || block.Kind != MarkdownBlockKind.CodeBlock || !CodeBlockLineClassifier.IsContentLine(EditBox.Document, documentLine, block))
		{
			return 0.0;
		}
		return 23.0;
	}

	public void ShowInsertLink(LinkLineInfoModel model)
	{
		if (ReadOnly || CaretNearAttachment())
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
		InsertLink(model);
	}

	public void ShowInsertLinkForCurrentContext()
	{
		LinkLineInfoModel linkLineInfoModel = TryGetCurrentLinkLineInfo();
		if (linkLineInfoModel == null)
		{
			linkLineInfoModel = new LinkLineInfoModel
			{
				Text = EditBox.SelectedText
			};
		}
		ShowInsertLink(linkLineInfoModel);
	}

	private LinkLineInfoModel TryGetCurrentLinkLineInfo()
	{
		return MarkdownLinkContextHelper.TryCreateCurrentLinkLineInfo(EditBox.SelectionStart, EditBox.SelectionLength, EditBox.CaretOffset, LinkNameDict, LinkUrlDict, (int offset) => EditBox.Document?.GetLineByOffset(Math.Max(0, Math.Min(offset, EditBox.Text.Length)))?.LineNumber, (int lineNumber) => EditBox.TextArea.TextView.GetVisualLine(lineNumber));
	}

	private async void InsertLink(LinkLineInfoModel model)
	{
		if (string.IsNullOrEmpty(model.Text) || !model.Text.Contains("\n"))
		{
			this.LinkPopupOpened?.Invoke(this, null);
			Point startPosition = GetStartPosition(model.VisualLine, 320, 210);
			_linkWindow = new InsertLinkWindow(this, model)
			{
				Left = startPosition.X + 10.0,
				Top = startPosition.Y + 10.0,
				Owner = Window.GetWindow(this)
			};
			_linkWindow.Closed -= OnLinkClosed;
			_linkWindow.Closed += OnLinkClosed;
			_linkWindow.ShowDialog();
		}
	}

	private void OnLinkClosed(object sender, EventArgs e)
	{
		this.LinkPopupClosed?.Invoke(sender, e);
	}

	private void EditBoxOnTextChanged(object sender, EventArgs eventArgs)
	{
		if (!_suppressEditBoxTextChanged)
		{
			MarkLocalDocumentChangePending();
			string persistedText = OnEditBoxTextChanged();
			if (!_deferBindingTextSyncToSemanticNotification)
			{
				UpdateBindingTextFromEditor(persistedText);
			}
		}
	}

	private string OnEditBoxTextChanged(bool captureHistory = true, bool scheduleTextChanged = true)
	{
		_searchRender?.SetTextChanged(b: true);
		SpellCheckProvider?.SetTextChanged(change: true);
		string persistedText;
		bool hadPendingInput;
		bool flag = TryApplyPendingPlainParagraphInput(out persistedText, out hadPendingInput);
		if (flag)
		{
			_textChangeHandler.TryDoAction(350);
		}
		else if (ShouldDeferExpensiveTextRefresh(EditBox.LineCount, EditBox.Text?.Length ?? 0))
		{
			if (hadPendingInput)
			{
				_incrementalPersistenceState = null;
			}
			if (_requiresImmediateDerivedMetadataRefresh)
			{
				RefreshEditingDerivedMetadataWithoutMarkdownParsing();
			}
			_textChangeHandler.TryDoAction(250);
		}
		else
		{
			if (hadPendingInput)
			{
				_incrementalPersistenceState = null;
			}
			HandleOnTextChanged();
		}
		if (_needsTypedSemanticMarkRedraw)
		{
			RequestImmediateRedrawAroundCaret();
			_needsTypedSemanticMarkRedraw = false;
		}
		if (scheduleTextChanged)
		{
			_scheduledTextChangeTaskId = _editingTaskId;
			_textChangeNotifyHandler.TryDoAction();
		}
		if (captureHistory)
		{
			string text = (flag ? persistedText : GetPersistedText());
			CaptureSemanticHistorySnapshotWithPersistedText(text);
			return text;
		}
		if (!flag)
		{
			return null;
		}
		return persistedText;
	}

	private void QueuePlainParagraphInput(int startOffset, int removedLength, string insertedText)
	{
		_pendingPlainParagraphInput = null;
		if (CanUsePlainParagraphIncrementalPath(startOffset, removedLength, insertedText, out var lineIndex))
		{
			string text = EditBox.Text ?? string.Empty;
			IncrementalPersistenceState incrementalPersistenceState = _incrementalPersistenceState;
			if (incrementalPersistenceState == null || !string.Equals(incrementalPersistenceState.DisplayText, text, StringComparison.Ordinal) || !SemanticBlockStampsMatch(incrementalPersistenceState.SemanticBlocks))
			{
				int[] displayOffsetMap = new int[text.Length + 1];
				string text2 = BuildPersistedText(ref displayOffsetMap);
				incrementalPersistenceState = (_incrementalPersistenceState = new IncrementalPersistenceState
				{
					DisplayText = text,
					PersistedText = (text2 ?? string.Empty),
					DisplayToPersistedOffsets = displayOffsetMap,
					SemanticBlocks = CaptureSemanticBlockPersistenceStamps()
				});
			}
			_pendingPlainParagraphInput = new PendingPlainParagraphInput
			{
				DisplayTextBeforeInput = incrementalPersistenceState.DisplayText,
				PersistedTextBeforeInput = incrementalPersistenceState.PersistedText,
				DisplayToPersistedOffsets = incrementalPersistenceState.DisplayToPersistedOffsets,
				StartOffset = startOffset,
				InsertedText = insertedText,
				LineIndex = lineIndex,
				SemanticBlocks = incrementalPersistenceState.SemanticBlocks
			};
		}
	}

	private bool TryApplyPendingPlainParagraphInput(out string persistedText, out bool hadPendingInput)
	{
		persistedText = null;
		PendingPlainParagraphInput pendingPlainParagraphInput = _pendingPlainParagraphInput;
		_pendingPlainParagraphInput = null;
		hadPendingInput = pendingPlainParagraphInput != null;
		if (pendingPlainParagraphInput == null || EditBox?.Document == null || string.IsNullOrEmpty(pendingPlainParagraphInput.InsertedText) || !SemanticBlockStampsMatch(pendingPlainParagraphInput.SemanticBlocks))
		{
			return false;
		}
		string text = EditBox.Text ?? string.Empty;
		int num = Math.Max(0, Math.Min(pendingPlainParagraphInput.StartOffset, pendingPlainParagraphInput.DisplayTextBeforeInput.Length));
		string b = pendingPlainParagraphInput.DisplayTextBeforeInput.Insert(num, pendingPlainParagraphInput.InsertedText);
		if (!string.Equals(text, b, StringComparison.Ordinal) || pendingPlainParagraphInput.DisplayToPersistedOffsets == null || pendingPlainParagraphInput.DisplayToPersistedOffsets.Length != pendingPlainParagraphInput.DisplayTextBeforeInput.Length + 1 || !TryGetSemanticBlockAtLine(pendingPlainParagraphInput.LineIndex, out var block) || block == null || block.Kind != MarkdownBlockKind.Paragraph || block.StartLine != pendingPlainParagraphInput.LineIndex || block.EndLine != pendingPlainParagraphInput.LineIndex || Math.Max(0, block.Indent) != 0)
		{
			return false;
		}
		DocumentLine lineByNumber = EditBox.Document.GetLineByNumber(pendingPlainParagraphInput.LineIndex + 1);
		if (lineByNumber == null)
		{
			return false;
		}
		string text2 = EditBox.Document.GetText(lineByNumber);
		block.Inlines.Clear();
		if (!IsEffectivelyEmptySemanticLine(text2))
		{
			block.Inlines.Add(new MarkdownInlineNode
			{
				Text = text2,
				SourceStart = 0,
				SourceLength = text2.Length
			});
		}
		int val = pendingPlainParagraphInput.DisplayToPersistedOffsets[num];
		val = Math.Max(0, Math.Min(val, pendingPlainParagraphInput.PersistedTextBeforeInput.Length));
		persistedText = pendingPlainParagraphInput.PersistedTextBeforeInput.Insert(val, pendingPlainParagraphInput.InsertedText);
		int[] displayToPersistedOffsets = InsertPlainTextIntoDisplayToPersistedOffsets(pendingPlainParagraphInput.DisplayToPersistedOffsets, num, pendingPlainParagraphInput.InsertedText.Length, text.Length);
		_incrementalPersistenceState = new IncrementalPersistenceState
		{
			DisplayText = text,
			PersistedText = persistedText,
			DisplayToPersistedOffsets = displayToPersistedOffsets,
			SemanticBlocks = CaptureSemanticBlockPersistenceStamps()
		};
		ClearLegacyAstStateForEditing();
		LogCodeRects(text);
		InvalidMargin();
		TryShowQuickSetPopup();
		TryShowStyleMenu();
		RefreshOpenMenuContexts();
		RequestSafeRedraw();
		return true;
	}

	private bool CanUsePlainParagraphIncrementalPath(int startOffset, int removedLength, string insertedText, out int lineIndex)
	{
		lineIndex = -1;
		if (EditBox?.Document == null || removedLength != 0 || string.IsNullOrEmpty(insertedText) || !IsSafePlainParagraphInput(insertedText) || LinkNameDict.Count != 0 || LinkUrlDict.Count != 0 || AttachmentDict.Count != 0 || AttachmentEndSet.Count != 0 || EmojiDict.Count != 0 || SemanticMarkList.Count != 0 || _typedSemanticMarks.Count != 0)
		{
			return false;
		}
		TextDocument document = EditBox.Document;
		int offset = Math.Max(0, Math.Min(startOffset, document.TextLength));
		DocumentLine lineByOffset = document.GetLineByOffset(offset);
		lineIndex = Math.Max(0, (lineByOffset?.LineNumber ?? 1) - 1);
		if (lineByOffset != null && TryGetSemanticBlockAtLine(lineIndex, out var block) && block != null && block.Kind == MarkdownBlockKind.Paragraph && block.StartLine == lineIndex && block.EndLine == lineIndex)
		{
			return Math.Max(0, block.Indent) == 0;
		}
		return false;
	}

	private static bool IsSafePlainParagraphInput(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		foreach (char c in text)
		{
			if (c == '\r' || c == '\n' || c == '\\' || c == '`' || c == '*' || c == '_' || c == '~' || c == '[' || c == ']' || c == '(' || c == ')' || c == '!' || c == '#' || c == '>' || c == '+' || c == '-' || c == '=' || c == '|')
			{
				return false;
			}
		}
		return true;
	}

	private static int[] InsertPlainTextIntoDisplayToPersistedOffsets(int[] previousOffsets, int insertionOffset, int insertedLength, int newDisplayLength)
	{
		int num = Math.Max(0, insertedLength);
		int val = Math.Max(0, newDisplayLength - num);
		int[] array = new int[newDisplayLength + 1];
		int num2 = Math.Max(0, Math.Min(insertionOffset, val));
		for (int i = 0; i <= num2; i++)
		{
			array[i] = previousOffsets[Math.Min(i, previousOffsets.Length - 1)];
		}
		int num3 = array[num2];
		for (int j = 1; j <= num; j++)
		{
			array[num2 + j] = num3 + j;
		}
		for (int k = num2 + num; k <= newDisplayLength; k++)
		{
			int val2 = k - num;
			array[k] = previousOffsets[Math.Min(val2, previousOffsets.Length - 1)] + num;
		}
		return array;
	}

	private List<SemanticBlockPersistenceStamp> CaptureSemanticBlockPersistenceStamps()
	{
		return (from block in GetSemanticBlocks()
			where block != null
			select new SemanticBlockPersistenceStamp
			{
				Kind = block.Kind,
				StartLine = block.StartLine,
				EndLine = block.EndLine,
				Indent = block.Indent,
				Level = block.Level,
				ListKind = block.ListKind,
				Checked = block.Checked,
				Folded = block.Folded,
				SoftTab = block.SoftTab,
				CustomOrder = block.CustomOrder,
				Language = block.Language,
				CodeText = block.CodeText,
				CodeBlockAttachmentIdentity = block.CodeBlockAttachmentIdentity,
				FileKind = block.FileKind,
				Source = block.Source,
				AltText = block.AltText,
				Caption = block.Caption
			}).ToList();
	}

	private bool SemanticBlockStampsMatch(IReadOnlyList<SemanticBlockPersistenceStamp> stamps)
	{
		List<MarkdownBlockNode> list = (from block in GetSemanticBlocks()
			where block != null
			select block).ToList();
		if (stamps == null || stamps.Count != list.Count)
		{
			return false;
		}
		for (int num = 0; num < stamps.Count; num++)
		{
			SemanticBlockPersistenceStamp semanticBlockPersistenceStamp = stamps[num];
			MarkdownBlockNode markdownBlockNode = list[num];
			if (semanticBlockPersistenceStamp == null || semanticBlockPersistenceStamp.Kind != markdownBlockNode.Kind || semanticBlockPersistenceStamp.StartLine != markdownBlockNode.StartLine || semanticBlockPersistenceStamp.EndLine != markdownBlockNode.EndLine || semanticBlockPersistenceStamp.Indent != markdownBlockNode.Indent || semanticBlockPersistenceStamp.Level != markdownBlockNode.Level || semanticBlockPersistenceStamp.ListKind != markdownBlockNode.ListKind || semanticBlockPersistenceStamp.Checked != markdownBlockNode.Checked || semanticBlockPersistenceStamp.Folded != markdownBlockNode.Folded || semanticBlockPersistenceStamp.SoftTab != markdownBlockNode.SoftTab || (object)semanticBlockPersistenceStamp.CustomOrder != markdownBlockNode.CustomOrder || (object)semanticBlockPersistenceStamp.Language != markdownBlockNode.Language || (object)semanticBlockPersistenceStamp.CodeText != markdownBlockNode.CodeText || (object)semanticBlockPersistenceStamp.CodeBlockAttachmentIdentity != markdownBlockNode.CodeBlockAttachmentIdentity || semanticBlockPersistenceStamp.FileKind != markdownBlockNode.FileKind || (object)semanticBlockPersistenceStamp.Source != markdownBlockNode.Source || (object)semanticBlockPersistenceStamp.AltText != markdownBlockNode.AltText || (object)semanticBlockPersistenceStamp.Caption != markdownBlockNode.Caption)
			{
				return false;
			}
		}
		return true;
	}

	internal static bool ShouldDeferExpensiveTextRefresh(int lineCount, int textLength)
	{
		if (lineCount < 200)
		{
			return textLength >= 4096;
		}
		return true;
	}

	private void OnEditDocumentChanging(object sender, DocumentChangeEventArgs e)
	{
		_imageElementGenerator?.RecordDocumentChange(e, _suppressEditBoxTextChanged || _suppressExternalTextChanged);
		if (!_suppressEditBoxTextChanged && !_suppressExternalTextChanged && e != null)
		{
			BeginSearchDetailEditingIfNeeded();
		}
	}

	private void BeginSearchDetailEditingIfNeeded()
	{
		if (!SearchHelper.DetailChanged)
		{
			SearchHighlightBackgroundRender searchRender = _searchRender;
			if (searchRender != null && searchRender.InDetail)
			{
				PromoteSearchHeadingDisplayFoldOverridesForEditing();
				SearchHelper.DetailChanged = true;
			}
		}
	}

	private void OnEditDocumentChanged(object sender, DocumentChangeEventArgs e)
	{
		if (!_suppressEditBoxTextChanged && e != null && sender == EditBox?.Document)
		{
			TextEditor editBox = EditBox;
			if (editBox != null && editBox.TextArea?.IsImeCompositionDocumentUpdate == true && EditBox.TextArea.ImeCompositionStartOffset == e.Offset)
			{
				AdjustInlineSemanticMarksForImeCompositionMutation(e);
			}
		}
		if (!_suppressEditBoxTextChanged && e != null && EditBox?.Document != null && ShouldDeferExpensiveTextRefresh(EditBox.Document.LineCount, EditBox.Document.TextLength) && HasPositionSensitiveDerivedVisuals() && !_requiresImmediateDerivedMetadataRefresh)
		{
			if (DocumentChangeIntersectsDerivedVisual(e))
			{
				_requiresImmediateDerivedMetadataRefresh = true;
			}
			else
			{
				ShiftDerivedVisualOffsets(e);
			}
		}
	}

	private bool HasPositionSensitiveDerivedVisuals()
	{
		if (LinkNameDict.Count == 0 && LinkUrlDict.Count == 0 && AttachmentDict.Count == 0 && AttachmentEndSet.Count == 0)
		{
			return EmojiDict.Count != 0;
		}
		return true;
	}

	private bool DocumentChangeIntersectsDerivedVisual(DocumentChangeEventArgs change)
	{
		if (!AttachmentDict.Any((KeyValuePair<int, AttachmentInfo> pair) => ChangeIntersectsSpan(change, pair.Key, (pair.Value?.Value?.Length).GetValueOrDefault())) && !LinkNameDict.Any((KeyValuePair<int, LinkInfo> pair) => ChangeIntersectsSpan(change, pair.Key, (pair.Value?.Link?.Length).GetValueOrDefault())) && !LinkUrlDict.Any((KeyValuePair<int, LinkInfo> pair) => ChangeIntersectsSpan(change, pair.Key, (pair.Value?.Url?.Length).GetValueOrDefault())))
		{
			return EmojiDict.Any((KeyValuePair<int, string> pair) => ChangeIntersectsSpan(change, pair.Key, pair.Value?.Length ?? 0));
		}
		return true;
	}

	private static bool ChangeIntersectsSpan(DocumentChangeEventArgs change, int spanStart, int spanLength)
	{
		int num = Math.Max(0, spanLength);
		int num2 = spanStart + num;
		int num3 = change.Offset + change.RemovalLength;
		if (change.RemovalLength > 0 && change.Offset < num2 && num3 > spanStart)
		{
			return true;
		}
		if (change.InsertionLength > 0 && change.Offset > spanStart)
		{
			return change.Offset < num2;
		}
		return false;
	}

	private void ShiftDerivedVisualOffsets(DocumentChangeEventArgs change)
	{
		List<KeyValuePair<int, LinkInfo>> list = LinkNameDict.Select((KeyValuePair<int, LinkInfo> pair) => new KeyValuePair<int, LinkInfo>(change.GetNewOffset(pair.Key, AnchorMovementType.AfterInsertion), pair.Value)).ToList();
		LinkNameDict.Clear();
		foreach (KeyValuePair<int, LinkInfo> item in list)
		{
			LinkNameDict[item.Key] = item.Value;
		}
		List<KeyValuePair<int, LinkInfo>> list2 = LinkUrlDict.Select((KeyValuePair<int, LinkInfo> pair) => new KeyValuePair<int, LinkInfo>(change.GetNewOffset(pair.Key, AnchorMovementType.AfterInsertion), pair.Value)).ToList();
		LinkUrlDict.Clear();
		foreach (KeyValuePair<int, LinkInfo> item2 in list2)
		{
			LinkUrlDict[item2.Key] = item2.Value;
		}
		List<KeyValuePair<int, AttachmentInfo>> list3 = AttachmentDict.Select(delegate(KeyValuePair<int, AttachmentInfo> pair)
		{
			int newOffset = change.GetNewOffset(pair.Key, AnchorMovementType.AfterInsertion);
			AttachmentInfo value = new AttachmentInfo(pair.Value?.Value, pair.Value?.Kind, pair.Value?.Url, newOffset)
			{
				Duplicate = (pair.Value?.Duplicate ?? false)
			};
			return new KeyValuePair<int, AttachmentInfo>(newOffset, value);
		}).ToList();
		AttachmentDict.Clear();
		AttachmentEndSet.Clear();
		foreach (KeyValuePair<int, AttachmentInfo> item3 in list3)
		{
			AttachmentDict[item3.Key] = item3.Value;
			AttachmentEndSet.Add(item3.Key + (item3.Value?.Value?.Length).GetValueOrDefault());
		}
		List<KeyValuePair<int, string>> list4 = EmojiDict.Select((KeyValuePair<int, string> pair) => new KeyValuePair<int, string>(change.GetNewOffset(pair.Key, AnchorMovementType.AfterInsertion), pair.Value)).ToList();
		EmojiDict.Clear();
		foreach (KeyValuePair<int, string> item4 in list4)
		{
			EmojiDict[item4.Key] = item4.Value;
		}
	}

	private void HandlerTextChanged(object sender, EventArgs e)
	{
		Utils.RunOnUiThread(base.Dispatcher, HandleOnTextChanged);
	}

	private void SetupSpellCheck()
	{
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

	public void SetupSearchRender(bool isTitle, bool inDetail)
	{
		if (_searchRender == null)
		{
			_searchRender = new SearchHighlightBackgroundRender(EditBox, LinkUrlDict, isTitle, inDetail, inPreSearch: false, IsDocumentOffsetHiddenByHeadingFold);
			EditBox.TextArea.TextView.BackgroundRenderers.Add(_searchRender);
			SearchHelper.SearchKeyChanged += OnSearchKeyChanged;
			SearchHelper.SearchModeChanged += OnSearchModeChanged;
		}
	}

	private void OnSearchKeyChanged(object sender, EventArgs e)
	{
		_searchRender.SetTextChanged(b: true);
		UpdateHeadingFoldings();
		RequestSafeRedraw();
	}

	private void OnSearchModeChanged(object sender, EventArgs e)
	{
		_searchRender.SetTextChanged(b: true);
		UpdateHeadingFoldings();
		RequestSafeRedraw();
		InvalidMargin();
	}

	public double GetFirstSearchIndex()
	{
		int num = _searchRender?.GetFirstIndex() ?? (-1);
		if (TryGetFirstCodeBlockSearchOffset(num, out var verticalOffset))
		{
			return verticalOffset;
		}
		if (num >= 0 && num < Text.Length)
		{
			TextViewPosition position = new TextViewPosition(EditBox.Document.GetLocation(num));
			if (TryGetVisualOffset(position, VisualYPosition.LineBottom, out var offset))
			{
				return offset;
			}
		}
		return -1.0;
	}

	private bool TryGetFirstCodeBlockSearchOffset(int firstParentMatchOffset, out double verticalOffset)
	{
		verticalOffset = 0.0;
		TextDocument textDocument = EditBox?.Document;
		TextView textView = EditBox?.TextArea?.TextView;
		if (_searchRender == null || textDocument == null || textView == null)
		{
			return false;
		}
		foreach (MarkdownBlockNode semanticBlock in GetSemanticBlocks())
		{
			if (!IsCodeBlockAttachment(semanticBlock) || IsSemanticBlockHiddenByHeadingFold(semanticBlock) || !_searchRender.GetFirstMatchIndex(semanticBlock.CodeText ?? string.Empty).HasValue)
			{
				continue;
			}
			DocumentLine lineByNumber = textDocument.GetLineByNumber(semanticBlock.StartLine + 1);
			if (lineByNumber == null || (firstParentMatchOffset >= 0 && lineByNumber.Offset >= firstParentMatchOffset))
			{
				return false;
			}
			try
			{
				double visualTopByDocumentLine = textView.GetVisualTopByDocumentLine(lineByNumber.LineNumber);
				CodeBlockAttachmentElementGenerator codeBlockAttachmentElementGenerator = _codeBlockAttachmentElementGenerator;
				if (codeBlockAttachmentElementGenerator != null && codeBlockAttachmentElementGenerator.TryGetFirstSearchMatchVerticalOffset(semanticBlock, out var verticalOffset2))
				{
					verticalOffset = visualTopByDocumentLine + verticalOffset2;
				}
				else
				{
					verticalOffset = visualTopByDocumentLine + 24.0;
				}
				return true;
			}
			catch (InvalidOperationException)
			{
				return false;
			}
			catch (VisualLinesInvalidException)
			{
				return false;
			}
		}
		return false;
	}

	private void SetupSyntaxHighlighting()
	{
		EditBox.TextChanged -= EditBoxOnTextChanged;
		EditBox.TextChanged += EditBoxOnTextChanged;
		EditBox.TextArea.TextView.LineTransformers.Add(_colorizer);
		EditBox.TextArea.TextView.LineTransformers.Add(new CodeBlockFenceColorizer(this));
		_backgroundRenderer = new BlockBackgroundRenderer(this);
		EditBox.TextArea.TextView.BackgroundRenderers.Add(_backgroundRenderer);
		if (_searchRender != null)
		{
			EditBox.TextArea.TextView.BackgroundRenderers.Remove(_searchRender);
			EditBox.TextArea.TextView.BackgroundRenderers.Add(_searchRender);
		}
	}

	private void HandleOnTextChanged()
	{
		try
		{
			RefreshEditingStateWithoutMarkdownParsing(Text);
			bool suppressEditBoxTextChanged = _suppressEditBoxTextChanged;
			_suppressEditBoxTextChanged = true;
			try
			{
				if (TrySyncParagraphIndentMarkersWithSemanticState(EditBox.CaretOffset, out var adjustedCaretOffset))
				{
					EditBox.SelectionLength = 0;
					EditBox.CaretOffset = adjustedCaretOffset;
				}
			}
			finally
			{
				_suppressEditBoxTextChanged = suppressEditBoxTextChanged;
			}
			InvalidMargin();
			TryShowQuickSetPopup();
			TryShowStyleMenu();
			RefreshOpenMenuContexts();
			RequestImmediateRedrawAroundCaret();
		}
		catch (Exception)
		{
		}
	}

	private void LogCodeRects(string text)
	{
		CodeDict.Clear();
		foreach (MarkdownBlockNode semanticBlock in GetSemanticBlocks())
		{
			if (semanticBlock.Kind == MarkdownBlockKind.CodeBlock && TryGetSemanticBlockOffsetRange(semanticBlock, text, out var startOffset, out var endOffset))
			{
				string key = semanticBlock.Language ?? string.Empty;
				Point item = new Point(startOffset, endOffset);
				if (CodeDict.ContainsKey(key))
				{
					CodeDict[key].Add(item);
					continue;
				}
				CodeDict.Add(key, new List<Point> { item });
			}
		}
		RefreshCodeColorizers();
	}

	private void RefreshCodeColorizers()
	{
		foreach (string item in _codeColorizers.Keys.ToList())
		{
			if (!CodeDict.ContainsKey(item))
			{
				EditBox.TextArea.TextView.LineTransformers.Remove(_codeColorizers[item]);
				_codeColorizers.Remove(item);
			}
		}
		if (CodeDict == null || !CodeDict.Any())
		{
			return;
		}
		foreach (string key in CodeDict.Keys)
		{
			if (!_codeColorizers.ContainsKey(key))
			{
				IVisualLineTransformer visualLineTransformer = TryCreateCodeColorizer(key);
				if (visualLineTransformer != null)
				{
					_codeColorizers[key] = visualLineTransformer;
					EditBox.TextArea.TextView.LineTransformers.Add(visualLineTransformer);
				}
			}
		}
	}

	private void RefreshCodeColorizersForTheme()
	{
		if (EditBox?.TextArea?.TextView == null || _codeColorizers.Count == 0)
		{
			return;
		}
		foreach (IVisualLineTransformer item in _codeColorizers.Values.ToList())
		{
			EditBox.TextArea.TextView.LineTransformers.Remove(item);
		}
		_codeColorizers.Clear();
		RefreshCodeColorizers();
	}

	private void ClearLegacyAstStateForEditing()
	{
		_lastText = EditBox.Text ?? string.Empty;
		_colorizer?.UpdateAbstractSyntaxTree(null);
		_backgroundRenderer?.UpdateAbstractSyntaxTree(null);
	}

	private void RefreshEditingStateWithoutMarkdownParsing(string text)
	{
		ClearLegacyAstStateForEditing();
		RefreshEditingSemanticStateWithoutMarkdownParsing(text);
		RefreshEditingDerivedMetadataWithoutMarkdownParsing();
	}

	private void RefreshEditingDerivedMetadataWithoutMarkdownParsing(bool startExternalImageDownloads = true)
	{
		int[] displayToPersistedOffsets;
		int[] persistedToDisplayOffsets;
		string persistedText = BuildPersistedTextWithOffsetMaps(out displayToPersistedOffsets, out persistedToDisplayOffsets);
		RebuildEditingLinkAndAttachmentState(persistedText, persistedToDisplayOffsets, startExternalImageDownloads);
		LogEmojiPosition(EditBox.Text ?? string.Empty);
		LogCodeRects(EditBox.Text ?? string.Empty);
		_requiresImmediateDerivedMetadataRefresh = false;
		_imageElementGenerator?.DiagnosticMetadataRefreshed();
	}

	private string BuildPersistedTextWithOffsetMaps(out int[] displayToPersistedOffsets, out int[] persistedToDisplayOffsets)
	{
		int num = EditBox.Text?.Length ?? 0;
		displayToPersistedOffsets = new int[num + 1];
		string text = BuildPersistedText(ref displayToPersistedOffsets);
		persistedToDisplayOffsets = BuildPersistedToDisplayOffsetMap(text ?? string.Empty, displayToPersistedOffsets, num);
		return text ?? string.Empty;
	}

	private static int[] BuildPersistedToDisplayOffsetMap(string persistedText, int[] displayToPersistedOffsets, int displayTextLength)
	{
		persistedText = persistedText ?? string.Empty;
		if (displayToPersistedOffsets == null || displayToPersistedOffsets.Length == 0)
		{
			int[] array = new int[persistedText.Length + 1];
			for (int i = 0; i <= persistedText.Length; i++)
			{
				array[i] = Math.Min(i, displayTextLength);
			}
			return array;
		}
		int[] array2 = new int[persistedText.Length + 1];
		int j = 0;
		for (int k = 0; k <= persistedText.Length; k++)
		{
			for (; j < displayToPersistedOffsets.Length && displayToPersistedOffsets[j] < k; j++)
			{
			}
			array2[k] = Math.Min(j, displayTextLength);
		}
		return array2;
	}

	private IReadOnlyList<AttachmentInfo> RebuildEditingLinkAndAttachmentState(string persistedText, int[] persistedToDisplayOffsets, bool startExternalImageDownloads = true)
	{
		List<AttachmentInfo> list = new List<AttachmentInfo>();
		LinkNameDict.Clear();
		LinkUrlDict.Clear();
		AttachmentDict.Clear();
		AttachmentEndSet.Clear();
		SemanticMarkList.RemoveAll((MarkdownInlineDisplaySegment mark) => mark == null || mark.Kind == MarkdownMarkKind.Link);
		RebuildTypedLinkDisplayState();
		persistedText = persistedText ?? string.Empty;
		if (persistedText.Length == 0)
		{
			return list;
		}
		foreach (PersistedMarkdownLinkMatch item in EnumeratePersistedMarkdownLinks(persistedText))
		{
			if (IsLinkMatchInsideMarkdownImageSyntax(persistedText, item.Index))
			{
				continue;
			}
			string url = item.Url;
			if (string.IsNullOrEmpty(url))
			{
				continue;
			}
			int num = TranslatePersistedOffsetToDisplayOffset(item.Index, persistedToDisplayOffsets);
			int num2 = TranslatePersistedOffsetToDisplayOffset(item.Index + item.Length, persistedToDisplayOffsets);
			int num3 = TranslatePersistedOffsetToDisplayOffset(item.TextIndex, persistedToDisplayOffsets);
			int num4 = TranslatePersistedOffsetToDisplayOffset(item.TextIndex + item.TextLength, persistedToDisplayOffsets);
			int num5 = TranslatePersistedOffsetToDisplayOffset(item.UrlIndex, persistedToDisplayOffsets);
			int num6 = TranslatePersistedOffsetToDisplayOffset(item.UrlIndex + item.UrlLength, persistedToDisplayOffsets);
			int num7 = Math.Max(0, num4 - num3);
			int num8 = Math.Max(0, num6 - num5);
			string text = EditBox?.Text ?? string.Empty;
			string text2 = ((num7 > 0 && num3 >= 0 && num3 + num7 <= text.Length) ? text.Substring(num3, num7) : item.Text);
			LinkInfo linkInfo = new LinkInfo(text2, url);
			if (!HasEquivalentTypedLinkMark(num3, num7, text2, url))
			{
				AddLinkNameInfo(num3, linkInfo);
				if (num8 > 0)
				{
					LinkUrlDict[num5] = linkInfo;
				}
				SemanticMarkList.Add(new MarkdownInlineDisplaySegment
				{
					Kind = MarkdownMarkKind.Link,
					SourcePosition = num,
					SourceLength = Math.Max(0, num2 - num),
					OpeningDelimiterLength = Math.Max(0, num3 - num),
					ClosingDelimiterLength = Math.Max(0, num2 - num4),
					LinkText = text2,
					Url = url,
					UrlPosition = num5,
					UrlLength = num8
				});
			}
		}
		foreach (Match item2 in AttachmentRegex.Matches(persistedText))
		{
			if (!item2.Success || item2.Groups.Count < 3)
			{
				continue;
			}
			string value = item2.Groups[2].Value;
			if (string.IsNullOrEmpty(value))
			{
				continue;
			}
			int num9 = TranslatePersistedOffsetToDisplayOffset(item2.Index, persistedToDisplayOffsets);
			int num10 = TranslatePersistedOffsetToDisplayOffset(item2.Index + item2.Length, persistedToDisplayOffsets);
			if (IsDisplayRangeInsideCode(num9, num10))
			{
				continue;
			}
			string value2 = item2.Value;
			if (!TryGetRenderableAttachmentKind(persistedText, item2, out var attachmentKind))
			{
				continue;
			}
			if (attachmentKind == "image" && Uri.IsWellFormedUriString(value, UriKind.Absolute))
			{
				new Uri(value, UriKind.Absolute);
				int hashCode = value.GetHashCode();
				string text3 = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Tick_Tick\\Image\\" + hashCode + ".png";
				if (startExternalImageDownloads && !string.IsNullOrEmpty(_editingTaskId) && !ImageElementGenerator.NotImageUrlSet.Contains(hashCode) && (!File.Exists(text3) || new FileInfo(text3).Length == 0L))
				{
					TryDownLoadUrlImage(value, text3);
				}
			}
			AttachmentInfo attachmentInfo = new AttachmentInfo(value2, attachmentKind, value, num9);
			list.Add(attachmentInfo);
			AttachmentDict[num9] = attachmentInfo;
			AttachmentEndSet.Add(num10);
		}
		return list;
	}

	internal bool IsAttachmentDiagnosticLiteralRange(int start, int end)
	{
		if (!IsDisplayRangeInsideCode(start, end))
		{
			return GetSemanticMarkList().Any((MarkdownInlineDisplaySegment mark) => mark.Kind == MarkdownMarkKind.LiteralInlineSyntax && mark.SourcePosition < end && mark.SourcePosition + mark.SourceLength > start);
		}
		return true;
	}

	private bool IsDisplayRangeInsideCode(int start, int end)
	{
		int valueOrDefault = (EditBox?.Text?.Length).GetValueOrDefault();
		int safeStart = Math.Max(0, Math.Min(start, valueOrDefault));
		int safeEnd = Math.Max(safeStart, Math.Min(end, valueOrDefault));
		if (TryGetSemanticBlockAtOffset(safeStart, out var block) && block != null && block.Kind == MarkdownBlockKind.CodeBlock)
		{
			return true;
		}
		if (safeEnd > safeStart && TryGetSemanticBlockAtOffset(safeEnd - 1, out var block2) && block2 != null && block2.Kind == MarkdownBlockKind.CodeBlock)
		{
			return true;
		}
		return SemanticMarkList.Any((MarkdownInlineDisplaySegment mark) => mark != null && mark.Kind == MarkdownMarkKind.Code && mark.SourcePosition < safeEnd && mark.SourcePosition + mark.SourceLength > safeStart);
	}

	private static bool IsLinkMatchInsideMarkdownImageSyntax(string text, int matchIndex)
	{
		if (!string.IsNullOrEmpty(text) && matchIndex > 0 && matchIndex <= text.Length)
		{
			return text[matchIndex - 1] == '!';
		}
		return false;
	}

	private void RebuildTypedLinkDisplayState()
	{
		string text = EditBox?.Text ?? string.Empty;
		int length = text.Length;
		foreach (MarkdownInlineDisplaySegment typedSemanticMark in _typedSemanticMarks)
		{
			if (typedSemanticMark == null || typedSemanticMark.Kind != MarkdownMarkKind.Link || typedSemanticMark.SourceLength <= 0 || typedSemanticMark.OpeningDelimiterLength != 0 || typedSemanticMark.ClosingDelimiterLength != 0 || string.IsNullOrEmpty(typedSemanticMark.Url))
			{
				continue;
			}
			int num = Math.Max(0, Math.Min(typedSemanticMark.SourcePosition, length));
			int num2 = Math.Max(num, Math.Min(typedSemanticMark.SourcePosition + typedSemanticMark.SourceLength, length));
			if (num2 > num)
			{
				string text2 = text.Substring(num, num2 - num);
				if (!string.IsNullOrEmpty(text2))
				{
					AddLinkNameInfo(num, new LinkInfo(text2, typedSemanticMark.Url));
				}
			}
		}
	}

	private void AddLinkNameInfo(int sourcePosition, LinkInfo linkInfo)
	{
		if (linkInfo == null || string.IsNullOrEmpty(linkInfo.Link))
		{
			return;
		}
		int num = NormalizeLinkNameStart(sourcePosition, linkInfo);
		int num2 = num + linkInfo.Link.Length;
		foreach (KeyValuePair<int, LinkInfo> item in LinkNameDict.ToList())
		{
			LinkInfo value = item.Value;
			if (value == null || !string.Equals(value.Link ?? string.Empty, linkInfo.Link ?? string.Empty, StringComparison.Ordinal) || !string.Equals(value.Url ?? string.Empty, linkInfo.Url ?? string.Empty, StringComparison.Ordinal))
			{
				continue;
			}
			int key = item.Key;
			int num3 = key + (value.Link?.Length ?? 0);
			if (num < num3 && num2 > key)
			{
				if (key != num)
				{
					LinkNameDict.Remove(key);
					LinkNameDict[num] = linkInfo;
				}
				return;
			}
		}
		LinkNameDict[num] = linkInfo;
	}

	private int NormalizeLinkNameStart(int sourcePosition, LinkInfo linkInfo)
	{
		string text = EditBox?.Text ?? string.Empty;
		string text2 = linkInfo?.Link ?? string.Empty;
		int length = text.Length;
		int num = Math.Max(0, Math.Min(sourcePosition, length));
		if (string.IsNullOrEmpty(text2) || text2.Length > length)
		{
			return num;
		}
		if (num + text2.Length <= length && string.Equals(text.Substring(num, text2.Length), text2, StringComparison.Ordinal))
		{
			return num;
		}
		int result = num;
		int num2 = int.MaxValue;
		for (int num3 = text.IndexOf(text2, StringComparison.Ordinal); num3 >= 0; num3 = text.IndexOf(text2, num3 + 1, StringComparison.Ordinal))
		{
			int num4 = Math.Abs(num3 - num);
			if (num4 < num2)
			{
				num2 = num4;
				result = num3;
			}
		}
		if (num2 > text2.Length + 2)
		{
			return num;
		}
		return result;
	}

	private bool HasEquivalentTypedLinkMark(int sourcePosition, int sourceLength, string linkText, string url)
	{
		string text = EditBox?.Text ?? string.Empty;
		int num = Math.Max(0, Math.Min(sourcePosition, text.Length));
		int num2 = Math.Max(0, Math.Min(sourceLength, text.Length - num));
		int num3 = num + num2;
		foreach (MarkdownInlineDisplaySegment typedSemanticMark in _typedSemanticMarks)
		{
			if (typedSemanticMark != null && typedSemanticMark.Kind == MarkdownMarkKind.Link && typedSemanticMark.SourceLength > 0 && string.Equals(typedSemanticMark.Url ?? string.Empty, url ?? string.Empty, StringComparison.Ordinal))
			{
				int num4 = Math.Max(0, Math.Min(typedSemanticMark.SourcePosition, text.Length));
				int num5 = Math.Max(0, Math.Min(typedSemanticMark.SourceLength, text.Length - num4));
				int num6 = num4 + num5;
				if (num5 == num2 && num4 < num3 && num6 > num && string.Equals(GetSafeTextSlice(text, num4, num5), linkText ?? string.Empty, StringComparison.Ordinal))
				{
					return true;
				}
			}
		}
		return false;
	}

	private static int TranslatePersistedOffsetToDisplayOffset(int persistedOffset, int[] persistedToDisplayOffsets)
	{
		if (persistedToDisplayOffsets == null || persistedToDisplayOffsets.Length == 0)
		{
			return Math.Max(0, persistedOffset);
		}
		return persistedToDisplayOffsets[Math.Max(0, Math.Min(persistedOffset, persistedToDisplayOffsets.Length - 1))];
	}

	private bool TryGetSemanticBlockOffsetRange(MarkdownBlockNode block, string text, out int startOffset, out int endOffset)
	{
		startOffset = 0;
		endOffset = 0;
		text = text ?? string.Empty;
		if (block == null)
		{
			return false;
		}
		if (!TryGetLineStartOffset(text, block.StartLine, out startOffset) || !TryGetLineEndOffset(text, block.EndLine, out endOffset))
		{
			return false;
		}
		return true;
	}

	private static bool TryGetLineStartOffset(string text, int lineIndex, out int offset)
	{
		offset = 0;
		if (lineIndex < 0)
		{
			return false;
		}
		int num = 0;
		int num2 = 0;
		while (num < lineIndex && num2 < text.Length)
		{
			if (text[num2] == '\r')
			{
				num2++;
				if (num2 < text.Length && text[num2] == '\n')
				{
					num2++;
				}
				num++;
			}
			else if (text[num2] == '\n')
			{
				num2++;
				num++;
			}
			else
			{
				num2++;
			}
		}
		if (num != lineIndex)
		{
			return false;
		}
		offset = num2;
		return true;
	}

	private static bool TryGetLineEndOffset(string text, int lineIndex, out int offset)
	{
		offset = 0;
		if (!TryGetLineStartOffset(text, lineIndex, out var offset2))
		{
			return false;
		}
		int i;
		for (i = offset2; i < text.Length && text[i] != '\r' && text[i] != '\n'; i++)
		{
		}
		offset = i;
		return true;
	}

	private IVisualLineTransformer TryCreateCodeColorizer(string lang)
	{
		if (!CodeBlockLanguageCatalog.TryGetHighlightingResourceKey(lang, out var resourceKey))
		{
			return null;
		}
		IHighlightingDefinition definition = new ThemedHighlightingDefinitionResolver(IsCurrentEditorThemeDark() ? "dark" : "light").GetDefinition(resourceKey);
		if (definition == null)
		{
			return null;
		}
		return new CodeColorizer(this, definition, lang);
	}

	private bool IsCurrentEditorThemeDark()
	{
		if (_isDark.HasValue)
		{
			return _isDark.Value;
		}
		object obj = TryFindResource("IsDarkTheme");
		if (obj is bool)
		{
			return (bool)obj;
		}
		return ThemeKey.IsDarkTheme(LocalSettings.Settings.ThemeId);
	}

	private void LogPositions(string content)
	{
		RefreshEditingDerivedMetadataWithoutMarkdownParsing();
	}

	private void LogSemanticInlineSegments(string content)
	{
		content = content ?? string.Empty;
		foreach (MarkdownBlockNode semanticBlock in GetSemanticBlocks())
		{
			if (semanticBlock == null || semanticBlock.Kind == MarkdownBlockKind.CodeBlock || !TryGetSemanticInlineBaseOffset(semanticBlock, content, out var baseOffset))
			{
				continue;
			}
			foreach (MarkdownInlineNode inline in semanticBlock.Inlines)
			{
				if (inline == null || inline.Marks.Count == 0 || inline.SourceLength <= 0)
				{
					continue;
				}
				foreach (MarkdownMark mark in inline.Marks)
				{
					if (!IsSemanticInlineDisplayMark(mark.Kind))
					{
						continue;
					}
					int num = baseOffset + inline.SourceStart;
					if (num < 0 || num + inline.SourceLength > content.Length)
					{
						continue;
					}
					if (mark.Kind == MarkdownMarkKind.Link)
					{
						int num2 = num + inline.OpeningDelimiterLength;
						int num3 = Math.Max(0, inline.SourceLength - inline.OpeningDelimiterLength - inline.ClosingDelimiterLength);
						if (num3 > 0 && num2 + num3 <= content.Length)
						{
							SemanticMarkList.Add(new MarkdownInlineDisplaySegment
							{
								Kind = MarkdownMarkKind.Link,
								SourcePosition = num2,
								SourceLength = num3,
								OpeningDelimiterLength = 0,
								ClosingDelimiterLength = 0,
								LinkText = inline.Text,
								Url = mark.Url,
								UrlPosition = 0,
								UrlLength = 0
							});
							LogSemanticLink(num, inline, mark);
						}
					}
					else
					{
						SemanticMarkList.Add(new MarkdownInlineDisplaySegment
						{
							Kind = mark.Kind,
							SourcePosition = num,
							SourceLength = inline.SourceLength,
							OpeningDelimiterLength = inline.OpeningDelimiterLength,
							ClosingDelimiterLength = inline.ClosingDelimiterLength,
							Color = mark.Color,
							LinkText = inline.Text,
							Url = mark.Url,
							UrlPosition = GetSemanticLinkUrlPosition(num, inline),
							UrlLength = ((!string.IsNullOrEmpty(mark.Url)) ? mark.Url.Length : 0)
						});
						LogSemanticLink(num, inline, mark);
					}
				}
			}
		}
	}

	private static bool IsSemanticInlineDisplayMark(MarkdownMarkKind kind)
	{
		if (kind != MarkdownMarkKind.Highlight)
		{
			return kind == MarkdownMarkKind.Link;
		}
		return true;
	}

	private void LogSemanticLink(int sourcePosition, MarkdownInlineNode inline, MarkdownMark mark)
	{
		if (mark.Kind == MarkdownMarkKind.Link && !string.IsNullOrEmpty(mark.Url) && inline != null)
		{
			string link = inline.Text ?? string.Empty;
			AddLinkNameInfo(sourcePosition + inline.OpeningDelimiterLength, new LinkInfo(link, mark.Url));
		}
	}

	private static int GetSemanticLinkUrlPosition(int sourcePosition, MarkdownInlineNode inline)
	{
		if (inline == null)
		{
			return sourcePosition;
		}
		return sourcePosition + inline.OpeningDelimiterLength + Math.Max(0, inline.Text?.Length ?? 0) + 2;
	}

	private bool TryGetSemanticInlineBaseOffset(MarkdownBlockNode block, string text, out int baseOffset)
	{
		baseOffset = 0;
		if (!TryGetLineStartOffset(text, block.StartLine, out var offset) || !TryGetLineEndOffset(text, block.StartLine, out var offset2) || offset > offset2)
		{
			return false;
		}
		string text2 = text.Substring(offset, offset2 - offset);
		if (TryGetDisplayBlockPrefixReplacement(text2, block, out var markerOffset, out var markerLength, out var _))
		{
			baseOffset = Math.Max(offset, Math.Min(offset2, offset + markerOffset + markerLength));
			return true;
		}
		baseOffset = GetSemanticContentStartOffset(text2, block, offset, offset + text2.Length);
		return true;
	}

	private static int GetSemanticBlockContentOffset(string line, MarkdownBlockNode block)
	{
		line = line ?? string.Empty;
		if (block == null)
		{
			return 0;
		}
		int num = CountSemanticIndentCharacters(line, block.Indent);
		if (num > line.Length)
		{
			return line.Length;
		}
		return block.Kind switch
		{
			MarkdownBlockKind.Heading => SkipDisplayCaretAnchor(line, Math.Min(line.Length, num)), 
			MarkdownBlockKind.BlockQuote => SkipDisplayCaretAnchor(line, Math.Min(line.Length, num)), 
			MarkdownBlockKind.List => GetSemanticListContentOffset(line, num), 
			MarkdownBlockKind.CodeBlock => GetSemanticCodeBlockContentOffset(line, num), 
			_ => SkipDisplayCaretAnchor(line, (block.Indent > 0) ? num : 0), 
		};
	}

	private static int GetSemanticListContentOffset(string line, int offset)
	{
		return SkipDisplayCaretAnchor(line ?? string.Empty, Math.Min(line?.Length ?? 0, Math.Max(0, offset)));
	}

	private static int GetSemanticCodeBlockContentOffset(string line, int offset)
	{
		return Math.Min((line ?? string.Empty).Length, Math.Max(0, offset));
	}

	private static int GetSemanticContentStartOffset(string lineText, MarkdownBlockNode block, int lineOffset, int lineEndOffset)
	{
		int num = Math.Max(0, lineOffset);
		int val = Math.Max(num, lineEndOffset);
		int semanticBlockContentOffset = GetSemanticBlockContentOffset(lineText, block);
		return Math.Max(num, Math.Min(val, num + semanticBlockContentOffset));
	}

	private static int SkipDisplayCaretAnchor(string line, int offset)
	{
		string text = line ?? string.Empty;
		int num = Math.Min(text.Length, Math.Max(0, offset));
		if (num < text.Length && (text[num] == '\u00a0' || text[num] == '\u2065'))
		{
			num++;
		}
		return Math.Min(text.Length, num);
	}

	private static int CountSemanticIndentCharacters(string line, int indent)
	{
		int num = 0;
		int num2 = Math.Max(0, indent);
		while (num2 > 0 && num < line.Length)
		{
			if (line[num] == '\t')
			{
				num++;
				num2--;
				continue;
			}
			if (num + 4 > line.Length || line[num] != ' ' || line[num + 1] != ' ' || line[num + 2] != ' ' || line[num + 3] != ' ')
			{
				break;
			}
			num += 4;
			num2--;
		}
		return num;
	}

	private void NormalizeCaretIntoSemanticContentIfNeeded()
	{
		if (EditBox?.Document == null)
		{
			return;
		}
		int num = Math.Max(0, Math.Min(EditBox.CaretOffset, EditBox.Document.TextLength));
		DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(num);
		if (lineByOffset == null)
		{
			return;
		}
		int lineIndex = Math.Max(0, lineByOffset.LineNumber - 1);
		if (TryGetSemanticBlockAtLine(lineIndex, out var block) && block != null && (block.Kind != MarkdownBlockKind.CodeBlock || !TryNormalizeCaretInsideCodeBlock(lineByOffset, lineIndex, block)) && (block.Kind == MarkdownBlockKind.List || block.Kind == MarkdownBlockKind.BlockQuote || block.Kind == MarkdownBlockKind.Heading || block.Kind == MarkdownBlockKind.CodeBlock))
		{
			int semanticContentStartOffset = GetSemanticContentStartOffset(EditBox.Document.GetText(lineByOffset), block, lineByOffset.Offset, lineByOffset.EndOffset);
			if (num < semanticContentStartOffset)
			{
				EditBox.CaretOffset = semanticContentStartOffset;
			}
		}
	}

	private bool TryNormalizeCaretInsideCodeBlock(DocumentLine line, int lineIndex, MarkdownBlockNode block)
	{
		if (line == null || block == null || block.Kind != MarkdownBlockKind.CodeBlock)
		{
			return false;
		}
		if (IsCodeBlockAttachment(block))
		{
			return false;
		}
		int num = Math.Max(0, Math.Min(EditBox.CaretOffset, EditBox.Document.TextLength));
		if (!TryGetCodeBlockEditableOffset(line, lineIndex, block, out var targetOffset))
		{
			return false;
		}
		if (IsSemanticCodeBlockContentLine(EditBox.Document.GetText(line), block, lineIndex))
		{
			if (num >= targetOffset)
			{
				return false;
			}
			EditBox.CaretOffset = targetOffset;
			return true;
		}
		if (num == targetOffset)
		{
			return false;
		}
		EditBox.CaretOffset = targetOffset;
		return true;
	}

	private bool TryKeepCaretInsideSemanticContent(bool moveToPreviousLineWhenAtStart)
	{
		if (EditBox?.Document == null || EditBox.SelectionLength > 0)
		{
			return false;
		}
		int num = Math.Max(0, Math.Min(EditBox.CaretOffset, EditBox.Document.TextLength));
		DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(num);
		if (lineByOffset == null)
		{
			return false;
		}
		int num2 = Math.Max(0, lineByOffset.LineNumber - 1);
		if (!TryGetSemanticBlockAtLine(num2, out var block) || block == null)
		{
			return false;
		}
		if (block.Kind == MarkdownBlockKind.CodeBlock)
		{
			if (TryGetCodeBlockEditableOffset(lineByOffset, num2, block, out var targetOffset))
			{
				if (IsSemanticCodeBlockContentLine(EditBox.Document.GetText(lineByOffset), block, num2))
				{
					if (num > targetOffset)
					{
						return false;
					}
					if (moveToPreviousLineWhenAtStart && num == targetOffset && num2 <= block.StartLine + 1)
					{
						return false;
					}
					if (moveToPreviousLineWhenAtStart && num == targetOffset && lineByOffset.PreviousLine != null && num2 > block.StartLine + 1)
					{
						EditBox.CaretOffset = lineByOffset.PreviousLine.EndOffset;
						return true;
					}
					EditBox.CaretOffset = targetOffset;
					return true;
				}
				if (num == targetOffset)
				{
					return false;
				}
				EditBox.CaretOffset = targetOffset;
				return true;
			}
			return false;
		}
		if (block.Kind != MarkdownBlockKind.List && block.Kind != MarkdownBlockKind.BlockQuote && block.Kind != MarkdownBlockKind.Heading)
		{
			return false;
		}
		int semanticContentStartOffset = GetSemanticContentStartOffset(EditBox.Document.GetText(lineByOffset), block, lineByOffset.Offset, lineByOffset.EndOffset);
		if (num > semanticContentStartOffset)
		{
			return false;
		}
		if (moveToPreviousLineWhenAtStart && num == semanticContentStartOffset && lineByOffset.PreviousLine != null)
		{
			EditBox.CaretOffset = lineByOffset.PreviousLine.EndOffset;
			return true;
		}
		EditBox.CaretOffset = semanticContentStartOffset;
		return true;
	}

	private bool TryKeepCaretBeforeSemanticBoundary()
	{
		if (EditBox?.Document == null || EditBox.SelectionLength > 0)
		{
			return false;
		}
		int num = Math.Max(0, Math.Min(EditBox.CaretOffset, EditBox.Document.TextLength));
		DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(num);
		if (lineByOffset == null)
		{
			return false;
		}
		int num2 = Math.Max(0, lineByOffset.LineNumber - 1);
		if (!TryGetSemanticBlockAtLine(num2, out var block) || block == null || block.Kind != MarkdownBlockKind.CodeBlock || num2 >= block.EndLine)
		{
			return false;
		}
		DocumentLine nextLine = lineByOffset.NextLine;
		if (nextLine == null)
		{
			return false;
		}
		int num3 = Math.Max(0, nextLine.LineNumber - 1);
		if (num3 != block.EndLine)
		{
			return false;
		}
		if (!IsSemanticCodeBlockClosingFenceLine(EditBox.Document.GetText(nextLine), block, num3))
		{
			return false;
		}
		if (!ShouldKeepCaretBeforeCodeBlockBoundary(lineByOffset.Offset, lineByOffset.EndOffset, num))
		{
			return false;
		}
		EditBox.CaretOffset = lineByOffset.EndOffset;
		return true;
	}

	private static bool ShouldKeepCaretBeforeCodeBlockBoundary(int lineOffset, int lineEndOffset, int caretOffset)
	{
		int val = Math.Max(0, lineOffset);
		int num = Math.Max(val, lineEndOffset);
		return Math.Max(val, caretOffset) >= num;
	}

	private void NormalizeSelectionInsideCodeBlockContentIfNeeded()
	{
		if (EditBox?.Document == null || EditBox.SelectionLength <= 0)
		{
			ClearObjectSelectedCodeBlocks();
			return;
		}
		int selectionStart = EditBox.SelectionStart;
		int num = selectionStart + EditBox.SelectionLength;
		int num2 = Math.Max(0, Math.Min(EditBox.CaretOffset, EditBox.Document.TextLength));
		int normalizedSelectionStart = selectionStart;
		int normalizedSelectionEnd = num;
		if (!TryNormalizeCodeBlockSelectionRange(selectionStart, num, num2, out normalizedSelectionStart, out normalizedSelectionEnd))
		{
			normalizedSelectionStart = GetCodeBlockContentSelectionStart(selectionStart);
			normalizedSelectionEnd = GetCodeBlockContentSelectionEnd(selectionStart, num);
		}
		if (normalizedSelectionStart == selectionStart && normalizedSelectionEnd == num)
		{
			_codeBlockSelectionLayer?.InvalidateVisual();
			return;
		}
		normalizedSelectionStart = Math.Max(0, Math.Min(normalizedSelectionStart, EditBox.Document.TextLength));
		normalizedSelectionEnd = Math.Max(normalizedSelectionStart, Math.Min(normalizedSelectionEnd, EditBox.Document.TextLength));
		_semanticSelectionLastStartOffset = normalizedSelectionStart;
		_semanticSelectionLastEndOffset = normalizedSelectionEnd;
		_semanticSelectionLastActiveOffset = num2;
		if (Mouse.LeftButton == MouseButtonState.Pressed && _semanticSelectionAnchorOffset.HasValue)
		{
			_semanticSelectionDragHandled = true;
		}
		bool suppressSemanticSelectionNormalization = _suppressSemanticSelectionNormalization;
		_suppressSemanticSelectionNormalization = true;
		try
		{
			ApplyNormalizedSemanticSelection(normalizedSelectionStart, normalizedSelectionEnd, num2);
			_codeBlockSelectionLayer?.InvalidateVisual();
		}
		finally
		{
			_suppressSemanticSelectionNormalization = suppressSemanticSelectionNormalization;
		}
	}

	private void ApplyNormalizedSemanticSelection(int selectionStart, int selectionEnd, int activeOffset)
	{
		if (EditBox?.TextArea != null && selectionEnd >= selectionStart)
		{
			if (selectionEnd == selectionStart)
			{
				EditBox.TextArea.ClearSelection();
				EditBox.TextArea.Caret.Offset = Math.Max(0, Math.Min(selectionStart, EditBox.Document.TextLength));
			}
			else
			{
				int normalizedSelectionActiveOffset = GetNormalizedSelectionActiveOffset(selectionStart, selectionEnd, activeOffset);
				EditBox.TextArea.Selection = Selection.Create(EditBox.TextArea, selectionStart, selectionEnd);
				EditBox.TextArea.Caret.Offset = normalizedSelectionActiveOffset;
			}
		}
	}

	private bool TryClampSelectionAroundHeadingFoldForMutation(ref int selectionStart, ref int selectionEnd)
	{
		if (_headingFoldingManager == null || EditBox?.Document == null || selectionEnd <= selectionStart)
		{
			return false;
		}
		int textLength = EditBox.Document.TextLength;
		int num = Math.Max(0, Math.Min(selectionStart, textLength));
		int num2 = Math.Max(num, Math.Min(selectionEnd, textLength));
		bool flag = false;
		foreach (FoldingSection allFolding in _headingFoldingManager.AllFoldings)
		{
			if (allFolding != null && allFolding.IsFolded && num < allFolding.EndOffset && num2 > allFolding.StartOffset)
			{
				int num3 = Math.Max(0, allFolding.StartOffset - num);
				int num4 = Math.Max(0, num2 - allFolding.EndOffset);
				if (num3 >= num4)
				{
					num2 = Math.Min(num2, allFolding.StartOffset);
				}
				else
				{
					num = Math.Max(num, allFolding.EndOffset);
				}
				flag = true;
			}
		}
		if (!flag)
		{
			return false;
		}
		selectionStart = num;
		selectionEnd = Math.Max(selectionStart, num2);
		return true;
	}

	private bool TryApplySemanticCodeBlockDragSelection(int anchorOffset, int activeOffset)
	{
		if (EditBox?.Document == null)
		{
			return false;
		}
		if (anchorOffset == activeOffset)
		{
			if (!_semanticSelectionDragHandled)
			{
				return false;
			}
			ClearObjectSelectedCodeBlocks();
			_semanticSelectionLastStartOffset = anchorOffset;
			_semanticSelectionLastEndOffset = anchorOffset;
			_semanticSelectionLastActiveOffset = activeOffset;
			bool suppressSemanticSelectionNormalization = _suppressSemanticSelectionNormalization;
			_suppressSemanticSelectionNormalization = true;
			try
			{
				ApplyNormalizedSemanticSelection(anchorOffset, anchorOffset, activeOffset);
				_codeBlockSelectionLayer?.InvalidateVisual();
			}
			finally
			{
				_suppressSemanticSelectionNormalization = suppressSemanticSelectionNormalization;
			}
			return true;
		}
		int num = Math.Max(0, Math.Min(anchorOffset, activeOffset));
		int val = Math.Max(num, Math.Max(anchorOffset, activeOffset));
		val = Math.Min(val, EditBox.Document.TextLength);
		int normalizedSelectionStart = num;
		int normalizedSelectionEnd = val;
		if (!TryNormalizeCodeBlockSelectionRange(num, val, activeOffset, out normalizedSelectionStart, out normalizedSelectionEnd))
		{
			if (!_semanticSelectionDragHandled)
			{
				return false;
			}
			ClearObjectSelectedCodeBlocks();
			normalizedSelectionStart = num;
			normalizedSelectionEnd = val;
		}
		normalizedSelectionStart = Math.Max(0, Math.Min(normalizedSelectionStart, EditBox.Document.TextLength));
		normalizedSelectionEnd = Math.Max(normalizedSelectionStart, Math.Min(normalizedSelectionEnd, EditBox.Document.TextLength));
		_semanticSelectionLastStartOffset = normalizedSelectionStart;
		_semanticSelectionLastEndOffset = normalizedSelectionEnd;
		_semanticSelectionLastActiveOffset = activeOffset;
		bool suppressSemanticSelectionNormalization2 = _suppressSemanticSelectionNormalization;
		_suppressSemanticSelectionNormalization = true;
		try
		{
			ApplyNormalizedSemanticSelection(normalizedSelectionStart, normalizedSelectionEnd, activeOffset);
			_codeBlockSelectionLayer?.InvalidateVisual();
		}
		finally
		{
			_suppressSemanticSelectionNormalization = suppressSemanticSelectionNormalization2;
		}
		return true;
	}

	private bool TryGetSemanticCodeBlockDragSelectionRange(int anchorOffset, int activeOffset, out int selectionStart, out int selectionEnd)
	{
		selectionStart = 0;
		selectionEnd = 0;
		if (EditBox?.Document == null || !TryGetCodeBlockAtSelectionOffset(anchorOffset, treatAsSelectionEnd: false, out var block) || !TryGetCodeBlockContentRange(block, out var contentStartOffset, out var contentEndOffset))
		{
			return false;
		}
		int val = Math.Max(0, Math.Min(anchorOffset, EditBox.Document.TextLength));
		int val2 = Math.Max(0, Math.Min(activeOffset, EditBox.Document.TextLength));
		int val3 = Math.Min(val, val2);
		int val4 = Math.Max(val, val2);
		selectionStart = Math.Max(val3, contentStartOffset);
		selectionEnd = Math.Min(val4, contentEndOffset);
		if (selectionEnd <= selectionStart)
		{
			selectionStart = 0;
			selectionEnd = 0;
			return false;
		}
		return true;
	}

	private void RestoreLastSemanticDragSelectionIfNeeded()
	{
		if (!_semanticSelectionDragHandled || !_semanticSelectionLastStartOffset.HasValue || !_semanticSelectionLastEndOffset.HasValue || EditBox?.Document == null)
		{
			return;
		}
		int num = Math.Max(0, Math.Min(_semanticSelectionLastStartOffset.Value, EditBox.Document.TextLength));
		int num2 = Math.Max(num, Math.Min(_semanticSelectionLastEndOffset.Value, EditBox.Document.TextLength));
		int activeOffset = Math.Max(0, Math.Min(_semanticSelectionLastActiveOffset ?? num2, EditBox.Document.TextLength));
		if (num2 <= num)
		{
			return;
		}
		int normalizedSelectionStart = num;
		int normalizedSelectionEnd = num2;
		if (TryNormalizeCodeBlockSelectionRange(num, num2, activeOffset, out normalizedSelectionStart, out normalizedSelectionEnd))
		{
			num = Math.Max(0, Math.Min(normalizedSelectionStart, EditBox.Document.TextLength));
			num2 = Math.Max(num, Math.Min(normalizedSelectionEnd, EditBox.Document.TextLength));
		}
		bool suppressSemanticSelectionNormalization = _suppressSemanticSelectionNormalization;
		_suppressSemanticSelectionNormalization = true;
		try
		{
			ApplyNormalizedSemanticSelection(num, num2, activeOffset);
			_codeBlockSelectionLayer?.InvalidateVisual();
		}
		finally
		{
			_suppressSemanticSelectionNormalization = suppressSemanticSelectionNormalization;
		}
	}

	private int GetNormalizedSelectionActiveOffset(int selectionStart, int selectionEnd, int activeOffset)
	{
		if (_semanticSelectionAnchorOffset.HasValue)
		{
			if (Math.Max(selectionStart, Math.Min(_semanticSelectionAnchorOffset.Value, selectionEnd)) > selectionStart)
			{
				return selectionStart;
			}
			return selectionEnd;
		}
		if (activeOffset > selectionStart)
		{
			return selectionEnd;
		}
		return selectionStart;
	}

	private bool TryNormalizeCodeBlockSelectionRange(int selectionStart, int selectionEnd, int activeOffset, out int normalizedSelectionStart, out int normalizedSelectionEnd)
	{
		normalizedSelectionStart = selectionStart;
		normalizedSelectionEnd = selectionEnd;
		ClearObjectSelectedCodeBlocks();
		if (EditBox?.Document == null || selectionEnd <= selectionStart)
		{
			return false;
		}
		int num = Math.Max(0, Math.Min(selectionStart, EditBox.Document.TextLength));
		int num2 = Math.Max(num, Math.Min(selectionEnd, EditBox.Document.TextLength));
		if (num == 0 && num2 == EditBox.Document.TextLength)
		{
			return true;
		}
		int activeOffset2 = Math.Max(num, Math.Min(activeOffset, num2));
		int semanticSelectionAnchorOffset = GetSemanticSelectionAnchorOffset(num, num2, activeOffset2);
		bool treatAsSelectionEnd = !_semanticSelectionAnchorOffset.HasValue && semanticSelectionAnchorOffset == num2;
		if (TryGetCodeBlockAtSelectionOffset(semanticSelectionAnchorOffset, treatAsSelectionEnd, out var block))
		{
			if (IsCodeBlockAttachment(block) && TryGetCodeBlockOuterRange(block, out var blockStartOffset, out var blockEndOffset))
			{
				GetObjectSelectedCodeBlockKeys().Add(GetCodeBlockSelectionKey(block));
				normalizedSelectionStart = Math.Min(num, blockStartOffset);
				normalizedSelectionEnd = Math.Max(num2, blockEndOffset);
				return true;
			}
			return TryClampSelectionToCodeBlockContentRange(block, num, num2, out normalizedSelectionStart, out normalizedSelectionEnd);
		}
		return TryExpandSelectionToTouchedCodeBlockRanges(num, num2, out normalizedSelectionStart, out normalizedSelectionEnd);
	}

	private int GetSemanticSelectionAnchorOffset(int selectionStart, int selectionEnd, int activeOffset)
	{
		if (_semanticSelectionAnchorOffset.HasValue)
		{
			return Math.Max(selectionStart, Math.Min(_semanticSelectionAnchorOffset.Value, selectionEnd));
		}
		if (activeOffset != selectionStart)
		{
			return selectionStart;
		}
		return selectionEnd;
	}

	private int GetCodeBlockContentSelectionStart(int selectionStart)
	{
		int num = Math.Max(0, Math.Min(selectionStart, EditBox.Document.TextLength));
		DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(num);
		if (lineByOffset == null)
		{
			return num;
		}
		int lineIndex = Math.Max(0, lineByOffset.LineNumber - 1);
		if (!TryGetSemanticBlockAtLine(lineIndex, out var block) || block == null || block.Kind != MarkdownBlockKind.CodeBlock)
		{
			return num;
		}
		if (!TryGetCodeBlockEditableOffset(lineByOffset, lineIndex, block, out var targetOffset))
		{
			return num;
		}
		if (IsSemanticCodeBlockContentLine(EditBox.Document.GetText(lineByOffset), block, lineIndex))
		{
			return Math.Max(targetOffset, num);
		}
		return targetOffset;
	}

	private int GetCodeBlockContentSelectionEnd(int selectionStart, int selectionEnd)
	{
		int num = Math.Max(selectionStart, Math.Min(selectionEnd, EditBox.Document.TextLength));
		int val = ((num > selectionStart) ? (num - 1) : num);
		DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(Math.Max(0, Math.Min(val, EditBox.Document.TextLength)));
		if (lineByOffset == null)
		{
			return num;
		}
		int num2 = Math.Max(0, lineByOffset.LineNumber - 1);
		if (!TryGetSemanticBlockAtLine(num2, out var block) || block == null || block.Kind != MarkdownBlockKind.CodeBlock)
		{
			return num;
		}
		string text = EditBox.Document.GetText(lineByOffset);
		if (num2 == block.EndLine && IsSemanticCodeBlockClosingFenceLine(text, block, num2))
		{
			return Math.Max(lineByOffset.Offset, Math.Min(num, lineByOffset.Offset));
		}
		if (TryGetCodeBlockEditableOffset(lineByOffset, num2, block, out var targetOffset) && IsSemanticCodeBlockContentLine(text, block, num2) && num <= targetOffset)
		{
			return targetOffset;
		}
		if (num2 == block.StartLine && TryGetCodeBlockEditableOffset(lineByOffset, num2, block, out var targetOffset2))
		{
			return targetOffset2;
		}
		return num;
	}

	private bool TryGetCodeBlockAtSelectionOffset(int offset, bool treatAsSelectionEnd, out MarkdownBlockNode block)
	{
		block = null;
		if (EditBox?.Document == null)
		{
			return false;
		}
		int num = Math.Max(0, Math.Min(offset, EditBox.Document.TextLength));
		int val = ((treatAsSelectionEnd && num > 0) ? (num - 1) : num);
		val = Math.Max(0, Math.Min(val, EditBox.Document.TextLength));
		DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(val);
		if (lineByOffset == null)
		{
			return false;
		}
		int lineIndex = Math.Max(0, lineByOffset.LineNumber - 1);
		if (TryGetSemanticBlockAtLine(lineIndex, out block))
		{
			MarkdownBlockNode obj = block;
			if (obj != null && obj.Kind == MarkdownBlockKind.CodeBlock)
			{
				return true;
			}
		}
		block = null;
		return false;
	}

	private bool TryClampSelectionToCodeBlockContentRange(MarkdownBlockNode block, int selectionStart, int selectionEnd, out int normalizedSelectionStart, out int normalizedSelectionEnd)
	{
		normalizedSelectionStart = selectionStart;
		normalizedSelectionEnd = selectionEnd;
		if (!TryGetCodeBlockContentRange(block, out var contentStartOffset, out var contentEndOffset))
		{
			return false;
		}
		normalizedSelectionStart = Math.Max(selectionStart, contentStartOffset);
		normalizedSelectionEnd = Math.Min(selectionEnd, contentEndOffset);
		if (normalizedSelectionEnd < normalizedSelectionStart)
		{
			normalizedSelectionEnd = normalizedSelectionStart;
		}
		if (normalizedSelectionStart == selectionStart)
		{
			return normalizedSelectionEnd != selectionEnd;
		}
		return true;
	}

	private bool TryExpandSelectionToTouchedCodeBlockRanges(int selectionStart, int selectionEnd, out int normalizedSelectionStart, out int normalizedSelectionEnd)
	{
		normalizedSelectionStart = selectionStart;
		normalizedSelectionEnd = selectionEnd;
		if (_semanticDocument?.Blocks == null)
		{
			return false;
		}
		bool result = false;
		foreach (MarkdownBlockNode block in _semanticDocument.Blocks)
		{
			if (block != null && block.Kind == MarkdownBlockKind.CodeBlock && TryGetCodeBlockOuterRange(block, out var blockStartOffset, out var blockEndOffset) && blockEndOffset > selectionStart && blockStartOffset < selectionEnd)
			{
				result = true;
				GetObjectSelectedCodeBlockKeys().Add(GetCodeBlockSelectionKey(block));
				normalizedSelectionStart = Math.Min(normalizedSelectionStart, blockStartOffset);
				normalizedSelectionEnd = Math.Max(normalizedSelectionEnd, blockEndOffset);
			}
		}
		return result;
	}

	private bool TryGetCodeBlockOuterRange(MarkdownBlockNode block, out int blockStartOffset, out int blockEndOffset)
	{
		blockStartOffset = 0;
		blockEndOffset = 0;
		if (EditBox?.Document == null || block == null || block.Kind != MarkdownBlockKind.CodeBlock)
		{
			return false;
		}
		DocumentLine documentLine = TryGetDocumentLineBySemanticIndex(block.StartLine);
		DocumentLine documentLine2 = TryGetDocumentLineBySemanticIndex(block.EndLine);
		if (documentLine == null || documentLine2 == null)
		{
			return false;
		}
		blockStartOffset = documentLine.Offset;
		blockEndOffset = documentLine2.EndOffset + documentLine2.DelimiterLength;
		return true;
	}

	internal bool IsCodeBlockObjectSelected(MarkdownBlockNode block)
	{
		if (block != null)
		{
			return GetObjectSelectedCodeBlockKeys().Contains(GetCodeBlockSelectionKey(block));
		}
		return false;
	}

	private void ClearObjectSelectedCodeBlocks()
	{
		HashSet<string> objectSelectedCodeBlockKeys = GetObjectSelectedCodeBlockKeys();
		if (objectSelectedCodeBlockKeys.Count != 0)
		{
			objectSelectedCodeBlockKeys.Clear();
			_suppressParentCaretForCodeBlockObjectSelection = false;
			_codeBlockSelectionLayer?.InvalidateVisual();
			RefreshParentCaretVisibilityForObjectSelection();
		}
	}

	private void RefreshParentCaretVisibilityForObjectSelection()
	{
		Caret caret = EditBox?.TextArea?.Caret;
		if (caret != null)
		{
			bool num = ShouldSuppressParentCaret(EditBox.IsReadOnly, _suppressParentCaretForCodeBlockAttachment, _suppressParentCaretForCodeBlockObjectSelection);
			caret.CaretBrush = ResolveParentCaretBrush(_textColor);
			if (num)
			{
				caret.Hide();
			}
			else if (EditBox.IsKeyboardFocusWithin)
			{
				caret.Show();
			}
		}
	}

	private HashSet<string> GetObjectSelectedCodeBlockKeys()
	{
		return _objectSelectedCodeBlockKeys ?? (_objectSelectedCodeBlockKeys = new HashSet<string>());
	}

	private static string GetCodeBlockSelectionKey(MarkdownBlockNode block)
	{
		if (block != null)
		{
			return block.StartLine + ":" + block.EndLine;
		}
		return string.Empty;
	}

	private bool TryGetCodeBlockContentRange(MarkdownBlockNode block, out int contentStartOffset, out int contentEndOffset)
	{
		contentStartOffset = 0;
		contentEndOffset = 0;
		if (EditBox?.Document == null || block == null || block.Kind != MarkdownBlockKind.CodeBlock)
		{
			return false;
		}
		int num = block.StartLine + 1;
		int lastCodeBlockContentLineIndex = GetLastCodeBlockContentLineIndex(block);
		if (num > lastCodeBlockContentLineIndex)
		{
			return false;
		}
		DocumentLine documentLine = TryGetDocumentLineBySemanticIndex(num);
		DocumentLine documentLine2 = TryGetDocumentLineBySemanticIndex(lastCodeBlockContentLineIndex);
		if (documentLine == null || documentLine2 == null)
		{
			return false;
		}
		string text = EditBox.Document.GetText(documentLine);
		contentStartOffset = GetSemanticContentStartOffset(text, block, documentLine.Offset, documentLine.EndOffset);
		contentEndOffset = documentLine2.EndOffset;
		return true;
	}

	private int GetLastCodeBlockContentLineIndex(MarkdownBlockNode block)
	{
		if (block == null || block.Kind != MarkdownBlockKind.CodeBlock)
		{
			return -1;
		}
		DocumentLine documentLine = TryGetDocumentLineBySemanticIndex(block.EndLine);
		if (documentLine == null)
		{
			return block.EndLine;
		}
		if (!IsSemanticCodeBlockClosingFenceLine(EditBox.Document.GetText(documentLine), block, block.EndLine))
		{
			return block.EndLine;
		}
		return block.EndLine - 1;
	}

	private DocumentLine TryGetDocumentLineBySemanticIndex(int lineIndex)
	{
		if (EditBox?.Document == null || lineIndex < 0)
		{
			return null;
		}
		int num = lineIndex + 1;
		if (num < 1 || num > EditBox.Document.LineCount)
		{
			return null;
		}
		return EditBox.Document.GetLineByNumber(num);
	}

	private int? TryGetDocumentOffsetFromTextViewPoint(Point textViewPoint)
	{
		if (EditBox?.Document == null || EditBox.TextArea?.TextView == null)
		{
			return null;
		}
		try
		{
			TextView textView = EditBox.TextArea.TextView;
			TextViewPosition? positionFloor = textView.GetPositionFloor(textViewPoint + textView.ScrollOffset);
			if (!positionFloor.HasValue)
			{
				return null;
			}
			return Math.Max(0, Math.Min(EditBox.Document.GetOffset(positionFloor.Value.Location), EditBox.Document.TextLength));
		}
		catch (InvalidOperationException)
		{
			return null;
		}
		catch (ArgumentException)
		{
			return null;
		}
	}

	private bool TryGetCodeBlockEditableOffset(DocumentLine line, int lineIndex, MarkdownBlockNode block, out int targetOffset)
	{
		targetOffset = 0;
		if (line == null || block == null || block.Kind != MarkdownBlockKind.CodeBlock)
		{
			return false;
		}
		string text = EditBox.Document.GetText(line);
		if (lineIndex == block.StartLine && IsSemanticCodeBlockOpeningFenceLine(text, block, lineIndex))
		{
			DocumentLine nextLine = line.NextLine;
			if (nextLine == null)
			{
				return false;
			}
			string text2 = EditBox.Document.GetText(nextLine);
			int num = Math.Max(0, nextLine.LineNumber - 1);
			if (num == block.EndLine && IsSemanticCodeBlockClosingFenceLine(text2, block, num))
			{
				targetOffset = line.EndOffset;
				return true;
			}
			targetOffset = GetSemanticContentStartOffset(text2, block, nextLine.Offset, nextLine.EndOffset);
			return true;
		}
		if (lineIndex == block.EndLine && IsSemanticCodeBlockClosingFenceLine(text, block, lineIndex))
		{
			DocumentLine previousLine = line.PreviousLine;
			if (previousLine == null)
			{
				return false;
			}
			targetOffset = previousLine.EndOffset;
			return true;
		}
		if (lineIndex < block.StartLine || lineIndex > block.EndLine)
		{
			return false;
		}
		targetOffset = GetSemanticContentStartOffset(text, block, line.Offset, line.EndOffset);
		return true;
	}

	private static bool IsSemanticCodeBlockOpeningFenceLine(string lineText, MarkdownBlockNode block, int lineIndex)
	{
		int indent;
		string language;
		if (block != null && block.Kind == MarkdownBlockKind.CodeBlock && lineIndex == block.StartLine)
		{
			return TryGetEditingCodeFenceInfo(lineText, out indent, out language);
		}
		return false;
	}

	private static bool IsSemanticCodeBlockClosingFenceLine(string lineText, MarkdownBlockNode block, int lineIndex)
	{
		int indent;
		if (block != null && block.Kind == MarkdownBlockKind.CodeBlock && lineIndex == block.EndLine)
		{
			return TryGetEditingCodeFenceClosingInfo(lineText, out indent);
		}
		return false;
	}

	private static bool IsSemanticCodeBlockContentLine(string lineText, MarkdownBlockNode block, int lineIndex)
	{
		if (block == null || block.Kind != MarkdownBlockKind.CodeBlock || lineIndex <= block.StartLine || lineIndex > block.EndLine)
		{
			return false;
		}
		return !IsSemanticCodeBlockClosingFenceLine(lineText, block, lineIndex);
	}

	private bool CanHandleSemanticTabKey()
	{
		if (EditBox?.Document == null)
		{
			return false;
		}
		if (EditBox.SelectionLength > 0)
		{
			Selection selection = EditBox.TextArea.Selection;
			if (selection == null)
			{
				return false;
			}
			int num = Math.Max(1, Math.Min(selection.StartPosition.Line, selection.EndPosition.Line));
			int num2 = Math.Max(num, Math.Max(selection.StartPosition.Line, selection.EndPosition.Line));
			for (int i = num; i <= num2; i++)
			{
				if (TryGetSemanticBlockAtLine(i - 1, out var block) && block != null && (block.Kind == MarkdownBlockKind.List || block.Kind == MarkdownBlockKind.BlockQuote || block.Kind == MarkdownBlockKind.Paragraph || block.Kind == MarkdownBlockKind.CodeBlock))
				{
					return true;
				}
			}
			return false;
		}
		DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(EditBox.CaretOffset);
		if (lineByOffset == null)
		{
			return false;
		}
		if (TryGetSemanticBlockAtLine(lineByOffset.LineNumber - 1, out var block2) && block2 != null)
		{
			if (block2.Kind != MarkdownBlockKind.List && block2.Kind != MarkdownBlockKind.BlockQuote && block2.Kind != MarkdownBlockKind.Paragraph)
			{
				return block2.Kind == MarkdownBlockKind.CodeBlock;
			}
			return true;
		}
		return false;
	}

	private static bool StartsWithAt(string text, int offset, string value)
	{
		return MarkdownDisplayTextNormalizer.StartsWithAt(text, offset, value);
	}

	internal static string NormalizeDisplayTaskMarkersForPersistence(string text)
	{
		return MarkdownDisplayTextNormalizer.NormalizeDisplayTaskMarkersForPersistence(text);
	}

	internal static string NormalizeDisplayTaskMarkerLine(string lineText)
	{
		return MarkdownDisplayTextNormalizer.NormalizeDisplayTaskMarkerLine(lineText);
	}

	internal static string NormalizePersistenceTextForEditorDisplay(string text)
	{
		return MarkdownDisplayTextNormalizer.NormalizePersistenceTextForEditorDisplay(text);
	}

	private static string NormalizeLegacyDisplayPlaceholders(string text)
	{
		return MarkdownDisplayTextNormalizer.NormalizeLegacyDisplayPlaceholders(text);
	}

	private static string ConvertRawLineToDisplayLine(string lineText)
	{
		return MarkdownDisplayTextNormalizer.ConvertRawLineToDisplayLine(lineText);
	}

	private static MarkdownBlockNode ConvertSyntaxInfoToBlockNode(MarkdownBlockSyntaxInfo syntax)
	{
		return MarkdownDisplayTextNormalizer.ConvertSyntaxInfoToBlockNode(syntax);
	}

	private static int ConvertIndentLevelsToWhitespaceCount(int indentLevels)
	{
		return MarkdownDisplayTextNormalizer.ConvertIndentLevelsToWhitespaceCount(indentLevels);
	}

	internal static string BuildDisplayHeadingMarker(int level)
	{
		return MarkdownDisplayTextNormalizer.BuildDisplayHeadingMarker(level);
	}

	internal static string BuildDisplayOrderedListMarker(string numberText)
	{
		return MarkdownDisplayTextNormalizer.BuildDisplayOrderedListMarker(numberText);
	}

	private static string GetDisplayBulletMarker(string rawMarker)
	{
		return MarkdownDisplayTextNormalizer.GetDisplayBulletMarker(rawMarker);
	}

	private static bool TryGetDisplayBulletMarker(string lineText, int offset, out string rawMarker)
	{
		return MarkdownDisplayTextNormalizer.TryGetDisplayBulletMarker(lineText, offset, out rawMarker);
	}

	private static bool TryGetDisplayHeadingLevel(string lineText, int offset, out int level)
	{
		return MarkdownDisplayTextNormalizer.TryGetDisplayHeadingLevel(lineText, offset, out level);
	}

	private static bool TryGetDisplayOrderedNumberText(string lineText, int offset, out string numberText, out int markerLength)
	{
		return MarkdownDisplayTextNormalizer.TryGetDisplayOrderedNumberText(lineText, offset, out numberText, out markerLength);
	}

	private static bool TryGetBulletListMarkerInfo(string lineText, out int markerOffset, out int markerLength, out string rawMarker)
	{
		return MarkdownDisplayTextNormalizer.TryGetBulletListMarkerInfo(lineText, out markerOffset, out markerLength, out rawMarker);
	}

	private async void TryDownLoadUrlImage(string matchUrl, string filePath)
	{
		new Thread((ThreadStart)async delegate
		{
			try
			{
				byte[] buffer = new WebClient().DownloadData(matchUrl);
				BitmapImage bitmapImage = new BitmapImage();
				using (MemoryStream streamSource = new MemoryStream(buffer))
				{
					bitmapImage.BeginInit();
					bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
					bitmapImage.StreamSource = streamSource;
					bitmapImage.EndInit();
					bitmapImage.Freeze();
				}
				if (!bitmapImage.IsDownloading && bitmapImage.Height > 1.0 && bitmapImage.Width > 1.0)
				{
					BitmapEncoder bitmapEncoder = new PngBitmapEncoder
					{
						Frames = { BitmapFrame.Create(bitmapImage) }
					};
					using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
					{
						bitmapEncoder.Save(fileStream);
						fileStream.Close();
					}
					Dispatcher?.Invoke(delegate
					{
						EditBox.TextArea.TextView.Redraw();
					});
				}
				else
				{
					ImageElementGenerator.NotImageUrlSet.Add(matchUrl.GetHashCode());
				}
			}
			catch (NotSupportedException)
			{
			}
			catch (Exception)
			{
			}
		}).Start();
	}

	public void InvalidMargin()
	{
		foreach (UIElement leftMargin in EditBox.TextArea.LeftMargins)
		{
			leftMargin.InvalidateVisual();
		}
	}

	private void OnPaste(object sender, DataObjectPastingEventArgs arg)
	{
		if (CodeBlockAttachmentControl.TryFindFromOriginalSource(arg?.OriginalSource, out var control) && control.IsSourceInsideCodeEditor(arg.OriginalSource))
		{
			System.Windows.IDataObject fallbackDataObject = null;
			if (arg == null || !arg.IsDragDrop)
			{
				try
				{
					fallbackDataObject = System.Windows.Clipboard.GetDataObject();
				}
				catch (Exception)
				{
				}
			}
			if (control.TryHandlePaste(arg.SourceDataObject, fallbackDataObject))
			{
				arg.CancelCommand();
			}
			return;
		}
		try
		{
			arg.CancelCommand();
			if (EditBox.SelectionLength > 0)
			{
				HandleSelectionBehindAttachment();
			}
			ClipboardRichTextContent clipboardRichTextContent;
			try
			{
				clipboardRichTextContent = ClipboardRichTextHelper.Extract(arg.SourceDataObject, System.Windows.Clipboard.GetDataObject());
			}
			catch (Exception)
			{
				clipboardRichTextContent = ClipboardRichTextHelper.Extract(arg.SourceDataObject);
			}
			if (clipboardRichTextContent.IsLiteralExport)
			{
				HandlePastePlainTextAsync(clipboardRichTextContent.PlainText, literalExport: true);
				return;
			}
			int pasteStartOffset = GetPasteStartOffset();
			int selectionLength = EditBox.SelectionLength;
			if (ShouldPastePlainTextInsideCodeBlock(pasteStartOffset, selectionLength))
			{
				HandlePastePlainTextAsync(GetPlainTextForCodeBlockPaste(clipboardRichTextContent));
			}
			else if (!clipboardRichTextContent.ShouldParseEmphasis)
			{
				HandlePasteTextAsync(clipboardRichTextContent.PreferredText);
			}
			else
			{
				HandleStructuredPasteTextAsync(clipboardRichTextContent.PreferredText);
			}
		}
		catch (Exception)
		{
		}
	}

	private int GetPasteStartOffset()
	{
		int result = EditBox?.CaretOffset ?? 0;
		if ((EditBox?.SelectionLength ?? 0) <= 0)
		{
			return result;
		}
		if (EditBox.CaretOffset >= EditBox.SelectionStart && EditBox.CaretOffset <= EditBox.SelectionStart + EditBox.SelectionLength)
		{
			return EditBox.SelectionStart;
		}
		if (EditBox.CaretOffset > EditBox.SelectionStart + EditBox.SelectionLength)
		{
			return EditBox.SelectionStart + (EditBox.CaretOffset - (EditBox.SelectionStart + EditBox.SelectionLength));
		}
		return result;
	}

	private static string GetPlainTextForCodeBlockPaste(ClipboardRichTextContent clipboardContent)
	{
		if (clipboardContent == null)
		{
			return string.Empty;
		}
		if (string.IsNullOrEmpty(clipboardContent.PlainText))
		{
			return clipboardContent.PreferredText;
		}
		return clipboardContent.PlainText;
	}

	private bool ShouldPastePlainTextInsideCodeBlock(int startOffset, int removedLength)
	{
		if (EditBox?.Document == null)
		{
			return false;
		}
		int textLength = EditBox.Document.TextLength;
		int num = Math.Max(0, Math.Min(startOffset, textLength));
		int num2 = Math.Max(num, Math.Min(num + Math.Max(0, removedLength), textLength));
		if (!TryGetCodeBlockAtSelectionOffset(num, treatAsSelectionEnd: false, out var block) || !TryGetCodeBlockContentRange(block, out var contentStartOffset, out var contentEndOffset) || num < contentStartOffset || num > contentEndOffset)
		{
			return false;
		}
		if (num2 == num)
		{
			return true;
		}
		if (num2 > contentEndOffset || !TryGetCodeBlockAtSelectionOffset(num2, treatAsSelectionEnd: true, out var block2))
		{
			return false;
		}
		return string.Equals(GetCodeBlockSelectionKey(block), GetCodeBlockSelectionKey(block2), StringComparison.Ordinal);
	}

	public void PastePlainText()
	{
		try
		{
			if (!ReadOnly)
			{
				if (EditBox.SelectionLength > 0)
				{
					HandleSelectionBehindAttachment();
				}
				System.Windows.IDataObject dataObject;
				try
				{
					dataObject = System.Windows.Clipboard.GetDataObject();
				}
				catch (Exception)
				{
					return;
				}
				ClipboardRichTextContent clipboardRichTextContent = ClipboardRichTextHelper.Extract(dataObject);
				string text = (clipboardRichTextContent.IsLiteralExport ? clipboardRichTextContent.PlainText : GetPlainTextForPlainPaste(dataObject));
				HandlePastePlainTextAsync(text, clipboardRichTextContent.IsLiteralExport);
			}
		}
		catch (Exception)
		{
		}
	}

	private string GetPlainTextForPlainPaste(System.Windows.IDataObject dataObject)
	{
		return TryGetClipboardStringData(dataObject, "TickTickPlainText") ?? ClipboardRichTextHelper.ExtractPlainText(dataObject);
	}

	private static string TryGetClipboardStringData(System.Windows.IDataObject dataObject, string format)
	{
		try
		{
			return (dataObject != null && dataObject.GetDataPresent(format, autoConvert: false)) ? (dataObject.GetData(format, autoConvert: false) as string) : null;
		}
		catch (Exception)
		{
			return null;
		}
	}

	private async Task HandlePastePlainTextAsync(string text, bool literalExport = false)
	{
		try
		{
			int contentLengthForMaxLength = GetContentLengthForMaxLength();
			int selectionLengthForMaxLength = GetSelectionLengthForMaxLength(EditBox.SelectionStart, EditBox.SelectionLength);
			if (MaxLength >= 0 && text != null && contentLengthForMaxLength + text.Length - selectionLengthForMaxLength > MaxLength)
			{
				text = text.Substring(0, Math.Max(0, MaxLength - contentLengthForMaxLength + selectionLengthForMaxLength));
			}
			if (string.IsNullOrEmpty(text))
			{
				return;
			}
			TryNormalizeCaretBeforeCodeBlockAttachment(text.Length, out var blockedByMaxLength);
			if (blockedByMaxLength)
			{
				Utils.Toast(Utils.GetString("CharactersNumberLimit"));
				return;
			}
			TryNormalizeCaretAfterCodeBlockAttachment(text.Length, out var _, out var blockedByMaxLength2);
			if (blockedByMaxLength2)
			{
				Utils.Toast(Utils.GetString("CharactersNumberLimit"));
				return;
			}
			if (AttachmentDict.ContainsKey(EditBox.CaretOffset))
			{
				ReplaceEditorText(EditBox.CaretOffset, 0, "\n", EditBox.CaretOffset);
			}
			else if (AttachmentEndSet.Contains(EditBox.CaretOffset))
			{
				ReplaceEditorText(EditBox.CaretOffset, 0, "\n", EditBox.CaretOffset + 1);
			}
			string text2 = text.TrimEnd();
			if (!literalExport && text.Length - text2.Length > 100)
			{
				text = text2;
			}
			int num = EditBox.CaretOffset;
			int selectionLength = EditBox.SelectionLength;
			if (selectionLength > 0)
			{
				if (EditBox.CaretOffset >= EditBox.SelectionStart && EditBox.CaretOffset <= EditBox.SelectionStart + EditBox.SelectionLength)
				{
					num = EditBox.SelectionStart;
				}
				if (EditBox.CaretOffset > EditBox.SelectionStart + EditBox.SelectionLength)
				{
					num = EditBox.SelectionStart + (EditBox.CaretOffset - (EditBox.SelectionStart + EditBox.SelectionLength));
				}
			}
			if (literalExport && !ShouldPastePlainTextInsideCodeBlock(num, selectionLength))
			{
				InsertPreparedEditorText(PrepareExportedEditorTextState(text), num, selectionLength, convertCompletedMarkdownLink: false);
			}
			else
			{
				ReplaceEditorText(num, selectionLength, text, num + text.Length, delegate(int insertedOffset, int insertedLength)
				{
					ClearInlineSemanticMarksInRange(insertedOffset, insertedLength);
				});
			}
			await Task.Delay(5);
			NotifyOffsetChanged();
		}
		catch (Exception)
		{
		}
	}

	private void ClearInlineSemanticMarksInRange(int startOffset, int length)
	{
		if (length > 0 && (0u | (ClearInlineSemanticMarksInRange(SemanticMarkList, startOffset, length) ? 1u : 0u) | (ClearInlineSemanticMarksInRange(_typedSemanticMarks, startOffset, length) ? 1u : 0u)) != 0)
		{
			_needsTypedSemanticMarkRedraw = true;
		}
	}

	private static bool ClearInlineSemanticMarksInRange(IList<MarkdownInlineDisplaySegment> marks, int startOffset, int length)
	{
		if (marks == null || marks.Count == 0 || length <= 0)
		{
			return false;
		}
		bool result = false;
		int num = Math.Max(0, startOffset);
		int num2 = num + Math.Max(0, length);
		for (int num3 = marks.Count - 1; num3 >= 0; num3--)
		{
			MarkdownInlineDisplaySegment markdownInlineDisplaySegment = marks[num3];
			if (markdownInlineDisplaySegment == null)
			{
				marks.RemoveAt(num3);
				result = true;
			}
			else
			{
				int sourcePosition = markdownInlineDisplaySegment.SourcePosition;
				int num4 = markdownInlineDisplaySegment.SourcePosition + markdownInlineDisplaySegment.SourceLength;
				if (num < num4 && num2 > sourcePosition)
				{
					if (num <= sourcePosition && num2 >= num4)
					{
						marks.RemoveAt(num3);
						result = true;
					}
					else if (num <= sourcePosition)
					{
						markdownInlineDisplaySegment.SourcePosition = num2;
						markdownInlineDisplaySegment.SourceLength = Math.Max(0, num4 - num2);
						result = true;
					}
					else if (num2 >= num4)
					{
						markdownInlineDisplaySegment.SourceLength = Math.Max(0, num - sourcePosition);
						result = true;
					}
					else
					{
						MarkdownInlineDisplaySegment markdownInlineDisplaySegment2 = CloneInlineDisplaySegment(markdownInlineDisplaySegment);
						markdownInlineDisplaySegment2.SourcePosition = num2;
						markdownInlineDisplaySegment2.SourceLength = num4 - num2;
						markdownInlineDisplaySegment.SourceLength = num - sourcePosition;
						marks.Insert(num3 + 1, markdownInlineDisplaySegment2);
						result = true;
					}
				}
			}
		}
		return result;
	}

	private static MarkdownInlineDisplaySegment CloneInlineDisplaySegment(MarkdownInlineDisplaySegment mark)
	{
		return new MarkdownInlineDisplaySegment
		{
			Kind = mark.Kind,
			SourcePosition = mark.SourcePosition,
			SourceLength = mark.SourceLength,
			OpeningDelimiterLength = mark.OpeningDelimiterLength,
			ClosingDelimiterLength = mark.ClosingDelimiterLength,
			Color = mark.Color,
			LinkText = mark.LinkText,
			Url = mark.Url,
			UrlPosition = mark.UrlPosition,
			UrlLength = mark.UrlLength
		};
	}

	private async Task HandlePasteTextAsync(string text)
	{
		await HandlePasteTextAsyncCore(text, parseEmphasis: false);
	}

	private async Task HandleStructuredPasteTextAsync(string text)
	{
		await HandlePasteTextAsyncCore(text, parseEmphasis: true);
	}

	private async Task HandlePasteTextAsyncCore(string text, bool parseEmphasis)
	{
		_ = 3;
		try
		{
			int contentLengthForMaxLength = GetContentLengthForMaxLength();
			int selectionLengthForMaxLength = GetSelectionLengthForMaxLength(EditBox.SelectionStart, EditBox.SelectionLength);
			if (MaxLength >= 0 && text != null && contentLengthForMaxLength + text.Length - selectionLengthForMaxLength > MaxLength)
			{
				text = text.Substring(0, Math.Max(0, MaxLength - contentLengthForMaxLength + selectionLengthForMaxLength));
			}
			if (string.IsNullOrEmpty(text))
			{
				return;
			}
			TryNormalizeCaretBeforeCodeBlockAttachment(text.Length, out var blockedByMaxLength);
			if (blockedByMaxLength)
			{
				Utils.Toast(Utils.GetString("CharactersNumberLimit"));
				return;
			}
			TryNormalizeCaretAfterCodeBlockAttachment(text.Length, out var _, out var blockedByMaxLength2);
			if (blockedByMaxLength2)
			{
				Utils.Toast(Utils.GetString("CharactersNumberLimit"));
				return;
			}
			if (AttachmentDict.ContainsKey(EditBox.CaretOffset))
			{
				ReplaceEditorText(EditBox.CaretOffset, 0, "\n", EditBox.CaretOffset);
			}
			else if (AttachmentEndSet.Contains(EditBox.CaretOffset))
			{
				ReplaceEditorText(EditBox.CaretOffset, 0, "\n", EditBox.CaretOffset + 1);
			}
			ProjectTask projectTask = TaskUtils.ParseTaskUrl(text) ?? TaskUtils.ParseTaskUrlWithoutTitle(text);
			if (projectTask != null && !text.StartsWith("[") && !text.EndsWith(")"))
			{
				string text2 = text.Replace(" " + projectTask.Title, string.Empty);
				await TryPasteLinkTask(projectTask, text2);
				return;
			}
			string text3 = await HandlePasteAttachment(text);
			if (text3 == null)
			{
				return;
			}
			if (!string.IsNullOrEmpty(text3))
			{
				text = text3;
			}
			string text4 = text.TrimEnd();
			if (text.Length - text4.Length > 100)
			{
				text = text4;
			}
			text = EscapeMarkdownSyntaxInPastedUrls(text);
			int num = EditBox.CaretOffset;
			int selectionLength = EditBox.SelectionLength;
			if (selectionLength > 0)
			{
				if (EditBox.CaretOffset >= EditBox.SelectionStart && EditBox.CaretOffset <= EditBox.SelectionStart + EditBox.SelectionLength)
				{
					num = EditBox.SelectionStart;
				}
				if (EditBox.CaretOffset > EditBox.SelectionStart + EditBox.SelectionLength)
				{
					num = EditBox.SelectionStart + (EditBox.CaretOffset - (EditBox.SelectionStart + EditBox.SelectionLength));
				}
			}
			PreparedEditorTextState preparedEditorTextState = (parseEmphasis ? PrepareStructuredPastedEditorTextState(text) : PreparePastedEditorTextState(text));
			bool flag = preparedEditorTextState.SemanticDocument?.Blocks.Any((MarkdownBlockNode block) => block != null && (block.Kind != MarkdownBlockKind.Paragraph || block.Level.HasValue || block.ListKind.HasValue || block.Checked.HasValue || Math.Max(0, block.Indent) > 0)) ?? false;
			if (!string.Equals(preparedEditorTextState.DisplayText, text, StringComparison.Ordinal) || preparedEditorTextState.SemanticMarks.Count > 0 || flag)
			{
				InsertPreparedEditorText(preparedEditorTextState, num, selectionLength);
			}
			else
			{
				ReplaceEditorText(num, selectionLength, text, num + text.Length);
			}
			if (text3 == string.Empty)
			{
				await Task.Delay(5);
				CheckIfExtraPaste(text);
			}
			await Task.Delay(5);
			NotifyOffsetChanged();
		}
		catch (Exception)
		{
		}
	}

	private string EscapeMarkdownSyntaxInPastedUrls(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text ?? string.Empty;
		}
		StringBuilder stringBuilder = new StringBuilder(text.Length);
		int num = 0;
		int num2 = 0;
		int i = 0;
		foreach (PersistedMarkdownLinkMatch item in EnumeratePersistedMarkdownLinks(text, includeImages: true, singleLine: true))
		{
			if (item.Index < i)
			{
				continue;
			}
			int num3 = item.Index + Math.Min(2, text.Length - item.Index);
			while (num2 < item.Index)
			{
				Match match = TaskUtils.UrlRegex.Match(text, num2, num3 - num2);
				if (!match.Success || match.Index >= item.Index)
				{
					break;
				}
				for (i = match.Index + match.Length; i < text.Length && !char.IsWhiteSpace(text[i]); i++)
				{
				}
				num2 = i;
				if (item.Index < i)
				{
					break;
				}
			}
			if (item.Index >= i)
			{
				stringBuilder.Append(EscapeBareUrlsInPastedText(text.Substring(num, item.Index - num)));
				stringBuilder.Append(text, item.Index, item.Length);
				num = item.Index + item.Length;
				num2 = num;
			}
		}
		stringBuilder.Append(EscapeBareUrlsInPastedText(text.Substring(num)));
		return stringBuilder.ToString();
	}

	private static string EscapeBareUrlsInPastedText(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text ?? string.Empty;
		}
		List<Match> list = (from Match match in TaskUtils.UrlRegex.Matches(text)
			where match.Success && match.Length > 0
			select match).ToList();
		if (list.Count == 0)
		{
			return text;
		}
		StringBuilder stringBuilder = new StringBuilder(text.Length);
		int num = 0;
		bool flag = false;
		foreach (Match item in list)
		{
			if (item.Index >= num)
			{
				stringBuilder.Append(text, num, item.Index - num);
				string value = item.Value;
				string text2 = (IsMarkdownLinkDestinationStart(text, item.Index) ? value : EscapeBareUrlMarkdownSyntax(value));
				stringBuilder.Append(text2);
				flag |= !string.Equals(text2, value, StringComparison.Ordinal);
				num = item.Index + item.Length;
			}
		}
		if (!flag)
		{
			return text;
		}
		stringBuilder.Append(text, num, text.Length - num);
		return stringBuilder.ToString();
	}

	private static string EscapeBareUrlMarkdownSyntax(string url)
	{
		if (string.IsNullOrEmpty(url))
		{
			return url ?? string.Empty;
		}
		StringBuilder stringBuilder = new StringBuilder(url.Length);
		int num = 0;
		foreach (char c in url)
		{
			if (c == '\\')
			{
				stringBuilder.Append(c);
				num++;
				continue;
			}
			if ("!*~[]`_={}()".IndexOf(c) >= 0 && num % 2 == 0)
			{
				stringBuilder.Append('\\');
			}
			stringBuilder.Append(c);
			num = 0;
		}
		return stringBuilder.ToString();
	}

	private static bool IsMarkdownLinkDestinationStart(string text, int urlStartIndex)
	{
		int num = Math.Min(urlStartIndex, text?.Length ?? 0) - 1;
		while (num >= 0 && (text[num] == ' ' || text[num] == '\t'))
		{
			num--;
		}
		if (num >= 1 && text[num] == '(')
		{
			return text[num - 1] == ']';
		}
		return false;
	}

	private void NotifyOffsetChanged()
	{
		int line = EditBox.TextArea.Caret.GetPosition().Line;
		if (TryGetVisualOffset(new TextViewPosition(line, Math.Max(1, EditBox.TextArea.Caret.Column)), VisualYPosition.LineTop, out var offset) && CurrentFocused && !_suppressCaretVerticalOffsetNotification && !_internalSelectionDropCaretRestorePending && !_codeBlockParagraphExitCaretPlacementPending)
		{
			this.CaretVerticalOffsetChanged?.Invoke(this, offset);
		}
	}

	private bool TryGetVisualOffset(TextViewPosition position, VisualYPosition yPosition, out double offset)
	{
		offset = -1.0;
		try
		{
			TextView textView = EditBox?.TextArea?.TextView;
			if (textView == null)
			{
				return false;
			}
			offset = textView.GetVisualPosition(position, yPosition).Y + 21.0;
			return true;
		}
		catch (InvalidOperationException ex)
		{
			UtilLog.Error(ex);
			return false;
		}
	}

	private async Task<string> HandlePasteAttachment(string text)
	{
		if (!string.IsNullOrEmpty(_editingTaskId))
		{
			List<AttachmentInfo> attachmentInfos = GetSelectedAttachment();
			text = text.Replace("\r\n", "\n");
			Dictionary<Match, AttachmentModel> pastAttachments = GetAttachmentsInText(text);
			if (pastAttachments.Count > 0)
			{
				TaskDetailView taskDetailView = Utils.FindParent<TaskDetailView>(this);
				bool flag = taskDetailView != null;
				if (flag)
				{
					flag = !(await taskDetailView.CheckAttachmentLimit(pastAttachments.Count, attachmentInfos?.Count ?? 0));
				}
				if (flag)
				{
					return null;
				}
				DocumentLine line = EditBox.TextArea.Document.GetLineByOffset(EditBox.CaretOffset);
				StringBuilder newText = new StringBuilder();
				int index = 0;
				List<Match> list = pastAttachments.Keys.ToList();
				list.Sort((Match a, Match b) => a.Index.CompareTo(b.Index));
				foreach (Match match in list)
				{
					if (match.Index == 0 && line.Offset != EditBox.CaretOffset)
					{
						newText.Insert(0, "\n");
					}
					AttachmentModel attachmentModel = pastAttachments[match];
					List<AttachmentInfo> list2 = attachmentInfos?.Where((AttachmentInfo info) => string.Equals(info.Value, match.Value, StringComparison.CurrentCultureIgnoreCase)).ToList();
					string text2 = string.Empty;
					if (list2 != null && list2.Count > 0)
					{
						list2.ForEach(delegate(AttachmentInfo info)
						{
							attachmentInfos.Remove(info);
						});
					}
					else if (attachmentModel.taskId != _editingTaskId || attachmentModel.status != 1 || attachmentModel.deleted)
					{
						text2 = Utils.GetGuid();
					}
					newText.Append(text.Substring(index, match.Index - index));
					index = match.Index + match.Length;
					if (!string.IsNullOrEmpty(text2))
					{
						newText.Append(match.Value.Replace(attachmentModel.id, text2));
						await AttachmentDao.CopyAttachment(attachmentModel, text2, _editingTaskId);
					}
					else
					{
						newText.Append(match.Value);
					}
					if (match.Index + match.Length == text.Length && line.EndOffset != EditBox.CaretOffset)
					{
						newText.Append("\n");
					}
				}
				if (index != text.Length)
				{
					newText.Append(text.Substring(index));
				}
				text = newText.ToString();
				return text;
			}
		}
		return string.Empty;
	}

	private Dictionary<Match, AttachmentModel> GetAttachmentsInText(string text)
	{
		Dictionary<Match, AttachmentModel> dictionary = new Dictionary<Match, AttachmentModel>();
		MatchCollection matchCollection = AttachmentRegex.Matches(text.ToLower());
		for (int i = 0; i < matchCollection.Count; i++)
		{
			Match match = matchCollection[i];
			if (IsInternalAttachmentMatch(text, match) && (match.Index <= 0 || match.Index + match.Length >= text.Length || (text[match.Index - 1] == '\n' && text[match.Index + match.Length] == '\n')))
			{
				AttachmentModel attachmentInUrl = AttachmentDao.GetAttachmentInUrl(match.Groups[2].Value);
				if (attachmentInUrl != null)
				{
					dictionary.Add(match, attachmentInUrl);
				}
			}
		}
		return dictionary;
	}

	private List<AttachmentInfo> GetSelectedAttachment()
	{
		if (EditBox.SelectionLength <= 0)
		{
			return null;
		}
		return AttachmentDict.Values.ToList().Where(delegate(AttachmentInfo info)
		{
			if (Uri.IsWellFormedUriString(info.Url, UriKind.Absolute))
			{
				return false;
			}
			return (EditBox.SelectionStart <= info.Offset && EditBox.SelectionStart + EditBox.SelectionLength >= info.Offset + info.Value.Length && AttachmentDao.GetAttachmentInUrl(info.Url) != null) ? true : false;
		}).ToList();
	}

	private async Task TryPasteLinkTask(ProjectTask extra, string text)
	{
		string text2 = extra.Title;
		if (string.IsNullOrEmpty(text2))
		{
			TaskModel taskModel = await TaskDao.GetThinTaskById(extra.TaskId);
			text2 = ((taskModel == null || string.IsNullOrEmpty(taskModel.title)) ? Utils.GetString("MyTask") : taskModel.title);
		}
		ReplaceEditorRangeWithLinkDisplayText(EditBox.CaretOffset, 0, text2, text);
	}

	private void CheckIfExtraPaste(string content)
	{
		TextEditor editBox = EditBox;
		string checkTextExtra = GetCheckTextExtra(content);
		string text = content + checkTextExtra;
		string text2 = NormalizeDisplayTaskMarkersForPersistence(editBox.Text ?? string.Empty);
		if (text2.Contains(text) && !string.IsNullOrEmpty(checkTextExtra))
		{
			int num = text2.IndexOf(text, StringComparison.Ordinal);
			if (num >= 0)
			{
				ReplaceEditorText(num, text.Length, content, num + content.Length);
			}
		}
	}

	private string GetCheckTextExtra(string content)
	{
		content = NormalizeDisplayTaskMarkersForPersistence(content ?? string.Empty);
		List<string> source = new List<string> { "- [ ] ", "- [x] ", "- [X] ", "- ", "+ ", "* ", "> " };
		using (IEnumerator<string> enumerator = source.Where(content.StartsWith).GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				return enumerator.Current;
			}
		}
		string text = content.Split('\n').Last();
		using (IEnumerator<string> enumerator = source.Where(text.StartsWith).GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				return enumerator.Current;
			}
		}
		Match match = new Regex("(\\d+)\\. ", RegexOptions.Compiled).Match(text);
		if (!match.Success)
		{
			return string.Empty;
		}
		return match.Groups[0].Value;
	}

	private bool TryHandleSemanticBlockEnter(out bool lineCountChanged)
	{
		lineCountChanged = false;
		if (EditBox == null || EditBox.SelectionLength > 0)
		{
			return false;
		}
		TextDocument document = EditBox.Document;
		int caretOffset = EditBox.CaretOffset;
		DocumentLine documentLine = document?.GetLineByOffset(caretOffset);
		if (documentLine == null)
		{
			return false;
		}
		string text = document.GetText(documentLine.Offset, documentLine.Length);
		if (caretOffset == documentLine.EndOffset && TryHandleEmptyHeadingEnter(documentLine, text, out lineCountChanged))
		{
			return true;
		}
		if (TryHandleFoldedHeadingEnterAtCaret(caretOffset, out lineCountChanged))
		{
			return true;
		}
		if (TryHandleFoldedHeadingSplitEnter(documentLine, caretOffset, out lineCountChanged))
		{
			return true;
		}
		if (caretOffset != documentLine.EndOffset)
		{
			if (!TryHandleSemanticListSplitEnter(documentLine, text, caretOffset, out lineCountChanged) && !TryHandleBlockQuoteSplitEnter(documentLine, text, caretOffset, out lineCountChanged))
			{
				return TryHandleParagraphEnter(documentLine, text, out lineCountChanged);
			}
			return true;
		}
		if (TryHandleTaskListEnter(documentLine, text, out lineCountChanged))
		{
			return true;
		}
		if (TryHandleBulletListEnter(documentLine, text, out lineCountChanged))
		{
			return true;
		}
		if (TryHandleOrderedListEnter(documentLine, text, out lineCountChanged))
		{
			return true;
		}
		if (TryHandleBlockQuoteEnter(documentLine, text, out lineCountChanged))
		{
			return true;
		}
		if (TryHandleOpeningCodeFenceEnter(documentLine, text, out lineCountChanged))
		{
			return true;
		}
		if (TryHandleCodeBlockShiftEnter(documentLine, out lineCountChanged))
		{
			return true;
		}
		if (TryHandleParagraphEnter(documentLine, text, out lineCountChanged))
		{
			return true;
		}
		return false;
	}

	private bool TryHandleSemanticListSplitEnter(DocumentLine line, string lineText, int caretOffset, out bool lineCountChanged)
	{
		lineCountChanged = false;
		if (ReadOnly || line == null || EditBox?.Document == null || IsEffectivelyEmptySemanticLine(lineText))
		{
			return false;
		}
		int lineIndex = Math.Max(0, line.LineNumber - 1);
		if (!TryGetSemanticBlockAtLine(lineIndex, out var block) || block == null || block.Kind != MarkdownBlockKind.List)
		{
			return false;
		}
		TextDocument document = EditBox.Document;
		int num = Math.Max(line.Offset, Math.Min(caretOffset, line.EndOffset));
		string lineText2 = ((num > line.Offset) ? document.GetText(line.Offset, num - line.Offset) : string.Empty);
		bool splitAtContentStart = IsEffectivelyEmptySemanticLine(lineText2);
		string defaultEnterText = GetDefaultEnterText();
		string insertText = (splitAtContentStart ? ("\u00a0" + defaultEnterText) : defaultEnterText);
		int value = num + (splitAtContentStart ? "\u00a0".Length : defaultEnterText.Length);
		int currentIndent = GetSemanticIndentForLineOrZero(lineIndex);
		MarkdownListKind listKind = block.ListKind.GetValueOrDefault();
		bool wasChecked = block.Checked == true;
		int currentNumber = 1;
		if (listKind == MarkdownListKind.Numbered)
		{
			int.TryParse(string.IsNullOrEmpty(block.CustomOrder) ? "1" : block.CustomOrder, out currentNumber);
			currentNumber = Math.Max(1, currentNumber);
		}
		string persistedText = GetPersistedText();
		ApplyDocumentMutationsAndNotify(new DocumentMutation[1]
		{
			new DocumentMutation
			{
				StartOffset = num,
				RemoveLength = 0,
				InsertText = insertText
			}
		}, value, delegate
		{
			switch (listKind)
			{
			case MarkdownListKind.Task:
				SetSemanticTaskListForLine(lineIndex, enabled: true, !splitAtContentStart && wasChecked, currentIndent);
				SetSemanticTaskListForLine(lineIndex + 1, enabled: true, splitAtContentStart && wasChecked, currentIndent);
				break;
			case MarkdownListKind.Numbered:
				SetSemanticOrderedListForLine(lineIndex, enabled: true, currentNumber.ToString(), currentIndent);
				SetSemanticOrderedListForLine(lineIndex + 1, enabled: true, (currentNumber + 1).ToString(), currentIndent);
				AdjustOrderedListMarkers(line.LineNumber);
				break;
			default:
				SetSemanticBulletListForLine(lineIndex, enabled: true, currentIndent);
				SetSemanticBulletListForLine(lineIndex + 1, enabled: true, currentIndent);
				break;
			}
		});
		ForceImmediateBlockRenderAfterEnter();
		NotifySemanticTextChangedIfNeeded(persistedText);
		lineCountChanged = true;
		return true;
	}

	private bool TryHandleBlockQuoteSplitEnter(DocumentLine line, string lineText, int caretOffset, out bool lineCountChanged)
	{
		lineCountChanged = false;
		if (ReadOnly || line == null || EditBox?.Document == null || IsEffectivelyEmptySemanticLine(lineText))
		{
			return false;
		}
		int lineIndex = Math.Max(0, line.LineNumber - 1);
		if (!TryGetSemanticBlockAtLine(lineIndex, out var block) || block == null || block.Kind != MarkdownBlockKind.BlockQuote)
		{
			return false;
		}
		TextDocument document = EditBox.Document;
		int num = Math.Max(line.Offset, Math.Min(caretOffset, line.EndOffset));
		bool flag = IsEffectivelyEmptySemanticLine((num > line.Offset) ? document.GetText(line.Offset, num - line.Offset) : string.Empty);
		string defaultEnterText = GetDefaultEnterText();
		string insertText = (flag ? ("\u00a0" + defaultEnterText) : defaultEnterText);
		int value = num + (flag ? "\u00a0".Length : defaultEnterText.Length);
		int currentIndent = GetSemanticIndentForLineOrZero(lineIndex);
		string persistedText = GetPersistedText();
		ApplyDocumentMutationsAndNotify(new DocumentMutation[1]
		{
			new DocumentMutation
			{
				StartOffset = num,
				RemoveLength = 0,
				InsertText = insertText
			}
		}, value, delegate
		{
			SetSemanticBlockQuoteForLine(lineIndex, enabled: true, currentIndent);
			SetSemanticBlockQuoteForLine(lineIndex + 1, enabled: true, currentIndent);
		});
		ForceImmediateBlockRenderAfterEnter();
		NotifySemanticTextChangedIfNeeded(persistedText);
		lineCountChanged = true;
		return true;
	}

	private bool TryHandleEmptyHeadingEnter(DocumentLine line, string lineText, out bool lineCountChanged)
	{
		lineCountChanged = false;
		if (ReadOnly || line == null || EditBox?.Document == null || !IsEffectivelyEmptySemanticLine(lineText))
		{
			return false;
		}
		int lineIndex = Math.Max(0, line.LineNumber - 1);
		if (!TryGetSemanticBlockAtLine(lineIndex, out var block) || block == null || block.Kind != MarkdownBlockKind.Heading)
		{
			return false;
		}
		int headingLevel = Math.Max(1, Math.Min(6, block.Level ?? 1));
		string defaultEnterText = GetDefaultEnterText();
		int value = line.EndOffset + defaultEnterText.Length;
		string persistedText = GetPersistedText();
		ApplyDocumentMutationsAndNotify(new DocumentMutation[1]
		{
			new DocumentMutation
			{
				StartOffset = line.EndOffset,
				RemoveLength = 0,
				InsertText = defaultEnterText
			}
		}, value, delegate
		{
			SetSemanticHeadingLevelForLine(lineIndex, headingLevel);
			SetSemanticHeadingLevelForLine(lineIndex + 1, null);
		});
		NotifySemanticTextChangedIfNeeded(persistedText);
		lineCountChanged = true;
		return true;
	}

	private bool TryHandleFoldedHeadingSplitEnter(DocumentLine line, int caretOffset, out bool lineCountChanged)
	{
		lineCountChanged = false;
		if (ReadOnly || line == null || EditBox?.Document == null || _semanticDocument?.Blocks == null)
		{
			return false;
		}
		int lineIndex = Math.Max(0, line.LineNumber - 1);
		if (!TryGetSemanticBlockAtLine(lineIndex, out var block) || block == null || block.Kind != MarkdownBlockKind.Heading || block.Folded != true || !block.Level.HasValue || caretOffset <= line.Offset || caretOffset >= line.EndOffset || !MarkdownHeadingFoldHelper.TryGetFoldRange(_semanticDocument.Blocks, block, EditBox.Document.LineCount, out var range) || !TryGetHeadingFoldOffsets(range, out var _, out var endOffset))
		{
			return false;
		}
		TextDocument document = EditBox.Document;
		int num = Math.Max(line.Offset, Math.Min(caretOffset, line.EndOffset));
		int num2 = Math.Max(0, line.EndOffset - num);
		if (num2 <= 0)
		{
			return false;
		}
		string text = document.GetText(num, num2);
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		int num3 = Math.Max(num, Math.Min(endOffset, document.TextLength));
		string defaultEnterText = GetDefaultEnterText();
		int newHeadingLineIndex = GetLineIndexAfterInsertionAtLineEnd(num3);
		int value = num3 + defaultEnterText.Length - num2;
		string persistedText = GetPersistedText();
		ApplyDocumentMutationsAndNotify(new DocumentMutation[2]
		{
			new DocumentMutation
			{
				StartOffset = num,
				RemoveLength = num2,
				InsertText = string.Empty
			},
			new DocumentMutation
			{
				StartOffset = num3,
				RemoveLength = 0,
				InsertText = defaultEnterText + text
			}
		}, value, delegate
		{
			SetSemanticHeadingLevelForLine(lineIndex, block.Level.Value);
			SetHeadingFoldedStateAtLine(lineIndex, folded: true);
			SetSemanticHeadingLevelForLine(newHeadingLineIndex, block.Level.Value);
			SetHeadingFoldedStateAtLine(newHeadingLineIndex, folded: false);
		});
		NotifySemanticTextChangedIfNeeded(persistedText);
		lineCountChanged = true;
		return true;
	}

	private bool TryHandleFoldedHeadingEnterAtCaret(int caretOffset, out bool lineCountChanged)
	{
		lineCountChanged = false;
		if (ReadOnly || EditBox?.Document == null || !TryGetFoldedHeadingAtCaretBoundary(caretOffset, out var folding, out var block, out var _) || !block.Level.HasValue)
		{
			return false;
		}
		TextDocument document = EditBox.Document;
		int num = Math.Max(0, Math.Min(folding.EndOffset, document.TextLength));
		string defaultEnterText = GetDefaultEnterText();
		int newHeadingLineIndex = GetLineIndexAfterInsertionAtLineEnd(num);
		int value = num + defaultEnterText.Length;
		string persistedText = GetPersistedText();
		ApplyDocumentMutationsAndNotify(new DocumentMutation[1]
		{
			new DocumentMutation
			{
				StartOffset = num,
				RemoveLength = 0,
				InsertText = defaultEnterText
			}
		}, value, delegate
		{
			SetSemanticHeadingLevelForLine(newHeadingLineIndex, block.Level.Value);
			SetHeadingFoldedStateAtLine(newHeadingLineIndex, folded: false);
		});
		NotifySemanticTextChangedIfNeeded(persistedText);
		lineCountChanged = true;
		return true;
	}

	private int GetLineIndexAfterInsertionAtLineEnd(int insertOffset)
	{
		TextDocument textDocument = EditBox?.Document;
		if (textDocument == null)
		{
			return 0;
		}
		int offset = Math.Max(0, Math.Min(insertOffset, textDocument.TextLength));
		DocumentLine lineByOffset = textDocument.GetLineByOffset(offset);
		if (lineByOffset != null)
		{
			return Math.Max(0, lineByOffset.LineNumber);
		}
		return 0;
	}

	private bool TryHandleOpeningCodeFenceEnter(DocumentLine line, string lineText, out bool lineCountChanged)
	{
		lineCountChanged = false;
		int lineIndex = Math.Max(0, line.LineNumber - 1);
		if (TryGetSemanticBlockAtLine(lineIndex, out var block) && block != null && block.Kind == MarkdownBlockKind.CodeBlock)
		{
			return false;
		}
		if (IsLiteralEditingCodeFence(lineIndex, lineText) || !TryGetEditingCodeFenceInfo(lineText, out var _, out var _))
		{
			return false;
		}
		int num = lineText.Length - lineText.TrimStart().Length;
		string text = ((num > 0) ? lineText.Substring(0, num) : string.Empty);
		string text2 = (lineText ?? string.Empty) + GetDefaultEnterText() + GetDefaultEnterText() + text + "```";
		PreparedEditorTextState prepared = PrepareEditorTextState(text2);
		string text3 = prepared.DisplayText ?? string.Empty;
		int num2 = (lineText?.Length ?? 0) + GetDefaultEnterText().Length;
		int value = line.Offset + ((prepared.RawToDisplayOffsetMap != null && prepared.RawToDisplayOffsetMap.Length > num2) ? prepared.RawToDisplayOffsetMap[num2] : text3.Length);
		string persistedText = GetPersistedText();
		ReplaceEditorText(line.Offset, line.Length, text3, value, delegate(int insertedOffset, int insertedLength)
		{
			ApplyPreparedSemanticBlocks(prepared, insertedOffset, insertedLength);
		});
		ForceImmediateBlockRenderAfterEnter();
		ScheduleFocusCodeBlockAttachment(lineIndex, moveToEnd: false);
		NotifySemanticTextChangedIfNeeded(persistedText);
		lineCountChanged = true;
		return true;
	}

	private bool TryHandleCodeBlockShiftEnter(DocumentLine line, out bool lineCountChanged)
	{
		lineCountChanged = false;
		if ((Keyboard.Modifiers & ModifierKeys.Shift) == 0)
		{
			return false;
		}
		int num = Math.Max(0, line.LineNumber - 1);
		if (!TryGetSemanticBlockAtLine(num, out var block) || block == null || block.Kind != MarkdownBlockKind.CodeBlock || num <= block.StartLine || num >= block.EndLine)
		{
			return false;
		}
		DocumentLine documentLine = FindClosingCodeFenceLine(block, line.LineNumber);
		if (documentLine == null)
		{
			return false;
		}
		string defaultEnterText = GetDefaultEnterText();
		int value = documentLine.EndOffset + defaultEnterText.Length;
		string persistedText = GetPersistedText();
		ApplyDocumentMutationsAndNotify(new DocumentMutation[1]
		{
			new DocumentMutation
			{
				StartOffset = documentLine.EndOffset,
				RemoveLength = 0,
				InsertText = defaultEnterText
			}
		}, value);
		NotifySemanticTextChangedIfNeeded(persistedText);
		lineCountChanged = true;
		return true;
	}

	private DocumentLine FindClosingCodeFenceLine(MarkdownBlockNode codeBlock, int currentLineNumber)
	{
		TextDocument textDocument = EditBox?.Document;
		if (textDocument == null || codeBlock == null || codeBlock.Kind != MarkdownBlockKind.CodeBlock)
		{
			return null;
		}
		int val = Math.Max(currentLineNumber + 1, codeBlock.StartLine + 2);
		val = Math.Max(1, Math.Min(val, textDocument.LineCount));
		for (int i = val; i <= textDocument.LineCount; i++)
		{
			DocumentLine lineByNumber = textDocument.GetLineByNumber(i);
			if (lineByNumber != null && TryGetEditingCodeFenceClosingInfo(textDocument.GetText(lineByNumber.Offset, lineByNumber.Length), out var _))
			{
				return lineByNumber;
			}
		}
		return null;
	}

	internal void ForceImmediateBlockRenderAfterEnter()
	{
		ForceImmediateBlockRender();
	}

	private void ForceImmediateBlockRender()
	{
		HandleOnTextChanged();
		RequestImmediateRedrawAroundCaret();
	}

	private bool TryHandleTaskListEnter(DocumentLine line, string lineText, out bool lineCountChanged)
	{
		lineCountChanged = false;
		int lineIndex = Math.Max(0, line.LineNumber - 1);
		if (!TryGetSemanticTaskListForLine(lineIndex, out var isChecked))
		{
			return false;
		}
		if (IsEffectivelyEmptySemanticLine(lineText))
		{
			string persistedText = GetPersistedText();
			if (TryGetSemanticBlockAtLine(lineIndex, out var block) && TryRetreatEmptySemanticBlockLine(lineIndex, block))
			{
				NotifySemanticTextChangedIfNeeded(persistedText);
				return true;
			}
			ApplyDocumentMutationsAndNotify(new DocumentMutation[1]
			{
				new DocumentMutation
				{
					StartOffset = line.Offset,
					RemoveLength = line.Length,
					InsertText = string.Empty
				}
			}, line.Offset, delegate
			{
				SetSemanticTaskListForLine(lineIndex, enabled: false);
			});
			NotifySemanticTextChangedIfNeeded(persistedText);
			return true;
		}
		int currentIndent = GetSemanticIndentForLineOrZero(lineIndex);
		string text = Environment.NewLine + "\u00a0";
		int value = line.EndOffset + text.Length;
		string persistedText2 = GetPersistedText();
		ApplyDocumentMutationsAndNotify(new DocumentMutation[1]
		{
			new DocumentMutation
			{
				StartOffset = line.EndOffset,
				RemoveLength = 0,
				InsertText = text
			}
		}, value, delegate
		{
			SetSemanticTaskListForLine(lineIndex, enabled: true, isChecked, currentIndent);
			SetSemanticTaskListForLine(lineIndex + 1, enabled: true, isChecked: false, currentIndent);
		});
		ForceImmediateBlockRenderAfterEnter();
		NotifySemanticTextChangedIfNeeded(persistedText2);
		lineCountChanged = true;
		return true;
	}

	private bool TryHandleBulletListEnter(DocumentLine line, string lineText, out bool lineCountChanged)
	{
		lineCountChanged = false;
		int lineIndex = Math.Max(0, line.LineNumber - 1);
		if (!TryGetSemanticBulletListForLine(lineIndex))
		{
			return false;
		}
		if (IsEffectivelyEmptySemanticLine(lineText))
		{
			string persistedText = GetPersistedText();
			if (TryGetSemanticBlockAtLine(lineIndex, out var block) && TryRetreatEmptySemanticBlockLine(lineIndex, block))
			{
				NotifySemanticTextChangedIfNeeded(persistedText);
				return true;
			}
			ApplyDocumentMutationsAndNotify(new DocumentMutation[1]
			{
				new DocumentMutation
				{
					StartOffset = line.Offset,
					RemoveLength = line.Length,
					InsertText = string.Empty
				}
			}, line.Offset, delegate
			{
				SetSemanticBulletListForLine(lineIndex, enabled: false);
			});
			NotifySemanticTextChangedIfNeeded(persistedText);
			return true;
		}
		int currentIndent = GetSemanticIndentForLineOrZero(lineIndex);
		string text = Environment.NewLine + "\u00a0";
		int value = line.EndOffset + text.Length;
		string persistedText2 = GetPersistedText();
		ApplyDocumentMutationsAndNotify(new DocumentMutation[1]
		{
			new DocumentMutation
			{
				StartOffset = line.EndOffset,
				RemoveLength = 0,
				InsertText = text
			}
		}, value, delegate
		{
			SetSemanticBulletListForLine(lineIndex, enabled: true, currentIndent);
			SetSemanticBulletListForLine(lineIndex + 1, enabled: true, currentIndent);
		});
		ForceImmediateBlockRenderAfterEnter();
		NotifySemanticTextChangedIfNeeded(persistedText2);
		lineCountChanged = true;
		return true;
	}

	private bool TryHandleOrderedListEnter(DocumentLine line, string lineText, out bool lineCountChanged)
	{
		lineCountChanged = false;
		int lineIndex = Math.Max(0, line.LineNumber - 1);
		if (!TryGetSemanticOrderedListForLine(lineIndex, out var numberText))
		{
			return false;
		}
		if (IsEffectivelyEmptySemanticLine(lineText))
		{
			string persistedText = GetPersistedText();
			if (TryGetSemanticBlockAtLine(lineIndex, out var block) && TryRetreatEmptySemanticBlockLine(lineIndex, block))
			{
				NotifySemanticTextChangedIfNeeded(persistedText);
				return true;
			}
			ApplyDocumentMutationsAndNotify(new DocumentMutation[1]
			{
				new DocumentMutation
				{
					StartOffset = line.Offset,
					RemoveLength = line.Length,
					InsertText = string.Empty
				}
			}, line.Offset, delegate
			{
				SetSemanticOrderedListForLine(lineIndex, enabled: false);
				AdjustOrderedListMarkers(line.LineNumber);
			});
			NotifySemanticTextChangedIfNeeded(persistedText);
			return true;
		}
		int currentNumber = 1;
		int.TryParse(numberText, out currentNumber);
		currentNumber = Math.Max(1, currentNumber);
		int currentIndent = GetSemanticIndentForLineOrZero(lineIndex);
		string text = Environment.NewLine + "\u00a0";
		int value = line.EndOffset + text.Length;
		string persistedText2 = GetPersistedText();
		ApplyDocumentMutationsAndNotify(new DocumentMutation[1]
		{
			new DocumentMutation
			{
				StartOffset = line.EndOffset,
				RemoveLength = 0,
				InsertText = text
			}
		}, value, delegate
		{
			SetSemanticOrderedListForLine(lineIndex, enabled: true, currentNumber.ToString(), currentIndent);
			SetSemanticOrderedListForLine(lineIndex + 1, enabled: true, (currentNumber + 1).ToString(), currentIndent);
			AdjustOrderedListMarkers(line.LineNumber);
		});
		ForceImmediateBlockRenderAfterEnter();
		NotifySemanticTextChangedIfNeeded(persistedText2);
		lineCountChanged = true;
		return true;
	}

	private bool TryHandleBlockQuoteEnter(DocumentLine line, string lineText, out bool lineCountChanged)
	{
		lineCountChanged = false;
		int lineIndex = Math.Max(0, line.LineNumber - 1);
		if (!TryGetSemanticBlockQuoteForLine(lineIndex))
		{
			return false;
		}
		if (IsEffectivelyEmptySemanticLine(lineText))
		{
			string persistedText = GetPersistedText();
			if (TryGetSemanticBlockAtLine(lineIndex, out var block) && TryRetreatEmptySemanticBlockLine(lineIndex, block))
			{
				NotifySemanticTextChangedIfNeeded(persistedText);
				return true;
			}
			ApplyDocumentMutationsAndNotify(new DocumentMutation[1]
			{
				new DocumentMutation
				{
					StartOffset = line.Offset,
					RemoveLength = line.Length,
					InsertText = string.Empty
				}
			}, line.Offset, delegate
			{
				SetSemanticBlockQuoteForLine(lineIndex, enabled: false);
			});
			NotifySemanticTextChangedIfNeeded(persistedText);
			return true;
		}
		int currentIndent = GetSemanticIndentForLineOrZero(lineIndex);
		string text = Environment.NewLine + "\u00a0";
		int value = line.EndOffset + text.Length;
		string persistedText2 = GetPersistedText();
		ApplyDocumentMutationsAndNotify(new DocumentMutation[1]
		{
			new DocumentMutation
			{
				StartOffset = line.EndOffset,
				RemoveLength = 0,
				InsertText = text
			}
		}, value, delegate
		{
			SetSemanticBlockQuoteForLine(lineIndex, enabled: true, currentIndent);
			SetSemanticBlockQuoteForLine(lineIndex + 1, enabled: true, currentIndent);
		});
		ForceImmediateBlockRenderAfterEnter();
		NotifySemanticTextChangedIfNeeded(persistedText2);
		lineCountChanged = true;
		return true;
	}

	private bool TryHandleParagraphEnter(DocumentLine line, string lineText, out bool lineCountChanged)
	{
		lineCountChanged = false;
		int lineIndex = Math.Max(0, line.LineNumber - 1);
		if (!TryGetSemanticBlockAtLine(lineIndex, out var block) || block == null || block.Kind != MarkdownBlockKind.Paragraph)
		{
			return false;
		}
		if (IsEffectivelyEmptySemanticLine(lineText))
		{
			string persistedText = GetPersistedText();
			if (block.Indent > 0 && TryGetRetreatedSemanticIndent(block, out var nextIndent))
			{
				ApplyDocumentMutationsAndNotify(new DocumentMutation[1]
				{
					new DocumentMutation
					{
						StartOffset = line.Offset,
						RemoveLength = line.Length,
						InsertText = string.Empty
					}
				}, line.Offset, delegate
				{
					SetSemanticParagraphForLine(lineIndex, nextIndent);
				});
				NotifySemanticTextChangedIfNeeded(persistedText);
				return true;
			}
			string newLine = Environment.NewLine;
			int value = line.EndOffset + newLine.Length;
			ApplyDocumentMutationsAndNotify(new DocumentMutation[1]
			{
				new DocumentMutation
				{
					StartOffset = line.EndOffset,
					RemoveLength = 0,
					InsertText = newLine
				}
			}, value, delegate
			{
				SetSemanticParagraphForLine(lineIndex, 0);
				SetSemanticParagraphForLine(lineIndex + 1, 0);
			});
			NotifySemanticTextChangedIfNeeded(persistedText);
			lineCountChanged = true;
			return true;
		}
		int currentIndent = GetSemanticIndentForLineOrZero(lineIndex);
		int num = Math.Max(line.Offset, Math.Min(EditBox.CaretOffset, line.EndOffset));
		string newLine2 = Environment.NewLine;
		int value2 = num + newLine2.Length;
		string persistedText2 = GetPersistedText();
		ApplyDocumentMutationsAndNotify(new DocumentMutation[1]
		{
			new DocumentMutation
			{
				StartOffset = num,
				RemoveLength = 0,
				InsertText = newLine2
			}
		}, value2, delegate
		{
			SetSemanticParagraphForLine(lineIndex, currentIndent);
			SetSemanticParagraphForLine(lineIndex + 1, 0);
		});
		NotifySemanticTextChangedIfNeeded(persistedText2);
		lineCountChanged = true;
		return true;
	}

	internal static string ConvertNormalizedBlockMarkerToDisplay(string marker)
	{
		if (string.IsNullOrEmpty(marker))
		{
			return string.Empty;
		}
		Match match = Regex.Match(marker, "^(#{1,6}) ");
		if (match.Success)
		{
			return BuildDisplayHeadingMarker(match.Groups[1].Value.Length);
		}
		if (string.Equals(marker, "> ", StringComparison.Ordinal))
		{
			return string.Empty;
		}
		if (string.Equals(marker, "---", StringComparison.Ordinal))
		{
			return "\u200c\u200d\u2060";
		}
		if (string.Equals(marker, "```", StringComparison.Ordinal))
		{
			return "```";
		}
		if (Regex.IsMatch(marker, "^\\d+\\. $"))
		{
			return BuildDisplayOrderedListMarker(marker.Substring(0, marker.Length - 2));
		}
		if (string.Equals(marker, "- [ ] ", StringComparison.Ordinal))
		{
			return "\u200b\u200c\u200d \u200e\u200f";
		}
		if (string.Equals(marker, "- [x] ", StringComparison.Ordinal) || string.Equals(marker, "- [X] ", StringComparison.Ordinal))
		{
			return "\u200b\u200c\u200d\u2060\u200e\u200f";
		}
		if (string.Equals(marker, "- ", StringComparison.Ordinal))
		{
			return "\u2063\u2064";
		}
		if (string.Equals(marker, "* ", StringComparison.Ordinal))
		{
			return "\u2064\u2063";
		}
		if (string.Equals(marker, "+ ", StringComparison.Ordinal))
		{
			return "\u2061\u2064";
		}
		return NormalizePersistenceTextForEditorDisplay(marker);
	}

	private static string GetLeadingWhitespace(string text)
	{
		string text2 = text ?? string.Empty;
		int num = text2.Length - text2.TrimStart().Length;
		if (num <= 0)
		{
			return string.Empty;
		}
		return text2.Substring(0, num);
	}

	private bool TryRemoveLeadingEmptyLineOnDelete()
	{
		if (EditBox?.Document == null || EditBox.SelectionLength > 0)
		{
			return false;
		}
		int num = Math.Max(0, Math.Min(EditBox.CaretOffset, EditBox.Document.TextLength));
		TextDocument document = EditBox.Document;
		DocumentLine lineByOffset = document.GetLineByOffset(num);
		if (lineByOffset == null || lineByOffset.LineNumber != 1 || lineByOffset.NextLine == null || num > lineByOffset.EndOffset || !IsEffectivelyEmptySemanticLine(document.GetText(lineByOffset)))
		{
			return false;
		}
		string persistedText = GetPersistedText();
		RemoveSemanticLineAndShiftFollowing(0);
		ApplyDocumentMutationsAndNotify(new DocumentMutation[1]
		{
			new DocumentMutation
			{
				StartOffset = lineByOffset.Offset,
				RemoveLength = lineByOffset.Length + lineByOffset.DelimiterLength,
				InsertText = string.Empty
			}
		}, 0, delegate
		{
			_seedSemanticDocumentForRefresh = CloneSemanticDocument(_semanticDocument);
			AdjustOrderedListMarkers(1);
		}, preserveSemanticBlocks: false);
		EditBox.SelectionStart = 0;
		NotifySemanticTextChangedIfNeeded(persistedText);
		return true;
	}

	private static bool TryBuildRetreatedListLine(string lineText, string marker, out string replacementText)
	{
		replacementText = null;
		if (string.IsNullOrEmpty(lineText) || string.IsNullOrEmpty(marker))
		{
			return false;
		}
		string leadingWhitespace = GetLeadingWhitespace(lineText);
		if (!string.Equals(lineText.TrimStart(), marker, StringComparison.Ordinal))
		{
			return false;
		}
		replacementText = (string.IsNullOrEmpty(leadingWhitespace) ? string.Empty : (RemoveOneIndentLevel(leadingWhitespace) + marker));
		return true;
	}

	private static bool TryBuildRetreatedBlockQuoteLine(string lineText, string marker, out string replacementText)
	{
		replacementText = null;
		if (string.IsNullOrEmpty(lineText) || string.IsNullOrEmpty(marker))
		{
			return false;
		}
		string leadingWhitespace = GetLeadingWhitespace(lineText);
		if (!string.Equals(lineText.TrimStart(), marker, StringComparison.Ordinal))
		{
			return false;
		}
		replacementText = (string.IsNullOrEmpty(leadingWhitespace) ? string.Empty : leadingWhitespace);
		return true;
	}

	private static string RemoveOneIndentLevel(string indent)
	{
		if (string.IsNullOrEmpty(indent))
		{
			return string.Empty;
		}
		if (indent.StartsWith("\t", StringComparison.Ordinal))
		{
			return indent.Substring(1);
		}
		if (indent.Length >= 4)
		{
			return indent.Substring(4);
		}
		return string.Empty;
	}

	private void OnEditBoxPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
	{
		if (propertyChangedEventArgs.PropertyName == "LineCount")
		{
			int lineCount = EditBox.LineCount;
			if (_suppressNextListAdjust)
			{
				_suppressNextListAdjust = false;
				_previousLineCount = lineCount;
			}
			else
			{
				_previousLineCount = lineCount;
			}
		}
	}

	private Point GetStartPosition(VisualLine line = null, int width = 245, int height = 140)
	{
		TextView textView = EditBox.TextArea.TextView;
		TextViewPosition position = new TextViewPosition(EditBox.Document.GetLocation(EditBox.CaretOffset));
		if (line != null)
		{
			position = new TextViewPosition(EditBox.Document.GetLocation(line.StartOffset));
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

	public void ExitEditor()
	{
	}

	private void EditorMenuOnContextMenuOpening(object sender, ContextMenuEventArgs context)
	{
		AddEditorContextMenu(context);
		this.QuickPopupOpened?.Invoke(this, null);
	}

	public void AddEditorContextMenu(ContextMenuEventArgs context)
	{
		FocusEditBox();
		System.Windows.Controls.ContextMenu contextMenu = new System.Windows.Controls.ContextMenu
		{
			Width = 230.0
		};
		contextMenu.SetResourceReference(FrameworkElement.StyleProperty, "ContextMenuStyle");
		contextMenu.SetResourceReference(System.Windows.Controls.Control.BackgroundProperty, "PopupBackground");
		contextMenu.Closed += delegate
		{
			this.QuickPopupClosed?.Invoke(this, null);
		};
		contextMenu.StaysOpen = true;
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
			CommandTarget = EditBox.TextArea
		});
		contextMenu.Items.Add(new EditMenuItem
		{
			Title = Utils.GetString("Copy"),
			Command = ApplicationCommands.Copy,
			CommandTarget = EditBox.TextArea,
			Shortcut = "Ctrl+C"
		});
		EditMenuItem editMenuItem = new EditMenuItem
		{
			Title = Utils.GetString("Paste"),
			Command = ApplicationCommands.Paste,
			CommandTarget = EditBox.TextArea,
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
			CommandTarget = EditBox,
			Shortcut = "Ctrl+Z"
		});
		contextMenu.Items.Add(new EditMenuItem
		{
			Title = Utils.GetString("Redo"),
			Command = ApplicationCommands.Redo,
			CommandTarget = EditBox,
			Shortcut = "Ctrl+Shift+Z"
		});
		FrameworkElement obj = (FrameworkElement)context.Source;
		obj.ContextMenu = contextMenu;
		obj.ContextMenu.IsOpen = true;
	}

	public void EnterImmersiveMode()
	{
		this.EnterImmersive?.Invoke(this, null);
	}

	public void NavigateTask(string projectId, string taskId)
	{
		this.Navigate?.Invoke(this, new ProjectTask
		{
			ProjectId = projectId,
			TaskId = taskId
		});
	}

	public void SetText(string text)
	{
		PreparedEditorTextState prepared = PrepareEditorTextState(text);
		ApplyPreparedEditorState(prepared, text ?? string.Empty, restoreOffset: false, resetHistory: true);
		ClearPendingLocalDocumentChanges();
	}

	public void SetTextAndOffset(string text, bool restoreOffset)
	{
		PreparedEditorTextState prepared = PrepareEditorTextState(text);
		ApplyPreparedEditorState(prepared, text ?? string.Empty, restoreOffset, resetHistory: true);
		ClearPendingLocalDocumentChanges();
	}

	public void SetExternalTextAndOffset(string text, bool restoreOffset)
	{
		SetExternalTextAndOffsetCore(text, restoreOffset, null);
	}

	public void SetExternalTextAndCaretOffset(string text, int caretOffset)
	{
		SetExternalTextAndOffsetCore(text, restoreOffset: true, caretOffset);
	}

	private void SetExternalTextAndOffsetCore(string text, bool restoreOffset, int? caretOffset)
	{
		string text2 = text ?? string.Empty;
		PreparedEditorTextState prepared = PrepareEditorTextState(text2);
		if (EditBox?.Document == null)
		{
			ApplyPreparedEditorState(prepared, text2, restoreOffset, resetHistory: true);
			ClearPendingLocalDocumentChanges();
			return;
		}
		bool suppressExternalTextChanged = _suppressExternalTextChanged;
		EditorScrollSnapshot scrollSnapshot = CaptureEditorScrollSnapshot();
		DocumentMutation documentMutation = BuildExternalTextReplacement(EditBox.Text, prepared.DisplayText);
		int num = (caretOffset.HasValue ? Math.Max(0, Math.Min(caretOffset.Value, prepared.DisplayText.Length)) : AdjustOffsetForExternalTextReplacement(EditBox.CaretOffset, documentMutation));
		int? explicitCaretOffset = (restoreOffset ? new int?(num) : ((int?)null));
		_suppressExternalTextChanged = true;
		try
		{
			Action action = delegate
			{
				ResetTransientSemanticEditingState();
				_typedSemanticMarks.AddRange(prepared.SemanticMarks);
				_seedSemanticDocumentForRefresh = prepared.SemanticDocument;
			};
			if (documentMutation == null)
			{
				action();
				RefreshEditingStateWithoutMarkdownParsing(EditBox.Text);
				UpdateBindingTextFromEditor(text2);
			}
			else
			{
				ApplyDocumentMutationsAndNotify(new DocumentMutation[1] { documentMutation }, explicitCaretOffset, action, preserveSemanticBlocks: false, refreshEditingState: true, notifyTextChanged: false);
				UpdateBindingTextFromEditor(text2);
			}
			RebuildPreparedLinkAndAttachmentState(prepared);
			_imageElementGenerator?.DiagnosticTaskContentLoaded();
			_imageElementGenerator?.QueuePersistedAttachmentCheck(text2);
			RequestSafeRedraw();
			RestoreEditorScrollSnapshot(scrollSnapshot);
			base.Dispatcher?.BeginInvoke((Action)delegate
			{
				RestoreEditorScrollSnapshot(scrollSnapshot);
			}, DispatcherPriority.ContextIdle);
			if (restoreOffset)
			{
				EditBox.CaretOffset = Math.Max(0, Math.Min(num, EditBox.Text.Length));
			}
			ResetSemanticHistoryToCurrentState();
		}
		finally
		{
			_suppressExternalTextChanged = suppressExternalTextChanged;
		}
		ClearPendingLocalDocumentChanges();
	}

	public string HandleTabs(bool usePersistedText = false)
	{
		try
		{
			StringBuilder stringBuilder = new StringBuilder();
			int frontTabNum = -1;
			int startTabNum = 0;
			if (usePersistedText)
			{
				int num = 0;
				foreach (string item in EnumerateTextLines(GetPersistedText()))
				{
					TryGetSemanticBlockAtLine(num, out var block);
					AppendHandledTabLine(stringBuilder, item, block, ref frontTabNum, ref startTabNum);
					num++;
				}
			}
			else
			{
				foreach (DocumentLine line in EditBox.Document.Lines)
				{
					string lineText = Text.Substring(line.Offset, line.Length);
					TryGetSemanticBlockAtOffset(line.Offset, out var block2);
					AppendHandledTabLine(stringBuilder, lineText, block2, ref frontTabNum, ref startTabNum);
				}
			}
			return stringBuilder.ToString();
		}
		catch (Exception)
		{
			return usePersistedText ? GetPersistedText() : Text;
		}
	}

	private static IEnumerable<string> EnumerateTextLines(string text)
	{
		text = text ?? string.Empty;
		if (text.Length == 0)
		{
			yield return string.Empty;
			yield break;
		}
		int cursor = 0;
		while (cursor < text.Length)
		{
			int num = cursor;
			for (; cursor < text.Length && text[cursor] != '\r' && text[cursor] != '\n'; cursor++)
			{
			}
			yield return text.Substring(num, cursor - num);
			if (cursor >= text.Length)
			{
				break;
			}
			cursor = ((text[cursor] != '\r' || cursor + 1 >= text.Length || text[cursor + 1] != '\n') ? (cursor + 1) : (cursor + 2));
			if (cursor == text.Length)
			{
				yield return string.Empty;
			}
		}
	}

	private static void AppendHandledTabLine(StringBuilder text, string lineText, MarkdownBlockNode block, ref int frontTabNum, ref int startTabNum)
	{
		if (block == null || block.Kind != MarkdownBlockKind.List)
		{
			text.Append(lineText);
			text.Append("\r\n");
			return;
		}
		string text2 = lineText.TrimStart();
		int num = lineText.Length - text2.Length;
		if (frontTabNum < 0 || num < startTabNum * 4)
		{
			startTabNum = num / 4;
			frontTabNum = 0;
			text.Append(text2);
			text.Append("\r\n");
		}
		else if (num >= startTabNum * 4)
		{
			frontTabNum = Math.Min((num - startTabNum * 4) / 4, frontTabNum + 1);
			for (int i = 0; i < frontTabNum; i++)
			{
				text.Append("    ");
			}
			text.Append(text2);
			text.Append("\r\n");
		}
		else
		{
			frontTabNum = -1;
			startTabNum = 0;
			text.Append(text2);
			text.Append("\r\n");
		}
	}

	public int GetPageLastIndex(double height)
	{
		TextView textView = EditBox.TextArea.TextView;
		DocumentLine documentLine = null;
		foreach (DocumentLine line in EditBox.Document.Lines)
		{
			TextViewPosition position = new TextViewPosition(EditBox.Document.GetLocation(line.EndOffset));
			if (textView.GetVisualPosition(position, VisualYPosition.LineBottom).Y > height)
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
		return Text.Length;
	}

	private int GetPageLastIndex(double height, int start, int end, int interval)
	{
		try
		{
			TextView textView = EditBox.TextArea.TextView;
			if (start == end)
			{
				return end;
			}
			for (int i = start; i < end; i += interval)
			{
				TextViewPosition position = new TextViewPosition(EditBox.Document.GetLocation(i));
				Point visualPosition = textView.GetVisualPosition(position, VisualYPosition.LineBottom);
				TextViewPosition position2 = new TextViewPosition(EditBox.Document.GetLocation(i + interval));
				Point visualPosition2 = textView.GetVisualPosition(position2, VisualYPosition.LineBottom);
				if (visualPosition.Y < height && visualPosition2.Y > height)
				{
					return (interval != 1) ? GetPageLastIndex(height, i, Math.Min(end, i + interval), interval / 10) : i;
				}
				if (visualPosition.Y > height)
				{
					return i;
				}
			}
			return end;
		}
		catch (Exception)
		{
			return end;
		}
	}

	public List<MarkdownInlineDisplaySegment> GetSemanticMarkList()
	{
		int textLength = EditBox?.Document?.TextLength ?? Text?.Length ?? 0;
		return (from mark in SemanticMarkList.Concat(_typedSemanticMarks)
			where IsValidSemanticDisplayMark(mark, textLength) && mark.Kind != MarkdownMarkKind.LiteralInlineSyntax
			orderby mark.SourcePosition, mark.SourceLength
			select mark).ToList();
	}

	private static bool IsValidSemanticDisplayMark(MarkdownInlineDisplaySegment mark, int textLength)
	{
		if (mark == null || mark.SourcePosition < 0 || mark.SourceLength <= 0 || mark.SourcePosition + mark.SourceLength > Math.Max(0, textLength) || mark.OpeningDelimiterLength < 0 || mark.ClosingDelimiterLength < 0 || mark.OpeningDelimiterLength + mark.ClosingDelimiterLength > mark.SourceLength)
		{
			return false;
		}
		return true;
	}

	public Dictionary<int, LinkInfo> GetLinkNameDict()
	{
		return LinkNameDict;
	}

	public Dictionary<int, LinkInfo> GetLinkUrlDict()
	{
		return LinkUrlDict;
	}

	public TextEditor GetEditBox()
	{
		return EditBox;
	}

	public Brush GetBracketColor()
	{
		return _bracketColor;
	}

	public Brush GetHighLightColor()
	{
		return _highLightColor;
	}

	public Dictionary<int, AttachmentInfo> GetImageDict()
	{
		return AttachmentDict;
	}

	public void CorrectSpellingError(string correct, TextSegment errorSegment)
	{
		ReplaceEditorText(errorSegment.StartOffset, errorSegment.Length, correct, errorSegment.StartOffset + (correct?.Length ?? 0));
	}

	public void AddErrorToDict(string error, TextSegment errorSegment)
	{
		SpellCheckProvider?.Add(error);
		EditBox.CaretOffset = errorSegment.StartOffset;
		SpellCheckProvider?.ForceRefresh();
	}

	public void SetImageGeneratorTaskId(string id)
	{
		_editingTaskId = id;
		this.TaskIdChanged?.Invoke(this, id);
	}

	public void TryDeleteAttachment(int offset)
	{
		if (AttachmentDict.ContainsKey(offset))
		{
			AttachmentInfo attachmentInfo = AttachmentDict[offset];
			ReplaceEditorText(offset, attachmentInfo.Value.Length, string.Empty, offset);
			FocusEditBox();
			Keyboard.Focus(EditBox);
		}
	}

	public string RemoveAttachment()
	{
		IOrderedEnumerable<int> orderedEnumerable = AttachmentDict.Keys.OrderByDescending((int k) => k);
		string text = GetPersistedText();
		foreach (int item in orderedEnumerable)
		{
			if (!AttachmentDict.ContainsKey(item))
			{
				continue;
			}
			AttachmentInfo attachmentInfo = AttachmentDict[item];
			AttachmentModel attachmentInUrl = AttachmentDao.GetAttachmentInUrl(attachmentInfo.Url);
			int persistedOffset = GetPersistedOffset(attachmentInfo.Offset);
			int persistedOffset2 = GetPersistedOffset(attachmentInfo.Offset + attachmentInfo.Value.Length);
			int num = Math.Max(0, persistedOffset2 - persistedOffset);
			if (attachmentInUrl == null || num <= 0 || text.Length < persistedOffset2)
			{
				continue;
			}
			int num2 = persistedOffset;
			int num3 = num;
			if (num2 != 0)
			{
				if (text[num2 - 1] == '\r')
				{
					num2--;
					num3++;
				}
				else if (num2 > 1 && text[num2 - 2] == '\r' && text[num2 - 1] == '\n')
				{
					num2 -= 2;
					num3 += 2;
				}
			}
			text = text.Remove(num2, num3);
		}
		return text;
	}

	public int GetAttachmentDefaultInsertIndex()
	{
		return Text.Length;
	}

	private bool CaretNearAttachment()
	{
		int selectionStart = EditBox.SelectionStart;
		int num = EditBox.SelectionStart + EditBox.SelectionLength;
		foreach (int item in AttachmentDict.Keys.ToList())
		{
			if (AttachmentDict.ContainsKey(item))
			{
				AttachmentInfo attachmentInfo = AttachmentDict[item];
				int num2 = item + attachmentInfo.Value.Length;
				if ((item <= num && item >= selectionStart) || (num2 <= num && num2 >= selectionStart))
				{
					return true;
				}
			}
		}
		return false;
	}

	public void Redraw()
	{
		RequestSafeRedraw();
	}

	public void RedrawImmediately()
	{
		RequestImmediateRedraw();
	}

	public void RefreshVisualLayersAfterLayout()
	{
		QueueVisualLayerRefresh(DispatcherPriority.Loaded);
		QueueVisualLayerRefresh(DispatcherPriority.Render);
		QueueVisualLayerRefresh(DispatcherPriority.ContextIdle);
	}

	private void QueueVisualLayerRefresh(DispatcherPriority priority)
	{
		if (base.Dispatcher != null && !base.Dispatcher.HasShutdownStarted && !base.Dispatcher.HasShutdownFinished)
		{
			base.Dispatcher.BeginInvoke(new Action(RefreshVisualLayersNow), priority);
		}
	}

	private void RefreshVisualLayersNow()
	{
		try
		{
			TextView textView = EditBox?.TextArea?.TextView;
			if (textView != null)
			{
				ApplyEditorForegroundResource();
				EditBox.UpdateLayout();
				EditBox.TextArea.UpdateLayout();
				textView.UpdateLayout();
				textView.Redraw();
				RefreshCodeBlockLanguageOverlayTheme();
				CodeBlockLanguageOverlay?.InvalidateVisual();
				RefreshPersistentCodeBlockHeaders();
				_codeBlockSelectionLayer?.InvalidateVisual();
				InvalidMargin();
			}
		}
		catch (ArgumentException)
		{
			RequestSafeRedraw();
		}
		catch (InvalidOperationException)
		{
			RequestSafeRedraw();
		}
	}

	private void RequestSafeRedraw()
	{
		_redrawNotifyHandler?.TryDoAction();
	}

	private void RequestImmediateRedraw()
	{
		if (base.Dispatcher != null && !base.Dispatcher.CheckAccess())
		{
			base.Dispatcher.BeginInvoke(new Action(RequestImmediateRedraw), DispatcherPriority.Render);
			return;
		}
		try
		{
			EditBox?.TextArea?.TextView?.Redraw();
		}
		catch (ArgumentException)
		{
			RequestSafeRedraw();
		}
		catch (InvalidOperationException)
		{
			RequestSafeRedraw();
		}
	}

	private void RequestImmediateRedraw(int offset, int length)
	{
		if (base.Dispatcher != null && !base.Dispatcher.CheckAccess())
		{
			base.Dispatcher.BeginInvoke((Action)delegate
			{
				RequestImmediateRedraw(offset, length);
			}, DispatcherPriority.Render);
			return;
		}
		TextDocument textDocument = EditBox?.Document;
		TextView textView = EditBox?.TextArea?.TextView;
		if (textDocument == null || textView == null)
		{
			return;
		}
		int num = Math.Max(0, Math.Min(offset, textDocument.TextLength));
		int length2 = Math.Max(0, Math.Min(length, textDocument.TextLength - num));
		try
		{
			FlushPendingOrderedListRedraws();
			textView.Redraw(num, length2);
		}
		catch (ArgumentException)
		{
			RequestSafeRedraw();
		}
		catch (InvalidOperationException)
		{
			RequestSafeRedraw();
		}
	}

	private void SchedulePendingOrderedListRedraw()
	{
		if (PendingOrderedListRedrawLineIndices.Count != 0 && !_pendingOrderedListRedrawScheduled)
		{
			Dispatcher dispatcher = base.Dispatcher;
			if (dispatcher != null)
			{
				_pendingOrderedListRedrawScheduled = true;
				dispatcher.BeginInvoke(new Action(FlushPendingOrderedListRedraws), DispatcherPriority.Render);
			}
		}
	}

	private void FlushPendingOrderedListRedraws()
	{
		_pendingOrderedListRedrawScheduled = false;
		TextDocument textDocument = EditBox?.Document;
		TextView textView = EditBox?.TextArea?.TextView;
		if (textView == null || textDocument == null || PendingOrderedListRedrawLineIndices.Count == 0)
		{
			return;
		}
		List<int> list = PendingOrderedListRedrawLineIndices.OrderBy((int lineIndex) => lineIndex).ToList();
		PendingOrderedListRedrawLineIndices.Clear();
		try
		{
			foreach (int item in list)
			{
				int num = item + 1;
				if (num >= 1 && num <= textDocument.LineCount)
				{
					DocumentLine lineByNumber = textDocument.GetLineByNumber(num);
					if (lineByNumber != null)
					{
						textView.Redraw(lineByNumber.Offset, lineByNumber.Length);
					}
				}
			}
		}
		catch (ArgumentException)
		{
			RequestSafeRedraw();
		}
		catch (InvalidOperationException)
		{
			RequestSafeRedraw();
		}
	}

	private void RequestImmediateRedrawForDocumentRange(int startOffset, int endOffset)
	{
		TextDocument textDocument = EditBox?.Document;
		if (textDocument != null)
		{
			int num = Math.Max(0, Math.Min(startOffset, textDocument.TextLength));
			int offset = Math.Max(num, Math.Min(endOffset, textDocument.TextLength));
			DocumentLine lineByOffset = textDocument.GetLineByOffset(num);
			DocumentLine lineByOffset2 = textDocument.GetLineByOffset(offset);
			if (lineByOffset != null && lineByOffset2 != null)
			{
				int offset2 = lineByOffset.Offset;
				int num2 = Math.Max(offset2, lineByOffset2.EndOffset);
				RequestImmediateRedraw(offset2, num2 - offset2);
			}
		}
	}

	private void RequestImmediateRedrawForDocumentRangeWithPreviousLine(int startOffset, int endOffset)
	{
		TextDocument textDocument = EditBox?.Document;
		if (textDocument != null)
		{
			int num = Math.Max(0, Math.Min(startOffset, textDocument.TextLength));
			RequestImmediateRedrawForDocumentRange(textDocument.GetLineByOffset(num)?.PreviousLine?.Offset ?? num, endOffset);
		}
	}

	private void RequestImmediateRedrawForLines(IEnumerable<DocumentLine> lines)
	{
		List<DocumentLine> list = lines?.Where((DocumentLine line) => line != null && !line.IsDeleted).ToList();
		if (list != null && list.Count != 0)
		{
			RequestImmediateRedrawForDocumentRange(list.Min((DocumentLine line) => line.Offset), list.Max((DocumentLine line) => line.EndOffset));
		}
	}

	private void RequestImmediateRedrawForSelectionOrCaret()
	{
		if (EditBox?.Document != null)
		{
			if (EditBox.SelectionLength > 0)
			{
				RequestImmediateRedrawForDocumentRange(EditBox.SelectionStart, EditBox.SelectionStart + EditBox.SelectionLength);
			}
			else
			{
				RequestImmediateRedrawAroundCaret();
			}
		}
	}

	private void RequestImmediateRedrawAroundCaret()
	{
		TextDocument textDocument = EditBox?.Document;
		if (textDocument != null)
		{
			int offset = Math.Max(0, Math.Min(EditBox.CaretOffset, textDocument.TextLength));
			DocumentLine lineByOffset = textDocument.GetLineByOffset(offset);
			if (lineByOffset != null)
			{
				DocumentLine documentLine = lineByOffset.PreviousLine ?? lineByOffset;
				RequestImmediateRedrawForDocumentRange(documentLine.Offset, lineByOffset.EndOffset);
			}
		}
	}

	private void OnImeCompositionTextEntering(object sender, TextCompositionEventArgs e)
	{
		if (e != null && !e.Handled)
		{
			TextEditor editBox = EditBox;
			if (editBox != null && editBox.TextArea?.IsImeCompositionCommitInProgress == true)
			{
				BeforeTextInput(sender, e);
			}
		}
	}

	private void BeforeTextInput(object sender, TextCompositionEventArgs e)
	{
		if (CodeBlockAttachmentControl.TryFindFromOriginalSource(e?.OriginalSource, out var control) && control.IsSourceInsideCodeEditor(e.OriginalSource))
		{
			if (control.TryHandleTextInputForMaxLength(e.Text))
			{
				e.Handled = true;
			}
			return;
		}
		bool appendedParagraph;
		if (e != null)
		{
			TextEditor editBox = EditBox;
			if (editBox != null)
			{
				bool? flag = editBox.TextArea?.IsImeCompositionCommitInProgress;
				appendedParagraph = false;
				if (flag == appendedParagraph && EditBox.TextArea.ConsumeDuplicateImeTextInput(e.Text))
				{
					e.Handled = true;
					return;
				}
			}
		}
		if (string.IsNullOrEmpty(e.Text))
		{
			return;
		}
		TryNormalizeCaretBeforeCodeBlockAttachment(e.Text.Length, out var blockedByMaxLength);
		if (blockedByMaxLength)
		{
			e.Handled = true;
			Utils.Toast(Utils.GetString("CharactersNumberLimit"));
			return;
		}
		TryNormalizeCaretAfterCodeBlockAttachment(e.Text.Length, out appendedParagraph, out var blockedByMaxLength2);
		if (blockedByMaxLength2)
		{
			e.Handled = true;
			Utils.Toast(Utils.GetString("CharactersNumberLimit"));
			return;
		}
		NormalizeCaretIntoSemanticContentIfNeeded();
		if (!_inImeProcess)
		{
			TryNormalizeCaretOutsideLinkDisplayBoundary();
		}
		string text = e.Text;
		if (MaxLength >= 0)
		{
			int contentLengthForMaxLength = GetContentLengthForMaxLength();
			int selectionLengthForMaxLength = GetSelectionLengthForMaxLength(EditBox.SelectionStart, EditBox.SelectionLength);
			if (contentLengthForMaxLength >= MaxLength && selectionLengthForMaxLength == 0)
			{
				e.Handled = true;
				Utils.Toast(Utils.GetString("CharactersNumberLimit"));
				return;
			}
			if (contentLengthForMaxLength + text.Length - selectionLengthForMaxLength > MaxLength)
			{
				e.Handled = true;
				text = text.Substring(0, Math.Max(0, MaxLength - contentLengthForMaxLength + selectionLengthForMaxLength));
				if (text.Length == 0)
				{
					Utils.Toast(Utils.GetString("CharactersNumberLimit"));
					return;
				}
			}
		}
		BeginSearchDetailEditingIfNeeded();
		if (EditBox.SelectionLength == 0 && TryReplaceCaretAnchorWithTextInput(text))
		{
			e.Handled = true;
			RefreshOpenMenuContexts();
			return;
		}
		if (EditBox.SelectionLength == 0 && _activeTypingMarks.Count == 0 && TryApplyHeadingShortcut(e.Text))
		{
			e.Handled = true;
			RefreshOpenMenuContexts();
			return;
		}
		if (EditBox.SelectionLength == 0 && _activeTypingMarks.Count == 0 && TryApplyBlockShortcut(e.Text))
		{
			e.Handled = true;
			RefreshOpenMenuContexts();
			return;
		}
		if (EditBox.SelectionLength == 0 && _activeTypingMarks.Count == 0 && TryApplyHorizontalRuleShortcut(e.Text))
		{
			e.Handled = true;
			RefreshOpenMenuContexts();
			return;
		}
		if (EditBox.SelectionLength == 0 && CanApplyForwardInlineShortcutDuringActiveTyping(e.Text) && TryApplyForwardInlineShortcutFromTextInput(e.Text))
		{
			e.Handled = true;
			RefreshOpenMenuContexts();
			return;
		}
		int startOffset = ((EditBox.SelectionLength > 0) ? EditBox.SelectionStart : EditBox.CaretOffset);
		int selectionLength = EditBox.SelectionLength;
		List<MarkdownInlineDisplaySegment> typingMarksForInsertion = GetTypingMarksForInsertion(startOffset, selectionLength);
		if (typingMarksForInsertion.Count > 0)
		{
			InsertText(text, typingMarksForInsertion);
			e.Handled = true;
			RefreshOpenMenuContexts();
		}
		else if (e.Handled)
		{
			InsertText(text);
		}
		else
		{
			QueuePlainParagraphInput(startOffset, selectionLength, text);
			AdjustInlineSemanticMarksForDefaultTextMutation(startOffset, selectionLength, text.Length);
			PreserveSemanticBlocksForDefaultTextMutation(startOffset, selectionLength, text);
		}
	}

	private static bool IsCodeBlockAttachmentEditorInputSource(object originalSource)
	{
		if (CodeBlockAttachmentControl.TryFindFromOriginalSource(originalSource, out var control))
		{
			return control.IsSourceInsideCodeEditor(originalSource);
		}
		return false;
	}

	private bool TryReplaceCaretAnchorWithTextInput(string text)
	{
		if (string.IsNullOrEmpty(text) || EditBox?.Document == null || EditBox.SelectionLength > 0)
		{
			return false;
		}
		TextDocument document = EditBox.Document;
		int num = Math.Max(0, Math.Min(EditBox.CaretOffset, document.TextLength));
		if (num <= 0)
		{
			return false;
		}
		DocumentLine lineByOffset = document.GetLineByOffset(num);
		int anchorOffset = num - "\u00a0".Length;
		if (lineByOffset == null || anchorOffset < lineByOffset.Offset || document.GetText(anchorOffset, "\u00a0".Length) != "\u00a0")
		{
			return false;
		}
		MoveActiveTypingMarksFromOffset(num, anchorOffset);
		List<MarkdownInlineDisplaySegment> typingMarks = (from @group in (from mark in GetTypingMarksForInsertion(anchorOffset, "\u00a0".Length)
				where mark != null
				select mark).GroupBy(MarkdownInlineSemanticMetadata.GetPersistenceIdentity)
			select @group.First()).ToList();
		if (!CanApplyInlineSemanticTypingAtOffset(anchorOffset))
		{
			typingMarks.Clear();
		}
		if (typingMarks.Any((MarkdownInlineDisplaySegment mark) => mark != null && IsActiveTypingMarkAtOffset(mark.Kind, anchorOffset)))
		{
			MoveActiveTypingMarksToOffset(anchorOffset + text.Length);
		}
		AdjustInlineSemanticMarksForDefaultTextMutation(anchorOffset, "\u00a0".Length, text.Length);
		ApplyDocumentMutationsAndNotify(new DocumentMutation[1]
		{
			new DocumentMutation
			{
				StartOffset = anchorOffset,
				RemoveLength = "\u00a0".Length,
				InsertText = text
			}
		}, anchorOffset + text.Length, delegate
		{
			if (typingMarks.Count > 0)
			{
				ApplyTypingMarksToInsertedText(anchorOffset, text.Length, typingMarks);
			}
		});
		return true;
	}

	private bool TryApplyHeadingShortcut(string inputText)
	{
		if (!string.Equals(inputText, " ", StringComparison.Ordinal) || ReadOnly)
		{
			return false;
		}
		TextDocument document = EditBox.Document;
		if (document == null)
		{
			return false;
		}
		int caretOffset = EditBox.CaretOffset;
		DocumentLine lineByOffset = document.GetLineByOffset(Math.Max(0, Math.Min(caretOffset, document.TextLength)));
		if (lineByOffset == null)
		{
			return false;
		}
		int lineIndex = lineByOffset.LineNumber - 1;
		if (!CanApplyMarkdownShortcutsAtLine(lineIndex))
		{
			return false;
		}
		string text = document.GetText(lineByOffset.Offset, Math.Max(0, caretOffset - lineByOffset.Offset));
		int num = text.Length - text.TrimStart().Length;
		string text2 = text.Substring(Math.Min(num, text.Length));
		if (string.IsNullOrEmpty(text2) || text2.Length > 6 || text2.Any((char ch) => ch != '#'))
		{
			return false;
		}
		if (!CanApplyMarkdownBlockPrefixShortcutAtLine(lineIndex, text2))
		{
			return false;
		}
		int headingLevel = text2.Length;
		ApplyDocumentMutationsAndNotify(new DocumentMutation[1]
		{
			new DocumentMutation
			{
				StartOffset = lineByOffset.Offset + num,
				RemoveLength = text2.Length,
				InsertText = string.Empty
			}
		}, lineByOffset.Offset + num, delegate
		{
			SetSemanticHeadingLevelForLine(lineIndex, headingLevel);
		});
		ForceImmediateBlockRender();
		return true;
	}

	private bool TryApplyBlockShortcut(string inputText)
	{
		if (!string.Equals(inputText, " ", StringComparison.Ordinal) || ReadOnly)
		{
			return false;
		}
		TextDocument document = EditBox.Document;
		if (document == null)
		{
			return false;
		}
		int caretOffset = EditBox.CaretOffset;
		DocumentLine lineByOffset = document.GetLineByOffset(Math.Max(0, Math.Min(caretOffset, document.TextLength)));
		if (lineByOffset == null)
		{
			return false;
		}
		int lineIndex = lineByOffset.LineNumber - 1;
		if (!CanApplyMarkdownShortcutsAtLine(lineIndex))
		{
			return false;
		}
		string text = document.GetText(lineByOffset.Offset, Math.Max(0, caretOffset - lineByOffset.Offset));
		int num = text.Length - text.TrimStart().Length;
		string text2 = text.Substring(Math.Min(num, text.Length));
		if (string.IsNullOrEmpty(text2))
		{
			return false;
		}
		if (!CanApplyMarkdownBlockPrefixShortcutAtLine(lineIndex, text2))
		{
			return false;
		}
		int semanticIndentForLineOrZero = GetSemanticIndentForLineOrZero(lineIndex);
		int currentIndent = Math.Max(num, semanticIndentForLineOrZero);
		MarkdownBlockSyntaxInfo markdownBlockSyntaxInfo = MarkdownBlockSyntax.Parse(text + inputText);
		bool flag = false;
		Action action = null;
		if (string.Equals(text2, ">", StringComparison.Ordinal))
		{
			action = delegate
			{
				SetSemanticBlockQuoteForLine(lineIndex, enabled: true, currentIndent);
			};
		}
		else if (string.Equals(text2, "-", StringComparison.Ordinal) || string.Equals(text2, "*", StringComparison.Ordinal) || string.Equals(text2, "+", StringComparison.Ordinal))
		{
			if (markdownBlockSyntaxInfo.Kind != MarkdownBlockSyntaxKind.List || markdownBlockSyntaxInfo.ListKind != MarkdownListKind.Bullet)
			{
				return false;
			}
			currentIndent = Math.Max(markdownBlockSyntaxInfo.Indent * 4, semanticIndentForLineOrZero);
			flag = true;
			action = delegate
			{
				SetSemanticBulletListForLine(lineIndex, enabled: true, currentIndent);
			};
		}
		else if (string.Equals(text2, "[ ]", StringComparison.Ordinal) && TryGetSemanticBulletListForLine(lineIndex))
		{
			action = delegate
			{
				SetSemanticTaskListForLine(lineIndex, enabled: true, isChecked: false, currentIndent);
			};
		}
		else if (string.Equals(text2, "[x]", StringComparison.OrdinalIgnoreCase) && TryGetSemanticBulletListForLine(lineIndex))
		{
			action = delegate
			{
				SetSemanticTaskListForLine(lineIndex, enabled: true, isChecked: true, currentIndent);
			};
		}
		else if (string.Equals(text2, "- [ ]", StringComparison.Ordinal))
		{
			if (markdownBlockSyntaxInfo.Kind != MarkdownBlockSyntaxKind.List || markdownBlockSyntaxInfo.ListKind != MarkdownListKind.Task)
			{
				return false;
			}
			currentIndent = Math.Max(markdownBlockSyntaxInfo.Indent * 4, semanticIndentForLineOrZero);
			flag = true;
			action = delegate
			{
				SetSemanticTaskListForLine(lineIndex, enabled: true, isChecked: false, currentIndent);
			};
		}
		else if (string.Equals(text2, "- [x]", StringComparison.OrdinalIgnoreCase))
		{
			if (markdownBlockSyntaxInfo.Kind != MarkdownBlockSyntaxKind.List || markdownBlockSyntaxInfo.ListKind != MarkdownListKind.Task)
			{
				return false;
			}
			currentIndent = Math.Max(markdownBlockSyntaxInfo.Indent * 4, semanticIndentForLineOrZero);
			flag = true;
			action = delegate
			{
				SetSemanticTaskListForLine(lineIndex, enabled: true, isChecked: true, currentIndent);
			};
		}
		else
		{
			Match match = Regex.Match(text2, "^(?<number>\\d+)\\.$");
			if (match.Success)
			{
				if (markdownBlockSyntaxInfo.Kind != MarkdownBlockSyntaxKind.List || markdownBlockSyntaxInfo.ListKind != MarkdownListKind.Numbered)
				{
					return false;
				}
				currentIndent = Math.Max(markdownBlockSyntaxInfo.Indent * 4, semanticIndentForLineOrZero);
				flag = true;
				string numberText = match.Groups["number"].Value;
				action = delegate
				{
					SetSemanticOrderedListForLine(lineIndex, enabled: true, numberText, currentIndent);
				};
			}
		}
		if (action == null)
		{
			return false;
		}
		int num2 = (flag ? lineByOffset.Offset : (lineByOffset.Offset + num));
		int removeLength = text2.Length + (flag ? num : 0);
		ApplyDocumentMutationsAndNotify(new DocumentMutation[1]
		{
			new DocumentMutation
			{
				StartOffset = num2,
				RemoveLength = removeLength,
				InsertText = "\u00a0"
			}
		}, num2 + "\u00a0".Length, action);
		ForceImmediateBlockRender();
		return true;
	}

	private bool TryApplyHorizontalRuleShortcut(string inputText)
	{
		if (!string.Equals(inputText, "*", StringComparison.Ordinal) || ReadOnly || EditBox?.Document == null || EditBox.SelectionLength > 0)
		{
			return false;
		}
		TextDocument document = EditBox.Document;
		int num = Math.Max(0, Math.Min(EditBox.CaretOffset, document.TextLength));
		DocumentLine lineByOffset = document.GetLineByOffset(num);
		if (lineByOffset == null || num != lineByOffset.EndOffset)
		{
			return false;
		}
		int lineIndex = lineByOffset.LineNumber - 1;
		if (!CanApplyMarkdownShortcutsAtLine(lineIndex) || (TryGetSemanticBlockAtLine(lineIndex, out var block) && (block == null || block.Kind != MarkdownBlockKind.Paragraph)))
		{
			return false;
		}
		string text = document.GetText(lineByOffset);
		int startIndex = num - lineByOffset.Offset;
		string line = text.Insert(startIndex, inputText);
		MarkdownBlockSyntaxInfo completedSyntax = MarkdownBlockSyntax.Parse(line);
		if (completedSyntax.Kind != MarkdownBlockSyntaxKind.SplitLine)
		{
			return false;
		}
		string text2 = EditBox.Text ?? string.Empty;
		string completedRawText = text2.Insert(num, inputText);
		MarkdownSemanticDocument semanticDocumentOverride = BuildEditingSemanticDocument(text2);
		CaptureForwardInlineShortcutRawUndoState(text2, completedRawText, num, inputText.Length, num, 0, lineByOffset.Offset, lineByOffset.EndOffset + inputText.Length, semanticDocumentOverride);
		ApplyDocumentMutationsAndNotify(new DocumentMutation[1]
		{
			new DocumentMutation
			{
				StartOffset = num,
				RemoveLength = 0,
				InsertText = inputText
			}
		}, num + inputText.Length, delegate
		{
			SetSemanticHorizontalRuleForLine(lineIndex, ConvertIndentLevelsToWhitespaceCount(completedSyntax.Indent));
		});
		ForceImmediateBlockRender();
		return true;
	}

	private bool TryApplyForwardInlineShortcutFromTextInput(string inputText)
	{
		if (string.Equals(inputText, ")", StringComparison.Ordinal))
		{
			return TryApplyForwardInlineShortcut(inputText);
		}
		if (string.Equals(inputText, " ", StringComparison.Ordinal))
		{
			return TryApplyForwardInlineShortcutOnSpace();
		}
		return TryInsertCompletedInlineShortcutSyntaxAsLiteral(inputText);
	}

	private bool TryApplyForwardInlineShortcutOnSpace()
	{
		if (EditBox?.Document == null || EditBox.SelectionLength > 0)
		{
			return false;
		}
		int num = Math.Max(0, Math.Min(EditBox.CaretOffset, EditBox.Document.TextLength));
		if (num <= 0)
		{
			return false;
		}
		DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(num);
		if (lineByOffset != null && !CanApplyMarkdownShortcutsAtLine(lineByOffset.LineNumber - 1))
		{
			return false;
		}
		string text = EditBox.Text ?? string.Empty;
		string completedRawText = text.Insert(num, " ");
		List<MarkdownInlineDisplaySegment> activeTypingMarks = GetActiveTypingMarkSegments(num).Select(CloneTypingStyleMark).ToList();
		return TryApplyForwardInlineStyleShortcut(text, text, completedRawText, num, text[num - 1], num, 1, triggeredBySpace: true, activeTypingMarks);
	}

	private bool TryInsertCompletedInlineShortcutSyntaxAsLiteral(string inputText)
	{
		if (string.IsNullOrEmpty(inputText) || inputText.Length != 1 || EditBox?.Document == null || EditBox.SelectionLength > 0)
		{
			return false;
		}
		int caretOffset = Math.Max(0, Math.Min(EditBox.CaretOffset, EditBox.Document.TextLength));
		DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(caretOffset);
		if (lineByOffset != null && !CanApplyMarkdownShortcutsAtLine(lineByOffset.LineNumber - 1))
		{
			return false;
		}
		string syntaxText = (EditBox.Text ?? string.Empty).Insert(caretOffset, inputText);
		int num = caretOffset + inputText.Length;
		if (!TryGetForwardInlineStyleShortcutMatch(syntaxText, num, inputText[0], out var delimiter, out var _, out var openStart, out var _, out var closeStart, out var _, out var _))
		{
			return false;
		}
		List<MarkdownInlineDisplaySegment> typingMarks = GetTypingMarksForInsertion(caretOffset, 0);
		int literalEnd = closeStart + delimiter.Length;
		ApplyDocumentMutationsAndNotify(new DocumentMutation[1]
		{
			new DocumentMutation
			{
				StartOffset = caretOffset,
				RemoveLength = 0,
				InsertText = inputText
			}
		}, num, delegate
		{
			if (typingMarks.Count > 0)
			{
				ApplyTypingMarksToInsertedText(caretOffset, inputText.Length, typingMarks);
			}
			AddOrExtendTypedSemanticMark(MarkdownMarkKind.LiteralInlineSyntax, openStart, literalEnd - openStart);
		}, preserveSemanticBlocks: true, refreshEditingState: false);
		return true;
	}

	private bool TryApplyForwardInlineShortcut(string inputText)
	{
		if (string.IsNullOrEmpty(inputText) || inputText.Length != 1 || EditBox.Document == null)
		{
			return false;
		}
		int caretOffset = EditBox.CaretOffset;
		DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(Math.Max(0, Math.Min(caretOffset, EditBox.Document.TextLength)));
		if (lineByOffset != null && !CanApplyMarkdownShortcutsAtLine(lineByOffset.LineNumber - 1))
		{
			return false;
		}
		string text = EditBox.Text ?? string.Empty;
		string text2 = text.Insert(caretOffset, inputText);
		int num = caretOffset + inputText.Length;
		List<MarkdownInlineDisplaySegment> activeTypingMarks = GetActiveTypingMarkSegments(caretOffset).Select(CloneTypingStyleMark).ToList();
		if (inputText[0] == ')' && TryApplyMarkdownLinkForwardShortcut(text2, caretOffset, num))
		{
			return true;
		}
		return TryApplyForwardInlineStyleShortcut(text, text2, text2, num, inputText[0], caretOffset, inputText.Length, triggeredBySpace: false, activeTypingMarks);
	}

	private bool TryApplyForwardInlineStyleShortcut(string currentText, string syntaxText, string completedRawText, int closeEnd, char trailingChar, int insertedOffset, int insertedLength, bool triggeredBySpace, IReadOnlyCollection<MarkdownInlineDisplaySegment> activeTypingMarks)
	{
		if (!TryGetForwardInlineStyleShortcutMatch(syntaxText, closeEnd, trailingChar, out var delimiter, out var kind, out var openStart, out var contentStart, out var closeStart, out var contentLength, out var shortcutColor))
		{
			return false;
		}
		List<DocumentMutation> list = new List<DocumentMutation>
		{
			new DocumentMutation
			{
				StartOffset = openStart,
				RemoveLength = contentStart - openStart,
				InsertText = string.Empty
			}
		};
		int num = (triggeredBySpace ? delimiter.Length : Math.Max(0, delimiter.Length - insertedLength));
		if (num > 0)
		{
			list.Add(new DocumentMutation
			{
				StartOffset = closeStart,
				RemoveLength = num,
				InsertText = string.Empty
			});
		}
		CaptureForwardInlineShortcutRawUndoState(currentText, completedRawText, insertedOffset, insertedLength, closeStart, (!triggeredBySpace) ? num : 0, openStart, closeStart + delimiter.Length);
		RemoveTypedSemanticMarks(MarkdownMarkKind.LiteralInlineSyntax, openStart, closeStart + delimiter.Length);
		ApplyDocumentMutationsAndNotify(list, openStart + contentLength, delegate
		{
			IReadOnlyCollection<MarkdownInlineDisplaySegment> readOnlyCollection = activeTypingMarks;
			if (readOnlyCollection != null && readOnlyCollection.Count > 0)
			{
				ApplyTypingMarksToInsertedText(openStart, contentLength, activeTypingMarks);
			}
			AddOrExtendTypedSemanticMark(kind, openStart, contentLength, shortcutColor);
		}, preserveSemanticBlocks: false, refreshEditingState: false);
		if (triggeredBySpace)
		{
			CaptureSemanticHistorySnapshot();
		}
		_suppressedTypingMarks[kind] = openStart + contentLength;
		_needsTypedSemanticMarkRedraw = false;
		return true;
	}

	private bool TryGetForwardInlineStyleShortcutMatch(string syntaxText, int closeEnd, char trailingChar, out string delimiter, out MarkdownMarkKind kind, out int openStart, out int contentStart, out int closeStart, out int contentLength, out string shortcutColor)
	{
		delimiter = null;
		kind = MarkdownMarkKind.Link;
		openStart = -1;
		contentStart = -1;
		closeStart = -1;
		contentLength = 0;
		shortcutColor = null;
		foreach (KeyValuePair<string, MarkdownMarkKind> forwardInlineShortcutCandidate in GetForwardInlineShortcutCandidates(trailingChar))
		{
			string key = forwardInlineShortcutCandidate.Key;
			MarkdownMarkKind value = forwardInlineShortcutCandidate.Value;
			if (LooksLikeReversePrefixInsertion(syntaxText, closeEnd, key))
			{
				continue;
			}
			int num = closeEnd - key.Length;
			if (num <= 0 || num + key.Length > syntaxText.Length || !string.Equals(syntaxText.Substring(num, key.Length), key, StringComparison.Ordinal))
			{
				continue;
			}
			int num2 = syntaxText.LastIndexOf(key, num - 1, StringComparison.Ordinal);
			if (num2 < 0 || !CanApplyForwardInlineShortcut(syntaxText, num2, num, key))
			{
				continue;
			}
			int contentStart2 = num2 + key.Length;
			string color = null;
			if (value == MarkdownMarkKind.Highlight && !TryResolveHighlightShortcutContentStart(syntaxText, num2, num, key, out contentStart2, out color))
			{
				continue;
			}
			if (value == MarkdownMarkKind.Highlight && string.Equals(key, "::", StringComparison.Ordinal))
			{
				color = null;
			}
			int num3 = num - contentStart2;
			if (num3 > 0)
			{
				string text = syntaxText.Substring(contentStart2, num3);
				if (text.IndexOf('\n') < 0 && text.IndexOf('\r') < 0)
				{
					delimiter = key;
					kind = value;
					openStart = num2;
					contentStart = contentStart2;
					closeStart = num;
					contentLength = num3;
					shortcutColor = color;
					return true;
				}
			}
		}
		return false;
	}

	private void CaptureForwardInlineShortcutRawUndoState(string currentText, string completedRawText, int insertedOffset, int insertedLength, int closingPrefixStart, int closingPrefixLength, int literalStart = -1, int literalEnd = -1, MarkdownSemanticDocument semanticDocumentOverride = null)
	{
		if (_suppressSemanticHistory || completedRawText == null)
		{
			return;
		}
		if (closingPrefixLength > 0 && !string.IsNullOrEmpty(currentText) && closingPrefixStart >= 0 && closingPrefixStart + closingPrefixLength <= currentText.Length && _semanticUndoHistory.Count >= 2)
		{
			EditorHistoryState editorHistoryState = _semanticUndoHistory.LastOrDefault();
			EditorHistoryState editorHistoryState2 = _semanticUndoHistory[_semanticUndoHistory.Count - 2];
			if (editorHistoryState != null && editorHistoryState2 != null && editorHistoryState.CaretOffset == closingPrefixStart + closingPrefixLength && string.Equals(editorHistoryState.DisplayText, currentText, StringComparison.Ordinal))
			{
				string b = currentText.Remove(closingPrefixStart, closingPrefixLength);
				if (string.Equals(editorHistoryState2.DisplayText, b, StringComparison.Ordinal))
				{
					_semanticUndoHistory.RemoveAt(_semanticUndoHistory.Count - 1);
				}
			}
		}
		List<MarkdownInlineDisplaySegment> list = CloneDisplaySegments(_typedSemanticMarks);
		if (insertedLength > 0)
		{
			AdjustInlineSemanticMarksForReplace(list, insertedOffset, 0, insertedLength);
		}
		RemoveInlineSemanticMarksOverlappingRange(list, literalStart, literalEnd, completedRawText.Length);
		if (literalStart >= 0 && literalEnd > literalStart)
		{
			int num = Math.Max(0, Math.Min(literalStart, completedRawText.Length));
			int num2 = Math.Max(num, Math.Min(literalEnd, completedRawText.Length));
			if (num2 > num)
			{
				list.Add(new MarkdownInlineDisplaySegment
				{
					Kind = MarkdownMarkKind.LiteralInlineSyntax,
					SourcePosition = num,
					SourceLength = num2 - num,
					OpeningDelimiterLength = 0,
					ClosingDelimiterLength = 0
				});
			}
		}
		MarkdownSemanticDocument semanticDocument = CloneSemanticDocument(semanticDocumentOverride ?? _semanticDocument);
		string persistedText = BuildPersistedTextForHistoryState(completedRawText, list, semanticDocument);
		EditorHistoryState editorHistoryState3 = new EditorHistoryState
		{
			PersistedText = persistedText,
			DisplayText = completedRawText,
			SemanticDocument = semanticDocument,
			CaretOffset = Math.Max(0, Math.Min(insertedOffset + Math.Max(0, insertedLength), completedRawText.Length))
		};
		editorHistoryState3.TypedSemanticMarks.AddRange(list);
		AddSemanticHistorySnapshot(editorHistoryState3);
	}

	private static void RemoveInlineSemanticMarksOverlappingRange(IList<MarkdownInlineDisplaySegment> marks, int rangeStart, int rangeEnd, int textLength)
	{
		if (marks == null || marks.Count == 0 || rangeStart < 0 || rangeEnd <= rangeStart)
		{
			return;
		}
		int num = Math.Max(0, Math.Min(rangeStart, Math.Max(0, textLength)));
		int num2 = Math.Max(num, Math.Min(rangeEnd, Math.Max(0, textLength)));
		if (num2 <= num)
		{
			return;
		}
		for (int num3 = marks.Count - 1; num3 >= 0; num3--)
		{
			MarkdownInlineDisplaySegment markdownInlineDisplaySegment = marks[num3];
			if (markdownInlineDisplaySegment != null && markdownInlineDisplaySegment.SourceLength > 0)
			{
				int num4 = Math.Max(0, Math.Min(markdownInlineDisplaySegment.SourcePosition, Math.Max(0, textLength)));
				int num5 = Math.Max(num4, Math.Min(markdownInlineDisplaySegment.SourcePosition + markdownInlineDisplaySegment.SourceLength, Math.Max(0, textLength)));
				if (num4 < num2 && num5 > num)
				{
					marks.RemoveAt(num3);
				}
			}
		}
	}

	private bool TryApplyMarkdownLinkForwardShortcut(string prospectiveText, int caretOffset, int linkEndExclusive)
	{
		if (string.IsNullOrEmpty(prospectiveText) || linkEndExclusive <= 0 || linkEndExclusive > prospectiveText.Length || prospectiveText[linkEndExclusive - 1] != ')')
		{
			return false;
		}
		int lineStartOffset = GetLineStartOffset(prospectiveText, linkEndExclusive - 1);
		int num = linkEndExclusive - 2;
		if (num < lineStartOffset)
		{
			return false;
		}
		int num2 = prospectiveText.LastIndexOf("](", num, num - lineStartOffset + 1, StringComparison.Ordinal);
		if (num2 <= lineStartOffset)
		{
			return false;
		}
		int linkStart = prospectiveText.LastIndexOf('[', num2 - 1, num2 - lineStartOffset);
		if (linkStart < lineStartOffset || (linkStart > 0 && prospectiveText[linkStart - 1] == '!'))
		{
			return false;
		}
		int num3 = num2 + 2;
		if (FindRawMarkdownLinkUrlEnd(prospectiveText, num3) != linkEndExclusive - 1)
		{
			return false;
		}
		int num4 = linkStart + 1;
		int num5 = num2 - num4;
		int num6 = linkEndExclusive - 1 - num3;
		if (num5 <= 0 || num6 <= 0)
		{
			return false;
		}
		string text = prospectiveText.Substring(num4, num5);
		string url = prospectiveText.Substring(num3, num6);
		if (text.IndexOf('\r') >= 0 || text.IndexOf('\n') >= 0 || url.IndexOf('\r') >= 0 || url.IndexOf('\n') >= 0)
		{
			return false;
		}
		DocumentMutation[] mutations = new DocumentMutation[1]
		{
			new DocumentMutation
			{
				StartOffset = linkStart,
				RemoveLength = caretOffset - linkStart,
				InsertText = text
			}
		};
		int displayLinkLength = text.Length;
		CaptureForwardInlineShortcutRawUndoState(EditBox.Text ?? string.Empty, prospectiveText, caretOffset, Math.Max(0, linkEndExclusive - caretOffset), linkEndExclusive - 1, 0, linkStart, linkEndExclusive);
		ApplyDocumentMutationsAndNotify(mutations, linkStart + displayLinkLength, delegate
		{
			AddLinkSemanticMark(linkStart, displayLinkLength, url);
		}, preserveSemanticBlocks: false, refreshEditingState: false);
		_needsTypedSemanticMarkRedraw = false;
		return true;
	}

	private bool TryConvertCompletedRawMarkdownLinkAroundCaret(bool notifyTextChanged = true, int? urlOffset = null)
	{
		if (_inImeProcess || EditBox?.Document == null || EditBox.SelectionLength > 0)
		{
			return false;
		}
		string text = EditBox.Text ?? string.Empty;
		int num = Math.Max(0, Math.Min(EditBox.CaretOffset, text.Length));
		int offset = (urlOffset.HasValue ? Math.Max(0, Math.Min(urlOffset.Value, text.Length)) : num);
		if (!TryFindCompletedRawMarkdownLinkAroundUrlOffset(text, offset, out var linkStart, out var textStart, out var textLength, out var urlStart, out var urlLength, out var linkEndExclusive))
		{
			return false;
		}
		string linkText = text.Substring(textStart, textLength);
		string url = text.Substring(urlStart, urlLength);
		if (!IsAutoConvertibleRawMarkdownLinkUrl(url))
		{
			return false;
		}
		CaptureForwardInlineShortcutRawUndoState(text, text, num, 0, linkEndExclusive - 1, 0, linkStart, linkEndExclusive);
		ApplyDocumentMutationsAndNotify(new DocumentMutation[1]
		{
			new DocumentMutation
			{
				StartOffset = linkStart,
				RemoveLength = linkEndExclusive - linkStart,
				InsertText = linkText
			}
		}, linkStart + linkText.Length, delegate
		{
			AddLinkSemanticMark(linkStart, linkText.Length, url);
		}, preserveSemanticBlocks: false, refreshEditingState: false, notifyTextChanged);
		_needsTypedSemanticMarkRedraw = false;
		return true;
	}

	private void TrackRawMarkdownLinkUrlAtCaret()
	{
		if (_inImeProcess || EditBox?.Document == null || EditBox.SelectionLength > 0)
		{
			ClearTrackedRawMarkdownLinkUrl();
			return;
		}
		string text = EditBox.Text ?? string.Empty;
		int offset = Math.Max(0, Math.Min(EditBox.CaretOffset, text.Length));
		if (TryFindCompletedRawMarkdownLinkAroundUrlOffset(text, offset, out var _, out var _, out var _, out var urlStart, out var urlLength, out var _))
		{
			_trackedRawMarkdownLinkUrlStart = urlStart;
			_trackedRawMarkdownLinkUrlEnd = urlStart + urlLength;
			_trackedRawMarkdownLinkTextLength = text.Length;
		}
		else
		{
			ClearTrackedRawMarkdownLinkUrl();
		}
	}

	private bool TryConvertTrackedRawMarkdownLinkUrlIfCaretLeft(bool force = false)
	{
		if (_inImeProcess)
		{
			return false;
		}
		if (_trackedRawMarkdownLinkUrlStart < 0 || _trackedRawMarkdownLinkUrlEnd < _trackedRawMarkdownLinkUrlStart || EditBox?.Document == null)
		{
			if (force && TryConvertCompletedRawMarkdownLinkAroundCaret())
			{
				ClearTrackedRawMarkdownLinkUrl();
				return true;
			}
			TrackRawMarkdownLinkUrlAtCaret();
			return false;
		}
		string text = EditBox.Text ?? string.Empty;
		int num = Math.Max(0, Math.Min(EditBox.CaretOffset, text.Length));
		bool flag = text.Length == _trackedRawMarkdownLinkTextLength;
		bool flag2 = num >= _trackedRawMarkdownLinkUrlStart && num <= _trackedRawMarkdownLinkUrlEnd;
		if (!force && (!flag || flag2))
		{
			TrackRawMarkdownLinkUrlAtCaret();
			return false;
		}
		int value = Math.Max(_trackedRawMarkdownLinkUrlStart, Math.Min(_trackedRawMarkdownLinkUrlEnd, text.Length));
		ClearTrackedRawMarkdownLinkUrl();
		bool num2 = TryConvertCompletedRawMarkdownLinkAroundCaret(notifyTextChanged: true, value);
		if (!num2)
		{
			TrackRawMarkdownLinkUrlAtCaret();
		}
		return num2;
	}

	private void ClearTrackedRawMarkdownLinkUrl()
	{
		_trackedRawMarkdownLinkUrlStart = -1;
		_trackedRawMarkdownLinkUrlEnd = -1;
		_trackedRawMarkdownLinkTextLength = -1;
	}

	private static bool TryFindCompletedRawMarkdownLinkAroundUrlOffset(string text, int offset, out int linkStart, out int textStart, out int textLength, out int urlStart, out int urlLength, out int linkEndExclusive)
	{
		linkStart = 0;
		textStart = 0;
		textLength = 0;
		urlStart = 0;
		urlLength = 0;
		linkEndExclusive = 0;
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		int num = Math.Max(0, Math.Min(offset, text.Length));
		int offset2 = Math.Max(0, Math.Min(num, text.Length - 1));
		int lineStartOffset = GetLineStartOffset(text, offset2);
		int num2 = Math.Min(Math.Max(num - 1, lineStartOffset), text.Length - 1);
		if (num2 < lineStartOffset)
		{
			return false;
		}
		int num3 = text.LastIndexOf("](", num2, num2 - lineStartOffset + 1, StringComparison.Ordinal);
		if (num3 <= lineStartOffset)
		{
			return false;
		}
		linkStart = text.LastIndexOf('[', num3 - 1, num3 - lineStartOffset);
		if (linkStart < lineStartOffset || (linkStart > 0 && text[linkStart - 1] == '!'))
		{
			return false;
		}
		urlStart = num3 + 2;
		int num4 = FindRawMarkdownLinkUrlEnd(text, urlStart);
		if (num4 < 0 || num < urlStart || num > num4)
		{
			return false;
		}
		textStart = linkStart + 1;
		textLength = num3 - textStart;
		urlLength = num4 - urlStart;
		linkEndExclusive = num4 + 1;
		if (textLength <= 0 || urlLength <= 0)
		{
			return false;
		}
		string text2 = text.Substring(textStart, textLength);
		string text3 = text.Substring(urlStart, urlLength);
		if (text2.IndexOf('\r') < 0 && text2.IndexOf('\n') < 0 && text3.IndexOf('\r') < 0)
		{
			return text3.IndexOf('\n') < 0;
		}
		return false;
	}

	private static bool ShouldTryConvertCompletedRawMarkdownLinkAfterInsertedText(string insertedText)
	{
		if (!string.IsNullOrEmpty(insertedText) && insertedText.Length > 1 && insertedText.IndexOf('\r') < 0)
		{
			return insertedText.IndexOf('\n') < 0;
		}
		return false;
	}

	private static bool IsAutoConvertibleRawMarkdownLinkUrl(string url)
	{
		if (string.IsNullOrWhiteSpace(url))
		{
			return false;
		}
		if (!string.Equals(url.Trim(), url, StringComparison.Ordinal))
		{
			return false;
		}
		Match match = TaskUtils.UrlRegex.Match(url);
		if (match.Success && match.Index == 0)
		{
			return match.Length == url.Length;
		}
		return false;
	}

	private static int GetLineStartOffset(string text, int offset)
	{
		if (string.IsNullOrEmpty(text))
		{
			return 0;
		}
		int num = Math.Max(0, Math.Min(offset, text.Length - 1));
		while (num > 0 && text[num - 1] != '\r' && text[num - 1] != '\n')
		{
			num--;
		}
		return num;
	}

	private bool CanApplyMarkdownShortcutsAtLine(int lineIndex)
	{
		if (lineIndex < 0)
		{
			return true;
		}
		if (TryGetSemanticBlockAtLine(lineIndex, out var block))
		{
			return CanApplyMarkdownShortcutsForBlock(block);
		}
		return true;
	}

	private static bool CanApplyMarkdownShortcutsForBlock(MarkdownBlockNode block)
	{
		if (block == null)
		{
			return true;
		}
		return block.Kind != MarkdownBlockKind.CodeBlock;
	}

	private bool CanApplyMarkdownBlockPrefixShortcutAtLine(int lineIndex, string trimmedPrefix)
	{
		if (TryGetSemanticBlockAtLine(lineIndex, out var block))
		{
			return CanApplyMarkdownBlockPrefixShortcutForBlock(block, trimmedPrefix);
		}
		return true;
	}

	private static bool CanApplyMarkdownBlockPrefixShortcutForBlock(MarkdownBlockNode block, string trimmedPrefix)
	{
		if (block != null && block.Kind == MarkdownBlockKind.CodeBlock)
		{
			return false;
		}
		if (block == null || block.Kind != MarkdownBlockKind.List)
		{
			return true;
		}
		if (!string.Equals(trimmedPrefix, "[ ]", StringComparison.Ordinal))
		{
			return string.Equals(trimmedPrefix, "[x]", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	private static bool TryResolveHighlightShortcutContentStart(string text, int openStart, int closeStart, string delimiter, out int contentStart, out string color)
	{
		contentStart = openStart + delimiter.Length;
		color = null;
		if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(delimiter))
		{
			return false;
		}
		if (contentStart >= closeStart)
		{
			return false;
		}
		if (text[contentStart] != '{')
		{
			return true;
		}
		int num = text.IndexOf('}', contentStart);
		if (num <= contentStart + 1 || num >= closeStart)
		{
			return false;
		}
		color = text.Substring(contentStart + 1, num - contentStart - 1);
		contentStart = num + 1;
		return contentStart < closeStart;
	}

	private static bool LooksLikeReversePrefixInsertion(string text, int insertedEnd, string delimiter)
	{
		if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(delimiter) || insertedEnd < delimiter.Length || insertedEnd > text.Length)
		{
			return false;
		}
		int num = insertedEnd - delimiter.Length;
		string text2 = ExpandReverseDelimiterCluster(text, num, delimiter);
		if (!string.Equals(text.Substring(num, text2.Length), text2, StringComparison.Ordinal))
		{
			return false;
		}
		int num2 = num + text2.Length;
		if (num2 >= text.Length || char.IsWhiteSpace(text[num2]))
		{
			return false;
		}
		return text.IndexOf(text2, num2, StringComparison.Ordinal) >= 0;
	}

	private static bool CanApplyForwardInlineShortcut(string text, int openStart, int closeStart, string delimiter)
	{
		if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(delimiter))
		{
			return false;
		}
		if (delimiter.Length != 1)
		{
			return true;
		}
		char c = delimiter[0];
		if ((openStart > 0 && text[openStart - 1] == c) || (openStart + 1 < text.Length && text[openStart + 1] == c))
		{
			return false;
		}
		if ((closeStart > 0 && text[closeStart - 1] == c) || (closeStart + 1 < text.Length && text[closeStart + 1] == c))
		{
			return false;
		}
		return true;
	}

	private static IEnumerable<KeyValuePair<string, MarkdownMarkKind>> GetForwardInlineShortcutCandidates(char trailingChar)
	{
		switch (trailingChar)
		{
		case '*':
			yield return new KeyValuePair<string, MarkdownMarkKind>("**", MarkdownMarkKind.Strong);
			yield return new KeyValuePair<string, MarkdownMarkKind>("*", MarkdownMarkKind.Emphasis);
			break;
		case '`':
			yield return new KeyValuePair<string, MarkdownMarkKind>("`", MarkdownMarkKind.Code);
			break;
		case ':':
			yield return new KeyValuePair<string, MarkdownMarkKind>("::", MarkdownMarkKind.Highlight);
			break;
		case '=':
			yield return new KeyValuePair<string, MarkdownMarkKind>("==", MarkdownMarkKind.Highlight);
			break;
		case '~':
			yield return new KeyValuePair<string, MarkdownMarkKind>("~~", MarkdownMarkKind.Strikethrough);
			yield return new KeyValuePair<string, MarkdownMarkKind>("~", MarkdownMarkKind.Underline);
			break;
		}
	}

	private static bool ShouldNormalizeLinkBoundaryBeforeKeyInput(Key key)
	{
		switch (key)
		{
		case Key.Back:
		case Key.Tab:
		case Key.Escape:
		case Key.Prior:
		case Key.Next:
		case Key.End:
		case Key.Home:
		case Key.Left:
		case Key.Up:
		case Key.Right:
		case Key.Down:
		case Key.Delete:
			return false;
		default:
			return true;
		}
	}

	private List<MarkdownInlineDisplaySegment> GetTypingMarksForInsertion(int startOffset, int removedLength)
	{
		if (!CanApplyInlineSemanticTypingAtOffset(startOffset))
		{
			ClearBackspaceRetainedTypingMarks();
			return new List<MarkdownInlineDisplaySegment>();
		}
		if (TryTakeBackspaceRetainedTypingMarks(startOffset, out var marks))
		{
			return marks;
		}
		List<MarkdownInlineDisplaySegment> inheritedTypingMarkSegmentsForInsertion = GetInheritedTypingMarkSegmentsForInsertion(startOffset, removedLength);
		List<MarkdownInlineDisplaySegment> list = GetActiveTypingMarkSegments(startOffset).ToList();
		if (list.Count == 0)
		{
			return inheritedTypingMarkSegmentsForInsertion;
		}
		if (inheritedTypingMarkSegmentsForInsertion.Any((MarkdownInlineDisplaySegment mark) => mark.Kind == MarkdownMarkKind.Highlight) && list.Any((MarkdownInlineDisplaySegment mark) => mark.Kind == MarkdownMarkKind.Highlight))
		{
			if (_activeTypingHighlightOffset == startOffset)
			{
				inheritedTypingMarkSegmentsForInsertion.RemoveAll((MarkdownInlineDisplaySegment mark) => mark.Kind == MarkdownMarkKind.Highlight);
			}
			else
			{
				list.RemoveAll((MarkdownInlineDisplaySegment mark) => mark.Kind == MarkdownMarkKind.Highlight);
			}
		}
		HashSet<MarkdownMarkKind> inheritedKinds = new HashSet<MarkdownMarkKind>(inheritedTypingMarkSegmentsForInsertion.Select((MarkdownInlineDisplaySegment mark) => mark.Kind));
		return (from @group in inheritedTypingMarkSegmentsForInsertion.Concat(list.Where((MarkdownInlineDisplaySegment mark) => mark != null && !inheritedKinds.Contains(mark.Kind))).GroupBy(MarkdownInlineSemanticMetadata.GetPersistenceIdentity)
			select @group.First()).ToList();
	}

	private void CaptureBackspaceRetainedTypingMarks(int deletionStart, int deletionLength)
	{
		ClearBackspaceRetainedTypingMarks();
		if (deletionLength <= 0 || EditBox?.Document == null || EditBox.SelectionLength > 0 || !CanApplyInlineSemanticTypingAtOffset(deletionStart))
		{
			return;
		}
		int deletionEnd = deletionStart + deletionLength;
		List<MarkdownInlineDisplaySegment> list = (from @group in (from mark in (from mark in GetSemanticMarkList()
					where mark != null && mark.SourceLength > 0 && IsInlineTypingContextMark(mark) && mark.SourcePosition < deletionEnd && mark.SourcePosition + mark.SourceLength > deletionStart && !IsTypingMarkSuppressedAtOffset(mark.Kind, deletionStart) && !IsTypingMarkSuppressedAtOffset(mark.Kind, deletionEnd)
					select mark).Select(CloneTypingStyleMark).Concat(GetActiveTypingMarkSegments(deletionStart).Select(CloneTypingStyleMark))
				where mark != null
				select mark).GroupBy(MarkdownInlineSemanticMetadata.GetPersistenceIdentity)
			select @group.First()).ToList();
		if (list.Count == 0)
		{
			return;
		}
		MarkdownInlineDisplaySegment markdownInlineDisplaySegment = list.LastOrDefault((MarkdownInlineDisplaySegment mark) => mark.Kind == MarkdownMarkKind.Highlight);
		if (markdownInlineDisplaySegment != null)
		{
			list.RemoveAll((MarkdownInlineDisplaySegment mark) => mark.Kind == MarkdownMarkKind.Highlight);
			list.Add(markdownInlineDisplaySegment);
		}
		_backspaceRetainedTypingMarks.AddRange(list);
		_backspaceRetainedTypingMarkOffset = deletionStart;
	}

	private bool TryTakeBackspaceRetainedTypingMarks(int startOffset, out List<MarkdownInlineDisplaySegment> marks)
	{
		marks = null;
		if (_backspaceRetainedTypingMarks.Count == 0 || _backspaceRetainedTypingMarkOffset != startOffset)
		{
			return false;
		}
		marks = (from mark in _backspaceRetainedTypingMarks.Select(CloneTypingStyleMark)
			where mark != null
			select mark).ToList();
		ClearBackspaceRetainedTypingMarks();
		List<MarkdownInlineDisplaySegment> list = (from mark in GetActiveTypingMarkSegments(startOffset).Select(CloneTypingStyleMark)
			where mark != null
			select mark).ToList();
		if (list.Count > 0)
		{
			if (list.Any((MarkdownInlineDisplaySegment mark) => mark.Kind == MarkdownMarkKind.Highlight))
			{
				marks.RemoveAll((MarkdownInlineDisplaySegment mark) => mark.Kind == MarkdownMarkKind.Highlight);
			}
			HashSet<MarkdownMarkKind> retainedKinds = new HashSet<MarkdownMarkKind>(marks.Select((MarkdownInlineDisplaySegment mark) => mark.Kind));
			marks.AddRange(list.Where((MarkdownInlineDisplaySegment mark) => mark.Kind == MarkdownMarkKind.Highlight || !retainedKinds.Contains(mark.Kind)));
		}
		marks = (from @group in marks.GroupBy(MarkdownInlineSemanticMetadata.GetPersistenceIdentity)
			select @group.First()).ToList();
		return marks.Count > 0;
	}

	private static MarkdownInlineDisplaySegment CloneTypingStyleMark(MarkdownInlineDisplaySegment mark)
	{
		if (mark == null)
		{
			return null;
		}
		return new MarkdownInlineDisplaySegment
		{
			Kind = mark.Kind,
			Color = ((mark.Kind == MarkdownMarkKind.Highlight) ? mark.Color : null)
		};
	}

	private void ClearBackspaceRetainedTypingMarks()
	{
		_backspaceRetainedTypingMarks.Clear();
		_backspaceRetainedTypingMarkOffset = -1;
	}

	private void ClearBackspaceRetainedTypingMarksAwayFromOffset(int offset)
	{
		if (_backspaceRetainedTypingMarks.Count > 0 && _backspaceRetainedTypingMarkOffset != offset)
		{
			ClearBackspaceRetainedTypingMarks();
		}
	}

	private bool IsBackspaceRetainedTypingMarkActive(MarkdownMarkKind kind, int offset)
	{
		if (_backspaceRetainedTypingMarks.Count > 0 && _backspaceRetainedTypingMarkOffset == offset)
		{
			return _backspaceRetainedTypingMarks.Any((MarkdownInlineDisplaySegment mark) => mark.Kind == kind);
		}
		return false;
	}

	private bool TryGetBackspaceRetainedHighlightColor(int offset, out string highlightColor)
	{
		highlightColor = null;
		if (_backspaceRetainedTypingMarks.Count == 0 || _backspaceRetainedTypingMarkOffset != offset)
		{
			return false;
		}
		MarkdownInlineDisplaySegment markdownInlineDisplaySegment = _backspaceRetainedTypingMarks.LastOrDefault((MarkdownInlineDisplaySegment candidate) => candidate.Kind == MarkdownMarkKind.Highlight);
		if (markdownInlineDisplaySegment == null || string.IsNullOrWhiteSpace(markdownInlineDisplaySegment.Color))
		{
			return false;
		}
		highlightColor = HighlightColorMenuState.GetEditorColor(markdownInlineDisplaySegment.Color);
		return true;
	}

	private void SetActiveTypingMark(MarkdownMarkKind kind, int offset)
	{
		_activeTypingMarks.Add(kind);
		_activeTypingMarkOffsets[kind] = offset;
		if (kind == MarkdownMarkKind.Highlight)
		{
			_activeTypingHighlightOffset = offset;
		}
	}

	private bool ClearActiveTypingMark(MarkdownMarkKind kind)
	{
		bool flag = _activeTypingMarks.Remove(kind);
		flag |= _activeTypingMarkOffsets.Remove(kind);
		if (kind == MarkdownMarkKind.Highlight)
		{
			if (_activeTypingHighlightColor != null || _activeTypingHighlightOffset != -1)
			{
				flag = true;
			}
			_activeTypingHighlightColor = null;
			_activeTypingHighlightOffset = -1;
		}
		return flag;
	}

	private void ClearAllActiveTypingMarks()
	{
		_activeTypingMarks.Clear();
		_activeTypingMarkOffsets.Clear();
		_activeTypingHighlightColor = null;
		_activeTypingHighlightOffset = -1;
	}

	private void ClearActiveTypingMarksAwayFromOffset(int offset)
	{
		if (_activeTypingMarks.Count == 0)
		{
			return;
		}
		foreach (MarkdownMarkKind item in _activeTypingMarks.Where((MarkdownMarkKind kind) => !IsActiveTypingMarkAtOffset(kind, offset)).ToList())
		{
			ClearActiveTypingMark(item);
		}
	}

	private void MoveActiveTypingMarksToOffset(int offset)
	{
		if (_activeTypingMarks.Count == 0)
		{
			return;
		}
		foreach (MarkdownMarkKind item in _activeTypingMarks.ToList())
		{
			_activeTypingMarkOffsets[item] = offset;
		}
		if (_activeTypingMarks.Contains(MarkdownMarkKind.Highlight))
		{
			_activeTypingHighlightOffset = offset;
		}
	}

	private void MoveActiveTypingMarksFromOffset(int sourceOffset, int targetOffset)
	{
		if (sourceOffset == targetOffset || _activeTypingMarks.Count == 0)
		{
			return;
		}
		List<MarkdownMarkKind> list = _activeTypingMarks.Where((MarkdownMarkKind kind) => IsActiveTypingMarkAtOffset(kind, sourceOffset)).ToList();
		foreach (MarkdownMarkKind item in list)
		{
			_activeTypingMarkOffsets[item] = targetOffset;
		}
		if (list.Contains(MarkdownMarkKind.Highlight))
		{
			_activeTypingHighlightOffset = targetOffset;
		}
	}

	private bool IsActiveTypingMarkAtOffset(MarkdownMarkKind kind, int offset)
	{
		if (_activeTypingMarks.Contains(kind) && _activeTypingMarkOffsets.TryGetValue(kind, out var value))
		{
			return value == offset;
		}
		return false;
	}

	private List<MarkdownInlineDisplaySegment> GetInheritedTypingMarkSegmentsForInsertion(int startOffset, int removedLength)
	{
		List<MarkdownInlineDisplaySegment> list = (from @group in (from mark in GetSemanticMarkList()
				where mark != null && mark.SourceLength > 0 && !IsTypingMarkSuppressedAtOffset(mark.Kind, startOffset) && IsInlineTypingContextMark(mark) && IsInlineTypingContextActive(mark, startOffset, removedLength)
				select mark).GroupBy(MarkdownInlineSemanticMetadata.GetPersistenceIdentity)
			select new MarkdownInlineDisplaySegment
			{
				Kind = @group.First().Kind,
				Color = @group.First().Color
			}).ToList();
		if (TryGetHighlightContextTypingMarkAtOffset(startOffset, removedLength, out var highlightMark))
		{
			list.RemoveAll((MarkdownInlineDisplaySegment mark) => mark.Kind == MarkdownMarkKind.Highlight);
			list.Add(highlightMark);
		}
		return list;
	}

	private bool TryGetHighlightContextTypingMarkAtOffset(int startOffset, int removedLength, out MarkdownInlineDisplaySegment highlightMark)
	{
		highlightMark = null;
		if (removedLength != 0 || IsTypingMarkSuppressedAtOffset(MarkdownMarkKind.Highlight, startOffset))
		{
			return false;
		}
		if (!TryGetHighlightContextColorAtOffset(startOffset, out var highlightColor))
		{
			return false;
		}
		highlightMark = new MarkdownInlineDisplaySegment
		{
			Kind = MarkdownMarkKind.Highlight,
			Color = highlightColor
		};
		return true;
	}

	private bool TryGetHighlightContextColorAtOffset(int caretOffset, out string highlightColor)
	{
		highlightColor = null;
		if (IsTypingMarkSuppressedAtOffset(MarkdownMarkKind.Highlight, caretOffset))
		{
			return false;
		}
		if (TryGetSemanticTextIndexBeforeOffset(caretOffset, out var textIndex))
		{
			return TryGetHighlightColorAtTextIndex(textIndex, out highlightColor);
		}
		return TryGetHighlightColorAtBoundaryOffset(caretOffset, out highlightColor);
	}

	private bool TryGetSemanticTextIndexBeforeOffset(int caretOffset, out int textIndex)
	{
		textIndex = -1;
		string text = EditBox?.Text ?? Text ?? string.Empty;
		if (string.IsNullOrEmpty(text) || caretOffset <= 0)
		{
			return false;
		}
		for (int num = Math.Min(caretOffset, text.Length) - 1; num >= 0; num--)
		{
			char c = text[num];
			if (!IsInlineContextIgnoredDisplayCharacter(c))
			{
				if (char.IsWhiteSpace(c))
				{
					return false;
				}
				textIndex = num;
				return true;
			}
		}
		return false;
	}

	private bool TryGetHighlightColorAtTextIndex(int textIndex, out string highlightColor)
	{
		highlightColor = null;
		MarkdownInlineDisplaySegment markdownInlineDisplaySegment = (from candidate in GetHighlightContextMarks()
			where textIndex >= candidate.ContentPosition && textIndex < candidate.ContentPosition + candidate.ContentLength
			orderby candidate.ContentPosition descending, candidate.ContentLength
			select candidate).FirstOrDefault();
		if (markdownInlineDisplaySegment == null)
		{
			return false;
		}
		highlightColor = HighlightColorMenuState.GetEditorColor(markdownInlineDisplaySegment.Color);
		return true;
	}

	private bool TryGetHighlightColorAtBoundaryOffset(int caretOffset, out string highlightColor)
	{
		highlightColor = null;
		int safeOffset = Math.Max(0, caretOffset);
		MarkdownInlineDisplaySegment markdownInlineDisplaySegment = (from candidate in GetHighlightContextMarks()
			where safeOffset >= candidate.ContentPosition && safeOffset < candidate.ContentPosition + candidate.ContentLength
			orderby candidate.ContentPosition descending, candidate.ContentLength
			select candidate).FirstOrDefault();
		if (markdownInlineDisplaySegment == null)
		{
			markdownInlineDisplaySegment = (from candidate in GetHighlightContextMarks()
				where safeOffset > candidate.ContentPosition && safeOffset <= candidate.ContentPosition + candidate.ContentLength
				orderby candidate.ContentPosition descending, candidate.ContentLength
				select candidate).FirstOrDefault();
		}
		if (markdownInlineDisplaySegment == null)
		{
			return false;
		}
		highlightColor = HighlightColorMenuState.GetEditorColor(markdownInlineDisplaySegment.Color);
		return true;
	}

	private IEnumerable<MarkdownInlineDisplaySegment> GetHighlightContextMarks()
	{
		return from mark in GetSemanticMarkList()
			where mark != null && mark.Kind == MarkdownMarkKind.Highlight && !string.IsNullOrWhiteSpace(mark.Color) && mark.ContentLength > 0
			select mark;
	}

	private static bool IsInlineContextIgnoredDisplayCharacter(char ch)
	{
		if (ch != '\u00a0' && ch != '\u2065')
		{
			return "\u200c\u200d\u2060".IndexOf(ch) >= 0;
		}
		return true;
	}

	private static bool IsInlineTypingContextMark(MarkdownInlineDisplaySegment mark)
	{
		if (mark != null && mark.Kind != MarkdownMarkKind.Link && !string.IsNullOrEmpty(MarkdownInlineSemanticMetadata.GetOpeningDelimiter(mark)))
		{
			return !string.IsNullOrEmpty(MarkdownInlineSemanticMetadata.GetClosingDelimiter(mark));
		}
		return false;
	}

	private bool IsInlineTypingContextActive(MarkdownInlineDisplaySegment mark, int startOffset, int removedLength)
	{
		if (mark == null || mark.SourceLength <= 0)
		{
			return false;
		}
		int sourcePosition = mark.SourcePosition;
		int num = mark.SourcePosition + mark.SourceLength;
		if (removedLength > 0)
		{
			if (startOffset < num)
			{
				return startOffset + removedLength > sourcePosition;
			}
			return false;
		}
		if (startOffset <= sourcePosition)
		{
			if (startOffset == sourcePosition)
			{
				return !HasPlainTextBeforeInlineMark(mark.Kind, startOffset);
			}
			return false;
		}
		if (startOffset < num)
		{
			return true;
		}
		return startOffset == num;
	}

	private bool HasPlainTextBeforeInlineMark(MarkdownMarkKind kind, int caretOffset)
	{
		string text = EditBox?.Text ?? Text ?? string.Empty;
		if (string.IsNullOrEmpty(text) || caretOffset <= 0)
		{
			return false;
		}
		int index;
		for (index = Math.Min(caretOffset, text.Length) - 1; index >= 0; index--)
		{
			char c = text[index];
			if (!IsInlineContextIgnoredDisplayCharacter(c))
			{
				if (char.IsWhiteSpace(c))
				{
					return false;
				}
				return !GetSemanticMarkList().Any((MarkdownInlineDisplaySegment mark) => mark != null && mark.Kind == kind && mark.SourceLength > 0 && index >= mark.SourcePosition && index < mark.SourcePosition + mark.SourceLength);
			}
		}
		return false;
	}

	private void AdjustInlineSemanticMarksForDefaultTextMutation(int startOffset, int removedLength, int insertedLength)
	{
		_needsTypedSemanticMarkRedraw |= AdjustSemanticMarkListForReplace(startOffset, removedLength, insertedLength);
		_needsTypedSemanticMarkRedraw |= AdjustTypedSemanticMarksForReplace(startOffset, removedLength, insertedLength);
	}

	private void AdjustInlineSemanticMarksForImeCompositionMutation(DocumentChangeEventArgs change)
	{
		if (change.RemovalLength == 0 || change.InsertionLength == 0)
		{
			AdjustInlineSemanticMarksForDefaultTextMutation(change.Offset, change.RemovalLength, change.InsertionLength);
			return;
		}
		_needsTypedSemanticMarkRedraw |= AdjustImeCompositionMarksForReplace(SemanticMarkList, change.Offset, change.RemovalLength, change.InsertionLength);
		_needsTypedSemanticMarkRedraw |= AdjustImeCompositionMarksForReplace(_typedSemanticMarks, change.Offset, change.RemovalLength, change.InsertionLength);
	}

	private static bool AdjustImeCompositionMarksForReplace(IList<MarkdownInlineDisplaySegment> marks, int startOffset, int removedLength, int insertedLength)
	{
		bool flag = false;
		int num = startOffset + removedLength;
		int num2 = insertedLength - removedLength;
		for (int num3 = marks.Count - 1; num3 >= 0; num3--)
		{
			MarkdownInlineDisplaySegment markdownInlineDisplaySegment = marks[num3];
			if (markdownInlineDisplaySegment == null)
			{
				marks.RemoveAt(num3);
				flag = true;
			}
			else
			{
				int sourcePosition = markdownInlineDisplaySegment.SourcePosition;
				int num4 = sourcePosition + markdownInlineDisplaySegment.SourceLength;
				if (num4 > startOffset)
				{
					if (sourcePosition >= num)
					{
						markdownInlineDisplaySegment.SourcePosition += num2;
					}
					else if (sourcePosition <= startOffset && num4 >= num)
					{
						markdownInlineDisplaySegment.SourceLength += num2;
					}
					else if (sourcePosition < startOffset)
					{
						markdownInlineDisplaySegment.SourceLength = startOffset - sourcePosition + insertedLength;
					}
					else if (num4 > num)
					{
						markdownInlineDisplaySegment.SourcePosition = startOffset + insertedLength;
						markdownInlineDisplaySegment.SourceLength = num4 - num;
					}
					else
					{
						markdownInlineDisplaySegment.SourcePosition = startOffset;
						markdownInlineDisplaySegment.SourceLength = insertedLength;
					}
					flag |= markdownInlineDisplaySegment.SourcePosition != sourcePosition || markdownInlineDisplaySegment.SourceLength != num4 - sourcePosition;
				}
			}
		}
		return flag;
	}

	private string GetDefaultEnterText()
	{
		if (EditBox?.Document == null || EditBox.TextArea?.Caret == null)
		{
			return Environment.NewLine;
		}
		return TextUtilities.GetNewLineFromDocument(EditBox.Document, EditBox.TextArea.Caret.Line);
	}

	private string GetDefaultEnterTextForSemanticMutation(string lineBreak)
	{
		if (string.IsNullOrEmpty(lineBreak) || EditBox?.Document == null || EditBox.SelectionLength > 0)
		{
			return lineBreak ?? string.Empty;
		}
		TextDocument document = EditBox.Document;
		int num = Math.Max(0, Math.Min(EditBox.CaretOffset, document.TextLength));
		DocumentLine lineByOffset = document.GetLineByOffset(num);
		if (lineByOffset == null)
		{
			return lineBreak;
		}
		ISegment whitespaceAfter = TextUtilities.GetWhitespaceAfter(document, lineByOffset.Offset);
		if (whitespaceAfter == null || whitespaceAfter.Length == 0 || num < whitespaceAfter.Offset + whitespaceAfter.Length)
		{
			return lineBreak;
		}
		return lineBreak + document.GetText(whitespaceAfter);
	}

	private bool TrySplitSemanticLinkAtCaretForEnter(string enterText)
	{
		if (EditBox?.Document == null || EditBox.SelectionLength > 0 || string.IsNullOrEmpty(enterText))
		{
			return false;
		}
		int caretOffset = Math.Max(0, Math.Min(EditBox.CaretOffset, EditBox.Document.TextLength));
		if (!HasSplittableDisplayOnlyLinkAtOffset(caretOffset))
		{
			return false;
		}
		int insertedLength = enterText.Length;
		_needsTypedSemanticMarkRedraw = true;
		ApplyDocumentMutationsAndNotify(new DocumentMutation[1]
		{
			new DocumentMutation
			{
				StartOffset = caretOffset,
				RemoveLength = 0,
				InsertText = enterText
			}
		}, caretOffset + insertedLength, delegate
		{
			if (SplitDisplayOnlyLinkMarksAfterInsertion(_typedSemanticMarks, caretOffset, insertedLength) + SplitDisplayOnlyLinkMarksAfterInsertion(SemanticMarkList, caretOffset, insertedLength) > 0)
			{
				_needsTypedSemanticMarkRedraw = true;
			}
		});
		return true;
	}

	private bool HasSplittableDisplayOnlyLinkAtOffset(int offset)
	{
		if (!HasSplittableDisplayOnlyLinkAtOffset(_typedSemanticMarks, offset))
		{
			return HasSplittableDisplayOnlyLinkAtOffset(SemanticMarkList, offset);
		}
		return true;
	}

	private static bool HasSplittableDisplayOnlyLinkAtOffset(IEnumerable<MarkdownInlineDisplaySegment> marks, int offset)
	{
		return marks?.Any((MarkdownInlineDisplaySegment mark) => IsSplittableDisplayOnlyLinkAtOffset(mark, offset)) ?? false;
	}

	private static bool IsSplittableDisplayOnlyLinkAtOffset(MarkdownInlineDisplaySegment mark, int offset)
	{
		if (!IsDisplayOnlySemanticLink(mark) || string.IsNullOrWhiteSpace(mark.Url) || mark.SourceLength <= 1)
		{
			return false;
		}
		int sourcePosition = mark.SourcePosition;
		int num = mark.SourcePosition + mark.SourceLength;
		if (sourcePosition < offset)
		{
			return offset < num;
		}
		return false;
	}

	private int SplitDisplayOnlyLinkMarksAfterInsertion(IList<MarkdownInlineDisplaySegment> marks, int insertionOffset, int insertedLength)
	{
		if (marks == null || marks.Count == 0 || insertedLength <= 0)
		{
			return 0;
		}
		string text = EditBox?.Text ?? string.Empty;
		int num = insertionOffset + insertedLength;
		List<MarkdownInlineDisplaySegment> list = new List<MarkdownInlineDisplaySegment>();
		int num2 = 0;
		for (int num3 = marks.Count - 1; num3 >= 0; num3--)
		{
			MarkdownInlineDisplaySegment markdownInlineDisplaySegment = marks[num3];
			if (IsDisplayOnlySemanticLink(markdownInlineDisplaySegment) && !string.IsNullOrWhiteSpace(markdownInlineDisplaySegment.Url))
			{
				int sourcePosition = markdownInlineDisplaySegment.SourcePosition;
				int num4 = markdownInlineDisplaySegment.SourcePosition + markdownInlineDisplaySegment.SourceLength;
				if (sourcePosition < insertionOffset && num < num4)
				{
					int num5 = insertionOffset - sourcePosition;
					int num6 = num4 - num;
					if (num5 > 0 && num6 > 0)
					{
						markdownInlineDisplaySegment.SourceLength = num5;
						markdownInlineDisplaySegment.OpeningDelimiterLength = 0;
						markdownInlineDisplaySegment.ClosingDelimiterLength = 0;
						markdownInlineDisplaySegment.UrlPosition = 0;
						markdownInlineDisplaySegment.UrlLength = 0;
						markdownInlineDisplaySegment.LinkText = GetSafeTextSlice(text, sourcePosition, num5);
						list.Add(new MarkdownInlineDisplaySegment
						{
							Kind = MarkdownMarkKind.Link,
							SourcePosition = num,
							SourceLength = num6,
							OpeningDelimiterLength = 0,
							ClosingDelimiterLength = 0,
							LinkText = GetSafeTextSlice(text, num, num6),
							Url = markdownInlineDisplaySegment.Url,
							UrlPosition = 0,
							UrlLength = 0
						});
						num2++;
					}
				}
			}
		}
		if (list.Count > 0)
		{
			foreach (MarkdownInlineDisplaySegment item in list)
			{
				marks.Add(item);
			}
		}
		return num2;
	}

	private static string GetSafeTextSlice(string text, int start, int length)
	{
		if (string.IsNullOrEmpty(text) || length <= 0)
		{
			return string.Empty;
		}
		int num = Math.Max(0, Math.Min(start, text.Length));
		int num2 = Math.Max(0, Math.Min(length, text.Length - num));
		if (num2 <= 0)
		{
			return string.Empty;
		}
		return text.Substring(num, num2);
	}

	private void InsertText(string text, IReadOnlyCollection<MarkdownInlineDisplaySegment> typingMarks = null)
	{
		if (!string.IsNullOrEmpty(text))
		{
			int startOffset = EditBox.CaretOffset;
			int selectionLength = EditBox.SelectionLength;
			if (selectionLength > 0)
			{
				startOffset = EditBox.SelectionStart;
			}
			InsertTextAt(startOffset, selectionLength, text, typingMarks);
		}
	}

	private void InsertTextAt(int startOffset, int removedLength, string text, IReadOnlyCollection<MarkdownInlineDisplaySegment> typingMarks = null)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		int num = EditBox.Text?.Length ?? 0;
		bool flag = text.IndexOf('\r') >= 0 || text.IndexOf('\n') >= 0;
		int offset = Math.Max(0, Math.Min(startOffset, num));
		if (removedLength == 0 && !_inImeProcess)
		{
			offset = GetInsertionOffsetOutsideLinkDisplayBoundary(offset);
		}
		ClearTypingMarkSuppressionsAwayFromOffset(offset);
		int removeLength = Math.Max(0, Math.Min(removedLength, num - offset));
		List<MarkdownInlineDisplaySegment> marksToApply = (from @group in typingMarks?.Where((MarkdownInlineDisplaySegment mark) => mark != null).GroupBy(MarkdownInlineSemanticMetadata.GetPersistenceIdentity)
			select @group.First()).ToList() ?? new List<MarkdownInlineDisplaySegment>();
		if (!CanApplyInlineSemanticTypingAtOffset(offset))
		{
			marksToApply.Clear();
		}
		if (flag)
		{
			marksToApply.Clear();
		}
		if (!flag && marksToApply.Any((MarkdownInlineDisplaySegment mark) => mark != null && IsActiveTypingMarkAtOffset(mark.Kind, offset)))
		{
			MoveActiveTypingMarksToOffset(offset + text.Length);
		}
		ApplyDocumentMutationsAndNotify(new DocumentMutation[1]
		{
			new DocumentMutation
			{
				StartOffset = offset,
				RemoveLength = removeLength,
				InsertText = text
			}
		}, offset + text.Length, delegate
		{
			if (marksToApply.Count > 0 && text.Length > 0)
			{
				ApplyTypingMarksToInsertedText(offset, text.Length, marksToApply);
			}
		});
		if (!_inImeProcess && ShouldTryConvertCompletedRawMarkdownLinkAfterInsertedText(text))
		{
			TryConvertCompletedRawMarkdownLinkAroundCaret();
		}
		if (flag)
		{
			ClearActiveInlineTypingAfterLineBreak();
		}
		ClearTypingMarkSuppressionsAtOffset(offset);
		RecordLastInsertRangeFromEditor();
	}

	private void ClearTypingMarkSuppressionsAwayFromOffset(int offset)
	{
		if (_suppressedTypingMarks.Count == 0)
		{
			return;
		}
		foreach (MarkdownMarkKind item in (from item in _suppressedTypingMarks
			where item.Value != offset
			select item.Key).ToList())
		{
			_suppressedTypingMarks.Remove(item);
		}
	}

	private void ClearTypingMarkSuppressionsAtOffset(int offset)
	{
		if (_suppressedTypingMarks.Count == 0)
		{
			return;
		}
		foreach (MarkdownMarkKind item in (from item in _suppressedTypingMarks
			where item.Value == offset
			select item.Key).ToList())
		{
			_suppressedTypingMarks.Remove(item);
		}
	}

	private bool TryNormalizeCaretOutsideLinkDisplayBoundary()
	{
		if (_inImeProcess || EditBox?.Document == null || EditBox.SelectionLength > 0)
		{
			return false;
		}
		int num = Math.Max(0, Math.Min(EditBox.CaretOffset, EditBox.Document.TextLength));
		int insertionOffsetOutsideLinkDisplayBoundary = GetInsertionOffsetOutsideLinkDisplayBoundary(num);
		if (insertionOffsetOutsideLinkDisplayBoundary == num)
		{
			return false;
		}
		EditBox.CaretOffset = insertionOffsetOutsideLinkDisplayBoundary;
		return true;
	}

	private bool CanApplyForwardInlineShortcutDuringActiveTyping(string inputText)
	{
		if (_activeTypingMarks.Count == 0)
		{
			return true;
		}
		if ((string.Equals(inputText, "*", StringComparison.Ordinal) || string.Equals(inputText, " ", StringComparison.Ordinal)) && _activeTypingMarks.Count == 1)
		{
			return _activeTypingMarks.Contains(MarkdownMarkKind.Highlight);
		}
		return false;
	}

	private int GetInsertionOffsetOutsideLinkDisplayBoundary(int offset)
	{
		string text = EditBox?.Text ?? string.Empty;
		int length = text.Length;
		int num = Math.Max(0, Math.Min(offset, length));
		if (!CanApplyInlineSemanticTypingAtOffset(num))
		{
			return num;
		}
		foreach (MarkdownInlineDisplaySegment semanticMark in GetSemanticMarkList())
		{
			if (semanticMark == null || semanticMark.Kind != MarkdownMarkKind.Link || semanticMark.SourceLength <= 0)
			{
				continue;
			}
			int val = Math.Max(0, Math.Min(semanticMark.SourcePosition, length));
			int num2 = Math.Max(val, Math.Min(semanticMark.SourcePosition + semanticMark.SourceLength, length));
			int num3 = Math.Max(val, Math.Min(semanticMark.ContentPosition, num2));
			int num4 = Math.Max(num3, Math.Min(semanticMark.ContentPosition + Math.Max(0, semanticMark.ContentLength), num2));
			if (num4 <= num3)
			{
				continue;
			}
			if (semanticMark.UrlLength > 0)
			{
				int num5 = Math.Max(val, Math.Min(semanticMark.UrlPosition, num2));
				int num6 = Math.Max(num5, Math.Min(semanticMark.UrlPosition + semanticMark.UrlLength, num2));
				if (num >= num5 && num <= num6)
				{
					return num;
				}
			}
			if (num == num4 || (num > num4 && num < num2))
			{
				return num2;
			}
		}
		foreach (OffsetRange linkDisplayBoundaryRange in GetLinkDisplayBoundaryRanges())
		{
			if (linkDisplayBoundaryRange != null && linkDisplayBoundaryRange.End > linkDisplayBoundaryRange.Start && (num == linkDisplayBoundaryRange.Start || (num > linkDisplayBoundaryRange.Start && num < linkDisplayBoundaryRange.End)))
			{
				return linkDisplayBoundaryRange.End;
			}
		}
		foreach (KeyValuePair<int, LinkInfo> item in LinkNameDict)
		{
			string text2 = item.Value?.Link ?? string.Empty;
			if (text2.Length != 0)
			{
				int num7 = item.Key + text2.Length;
				if (num == num7 && TryGetRawMarkdownLinkEndOffset(item.Key, num7, out var rawLinkEnd) && rawLinkEnd > num)
				{
					return rawLinkEnd;
				}
			}
		}
		foreach (PersistedMarkdownLinkMatch item2 in EnumeratePersistedMarkdownLinks(text))
		{
			if (item2.Length <= 0 || IsLinkMatchInsideMarkdownImageSyntax(text, item2.Index))
			{
				continue;
			}
			int num8 = Math.Max(item2.Index, Math.Min(item2.Index + item2.Length, length));
			int num9 = Math.Max(item2.TextIndex, Math.Min(item2.TextIndex + item2.TextLength, num8));
			if (num9 <= item2.TextIndex)
			{
				continue;
			}
			if (item2.UrlLength > 0)
			{
				int num10 = Math.Max(item2.Index, Math.Min(item2.UrlIndex, num8));
				int num11 = Math.Max(num10, Math.Min(item2.UrlIndex + item2.UrlLength, num8));
				if (num >= num10 && num <= num11)
				{
					return num;
				}
			}
			if (num == num9 || (num > num9 && num < num8))
			{
				return num8;
			}
		}
		return num;
	}

	private bool TryGetRawMarkdownLinkEndOffset(int contentStart, int contentEnd, out int rawLinkEnd)
	{
		rawLinkEnd = 0;
		string text = EditBox?.Text ?? string.Empty;
		int num = Math.Max(0, Math.Min(contentStart, text.Length));
		int num2 = Math.Max(num, Math.Min(contentEnd, text.Length));
		if (num <= 0 || num2 + 2 > text.Length || text[num - 1] != '[' || (num > 1 && text[num - 2] == '!') || text[num2] != ']' || text[num2 + 1] != '(')
		{
			return false;
		}
		int num3 = FindRawMarkdownLinkUrlEnd(text, num2 + 2);
		if (num3 <= num2 + 2)
		{
			return false;
		}
		rawLinkEnd = num3 + 1;
		return true;
	}

	private static int FindRawMarkdownLinkUrlEnd(string text, int urlStart)
	{
		int num = 0;
		for (int i = Math.Max(0, urlStart); i < (text?.Length ?? 0); i++)
		{
			if (text[i] == '\\')
			{
				i++;
			}
			else if (text[i] == '(')
			{
				num++;
			}
			else if (text[i] == ')')
			{
				if (num == 0)
				{
					return i;
				}
				num--;
			}
		}
		return -1;
	}

	private IEnumerable<OffsetRange> GetLinkDisplayBoundaryRanges()
	{
		string text = EditBox?.Text ?? string.Empty;
		int textLength = text.Length;
		HashSet<string> emitted = new HashSet<string>();
		foreach (MarkdownInlineDisplaySegment semanticMark in GetSemanticMarkList())
		{
			if (semanticMark == null || semanticMark.Kind != MarkdownMarkKind.Link || semanticMark.SourceLength <= 0)
			{
				continue;
			}
			int val = Math.Max(0, Math.Min(semanticMark.SourcePosition, textLength));
			int num = Math.Max(val, Math.Min(semanticMark.SourcePosition + semanticMark.SourceLength, textLength));
			int num2 = Math.Max(val, Math.Min(semanticMark.ContentPosition, num));
			int num3 = Math.Max(num2, Math.Min(semanticMark.ContentPosition + Math.Max(0, semanticMark.ContentLength), num));
			if (num3 > num2 && num > num3)
			{
				string item = num3 + ":" + num;
				if (emitted.Add(item))
				{
					yield return new OffsetRange
					{
						Start = num3,
						End = num
					};
				}
			}
		}
		foreach (KeyValuePair<int, LinkInfo> item3 in LinkNameDict)
		{
			string text2 = item3.Value?.Link ?? string.Empty;
			string text3 = item3.Value?.Url ?? string.Empty;
			if (text2.Length == 0 || text3.Length == 0)
			{
				continue;
			}
			string text4 = "[" + text2 + "](" + text3 + ")";
			int num4 = item3.Key - 1;
			if (num4 >= 0 && num4 + text4.Length <= textLength && string.Equals(text.Substring(num4, text4.Length), text4, StringComparison.Ordinal))
			{
				int num5 = item3.Key + text2.Length;
				int num6 = num4 + text4.Length;
				string item2 = num5 + ":" + num6;
				if (num6 > num5 && emitted.Add(item2))
				{
					yield return new OffsetRange
					{
						Start = num5,
						End = num6
					};
				}
			}
		}
	}

	private bool CanApplyInlineSemanticTypingAtOffset(int offset)
	{
		if (EditBox?.Document == null)
		{
			return true;
		}
		int offset2 = Math.Max(0, Math.Min(offset, EditBox.Document.TextLength));
		if (TryGetSemanticBlockAtOffset(offset2, out var block))
		{
			return CanApplyInlineSemanticTypingForBlock(block);
		}
		return true;
	}

	private static bool CanApplyInlineSemanticTypingForBlock(MarkdownBlockNode block)
	{
		if (block == null)
		{
			return true;
		}
		return block.Kind != MarkdownBlockKind.CodeBlock;
	}

	private bool CanApplyInlineSemanticAtCurrentSelection()
	{
		if (EditBox?.Document == null)
		{
			return true;
		}
		if (EditBox.SelectionLength <= 0)
		{
			return CanApplyInlineSemanticTypingAtOffset(EditBox.CaretOffset);
		}
		int num = Math.Max(0, Math.Min(EditBox.SelectionStart, EditBox.Document.TextLength));
		int num2 = Math.Max(num, Math.Min(EditBox.SelectionStart + EditBox.SelectionLength, EditBox.Document.TextLength));
		DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(num);
		int val = ((num2 > num) ? (num2 - 1) : num2);
		DocumentLine lineByOffset2 = EditBox.Document.GetLineByOffset(Math.Max(0, Math.Min(val, EditBox.Document.TextLength)));
		if (lineByOffset == null || lineByOffset2 == null)
		{
			return true;
		}
		for (int i = lineByOffset.LineNumber; i <= lineByOffset2.LineNumber; i++)
		{
			if (TryGetSemanticBlockAtLine(i - 1, out var block) && !CanApplyInlineSemanticTypingForBlock(block))
			{
				return false;
			}
		}
		return true;
	}

	private void ReplaceEditorText(int startOffset, int removedLength, string insertedText, int? explicitCaretOffset = null, Action<int, int> postDocumentUpdate = null, bool deferTextChangedNotification = false, bool convertCompletedMarkdownLink = true)
	{
		insertedText = insertedText ?? string.Empty;
		int num = EditBox.Text?.Length ?? 0;
		int num2 = Math.Max(0, Math.Min(startOffset, num));
		int removeLength = Math.Max(0, Math.Min(removedLength, num - num2));
		string persistedText = GetPersistedText();
		DocumentMutation[] array = new DocumentMutation[1]
		{
			new DocumentMutation
			{
				StartOffset = num2,
				RemoveLength = removeLength,
				InsertText = insertedText
			}
		};
		NormalizeTextInsertionsBeforeCodeBlockAttachments(array);
		PreserveSemanticBlocksAcrossMutations(array);
		bool suppressed = TryBeginPlaceholderMutationSuppression(array);
		bool flag = ClearHeadingFoldingsBeforeDocumentMutationsIfNeeded(array);
		_suppressEditBoxTextChanged = true;
		try
		{
			using (EditBox.Document.RunUpdate())
			{
				ApplyDocumentMutations(array);
				postDocumentUpdate?.Invoke(num2, array[0].InsertText.Length);
				if (postDocumentUpdate != null && _semanticDocument != null)
				{
					_seedSemanticDocumentForRefresh = CloneSemanticDocument(_semanticDocument);
				}
			}
			EditBox.SelectionLength = 0;
			EditBox.CaretOffset = explicitCaretOffset ?? Math.Max(0, Math.Min(num2 + insertedText.Length, EditBox.Text.Length));
		}
		finally
		{
			_suppressEditBoxTextChanged = false;
			EndPlaceholderOpacitySuppression(suppressed);
		}
		bool deferBindingTextSyncToSemanticNotification = _deferBindingTextSyncToSemanticNotification;
		_deferBindingTextSyncToSemanticNotification = true;
		try
		{
			OnEditBoxTextChanged(captureHistory: false);
		}
		finally
		{
			_deferBindingTextSyncToSemanticNotification = deferBindingTextSyncToSemanticNotification;
		}
		if (flag)
		{
			UpdateHeadingFoldings();
		}
		if (convertCompletedMarkdownLink && ShouldTryConvertCompletedRawMarkdownLinkAfterInsertedText(insertedText))
		{
			TryConvertCompletedRawMarkdownLinkAroundCaret(notifyTextChanged: false);
		}
		NotifySemanticTextChangedIfNeeded(persistedText, deferTextChangedNotification);
	}

	private void ApplyDocumentMutationsAndNotify(IEnumerable<DocumentMutation> mutations, int? explicitCaretOffset = null, Action postDocumentUpdate = null, bool preserveSemanticBlocks = true, bool refreshEditingState = true, bool notifyTextChanged = true)
	{
		if (mutations == null)
		{
			return;
		}
		List<DocumentMutation> list = mutations.Where((DocumentMutation mutation) => mutation != null).ToList();
		if (list.Count == 0)
		{
			return;
		}
		NormalizeTextInsertionsBeforeCodeBlockAttachments(list);
		string persistedText = GetPersistedText();
		if (preserveSemanticBlocks)
		{
			PreserveSemanticBlocksAcrossMutations(list);
		}
		bool suppressed = TryBeginPlaceholderMutationSuppression(list);
		bool flag = ClearHeadingFoldingsBeforeDocumentMutationsIfNeeded(list);
		_suppressEditBoxTextChanged = true;
		try
		{
			using (EditBox.Document.RunUpdate())
			{
				ApplyDocumentMutations(list);
			}
			postDocumentUpdate?.Invoke();
			SyncSemanticCodeBlocksFromEditorText();
			int desiredCaretOffset = explicitCaretOffset ?? EditBox.CaretOffset;
			int adjustedCaretOffset;
			bool flag2 = TrySyncParagraphIndentMarkersWithSemanticState(desiredCaretOffset, out adjustedCaretOffset);
			if (explicitCaretOffset.HasValue || flag2)
			{
				EditBox.SelectionLength = 0;
				EditBox.CaretOffset = Math.Max(0, Math.Min(adjustedCaretOffset, EditBox.Text.Length));
				NormalizeCaretIntoSemanticContentIfNeeded();
			}
		}
		finally
		{
			_suppressEditBoxTextChanged = false;
			EndPlaceholderOpacitySuppression(suppressed);
		}
		if (refreshEditingState)
		{
			bool deferBindingTextSyncToSemanticNotification = _deferBindingTextSyncToSemanticNotification;
			_deferBindingTextSyncToSemanticNotification = true;
			try
			{
				OnEditBoxTextChanged(captureHistory: false, notifyTextChanged);
			}
			finally
			{
				_deferBindingTextSyncToSemanticNotification = deferBindingTextSyncToSemanticNotification;
			}
			NormalizeCaretIntoSemanticContentIfNeeded();
		}
		else
		{
			RefreshEditingDerivedMetadataWithoutMarkdownParsing();
		}
		if (flag)
		{
			UpdateHeadingFoldings();
		}
		RefreshOpenMenuContexts();
		RequestImmediateRedrawForDocumentMutations(list);
		if (notifyTextChanged)
		{
			NotifySemanticTextChangedIfNeeded(persistedText);
		}
	}

	private void RequestImmediateRedrawForDocumentMutations(IList<DocumentMutation> mutations)
	{
		if (EditBox?.Document == null || mutations == null || mutations.Count == 0)
		{
			return;
		}
		int num = 0;
		foreach (DocumentMutation item in from mutation in mutations
			where mutation != null
			orderby mutation.StartOffset
			select mutation)
		{
			int num2 = item.InsertText?.Length ?? 0;
			int num3 = item.StartOffset + num;
			RequestImmediateRedrawForDocumentRangeWithPreviousLine(num3, num3 + num2);
			num += num2 - Math.Max(0, item.RemoveLength);
		}
	}

	private void RemoveSemanticLineAndShiftFollowing(int lineIndex)
	{
		if (lineIndex >= 0)
		{
			if (_semanticDocument != null)
			{
				_semanticDocument = CloneSemanticDocument(_semanticDocument);
				RemoveSemanticLineAndShiftFollowing(_semanticDocument, lineIndex);
				_semanticBlocksByLine = BuildSemanticBlockIndex(_semanticDocument);
			}
			if (_seedSemanticDocumentForRefresh != null)
			{
				_seedSemanticDocumentForRefresh = CloneSemanticDocument(_seedSemanticDocumentForRefresh);
				RemoveSemanticLineAndShiftFollowing(_seedSemanticDocumentForRefresh, lineIndex);
			}
		}
	}

	private static void RemoveSemanticLineAndShiftFollowing(MarkdownSemanticDocument document, int lineIndex)
	{
		MarkdownSemanticDocumentMutations.RemoveLineAndShiftFollowing(document, lineIndex);
	}

	private void RemoveSemanticRangeAndShiftFollowing(int startLineIndex, int endLineIndex, int removedLineCount)
	{
		if (startLineIndex >= 0 && endLineIndex >= startLineIndex && removedLineCount > 0)
		{
			if (_semanticDocument != null)
			{
				_semanticDocument = CloneSemanticDocument(_semanticDocument);
				RemoveSemanticRangeAndShiftFollowing(_semanticDocument, startLineIndex, endLineIndex, removedLineCount);
				_semanticBlocksByLine = BuildSemanticBlockIndex(_semanticDocument);
			}
			if (_seedSemanticDocumentForRefresh != null)
			{
				_seedSemanticDocumentForRefresh = CloneSemanticDocument(_seedSemanticDocumentForRefresh);
				RemoveSemanticRangeAndShiftFollowing(_seedSemanticDocumentForRefresh, startLineIndex, endLineIndex, removedLineCount);
			}
		}
	}

	private void PreserveSemanticBlocksAcrossMutations(IReadOnlyList<DocumentMutation> mutations)
	{
		if (mutations != null && mutations.Count != 0 && ((_semanticDocument?.Blocks?.Count).GetValueOrDefault() != 0 || (_seedSemanticDocumentForRefresh?.Blocks?.Count).GetValueOrDefault() != 0))
		{
			string originalText = EditBox?.Text ?? string.Empty;
			if (_semanticDocument != null)
			{
				_semanticDocument = CloneSemanticDocument(_semanticDocument);
				ShiftSemanticBlocksForMutations(_semanticDocument, originalText, mutations);
			}
			if (_seedSemanticDocumentForRefresh != null)
			{
				_seedSemanticDocumentForRefresh = CloneSemanticDocument(_seedSemanticDocumentForRefresh);
				ShiftSemanticBlocksForMutations(_seedSemanticDocumentForRefresh, originalText, mutations);
			}
			if (_semanticDocument != null)
			{
				_semanticBlocksByLine = BuildSemanticBlockIndex(_semanticDocument);
			}
		}
	}

	private void SyncSemanticCodeBlocksFromEditorText()
	{
		string displayText = EditBox?.Text ?? string.Empty;
		if (_semanticDocument != null)
		{
			SyncCodeBlocksFromDisplayText(_semanticDocument, displayText);
			_semanticBlocksByLine = BuildSemanticBlockIndex(_semanticDocument);
		}
		if (_seedSemanticDocumentForRefresh != null)
		{
			SyncCodeBlocksFromDisplayText(_seedSemanticDocumentForRefresh, displayText);
		}
	}

	private void PreserveSemanticBlocksForDefaultTextMutation(int startOffset, int removeLength, string insertText)
	{
		int valueOrDefault = (EditBox?.Text?.Length).GetValueOrDefault();
		int num = Math.Max(0, Math.Min(startOffset, valueOrDefault));
		int removeLength2 = Math.Max(0, Math.Min(removeLength, valueOrDefault - num));
		DocumentMutation[] mutations = new DocumentMutation[1]
		{
			new DocumentMutation
			{
				StartOffset = num,
				RemoveLength = removeLength2,
				InsertText = (insertText ?? string.Empty)
			}
		};
		ClearHeadingFoldingsBeforeDocumentMutationsIfNeeded(mutations);
		PreserveSemanticBlocksAcrossMutations(mutations);
	}

	private bool ClearHeadingFoldingsBeforeDocumentMutationsIfNeeded(IEnumerable<DocumentMutation> mutations)
	{
		if (_headingFoldingManager == null || EditBox?.Document == null || mutations == null)
		{
			return false;
		}
		List<FoldingSection> list = _headingFoldingManager.AllFoldings.Where((FoldingSection folding) => folding?.IsFolded ?? false).ToList();
		if (list.Count == 0)
		{
			return false;
		}
		TextDocument document = EditBox.Document;
		int textLength = document.TextLength;
		foreach (DocumentMutation item in mutations.Where((DocumentMutation mutation) => mutation != null))
		{
			int num = Math.Max(0, Math.Min(item.StartOffset, textLength));
			int num2 = Math.Max(num, Math.Min(num + Math.Max(0, item.RemoveLength), textLength));
			bool flag = num2 == num && !string.IsNullOrEmpty(item.InsertText);
			foreach (FoldingSection item2 in list)
			{
				int num3 = Math.Max(0, Math.Min(item2.StartOffset, textLength));
				int num4 = Math.Max(num3, Math.Min(item2.EndOffset, textLength));
				int val = ((num3 > 0) ? (num3 - 1) : num3);
				int num5 = document.GetLineByOffset(Math.Max(0, Math.Min(val, textLength)))?.Offset ?? num3;
				int num6 = num4;
				bool num7;
				if (!flag)
				{
					if (num >= num6)
					{
						continue;
					}
					num7 = num2 > num5;
				}
				else
				{
					if (num < num5)
					{
						continue;
					}
					num7 = num <= num6;
				}
				if (num7)
				{
					ClearHeadingFoldingsBeforeTextReset();
					return true;
				}
			}
		}
		return false;
	}

	private bool TryRemoveSelectedSemanticRange()
	{
		if (EditBox?.Document == null || EditBox.SelectionLength <= 0)
		{
			return false;
		}
		int selectionStart = EditBox.SelectionStart;
		int selectionLength = EditBox.SelectionLength;
		int selectionEnd = selectionStart + selectionLength;
		if (selectionLength <= 0)
		{
			return false;
		}
		int? num = null;
		if (AttachmentEndSet.Contains(selectionStart))
		{
			DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(selectionEnd);
			DocumentLine documentLine = ((AttachmentDict.ContainsKey(selectionEnd) && lineByOffset.Offset == selectionEnd) ? lineByOffset.PreviousLine : ((lineByOffset.EndOffset == selectionEnd && lineByOffset.NextLine != null && AttachmentDict.ContainsKey(lineByOffset.NextLine.Offset)) ? lineByOffset : null));
			if (documentLine != null && documentLine.DelimiterLength > 0 && documentLine.EndOffset >= selectionStart)
			{
				selectionEnd = documentLine.EndOffset;
				selectionLength = selectionEnd - selectionStart;
				num = selectionStart + documentLine.DelimiterLength;
				if (selectionLength == 0)
				{
					EditBox.Select(num.Value, 0);
					return true;
				}
			}
		}
		if (selectionStart == 0 && selectionEnd >= EditBox.Document.TextLength)
		{
			return TryRemoveEntireDocumentSelection();
		}
		if (TryRemoveSelectedRangeSkippingHeadingFolds(selectionStart, selectionEnd))
		{
			return true;
		}
		if (TryClampSelectionAroundHeadingFoldForMutation(ref selectionStart, ref selectionEnd))
		{
			selectionLength = Math.Max(0, selectionEnd - selectionStart);
			if (selectionLength <= 0)
			{
				EditBox.SelectionLength = 0;
				return true;
			}
			EditBox.Select(selectionStart, selectionLength);
		}
		if (TryRemoveSelectedHeadingFoldContent(selectionStart, selectionEnd))
		{
			return true;
		}
		if (TryRemoveSelectedDisplayOnlyLinkRange(selectionStart, selectionEnd))
		{
			return true;
		}
		if (TryRemoveSelectedSemanticRangeWithLocalMutation(selectionStart, selectionEnd, num))
		{
			return true;
		}
		ExpandSelectionToCodeBlockBoundariesIfNeeded(ref selectionStart, ref selectionEnd);
		selectionLength = Math.Max(0, selectionEnd - selectionStart);
		if (selectionLength <= 0)
		{
			return false;
		}
		DocumentLine lineByOffset2 = EditBox.Document.GetLineByOffset(Math.Max(0, Math.Min(selectionStart, EditBox.Document.TextLength)));
		int val = ((selectionEnd > selectionStart) ? Math.Max(selectionStart, selectionEnd - 1) : selectionStart);
		DocumentLine lineByOffset3 = EditBox.Document.GetLineByOffset(Math.Max(0, Math.Min(val, EditBox.Document.TextLength)));
		int lineCount = EditBox.Document.LineCount;
		string persistedText = GetPersistedText();
		if (!TryGetPersistedSelectionRange(selectionStart, selectionEnd, persistedText, out var persistedStart, out var persistedEnd))
		{
			return false;
		}
		IncludeGeneratedOpeningDelimiterForMultiLineSelection(selectionStart, selectionEnd, persistedText, ref persistedStart);
		if (lineByOffset2 != null && lineByOffset3 != null && lineByOffset2.LineNumber != lineByOffset3.LineNumber && SelectionEndsAtSemanticLineContentEnd(lineByOffset3, selectionEnd))
		{
			persistedEnd = Math.Max(persistedEnd, GetPersistedOffset(Math.Max(lineByOffset3.Offset, lineByOffset3.EndOffset)));
		}
		if (persistedEnd <= persistedStart)
		{
			return false;
		}
		string persistedText2 = persistedText.Remove(persistedStart, persistedEnd - persistedStart);
		persistedText2 = AdjustPersistedInlineSemanticMetadataForDisplayReplacement(persistedText2, selectionStart, selectionLength, 0);
		PreparedEditorTextState preparedEditorTextState = PrepareEditorTextState(persistedText2, repairMisplacedBlockMetadata: true);
		preparedEditorTextState.SemanticDocument = BuildSemanticDocumentAfterSelectionRemoval(selectionStart, selectionEnd, preparedEditorTextState.SemanticDocument, preparedEditorTextState.DisplayText);
		int[] rawToDisplayOffsetMap = preparedEditorTextState.RawToDisplayOffsetMap;
		int num2 = num ?? ((rawToDisplayOffsetMap != null && rawToDisplayOffsetMap.Length != 0) ? rawToDisplayOffsetMap[Math.Max(0, Math.Min(persistedStart, rawToDisplayOffsetMap.Length - 1))] : Math.Max(0, Math.Min(persistedStart, preparedEditorTextState.DisplayText?.Length ?? 0)));
		ApplyPreparedEditorState(preparedEditorTextState, persistedText2, restoreOffset: false, resetHistory: false, num2);
		if (lineCount != EditBox.Document.LineCount)
		{
			AdjustOrderedListMarkers(Math.Max(1, EditBox.Document.GetLineByOffset(Math.Max(0, Math.Min(num2, EditBox.Document.TextLength))).LineNumber));
		}
		CaptureSemanticHistorySnapshot();
		NotifySemanticTextChangedIfNeeded(persistedText);
		return true;
	}

	private bool TryRemoveEntireDocumentSelection()
	{
		if (EditBox?.Document == null || EditBox.Document.TextLength == 0)
		{
			return false;
		}
		string persistedText = GetPersistedText();
		PreparedEditorTextState prepared = PrepareEditorTextState(string.Empty, repairMisplacedBlockMetadata: true);
		ApplyPreparedEditorState(prepared, string.Empty, restoreOffset: false, resetHistory: false, 0);
		CaptureSemanticHistorySnapshot();
		NotifySemanticTextChangedIfNeeded(persistedText);
		return true;
	}

	private bool TryRemoveSelectedSemanticRangeWithLocalMutation(int selectionStart, int selectionEnd, int? caretOffset = null)
	{
		if (EditBox?.Document == null || selectionEnd <= selectionStart)
		{
			return false;
		}
		int textLength = EditBox.Document.TextLength;
		int safeSelectionStart = Math.Max(0, Math.Min(selectionStart, textLength));
		int num = Math.Max(safeSelectionStart, Math.Min(selectionEnd, textLength));
		if (num <= safeSelectionStart)
		{
			return false;
		}
		int num2 = num - safeSelectionStart;
		if (num2 <= 0)
		{
			return false;
		}
		string persistedText = GetPersistedText();
		DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(safeSelectionStart);
		int val = ((num > safeSelectionStart) ? (num - 1) : safeSelectionStart);
		DocumentLine lineByOffset2 = EditBox.Document.GetLineByOffset(Math.Max(0, Math.Min(val, textLength)));
		MarkdownBlockNode block;
		bool flag = lineByOffset != null && TryGetSemanticBlockAtLine(lineByOffset.LineNumber - 1, out block) && block != null && block.Kind == MarkdownBlockKind.List;
		if (!TryGetPersistedSelectionRange(safeSelectionStart, num, persistedText, out var persistedStart, out var persistedEnd) || persistedEnd <= persistedStart)
		{
			return false;
		}
		IncludeGeneratedOpeningDelimiterForMultiLineSelection(safeSelectionStart, num, persistedText, ref persistedStart);
		bool flag2 = flag && persistedStart > GetPersistedLineStart(persistedText, persistedStart);
		bool flag3 = flag && SelectionIncludesListPrefixAtStart(lineByOffset, safeSelectionStart);
		if (flag3)
		{
			persistedStart = GetPersistedLineStart(persistedText, persistedStart);
		}
		if (lineByOffset != null && lineByOffset2 != null && lineByOffset.LineNumber != lineByOffset2.LineNumber && SelectionEndsAtSemanticLineContentEnd(lineByOffset2, num))
		{
			persistedEnd = Math.Max(persistedEnd, GetPersistedOffset(Math.Max(lineByOffset2.Offset, lineByOffset2.EndOffset)));
		}
		string updatedPersistedText = persistedText.Remove(persistedStart, persistedEnd - persistedStart);
		PreparedEditorTextState prepared = PrepareEditorTextState(updatedPersistedText, repairMisplacedBlockMetadata: true);
		string b = (EditBox.Text ?? string.Empty).Remove(safeSelectionStart, num2);
		if (!string.Equals(prepared.DisplayText ?? string.Empty, b, StringComparison.Ordinal))
		{
			return TryRemoveSelectedInlineSemanticRangeWithDisplayMutation(safeSelectionStart, num);
		}
		MarkdownSemanticDocument rebuiltSemanticDocument = ((flag2 || flag3) ? prepared.SemanticDocument : BuildSemanticDocumentAfterSelectionRemoval(safeSelectionStart, num, prepared.SemanticDocument, prepared.DisplayText));
		int previousLineCount = EditBox.Document.LineCount;
		ReplaceEditorText(safeSelectionStart, num2, string.Empty, caretOffset.GetValueOrDefault(safeSelectionStart), delegate
		{
			if (rebuiltSemanticDocument != null)
			{
				_semanticDocument = rebuiltSemanticDocument;
				_semanticBlocksByLine = BuildSemanticBlockIndex(_semanticDocument);
			}
			if (_semanticDocument != null)
			{
				_seedSemanticDocumentForRefresh = CloneSemanticDocument(_semanticDocument);
			}
			if (previousLineCount != EditBox.Document.LineCount)
			{
				AdjustOrderedListMarkers(Math.Max(1, EditBox.Document.GetLineByOffset(Math.Max(0, Math.Min(safeSelectionStart, EditBox.Document.TextLength))).LineNumber));
			}
			RebuildEditingLinkAndAttachmentState(updatedPersistedText, prepared.RawToDisplayOffsetMap);
			LogEmojiPosition(EditBox.Text ?? string.Empty);
			LogCodeRects(EditBox.Text ?? string.Empty);
			if (caretOffset.HasValue)
			{
				EditBox.Document.UndoStack.PushOptional(new AttachmentDeletionCaretUndoAction(EditBox, caretOffset.Value));
			}
			_needsTypedSemanticMarkRedraw = true;
		});
		return true;
	}

	private void IncludeGeneratedOpeningDelimiterForMultiLineSelection(int selectionStart, int selectionEnd, string persistedText, ref int persistedSelectionStart)
	{
		if (EditBox?.Document != null && selectionEnd > selectionStart && EditBox.Document.GetText(selectionStart, selectionEnd - selectionStart).Any((char character) => character == '\r' || character == '\n'))
		{
			int num = (from mark in GetSemanticMarkList()
				where mark != null && MarkdownInlineSemanticMetadata.CanSerialize(mark) && mark.SourcePosition == selectionStart && mark.SourceLength > 0
				select MarkdownInlineSemanticMetadata.GetOpeningDelimiter(mark)?.Length ?? 0).DefaultIfEmpty(0).Sum();
			if (num > 0)
			{
				persistedSelectionStart = Math.Max(GetPersistedLineStart(persistedText, persistedSelectionStart), persistedSelectionStart - num);
			}
		}
	}

	private bool SelectionIncludesListPrefixAtStart(DocumentLine line, int selectionStart)
	{
		if (line == null || selectionStart != line.Offset || !TryGetSemanticBlockAtLine(line.LineNumber - 1, out var block) || block == null || block.Kind != MarkdownBlockKind.List)
		{
			return false;
		}
		SelectionSegment selectionSegment = (from segment in EditBox?.TextArea?.Selection?.Segments
			orderby segment.StartOffset, segment.StartVisualColumn
			select segment).FirstOrDefault();
		if (selectionSegment != null && selectionSegment.StartOffset == selectionStart)
		{
			return selectionSegment.StartVisualColumn == 0;
		}
		return false;
	}

	private static int GetPersistedLineStart(string persistedText, int persistedOffset)
	{
		persistedText = persistedText ?? string.Empty;
		int num = Math.Max(0, Math.Min(persistedOffset, persistedText.Length));
		return persistedText.LastIndexOf('\n', Math.Max(0, num - 1)) + 1;
	}

	private bool TryRemoveSelectedInlineSemanticRangeWithDisplayMutation(int selectionStart, int selectionEnd)
	{
		if (EditBox?.Document == null || selectionEnd <= selectionStart)
		{
			return false;
		}
		int textLength = EditBox.Document.TextLength;
		int num = Math.Max(0, Math.Min(selectionStart, textLength));
		int num2 = Math.Max(num, Math.Min(selectionEnd, textLength));
		int num3 = num2 - num;
		if (num3 <= 0)
		{
			return false;
		}
		string text = EditBox.Document.GetText(num, num3);
		if (text.IndexOf('\r') >= 0 || text.IndexOf('\n') >= 0)
		{
			return false;
		}
		DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(num);
		if (lineByOffset == null || num2 > lineByOffset.EndOffset || IsCodeBlockLine(lineByOffset.LineNumber - 1) || SelectionIntersectsAttachmentToken(num, num2) || SelectionIntersectsNonDisplayOnlyLinkToken(num, num2))
		{
			return false;
		}
		ReplaceEditorText(num, num3, string.Empty, num);
		return true;
	}

	private bool TryRemoveSelectedDisplayOnlyLinkRange(int selectionStart, int selectionEnd)
	{
		if (EditBox?.Document == null || selectionEnd <= selectionStart)
		{
			return false;
		}
		int textLength = EditBox.Document.TextLength;
		int num = Math.Max(0, Math.Min(selectionStart, textLength));
		int num2 = Math.Max(num, Math.Min(selectionEnd, textLength));
		int num3 = num2 - num;
		if (num3 <= 0 || !SelectionIntersectsLinkDisplayRange(num, num2))
		{
			return false;
		}
		string text = EditBox.Document.GetText(num, num3);
		if (text.IndexOf('\r') >= 0 || text.IndexOf('\n') >= 0)
		{
			return false;
		}
		DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(num);
		if (lineByOffset == null || num2 > lineByOffset.EndOffset || IsCodeBlockLine(lineByOffset.LineNumber - 1) || SelectionIntersectsAttachmentToken(num, num2))
		{
			return false;
		}
		ReplaceEditorText(num, num3, string.Empty, num);
		return true;
	}

	private bool SelectionIntersectsAttachmentToken(int selectionStart, int selectionEnd)
	{
		return AttachmentDict.Values.Any(delegate(AttachmentInfo info)
		{
			if (info == null)
			{
				return false;
			}
			int offset = info.Offset;
			int num = info.Offset + (info.Value?.Length ?? 0);
			return selectionStart < num && selectionEnd > offset;
		});
	}

	private bool SelectionIntersectsNonDisplayOnlyLinkToken(int selectionStart, int selectionEnd)
	{
		return GetSemanticMarkList().Any((MarkdownInlineDisplaySegment mark) => mark != null && mark.Kind == MarkdownMarkKind.Link && !IsDisplayOnlySemanticLink(mark) && selectionStart < mark.SourcePosition + mark.SourceLength && selectionEnd > mark.SourcePosition);
	}

	private bool SelectionIntersectsLinkDisplayRange(int selectionStart, int selectionEnd)
	{
		foreach (KeyValuePair<int, LinkInfo> item in LinkNameDict)
		{
			int key = item.Key;
			int valueOrDefault = (item.Value?.Link?.Length).GetValueOrDefault();
			if (valueOrDefault > 0)
			{
				int num = key + valueOrDefault;
				if (selectionStart < num && selectionEnd > key)
				{
					return true;
				}
			}
		}
		return GetSemanticMarkList().Any((MarkdownInlineDisplaySegment mark) => IsDisplayOnlySemanticLink(mark) && selectionStart < mark.SourcePosition + mark.SourceLength && selectionEnd > mark.SourcePosition);
	}

	private static bool IsDisplayOnlySemanticLink(MarkdownInlineDisplaySegment mark)
	{
		if (mark != null && mark.Kind == MarkdownMarkKind.Link && mark.OpeningDelimiterLength == 0)
		{
			return mark.ClosingDelimiterLength == 0;
		}
		return false;
	}

	private bool TryRemoveSelectedRangeSkippingHeadingFolds(int selectionStart, int selectionEnd)
	{
		if (_headingFoldingManager == null || EditBox?.Document == null || selectionEnd <= selectionStart)
		{
			return false;
		}
		int documentLength = EditBox.Document.TextLength;
		int safeSelectionStart = Math.Max(0, Math.Min(selectionStart, documentLength));
		int safeSelectionEnd = Math.Max(safeSelectionStart, Math.Min(selectionEnd, documentLength));
		List<FoldingSection> list = (from folding in _headingFoldingManager.AllFoldings
			where folding != null && folding.IsFolded && safeSelectionStart < folding.EndOffset && safeSelectionEnd > folding.StartOffset
			orderby folding.StartOffset
			select folding).ToList();
		if (list.Count == 0)
		{
			return false;
		}
		string previousPersistedText = GetPersistedText();
		List<DisplaySelectionSegment> persistedRanges = new List<DisplaySelectionSegment>();
		List<DisplaySelectionSegment> list2 = BuildHeadingFoldDisplayRemovalRanges(safeSelectionStart, safeSelectionEnd, list.Select((FoldingSection folding) => new DisplaySelectionSegment(Math.Max(0, Math.Min(folding.StartOffset, documentLength)), Math.Max(0, Math.Min(folding.EndOffset, documentLength)))));
		foreach (DisplaySelectionSegment item in list2)
		{
			AddPersistedRemovalRange(item.Start, item.End);
		}
		if (persistedRanges.Count == 0)
		{
			EditBox.TextArea.ClearSelection();
			EditBox.CaretOffset = safeSelectionStart;
			return true;
		}
		List<DisplaySelectionSegment> list3 = new List<DisplaySelectionSegment>();
		foreach (DisplaySelectionSegment item2 in persistedRanges.OrderBy((DisplaySelectionSegment range) => range.Start))
		{
			if (list3.Count == 0)
			{
				list3.Add(item2);
				continue;
			}
			DisplaySelectionSegment displaySelectionSegment = list3[list3.Count - 1];
			if (item2.Start <= displaySelectionSegment.End)
			{
				list3[list3.Count - 1] = new DisplaySelectionSegment(displaySelectionSegment.Start, Math.Max(displaySelectionSegment.End, item2.End));
			}
			else
			{
				list3.Add(item2);
			}
		}
		string text = previousPersistedText;
		foreach (DisplaySelectionSegment item3 in list3.OrderByDescending((DisplaySelectionSegment range) => range.Start))
		{
			int num = Math.Max(0, Math.Min(item3.Start, text.Length));
			int num2 = Math.Max(num, Math.Min(item3.End, text.Length));
			if (num2 > num)
			{
				text = text.Remove(num, num2 - num);
			}
		}
		text = AdjustPersistedInlineSemanticMetadataForDisplayRemovals(text, list2);
		int val = list3.Min((DisplaySelectionSegment range) => range.Start);
		PreparedEditorTextState preparedEditorTextState = PrepareEditorTextState(text, repairMisplacedBlockMetadata: true);
		preparedEditorTextState.SemanticDocument = BuildSemanticDocumentAfterSelectionRemoval(safeSelectionStart, safeSelectionEnd, preparedEditorTextState.SemanticDocument, preparedEditorTextState.DisplayText);
		int[] rawToDisplayOffsetMap = preparedEditorTextState.RawToDisplayOffsetMap;
		int value = ((rawToDisplayOffsetMap != null && rawToDisplayOffsetMap.Length != 0) ? rawToDisplayOffsetMap[Math.Max(0, Math.Min(val, rawToDisplayOffsetMap.Length - 1))] : Math.Max(0, Math.Min(safeSelectionStart, preparedEditorTextState.DisplayText?.Length ?? 0)));
		ApplyPreparedEditorState(preparedEditorTextState, text, restoreOffset: false, resetHistory: false, value);
		CaptureSemanticHistorySnapshot();
		NotifySemanticTextChangedIfNeeded(previousPersistedText);
		return true;
		void AddPersistedRemovalRange(int start, int end)
		{
			if (end > start && TryGetPersistedSelectionRange(start, end, previousPersistedText, out var persistedStart, out var persistedEnd) && persistedEnd > persistedStart)
			{
				persistedRanges.Add(new DisplaySelectionSegment(persistedStart, persistedEnd));
			}
		}
	}

	private static List<DisplaySelectionSegment> BuildHeadingFoldDisplayRemovalRanges(int selectionStart, int selectionEnd, IEnumerable<DisplaySelectionSegment> foldedRanges)
	{
		List<DisplaySelectionSegment> list = new List<DisplaySelectionSegment>();
		if (selectionEnd <= selectionStart)
		{
			return list;
		}
		List<DisplaySelectionSegment> list2 = (from range in foldedRanges ?? Enumerable.Empty<DisplaySelectionSegment>()
			where range.End > range.Start
			orderby range.Start
			select range).ToList();
		if (list2.Count == 0)
		{
			list.Add(new DisplaySelectionSegment(selectionStart, selectionEnd));
			return list;
		}
		int num = selectionStart;
		foreach (DisplaySelectionSegment item in list2)
		{
			if (item.End > num && item.Start < selectionEnd)
			{
				int num2 = Math.Min(item.Start, selectionEnd);
				if (num2 > num)
				{
					list.Add(new DisplaySelectionSegment(num, num2));
				}
				list.Add(new DisplaySelectionSegment(item.Start, item.End));
				num = Math.Max(num, Math.Min(item.End, selectionEnd));
				if (num >= selectionEnd)
				{
					break;
				}
			}
		}
		if (num < selectionEnd)
		{
			list.Add(new DisplaySelectionSegment(num, selectionEnd));
		}
		return list;
	}

	private static string AdjustPersistedInlineSemanticMetadataForDisplayReplacement(string persistedText, int startOffset, int removedLength, int insertedLength)
	{
		string text = persistedText ?? string.Empty;
		string rawText = text;
		List<MarkdownInlineDisplaySegment> list = MarkdownInlineSemanticMetadata.Extract(ref rawText);
		if (list.Count == 0)
		{
			return text;
		}
		if (!AdjustInlineSemanticMarksForReplace(list, startOffset, removedLength, insertedLength))
		{
			return text;
		}
		return rawText + MarkdownInlineSemanticMetadata.Serialize(list);
	}

	private static string AdjustPersistedInlineSemanticMetadataForDisplayRemovals(string persistedText, IEnumerable<DisplaySelectionSegment> removalRanges)
	{
		List<DisplaySelectionSegment> list = (from range in removalRanges?.Where((DisplaySelectionSegment range) => range.End > range.Start)
			orderby range.Start descending
			select range).ToList();
		if (list == null || list.Count == 0)
		{
			return persistedText ?? string.Empty;
		}
		string text = persistedText ?? string.Empty;
		string rawText = text;
		List<MarkdownInlineDisplaySegment> list2 = MarkdownInlineSemanticMetadata.Extract(ref rawText);
		if (list2.Count == 0)
		{
			return text;
		}
		bool flag = false;
		foreach (DisplaySelectionSegment item in list)
		{
			flag |= AdjustInlineSemanticMarksForReplace(list2, item.Start, item.End - item.Start, 0);
		}
		if (!flag)
		{
			return text;
		}
		return rawText + MarkdownInlineSemanticMetadata.Serialize(list2);
	}

	private bool TryRemoveSelectedHeadingFoldContent(int selectionStart, int selectionEnd)
	{
		if (EditBox?.Document == null || _headingFoldingManager == null || selectionEnd <= selectionStart)
		{
			return false;
		}
		TextDocument document = EditBox.Document;
		int safeSelectionStart = Math.Max(0, Math.Min(selectionStart, document.TextLength));
		int safeSelectionEnd = Math.Max(safeSelectionStart, Math.Min(selectionEnd, document.TextLength));
		FoldingSection foldingSection = _headingFoldingManager.AllFoldings.FirstOrDefault((FoldingSection section) => section != null && section.IsFolded && safeSelectionStart >= section.StartOffset && safeSelectionEnd <= section.EndOffset);
		if (foldingSection == null)
		{
			return false;
		}
		int num = Math.Max(0, Math.Min(foldingSection.StartOffset, document.TextLength));
		int num2 = Math.Max(num, Math.Min(foldingSection.EndOffset, document.TextLength));
		if (num2 <= num)
		{
			return false;
		}
		int lineIndex = Math.Max(0, (document.GetLineByOffset(num)?.LineNumber ?? 1) - 1);
		string persistedText = GetPersistedText();
		SetHeadingFoldedStateAtLine(lineIndex, folded: false);
		foldingSection.IsFolded = false;
		string persistedText2 = GetPersistedText();
		int num3 = Math.Max(0, Math.Min(GetPersistedOffset(num), persistedText2.Length));
		int num4 = Math.Max(num3, Math.Min(GetPersistedOffset(num2), persistedText2.Length));
		if (num4 <= num3)
		{
			UpdateHeadingFoldings();
			return true;
		}
		string persistedText3 = persistedText2.Remove(num3, num4 - num3);
		persistedText3 = AdjustPersistedInlineSemanticMetadataForDisplayReplacement(persistedText3, num, num2 - num, 0);
		PreparedEditorTextState preparedEditorTextState = PrepareEditorTextState(persistedText3, repairMisplacedBlockMetadata: true);
		preparedEditorTextState.SemanticDocument = BuildSemanticDocumentAfterSelectionRemoval(num, num2, preparedEditorTextState.SemanticDocument, preparedEditorTextState.DisplayText);
		int value = ((preparedEditorTextState.RawToDisplayOffsetMap != null && preparedEditorTextState.RawToDisplayOffsetMap.Length != 0) ? preparedEditorTextState.RawToDisplayOffsetMap[Math.Max(0, Math.Min(num3, preparedEditorTextState.RawToDisplayOffsetMap.Length - 1))] : Math.Max(0, Math.Min(num, preparedEditorTextState.DisplayText?.Length ?? 0)));
		ApplyPreparedEditorState(preparedEditorTextState, persistedText3, restoreOffset: false, resetHistory: false, value);
		CaptureSemanticHistorySnapshot();
		NotifySemanticTextChangedIfNeeded(persistedText);
		return true;
	}

	private void SetHeadingFoldedStateAtLine(int lineIndex, bool folded)
	{
		if (lineIndex < 0)
		{
			return;
		}
		if (TryGetSemanticBlockAtLine(lineIndex, out var block) && block != null && block.Kind == MarkdownBlockKind.Heading)
		{
			block.DisplayFolded = null;
			block.Folded = folded;
		}
		if (_seedSemanticDocumentForRefresh?.Blocks == null)
		{
			return;
		}
		foreach (MarkdownBlockNode block2 in _seedSemanticDocumentForRefresh.Blocks)
		{
			if (block2 != null && block2.Kind == MarkdownBlockKind.Heading && block2.StartLine == lineIndex)
			{
				block2.DisplayFolded = null;
				block2.Folded = folded;
			}
		}
	}

	private void ExpandSelectionToCodeBlockBoundariesIfNeeded(ref int selectionStart, ref int selectionEnd)
	{
		if (EditBox?.Document == null || selectionEnd <= selectionStart)
		{
			return;
		}
		int num = Math.Max(0, Math.Min(selectionStart, EditBox.Document.TextLength));
		int num2 = Math.Max(num, Math.Min(selectionEnd, EditBox.Document.TextLength));
		if (IsSelectionInsideSingleCodeBlockContent(num, num2))
		{
			return;
		}
		DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(num);
		int val = ((num2 > num) ? (num2 - 1) : num);
		DocumentLine lineByOffset2 = EditBox.Document.GetLineByOffset(Math.Max(0, Math.Min(val, EditBox.Document.TextLength)));
		if (lineByOffset == null || lineByOffset2 == null)
		{
			return;
		}
		MarkdownBlockNode[] array = new MarkdownBlockNode[EditBox.Document.LineCount];
		for (int i = 0; i < array.Length; i++)
		{
			TryGetSemanticBlockAtLine(i, out array[i]);
		}
		if (TryExpandCodeBlockLineRange(array, lineByOffset.LineNumber - 1, lineByOffset2.LineNumber - 1, out var expandedStartLineIndex, out var expandedEndLineIndex))
		{
			DocumentLine lineByNumber = EditBox.Document.GetLineByNumber(Math.Max(1, expandedStartLineIndex + 1));
			DocumentLine lineByNumber2 = EditBox.Document.GetLineByNumber(Math.Max(1, Math.Min(expandedEndLineIndex + 1, EditBox.Document.LineCount)));
			if (lineByNumber != null && lineByNumber2 != null)
			{
				int num3 = Math.Max(0, Math.Min(lineByNumber.Offset, EditBox.Document.TextLength));
				int val2 = Math.Max(num3, Math.Min(lineByNumber2.EndOffset + lineByNumber2.DelimiterLength, EditBox.Document.TextLength));
				selectionStart = Math.Min(num, num3);
				selectionEnd = Math.Max(num2, val2);
			}
		}
	}

	private bool IsSelectionInsideSingleCodeBlockContent(int selectionStart, int selectionEnd)
	{
		if (EditBox?.Document == null || selectionEnd <= selectionStart)
		{
			return false;
		}
		int num = Math.Max(0, Math.Min(selectionStart, EditBox.Document.TextLength));
		int num2 = Math.Max(num, Math.Min(selectionEnd, EditBox.Document.TextLength));
		int offset = ((num2 > num) ? (num2 - 1) : num);
		if (!TryGetCodeBlockAtSelectionOffset(num, treatAsSelectionEnd: false, out var block) || !TryGetCodeBlockAtSelectionOffset(offset, treatAsSelectionEnd: false, out var block2) || block == null || block2 == null || block.StartLine != block2.StartLine || block.EndLine != block2.EndLine || IsCodeBlockObjectSelected(block) || !TryGetCodeBlockContentRange(block, out var contentStartOffset, out var contentEndOffset))
		{
			return false;
		}
		if (num >= contentStartOffset)
		{
			return num2 <= contentEndOffset;
		}
		return false;
	}

	private MarkdownSemanticDocument BuildSemanticDocumentAfterSelectionRemoval(int selectionStart, int selectionEnd, MarkdownSemanticDocument fallbackDocument, string updatedDisplayText)
	{
		if (EditBox?.Document == null)
		{
			return fallbackDocument;
		}
		int num = Math.Max(0, Math.Min(selectionStart, EditBox.Document.TextLength));
		int num2 = Math.Max(num, Math.Min(selectionEnd, EditBox.Document.TextLength));
		DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(num);
		int val = ((num2 > num) ? Math.Max(num, num2 - 1) : num);
		DocumentLine lineByOffset2 = EditBox.Document.GetLineByOffset(Math.Max(0, Math.Min(val, EditBox.Document.TextLength)));
		if (lineByOffset == null || lineByOffset2 == null)
		{
			return fallbackDocument;
		}
		List<MarkdownBlockNode> list = CaptureLineSemanticSnapshot();
		if (list.Count == 0)
		{
			return fallbackDocument;
		}
		int num3 = Math.Max(0, lineByOffset.LineNumber - 1);
		int num4 = Math.Max(num3, lineByOffset2.LineNumber - 1);
		bool flag = !SelectionStartsAtSemanticLineContentStart(lineByOffset, num);
		bool flag2 = SelectionEndsAtSemanticLineContentEnd(lineByOffset, num);
		bool flag3 = !SelectionEndsAtSemanticLineContentEnd(lineByOffset2, num2);
		bool flag4 = lineByOffset2.DelimiterLength > 0 && num2 >= lineByOffset2.EndOffset + lineByOffset2.DelimiterLength;
		if (flag && flag4 && lineByOffset2.NextLine != null && num2 == lineByOffset2.NextLine.Offset && SelectionStartsAtSemanticLineContentEnd(lineByOffset, num))
		{
			flag2 = false;
			num4 = Math.Min(list.Count - 1, num4 + 1);
		}
		if (!flag2 && !flag && !flag3 && !flag4)
		{
			flag2 = true;
		}
		if (flag2 && !flag && flag4)
		{
			flag2 = false;
		}
		int lineCount = (updatedDisplayText ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n')
			.Length;
		if (ContainsCodeBlock(list))
		{
			return fallbackDocument;
		}
		if (DoesSelectionRemovalTouchCodeBlock(list, num3, num4))
		{
			return fallbackDocument;
		}
		return BuildSemanticDocumentFromLineSnapshot(BuildLineSemanticSnapshotAfterRangeRemoval(list, num3, num4, flag, flag3, flag2), lineCount);
	}

	private static bool ContainsCodeBlock(IReadOnlyList<MarkdownBlockNode> lineSnapshot)
	{
		return MarkdownSemanticDocumentMutations.ContainsCodeBlock(lineSnapshot);
	}

	private static bool DoesSelectionRemovalTouchCodeBlock(IReadOnlyList<MarkdownBlockNode> lineSnapshot, int startLineIndex, int endLineIndex)
	{
		return MarkdownSemanticDocumentMutations.DoesSelectionRemovalTouchCodeBlock(lineSnapshot, startLineIndex, endLineIndex);
	}

	private static bool TryExpandCodeBlockLineRange(IReadOnlyList<MarkdownBlockNode> lineSnapshot, int startLineIndex, int endLineIndex, out int expandedStartLineIndex, out int expandedEndLineIndex)
	{
		return MarkdownSemanticDocumentMutations.TryExpandCodeBlockLineRange(lineSnapshot, startLineIndex, endLineIndex, out expandedStartLineIndex, out expandedEndLineIndex);
	}

	private static void SyncCodeBlocksFromDisplayText(MarkdownSemanticDocument document, string displayText)
	{
		if (document?.Blocks == null || document.Blocks.Count == 0)
		{
			return;
		}
		string[] lines = (displayText ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
		List<MarkdownBlockNode> list = new List<MarkdownBlockNode>();
		foreach (MarkdownBlockNode block in document.Blocks)
		{
			if (!SyncCodeBlockFromDisplayLines(block, lines))
			{
				list.Add(block);
			}
		}
		foreach (MarkdownBlockNode item in list)
		{
			document.Blocks.Remove(item);
		}
	}

	private static bool SyncCodeBlockFromDisplayLines(MarkdownBlockNode block, string[] lines)
	{
		if (block == null || block.Kind != MarkdownBlockKind.CodeBlock)
		{
			return true;
		}
		if (lines == null || lines.Length == 0)
		{
			block.CodeText = string.Empty;
			return false;
		}
		if (block.StartLine < 0 || block.StartLine >= lines.Length)
		{
			block.CodeText = string.Empty;
			return false;
		}
		int startLine = block.StartLine;
		if (block.EndLine == block.StartLine && string.Equals(lines[startLine], '\ue000'.ToString(), StringComparison.Ordinal))
		{
			return true;
		}
		int num = Math.Max(startLine, Math.Min(block.EndLine, lines.Length - 1));
		if (!TryGetEditingCodeFenceInfo(NormalizeDisplayTaskMarkerLine(lines[startLine] ?? string.Empty), out var indent, out var language))
		{
			block.CodeText = string.Empty;
			return false;
		}
		block.Language = (string.IsNullOrWhiteSpace(language) ? null : language);
		string lineText = NormalizeDisplayTaskMarkerLine(lines[num] ?? string.Empty);
		bool num2 = num > startLine && TryGetEditingCodeFenceClosingInfo(lineText, out indent);
		int num3 = startLine + 1;
		int num4 = (num2 ? (num - 1) : num);
		if (num3 > num4 || num3 >= lines.Length)
		{
			block.CodeText = string.Empty;
			return true;
		}
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = num3; i <= num4; i++)
		{
			if (stringBuilder.Length > 0)
			{
				stringBuilder.Append('\n');
			}
			stringBuilder.Append(NormalizeDisplayTaskMarkerLine(lines[i] ?? string.Empty));
		}
		block.CodeText = stringBuilder.ToString();
		return true;
	}

	private List<MarkdownBlockNode> CaptureLineSemanticSnapshot()
	{
		List<MarkdownBlockNode> list = new List<MarkdownBlockNode>();
		int valueOrDefault = (EditBox?.Document?.LineCount).GetValueOrDefault();
		for (int i = 0; i < valueOrDefault; i++)
		{
			if (TryGetSemanticBlockAtLine(i, out var block) && block != null)
			{
				list.Add(CloneSemanticBlockAsSingleLine(block, i));
				continue;
			}
			list.Add(new MarkdownBlockNode
			{
				Kind = MarkdownBlockKind.Paragraph,
				StartLine = i,
				EndLine = i,
				Indent = 0
			});
		}
		return list;
	}

	private static List<MarkdownBlockNode> CaptureLineSemanticSnapshot(MarkdownSemanticDocument document, int lineCount)
	{
		return MarkdownSemanticDocumentMutations.CaptureLineSnapshot(document, lineCount);
	}

	private static List<MarkdownBlockNode> BuildLineSemanticSnapshotAfterRangeRemoval(IReadOnlyList<MarkdownBlockNode> sourceLines, int startLineIndex, int endLineIndex, bool keepsStartLinePrefix, bool keepsEndLineSuffix, bool preservesStartLineBoundary)
	{
		return MarkdownSemanticDocumentMutations.BuildLineSnapshotAfterRangeRemoval(sourceLines, startLineIndex, endLineIndex, keepsStartLinePrefix, keepsEndLineSuffix, preservesStartLineBoundary);
	}

	private static MarkdownSemanticDocument BuildSemanticDocumentFromLineSnapshot(IReadOnlyList<MarkdownBlockNode> lineBlocks, int lineCount)
	{
		return MarkdownSemanticDocumentMutations.BuildDocumentFromLineSnapshot(lineBlocks, lineCount);
	}

	private static MarkdownBlockNode CloneSemanticBlockAsSingleLine(MarkdownBlockNode source, int lineIndex)
	{
		return MarkdownSemanticDocumentMutations.CloneBlockAsSingleLine(source, lineIndex);
	}

	private static MarkdownBlockNode CloneSemanticBlockAsLineRange(MarkdownBlockNode source, int startLineIndex, int endLineIndex)
	{
		return MarkdownSemanticDocumentMutations.CloneBlockAsLineRange(source, startLineIndex, endLineIndex);
	}

	private bool SelectionStartsAtSemanticLineContentStart(DocumentLine line, int selectionStart)
	{
		if (line == null)
		{
			return false;
		}
		int num = Math.Max(line.Offset, Math.Min(selectionStart, line.EndOffset));
		if (num <= line.Offset)
		{
			return true;
		}
		string text = EditBox?.Document?.GetText(line) ?? string.Empty;
		for (int i = 0; i < Math.Min(text.Length, num - line.Offset); i++)
		{
			if (!IsIgnorableSemanticBoundaryChar(text[i]))
			{
				return false;
			}
		}
		return true;
	}

	private bool SelectionStartsAtSemanticLineContentEnd(DocumentLine line, int selectionStart)
	{
		if (IsEffectivelyEmptySemanticLine(EditBox?.Document?.GetText(line) ?? string.Empty))
		{
			return false;
		}
		return SelectionEndsAtSemanticLineContentEnd(line, selectionStart);
	}

	private bool SelectionEndsAtSemanticLineContentEnd(DocumentLine line, int selectionEnd)
	{
		if (line == null)
		{
			return false;
		}
		int num = Math.Max(line.Offset, Math.Min(selectionEnd, line.EndOffset + line.DelimiterLength));
		if (num >= line.EndOffset + line.DelimiterLength)
		{
			return true;
		}
		string text = EditBox?.Document?.GetText(line) ?? string.Empty;
		for (int i = Math.Max(0, Math.Min(text.Length, num - line.Offset)); i < text.Length; i++)
		{
			if (!IsIgnorableSemanticBoundaryChar(text[i]))
			{
				return false;
			}
		}
		return true;
	}

	private static bool IsIgnorableSemanticBoundaryChar(char c)
	{
		if (c != '\u00a0' && c != '\u2065' && c != '\u00a0' && c != '\u200b' && c != '\u200c' && c != '\u200d' && c != '\u200e' && c != '\u200f' && c != '\u2060' && c != '\u2061' && c != '\u2062' && c != '\u2063' && c != '\u2064' && c != '\u2065' && c != '\u2066' && c != '\u206a')
		{
			return char.IsWhiteSpace(c);
		}
		return true;
	}

	private static void RemoveSemanticRangeAndShiftFollowing(MarkdownSemanticDocument document, int startLineIndex, int endLineIndex, int removedLineCount)
	{
		MarkdownSemanticDocumentMutations.RemoveRangeAndShiftFollowing(document, startLineIndex, endLineIndex, removedLineCount);
	}

	private static MarkdownSemanticDocument CloneSemanticDocument(MarkdownSemanticDocument source)
	{
		return MarkdownSemanticDocumentMutations.CloneDocument(source);
	}

	private static EditorHistoryState CloneHistoryState(EditorHistoryState source)
	{
		if (source == null)
		{
			return null;
		}
		EditorHistoryState editorHistoryState = new EditorHistoryState();
		editorHistoryState.PersistedText = source.PersistedText;
		editorHistoryState.DisplayText = source.DisplayText;
		editorHistoryState.SemanticDocument = CloneSemanticDocument(source.SemanticDocument);
		editorHistoryState.CaretOffset = source.CaretOffset;
		editorHistoryState.TypedSemanticMarks.AddRange(CloneDisplaySegments(source.TypedSemanticMarks));
		return editorHistoryState;
	}

	private static List<MarkdownInlineDisplaySegment> CloneDisplaySegments(IEnumerable<MarkdownInlineDisplaySegment> segments)
	{
		return MarkdownSemanticDocumentMutations.CloneDisplaySegments(segments);
	}

	private static void ShiftSemanticBlocksForMutations(MarkdownSemanticDocument document, string originalText, IReadOnlyList<DocumentMutation> mutations)
	{
		if (document?.Blocks == null || mutations == null || mutations.Count == 0)
		{
			return;
		}
		foreach (DocumentMutation item in from m in mutations
			where m != null
			orderby m.StartOffset descending
			select m)
		{
			int lineIndexAtOffset = GetLineIndexAtOffset(originalText, item.StartOffset);
			bool flag = IsOffsetAtLineStart(originalText, item.StartOffset);
			string text = string.Empty;
			if (!string.IsNullOrEmpty(originalText) && item.RemoveLength > 0 && item.StartOffset >= 0 && item.StartOffset + item.RemoveLength <= originalText.Length)
			{
				text = originalText.Substring(item.StartOffset, item.RemoveLength);
			}
			int num = CountLineBreaks(item.InsertText ?? string.Empty) - CountLineBreaks(text);
			if (num == 0)
			{
				continue;
			}
			if (num < 0)
			{
				int num2 = -num;
				int firstRemovedLine = (flag ? lineIndexAtOffset : (lineIndexAtOffset + 1));
				int lastRemovedLine = firstRemovedLine + num2 - 1;
				foreach (MarkdownBlockNode item2 in document.Blocks.Where((MarkdownBlockNode block) => block != null && block.StartLine >= firstRemovedLine && block.EndLine <= lastRemovedLine).ToList())
				{
					document.Blocks.Remove(item2);
				}
			}
			foreach (MarkdownBlockNode block in document.Blocks)
			{
				if (block != null && (!(num > 0 && flag) || item.RemoveLength != 0 || !IsTextBearingSemanticBlock(block) || block.StartLine != lineIndexAtOffset || block.EndLine != lineIndexAtOffset || !IsEffectivelyEmptySemanticLineAt(originalText, lineIndexAtOffset)))
				{
					if (block.StartLine > lineIndexAtOffset || (num > 0 && flag && block.StartLine == lineIndexAtOffset))
					{
						block.StartLine = Math.Max(0, block.StartLine + num);
					}
					bool flag2 = block.EndLine > lineIndexAtOffset || (num < 0 && block.EndLine == lineIndexAtOffset) || (num > 0 && flag && block.EndLine == lineIndexAtOffset);
					if (!flag2 && num > 0 && block.Kind == MarkdownBlockKind.CodeBlock && block.StartLine <= lineIndexAtOffset && block.EndLine == lineIndexAtOffset && !IsCodeFenceLine(originalText, lineIndexAtOffset) && !IsCodeBlockAttachmentPlaceholderLine(originalText, lineIndexAtOffset))
					{
						flag2 = true;
					}
					if (flag2)
					{
						block.EndLine = Math.Max(block.StartLine, block.EndLine + num);
					}
				}
			}
		}
	}

	private static bool IsEffectivelyEmptySemanticLineAt(string text, int lineIndex)
	{
		if (!TryGetLineStartOffset(text, lineIndex, out var offset) || !TryGetLineEndOffset(text, lineIndex, out var offset2) || offset > offset2)
		{
			return false;
		}
		for (int i = offset; i < offset2; i++)
		{
			if (!IsIgnorableSemanticBoundaryChar(text[i]))
			{
				return false;
			}
		}
		return true;
	}

	private static bool IsTextBearingSemanticBlock(MarkdownBlockNode block)
	{
		if (block == null)
		{
			return false;
		}
		if (block.Kind != MarkdownBlockKind.List && block.Kind != MarkdownBlockKind.BlockQuote && block.Kind != MarkdownBlockKind.Heading)
		{
			if (block.Kind == MarkdownBlockKind.Paragraph)
			{
				if (Math.Max(0, block.Indent) <= 0)
				{
					return block.SoftTab == true;
				}
				return true;
			}
			return false;
		}
		return true;
	}

	private static bool IsCodeFenceLine(string text, int lineIndex)
	{
		if (!TryGetLineStartOffset(text, lineIndex, out var offset) || !TryGetLineEndOffset(text, lineIndex, out var offset2) || offset > offset2)
		{
			return false;
		}
		int indent;
		string language;
		return TryGetEditingCodeFenceInfo(text.Substring(offset, offset2 - offset), out indent, out language);
	}

	private static bool IsCodeBlockAttachmentPlaceholderLine(string text, int lineIndex)
	{
		if (!TryGetLineStartOffset(text, lineIndex, out var offset) || !TryGetLineEndOffset(text, lineIndex, out var offset2) || offset2 - offset != 1)
		{
			return false;
		}
		return text[offset] == '\ue000';
	}

	private static int CountLineBreaks(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < text.Length; i++)
		{
			if (text[i] == '\n')
			{
				num++;
			}
		}
		return num;
	}

	private static int GetLineIndexAtOffset(string text, int offset)
	{
		if (string.IsNullOrEmpty(text) || offset <= 0)
		{
			return 0;
		}
		int num = Math.Max(0, Math.Min(offset, text.Length));
		int num2 = 0;
		for (int i = 0; i < num; i++)
		{
			if (text[i] == '\n')
			{
				num2++;
			}
		}
		return num2;
	}

	private static bool IsOffsetAtLineStart(string text, int offset)
	{
		if (offset <= 0)
		{
			return true;
		}
		int num = Math.Max(0, Math.Min(offset, text?.Length ?? 0));
		if (num == 0)
		{
			return true;
		}
		if (!string.IsNullOrEmpty(text))
		{
			return text[num - 1] == '\n';
		}
		return false;
	}

	private static bool TryGetCodeBlockDeletionLineRange(MarkdownBlockNode block, int lineIndex, bool backspace, bool atBoundary, bool hasClosingFenceLine, out int startLineIndex, out int endLineIndex)
	{
		return MarkdownSemanticDocumentMutations.TryGetCodeBlockDeletionLineRange(block, lineIndex, backspace, atBoundary, hasClosingFenceLine, out startLineIndex, out endLineIndex);
	}

	private static bool TryGetAdjacentCodeBlockDeletionLineRange(IReadOnlyList<MarkdownBlockNode> lineSnapshot, int lineIndex, bool backspace, bool atBoundary, out int startLineIndex, out int endLineIndex)
	{
		return MarkdownSemanticDocumentMutations.TryGetAdjacentCodeBlockDeletionLineRange(lineSnapshot, lineIndex, backspace, atBoundary, out startLineIndex, out endLineIndex);
	}

	private void NormalizeTextInsertionsBeforeCodeBlockAttachments(IReadOnlyList<DocumentMutation> mutations)
	{
		if (EditBox?.Document == null || mutations == null)
		{
			return;
		}
		TextDocument document = EditBox.Document;
		foreach (DocumentMutation mutation in mutations)
		{
			string text = mutation?.InsertText ?? string.Empty;
			if (mutation != null && mutation.RemoveLength == 0 && !string.IsNullOrEmpty(text) && !text.EndsWith("\r", StringComparison.Ordinal) && !text.EndsWith("\n", StringComparison.Ordinal))
			{
				int num = Math.Max(0, Math.Min(mutation.StartOffset, document.TextLength));
				DocumentLine lineByOffset = document.GetLineByOffset(num);
				if (lineByOffset != null && num == lineByOffset.Offset && TryGetCodeBlockAttachmentForDocumentLine(lineByOffset, out var _))
				{
					mutation.InsertText = text + "\n";
				}
			}
		}
	}

	private bool HasValidCodeBlockAttachmentPlaceholders()
	{
		if (EditBox?.Document == null)
		{
			return true;
		}
		for (int i = 1; i <= EditBox.Document.LineCount; i++)
		{
			DocumentLine lineByNumber = EditBox.Document.GetLineByNumber(i);
			string text = ((lineByNumber == null) ? string.Empty : EditBox.Document.GetText(lineByNumber));
			if (text.IndexOf('\ue000') >= 0)
			{
				int lineIndex = i - 1;
				if (text != '\ue000'.ToString() || !TryGetSemanticBlockAtLine(lineIndex, out var block) || !IsCodeBlockAttachment(block))
				{
					return false;
				}
			}
		}
		return true;
	}

	private static DocumentMutation BuildExternalTextReplacement(string currentText, string targetText)
	{
		currentText = currentText ?? string.Empty;
		targetText = targetText ?? string.Empty;
		int i = 0;
		for (int num = Math.Min(currentText.Length, targetText.Length); i < num && currentText[i] == targetText[i]; i++)
		{
		}
		if (i == currentText.Length && i == targetText.Length)
		{
			return null;
		}
		int num2 = currentText.Length - 1;
		int num3 = targetText.Length - 1;
		while (num2 >= i && num3 >= i && currentText[num2] == targetText[num3])
		{
			num2--;
			num3--;
		}
		return new DocumentMutation
		{
			StartOffset = i,
			RemoveLength = num2 - i + 1,
			InsertText = targetText.Substring(i, num3 - i + 1)
		};
	}

	private static int AdjustOffsetForExternalTextReplacement(int offset, DocumentMutation mutation)
	{
		if (mutation == null)
		{
			return Math.Max(0, offset);
		}
		int num = Math.Max(0, mutation.StartOffset);
		int num2 = Math.Max(0, mutation.RemoveLength);
		int num3 = mutation.InsertText?.Length ?? 0;
		int num4 = num + num2;
		if (offset <= num)
		{
			return Math.Max(0, offset);
		}
		if (offset <= num4)
		{
			return num + num3;
		}
		return Math.Max(0, offset + num3 - num2);
	}

	private void ApplyDocumentMutations(IEnumerable<DocumentMutation> mutations)
	{
		if (mutations == null)
		{
			return;
		}
		foreach (DocumentMutation item in from mutation in mutations
			where mutation != null
			orderby mutation.StartOffset descending
			select mutation)
		{
			string text = item.InsertText ?? string.Empty;
			_needsTypedSemanticMarkRedraw |= AdjustSemanticMarkListForReplace(item.StartOffset, item.RemoveLength, text.Length);
			_needsTypedSemanticMarkRedraw |= AdjustTypedSemanticMarksForReplace(item.StartOffset, item.RemoveLength, text.Length);
			if (item.RemoveLength > 0)
			{
				EditBox.Document.Remove(item.StartOffset, item.RemoveLength);
			}
			if (text.Length > 0)
			{
				EditBox.Document.Insert(item.StartOffset, text);
			}
		}
	}

	private void ToggleLinePrefixesInSelection(Func<string, LinePrefixOperation> operationFactory)
	{
		ToggleLinePrefixesInSelection((string lineText, int _) => operationFactory?.Invoke(lineText));
	}

	private void ToggleLinePrefixesInSelection(Func<string, int, LinePrefixOperation> operationFactory)
	{
		if (operationFactory == null)
		{
			return;
		}
		List<DocumentLine> selectedDocumentLines = GetSelectedDocumentLines();
		if (selectedDocumentLines.Count == 0 || ContainsCodeBlockLine(selectedDocumentLines))
		{
			return;
		}
		List<LineMutation> list = new List<LineMutation>();
		int num = 1;
		foreach (DocumentLine item in selectedDocumentLines)
		{
			string text = EditBox.Document.GetText(item);
			LinePrefixOperation linePrefixOperation = operationFactory(text, num);
			if (linePrefixOperation == null)
			{
				if (!string.IsNullOrWhiteSpace(text))
				{
					num++;
				}
				continue;
			}
			list.Add(new LineMutation
			{
				LineOffset = item.Offset,
				StartOffset = item.Offset + linePrefixOperation.RemoveOffset,
				RemoveLength = linePrefixOperation.RemoveLength,
				InsertText = (linePrefixOperation.InsertText ?? string.Empty)
			});
			if (!string.IsNullOrWhiteSpace(text))
			{
				num++;
			}
		}
		if (list.Count != 0)
		{
			ApplyLinePrefixMutations(list);
		}
	}

	private bool ContainsCodeBlockLine(IEnumerable<DocumentLine> lines)
	{
		if (lines == null)
		{
			return false;
		}
		foreach (DocumentLine line in lines)
		{
			if (line != null && IsCodeBlockLine(line.LineNumber - 1))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsCodeBlockLine(int lineIndex)
	{
		if (lineIndex >= 0 && TryGetSemanticBlockAtLine(lineIndex, out var block))
		{
			if (block == null)
			{
				return false;
			}
			return block.Kind == MarkdownBlockKind.CodeBlock;
		}
		return false;
	}

	private void ApplyLinePrefixMutations(List<LineMutation> mutations)
	{
		if (mutations == null || mutations.Count == 0)
		{
			return;
		}
		int selectionStart = EditBox.SelectionStart;
		int selectionLength = EditBox.SelectionLength;
		int num = selectionStart + selectionLength;
		int caretOffset = EditBox.CaretOffset;
		int caretLineOffset = EditBox.Document.GetLineByOffset(caretOffset)?.Offset ?? mutations[0].LineOffset;
		LineMutation lineMutation = mutations.FirstOrDefault((LineMutation mutation) => mutation.LineOffset == caretLineOffset) ?? mutations[0];
		int num2 = mutations.Where((LineMutation mutation) => mutation.LineOffset < caretLineOffset).Sum((LineMutation mutation) => mutation.InsertText.Length - mutation.RemoveLength);
		int num3 = Math.Max(lineMutation.StartOffset + lineMutation.InsertText.Length, caretOffset + num2 + (lineMutation.InsertText.Length - lineMutation.RemoveLength));
		ApplyDocumentMutationsAndNotify(explicitCaretOffset: Math.Max(0, Math.Min((caretOffset < lineMutation.StartOffset) ? (caretOffset + num2) : ((caretOffset - lineMutation.StartOffset > lineMutation.RemoveLength) ? (caretOffset + num2 + (lineMutation.InsertText.Length - lineMutation.RemoveLength)) : (lineMutation.StartOffset + lineMutation.InsertText.Length)), EditBox.Text.Length)), mutations: mutations.Select((LineMutation mutation) => new DocumentMutation
		{
			StartOffset = mutation.StartOffset,
			RemoveLength = mutation.RemoveLength,
			InsertText = mutation.InsertText
		}));
		if (num > selectionStart)
		{
			int num4 = mutations.Sum((LineMutation mutation) => mutation.InsertText.Length - mutation.RemoveLength);
			EditBox.Select(selectionStart, Math.Max(0, selectionLength + num4));
		}
	}

	private static bool TryGetCodeBlockLineIndentMutation(int lineOffset, string lineText, int contentOffset, bool outdent, out LineMutation mutation)
	{
		mutation = null;
		string text = lineText ?? string.Empty;
		int num = Math.Max(0, Math.Min(contentOffset, text.Length));
		if (outdent)
		{
			int removableCodeBlockIndentLength = GetRemovableCodeBlockIndentLength(text, num);
			if (removableCodeBlockIndentLength <= 0)
			{
				return false;
			}
			mutation = new LineMutation
			{
				LineOffset = lineOffset,
				StartOffset = lineOffset + num,
				RemoveLength = removableCodeBlockIndentLength,
				InsertText = string.Empty
			};
			return true;
		}
		mutation = new LineMutation
		{
			LineOffset = lineOffset,
			StartOffset = lineOffset + num,
			RemoveLength = 0,
			InsertText = "    "
		};
		return true;
	}

	private static int GetRemovableCodeBlockIndentLength(string lineText, int contentOffset)
	{
		string text = lineText ?? string.Empty;
		int num = Math.Max(0, Math.Min(contentOffset, text.Length));
		if (num >= text.Length)
		{
			return 0;
		}
		if (text[num] == '\t')
		{
			return 1;
		}
		int i;
		for (i = 0; num + i < text.Length && text[num + i] == ' ' && i < 4; i++)
		{
		}
		return i;
	}

	private List<DocumentLine> GetSelectedDocumentLines()
	{
		int num = ((EditBox.SelectionLength > 0) ? EditBox.SelectionStart : EditBox.CaretOffset);
		int val = ((EditBox.SelectionLength > 0) ? (EditBox.SelectionStart + EditBox.SelectionLength) : EditBox.CaretOffset);
		DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(num);
		DocumentLine lineByOffset2 = EditBox.Document.GetLineByOffset(Math.Max(0, Math.Min(Math.Max(num, val), EditBox.Document.TextLength)));
		List<DocumentLine> list = new List<DocumentLine>();
		if (lineByOffset == null || lineByOffset2 == null)
		{
			return list;
		}
		for (int i = lineByOffset.LineNumber; i <= lineByOffset2.LineNumber; i++)
		{
			DocumentLine lineByNumber = EditBox.Document.GetLineByNumber(i);
			if (lineByNumber != null)
			{
				list.Add(lineByNumber);
			}
		}
		return list;
	}

	private static bool TryGetLineStyleRemoval(string lineText, out int removalOffset, out int removalLength)
	{
		removalOffset = 0;
		removalLength = 0;
		if (string.IsNullOrEmpty(lineText))
		{
			return false;
		}
		string text = NormalizeDisplayTaskMarkerLine(lineText);
		if (MarkdownBlockSyntax.IsSplitLine(text))
		{
			removalOffset = lineText.Length - lineText.TrimStart().Length;
			removalLength = (StartsWithAt(lineText, removalOffset, "\u200c\u200d\u2060") ? "\u200c\u200d\u2060".Length : (text.Length - removalOffset));
			return true;
		}
		if (lineText.StartsWith("> ") || lineText.StartsWith("\u2061\u2062"))
		{
			removalLength = (lineText.StartsWith("\u2061\u2062", StringComparison.Ordinal) ? "\u2061\u2062".Length : "> ".Length);
			return true;
		}
		if (EditorUtilities.IsListTypeText(lineText, out var offset, out var length))
		{
			removalOffset = Math.Max(0, offset);
			removalLength = length;
			return removalLength > 0;
		}
		if (text.StartsWith("```", StringComparison.Ordinal))
		{
			removalOffset = lineText.Length - lineText.TrimStart().Length;
			removalLength = "```".Length;
			return true;
		}
		if (lineText.StartsWith("    "))
		{
			removalLength = 4;
			return true;
		}
		if (lineText.StartsWith("\t"))
		{
			removalLength = 1;
			return true;
		}
		return false;
	}

	private void ReplaceLinePrefix(int startOffset, int removedLength, string insertedText, int caretOffset)
	{
		insertedText = insertedText ?? string.Empty;
		ApplyDocumentMutationsAndNotify(new DocumentMutation[1]
		{
			new DocumentMutation
			{
				StartOffset = startOffset,
				RemoveLength = removedLength,
				InsertText = insertedText
			}
		}, caretOffset);
	}

	public void FixCurrentIndex()
	{
		DocumentLine lineByOffset = EditBox.TextArea.Document.GetLineByOffset(EditBox.CaretOffset);
		if (EditBox.CaretOffset != lineByOffset.Offset && AttachmentEndSet.Contains(lineByOffset.EndOffset))
		{
			EditBox.CaretOffset = lineByOffset.EndOffset;
		}
	}

	public bool BetweenCode(int offset)
	{
		if ((!TryGetSemanticBlockAtOffset(offset, out var block) || block == null || block.Kind != MarkdownBlockKind.CodeBlock) && !CodeDict.Values.Any((List<Point> points) => points?.Any((Point p) => (double)offset >= p.X && (double)offset < p.Y) ?? false))
		{
			return GetSemanticMarkList().Any((MarkdownInlineDisplaySegment mark) => mark.Kind == MarkdownMarkKind.Code && offset >= mark.SourcePosition && offset < mark.SourcePosition + mark.SourceLength);
		}
		return true;
	}

	public void FocusEnd()
	{
		FocusEditBox();
		EditBox.CaretOffset = Text.Length;
	}

	public void FocusFirst()
	{
		FocusEditBox();
		EditBox.CaretOffset = 0;
	}

	public void FocusEditBox()
	{
		EditBox.TryFocus();
	}

	public void RetainLastSelectionForToolbarInteraction()
	{
		if (EditBox?.Document == null)
		{
			return;
		}
		_retainSelectionForToolbarInteraction = true;
		bool num = EditBox.SelectionLength > 0;
		if (num)
		{
			RecordLastInsertRangeFromEditor();
		}
		bool flag = num || !_currentFocused || (_customEditorPopup?.IsOpen ?? false);
		int selectionStart = (flag ? _lastInsertOffset : 0);
		int selectionLength = (flag ? _lastInsertSelectionLength : 0);
		int activeOffset = (flag ? _lastInsertActiveOffset : 0);
		TryRetainLastSelectionForToolbarInteraction(selectionStart, selectionLength, activeOffset);
		if (base.IsInitialized || base.IsLoaded)
		{
			base.Dispatcher?.BeginInvoke((Action)delegate
			{
				TryRetainLastSelectionForToolbarInteraction(selectionStart, selectionLength, activeOffset);
			}, DispatcherPriority.Input);
			base.Dispatcher?.BeginInvoke((Action)delegate
			{
				TryRetainLastSelectionForToolbarInteraction(selectionStart, selectionLength, activeOffset);
			}, DispatcherPriority.ContextIdle);
			base.Dispatcher?.BeginInvoke(new Action(ClearToolbarSelectionRetention), DispatcherPriority.ApplicationIdle);
		}
		else
		{
			ClearToolbarSelectionRetention();
		}
	}

	private void ClearToolbarSelectionRetention()
	{
		_retainSelectionForToolbarInteraction = false;
	}

	private void TryRetainLastSelectionForToolbarInteraction(int selectionStart, int selectionLength, int activeOffset)
	{
		if (EditBox?.Document != null)
		{
			if (EditBox.SelectionLength <= 0 && selectionLength > 0)
			{
				RestoreEditorSelection(selectionStart, selectionLength, activeOffset);
			}
			if (EditBox.SelectionLength > 0)
			{
				RequestImmediateRedrawForDocumentRange(EditBox.SelectionStart, EditBox.SelectionStart + EditBox.SelectionLength);
			}
		}
	}

	public void TryAddNewLine()
	{
		string text = EditBox.Text ?? string.Empty;
		Match match = AttachmentRegex.Match(text);
		if (match.Success && match.Index == 0 && match.Length == text.Length && TryGetRenderableAttachmentKind(text, match, out var _))
		{
			ReplaceEditorText(0, 0, "\r\n", 0);
			FocusFirst();
		}
		else
		{
			FocusEditBox();
		}
	}

	public bool IsStyleMenuOpen()
	{
		if (_styleMenuPopup != null)
		{
			return _styleMenuPopup.IsOpen;
		}
		return false;
	}

	private void LogPositionsIncremental(string text, int startOffset, int endOffset)
	{
		RefreshEditingDerivedMetadataWithoutMarkdownParsing();
	}

	private void LogEmojiPositionIncremental(string text, int startOffset, int endOffset)
	{
		Dictionary<int, string> dictionary = EmojiDict.Where((KeyValuePair<int, string> kv) => kv.Key < startOffset || kv.Key > endOffset).ToDictionary((KeyValuePair<int, string> kv) => kv.Key, (KeyValuePair<int, string> kv) => kv.Value);
		EmojiDict.Clear();
		foreach (KeyValuePair<int, string> item in dictionary)
		{
			EmojiDict[item.Key] = item.Value;
		}
		string input = text.Substring(startOffset, endOffset - startOffset);
		foreach (Match item2 in EmojiData.MatchOne2.Matches(input))
		{
			int adjustedIndex = item2.Index + startOffset;
			if (!LinkNameDict.Any((KeyValuePair<int, LinkInfo> pair) => adjustedIndex >= pair.Key && adjustedIndex <= pair.Key + pair.Value.Link.Length))
			{
				EmojiDict[adjustedIndex] = item2.Value;
			}
		}
	}

	private void LogCodeRectsIncremental(string text, int startOffset, int endOffset)
	{
		LogCodeRects(text);
	}

	private void InitEditorPopup()
	{
		if (_customEditorPopup == null)
		{
			_customEditorPopup = new Popup
			{
				StaysOpen = true,
				AllowsTransparency = true,
				Placement = PlacementMode.Relative,
				PopupAnimation = PopupAnimation.Fade,
				PlacementTarget = this,
				Child = EditorMenu
			};
			_customEditorPopup.MouseWheel -= _customEditorPopup_MouseWheel;
			_customEditorPopup.MouseWheel += _customEditorPopup_MouseWheel;
			_customEditorPopup.Closed -= OnEditorDismissed;
			_customEditorPopup.Closed += OnEditorDismissed;
		}
	}

	private void _customEditorPopup_MouseWheel(object sender, MouseWheelEventArgs e)
	{
		TryDismissEditorMenu();
	}

	private async void OnEditorDismissed(object sender, EventArgs e)
	{
		DetachCustomEditorPopupDismissHandlers();
		EditorMenu.DismissSecondLevelPopups();
		await Task.Delay(100);
		_customEditorMenuIsOpen = false;
	}

	private void AttachCustomEditorPopupDismissHandlers()
	{
		DetachCustomEditorPopupDismissHandlers();
		_customEditorPopupOwnerWindow = Window.GetWindow(this) ?? System.Windows.Application.Current?.Windows.OfType<Window>().FirstOrDefault((Window window) => window.IsActive) ?? System.Windows.Application.Current?.MainWindow;
		if (_customEditorPopupOwnerWindow != null)
		{
			_customEditorPopupOwnerWindow.PreviewMouseDown += OnCustomEditorPopupOwnerPreviewMouseDown;
			_customEditorPopupOwnerWindow.Deactivated += OnCustomEditorPopupOwnerDeactivated;
			_customEditorPopupOwnerWindow.StateChanged += OnCustomEditorPopupOwnerStateChanged;
		}
	}

	private void DetachCustomEditorPopupDismissHandlers()
	{
		if (_customEditorPopupOwnerWindow != null)
		{
			_customEditorPopupOwnerWindow.PreviewMouseDown -= OnCustomEditorPopupOwnerPreviewMouseDown;
			_customEditorPopupOwnerWindow.Deactivated -= OnCustomEditorPopupOwnerDeactivated;
			_customEditorPopupOwnerWindow.StateChanged -= OnCustomEditorPopupOwnerStateChanged;
			_customEditorPopupOwnerWindow = null;
		}
	}

	private void OnCustomEditorPopupOwnerPreviewMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (_customEditorPopup == null || !_customEditorPopup.IsOpen)
		{
			return;
		}
		DependencyObject source = e.OriginalSource as DependencyObject;
		if (!EditorMenu.IsInteractiveSource(source))
		{
			if (IsEditorToolbarSource(source))
			{
				RetainLastSelectionForToolbarInteraction();
				DismissEditorMenuAfterToolbarInteraction();
			}
			else
			{
				TryDismissEditorMenu();
			}
		}
	}

	private static bool IsEditorToolbarSource(DependencyObject source)
	{
		while (source != null)
		{
			if (source is EditorMenu)
			{
				return true;
			}
			DependencyObject dependencyObject = null;
			if (source is FrameworkElement frameworkElement)
			{
				if (IsEditorToolbarElementName(frameworkElement.Name))
				{
					return true;
				}
				dependencyObject = frameworkElement.Parent;
			}
			else if (source is FrameworkContentElement frameworkContentElement)
			{
				if (IsEditorToolbarElementName(frameworkContentElement.Name))
				{
					return true;
				}
				dependencyObject = frameworkContentElement.Parent;
			}
			if (dependencyObject == null)
			{
				try
				{
					dependencyObject = VisualTreeHelper.GetParent(source);
				}
				catch (InvalidOperationException)
				{
					dependencyObject = null;
				}
				catch (ArgumentException)
				{
					dependencyObject = null;
				}
			}
			source = dependencyObject;
		}
		return false;
	}

	private static bool IsEditorToolbarElementName(string name)
	{
		if (!(name == "EditorIcon") && !(name == "EditButton"))
		{
			return name == "EditIconSelectedBorder";
		}
		return true;
	}

	private void DismissEditorMenuAfterToolbarInteraction()
	{
		base.Dispatcher?.BeginInvoke(new Action(TryDismissEditorMenu), DispatcherPriority.ContextIdle);
	}

	private void OnCustomEditorPopupOwnerDeactivated(object sender, EventArgs e)
	{
		TryDismissEditorMenu();
	}

	private void OnCustomEditorPopupOwnerStateChanged(object sender, EventArgs e)
	{
		if (sender is Window { WindowState: WindowState.Minimized })
		{
			TryDismissEditorMenu();
		}
	}

	private void InitStyleMenuPopup()
	{
		if (_styleMenuPopup == null)
		{
			_styleMenuPopup = new Popup
			{
				StaysOpen = true,
				AllowsTransparency = true,
				Placement = PlacementMode.Relative,
				PopupAnimation = PopupAnimation.Fade,
				PlacementTarget = this,
				Child = StyleMenu
			};
			_styleMenuPopup.Closed -= OnStyleMenuClosed;
			_styleMenuPopup.Closed += OnStyleMenuClosed;
			StyleMenu.EditorStyleAction -= OnStyleClick;
			StyleMenu.EditorStyleAction += OnStyleClick;
			StyleMenu.TryDismiss -= DismissStyleMenu;
			StyleMenu.TryDismiss += DismissStyleMenu;
		}
	}

	private void OnStyleMenuClosed(object sender, EventArgs e)
	{
		StyleMenu.Reset();
	}

	private void DismissStyleMenu(object sender, EventArgs e)
	{
		DismissStyleMenu();
	}

	private void OnStyleClick(object sender, string e)
	{
		DismissStyleMenu();
	}

	private void TryShowStyleMenu()
	{
		if (BetweenCode(EditBox.CaretOffset) || !StyleEnabled)
		{
			return;
		}
		string text = TryGetMark();
		if (string.IsNullOrEmpty(text))
		{
			if (_styleMenuPopup != null)
			{
				_styleMenuPopup.IsOpen = false;
			}
		}
		else if (text == "/" || text == "、")
		{
			string text2 = _filterContent;
			int num = Math.Max(_filterContent.IndexOf("/", StringComparison.Ordinal), _filterContent.IndexOf("、", StringComparison.Ordinal));
			if (num > 0)
			{
				text2 = text2.Substring(num + 1);
			}
			bool flag = false;
			if (text == "/" && EditBox.CaretOffset - 2 >= 0 && EditBox.Text[EditBox.CaretOffset - 2] == ' ')
			{
				flag = true;
			}
			UserActCollectUtils.AddClickEvent("slash_om", "method", (text == "、") ? "comma" : (flag ? "space_slash" : "slash"));
			string currentParagraphStyle = GetCurrentParagraphStyle();
			currentParagraphStyle = ((!(currentParagraphStyle == "Text") && !string.IsNullOrEmpty(currentParagraphStyle)) ? ("_" + currentParagraphStyle) : string.Empty);
			ShowStyleMenu(text2, currentParagraphStyle, fromSplash: true, _markIndex);
		}
	}

	private void DismissStyleMenu()
	{
		if (_styleMenuPopup != null)
		{
			_styleMenuPopup.IsOpen = false;
		}
	}

	public void InsertTag()
	{
		EditBox.AddText("#", focused: true);
	}

	public void InsertLinkTask()
	{
		EditBox.AddText("[[", focused: true);
	}

	public void InsertSplash()
	{
		EditBox.AddText("/", focused: true);
	}

	private int GetMaxHeadingLevel()
	{
		int num = 0;
		foreach (MarkdownBlockNode semanticBlock in GetSemanticBlocks())
		{
			if (semanticBlock != null && semanticBlock.Kind == MarkdownBlockKind.Heading && semanticBlock.Level.HasValue)
			{
				num = Math.Max(num, Math.Max(1, Math.Min(6, semanticBlock.Level ?? 1)));
			}
		}
		return num;
	}

	public async void ShowStyleMenu(string filterContent, string action = "", bool fromSplash = true, int markIndex = -1)
	{
		if (ReadOnly || (_customEditorPopup != null && _customEditorPopup.IsOpen))
		{
			return;
		}
		if (_styleMenuPopup != null && _styleMenuPopup.IsOpen)
		{
			UpdateStyleMenu(filterContent, action, fromSplash);
			if (filterContent.Length >= 6 && StyleMenu.MatchCount == 0)
			{
				_styleMenuPopup.IsOpen = false;
			}
			return;
		}
		InitStyleMenuPopup();
		UpdateStyleMenu(filterContent, action, fromSplash);
		if (_styleMenuPopup == null)
		{
			return;
		}
		if (filterContent.Length >= 6 && StyleMenu.MatchCount == 0)
		{
			_styleMenuPopup.IsOpen = false;
			return;
		}
		if (!fromSplash)
		{
			VisualLine visualLine = EditBox.TextArea.TextView.GetVisualLine(EditBox.TextArea.Caret.Line);
			if (visualLine != null)
			{
				_styleMenuPopup.HorizontalOffset = -206.0;
				_styleMenuPopup.VerticalOffset = visualLine.VisualTop + visualLine.Height - 12.0;
			}
		}
		else
		{
			Point popupOffset = GetPopupOffset();
			_styleMenuPopup.HorizontalOffset = popupOffset.X + 18.0;
			_styleMenuPopup.VerticalOffset = popupOffset.Y - 10.0;
		}
		UserActCollectUtils.AddClickEvent("slash_om", "show", "detail");
		await Task.Delay(50);
		_styleMenuPopup.IsOpen = true;
		_styleMenuPopup.StaysOpen = true;
		await Task.Delay(200);
		_styleMenuPopup.StaysOpen = false;
	}

	private void UpdateStyleMenu(string filterContent, string action, bool fromSplash)
	{
		if (!string.IsNullOrEmpty(filterContent))
		{
			StyleMenu.UpdateContext(action, GetMaxHeadingLevel(), CheckIfAllowAddSubtask(), forceShow: false);
			StyleMenu.FilterContent = filterContent;
			StyleMenu.FromSplash = fromSplash;
		}
		else
		{
			StyleMenu.Reset();
			StyleMenu.UpdateContext(action, GetMaxHeadingLevel(), CheckIfAllowAddSubtask());
			StyleMenu.FromSplash = fromSplash;
			StyleMenu.EmptyVisibility = Visibility.Collapsed;
		}
		StyleMenu.SelectFirst();
	}

	private bool CheckIfAllowAddSubtask()
	{
		if (InImmersiveMode)
		{
			return false;
		}
		TaskBaseViewModel taskBaseModel = GetTaskBaseModel();
		if (taskBaseModel != null && taskBaseModel.CreatedTime.HasValue && !taskBaseModel.IsNote && TaskDao.GetTaskLevel(taskBaseModel.GetTaskId(), taskBaseModel.ProjectId) < 4)
		{
			return true;
		}
		return false;
	}

	public string GetRelatedText()
	{
		string fullText = NormalizeDisplayTaskMarkersForPersistence(EditBox.Text);
		string selectedText = NormalizeDisplayTaskMarkersForPersistence(EditBox.SelectedText);
		int persistedOffset = GetPersistedOffset(EditBox.CaretOffset);
		int persistedOffset2 = GetPersistedOffset(EditBox.SelectionStart);
		int persistedOffset3 = GetPersistedOffset(EditBox.SelectionStart + EditBox.SelectionLength);
		return MarkdownUtils.GetRelatedText(EditBox.Document, fullText, persistedOffset, persistedOffset2, Math.Max(0, persistedOffset3 - persistedOffset2), selectedText);
	}

	public string GetCurrentParagraphStyle()
	{
		if (TryGetCurrentSemanticParagraphAction(out var semanticAction))
		{
			return semanticAction;
		}
		string fullText = NormalizeDisplayTaskMarkersForPersistence(EditBox.Text);
		string selectedText = NormalizeDisplayTaskMarkersForPersistence(EditBox.SelectedText);
		int persistedOffset = GetPersistedOffset(EditBox.CaretOffset);
		int persistedOffset2 = GetPersistedOffset(EditBox.SelectionStart);
		int persistedOffset3 = GetPersistedOffset(EditBox.SelectionStart + EditBox.SelectionLength);
		return MarkdownUtils.GetParagraphType(EditBox.Document, fullText, persistedOffset, persistedOffset2, Math.Max(0, persistedOffset3 - persistedOffset2), selectedText);
	}

	private bool TryGetCurrentSemanticParagraphAction(out string semanticAction)
	{
		semanticAction = string.Empty;
		if (EditBox?.Document == null)
		{
			return false;
		}
		if (EditBox.SelectionLength <= 0)
		{
			if (!TryGetSemanticBlockAtOffset(EditBox.CaretOffset, out var block))
			{
				return false;
			}
			semanticAction = GetParagraphActionFromSemanticBlock(block);
			return !string.IsNullOrEmpty(semanticAction);
		}
		int textLength = EditBox.Document.TextLength;
		int num = Math.Max(0, Math.Min(EditBox.SelectionStart, textLength));
		int num2 = Math.Max(num, Math.Min(EditBox.SelectionStart + EditBox.SelectionLength, textLength));
		if (num2 <= num || !TryGetCommonSemanticParagraphActionForSelection(num, num2, out semanticAction))
		{
			return false;
		}
		return true;
	}

	private bool TryGetCommonSemanticParagraphActionForSelection(int selectionStart, int selectionEnd, out string semanticAction)
	{
		semanticAction = string.Empty;
		TextDocument textDocument = EditBox?.Document;
		if (textDocument == null || selectionEnd <= selectionStart)
		{
			return false;
		}
		DocumentLine lineByOffset = textDocument.GetLineByOffset(selectionStart);
		DocumentLine lineByOffset2 = textDocument.GetLineByOffset(Math.Max(selectionStart, selectionEnd - 1));
		if (lineByOffset == null || lineByOffset2 == null)
		{
			return false;
		}
		for (int i = lineByOffset.LineNumber; i <= lineByOffset2.LineNumber; i++)
		{
			if (!TryGetSemanticBlockAtLine(i - 1, out var block))
			{
				return false;
			}
			string paragraphActionFromSemanticBlock = GetParagraphActionFromSemanticBlock(block);
			if (string.IsNullOrEmpty(paragraphActionFromSemanticBlock))
			{
				return false;
			}
			if (string.IsNullOrEmpty(semanticAction))
			{
				semanticAction = paragraphActionFromSemanticBlock;
			}
			else if (!string.Equals(semanticAction, paragraphActionFromSemanticBlock, StringComparison.Ordinal))
			{
				semanticAction = string.Empty;
				return false;
			}
		}
		return !string.IsNullOrEmpty(semanticAction);
	}

	private static string GetParagraphActionFromSemanticBlock(MarkdownBlockNode block)
	{
		if (block == null)
		{
			return string.Empty;
		}
		return block.Kind switch
		{
			MarkdownBlockKind.Heading => Math.Max(1, Math.Min(6, block.Level ?? 1)) switch
			{
				1 => "Heading1", 
				2 => "Heading2", 
				3 => "Heading3", 
				4 => "Heading4", 
				5 => "Heading5", 
				_ => "Heading6", 
			}, 
			MarkdownBlockKind.BlockQuote => "Quote", 
			MarkdownBlockKind.List => block.ListKind switch
			{
				MarkdownListKind.Task => "CheckItem", 
				MarkdownListKind.Numbered => "NumberedList", 
				_ => "BulletList", 
			}, 
			_ => string.Empty, 
		};
	}

	public List<string> GetCurrentEditorActions()
	{
		HashSet<string> hashSet = new HashSet<string>();
		string currentParagraphStyle = GetCurrentParagraphStyle();
		if (!string.IsNullOrEmpty(currentParagraphStyle) && currentParagraphStyle != "Text")
		{
			hashSet.Add(currentParagraphStyle);
		}
		int selectionStart = EditBox.SelectionStart;
		int selectionEnd = EditBox.SelectionStart + EditBox.SelectionLength;
		bool flag = EditBox.SelectionLength > 0;
		int caretOffset = EditBox.CaretOffset;
		int caretOffset2 = (flag ? caretOffset : GetInsertionOffsetOutsideLinkDisplayBoundary(caretOffset));
		bool flag2 = !flag && IsCaretInsideSemanticCodeBlock(caretOffset);
		bool flag3 = flag && DoesSelectionIntersectSemanticCodeBlock(selectionStart, selectionEnd);
		AddSemanticMarkAction(hashSet, MarkdownMarkKind.Strong, "Bold", caretOffset, selectionStart, selectionEnd, flag);
		AddSemanticMarkAction(hashSet, MarkdownMarkKind.Emphasis, "Italic", caretOffset, selectionStart, selectionEnd, flag);
		AddSemanticMarkAction(hashSet, MarkdownMarkKind.Underline, "UnderLine", caretOffset, selectionStart, selectionEnd, flag);
		AddSemanticMarkAction(hashSet, MarkdownMarkKind.Strikethrough, "StrikeThrough", caretOffset, selectionStart, selectionEnd, flag);
		AddSemanticMarkAction(hashSet, MarkdownMarkKind.Highlight, "HighLight", caretOffset, selectionStart, selectionEnd, flag);
		if (!flag)
		{
			AddTypingStateAction(hashSet, MarkdownMarkKind.Strong, "Bold");
			AddTypingStateAction(hashSet, MarkdownMarkKind.Emphasis, "Italic");
			AddTypingStateAction(hashSet, MarkdownMarkKind.Underline, "UnderLine");
			AddTypingStateAction(hashSet, MarkdownMarkKind.Strikethrough, "StrikeThrough");
			AddTypingStateAction(hashSet, MarkdownMarkKind.Highlight, "HighLight");
			if (!flag2)
			{
				AddTypingStateAction(hashSet, MarkdownMarkKind.Code, "Code");
			}
		}
		bool num;
		if (!flag)
		{
			if (!flag2)
			{
				num = IsInlineCodeCaretActive(caretOffset);
				goto IL_0178;
			}
		}
		else if (!flag3)
		{
			num = IsInlineCodeRangeActive(selectionStart, selectionEnd);
			goto IL_0178;
		}
		goto IL_0186;
		IL_0186:
		if (!flag)
		{
			if (IsCollapsedCaretInsideSemanticLinkRange(caretOffset2))
			{
				hashSet.Add("Link");
			}
		}
		else
		{
			foreach (KeyValuePair<int, LinkInfo> item in LinkNameDict)
			{
				if (IsLinkRangeActive(item.Key, item.Value.Link?.Length ?? 0, caretOffset2, selectionStart, selectionEnd, flag))
				{
					hashSet.Add("Link");
					break;
				}
			}
			if (!hashSet.Contains("Link"))
			{
				foreach (KeyValuePair<int, LinkInfo> item2 in LinkUrlDict)
				{
					if (IsLinkRangeActive(item2.Key, item2.Value.Url?.Length ?? 0, caretOffset2, selectionStart, selectionEnd, flag))
					{
						hashSet.Add("Link");
						break;
					}
				}
			}
		}
		return hashSet.ToList();
		IL_0178:
		if (num)
		{
			hashSet.Add("Code");
		}
		goto IL_0186;
	}

	public string GetCurrentHighlightContextColor()
	{
		int selectionStart = EditBox.SelectionStart;
		int selectionEnd = EditBox.SelectionStart + EditBox.SelectionLength;
		bool num = EditBox.SelectionLength > 0;
		int caretOffset = EditBox.CaretOffset;
		if (num)
		{
			return GetSelectionHighlightContextColor(selectionStart, selectionEnd);
		}
		if (IsTypingMarkSuppressedAtOffset(MarkdownMarkKind.Highlight, caretOffset))
		{
			return null;
		}
		if (_activeTypingMarks.Contains(MarkdownMarkKind.Highlight) && _activeTypingHighlightOffset == caretOffset && !string.IsNullOrWhiteSpace(_activeTypingHighlightColor))
		{
			return HighlightColorMenuState.GetEditorColor(_activeTypingHighlightColor);
		}
		if (TryGetBackspaceRetainedHighlightColor(caretOffset, out var highlightColor))
		{
			return highlightColor;
		}
		if (!TryGetHighlightContextColorAtOffset(caretOffset, out var highlightColor2))
		{
			return null;
		}
		return highlightColor2;
	}

	private string GetSelectionHighlightContextColor(int selectionStart, int selectionEnd)
	{
		int safeStart = Math.Min(selectionStart, selectionEnd);
		int safeEnd = Math.Max(selectionStart, selectionEnd);
		return (from mark in GetSemanticMarkList()
			where mark.Kind == MarkdownMarkKind.Highlight && !string.IsNullOrWhiteSpace(mark.Color) && mark.ContentLength > 0
			select new
			{
				Color = HighlightColorMenuState.GetEditorColor(mark.Color),
				Start = mark.ContentPosition,
				End = mark.ContentPosition + mark.ContentLength
			} into mark
			where !string.IsNullOrWhiteSpace(mark.Color) && mark.End > safeStart && mark.Start < safeEnd
			orderby Math.Max(mark.Start, safeStart), mark.Start
			select mark.Color).FirstOrDefault();
	}

	private void AddSemanticMarkAction(HashSet<string> actions, MarkdownMarkKind kind, string action, int caretOffset, int selectionStart, int selectionEnd, bool hasSelection)
	{
		if (!actions.Contains(action) && IsSemanticMarkActive(kind, caretOffset, selectionStart, selectionEnd, hasSelection))
		{
			actions.Add(action);
		}
	}

	private void AddTypingStateAction(HashSet<string> actions, MarkdownMarkKind kind, string action)
	{
		if (!actions.Contains(action) && IsTypingMarkActive(kind))
		{
			actions.Add(action);
		}
	}

	private bool IsSemanticMarkActive(MarkdownMarkKind kind, int caretOffset, int selectionStart, int selectionEnd, bool hasSelection)
	{
		if (hasSelection)
		{
			return IsSelectionFullyCoveredBySemanticMarks(kind, selectionStart, selectionEnd);
		}
		if (IsTypingMarkSuppressedAtOffset(kind, caretOffset))
		{
			return false;
		}
		foreach (MarkdownInlineDisplaySegment semanticMark in GetSemanticMarkList())
		{
			if (semanticMark.Kind == kind && IsCollapsedCaretActiveInSemanticMark(semanticMark, caretOffset))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsCollapsedCaretActiveInSemanticMark(MarkdownInlineDisplaySegment mark, int caretOffset)
	{
		if (mark == null || mark.SourceLength <= 0)
		{
			return false;
		}
		int sourcePosition = mark.SourcePosition;
		int num = mark.SourcePosition + mark.SourceLength;
		if (IsInlineTypingContextMark(mark))
		{
			return IsInlineTypingContextActive(mark, caretOffset, 0);
		}
		if (caretOffset >= sourcePosition)
		{
			return caretOffset < num;
		}
		return false;
	}

	private bool IsSelectionFullyCoveredBySemanticMarks(MarkdownMarkKind kind, int selectionStart, int selectionEnd)
	{
		return IsSelectionCoveredIgnoringWhitespaceByRanges(from mark in GetSemanticMarkList()
			where mark.Kind == kind
			select new ActiveRange(mark.SourcePosition, mark.SourcePosition + mark.SourceLength), selectionStart, selectionEnd, EditBox?.Text ?? Text);
	}

	private static bool IsRangeActive(int start, int length, int caretOffset, int selectionStart, int selectionEnd, bool hasSelection)
	{
		if (length <= 0)
		{
			return false;
		}
		int num = start + length;
		if (hasSelection)
		{
			return IsSelectionFullyCoveredByRange(start, num, selectionStart, selectionEnd);
		}
		if (caretOffset >= start)
		{
			return caretOffset < num;
		}
		return false;
	}

	private static bool IsLinkRangeActive(int start, int length, int caretOffset, int selectionStart, int selectionEnd, bool hasSelection)
	{
		if (length <= 0)
		{
			return false;
		}
		int num = start + length;
		if (hasSelection)
		{
			return IsSelectionFullyCoveredByRange(start, num, selectionStart, selectionEnd);
		}
		if (caretOffset > start)
		{
			return caretOffset < num;
		}
		return false;
	}

	private bool IsCollapsedCaretInsideSemanticLinkRange(int caretOffset)
	{
		foreach (MarkdownInlineDisplaySegment semanticMark in GetSemanticMarkList())
		{
			if (semanticMark != null && semanticMark.Kind == MarkdownMarkKind.Link && semanticMark.SourceLength > 0)
			{
				int sourcePosition = semanticMark.SourcePosition;
				int num = semanticMark.SourcePosition + semanticMark.SourceLength;
				if (caretOffset > sourcePosition && caretOffset < num)
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool IsInlineCodeCaretActive(int caretOffset)
	{
		if (!CodeDict.Values.Any((List<Point> points) => points?.Any((Point p) => (double)caretOffset >= p.X && (double)caretOffset < p.Y) ?? false))
		{
			return GetSemanticMarkList().Any((MarkdownInlineDisplaySegment mark) => mark.Kind == MarkdownMarkKind.Code && caretOffset >= mark.SourcePosition && caretOffset < mark.SourcePosition + mark.SourceLength);
		}
		return true;
	}

	private bool IsInlineCodeRangeActive(int selectionStart, int selectionEnd)
	{
		if (selectionEnd <= selectionStart)
		{
			return false;
		}
		List<ActiveRange> list = new List<ActiveRange>();
		list.AddRange(from mark in GetSemanticMarkList()
			where mark.Kind == MarkdownMarkKind.Code
			select new ActiveRange(mark.SourcePosition, mark.SourcePosition + mark.SourceLength));
		foreach (List<Point> value in CodeDict.Values)
		{
			if (value != null)
			{
				list.AddRange(value.Select((Point point) => new ActiveRange((int)point.X, (int)point.Y)));
			}
		}
		return IsSelectionFullyCoveredByRanges(list, selectionStart, selectionEnd);
	}

	private bool IsCaretInsideSemanticCodeBlock(int offset)
	{
		if (TryGetSemanticBlockAtOffset(offset, out var block))
		{
			if (block == null)
			{
				return false;
			}
			return block.Kind == MarkdownBlockKind.CodeBlock;
		}
		return false;
	}

	private bool DoesSelectionIntersectSemanticCodeBlock(int selectionStart, int selectionEnd)
	{
		if (EditBox?.Document == null)
		{
			return false;
		}
		int val = Math.Min(selectionStart, selectionEnd);
		int val2 = Math.Max(selectionStart, selectionEnd);
		val = Math.Max(0, Math.Min(val, EditBox.Document.TextLength));
		val2 = Math.Max(val, Math.Min(val2, EditBox.Document.TextLength));
		if (val2 <= val)
		{
			return false;
		}
		foreach (MarkdownBlockNode semanticBlock in GetSemanticBlocks())
		{
			if (semanticBlock != null && semanticBlock.Kind == MarkdownBlockKind.CodeBlock && TryGetSemanticBlockOffsetRange(semanticBlock, Text, out var startOffset, out var endOffset) && endOffset > val && startOffset < val2)
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsSelectionFullyCoveredByRange(int start, int end, int selectionStart, int selectionEnd)
	{
		return IsSelectionFullyCoveredByRanges(new ActiveRange[1]
		{
			new ActiveRange(start, end)
		}, selectionStart, selectionEnd);
	}

	private static bool IsSelectionFullyCoveredByRanges(IEnumerable<ActiveRange> ranges, int selectionStart, int selectionEnd)
	{
		int safeStart = Math.Min(selectionStart, selectionEnd);
		int safeEnd = Math.Max(selectionStart, selectionEnd);
		if (safeEnd <= safeStart)
		{
			return false;
		}
		int num = safeStart;
		foreach (ActiveRange item in from range in ranges
			where range.End > safeStart && range.Start < safeEnd
			orderby range.Start, range.End descending
			select range)
		{
			int num2 = Math.Max(item.Start, safeStart);
			int num3 = Math.Min(item.End, safeEnd);
			if (num3 > num2)
			{
				if (num2 > num)
				{
					return false;
				}
				if (num3 > num)
				{
					num = num3;
				}
				if (num >= safeEnd)
				{
					return true;
				}
			}
		}
		return false;
	}

	private static bool IsSelectionCoveredIgnoringWhitespaceByRanges(IEnumerable<ActiveRange> ranges, int selectionStart, int selectionEnd, string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		int safeStart = Math.Min(selectionStart, selectionEnd);
		int safeEnd = Math.Max(selectionStart, selectionEnd);
		safeStart = Math.Max(0, Math.Min(safeStart, text.Length));
		safeEnd = Math.Max(safeStart, Math.Min(safeEnd, text.Length));
		if (safeEnd <= safeStart)
		{
			return false;
		}
		int num = safeStart;
		bool result = false;
		foreach (ActiveRange item in from range in ranges
			where range.End > safeStart && range.Start < safeEnd
			orderby range.Start, range.End descending
			select range)
		{
			int num2 = Math.Max(item.Start, safeStart);
			int num3 = Math.Min(item.End, safeEnd);
			if (num3 > num2)
			{
				if (num2 > num && HasNonWhitespace(text, num, num2))
				{
					return false;
				}
				int start = Math.Max(num, num2);
				if (HasNonWhitespace(text, start, num3))
				{
					result = true;
				}
				if (num3 > num)
				{
					num = num3;
				}
			}
		}
		if (num < safeEnd && HasNonWhitespace(text, num, safeEnd))
		{
			return false;
		}
		return result;
	}

	private static bool HasNonWhitespace(string text, int start, int end)
	{
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		int num = Math.Max(0, Math.Min(start, text.Length));
		int num2 = Math.Max(num, Math.Min(end, text.Length));
		for (int i = num; i < num2; i++)
		{
			if (!char.IsWhiteSpace(text[i]))
			{
				return true;
			}
		}
		return false;
	}

	private string GetCurrentStyleMenuAction()
	{
		string currentParagraphStyle = GetCurrentParagraphStyle();
		if (currentParagraphStyle == "Text" || string.IsNullOrEmpty(currentParagraphStyle))
		{
			return string.Empty;
		}
		return "_" + currentParagraphStyle;
	}

	public void UpdateEditorMenuContext()
	{
		EditorMenu.UpdateContext(GetRelatedText(), GetCurrentParagraphStyle(), GetCurrentEditorActions(), GetMaxHeadingLevel(), CheckIfAllowAddSubtask(), GetCurrentHighlightContextColor());
	}

	private void RefreshOpenMenuContexts()
	{
		if (_styleMenuPopup != null && _styleMenuPopup.IsOpen)
		{
			bool flag = string.IsNullOrEmpty(StyleMenu.FilterContent);
			StyleMenu.UpdateContext(GetCurrentStyleMenuAction(), GetMaxHeadingLevel(), CheckIfAllowAddSubtask(), flag);
			if (flag)
			{
				StyleMenu.SelectFirst();
			}
		}
		if (_customEditorPopup != null && _customEditorPopup.IsOpen)
		{
			UpdateEditorMenuContext();
		}
		this.EditorStateChanged?.Invoke(this, EventArgs.Empty);
	}

	public void CancelTryShowEditorMenu()
	{
		try
		{
			_cancellationTokenSource.Cancel();
		}
		catch (ObjectDisposedException)
		{
		}
	}

	private async void TryShowEditorMenu()
	{
		try
		{
			_cancellationTokenSource.Cancel();
		}
		catch (ObjectDisposedException)
		{
		}
		_cancellationTokenSource = new CancellationTokenSource();
		try
		{
			await Task.Delay(120, _cancellationTokenSource.Token);
			PerformShowEditorMenu();
		}
		catch (TaskCanceledException)
		{
		}
	}

	private async Task PerformShowEditorMenu()
	{
		if (ReadOnly)
		{
			return;
		}
		if (EditBox.SelectedText.Length >= 1)
		{
			if ((_customEditorPopup != null && _customEditorMenuIsOpen) || !EditorEnabled)
			{
				return;
			}
			string text = NormalizeDisplayTaskMarkersForPersistence(EditBox.SelectedText);
			if (IsSelectedHorizontalRule(text) || ((text.StartsWith("![file](") || text.StartsWith("![image](")) && text.EndsWith(")")))
			{
				return;
			}
			if (ShouldSuppressEditorMenuForCurrentSelection())
			{
				TryDismissEditorMenu();
				return;
			}
			InitEditorPopup();
			UpdateEditorMenuContext();
			Point popupOffset = GetPopupOffset1();
			double num = EditorMenu.ActualWidth;
			if (num <= 0.0)
			{
				num = 258.0;
			}
			_customEditorPopup.HorizontalOffset = popupOffset.X - num / 2.0 + 24.0;
			_customEditorPopup.VerticalOffset = popupOffset.Y;
			_customEditorPopup.IsOpen = true;
			AttachCustomEditorPopupDismissHandlers();
			_customEditorMenuIsOpen = true;
		}
		else
		{
			TryDismissEditorMenu();
		}
	}

	private bool IsSelectedHorizontalRule(string selectedText)
	{
		selectedText = selectedText ?? string.Empty;
		if (selectedText.EndsWith("\r\n", StringComparison.Ordinal))
		{
			selectedText = selectedText.Substring(0, selectedText.Length - 2);
		}
		else if (selectedText.EndsWith("\r", StringComparison.Ordinal) || selectedText.EndsWith("\n", StringComparison.Ordinal))
		{
			selectedText = selectedText.Substring(0, selectedText.Length - 1);
		}
		if (selectedText.IndexOf('\r') >= 0 || selectedText.IndexOf('\n') >= 0 || !MarkdownBlockSyntax.IsSplitLine(selectedText))
		{
			return false;
		}
		TextDocument textDocument = EditBox?.Document;
		if (textDocument == null)
		{
			return true;
		}
		int offset = Math.Max(0, Math.Min(EditBox.SelectionStart, textDocument.TextLength));
		DocumentLine lineByOffset = textDocument.GetLineByOffset(offset);
		if (lineByOffset != null && TryGetSemanticBlockAtLine(lineByOffset.LineNumber - 1, out var block) && block != null)
		{
			return block.Kind == MarkdownBlockKind.HorizontalRule;
		}
		return true;
	}

	private bool ShouldSuppressEditorMenuForCurrentSelection()
	{
		if (EditBox?.Document == null || EditBox.SelectionLength <= 0)
		{
			return false;
		}
		return DoesSelectionIntersectSemanticCodeBlock(EditBox.SelectionStart, EditBox.SelectionStart + EditBox.SelectionLength);
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
			SelectionPopup.Opened += OnQuickPopupOpen;
			SelectionPopup.Closed += OnQuickPopupClosed;
		}
	}

	private async void TryShowQuickSetPopup()
	{
		_projectImeRefresh.Cancel();
		if (BetweenCode(EditBox.CaretOffset))
		{
			return;
		}
		string text = TryGetMark();
		if (!string.IsNullOrEmpty(text))
		{
			switch (text)
			{
			case "/":
			case "、":
				break;
			case "^":
			case "~":
				if (!_filterContent.EndsWith(" "))
				{
					if (EditBox.TextArea.IsImeCompositionActive)
					{
						RefreshProjectPopupAfterComposition();
					}
					if (!_projectPopupState.IsTriggerSuppressed(_markIndex, text, _filterContent))
					{
						await ShowProjectPopup(text);
					}
					return;
				}
				goto default;
			default:
			{
				_projectPopupState.ClearSuppressedTrigger();
				InitPopup();
				if (_selectionControl == null)
				{
					_selectionControl = new QuickSelectionControl(SelectionPopup);
					SelectionPopup.Child = _selectionControl;
					_selectionControl.PopupOpenChanged += delegate(object o, bool e)
					{
						this.PopupOpenChanged?.Invoke(this, e);
					};
					_selectionControl.ItemSelected += OnQuickItemSelect;
				}
				bool flag = IsUsedInCalendar();
				string calId = ((flag && (text == "^" || text == "~")) ? GetAccountId() : string.Empty);
				string assignee = ((text == "@") ? GetAssignee() : null);
				int priority = ((text == "!") ? GetPriority() : (-1));
				List<string> tags = ((text == "#") ? GetTags() : null);
				if (_selectionControl.SetSelectionItems(text, _filterContent, flag, calId, null, priority, tags, assignee))
				{
					Point popupOffset = GetPopupOffset();
					_selectionControl.TryShowPopup(text, _filterContent, new Point(popupOffset.X + 10.0, popupOffset.Y - 12.0));
				}
				else
				{
					_selectionControl?.ClosePopup();
				}
				return;
			}
			}
		}
		_projectPopupState.ClearSuppressedTrigger();
		if (SelectionPopup != null)
		{
			SelectionPopup.IsOpen = false;
		}
		if (SetProjectPopup != null)
		{
			SetProjectPopup.IsOpen = false;
		}
	}

	private void OnQuickItemSelect(object sender, QuickSetModel e)
	{
		if (e.Type == QuickSetType.TaskLink)
		{
			int num = Math.Min(Text.Length, Math.Max(0, _markIndex));
			string linkText = (string.IsNullOrEmpty(e.task.Title) ? Utils.GetString("MyTask") : e.task.Title);
			string url = TaskUtils.GetCopyLink(e.task.ProjectId, e.task.Id, string.Empty).Trim();
			ReplaceEditorRangeWithLinkDisplayText(num, Math.Min(_filterContent.Length + 2, Text.Length - num), linkText, url);
			FocusEditBox();
		}
		else
		{
			RemoveQuickSetText();
			this.QuickItemSelected?.Invoke(sender, e);
		}
	}

	private Point GetPopupOffset()
	{
		TextView textView = EditBox.TextArea.TextView;
		TextViewPosition position = new TextViewPosition(EditBox.Document.GetLocation((_markIndex >= 0) ? _markIndex : EditBox.CaretOffset));
		return textView.GetVisualPosition(position, VisualYPosition.LineBottom);
	}

	private Point GetPopupOffset1()
	{
		TextView textView = EditBox.TextArea.TextView;
		TextViewPosition position = new TextViewPosition(EditBox.Document.GetLocation(EditBox.CaretOffset));
		Point result = textView.GetVisualPosition(position, VisualYPosition.LineTop);
		bool flag = true;
		if (EditBox.SelectedText.Length > 0)
		{
			if (EditBox.SelectionStart != EditBox.CaretOffset)
			{
				TextViewPosition position2 = new TextViewPosition(EditBox.Document.GetLocation(EditBox.SelectionStart));
				if (textView.GetVisualPosition(position2, VisualYPosition.LineTop).Y < result.Y)
				{
					flag = false;
				}
			}
			if (flag)
			{
				result = new Point(result.X, result.Y - 50.0);
			}
			else
			{
				result = textView.GetVisualPosition(position, VisualYPosition.LineBottom);
				result = new Point(result.X, result.Y - 8.0);
			}
		}
		return result;
	}

	private TaskBaseViewModel GetTaskBaseModel()
	{
		if (base.DataContext is TaskDetailViewModel taskDetailViewModel)
		{
			return taskDetailViewModel.SourceViewModel;
		}
		return null;
	}

	protected string GetAvatarProjectId()
	{
		return GetTaskBaseModel()?.ProjectId;
	}

	protected string GetAssignee()
	{
		return GetTaskBaseModel()?.Assignee;
	}

	protected int GetPriority()
	{
		return GetTaskBaseModel()?.Priority ?? (-1);
	}

	protected List<string> GetTags()
	{
		return GetTaskBaseModel()?.Tags?.ToList();
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

	public void RemoveQuickSetText()
	{
		int num = Math.Min(Text.Length, Math.Max(0, _markIndex));
		ReplaceEditorText(num, Math.Min(_filterContent.Length + 1, Text.Length - num), string.Empty, num, null, deferTextChangedNotification: true);
		FocusEditBox();
	}

	public void InsertHyperlinkText(string link)
	{
		string linkTextForInsert = EditorUtilities.GetLinkTextForInsert(link);
		if (!string.IsNullOrEmpty(linkTextForInsert))
		{
			int selectionStart = EditBox.SelectionStart;
			int selectionLength = EditBox.SelectionLength;
			if (TryParseMarkdownLinkText(linkTextForInsert, out var linkText, out var url))
			{
				ReplaceEditorRangeWithLinkDisplayText(selectionStart, selectionLength, linkText, url);
			}
			else
			{
				ReplaceEditorText(selectionStart, selectionLength, linkTextForInsert, selectionStart + linkTextForInsert.Length);
			}
		}
	}

	public void ReplaceLinkText(string original, string revised, LinkLineInfoModel model)
	{
		int caretOffset = EditBox.CaretOffset;
		if (TryFindLinkDisplayRange(model, original, out var displayStart, out var displayEnd))
		{
			int value = displayStart + Math.Max(0, revised?.Length ?? 0);
			if (caretOffset < displayStart)
			{
				value = caretOffset;
			}
			if (TryParseMarkdownLinkText(revised, out var linkText, out var url))
			{
				ReplaceEditorRangeWithLinkDisplayText(displayStart, displayEnd - displayStart, linkText, url);
			}
			else
			{
				ReplaceEditorText(displayStart, displayEnd - displayStart, revised, value);
			}
		}
	}

	private void ReplaceEditorRangeWithLinkDisplayText(int startOffset, int removedLength, string linkText, string url)
	{
		linkText = linkText ?? string.Empty;
		ReplaceEditorText(startOffset, removedLength, linkText, startOffset + linkText.Length, delegate(int insertedOffset, int insertedLength)
		{
			AddLinkSemanticMark(insertedOffset, insertedLength, url);
		});
	}

	private static bool TryParseMarkdownLinkText(string text, out string linkText, out string url)
	{
		linkText = string.Empty;
		url = string.Empty;
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		Match match = FutureLinkRegex.Match(text);
		if (!match.Success || match.Index != 0 || match.Length != text.Length)
		{
			return false;
		}
		linkText = match.Groups[1].Value;
		url = match.Groups[2].Value;
		if (!string.IsNullOrEmpty(linkText))
		{
			return !string.IsNullOrEmpty(url);
		}
		return false;
	}

	private string TryGetMark()
	{
		DocumentLine lineByOffset = EditBox.Document.GetLineByOffset(EditBox.CaretOffset);
		string text = string.Empty;
		if (lineByOffset != null)
		{
			if (lineByOffset.Offset < EditBox.CaretOffset)
			{
				text = EditBox.Text.Substring(lineByOffset.Offset, EditBox.CaretOffset - lineByOffset.Offset);
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
				if (text.StartsWith("[[") || text.StartsWith("【【"))
				{
					int num2 = 0;
					for (int j = 0; j < array.Length - 1; j++)
					{
						num2 += array[j].Length + 1;
					}
					_filterContent = text.Substring(2);
					_markIndex = lineByOffset.Offset + num2;
					return "[[";
				}
			}
		}
		_filterContent = string.Empty;
		_markIndex = -1;
		return string.Empty;
	}

	public void SetMarkRegexText(bool unable, bool inDetail = true, bool isNote = false)
	{
		if (unable)
		{
			MarkRegexText = "([/|、])(.*)";
		}
		else if (inDetail)
		{
			MarkRegexText = (isNote ? "([/|、|#|@|~|^|\\*])(.*)" : "([/|、|#|@|~|^|!|！|\\*])(.*)");
		}
		else
		{
			MarkRegexText = "([/|、|#|@|~|^|!|！|\\*])(.*)";
		}
	}

	public void OnAttachmentMouseEnter(object sender)
	{
		if (sender is Border bd)
		{
			Utils.FindParent<TaskDetailView>(this)?.OnAttachmentMouseEnter(bd);
		}
	}

	private void OnQuickPopupOpen(object sender, EventArgs e)
	{
		this.QuickPopupOpened?.Invoke(this, null);
	}

	private void OnQuickPopupClosed(object sender, EventArgs e)
	{
		this.QuickPopupClosed?.Invoke(this, null);
	}

	public async Task TryShowLinkMenuPopup(LinkLineInfoModel linkLineInfo)
	{
		if (!CanShowLinkMenuPopup(linkLineInfo) || (_linkEditorPopup != null && _linkEditorPopup.IsOpen))
		{
			return;
		}
		int showVersion = _linkEditorPopupShowVersion;
		Point startPos = Mouse.GetPosition(EditBox);
		try
		{
			await Task.Delay(300);
			if (showVersion != _linkEditorPopupShowVersion || !CanShowLinkMenuPopup(linkLineInfo))
			{
				return;
			}
			Point position = Mouse.GetPosition(EditBox);
			if (Math.Pow(startPos.X - position.X, 2.0) + Math.Pow(startPos.Y - position.Y, 2.0) > 10.0 || (_linkEditorPopup != null && _linkEditorPopup.IsOpen) || showVersion != _linkEditorPopupShowVersion || !linkLineInfo.TryResolveStartIndex(out var startIndex) || startIndex < 0 || startIndex > EditBox.Document.TextLength)
			{
				return;
			}
			if (_linkEditorPopup == null)
			{
				_linkEditorPopup = new Popup
				{
					StaysOpen = true,
					AllowsTransparency = true,
					Placement = PlacementMode.Relative,
					PopupAnimation = PopupAnimation.Fade,
					PlacementTarget = this,
					Child = EditorMenuLink
				};
				_linkEditorPopup.Closed += _linkEditorPopup_Closed;
			}
			int visualLength = 0;
			if (linkLineInfo.Source == LinkInfoSource.Link)
			{
				if (!string.IsNullOrEmpty(linkLineInfo.Url))
				{
					visualLength = linkLineInfo.Url.Length;
				}
			}
			else if (!string.IsNullOrEmpty(linkLineInfo.Text))
			{
				visualLength = linkLineInfo.Text.Length;
			}
			Point popupOffsetLink = GetPopupOffsetLink(startIndex, visualLength);
			double num = EditorMenuLink.ActualWidth;
			if (num <= 0.0)
			{
				num = 100.0;
			}
			_linkEditorPopup.HorizontalOffset = popupOffsetLink.X - num / 2.0;
			_linkEditorPopup.VerticalOffset = popupOffsetLink.Y;
			if (showVersion == _linkEditorPopupShowVersion && CanShowLinkMenuPopup(linkLineInfo))
			{
				_linkEditorPopup.IsOpen = true;
				_linkLineInfo = linkLineInfo;
				StartTrackLinkMenuPopupPlacementTarget();
			}
		}
		catch (VisualLinesInvalidException)
		{
		}
		catch (ArgumentOutOfRangeException)
		{
		}
	}

	public Task HideLinkMenuPopup()
	{
		CancelLinkMenuPopupShow();
		StopTrackLinkMenuPopupPlacementTarget();
		_linkLineInfo = null;
		if (_linkEditorPopup != null && _linkEditorPopup.IsOpen)
		{
			_linkEditorPopup.IsOpen = false;
		}
		return Task.CompletedTask;
	}

	private void _linkEditorPopup_Closed(object sender, EventArgs e)
	{
		_linkLineInfo = null;
		StopTrackLinkMenuPopupPlacementTarget();
		CancelLinkMenuPopupShow();
	}

	private void CancelLinkMenuPopupShow()
	{
		_linkEditorPopupShowVersion++;
	}

	private bool CanShowLinkMenuPopup(LinkLineInfoModel linkLineInfo)
	{
		if (ReadOnly)
		{
			return false;
		}
		if (!EditorEnabled)
		{
			return false;
		}
		if (DisableLinkEditorMenu)
		{
			return false;
		}
		if (linkLineInfo == null || EditBox?.Document == null)
		{
			return false;
		}
		if (linkLineInfo.StartIndex < 0 || linkLineInfo.StartIndex > EditBox.Document.TextLength)
		{
			return false;
		}
		return true;
	}

	private void DestroyLinkMenuPopup()
	{
		CancelLinkMenuPopupShow();
		StopTrackLinkMenuPopupPlacementTarget();
		_linkLineInfo = null;
		if (_linkEditorPopup != null)
		{
			_linkEditorPopup.Closed -= _linkEditorPopup_Closed;
			_linkEditorPopup.IsOpen = false;
			_linkEditorPopup.Child = null;
			_linkEditorPopup.PlacementTarget = null;
			_linkEditorPopup = null;
		}
	}

	private void StartTrackLinkMenuPopupPlacementTarget()
	{
		RememberLinkMenuPopupPlacementTargetPosition();
		base.LayoutUpdated -= OnLinkMenuPopupPlacementTargetLayoutUpdated;
		base.LayoutUpdated += OnLinkMenuPopupPlacementTargetLayoutUpdated;
		base.SizeChanged -= OnLinkMenuPopupPlacementTargetSizeChanged;
		base.SizeChanged += OnLinkMenuPopupPlacementTargetSizeChanged;
	}

	private void StopTrackLinkMenuPopupPlacementTarget()
	{
		base.LayoutUpdated -= OnLinkMenuPopupPlacementTargetLayoutUpdated;
		base.SizeChanged -= OnLinkMenuPopupPlacementTargetSizeChanged;
		_hasLinkEditorPopupPlacementTargetScreenPosition = false;
	}

	private void OnLinkMenuPopupPlacementTargetLayoutUpdated(object sender, EventArgs e)
	{
		HideLinkMenuPopupIfPlacementTargetMoved();
	}

	private void OnLinkMenuPopupPlacementTargetSizeChanged(object sender, SizeChangedEventArgs e)
	{
		HideLinkMenuPopup();
	}

	private void RememberLinkMenuPopupPlacementTargetPosition()
	{
		if (!base.IsLoaded || PresentationSource.FromVisual(this) == null)
		{
			_hasLinkEditorPopupPlacementTargetScreenPosition = false;
			return;
		}
		_linkEditorPopupPlacementTargetScreenPosition = PointToScreen(default(Point));
		_hasLinkEditorPopupPlacementTargetScreenPosition = true;
	}

	private void HideLinkMenuPopupIfPlacementTargetMoved()
	{
		if (_linkEditorPopup == null || !_linkEditorPopup.IsOpen)
		{
			return;
		}
		if (!base.IsLoaded || PresentationSource.FromVisual(this) == null)
		{
			HideLinkMenuPopup();
			return;
		}
		if (!_hasLinkEditorPopupPlacementTargetScreenPosition)
		{
			RememberLinkMenuPopupPlacementTargetPosition();
			return;
		}
		Point point = PointToScreen(default(Point));
		if (!(Math.Abs(point.X - _linkEditorPopupPlacementTargetScreenPosition.X) <= 1.0) || !(Math.Abs(point.Y - _linkEditorPopupPlacementTargetScreenPosition.Y) <= 1.0))
		{
			HideLinkMenuPopup();
		}
	}

	private Point GetPopupOffsetLink(int startIndex, int visualLength)
	{
		Point position = Mouse.GetPosition(EditBox);
		TextView textView = EditBox.TextArea.TextView;
		int textLength = EditBox.Document.TextLength;
		startIndex = Math.Max(0, Math.Min(startIndex, textLength));
		int offset = Math.Max(0, Math.Min(startIndex + (int)(0.5 * (double)visualLength), textLength));
		TextViewPosition position2 = new TextViewPosition(EditBox.Document.GetLocation(offset));
		Point visualPosition = textView.GetVisualPosition(position2, VisualYPosition.LineTop);
		bool flag = true;
		TextViewPosition position3 = new TextViewPosition(EditBox.Document.GetLocation(startIndex));
		if (textView.GetVisualPosition(position3, VisualYPosition.LineTop).Y < visualPosition.Y)
		{
			flag = false;
		}
		if (flag)
		{
			visualPosition = new Point(position.X, visualPosition.Y - 50.0);
		}
		else
		{
			visualPosition = textView.GetVisualPosition(position2, VisualYPosition.LineBottom);
			visualPosition = new Point(position.X, visualPosition.Y - 8.0);
		}
		return visualPosition;
	}

	private async void RefreshProjectPopupAfterComposition()
	{
		await _projectImeRefresh.RefreshAfterCompositionAsync(() => EditBox.TextArea.IsImeCompositionActive, () => base.IsLoaded && EditBox.TextArea.IsKeyboardFocused, TryShowQuickSetPopup);
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
				Placement = PlacementMode.Relative,
				HorizontalOffset = -18.0,
				VerticalOffset = -5.0
			};
			SetProjectPopup.Opened += OnQuickPopupOpen;
			SetProjectPopup.Closed += OnQuickPopupClosed;
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
			if (!EditBox.TextArea.IsImeCompositionActive)
			{
				FocusEditBox();
				Keyboard.Focus(EditBox.TextArea);
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
		TaskBaseViewModel taskBaseViewModel = GetTaskBaseModel();
		if (taskBaseViewModel == null)
		{
			taskBaseViewModel = new TaskBaseViewModel();
		}
		_projectOrGroupPopup = new ProjectOrGroupPopup(SetProjectPopup, new ProjectExtra
		{
			ProjectIds = ((!string.IsNullOrEmpty(taskBaseViewModel.ProjectId)) ? new List<string> { taskBaseViewModel.ProjectId } : new List<string>())
		}, new ProjectSelectorExtra
		{
			showAll = false,
			batchMode = false,
			canSelectGroup = false,
			onlyShowPermission = true,
			ShowColumn = true,
			ColumnId = taskBaseViewModel.ColumnId,
			IgnoreMiniHeight = true
		});
		_projectOrGroupPopup.NoSearchResult -= OnNoSearchResult;
		_projectOrGroupPopup.NoSearchResult += OnNoSearchResult;
		SetProjectPopup.PlacementTarget = target;
		if (extra != null)
		{
			SetProjectPopup.HorizontalOffset = extra.Position.X + 10.0;
			SetProjectPopup.VerticalOffset = extra.Position.Y - 10.0;
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
			SetProjectPopup.HorizontalOffset = extra.Position.X + 10.0;
			SetProjectPopup.VerticalOffset = extra.Position.Y - 10.0;
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
			RemoveQuickSetText();
			this.QuickItemSelected?.Invoke(this, new QuickSetModel
			{
				Type = QuickSetType.CalId,
				CalId = subscribeCalendarViewModel.Id,
				Title = subscribeCalendarViewModel.Title
			});
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
			RemoveQuickSetText();
			this.QuickItemSelected?.Invoke(this, new QuickSetModel
			{
				Type = QuickSetType.Project,
				Project = projectModel,
				ColumnId = item,
				ColumnName = item2
			});
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
		if (EditBox.TextArea.IsImeCompositionActive)
		{
			_projectPopupState.SuppressTrigger(_markIndex, EditBox.Text, _filterContent);
			_projectPopupState.HideUntil(SetProjectPopup, () => EditBox.TextArea.IsImeCompositionActive, delegate
			{
				IsInOperation = false;
			});
		}
		else
		{
			SetProjectPopup.IsOpen = false;
		}
	}

	private bool IsProjectPopupOpen()
	{
		if (SetProjectPopup != null && SetProjectPopup.IsOpen)
		{
			return !_projectPopupState.IsHidden;
		}
		return false;
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
			if (!SetProjectPopup.IsMouseOver)
			{
				_projectOrGroupPopup?.ClosePopup();
				SetProjectPopup.IsOpen = false;
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
			Uri resourceLocator = new Uri("/TickTick;component/views/markdown/markdowneditor.xaml", UriKind.Relative);
			System.Windows.Application.LoadComponent(this, resourceLocator);
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
			Root = (MarkDownEditor)target;
			Root.ContextMenuOpening += EditorMenuOnContextMenuOpening;
			break;
		case 2:
			EditBox = (TextEditor)target;
			EditBox.PreviewTextInput += BeforeTextInput;
			break;
		case 3:
			CodeBlockLanguageOverlay = (Canvas)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
