using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using Emoji.Wpf;
using ICSharpCode.AvalonEdit;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Detail;

namespace ticktick_WPF.Views.MarkDown;

public class EmojiEditor : UserControl, IEmojiRender, ILinkTextEditor, IComponentConnector
{
	public readonly Dictionary<int, string> EmojiDict = new Dictionary<int, string>();

	public new static readonly DependencyProperty FontSizeProperty = DependencyProperty.Register("FontSize", typeof(double), typeof(EmojiEditor), new PropertyMetadata(14.0));

	public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(EmojiEditor), new PropertyMetadata(null, OnTextChanged));

	public static readonly DependencyProperty TextPaddingProperty = DependencyProperty.Register("TextPadding", typeof(Thickness), typeof(EmojiEditor), new PropertyMetadata(new Thickness(0.0)));

	public new static readonly DependencyProperty BorderBrushProperty = DependencyProperty.Register("BorderBrush", typeof(Brush), typeof(EmojiEditor), new PropertyMetadata(Brushes.Transparent));

	public static readonly DependencyProperty BorderBackgroundProperty = DependencyProperty.Register("BorderBackground", typeof(Brush), typeof(EmojiEditor), new PropertyMetadata(Brushes.Transparent));

	public new static readonly DependencyProperty BorderThicknessProperty = DependencyProperty.Register("BorderThickness", typeof(Thickness), typeof(EmojiEditor), new PropertyMetadata(new Thickness(0.0)));

	public static readonly DependencyProperty BorderCornerProperty = DependencyProperty.Register("BorderCorner", typeof(CornerRadius), typeof(EmojiEditor), new PropertyMetadata(new CornerRadius(0.0)));

	public new static readonly DependencyProperty PaddingProperty = DependencyProperty.Register("Padding", typeof(Thickness), typeof(EmojiEditor), new PropertyMetadata(new Thickness(0.0)));

	public new static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register("Foreground", typeof(Brush), typeof(EmojiEditor), new PropertyMetadata(Brushes.Black));

	public new static readonly DependencyProperty FontWeightProperty = DependencyProperty.Register("FontWeight", typeof(FontWeight), typeof(EmojiEditor), new PropertyMetadata(FontWeights.Normal));

	public static readonly DependencyProperty ReadOnlyProperty = DependencyProperty.Register("ReadOnly", typeof(bool), typeof(EmojiEditor), new PropertyMetadata(false, OnIsReadOnlyChangedCallback));

	public static readonly DependencyProperty FocusedProperty = DependencyProperty.Register("Focused", typeof(bool), typeof(EmojiEditor), new PropertyMetadata(false));

	public static readonly DependencyProperty AcceptReturnProperty = DependencyProperty.Register("AcceptReturn", typeof(bool), typeof(EmojiEditor), new PropertyMetadata(false));

	public static readonly DependencyProperty WordWrapProperty = DependencyProperty.Register("WordWrap", typeof(bool), typeof(EmojiEditor), new PropertyMetadata(true, OnWordWrapChanged));

	public static readonly DependencyProperty TextVerticalAlignmentProperty = DependencyProperty.Register("TextVerticalAlignment", typeof(VerticalAlignment), typeof(EmojiEditor), new PropertyMetadata(VerticalAlignment.Center, OnTextVerticalAlignmentChanged));

	private bool _enterDown;

	private readonly CustomLinkElementGenerator _linkGenerator;

	internal EmojiEditor Root;

	internal Border TextBorder;

	internal TextBlock HintText;

	internal TextEditor EditBox;

	private bool _contentLoaded;

	public new double FontSize
	{
		get
		{
			return EditBox.FontSize;
		}
		set
		{
			SetValue(FontSizeProperty, value);
		}
	}

	public string Text
	{
		get
		{
			return EditBox.Text;
		}
		set
		{
			EditBox.Text = value;
			SetCurrentValue(TextProperty, value);
			EditBox.TextArea.TextView.LineTransformers.Clear();
			LogEmojiPosition();
			RefreshHintVisibility();
			EditBox.TextArea.TextView.Redraw();
		}
	}

	public Thickness TextPadding
	{
		get
		{
			return (Thickness)GetValue(TextPaddingProperty);
		}
		set
		{
			SetValue(TextPaddingProperty, value);
		}
	}

	public new Brush BorderBrush
	{
		get
		{
			return TextBorder.BorderBrush;
		}
		set
		{
			SetValue(BorderBrushProperty, value);
		}
	}

	public Brush BorderBackground
	{
		get
		{
			return TextBorder.Background;
		}
		set
		{
			SetValue(BorderBackgroundProperty, value);
		}
	}

	public new Thickness BorderThickness
	{
		get
		{
			return TextBorder.BorderThickness;
		}
		set
		{
			SetValue(BorderThicknessProperty, value);
		}
	}

	public CornerRadius BorderCorner
	{
		get
		{
			return TextBorder.CornerRadius;
		}
		set
		{
			SetValue(BorderCornerProperty, value);
		}
	}

	public new Thickness Padding
	{
		get
		{
			return TextBorder.Padding;
		}
		set
		{
			SetValue(PaddingProperty, value);
		}
	}

	public new Brush Foreground
	{
		get
		{
			return EditBox.Foreground;
		}
		set
		{
			SetValue(ForegroundProperty, value);
		}
	}

	public new FontWeight FontWeight
	{
		get
		{
			return EditBox.FontWeight;
		}
		set
		{
			SetValue(FontWeightProperty, value);
		}
	}

	public bool Focused
	{
		get
		{
			return (bool)GetValue(FocusedProperty);
		}
		set
		{
			SetValue(FocusedProperty, value);
		}
	}

	public bool WordWrap
	{
		get
		{
			return EditBox.WordWrap;
		}
		set
		{
			EditBox.WordWrap = value;
			EditBox.SetValue(ScrollViewer.PanningModeProperty, (!value) ? PanningMode.HorizontalFirst : PanningMode.None);
			EditBox.HorizontalScrollBarVisibility = ((!value) ? ScrollBarVisibility.Hidden : ScrollBarVisibility.Disabled);
			EditBox.VerticalScrollBarVisibility = (value ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled);
			EditBox.TextArea.TextView.LineSpacing = (value ? 4.0 : 0.2);
		}
	}

	public VerticalAlignment TextVerticalAlignment
	{
		get
		{
			return EditBox.VerticalAlignment;
		}
		set
		{
			SetValue(TextVerticalAlignmentProperty, value);
			EditBox.VerticalAlignment = value;
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

	public bool ReadOnly
	{
		get
		{
			return (bool)GetValue(ReadOnlyProperty);
		}
		set
		{
			EditBox.IsReadOnly = value;
			EditBox.TextArea.Caret.CaretBrush = (value ? Brushes.Transparent : ThemeUtil.GetColor("BaseColorOpacity100"));
			if (value)
			{
				EditBox.TextArea.ClearSelection();
			}
			if (ReadOnly != value)
			{
				SetValue(ReadOnlyProperty, value);
			}
		}
	}

	public int MaxLength { get; set; } = -1;

	public event EventHandler TextChanged;

	public event EventHandler<KeyEventArgs> KeysUp;

	public event EventHandler<KeyEventArgs> KeysDown;

	public event EventHandler EnterUp;

	public EmojiEditor()
		: this(parseTaskLink: false)
	{
	}

	public EmojiEditor(bool parseTaskLink)
	{
		InitializeComponent();
		EditBox.TextArea.TextView.ElementGenerators.Add(new EmojiGenerator(this));
		_linkGenerator = new CustomLinkElementGenerator(this, ignoreUrlDict: true, parseTaskLink);
		EditBox.TextArea.TextView.ElementGenerators.Add(_linkGenerator);
		WordWrap = true;
		EditBox.TextChanged += OnTextChanged;
		EditBox.GotFocus += OnFocused;
		EditBox.LostFocus += OnLostFocused;
		EditBox.PreviewKeyDown += OnKeyDown;
		EditBox.PreviewKeyUp += OnKeyUp;
		base.Loaded += OnLoaded;
	}

	private void OnKeyUp(object sender, KeyEventArgs e)
	{
		if (_enterDown && e.Key == Key.Return)
		{
			this.EnterUp?.Invoke(this, null);
		}
		_enterDown = false;
		this.KeysUp?.Invoke(this, e);
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		if (base.Tag is string text)
		{
			HintText.Text = text;
		}
		EditBox.TextArea.TextView.LinkTextForegroundBrush = ThemeUtil.GetColor("TextAccentColor", this);
		DataObject.RemovePastingHandler(EditBox, OnPaste);
		DataObject.AddPastingHandler(EditBox, OnPaste);
		EditBox.Options.EnableHyperlinks = false;
		EditBox.Options.EnableEmailHyperlinks = false;
		EditBox.Options.InheritWordWrapIndentation = false;
	}

	private void OnPaste(object sender, DataObjectPastingEventArgs e)
	{
		int num = 1;
		bool flag = true;
		while (flag)
		{
			try
			{
				if (e.SourceDataObject.GetDataPresent(DataFormats.UnicodeText, autoConvert: true))
				{
					string text = e.SourceDataObject.GetData(DataFormats.UnicodeText) as string;
					if (text != null)
					{
						if (MaxLength >= 0 && EditBox.Text.Length + text.Length - EditBox.SelectionLength > MaxLength)
						{
							e.CancelCommand();
							if (!WordWrap)
							{
								text = text.Replace("\n", " ").Replace("\r", " ");
							}
							text = TruncateWithoutSplittingSurrogatePair(text, MaxLength - EditBox.Text.Length + EditBox.SelectionLength);
							InsertText(text);
							break;
						}
						if (!WordWrap)
						{
							e.CancelCommand();
							string text2 = text.Replace("\n", " ").Replace("\r", " ");
							InsertText(text2);
						}
					}
				}
				flag = false;
			}
			catch (Exception)
			{
				flag = num++ < 10;
			}
		}
	}

	private void OnKeyDown(object sender, KeyEventArgs e)
	{
		if (ReadOnly)
		{
			return;
		}
		if (e.Key == Key.Return)
		{
			if (!AcceptReturn || (MaxLength > 0 && Text.Length + Environment.NewLine.Length > MaxLength))
			{
				e.Handled = true;
			}
			_enterDown = true;
		}
		else
		{
			_enterDown = false;
		}
		this.KeysDown?.Invoke(this, e);
	}

	private void OnLostFocused(object sender, RoutedEventArgs e)
	{
		Focused = false;
	}

	private void OnFocused(object sender, RoutedEventArgs e)
	{
		if (!ReadOnly)
		{
			Focused = true;
		}
	}

	private void OnTextChanged(object sender, EventArgs e)
	{
		this.TextChanged?.Invoke(this, null);
		LogEmojiPosition();
		RefreshHintVisibility();
	}

	private static void OnTextChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		if (sender is EmojiEditor emojiEditor && e.NewValue != null)
		{
			emojiEditor.Text = (string)e.NewValue;
		}
	}

	private void RefreshHintVisibility()
	{
		HintText.Visibility = ((!string.IsNullOrEmpty(Text)) ? Visibility.Collapsed : Visibility.Visible);
	}

	private static void OnIsReadOnlyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		if (sender is EmojiEditor emojiEditor && e.NewValue != null)
		{
			emojiEditor.ReadOnly = (bool)e.NewValue;
		}
	}

	private static void OnWordWrapChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		if (sender is EmojiEditor emojiEditor && e.NewValue is bool wordWrap)
		{
			emojiEditor.WordWrap = wordWrap;
		}
	}

	private static void OnTextVerticalAlignmentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is EmojiEditor emojiEditor && e.NewValue is VerticalAlignment verticalAlignment)
		{
			emojiEditor.EditBox.VerticalAlignment = verticalAlignment;
		}
	}

	public void FocusEnd()
	{
		EditBox.Focus();
		Keyboard.Focus(EditBox);
		EditBox.CaretOffset = Text.Length;
		EditBox.ScrollToEnd();
	}

	public new void Focus()
	{
		EditBox.Focus();
		Keyboard.Focus(EditBox);
	}

	public Dictionary<int, string> GetEmojiDict()
	{
		return EmojiDict;
	}

	private void LogEmojiPosition()
	{
		EmojiDict.Clear();
		if (string.IsNullOrEmpty(Text))
		{
			return;
		}
		foreach (Match item in EmojiData.MatchOne2.Matches(Text))
		{
			EmojiDict[item.Index] = item.Value;
		}
	}

	private void OnTextInput(object sender, TextCompositionEventArgs e)
	{
		if (MaxLength < 0)
		{
			return;
		}
		string text = e.Text;
		if (MaxLength >= 0)
		{
			if (EditBox.Text.Length - EditBox.SelectionLength >= MaxLength)
			{
				e.Handled = true;
			}
			else if (EditBox.Text.Length + text.Length - EditBox.SelectionLength > MaxLength)
			{
				e.Handled = true;
				text = TruncateWithoutSplittingSurrogatePair(text, MaxLength - EditBox.Text.Length + EditBox.SelectionLength);
				InsertText(text);
			}
		}
	}

	internal static string TruncateWithoutSplittingSurrogatePair(string text, int maxLength)
	{
		if (string.IsNullOrEmpty(text) || maxLength <= 0)
		{
			return string.Empty;
		}
		if (text.Length <= maxLength)
		{
			return text;
		}
		if (char.IsHighSurrogate(text[maxLength - 1]) && char.IsLowSurrogate(text[maxLength]))
		{
			maxLength--;
		}
		return text.Substring(0, maxLength);
	}

	private void InsertText(string text)
	{
		int num = EditBox.CaretOffset;
		if (EditBox.SelectionLength > 0)
		{
			EditBox.Document.Remove(EditBox.SelectionStart, EditBox.SelectionLength);
			num = EditBox.SelectionStart;
		}
		EditBox.Document.Insert(num, text);
		EditBox.CaretOffset = num + text.Length;
	}

	public Dictionary<int, LinkInfo> GetLinkNameDict()
	{
		return null;
	}

	public Dictionary<int, LinkInfo> GetLinkUrlDict()
	{
		return null;
	}

	public async void NavigateTask(string projectId, string taskId)
	{
		await TaskDetailWindow.NavigateProjectTask(new ProjectTask
		{
			ProjectId = projectId,
			TaskId = taskId
		}, Utils.FindParent<IToastShowWindow>(this));
	}

	public void SetParseTaskName(bool b)
	{
		_linkGenerator.SetParseTaskTitle(b);
	}

	public void ShowInsertLink(LinkLineInfoModel model)
	{
	}

	public void UnRegisterCaretChanged()
	{
	}

	public void RegisterCaretChanged()
	{
	}

	public TextEditor GetEditBox()
	{
		return EditBox;
	}

	public Brush GetHighLightColor()
	{
		return Foreground;
	}

	public Brush GetBracketColor()
	{
		return Foreground;
	}

	private void OnBorderMouseUp(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		if (!EditBox.TextArea.IsKeyboardFocused)
		{
			EditBox.Focus();
			EditBox.CaretOffset = EditBox.Text.Length;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/markdown/emojieditor.xaml", UriKind.Relative);
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
			Root = (EmojiEditor)target;
			break;
		case 2:
			TextBorder = (Border)target;
			TextBorder.MouseLeftButtonDown += OnBorderMouseUp;
			break;
		case 3:
			HintText = (TextBlock)target;
			break;
		case 4:
			EditBox = (TextEditor)target;
			EditBox.PreviewTextInput += OnTextInput;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
