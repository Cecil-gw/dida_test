using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using Emoji.Wpf;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.DateParser;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Config;
using ticktick_WPF.Views.MainListView;
using ticktick_WPF.Views.MarkDown;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Tag;
using ticktick_WPF.Views.Template;
using ticktick_WPF.Views.Widget;

namespace ticktick_WPF.Views.QuickAdd;

public class QuickAddText : UserControl, IEmojiRender, IDateParseBox, IComponentConnector
{
	private DateTokenGenerator _dateTokenGenerator;

	private string _filterContent;

	private string _quickDate;

	private TokenGenerator _tokenGenerator;

	private bool _canEnter;

	private QuickSelectionControl _selectionControl;

	public static readonly DependencyProperty FirstLineIndentProperty = DependencyProperty.Register("FirstLineIndent", typeof(double), typeof(QuickAddText), new PropertyMetadata(0.0, OnFirstLineIndentChanged));

	public readonly Dictionary<int, string> EmojiDict = new Dictionary<int, string>();

	private readonly DelayActionHandler _dateParserHandler = new DelayActionHandler(150);

	private bool _canMoveDown;

	private DateTime _lastKeyDownTime;

	private string _origin;

	private bool _inputByKeyBoard;

	private int _startIndex;

	private HashSet<string> _projectNames;

	private readonly ImeCompositionRefresh _quickPopupImeRefresh = new ImeCompositionRefresh();

	private readonly QuickSetProjectPopupState _projectPopupState = new QuickSetProjectPopupState();

	private bool _normalizingFirstLineIndentSelection;

	private DateTime _suppressFocusUntil = DateTime.MinValue;

	private bool _splashCommandSelected;

	private bool _suppressQuickPopupForSplashAction;

	private bool _shiftPressed;

	private bool _ctrlPressed;

	internal QuickAddText Root;

	internal Grid Grid;

	internal TextEditor EditBox;

	internal Popup SelectionPopup;

	private bool _contentLoaded;

	public double FirstLineIndent
	{
		get
		{
			return (double)GetValue(FirstLineIndentProperty);
		}
		set
		{
			SetValue(FirstLineIndentProperty, value);
		}
	}

	public bool CanParseDate { get; set; } = true;

	public bool Focused => EditBox.TextArea.IsKeyboardFocused;

	public bool FocusSuppressed => IsFocusSuppressed();

	public event EventHandler<double> CaretHorizontalOffsetChanged;

	public event EventHandler<string> TextChanged;

	public event EventHandler<string> TagSelected;

	public event EventHandler<string> EnterText;

	public event EventHandler<string> SplitText;

	public event EventHandler<IPaserDueDate> DateParsed;

	public event EventHandler<QuickAddToken> TokenRemoved;

	public event EventHandler<bool> PopupOpenChanged;

	public event EventHandler<int> PrioritySelect;

	public event EventHandler<ProjectModel> ProjectSelect;

	public event EventHandler<DateTime> DateSelect;

	public event EventHandler<AvatarViewModel> AssigneeSelect;

	public event EventHandler<string> CalendarSelect;

	public event EventHandler MoveDown;

	public event EventHandler<KeyEventArgs> TextKeyDown;

	public event EventHandler<string> SplashCommand;

	public event EventHandler<ProjectShowExtra> ShowProjectCommand;

	public event EventHandler HideProjectCommand;

	public QuickAddText()
	{
		InitializeComponent();
		SetupEvents();
		SetupGenerators();
		base.Loaded += delegate
		{
			BindEvents();
		};
		base.Unloaded += delegate
		{
			UnbindEvents();
		};
	}

	private void SetupGenerators()
	{
		_tokenGenerator = new TokenGenerator(this);
		_dateTokenGenerator = new DateTokenGenerator(this);
		EditBox.TextArea.TextView.ElementGenerators.Add(new FirstLineIndentGenerator(this));
		EditBox.TextArea.TextView.ElementGenerators.Add(_dateTokenGenerator);
		EditBox.TextArea.TextView.ElementGenerators.Add(_tokenGenerator);
		EditBox.TextArea.TextView.ElementGenerators.Add(new EmojiGenerator(this));
		_tokenGenerator.TokenRemoved -= OnTokenRemoved;
		_tokenGenerator.TokenRemoved += OnTokenRemoved;
		EditBox.TextArea.Caret.PositionChanged -= NotifyCaretChanged;
		EditBox.TextArea.Caret.PositionChanged += NotifyCaretChanged;
		EditBox.TextArea.Caret.PositionChanged -= NormalizeFirstLineIndentSelection;
		EditBox.TextArea.Caret.PositionChanged += NormalizeFirstLineIndentSelection;
		EditBox.TextArea.SelectionChanged -= NormalizeFirstLineIndentSelection;
		EditBox.TextArea.SelectionChanged += NormalizeFirstLineIndentSelection;
		EditBox.GotFocus += OnGotFocus;
		EditBox.TextArea.TextView.LinkTextForegroundBrush = ThemeUtil.GetColor("TextAccentColor", this);
	}

