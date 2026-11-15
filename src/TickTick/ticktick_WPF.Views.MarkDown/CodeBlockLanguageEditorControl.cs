using System;
using System.CodeDom.Compiler;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using ticktick_WPF.Views.MarkDown.Colorizer;

namespace ticktick_WPF.Views.MarkDown;

public class CodeBlockLanguageEditorControl : UserControl, IComponentConnector, IStyleConnector
{
	public sealed class LanguageItem : INotifyPropertyChanged
	{
		private bool _isSelected;

		public string DisplayName { get; }

		public bool IsSelected
		{
			get
			{
				return _isSelected;
			}
			set
			{
				if (_isSelected != value)
				{
					_isSelected = value;
					this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsSelected"));
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public LanguageItem(string displayName)
		{
			DisplayName = displayName;
		}
	}

	private const double LanguageItemHeight = 36.0;

	private int _selectedLanguageIndex = -1;

	internal CodeBlockLanguageEditorControl Root;

	internal Border EditorHost;

	internal TextBox LanguageTextBox;

	internal Border DropdownButton;

	internal Path DropdownIcon;

	internal Popup LanguageDropdownPopup;

	internal Border DropdownShadowHost;

	internal Border DropdownPopupBorder;

	internal ScrollViewer LanguageScrollViewer;

	internal ItemsControl LanguageItemsControl;

	private bool _contentLoaded;

	public ObservableCollection<LanguageItem> LanguageItems { get; } = new ObservableCollection<LanguageItem>();

	public TextBox Editor => LanguageTextBox;

	public DependencyObject DropdownChild => LanguageDropdownPopup.Child;

	public bool IsDropdownOpen => LanguageDropdownPopup.IsOpen;

	public bool IsEditorKeyboardFocusWithin => LanguageTextBox.IsKeyboardFocusWithin;

	public string Text
	{
		get
		{
			return LanguageTextBox.Text;
		}
		set
		{
			LanguageTextBox.Text = value ?? string.Empty;
		}
	}

	public event EventHandler<string> LanguageSelected;

	public event EventHandler DropdownClosed;

	public CodeBlockLanguageEditorControl()
	{
		InitializeComponent();
	}

	public void ApplyTheme(Brush editorBackground, Brush editorForeground, Brush popupBackground, Brush popupBorderBrush, Brush dropdownIconBrush)
	{
		EditorHost.Background = editorBackground ?? Brushes.Transparent;
		EditorHost.BorderBrush = Brushes.Transparent;
		EditorHost.Tag = editorBackground;
		LanguageTextBox.Background = Brushes.Transparent;
		LanguageTextBox.Foreground = editorForeground ?? Brushes.Black;
		LanguageTextBox.CaretBrush = LanguageTextBox.Foreground;
		LanguageTextBox.BorderBrush = Brushes.Transparent;
		DropdownButton.Background = Brushes.Transparent;
		DropdownIcon.Fill = dropdownIconBrush ?? Brushes.Black;
		DropdownPopupBorder.Background = popupBackground ?? Brushes.White;
		DropdownPopupBorder.BorderBrush = popupBorderBrush ?? Brushes.Transparent;
	}

	public void FocusEditor()
	{
		LanguageTextBox.Focus();
		Keyboard.Focus(LanguageTextBox);
		LanguageTextBox.CaretIndex = LanguageTextBox.Text?.Length ?? 0;
	}

	public void ShowDropdown()
	{
		RebuildLanguageItems();
		if (LanguageItems.Count == 0)
		{
			HideDropdown();
			return;
		}
		DropdownPopupBorder.MinWidth = Math.Max(Math.Max(base.ActualWidth, EditorHost.ActualWidth), 190.0);
		LanguageDropdownPopup.IsOpen = true;
		base.Dispatcher?.BeginInvoke(new Action(EnsureSelectedLanguageVisible), DispatcherPriority.Loaded);
	}

	public void HideDropdown()
	{
		SetDropdownSelectionIndex(-1);
		LanguageDropdownPopup.IsOpen = false;
	}

	public bool IsMouseOverDropdown()
	{
		if (!IsDropdownOpen || DropdownShadowHost.ActualWidth <= 0.0 || DropdownShadowHost.ActualHeight <= 0.0)
		{
			return false;
		}
		Point position = Mouse.GetPosition(DropdownShadowHost);
		if (position.X >= 0.0 && position.Y >= 0.0 && position.X <= DropdownShadowHost.ActualWidth)
		{
			return position.Y <= DropdownShadowHost.ActualHeight;
		}
		return false;
	}

	public void ScrollDropdownByWheelDelta(int delta)
	{
		int num = Math.Sign(delta);
		if (num != 0)
		{
			ScrollLanguageDropdownTo(LanguageScrollViewer.VerticalOffset - (double)num * 36.0);
		}
	}

	public void MoveDropdownSelection(int offset)
	{
		if (LanguageItems.Count != 0)
		{
			int num = _selectedLanguageIndex + offset;
			if (num < 0)
			{
				num = LanguageItems.Count - 1;
			}
			else if (num >= LanguageItems.Count)
			{
				num = 0;
			}
			SetDropdownSelectionIndex(num);
		}
	}

	public bool TryGetSelectedDropdownValue(out string displayName)
	{
		displayName = ((_selectedLanguageIndex >= 0 && _selectedLanguageIndex < LanguageItems.Count) ? LanguageItems[_selectedLanguageIndex].DisplayName : null);
		return !string.IsNullOrWhiteSpace(displayName);
	}

	private void RebuildLanguageItems()
	{
		LanguageItems.Clear();
		_selectedLanguageIndex = -1;
		foreach (string item in CodeBlockLanguageCatalog.HighlightedLanguageDisplayNames.ToList())
		{
			LanguageItems.Add(new LanguageItem(item));
		}
		SetDropdownSelectionIndex(CodeBlockLanguageCatalog.GetHighlightedLanguageDisplayNameMatchIndex(Text));
	}

	private void SetDropdownSelectionIndex(int index)
	{
		if (_selectedLanguageIndex >= 0 && _selectedLanguageIndex < LanguageItems.Count)
		{
			LanguageItems[_selectedLanguageIndex].IsSelected = false;
		}
		_selectedLanguageIndex = index;
		if (_selectedLanguageIndex >= 0 && _selectedLanguageIndex < LanguageItems.Count)
		{
			LanguageItems[_selectedLanguageIndex].IsSelected = true;
			EnsureSelectedLanguageVisible();
		}
	}

	private void EnsureSelectedLanguageVisible()
	{
		int selectedLanguageIndex = _selectedLanguageIndex;
		if (!IsDropdownOpen || selectedLanguageIndex < 0 || selectedLanguageIndex >= LanguageItems.Count)
		{
			return;
		}
		LanguageScrollViewer.UpdateLayout();
		double viewportHeight = LanguageScrollViewer.ViewportHeight;
		if (!(viewportHeight <= 0.0))
		{
			double num = (double)selectedLanguageIndex * 36.0;
			double num2 = num + 36.0;
			double verticalOffset = LanguageScrollViewer.VerticalOffset;
			double num3 = verticalOffset + viewportHeight;
			if (num < verticalOffset)
			{
				ScrollLanguageDropdownTo(num);
			}
			else if (num2 > num3)
			{
				ScrollLanguageDropdownTo(num2 - viewportHeight);
			}
		}
	}

	private void ScrollLanguageDropdownTo(double offset)
	{
		if (!double.IsNaN(offset) && !double.IsInfinity(offset))
		{
			double val = Math.Max(0.0, LanguageScrollViewer.ScrollableHeight);
			LanguageScrollViewer.ScrollToVerticalOffset(Math.Max(0.0, Math.Min(val, offset)));
		}
	}

	private void OnDropdownButtonMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
	}

	private void OnDropdownButtonMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		if (IsDropdownOpen)
		{
			HideDropdown();
		}
		else
		{
			LanguageTextBox.Focus();
			ShowDropdown();
		}
		e.Handled = true;
	}

