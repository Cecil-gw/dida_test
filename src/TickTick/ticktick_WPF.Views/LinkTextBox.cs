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
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.QuickAdd;
using ticktick_WPF.Views.Tag;
using ticktick_WPF.Views.TaskList;
using ticktick_WPF.Views.TaskList.Item;

namespace ticktick_WPF.Views;

public class LinkTextBox : UserControl, IComponentConnector
{
	public new static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register("Foreground", typeof(Brush), typeof(LinkTextBox), new PropertyMetadata(new SolidColorBrush(Colors.Black), OnForegroundChangedCallback));

	public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(LinkTextBox), new PropertyMetadata(string.Empty, OnTextChangedCallback));

	public static readonly DependencyProperty TextWrappingProperty = DependencyProperty.Register("TextWrapping", typeof(TextWrapping), typeof(LinkTextBox), new PropertyMetadata(TextWrapping.NoWrap, OnTextWrappingChangedCallback));

	public static readonly DependencyProperty AcceptsReturnProperty = DependencyProperty.Register("AcceptsReturn", typeof(bool), typeof(LinkTextBox), new PropertyMetadata(false, OnAcceptsReturnChangedCallback));

	public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(LinkTextBox), new PropertyMetadata(false, OnIsReadOnlyChangedCallback));

	private bool _canMerge = true;

	private bool _canSplit;

	private int _currentCaretLine;

	private DateTime _lastTagAddTime = DateTime.Now;

	private DateTime _lastTextChangeTime = DateTime.Now;

	private bool _originalAcceptReturn;

	private bool _rightClick;

	private TagSelectAddWindow _tagPopup;

	private ListItemContent _taskParent;

	private int _tagStartIndex = -1;

	private bool _textKeyPressed;

	private IQuickInput _quickInputItems;

	private int _startIndex = -1;

	internal LinkTextBox Root;

	internal TextBlock TextBlock;

	internal TextBox TextBox;

	internal Popup SelectionPopup;

	internal Grid ItemsContainer;

	private bool _contentLoaded;

	public new bool IsFocused => TextBox.IsFocused;

	public Window Context { private get; set; } = App.Window;

	public double LineHeightScale { get; set; } = 1.5;

	protected double TagPopupVerticalOffset { get; set; } = 4.0;

	public string Text
	{
		get
		{
			return TextBox.Text;
		}
		set
		{
			TextBox.TextChanged -= NotifyTextChanged;
			TextBox.Text = value;
			TextBox.TextChanged += NotifyTextChanged;
		}
	}

	public TextWrapping TextWrapping
	{
		get
		{
			return (TextWrapping)GetValue(TextWrappingProperty);
		}
		set
		{
			SetValue(TextWrappingProperty, value);
		}
	}

	public bool AcceptsReturn
	{
		private get
		{
			return (bool)GetValue(AcceptsReturnProperty);
		}
		set
		{
			SetValue(AcceptsReturnProperty, value);
		}
	}

	public bool IsReadOnly
	{
		get
		{
			return (bool)GetValue(IsReadOnlyProperty);
		}
		set
		{
			SetValue(IsReadOnlyProperty, value);
		}
	}

	public new Brush Foreground
	{
		get
		{
			return (Brush)GetValue(ForegroundProperty);
		}
		set
		{
			SetValue(ForegroundProperty, value);
		}
	}

	public int CaretIndex => TextBox.CaretIndex;

	public event EventHandler<string> TextChanged;

	public event EventHandler TextGotFocus;

	public event EventHandler TextLostFocus;

	public event EventHandler<string> MergeText;

	public event EventHandler<SplitData> SplitText;

	public event EventHandler MoveUp;

	public event EventHandler MoveDown;

	public event EventHandler<int> SelectionChanged;

	public event EventHandler<string> TagSelected;

	public event EventHandler TagPopOpened;

	public event EventHandler TagPopClosed;

	public event EventHandler<ProjectTask> Navigate;

	public LinkTextBox()
	{
		InitializeComponent();
		InitEvents();
	}

	public void SetMaxLength(int maxLength)
	{
		TextBox.MaxLength = maxLength;
	}

	private static void OnTextWrappingChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		if (sender is LinkTextBox linkTextBox && e.NewValue != null)
		{
			linkTextBox.TextBox.TextWrapping = (TextWrapping)e.NewValue;
			linkTextBox.TextBlock.TextWrapping = (TextWrapping)e.NewValue;
		}
	}

	private static void OnForegroundChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		if (sender is LinkTextBox linkTextBox && e.NewValue != null)
		{
			linkTextBox.TextBox.Foreground = (Brush)e.NewValue;
			linkTextBox.TextBlock.Foreground = (Brush)e.NewValue;
		}
	}

	private static void OnAcceptsReturnChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		if (sender is LinkTextBox linkTextBox && e.NewValue != null)
		{
			linkTextBox.TextBox.AcceptsReturn = (bool)e.NewValue;
		}
	}

	private static void OnIsReadOnlyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		if (sender is LinkTextBox linkTextBox && e.NewValue != null)
		{
			linkTextBox.TextBox.IsReadOnly = (bool)e.NewValue;
		}
	}

	private static void OnTextChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		if (sender is LinkTextBox linkTextBox && e.NewValue != null)
		{
			linkTextBox.TextBox.Text = e.NewValue.ToString();
		}
	}

	private void InitEvents()
	{
		TextBox.SelectionChanged += delegate
		{
			this.SelectionChanged?.Invoke(this, TextBox.CaretIndex);
		};
		TextBox.TextChanged += RenderLinks;
		TextBox.TextChanged -= NotifyTextChanged;
		TextBox.TextChanged += NotifyTextChanged;
	}

	public void FocusText(int caretIndex = -1)
	{
		TextBlock.Visibility = Visibility.Hidden;
		TextBox.Visibility = Visibility.Visible;
		TextBox.Focus();
		TextBox.CaretIndex = ((caretIndex == -1) ? TextBox.Text.Length : caretIndex);
	}

	private async void OnTextBlockClick(object sender, MouseButtonEventArgs e)
	{
		TextBlock.Visibility = Visibility.Hidden;
		TextBox.Visibility = Visibility.Visible;
		await Task.Delay(10);
		TextBox.Focus();
		try
		{
			if (TextBox.GetCharacterIndexFromPoint(Mouse.GetPosition(TextBox), snapToText: true) > 0)
			{
				TextBox.CaretIndex = TextBox.GetCharacterIndexFromPoint(Mouse.GetPosition(TextBox), snapToText: true) + 1;
			}
		}
		catch (Exception)
		{
		}
	}

	private void OnLeftClick(object sender, MouseButtonEventArgs e)
	{
		_rightClick = false;
	}

	private void OnRightClick(object sender, MouseButtonEventArgs e)
	{
		_rightClick = true;
	}

	private void OnTextBoxGotFocus(object sender, RoutedEventArgs e)
	{
		if (!_rightClick)
		{
			this.TextGotFocus?.Invoke(this, null);
		}
	}

	private async void OnTextBoxSelectionChanged(object sender, RoutedEventArgs e)
	{
		await Task.Delay(150);
		_currentCaretLine = TextBox.GetLineIndexFromCharacterIndex(TextBox.SelectionStart);
	}

	private async void OnTextBoxLostFocus(object sender, RoutedEventArgs e)
	{
		this.TextLostFocus?.Invoke(this, null);
		if (TextBlock.Visibility != Visibility.Visible)
		{
			RenderLinks();
		}
	}

	private void NotifyTextChanged(object sender, TextChangedEventArgs e)
	{
		if (!TextBox.IsFocused)
		{
			ICollection<TextChange> changes = e.Changes;
			if (changes == null || changes.Count <= 0)
			{
				return;
			}
		}
		_lastTextChangeTime = DateTime.Now;
		if (e.Changes != null && e.Changes.Count == 1)
		{
			foreach (TextChange change in e.Changes)
			{
				if (change.RemovedLength == 1 && TextBox.CaretIndex == 0)
				{
					_canMerge = false;
				}
			}
		}
		this.TextChanged?.Invoke(this, TextBox.Text);
	}

	private async void RenderLinks(object sender, TextChangedEventArgs textChange)
	{
		TextBox.Visibility = Visibility.Visible;
		if (_quickInputItems != null)
		{
			if (TextBox.CaretIndex < _startIndex || MarkChanged())
			{
				ClearSelection();
			}
			else
			{
				string key = TextBox.Text.Substring(_startIndex, TextBox.CaretIndex - _startIndex);
				SelectionPopup.IsOpen = _quickInputItems.Filter(key);
			}
		}
		if (TextBox.IsFocused && Utils.CheckIfAddText(textChange))
		{
			await Task.Delay(20);
			string sentileInput = GetSentileInput(textChange);
			if (TextBox.Text == "#" || sentileInput == " #" || sentileInput == "\n#" || TextBox.Text == "＃" || sentileInput == " ＃" || sentileInput == "\n＃")
			{
				ShowTagPopup();
				return;
			}
			if (GetParent() != null)
			{
				string mark = ((sentileInput.Length > 1) ? sentileInput.Substring(sentileInput.Length - 1, 1) : TextBox.Text);
				if (TextBox.Text == "!" || sentileInput == " !" || sentileInput == "\n!" || TextBox.Text == "！" || sentileInput == " ！" || sentileInput == "\n！")
				{
					if (GetParent().IsNote())
					{
						return;
					}
					TryShowQuickPopup(mark);
				}
				if (TextBox.Text == "^" || sentileInput == " ^" || sentileInput == "\n^" || TextBox.Text == "~" || sentileInput == " ~" || sentileInput == "\n~")
				{
					TryShowQuickPopup(mark);
				}
				if (TextBox.Text == "@" || sentileInput == " @" || sentileInput == "\n@")
				{
					TryShowQuickPopup(mark);
				}
				if (TextBox.Text == "*" || sentileInput == " *" || sentileInput == "\n*")
				{
					TryShowQuickPopup(mark);
				}
				return;
			}
		}
		if (!TextBox.IsFocused)
		{
			RenderLinks();
		}
	}

	private ListItemContent GetParent()
	{
		_taskParent = _taskParent ?? Utils.FindParent<ListItemContent>(this);
		return _taskParent;
	}

	private bool MarkChanged()
	{
		if (_startIndex <= TextBox.Text.Length && _startIndex > 0 && SelectionPopup.Tag is string text && TextBox.Text.Substring(_startIndex - 1, 1) == text)
		{
			return false;
		}
		return true;
	}

	private string GetSentileInput(TextChangedEventArgs textChange)
	{
		if (textChange.Changes != null && textChange.Changes.Count == 1)
		{
			int num = textChange.Changes.First().Offset - 1;
			if (num >= 0 && num + 2 <= TextBox.Text.Length)
			{
				return TextBox.Text.Substring(num, 2);
			}
		}
		return string.Empty;
	}

	private void RenderLinks()
	{
		TextBlock.Inlines.Clear();
		string text = TextBox.Text.Replace("\r\n", "\n");
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		int num = 0;
		MatchCollection matchCollection = TaskUtils.UrlRegex.Matches(text.ToLower());
		if (matchCollection.Count <= 0)
		{
			return;
		}
		TextBlock.Visibility = Visibility.Visible;
		TextBox.Visibility = Visibility.Collapsed;
		foreach (Match item in matchCollection)
		{
			if (item.Index != num)
			{
				TextBlock.Inlines.Add(new Run(text.Substring(num, item.Index - num)));
			}
			Hyperlink hyperlink = new Hyperlink(new Run(text.Substring(item.Index, item.Value.Length)))
			{
				Tag = text.Substring(item.Index, item.Value.Length)
			};
			hyperlink.Click += OnUrlClick;
			TextBlock.Inlines.Add(hyperlink);
			num = item.Index + item.Length;
		}
		if (num < text.Length)
		{
			TextBlock.Inlines.Add(new Run(text.Substring(num)));
		}
	}

	private async void ShowTagPopup()
	{
		_tagPopup = new TagSelectAddWindow();
		_tagPopup.TagItems.TagSelected -= OnTagSelected;
		_tagPopup.TagItems.TagSelected += OnTagSelected;
		_tagPopup.Closed -= OnTagPopupClosed;
		_tagPopup.Closed += OnTagPopupClosed;
		_tagPopup.TagExit -= OnTagInputExit;
		_tagPopup.TagExit += OnTagInputExit;
		Rect rectFromCharacterIndex = TextBox.GetRectFromCharacterIndex(TextBox.CaretIndex);
		Point popupOffset = Utils.GetPopupOffset(Context, this, rectFromCharacterIndex.X, rectFromCharacterIndex.Y + TagPopupVerticalOffset);
		if (Math.Abs(popupOffset.X) > 0.0 && Math.Abs(popupOffset.Y) > 0.0)
		{
			_tagPopup.Show(popupOffset.X, popupOffset.Y);
			this.TagPopOpened?.Invoke(this, null);
			int caretIndex = TextBox.CaretIndex;
			_tagStartIndex = caretIndex;
		}
	}

	private async void OnTagInputExit(object sender, string content)
	{
		if (_tagStartIndex > 0)
		{
			string text = TextBox.Text;
			string text2 = text.Substring(0, Math.Min(_tagStartIndex - 1, text.Length)) + content + text.Substring(Math.Min(_tagStartIndex, text.Length));
			TextBox.Text = text2;
			int startIndex = _tagStartIndex;
			_tagStartIndex = -1;
			await Task.Delay(100);
			TextBox.Focus();
			int val = startIndex + content.Length - 1;
			TextBox.CaretIndex = Math.Max(val, 0);
		}
	}

	private async void OnTagPopupClosed(object sender, EventArgs e)
	{
		_lastTagAddTime = DateTime.Now;
		this.TagPopClosed?.Invoke(this, null);
		await Task.Delay(100);
		TextBox.Focus();
	}

	private async void OnTagSelected(object sender, string tag)
	{
		if (_tagStartIndex > 0)
		{
			string text = TextBox.Text;
			string text2 = text.Substring(0, _tagStartIndex - 1) + text.Substring(Math.Min(text.Length, _tagStartIndex));
			TextBox.Text = text2;
			this.TextChanged?.Invoke(this, text2);
			await Task.Delay(40);
			this.TagSelected?.Invoke(this, tag.ToLower());
			await Task.Delay(40);
			TextBox.Focus();
			TextBox.CaretIndex = _tagStartIndex;
			_tagStartIndex = -1;
		}
	}

	private void OnUrlClick(object sender, RoutedEventArgs e)
	{
		string text = ((Hyperlink)sender).Tag.ToString();
		if (text.EndsWith(")"))
		{
			text = text.Substring(0, text.Length - 1);
		}
		if (text.StartsWith(" #") || text.StartsWith("#") || text.StartsWith("\n#"))
		{
			App.SelectTagProject(text.Replace(" #", "#").Replace("\n#", "#"));
			return;
		}
		ProjectTask projectTask = TaskUtils.ParseTaskUrlWithoutTitle(text);
		if (projectTask != null)
		{
			Utils.FindParent<IListViewParent>(this)?.NavigateTask(projectTask);
		}
		else
		{
			Utils.TryProcessStartUrl(text);
		}
	}

	private void OnKeyUp(object sender, KeyEventArgs e)
	{
		switch (e.Key)
		{
		case Key.Return:
			if (SelectionPopup.IsOpen)
			{
				_quickInputItems.TrySelectItem();
				return;
			}
			if (_canSplit)
			{
				TrySplit();
			}
			break;
		case Key.Back:
			TryMerge();
			break;
		case Key.Up:
			TryMoveUp();
			break;
		case Key.Down:
			TryMoveDown();
			break;
		case Key.Escape:
			ClearSelection();
			break;
		}
		_textKeyPressed = false;
	}

	private void TrySplit()
	{
		if ((DateTime.Now - _lastTextChangeTime).TotalMilliseconds < 100.0 || (DateTime.Now - _lastTagAddTime).TotalMilliseconds < 500.0)
		{
			return;
		}
		int caretIndex = TextBox.CaretIndex;
		if (caretIndex > TextBox.Text.Length)
		{
			return;
		}
		try
		{
			string text = TextBox.Text.Substring(0, caretIndex);
			string extra = TextBox.Text.Substring(caretIndex);
			this.SplitText?.Invoke(this, new SplitData(text, extra));
		}
		catch (Exception)
		{
		}
	}

	private async void TryMoveDown()
	{
		if (SelectionPopup.IsOpen)
		{
			_quickInputItems.Move(forward: false);
		}
		else if (CanMoveDown())
		{
			this.MoveDown?.Invoke(this, null);
		}
	}

	private bool CanMoveDown()
	{
		int lineIndexFromCharacterIndex = TextBox.GetLineIndexFromCharacterIndex(TextBox.Text.Length);
		return _currentCaretLine == lineIndexFromCharacterIndex;
	}

	private async void TryMoveUp()
	{
		if (SelectionPopup.IsOpen)
		{
			_quickInputItems.Move(forward: true);
		}
		else if (_currentCaretLine == 0)
		{
			this.MoveUp?.Invoke(this, null);
		}
	}

	private async void TryMerge()
	{
		if (_canMerge && TextBox.CaretIndex == 0)
		{
			this.MergeText?.Invoke(this, TextBox.Text);
		}
	}

	private void OnTextKeyDown(object sender, KeyEventArgs e)
	{
		if ((e.Key == Key.Left || e.Key == Key.Right) && SelectionPopup.IsOpen)
		{
			e.Handled = true;
		}
		if (e.ImeProcessedKey == Key.Return)
		{
			_canSplit = false;
		}
		if (e.Key == Key.Return)
		{
			_canSplit = true;
		}
		if (!_textKeyPressed)
		{
			_textKeyPressed = true;
			_canMerge = e.Key == Key.Back;
		}
	}

	public new bool Focus()
	{
		return TextBox.Focus();
	}

	public async void FocusEnd()
	{
		if (TextBox.Visibility != Visibility.Visible)
		{
			TextBlock.Visibility = Visibility.Hidden;
			TextBox.Visibility = Visibility.Visible;
			await Task.Delay(10);
		}
		Focus();
		TextBox.Select(TextBox.Text.Length, 0);
	}

	public async void FocusFirst()
	{
		if (TextBox.Visibility != Visibility.Visible)
		{
			TextBlock.Visibility = Visibility.Hidden;
			TextBox.Visibility = Visibility.Visible;
			await Task.Delay(10);
		}
		Focus();
		TextBox.Select(0, 0);
	}

	private async Task TryShowQuickPopup(string mark)
	{
		if (mark != null)
		{
			int length = mark.Length;
			if (length == 1)
			{
				switch (mark[0])
				{
				case '!':
				case '！':
					_quickInputItems = new PriorityInputItems((await GetTaskDisplayModel())?.Priority ?? (-1));
					((PriorityInputItems)_quickInputItems).OnItemSelected -= ItemSelected;
					((PriorityInputItems)_quickInputItems).OnItemSelected += ItemSelected;
					break;
				case '^':
				case '~':
					_quickInputItems = new ProjectInputItems((await GetTaskDisplayModel())?.ProjectId);
					((ProjectInputItems)_quickInputItems).OnItemSelected -= ItemSelected;
					((ProjectInputItems)_quickInputItems).OnItemSelected += ItemSelected;
					break;
				case '*':
					_quickInputItems = new DateInputItems();
					((DateInputItems)_quickInputItems).OnItemSelected -= ItemSelected;
					((DateInputItems)_quickInputItems).OnItemSelected += ItemSelected;
					break;
				case '@':
					_quickInputItems = new AssignInputItems(await GetAvatars());
					((AssignInputItems)_quickInputItems).OnItemSelected -= ItemSelected;
					((AssignInputItems)_quickInputItems).OnItemSelected += ItemSelected;
					break;
				case '/':
				case '、':
					_quickInputItems = new SplashInputItems(!(await GetTaskDisplayModel()).IsNote, withAttachment: false);
					((SplashInputItems)_quickInputItems).OnItemSelected -= ItemSelected;
					((SplashInputItems)_quickInputItems).OnItemSelected += ItemSelected;
					break;
				}
			}
		}
		if (_quickInputItems == null)
		{
			return;
		}
		Point popupOffset = GetPopupOffset();
		if (_quickInputItems.Filter(""))
		{
			ItemsContainer.Children.Clear();
			if (_quickInputItems is UIElement element)
			{
				ItemsContainer.Children.Add(element);
			}
			SelectionPopup.HorizontalOffset = popupOffset.X;
			SelectionPopup.VerticalOffset = 0.0;
			SelectionPopup.IsOpen = true;
			SelectionPopup.Tag = mark;
		}
		_startIndex = TextBox.CaretIndex;
	}

	private void ClearSelection()
	{
		_quickInputItems = null;
		ItemsContainer.Children.Clear();
		SelectionPopup.IsOpen = false;
		SelectionPopup.Tag = "";
		_startIndex = -1;
	}

	private Point GetPopupOffset()
	{
		Rect rectFromCharacterIndex = TextBox.GetRectFromCharacterIndex(TextBox.CaretIndex);
		return new Point(rectFromCharacterIndex.X, rectFromCharacterIndex.Y);
	}

	private async Task<TaskBaseViewModel> GetTaskDisplayModel()
	{
		ITaskOperation taskOperation = Utils.FindParent<ITaskOperation>(this);
		if (taskOperation != null && taskOperation is Control { DataContext: DisplayItemModel dataContext })
		{
			return dataContext.SourceViewModel;
		}
		return null;
	}

	private async Task<List<AvatarViewModel>> GetAvatars()
	{
		ITaskOperation taskOperation = Utils.FindParent<ITaskOperation>(this);
		if (taskOperation != null && taskOperation is Control { DataContext: var dataContext })
		{
			DisplayItemModel model = dataContext as DisplayItemModel;
			if (model != null)
			{
				ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == model.ProjectId);
				if (projectModel != null && projectModel.IsShareList())
				{
					return await AvatarHelper.GetProjectAvatars(model.ProjectId, showMyName: true, fetchRemote: true);
				}
			}
		}
		return null;
	}

	private async void ItemSelected<T>(object sender, InputItemViewModel<T> e)
	{
		int startIndex = _startIndex;
		int caretIndex = TextBox.CaretIndex;
		TextBox.Text = TextBox.Text.Remove(startIndex - 1, caretIndex - _startIndex + 1);
		TextBox.Focus();
		TextBox.CaretIndex = startIndex - 1;
		await Task.Delay(30);
		ClearSelection();
	}

	public async void FocusOnMove()
	{
		if (TextBox.Visibility != Visibility.Visible)
		{
			TextBlock.Visibility = Visibility.Hidden;
			TextBox.Visibility = Visibility.Visible;
			await Task.Delay(10);
		}
		Utils.Focus(TextBox);
		TextBox.Select(TextBox.Text.Length, 0);
		TextBox.ScrollToHorizontalOffset(10000.0);
	}

	public void SetText(string text)
	{
		Text = text;
		_lastTextChangeTime = DateTime.Now;
		this.TextChanged?.Invoke(this, text);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/linktextbox.xaml", UriKind.Relative);
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
			Root = (LinkTextBox)target;
			break;
		case 2:
			TextBlock = (TextBlock)target;
			TextBlock.MouseLeftButtonUp += OnTextBlockClick;
			break;
		case 3:
			TextBox = (TextBox)target;
			TextBox.GotFocus += OnTextBoxGotFocus;
			TextBox.LostFocus += OnTextBoxLostFocus;
			TextBox.KeyUp += OnKeyUp;
			TextBox.SelectionChanged += OnTextBoxSelectionChanged;
			TextBox.PreviewMouseRightButtonDown += OnRightClick;
			TextBox.PreviewMouseLeftButtonDown += OnLeftClick;
			TextBox.PreviewKeyDown += OnTextKeyDown;
			break;
		case 4:
			SelectionPopup = (Popup)target;
			break;
		case 5:
			ItemsContainer = (Grid)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
