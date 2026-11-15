using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Navigation;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.AI;

public class AIPrivacyAuthorizationView : UserControl, IComponentConnector
{
	private const string AgreementPlaceholder = "{0}";

	internal Grid DialogContent;

	internal ItemsControl PrivacyDescriptionList;

	internal CheckBox PrivacyAgreementCheckBox;

	internal TextBlock PrivacyAgreementText;

	internal Button AgreeAndContinueButton;

	private bool _contentLoaded;

	public event EventHandler Accepted;

	public AIPrivacyAuthorizationView()
	{
		InitializeComponent();
		PrivacyDescriptionList.ItemsSource = new string[3]
		{
			Utils.GetString("AIAgentPrivacyReadData"),
			Utils.GetString("AIAgentPrivacyModifyData"),
			Utils.GetString("AIAgentPrivacyUseInput")
		};
		BuildPrivacyAgreementText();
	}

	public void Reset()
	{
		PrivacyAgreementCheckBox.IsChecked = false;
		AgreeAndContinueButton.IsEnabled = false;
		FocusAgreementCheckBox();
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		FocusAgreementCheckBox();
	}

	private void FocusAgreementCheckBox()
	{
		base.Dispatcher.BeginInvoke((Action)delegate
		{
			if (base.IsVisible)
			{
				PrivacyAgreementCheckBox.Focus();
				Keyboard.Focus(PrivacyAgreementCheckBox);
			}
		});
	}

	private void BuildPrivacyAgreementText()
	{
		string text = Utils.GetString("AIAgentPrivacyAgree") ?? string.Empty;
		int num = text.IndexOf("{0}", StringComparison.Ordinal);
		PrivacyAgreementText.Inlines.Clear();
		if (num < 0)
		{
			PrivacyAgreementText.Inlines.Add(new Run(text));
			return;
		}
		if (num > 0)
		{
			PrivacyAgreementText.Inlines.Add(new Run(text.Substring(0, num)));
		}
		Hyperlink hyperlink = new Hyperlink(new Run(Utils.GetString("AIAgentPrivacy")))
		{
			Cursor = Cursors.Hand,
			NavigateUri = new Uri(Utils.IsDida() ? "https://dida365.com/privacy" : "https://ticktick.com/privacy"),
			TextDecorations = null
		};
		hyperlink.SetResourceReference(TextElement.ForegroundProperty, "PrimaryColor");
		hyperlink.RequestNavigate += OnPrivacyLinkRequestNavigate;
		PrivacyAgreementText.Inlines.Add(hyperlink);
		int num2 = num + "{0}".Length;
		if (num2 < text.Length)
		{
			PrivacyAgreementText.Inlines.Add(new Run(text.Substring(num2)));
		}
	}

	private void OnPrivacyAgreementChanged(object sender, RoutedEventArgs e)
	{
		AgreeAndContinueButton.IsEnabled = PrivacyAgreementCheckBox.IsChecked == true;
	}

	private void OnAgreeAndContinueClick(object sender, RoutedEventArgs e)
	{
		if (PrivacyAgreementCheckBox.IsChecked == true)
		{
			this.Accepted?.Invoke(this, EventArgs.Empty);
		}
	}

	private void OnPrivacyLinkRequestNavigate(object sender, RequestNavigateEventArgs e)
	{
		Utils.TryProcessStartUrl(e.Uri.AbsoluteUri);
		e.Handled = true;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/ai/aiprivacyauthorizationview.xaml", UriKind.Relative);
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
			((AIPrivacyAuthorizationView)target).Loaded += OnLoaded;
			break;
		case 2:
			DialogContent = (Grid)target;
			break;
		case 3:
			PrivacyDescriptionList = (ItemsControl)target;
			break;
		case 4:
			PrivacyAgreementCheckBox = (CheckBox)target;
			PrivacyAgreementCheckBox.Checked += OnPrivacyAgreementChanged;
			PrivacyAgreementCheckBox.Unchecked += OnPrivacyAgreementChanged;
			break;
		case 5:
			PrivacyAgreementText = (TextBlock)target;
			break;
		case 6:
			AgreeAndContinueButton = (Button)target;
			AgreeAndContinueButton.Click += OnAgreeAndContinueClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