	private static void OnFirstLineIndentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is QuickAddText quickAddText)
		{
			quickAddText.EditBox?.TextArea?.TextView?.Redraw();
		}
	}

	private void NormalizeFirstLineIndentSelection(object sender, EventArgs e)
	{
		if (_normalizingFirstLineIndentSelection || FirstLineIndent <= 0.0 || EditBox?.Document == null)
		{
			return;
		}
		TextArea textArea = EditBox.TextArea;
		int offset = EditBox.Document.GetLineByNumber(1).Offset;
		Selection selection = textArea.Selection;
		TextViewPosition position = textArea.Caret.Position;
		bool flag = textArea.Caret.Offset == offset && position.VisualColumn >= 0 && position.VisualColumn < 1;
		bool flag2 = !selection.IsEmpty && (IsInsideFirstLineIndent(selection.StartPosition, offset) || IsInsideFirstLineIndent(selection.EndPosition, offset));
		if (!flag && !flag2)
		{
			return;
		}
		_normalizingFirstLineIndentSelection = true;
		try
		{
			if (flag2)
			{
				textArea.Selection = Selection.Create(textArea, selection.SurroundingSegment.Offset, selection.SurroundingSegment.EndOffset);
			}
			if (flag)
			{
				textArea.Caret.Offset = offset;
			}
		}
		finally
		{
			_normalizingFirstLineIndentSelection = false;
		}
	}

	private bool IsInsideFirstLineIndent(TextViewPosition position, int firstLineOffset)
	{
		if (EditBox.Document.GetOffset(position.Location) == firstLineOffset && position.VisualColumn >= 0)
		{
			return position.VisualColumn < 1;
		}
		return false;
	}

	private void OnGotFocus(object sender, RoutedEventArgs e)
	{
		QuickAddView quickAddView = GetQuickAddView();
		if (string.IsNullOrEmpty(EditBox.Text))
		{
			EditBox.GotFocus -= OnGotFocus;
			AddTaskViewModel addTaskViewModel = quickAddView?.Model;
			if (addTaskViewModel != null && addTaskViewModel.AutoAddTags)
			{
				InitTagTokens(addTaskViewModel.Tags);
				quickAddView.TryInitAvatar(addTaskViewModel.Assignee);
				addTaskViewModel.AutoAddTags = false;
			}
			EditBox.GotFocus += OnGotFocus;
		}
		if (!_dateTokenGenerator.Any() && !_tokenGenerator.ExistToken(TokenType.QuickDate))
		{
			quickAddView?.ResetDate();
		}
		_projectNames = new HashSet<string>((from p in CacheManager.GetProjects()
			select p.name).ToList());
	}

	private void LogEmojiPosition()
	{
		EmojiDict.Clear();
		string text = EditBox.Text;
		foreach (Match item in ((text.Length > 200) ? EmojiData.MatchOne2 : EmojiData.MatchOne).Matches(text))
		{
			EmojiDict[item.Index] = item.Value;
		}
	}

	private void NotifyCaretChanged(object sender, EventArgs e)
	{
		double x = EditBox.TextArea.TextView.GetVisualPosition(new TextViewPosition(EditBox.TextArea.Caret.Position.Line, Math.Max(1, EditBox.TextArea.Caret.Column)), VisualYPosition.LineTop).X;
		this.CaretHorizontalOffsetChanged?.Invoke(this, x);
	}

	private void OnAppThemeChanged(object sender, EventArgs e)
	{
		EditBox.TextArea.TextView.LinkTextForegroundBrush = ThemeUtil.GetColor("TextAccentColor", this);
		EditBox.TextArea.TextView.Redraw();
	}

	private void OnTokenRemoved(object sender, QuickAddToken token)
	{
		if (token.TokenType == TokenType.QuickDate)
		{
			_quickDate = string.Empty;
		}
		if (!string.IsNullOrEmpty(EditBox.Text))
		{
			ForceRender();
		}
		this.TokenRemoved?.Invoke(this, token);
	}

	private async void OnPaste(object sender, DataObjectPastingEventArgs e)
	{
		int time = 1;
		bool flag = true;
		_ctrlPressed = false;
		while (flag)
		{
			try
			{
				if (e.SourceDataObject.GetDataPresent(DataFormats.UnicodeText, autoConvert: true))
				{
					object data = e.SourceDataObject.GetData(DataFormats.UnicodeText);
					string text = data as string;
					if (text != null)
					{
						e.CancelCommand();
						if (Utils.ShowBatchAdd(text) && !IsUsedInCalendar())
						{
							Utils.RunOnUiThread(base.Dispatcher, delegate
							{
								GetQuickAddView()?.ShowBatchAddWindow(text);
							});
						}
						else
						{
							ProjectTask projectTask = TaskUtils.ParseTaskUrl(text) ?? TaskUtils.ParseTaskUrlWithoutTitle(text);
							if (projectTask != null && !text.StartsWith("[") && !text.EndsWith(")"))
							{
								string text2 = text.Replace(" " + projectTask.Title, string.Empty);
								e.CancelCommand();
								await TryPasteLinkTask(projectTask, text2);
								break;
							}
							string text3 = text.Replace("\n", " ").Replace("\r", " ");
							InitTokens(text3);
							int num = EditBox.CaretOffset;
							if (EditBox.SelectionLength > 0)
							{
								EditBox.Document.Remove(EditBox.SelectionStart, EditBox.SelectionLength);
								num = EditBox.SelectionStart;
							}
							EditBox.Document.Insert(num, text3);
							EditBox.CaretOffset = num + text3.Length;
						}
					}
				}
				flag = false;
			}
			catch (Exception)
			{
				flag = time++ < 10;
			}
		}
	}

	private async Task TryPasteLinkTask(ProjectTask extra, string text)
	{
		string text2 = extra.Title;
		if (string.IsNullOrEmpty(text2))
		{
			TaskModel taskModel = await TaskDao.GetThinTaskById(extra.TaskId);
			text2 = ((taskModel == null || string.IsNullOrEmpty(taskModel.title)) ? Utils.GetString("MyTask") : taskModel.title);
		}
		EditBox.Document.Insert(EditBox.CaretOffset, "[" + text2 + "](" + text + ")");
	}

	private void InitTokens(string content)
	{
		foreach (TagModel item in from tag in CacheManager.GetTags().ToList()
			where content.ToLower().Contains("#" + tag.name)
			select tag)
		{
			_tokenGenerator.TryAddToken(QuickAddToken.BuildTag("#" + item.GetDisplayName()));
		}
		foreach (ProjectModel item2 in from project in CacheManager.GetProjects()
			where content.Contains("^" + project.name) || content.Contains("~" + project.name)
			select project)
		{
			_tokenGenerator.TryAddToken(content.Contains("^" + item2.name) ? QuickAddToken.BuildProject("^" + item2.name) : QuickAddToken.BuildProject("~" + item2.name));
			this.ProjectSelect?.Invoke(this, item2);
		}
		Dictionary<string, int> dictionary = new Dictionary<string, int>
		{
			{
				Utils.GetString("PriorityHigh"),
				5
			},
			{
				Utils.GetString("PriorityMedium"),
				3
			},
			{
				Utils.GetString("PriorityLow"),
				1
			},
			{
				Utils.GetString("PriorityNull"),
				0
			}
		};
		foreach (string item3 in dictionary.Keys.Where((string priority) => content.Contains("!" + priority) || content.Contains("！" + priority)))
		{
			_tokenGenerator.TryAddToken(content.Contains("!" + item3) ? QuickAddToken.BuildPriority("!" + item3) : QuickAddToken.BuildPriority("！" + item3));
			this.PrioritySelect?.Invoke(this, dictionary[item3]);
		}
		List<AvatarViewModel> list = (IsUsedInCalendar() ? null : GetQuickAddView()?.GetAvatars().ToList());
		if (list == null || !list.Any())
		{
			return;
		}
		foreach (AvatarViewModel item4 in list.Where((AvatarViewModel avatar) => content.Contains("@" + avatar.Name)))
		{
			_tokenGenerator.TryAddToken(QuickAddToken.BuildAssignee("@" + item4.Name));
			this.AssigneeSelect?.Invoke(this, item4);
		}
	}

	private void SetAssignTokens(string content)
	{
		if (!IsUsedInCalendar() && !content.Contains("@"))
		{
			_tokenGenerator.RemoveTokenByType(TokenType.Assignee);
			QuickAddView quickAddView = GetQuickAddView();
			if (quickAddView != null)
			{
				quickAddView.Model.Assignee = null;
			}
		}
	}

	private void SetProjectTokens(string content)
	{
		if (!IsUsedInCalendar())
		{
			if (!content.Contains("~") && !content.Contains("^"))
			{
				_tokenGenerator.RemoveTokenByType(TokenType.Project);
				GetQuickAddView()?.Model.ResetProject();
			}
		}
	}

	private void SetPriorityTokens(string content)
	{
		if (IsUsedInCalendar())
		{
			return;
		}
		if (!content.Contains("!") && !content.Contains("！"))
		{
			AddTaskViewModel addTaskViewModel = GetQuickAddView()?.Model;
			if (addTaskViewModel != null && !addTaskViewModel.ManuallySelectedPriority)
			{
				_tokenGenerator.RemoveTokenByType(TokenType.Priority);
				GetQuickAddView()?.Model.ResetPriority();
			}
		}
		else
		{
			if (_tokenGenerator.ExistToken(TokenType.Priority) && EditBox.CaretOffset > 0 && content[Math.Max(content.Length, EditBox.CaretOffset) - 1] != ' ')
			{
				return;
			}
			List<QuickAddToken> tokensByType = _tokenGenerator.GetTokensByType(TokenType.Priority);
			if (tokensByType.Any())
			{
				string input = content.Substring(0, Math.Max(content.Length, EditBox.CaretOffset));
				MatchCollection matchCollection = _tokenGenerator.PriorityRegex.Matches(input);
				if (matchCollection.Count > 0)
				{
					Match match = matchCollection[matchCollection.Count - 1];
					string pri = match.ToString().Trim();
					if (tokensByType.All((QuickAddToken tok) => tok.Value.Trim() != pri))
					{
						OnTokenSelected(QuickAddToken.BuildPriority(pri), withMark: false, handleText: false);
						this.PrioritySelect?.Invoke(this, TokenGenerator.GetPriorityByText(pri));
					}
				}
				else
				{
					_tokenGenerator.RemoveTokenByType(TokenType.Priority);
					GetQuickAddView()?.Model.ResetPriority();
				}
			}
			else
			{
				MatchCollection matchCollection2 = _tokenGenerator.PriorityRegex.Matches(content);
				if (matchCollection2.Count > 0)
				{
					string text = matchCollection2[matchCollection2.Count - 1].ToString().Trim();
					OnTokenSelected(QuickAddToken.BuildPriority(text), withMark: false, handleText: false);
					this.PrioritySelect?.Invoke(this, TokenGenerator.GetPriorityByText(text));
				}
			}
		}
	}

	private void SetTagTokens(string content)
	{
		if (IsUsedInCalendar())
		{
			return;
		}
		string[] array = content.Replace("#", " #").Split(' ');
		List<string> list = new List<string>();
		List<string> list2 = array.ToList();
		if (list2.Count > 0)
		{
			list2.Remove(list2[list2.Count - 1]);
		}
		for (int i = 0; i < array.Length - 1; i++)
		{
			string text = array[i].TrimStart();
			if (text.Length != 0 && !_tokenGenerator.ExistRemoveToken(text))
			{
				string exp = text.Substring(1);
				string text2 = text.Substring(0, 1);
				if ((text2 == "#" || text2 == "＃") && NameUtils.IsValidName(exp, checkSpace: false) && !string.IsNullOrWhiteSpace(text))
				{
					list.Add(text);
				}
			}
		}
		_tokenGenerator.HandleRemainText(list2);
		foreach (string item in list)
		{
			_tokenGenerator.TryAddTagToken(QuickAddToken.BuildTag(item));
		}
		if (content != content)
		{
			EditBox.SetTextAndOffset(content, restoreOffset: true);
		}
		else
		{
			EditBox.TextArea.TextView.Redraw();
		}
	}

	public string GetSelectedProject()
	{
		return _tokenGenerator.GetSelectedProject();
	}

	public string GetProjectTokenValue()
	{
		return _tokenGenerator.GetProjectTokenValue();
	}

	public List<string> GetSelectedTags()
	{
		return _tokenGenerator.GetSelectedTags();
	}

	public void InitTagTokens(List<string> tags)
	{
		List<QuickAddToken> list = new List<QuickAddToken>();
		for (int i = 0; i < tags.Count; i++)
		{
			string name = tags[i].ToLower();
			string text = (tags[i] = CacheManager.GetTags().FirstOrDefault((TagModel t) => name == t.name)?.GetDisplayName() ?? tags[i]);
			list.Add(QuickAddToken.BuildTag("#" + text.Replace("#", string.Empty)));
		}
		foreach (QuickAddToken item in list)
		{
			_tokenGenerator.TryAddToken(item);
		}
		EditBox.Text = tags.Aggregate(string.Empty, (string text3, string tag) => text3 + "#" + tag + " ");
		EditBox.CaretOffset = EditBox.Text.Length;
	}

	public void RemoveTextChangedEvent()
	{
		EditBox.TextChanged -= EditBoxOnTextChanged;
	}

	public void AddTextChangedEvent()
	{
		EditBox.TextChanged += EditBoxOnTextChanged;
	}

	public void SetTextWithoutTokenParsing(string text, bool resetTokens = false)
	{
		EditBox.TextChanged -= EditBoxOnTextChanged;
		try
		{
			if (resetTokens)
			{
				ResetTokens();
			}
			EditBox.Text = text ?? string.Empty;
			EditBox.CaretOffset = EditBox.Text.Length;
			_origin = EditBox.Text;
			EditBox.TextArea.TextView.Redraw();
		}
		finally
		{
			EditBox.TextChanged -= EditBoxOnTextChanged;
			EditBox.TextChanged += EditBoxOnTextChanged;
		}
	}

	public void AddTokenWithoutParsing(QuickAddToken token, bool withMark = true)
	{
		if (token == null || string.IsNullOrEmpty(token.Value))
		{
			return;
		}
		EditBox.TextChanged -= EditBoxOnTextChanged;
		try
		{
			_tokenGenerator.TryAddToken(token);
			string text = (withMark ? token.Value.Trim() : token.Value.Trim().Substring(1)) + " ";
			if (!(EditBox.Text ?? string.Empty).StartsWith(text, StringComparison.Ordinal))
			{
				EditBox.Document.Insert(0, text);
			}
			EditBox.CaretOffset = EditBox.Text.Length;
			_origin = EditBox.Text;
			EditBox.TextArea.TextView.Redraw();
		}
		finally
		{
			EditBox.TextChanged -= EditBoxOnTextChanged;
			EditBox.TextChanged += EditBoxOnTextChanged;
		}
	}

	private void SetupEvents()
	{
		EditBox.TextChanged -= EditBoxOnTextChanged;
		EditBox.TextChanged += EditBoxOnTextChanged;
		DataObject.AddPastingHandler(EditBox, OnPaste);
	}

	private void BindEvents()
	{
		DataChangedNotifier.ThemeModeChanged -= OnAppThemeChanged;
		DataChangedNotifier.ThemeModeChanged += OnAppThemeChanged;
		_dateParserHandler.SetAction(TryDateParser);
	}

	private void UnbindEvents()
	{
		_quickPopupImeRefresh.Cancel();
		DataChangedNotifier.ThemeModeChanged -= OnAppThemeChanged;
		_dateParserHandler.StopAndClear();
	}

	private void TryDateParser(object sender, EventArgs e)
	{
		Utils.RunOnUiThread(base.Dispatcher, TryParseDate);
	}

	public void ForceRender()
	{
		LogEmojiPosition();
		EditBox.Document.BeginUpdate();
		EditBox.Document.Insert(0, " ");
		EditBox.Document.Remove(0, 1);
		EditBox.Document.EndUpdate();
	}

	public double GetFontSize()
	{
		return EditBox.TextArea.FontSize;
	}

	public string GetColor()
	{
		return ((Color)FindResource("ColorPrimary")/*cast due to .constrained prefix*/).ToString();
	}

	private bool IsUsedInCalendar()
	{
		return GetQuickAddView()?.Model.IsCalendar ?? false;
	}

	private void TryShowQuickPopup(string text = null, int? caretOffset = null)
	{
		_quickPopupImeRefresh.Cancel();
		if (EditBox.TextArea.IsImeCompositionActive && !_suppressQuickPopupForSplashAction)
		{
			TryShowQuickPopupAfterImeComposition();
		}
		string content;
		string text2 = TryGetMark(text ?? EditBox.Text, caretOffset ?? EditBox.CaretOffset, out content);
		_filterContent = content;
		if (string.IsNullOrEmpty(text2))
		{
			ClearSuppressedProjectQuickPopup();
			this.HideProjectCommand?.Invoke(this, null);
			if (!_splashCommandSelected)
			{
				_selectionControl?.ClosePopup(clearChildren: false);
			}
			return;
		}
		if (text2 == "*")
		{
			List<string> validTokens = _dateTokenGenerator.GetValidTokens();
			if (validTokens != null && validTokens.Count > 0)
			{
				return;
			}
		}
		if ((text2 == "^" || text2 == "~") && !_filterContent.EndsWith(" "))
		{
			if (!_projectPopupState.IsTriggerSuppressed(_startIndex, text2, _filterContent))
			{
				this.ShowProjectCommand?.Invoke(this, new ProjectShowExtra
				{
					TriggerMark = text2,
					FilterContent = _filterContent,
					Position = GetPopupOffset()
				});
				_selectionControl?.ClosePopup();
				DelayFocus();
			}
			return;
		}
		ClearSuppressedProjectQuickPopup();
		if (!IsUsedInCalendar())
		{
			this.HideProjectCommand?.Invoke(this, null);
		}
		QuickAddView quickAddView = GetQuickAddView();
		if (_selectionControl == null)
		{
			_selectionControl = new QuickSelectionControl(SelectionPopup);
			if (Window.GetWindow(this) is WidgetWindow widgetWindow)
			{
				_selectionControl.Resources = widgetWindow.Resources;
			}
			_selectionControl.PopupOpenChanged += delegate(object o, bool e)
			{
				this.PopupOpenChanged?.Invoke(this, e);
			};
			_selectionControl.ItemSelected += OnQuickItemSelected;
		}
		bool flag = IsUsedInCalendar();
		string calId = "";
		string projectId = null;
		int priority = -1;
		List<string> tags = null;
		string assignee = null;
		bool flag2 = false;
		bool isNoteProject = false;
		bool canConvertToEvent = false;
		if (text2 != null)
		{
			int length = text2.Length;
			if (length == 1)
			{
				switch (text2[0])
				{
				case '^':
				case '~':
					if (flag)
					{
						calId = quickAddView?.GetQuickSetCalendarAccountId();
					}
					else
					{
						projectId = quickAddView?.Model.ProjectId;
					}
					break;
				case '@':
					if (!flag)
					{
						projectId = quickAddView?.Model.ProjectId;
						assignee = quickAddView?.Avatar?.UserId;
					}
					break;
				case '!':
				case '！':
					flag2 = quickAddView?.UseInNote() ?? false;
					priority = quickAddView?.Model.Priority ?? (-1);
					break;
				case '#':
				case '＃':
					if (!flag)
					{
						tags = quickAddView?.Model.Tags;
					}
					break;
				case '/':
				case '、':
					flag2 = quickAddView?.UseInNote() ?? false;
					isNoteProject = quickAddView?.Model.IsNoteProject ?? false;
					canConvertToEvent = quickAddView?.CanConvertToEvent() ?? false;
					break;
				}
			}
		}
		if (_selectionControl.SetSelectionItems(text2, content, flag, calId, projectId, priority, tags, assignee, !flag2 && !flag, !flag, !flag, flag2, flag, isNoteProject, canConvertToEvent))
		{
			Point popupOffset = GetPopupOffset();
			_selectionControl.TryShowPopup(text2, content, new Point(popupOffset.X - 12.0, -12.0), _splashCommandSelected);
			DelayFocus();
		}
		else if (!_splashCommandSelected)
		{
			_selectionControl?.ClosePopup();
		}
		MuteSplashSelected();
	}

	public void SuppressCurrentProjectQuickPopup()
	{
		_projectPopupState.SuppressTrigger(_startIndex, EditBox.Text, _filterContent);
	}

	public void ClearSuppressedProjectQuickPopup()
	{
		_projectPopupState.ClearSuppressedTrigger();
	}

	private async void MuteSplashSelected()
	{
		if (_splashCommandSelected)
		{
			await Task.Delay(200);
			_splashCommandSelected = false;
		}
	}

	public void SuppressFocus(TimeSpan duration)
	{
		DateTime dateTime = DateTime.Now.Add(duration);
		if (dateTime > _suppressFocusUntil)
		{
			_suppressFocusUntil = dateTime;
		}
	}

	private bool IsFocusSuppressed()
	{
		return DateTime.Now <= _suppressFocusUntil;
	}

	public void AllowFocus()
	{
		_suppressFocusUntil = DateTime.MinValue;
	}

	private async void DelayFocus()
	{
		await Task.Delay(100);
		if (!IsFocusSuppressed() && !EditBox.TextArea.IsImeCompositionActive)
		{
			EditBox.Focus();
			Keyboard.Focus(EditBox.TextArea);
		}
	}

	private async void OnQuickItemSelected(object sender, QuickSetModel e)
	{
		OnQuickItemSelect(sender, e);
	}

	public async void OnQuickItemSelect(object sender, QuickSetModel e, bool withTagPrefix = false)
	{
		_canEnter = false;
		switch (e.Type)
		{
		case QuickSetType.Priority:
			OnTokenSelected(QuickAddToken.BuildPriority(_selectionControl.Tag?.ToString() + e.Title));
			UserActCollectUtils.AddShortCutEvent("quick_add", "set_priority");
			this.PrioritySelect?.Invoke(this, e.Priority);
			break;
		case QuickSetType.Project:
			OnTokenSelected(QuickAddToken.BuildProject(_selectionControl.Tag?.ToString() + e.Project.name));
			UserActCollectUtils.AddShortCutEvent("quick_add", "add_to_list");
			this.ProjectSelect?.Invoke(this, e.Project);
			break;
		case QuickSetType.Tag:
			EditBox.TextChanged -= EditBoxOnTextChanged;
			OnTagTokenSelected(QuickAddToken.BuildTag("#" + e.Tag), withTagPrefix);
			UserActCollectUtils.AddShortCutEvent("quick_add", "set_tag");
			EditBox.TextChanged += EditBoxOnTextChanged;
			break;
		case QuickSetType.Date:
			if (e.Date.HasValue)
			{
				_quickDate = e.Title;
				OnTokenSelected(QuickAddToken.BuildQuickDate(_selectionControl.Tag?.ToString() + e.Title));
				this.DateSelect?.Invoke(this, e.Date.Value);
			}
			UserActCollectUtils.AddShortCutEvent("quick_add", "set_due_date ");
			break;
		case QuickSetType.Assign:
			OnTokenSelected(QuickAddToken.BuildAssignee(_selectionControl.Tag?.ToString() + e.Title));
			this.AssigneeSelect?.Invoke(this, e.Avatar);
			UserActCollectUtils.AddShortCutEvent("quick_add", "assign_to_member ");
			break;
		case QuickSetType.CalId:
			OnTokenSelected(QuickAddToken.BuildProject(_selectionControl.Tag?.ToString() + e.Title));
			this.CalendarSelect?.Invoke(this, e.CalId);
			break;
		case QuickSetType.Splash:
		{
			_splashCommandSelected = true;
			_suppressQuickPopupForSplashAction = IsSplashAction(e.SplashCommand);
			int num = Math.Min(EditBox.CaretOffset, EditBox.Text?.Length ?? 0);
			int splashIndexLength = Utils.GetSplashIndexLength(num, EditBox.Text ?? string.Empty);
			if (splashIndexLength < 0 || splashIndexLength > num)
			{
				_selectionControl?.ClosePopup();
				_splashCommandSelected = false;
				_suppressQuickPopupForSplashAction = false;
				break;
			}
			int length = num - splashIndexLength;
			EditBox.Document.Remove(splashIndexLength, length);
			if (e.SplashCommand == "template")
			{
				_selectionControl?.ClosePopup();
				_splashCommandSelected = false;
				QuickAddView quickAddView = GetQuickAddView();
				AddTaskViewModel addTaskViewModel = base.DataContext as AddTaskViewModel;
				AddTemplateDialog addTemplateDialog = new AddTemplateDialog(addTaskViewModel.IsNote ? TemplateKind.Note : TemplateKind.Task, addTaskViewModel, Utils.FindParent<ListViewContainer>(quickAddView));
				addTemplateDialog.Owner = Window.GetWindow(this);
				addTemplateDialog.TemplateSelected -= OnTemplateSelected;
				addTemplateDialog.TemplateSelected += OnTemplateSelected;
				addTemplateDialog.ShowDialog();
				addTemplateDialog.Activate();
				addTemplateDialog.Topmost = true;
				quickAddView?.TryFocus();
			}
			else if (!string.IsNullOrEmpty(e.SplashCommand) && !IsSplashAction(e.SplashCommand))
			{
				string mark = e.SplashCommand;
				await Task.Delay(50);
				EditBox.Document.Insert(EditBox.CaretOffset, mark);
				QuickAddView quickAdd = GetQuickAddView();
				if (quickAdd != null)
				{
					await Task.Delay(200);
					quickAdd.IsInOperation = true;
				}
			}
			else if (IsSplashAction(e.SplashCommand))
			{
				_selectionControl?.ClosePopup();
				this.SplashCommand?.Invoke(this, e.SplashCommand);
				_quickPopupImeRefresh.Cancel();
				await Task.Delay(50);
				_suppressQuickPopupForSplashAction = false;
				_quickPopupImeRefresh.Cancel();
				if (IsTaskKindSwitchAction(e.SplashCommand))
				{
					TryShowQuickPopup();
				}
				_splashCommandSelected = false;
			}
			else
			{
				_selectionControl?.ClosePopup();
				_splashCommandSelected = false;
				_suppressQuickPopupForSplashAction = false;
			}
			break;
		}
		case QuickSetType.TaskLink:
			break;
		}
	}

	private static bool IsSplashAction(string command)
	{
		switch (command)
		{
		default:
			return command == "switch_event";
		case "attachment":
		case "switch_task":
		case "switch_note":
			return true;
		}
	}

	private static bool IsTaskKindSwitchAction(string command)
	{
		if (!(command == "switch_task") && !(command == "switch_note"))
		{
			return command == "switch_event";
		}
		return true;
	}

	private async void OnTemplateSelected(object sender, string templateId)
	{
		AddTaskViewModel addModel = base.DataContext as AddTaskViewModel;
		if (await ProChecker.CheckTaskLimit(addModel.ProjectId) || string.IsNullOrEmpty(templateId))
		{
			return;
		}
		QuickAddView quickAdd = GetQuickAddView();
		if (quickAdd != null)
		{
			TaskModel task = await TaskService.AddTaskFromTemplate(templateId, addModel, quickAdd.GetExtra());
			if (task != null)
			{
				quickAdd.NotifyAddTask(task);
				await Task.Delay(200);
				Utils.FindParent<TaskView>(quickAdd)?.GetTaskDetail()?.TryFocusDetail(focusEnd: true, task.id);
			}
		}
	}

	public async Task FocusText(bool focusEnd = false)
	{
		await Task.Delay(50);
		if (!IsFocusSuppressed())
		{
			EditBox.Focus();
			EditBox.TextArea.Focus();
			if (focusEnd)
			{
				EditBox.CaretOffset = EditBox.Text.Length;
			}
		}
	}

	public void ResetTokens()
	{
		_dateTokenGenerator?.Reset();
		_tokenGenerator?.Reset();
		_quickDate = string.Empty;
	}

	private QuickAddView GetQuickAddView()
	{
		return Utils.FindParent<QuickAddView>(this);
	}

	private void OnTagTokenSelected(QuickAddToken token, bool withPrefix = false)
	{
		if (_tokenGenerator.ExistToken(token))
		{
			return;
		}
		_tokenGenerator.TryAddToken(token);
		int num = EditBox.CaretOffset;
		if (!string.IsNullOrEmpty(_filterContent))
		{
			string text = EditBox.Text;
			string text2 = EditBox.Text;
			int num2 = text.IndexOf("#" + _filterContent, Math.Max(Math.Min(_startIndex, text.Length), 0), StringComparison.Ordinal);
			if (num2 >= 0)
			{
				string text3 = text.Substring(0, num2 + 1);
				string text4 = text.Substring(num2 + 1 + _filterContent.Length);
				text2 = text3 + text4;
				num = text3.Length;
			}
			EditBox.Text = ((!string.IsNullOrEmpty(_filterContent)) ? text2 : EditBox.Text.Substring(1));
		}
		string text5 = token.Value.Trim().Substring((!withPrefix) ? 1 : 0) + " ";
		EditBox.Document.Insert(num, text5);
		EditBox.Focus();
		EditBox.TextArea.Focus();
		EditBox.CaretOffset = num + text5.Length;
		this.TagSelected?.Invoke(this, token.Value);
	}

	private string TryGetMark(string sourceText, int caretOffset, out string content)
	{
		if (!string.IsNullOrEmpty(sourceText))
		{
			string text = sourceText.Substring(0, Math.Min(caretOffset, sourceText.Length));
			MatchCollection matchCollection = new Regex("(?=[/|、|#|@|~|^|!|！|\\*])").Matches(text);
			if (matchCollection.Count > 0)
			{
				Match match = matchCollection[matchCollection.Count - 1];
				if (text.Length > match.Index)
				{
					bool flag = ((match.Index > 0) ? text[match.Index - 1].ToString() : " ") == " ";
					string text2 = text[match.Index].ToString();
					content = text.Substring(match.Index + 1, text.Length - match.Index - 1);
					switch (text2)
					{
					case "/":
					case "、":
						if (flag)
						{
							_startIndex = match.Index;
							return text2;
						}
						break;
					default:
						if (content.Contains(" "))
						{
							break;
						}
						goto case "~";
					case "~":
					case "^":
					case "@":
						_startIndex = match.Index;
						return text2;
					}
				}
			}
		}
		content = string.Empty;
		_startIndex = -1;
		return string.Empty;
	}

	public async void OnTokenSelected(QuickAddToken token, bool withMark = false, bool handleText = true, bool closePopup = true, bool forceFocus = true)
	{
		EditBox.TextChanged -= EditBoxOnTextChanged;
		string text = token.Value.Substring(0, 1);
		if (!string.IsNullOrEmpty(_filterContent))
		{
			if (_filterContent.StartsWith(text))
			{
				_filterContent = _filterContent.Substring(1);
			}
			string text2 = EditBox.Text;
			string text3 = EditBox.Text;
			int num = -1;
			if (Math.Max(0, _startIndex) < EditBox.Text.Length)
			{
				num = EditBox.Text.IndexOf(text + _filterContent, Math.Max(0, _startIndex), StringComparison.Ordinal);
			}
			if (num >= 0)
			{
				string text4 = text2.Substring(0, num + 1);
				string text5 = text2.Substring(num + 1 + _filterContent.Length);
				text3 = text4 + text5;
			}
			EditBox.SetTextAndOffset((!string.IsNullOrEmpty(_filterContent)) ? text3 : EditBox.Text.Substring(1), restoreOffset: true);
		}
		QuickAddToken quickAddToken = _tokenGenerator.TryAddToken(token);
		if (quickAddToken != null && quickAddToken.Value != token.Value)
		{
			int num2 = EditBox.Text.IndexOf(quickAddToken.Value, StringComparison.Ordinal);
			if (num2 >= 0)
			{
				int num3 = Math.Min(quickAddToken.Value.Length + 1, EditBox.Text.Length - num2);
				EditBox.Document.Remove(num2, num3);
				if (_startIndex > 0)
				{
					_startIndex -= num3;
				}
			}
		}
		if (handleText)
		{
			int offset = EditBox.Text.IndexOf(text, Math.Min(EditBox.Text.Length, Math.Max(0, _startIndex)), StringComparison.Ordinal) + 1;
			string text6 = (withMark ? token.Value.Trim() : token.Value.Trim().Substring(1)) + " ";
			EditBox.Document.Insert(offset, text6);
			if (forceFocus && !EditBox.TextArea.IsKeyboardFocused)
			{
				EditBox.Focus();
				EditBox.TextArea.Focus();
			}
			EditBox.CaretOffset = EditBox.Text.Length;
		}
		if (closePopup)
		{
			_selectionControl?.ClosePopup(clearChildren: false);
		}
		EditBox.TextChanged += EditBoxOnTextChanged;
		_startIndex = -1;
	}

	private Point GetPopupOffset()
	{
		TextView textView = EditBox.TextArea.TextView;
		TextViewPosition position = new TextViewPosition(EditBox.Document.GetLocation(EditBox.CaretOffset));
		return textView.GetVisualPosition(position, VisualYPosition.LineBottom);
	}

	private async void EditBoxOnTextChanged(object sender, EventArgs e)
	{
		if (EditBox.Text?.Length > _origin?.Length)
		{
			_inputByKeyBoard = (DateTime.Now - _lastKeyDownTime).TotalSeconds < 2.0;
		}
		_dateTokenGenerator.CheckIgnoreToken(EditBox.Text);
		_dateParserHandler.TryDoAction();
		LogEmojiPosition();
		if (EditBox.TextArea.IsImeCompositionActive)
		{
			if (!_suppressQuickPopupForSplashAction)
			{
				TryShowQuickPopup();
			}
			else
			{
				_quickPopupImeRefresh.Cancel();
			}
		}
		else
		{
			_quickPopupImeRefresh.Cancel();
			if (!_suppressQuickPopupForSplashAction)
			{
				TryShowQuickPopup();
			}
		}
		SetTagTokens(EditBox.Text);
		SetPriorityTokens(EditBox.Text);
		SetProjectTokens(EditBox.Text);
		SetAssignTokens(EditBox.Text);
		if (string.IsNullOrEmpty(EditBox.Text))
		{
			GetQuickAddView()?.ResetHint();
		}
		_origin = EditBox.Text;
		this.TextChanged?.Invoke(sender, EditBox.Text);
	}

	private async void TryShowQuickPopupAfterImeComposition()
	{
		if (!_suppressQuickPopupForSplashAction)
		{
			await _quickPopupImeRefresh.RefreshAfterCompositionAsync(() => EditBox.TextArea.IsImeCompositionActive, () => base.IsLoaded && EditBox.TextArea.IsKeyboardFocused && !_suppressQuickPopupForSplashAction, delegate
			{
				TryShowQuickPopup();
			});
		}
	}

	public void RefreshQuickPopup()
	{
		_quickPopupImeRefresh.Cancel();
		TryShowQuickPopup();
	}

	private void TryParseDate()
	{
		IPaserDueDate paserDueDate = ParseDate();
		if (paserDueDate != null)
		{
			this.DateParsed?.Invoke(this, paserDueDate);
		}
	}

	public bool IsQuickAddPopupOpened()
	{
		return _selectionControl?.PopupOpened() ?? false;
	}

	private IPaserDueDate ParseDate()
	{
		QuickSelectionControl selectionControl = _selectionControl;
		if (selectionControl != null && selectionControl.PopupOpened())
		{
			return null;
		}
		QuickAddView quickAddView = GetQuickAddView();
		if (quickAddView != null)
		{
			if (!LocalSettings.Settings.DateParsing || quickAddView.ManualSelectedDate)
			{
				return null;
			}
			string text = EditBox.Text;
			if (_dateTokenGenerator.GetIgnoreTokens().Any())
			{
				text = _dateTokenGenerator.GetIgnoreTokens().Aggregate(text, (string current, string token) => current.Replace(token, string.Empty));
			}
			if (_tokenGenerator.GetTokens().Any())
			{
				text = _tokenGenerator.GetTokens().Aggregate(text, (string current, QuickAddToken token) => current.Replace(token.Value, string.Empty)).Trim();
			}
			bool recognizeDuration = UserDao.IsPro() && !GetQuickAddView().UseInNote();
			string content = ((text.Length > 512) ? text.Substring(0, 512) : text);
			IPaserDueDate paserDueDate = ParseDateWithKanbanDefault(quickAddView, content, recognizeDuration);
			if (paserDueDate != null && paserDueDate.GetRecognizeStrings().Any())
			{
				List<string> list = paserDueDate.GetRecognizeStrings()?.Select((string str) => str.TrimEnd()).ToList() ?? new List<string>();
				list.RemoveAll((string t) => t.EndsWith("："));
				_dateTokenGenerator.AddTokens(list, text);
			}
			else
			{
				_dateTokenGenerator.ClearTokens();
			}
			EditBox.TextArea.TextView.Redraw();
			return paserDueDate;
		}
		return null;
	}

	private static IPaserDueDate ParseDateWithKanbanDefault(QuickAddView quickAdd, string content, bool recognizeDuration)
	{
		DateTime? baseDate = ((quickAdd.Model.OriginalScenario != QuickAddView.Scenario.Kanban) ? ((DateTime?)null) : quickAdd.Model.OriginalTimeData?.StartDate);
		IPaserDueDate paserDueDate = DateParser.Parse(content, baseDate, recognizeDuration);
		if (!baseDate.HasValue || !HasExplicitDateToken(paserDueDate))
		{
			return paserDueDate;
		}
		return DateParser.Parse(content, null, recognizeDuration);
	}

	private static bool HasExplicitDateToken(IPaserDueDate parsedData)
	{
		List<string> list = (from token in parsedData?.GetRecognizeStrings()?.Where((string token) => !string.IsNullOrWhiteSpace(token))
			select token.Trim()).Distinct().ToList();
		if (list == null)
		{
			return false;
		}
		foreach (string item in list)
		{
			IPaserDueDate paserDueDate = DateParser.Parse(item, null, recognizeDuration: false);
			if (paserDueDate != null && paserDueDate.IsTimeSeted())
			{
				TimeData timeData = paserDueDate.ToTimeData(addDefaultReminder: false);
				if (timeData != null && timeData.IsAllDay == true && string.IsNullOrEmpty(timeData.RepeatFlag))
				{
					return true;
				}
			}
		}
		return false;
	}

	private void OnEditTextKeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key != Key.Back)
		{
			_lastKeyDownTime = DateTime.Now;
		}
		if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
		{
			_shiftPressed = true;
		}
		else if (e.Key != Key.Return && e.Key != Key.LeftCtrl && e.Key != Key.RightCtrl)
		{
			_shiftPressed = false;
		}
		if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
		{
			_ctrlPressed = true;
		}
		else if (e.Key != Key.Return && e.Key != Key.LeftShift && e.Key != Key.RightShift)
		{
			_ctrlPressed = false;
		}
		switch (e.Key)
		{
		case Key.ImeProcessed:
			_canEnter = false;
			break;
		case Key.Left:
			if (GetQuickAddView().HandleKey(Key.Left))
			{
				e.Handled = true;
				return;
			}
			break;
		case Key.Space:
			if (GetQuickAddView().HandleKey(Key.Space))
			{
				e.Handled = true;
				return;
			}
			break;
		case Key.Right:
			if (GetQuickAddView().HandleKey(Key.Right))
			{
				e.Handled = true;
				return;
			}
			break;
		case Key.Return:
		{
			e.Handled = true;
			QuickSelectionControl selectionControl2 = _selectionControl;
			if (selectionControl2 != null && selectionControl2.IsOpen())
			{
				_selectionControl?.TrySelectItem();
				_canEnter = false;
			}
			if (GetQuickAddView().HandleKey(Key.Return))
			{
				_canEnter = false;
				return;
			}
			break;
		}
		case Key.Tab:
		{
			e.Handled = true;
			QuickSelectionControl selectionControl3 = _selectionControl;
			if (selectionControl3 != null && selectionControl3.IsOpen())
			{
				_selectionControl?.TryMovePopupItem(Utils.IfShiftPressed());
				return;
			}
			if (GetQuickAddView().HandleKey(Key.Tab))
			{
				e.Handled = true;
				return;
			}
			break;
		}
		case Key.Up:
		{
			QuickSelectionControl selectionControl = _selectionControl;
			if (selectionControl != null && selectionControl.IsOpen())
			{
				_selectionControl?.TryMovePopupItem(forward: true);
				e.Handled = true;
			}
			if (GetQuickAddView().HandleKey(Key.Up))
			{
				e.Handled = true;
				return;
			}
			break;
		}
		case Key.Down:
		{
			QuickSelectionControl selectionControl4 = _selectionControl;
			if (selectionControl4 != null && selectionControl4.IsOpen())
			{
				_selectionControl?.TryMovePopupItem(forward: false);
				e.Handled = true;
			}
			else if (e.ImeProcessedKey == Key.None && !GetQuickAddView().IsInOperation && !GetQuickAddView().IsInSetProject())
			{
				_canMoveDown = true;
			}
			if (GetQuickAddView().HandleKey(Key.Down))
			{
				e.Handled = true;
				return;
			}
			break;
		}
		case Key.Escape:
			_quickPopupImeRefresh.Cancel();
			_selectionControl?.ClosePopup(clearChildren: false);
			if (GetQuickAddView().HandleKey(Key.Escape))
			{
				e.Handled = true;
				return;
			}
			break;
		case Key.D:
			if (Keyboard.Modifiers == ModifierKeys.Control && LocalSettings.Settings.ShortCutModel.SetDate == "Ctrl+D" && e.Key == Key.D)
			{
				e.Handled = true;
				GetQuickAddView()?.SelectDate();
			}
			break;
		case Key.Back:
			foreach (string validToken in _dateTokenGenerator.GetValidTokens())
			{
				int num = EditBox.Text.LastIndexOf(validToken, EditBox.CaretOffset, StringComparison.Ordinal);
				if (num < 0 || num != EditBox.CaretOffset - validToken.Length)
				{
					continue;
				}
				foreach (QuickAddToken token in _tokenGenerator.GetTokens())
				{
					if (token.Value.EndsWith(validToken) && EditBox.Text.LastIndexOf(token.Value, EditBox.CaretOffset, StringComparison.Ordinal) == EditBox.CaretOffset - token.Value.Length)
					{
						return;
					}
				}
				_dateTokenGenerator.AddIgnoreToken(validToken);
				EditBox.TextArea.TextView.Redraw();
				if (_inputByKeyBoard)
				{
					e.Handled = true;
				}
			}
			break;
		}
		this.TextKeyDown?.Invoke(this, e);
	}

	private void OnEditTextKeyUp(object sender, KeyEventArgs e)
	{
		switch (e.Key)
		{
		case Key.Return:
		{
			bool flag = true;
			QuickAddView quickAddView = GetQuickAddView();
			if (quickAddView != null)
			{
				flag = !quickAddView.IsInOperation;
			}
			if (!flag)
			{
				_canEnter = true;
				return;
			}
			if (Utils.IfShiftPressed() && !Utils.IfCtrlPressed())
			{
				_shiftPressed = false;
				NotifySplitText();
				e.Handled = true;
			}
			else if (_canEnter)
			{
				NotifyEnterText();
				e.Handled = true;
			}
			break;
		}
		case Key.Down:
			if (_canMoveDown)
			{
				this.MoveDown?.Invoke(this, null);
			}
			break;
		case Key.LeftCtrl:
		case Key.RightCtrl:
			DelayMuteCtrlPressed();
			break;
		}
		_canEnter = true;
		_canMoveDown = false;
	}

	private async void DelayMuteCtrlPressed()
	{
		await Task.Delay(100);
		_ctrlPressed = false;
	}

	private void NotifySplitText()
	{
		string titleContent = GetTitleContent();
		this.SplitText?.Invoke(this, titleContent);
	}

	public string GetTitleWithOutToken()
	{
		string text = EditBox.Text.Replace("\r\n", string.Empty);
		if (!string.IsNullOrEmpty(text))
		{
			List<QuickAddToken> source = (from token in _tokenGenerator.GetTokens()
				where token.TokenType != TokenType.QuickDate
				select token).ToList();
			if (source.Any())
			{
				text = source.Aggregate(text, (string current, QuickAddToken token) => current.Replace(token.Value, string.Empty));
			}
		}
		return text;
	}

	public string GetTitleContent(bool forceRemove = false)
	{
		string text = EditBox.Text.Replace("\r\n", string.Empty);
		if (!string.IsNullOrEmpty(text))
		{
			List<QuickAddToken> source = (from token in _tokenGenerator.GetTokens()
				where token.TokenType != TokenType.Tag
				select token).ToList();
			if (source.Any())
			{
				text = source.Aggregate(text, (string text2, QuickAddToken token) => text2.Replace(token.Value, string.Empty));
			}
			List<QuickAddToken> source2 = (from token in _tokenGenerator.GetTokens()
				where token.TokenType == TokenType.Tag
				select token).ToList();
			if (source2.Any() && (!LocalSettings.Settings.KeepTagsInText || forceRemove))
			{
				text = source2.Aggregate(text, (string text2, QuickAddToken token) => text2.Replace(token.Value, string.Empty));
			}
			List<string> validTokens = _dateTokenGenerator.GetValidTokens();
			if (validTokens.Any() && (LocalSettings.Settings.RemoveTimeText || forceRemove))
			{
				foreach (string item in validTokens)
				{
					int num = text.IndexOf(item, StringComparison.Ordinal);
					if (num >= 0 && num + item.Length <= text.Length)
					{
						text = text.Remove(num, item.Length);
					}
				}
			}
		}
		return text;
	}

	private void NotifyEnterText()
	{
		ParseDateImmediately();
		string titleContent = GetTitleContent();
		this.EnterText?.Invoke(this, titleContent);
	}

	private void AddTag(string tag)
	{
		QuickAddToken quickAddToken = QuickAddToken.BuildTag("#" + TagDataHelper.GetTagDisplayName(tag));
		if (_tokenGenerator.TryAddTagToken(quickAddToken))
		{
			EditBox.Text = quickAddToken.Value + " " + EditBox.Text;
		}
	}

	public void SetPriority(int priority)
	{
		GetQuickAddView()?.SetPriority(priority);
	}

	public void SetDate(string date)
	{
		GetQuickAddView()?.SetDate(date);
	}

	public void SelectDate()
	{
		GetQuickAddView()?.SelectDate();
	}

	public void ClearDate()
	{
		GetQuickAddView()?.ClearDate();
	}

	public void OnTagsAdded(TagSelectData data)
	{
		string text = EditBox.Text;
		foreach (string omniSelectTag in data.OmniSelectTags)
		{
			QuickAddToken quickAddToken = QuickAddToken.BuildTag("#" + TagDataHelper.GetTagDisplayName(omniSelectTag));
			if (_tokenGenerator.TryAddTagToken(quickAddToken))
			{
				text = quickAddToken.Value + " " + text;
			}
		}
		foreach (QuickAddToken tagToken in _tokenGenerator.GetTagTokens())
		{
			if (!data.OmniSelectTags.Contains(tagToken.Value.Substring(1).ToLower()))
			{
				_tokenGenerator.RemoveToken(tagToken, notify: false, removeTemp: false);
				text = text.Replace(tagToken.Value + " ", "");
			}
		}
		EditBox.Text = text;
		EditBox.CaretOffset = text.Length;
	}

	public void RemoveTokenByType(TokenType type, bool removeText = true)
	{
		List<QuickAddToken> list = _tokenGenerator.RemoveTokenByType(type);
		if (removeText)
		{
			string text = EditBox.Text.Replace("\r\n", string.Empty);
			foreach (QuickAddToken item in list)
			{
				string value = item.Value;
				if (text.Contains(value + " "))
				{
					text = text.Replace(value + " ", "");
				}
				else if (text.Contains(value))
				{
					text = text.Replace(value, "");
				}
			}
			if (EditBox.Text != text)
			{
				EditBox.SetTextAndOffset(text, restoreOffset: true);
			}
		}
		ForceRender();
	}

	private void EditorMenuOnContextMenuOpening(object sender, ContextMenuEventArgs context)
	{
		AddEditorContextMenu(context);
	}

	public void AddEditorContextMenu(ContextMenuEventArgs context)
	{
		ContextMenu contextMenu = new ContextMenu
		{
			Width = 230.0
		};
		contextMenu.SetResourceReference(FrameworkElement.StyleProperty, "ContextMenuStyle");
		contextMenu.SetResourceReference(Control.BackgroundProperty, "PopupBackground");
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
			IDataObject dataObject = Clipboard.GetDataObject();
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
			CommandTarget = EditBox.TextArea,
			Shortcut = "Ctrl+Z"
		});
		contextMenu.Items.Add(new EditMenuItem
		{
			Title = Utils.GetString("Redo"),
			Command = ApplicationCommands.Redo,
			CommandTarget = EditBox.TextArea,
			Shortcut = "Ctrl+Shift+Z"
		});
		((FrameworkElement)context.Source).ContextMenu = contextMenu;
	}

	private void ParseDateImmediately()
	{
		_dateParserHandler.CancelAction();
		TryParseDate();
	}

	public void RemoveQuickMark1(int offset, params string[] marks)
	{
		int markIndexLength = Utils.GetMarkIndexLength(offset, EditBox.Text, marks);
		if (markIndexLength >= 0)
		{
			EditBox.Document.Remove(markIndexLength, 1);
		}
	}

	public void RemoveQuickMark(int offset, params string[] marks)
	{
		int markIndexLength = Utils.GetMarkIndexLength(offset, EditBox.Text, marks);
		if (markIndexLength >= 0)
		{
			int length = offset - markIndexLength;
			EditBox.Document.Remove(markIndexLength, length);
		}
	}

	public void RemoveTokenText(ref string text)
	{
		foreach (QuickAddToken token in _tokenGenerator.GetTokens())
		{
			text = text.Replace(token.Value, "");
		}
	}

	public bool? UseInWidgetDark()
	{
		return GetQuickAddView()?.UseInWidgetDark();
	}

	public void ClearHighlightDate()
	{
		_dateTokenGenerator.Reset();
		_quickDate = string.Empty;
		EditBox.TextArea.TextView.Redraw();
	}

	public Dictionary<int, string> GetEmojiDict()
	{
		return EmojiDict;
	}

	public void RemoveTagTokenText()
	{
		string text = EditBox.Text;
		foreach (QuickAddToken token in _tokenGenerator.GetTokens())
		{
			if (token.TokenType == TokenType.Tag)
			{
				text = text.Replace(token.Value, "");
			}
		}
		EditBox.Text = text;
	}

	public bool ExistToken(TokenType type)
	{
		return _tokenGenerator.ExistToken(type);
	}

	public int? GetPriorityInTokens()
	{
		List<QuickAddToken> tokensByType = _tokenGenerator.GetTokensByType(TokenType.Priority);
		if (tokensByType.Count > 0)
		{
			return TokenGenerator.GetPriorityByText(tokensByType[0].Value);
		}
		return null;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/quickadd/quickaddtext.xaml", UriKind.Relative);
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
			Root = (QuickAddText)target;
			Root.ContextMenuOpening += EditorMenuOnContextMenuOpening;
			break;
		case 2:
			Grid = (Grid)target;
			break;
		case 3:
			EditBox = (TextEditor)target;
			EditBox.PreviewKeyDown += OnEditTextKeyDown;
			EditBox.PreviewKeyUp += OnEditTextKeyUp;
			break;
		case 4:
			SelectionPopup = (Popup)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
