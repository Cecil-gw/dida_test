using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using Emoji.Wpf;

namespace ticktick_WPF.Views;

public class CustomerDialog : MyWindow, IOkCancelWindow, IComponentConnector
{
	internal EmjTextBlock TitleTextBlock;

	internal TextBlock ContentTextBlock;

	internal TextBlock ContentComplexTextBlock;

	internal Run CenPreText;

	internal Run CenCenterText;

	internal Run CenPostText;

	internal Grid EmphasizeTextBlock;

	internal TextBlock EmphasizeText;

	internal CheckBox PositiveCheckbox;

	internal ContentControl ExtraContent;

	internal Button OkButton;

	internal Button CancelButton;

	internal Border CloseButton;

	private bool _contentLoaded;

	public bool? YesNoCancelResult { get; set; }

	public event EventHandler TextClick;

	public event EventHandler CloseClick;

	public event EventHandler CancelClick;

	public CustomerDialog()
	{
		InitializeComponent();
	}

	public CustomerDialog(string title, string content, MessageBoxButton messageBoxButton, Window owner = null)
	{
		InitializeComponent();
		TitleTextBlock.Text = title;
		base.Title = (string.IsNullOrEmpty(title) ? "  " : title);
		ContentTextBlock.Text = content;
		if (messageBoxButton == MessageBoxButton.OKCancel || (uint)(messageBoxButton - 3) <= 1u)
		{
			OkButton.Visibility = Visibility.Visible;
			CancelButton.Visibility = Visibility.Visible;
		}
		else
		{
			OkButton.Visibility = Visibility.Visible;
			CancelButton.Visibility = Visibility.Collapsed;
		}
		if (owner != null)
		{
			base.WindowStartupLocation = WindowStartupLocation.CenterOwner;
			if (owner.Activate())
			{
				base.Owner = owner;
			}
		}
		else
		{
			base.WindowStartupLocation = WindowStartupLocation.CenterScreen;
		}
	}

	public CustomerDialog(string title, string pretext, string centertext, string posttext, string okbuttonText)
	{
		InitializeComponent();
		TitleTextBlock.Text = title;
		base.Title = (string.IsNullOrEmpty(title) ? "  " : title);
		ContentTextBlock.Visibility = Visibility.Collapsed;
		ContentComplexTextBlock.Visibility = Visibility.Visible;
		CenPreText.Text = pretext;
		CenCenterText.Text = centertext;
		CenPostText.Text = posttext;
		OkButton.Content = okbuttonText;
		CancelButton.Visibility = Visibility.Collapsed;
	}

	public CustomerDialog(string title, string content, string okButtonText, string cancelButtonText, Window owner = null, bool showClose = true)
	{
		InitializeComponent();
		TitleTextBlock.Text = title;
		base.Title = (string.IsNullOrEmpty(title) ? "  " : title);
		TitleTextBlock.Visibility = (string.IsNullOrEmpty(title) ? Visibility.Collapsed : Visibility.Visible);
		ContentTextBlock.Text = content;
		OkButton.Content = okButtonText;
		CancelButton.Content = cancelButtonText;
		if (string.IsNullOrEmpty(cancelButtonText))
		{
			CancelButton.Visibility = Visibility.Collapsed;
		}
		if (!showClose)
		{
			CloseButton.Visibility = Visibility.Collapsed;
		}
		if (owner != null)
		{
			base.WindowStartupLocation = WindowStartupLocation.CenterOwner;
			if (owner.Activate())
			{
				base.Owner = owner;
			}
		}
		else
		{
			base.WindowStartupLocation = WindowStartupLocation.CenterScreen;
		}
	}

	public void SetEmphasizeText(string text)
	{
		EmphasizeTextBlock.Visibility = Visibility.Visible;
		EmphasizeText.Text = text;
	}