	private void OnLanguageItemMouseEnter(object sender, MouseEventArgs e)
	{
		if ((sender as FrameworkElement)?.DataContext is LanguageItem item)
		{
			SetDropdownSelectionIndex(LanguageItems.IndexOf(item));
		}
	}

	private void OnLanguageItemMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
	}

	private void OnLanguageItemMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		string text = ((sender as FrameworkElement)?.DataContext as LanguageItem)?.DisplayName;
		if (!string.IsNullOrWhiteSpace(text))
		{
			this.LanguageSelected?.Invoke(this, text);
		}
		e.Handled = true;
	}

	private void OnDropdownClosed(object sender, EventArgs e)
	{
		SetDropdownSelectionIndex(-1);
		this.DropdownClosed?.Invoke(this, EventArgs.Empty);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/markdown/codeblocklanguageeditorcontrol.xaml", UriKind.Relative);
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
			Root = (CodeBlockLanguageEditorControl)target;
			break;
		case 2:
			EditorHost = (Border)target;
			break;
		case 3:
			LanguageTextBox = (TextBox)target;
			break;
		case 4:
			DropdownButton = (Border)target;
			DropdownButton.PreviewMouseLeftButtonDown += OnDropdownButtonMouseLeftButtonDown;
			DropdownButton.PreviewMouseLeftButtonUp += OnDropdownButtonMouseLeftButtonUp;
			break;
		case 5:
			DropdownIcon = (Path)target;
			break;
		case 6:
			LanguageDropdownPopup = (Popup)target;
			LanguageDropdownPopup.Closed += OnDropdownClosed;
			break;
		case 7:
			DropdownShadowHost = (Border)target;
			break;
		case 8:
			DropdownPopupBorder = (Border)target;
			break;
		case 9:
			LanguageScrollViewer = (ScrollViewer)target;
			break;
		case 10:
			LanguageItemsControl = (ItemsControl)target;
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
		if (connectionId == 11)
		{
			((Border)target).MouseEnter += OnLanguageItemMouseEnter;
			((Border)target).PreviewMouseLeftButtonDown += OnLanguageItemMouseLeftButtonDown;
			((Border)target).PreviewMouseLeftButtonUp += OnLanguageItemMouseLeftButtonUp;
		}
	}
}