	public CustomerDialog(string title, List<Inline> content, string okButtonText, string cancelButtonText, Window owner = null)
	{
		InitializeComponent();
		TitleTextBlock.Text = title;
		TitleTextBlock.Visibility = (string.IsNullOrEmpty(title) ? Visibility.Collapsed : Visibility.Visible);
		if (!TitleTextBlock.IsVisible)
		{
			ContentTextBlock.Margin = new Thickness(0.0, 20.0, 0.0, 10.0);
		}
		base.Title = (string.IsNullOrEmpty(title) ? "  " : title);
		ContentTextBlock.Inlines.Clear();
		ContentTextBlock.Inlines.AddRange(content);
		OkButton.Content = okButtonText;
		CancelButton.Content = cancelButtonText;
		if (string.IsNullOrEmpty(cancelButtonText))
		{
			CancelButton.Visibility = Visibility.Collapsed;
		}
		if (owner != null)
		{
			base.WindowStartupLocation = WindowStartupLocation.CenterOwner;
			base.Owner = owner;
		}
		else
		{
			base.WindowStartupLocation = WindowStartupLocation.CenterScreen;
		}
	}

	private async void OnCancelClick(object sender, RoutedEventArgs e)
	{
		if (this.CancelClick != null)
		{
			this.CancelClick(this, null);
			await Task.Delay(10);
			Close();
		}
		else
		{
			Cancel();
		}
	}

	protected virtual void Cancel()
	{
		YesNoCancelResult = false;
		base.DialogResult = false;
		Close();
	}

	protected virtual void OnOkClick(object sender, RoutedEventArgs e)
	{
		OnOk();
	}

	protected virtual void OnOk()
	{
		YesNoCancelResult = true;
		base.DialogResult = true;
		Close();
	}

	protected virtual async void OnCloseClick(object sender, MouseButtonEventArgs e)
	{
		if (this.CloseClick != null)
		{
			this.CloseClick(this, null);
			await Task.Delay(10);
		}
		Close();
	}

	private void OnCenterTextClick(object sender, RoutedEventArgs routedEventArgs)
	{
		this.TextClick?.Invoke(this, null);
	}

	public async void OnCancel()
	{
		if (this.CloseClick != null)
		{
			this.CloseClick(this, null);
			await Task.Delay(10);
		}
		Close();
	}

	public void Ok()
	{
		OnOk();
	}

	protected override void OnClosed(EventArgs eventArgs)
	{
		try
		{
			if (base.Owner != null)
			{
				base.Owner.Activate();
			}
		}
		catch (Exception)
		{
		}
	}

	private void OnWindowLoaded(object sender, RoutedEventArgs e)
	{
		base.InputBindings.Add(new KeyBinding(OkCancelWindowCommands.EscCommand, new KeyGesture(Key.Escape))
		{
			CommandParameter = this
		});
		base.InputBindings.Add(new KeyBinding(OkCancelWindowCommands.OkCommand, new KeyGesture(Key.Return))
		{
			CommandParameter = this
		});
	}

	public void SetExtraContent(UIElement element)
	{
		ExtraContent.Content = element;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/customerdialog.xaml", UriKind.Relative);
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
			TitleTextBlock = (EmjTextBlock)target;
			break;
		case 2:
			ContentTextBlock = (TextBlock)target;
			break;
		case 3:
			ContentComplexTextBlock = (TextBlock)target;
			break;
		case 4:
			CenPreText = (Run)target;
			break;
		case 5:
			CenCenterText = (Run)target;
			CenCenterText.PreviewMouseLeftButtonUp += OnCenterTextClick;
			break;
		case 6:
			CenPostText = (Run)target;
			break;
		case 7:
			EmphasizeTextBlock = (Grid)target;
			break;
		case 8:
			EmphasizeText = (TextBlock)target;
			break;
		case 9:
			PositiveCheckbox = (CheckBox)target;
			break;
		case 10:
			ExtraContent = (ContentControl)target;
			break;
		case 11:
			OkButton = (Button)target;
			OkButton.Click += OnOkClick;
			break;
		case 12:
			CancelButton = (Button)target;
			CancelButton.Click += OnCancelClick;
			break;
		case 13:
			CloseButton = (Border)target;
			CloseButton.MouseLeftButtonUp += OnCloseClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
