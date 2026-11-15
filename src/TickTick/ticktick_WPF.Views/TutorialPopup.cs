using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Shapes;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views;

public class TutorialPopup : UserControl, IComponentConnector
{
	public static readonly DependencyProperty StepDependencyProperty = DependencyProperty.Register("Step", typeof(int), typeof(TutorialPopup), new PropertyMetadata(1, OnStepChangedCallback));

	internal Border FirstBorder;

	internal Border SecondBorder;

	internal Border ThirdBorder;

	internal Grid TPopup;

	internal TextBlock TitleText;

	internal TextBlock ContentText;

	internal Ellipse FirstIndicator;

	internal Ellipse SecondIndicator;

	internal Ellipse ThirdIndicator;

	internal Ellipse ForthIndicator;

	internal Button ActionButton;

	internal TextBlock SkipText;

	private bool _contentLoaded;

	public int Step
	{
		get
		{
			return (int)GetValue(StepDependencyProperty);
		}
		set
		{
			SetValue(StepDependencyProperty, value);
		}
	}

	private static void OnStepChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		if (sender is TutorialPopup tutorialPopup && e.NewValue != null)
		{
			tutorialPopup.InitContent();
		}
	}

	public TutorialPopup()
	{
		InitializeComponent();
		InitContent();
	}

	private void InitContent()
	{
		switch (Step)
		{
		case 1:
			TitleText.Text = Utils.GetString("Inbox");
			ContentText.Text = Utils.GetString("TutorialFirst");
			ResetSetIndicator();
			FirstIndicator.Opacity = 1.0;
			break;
		case 2:
			TitleText.Text = Utils.GetString("SmartList");
			ContentText.Text = Utils.GetString("TutorialSecond");
			ResetSetIndicator();
			SecondIndicator.Opacity = 1.0;
			break;
		case 3:
			TitleText.Text = Utils.GetString("lists");
			ContentText.Text = Utils.GetString("TutorialThird");
			ResetSetIndicator();
			ThirdIndicator.Opacity = 1.0;
			break;
		case 4:
			TitleText.Text = Utils.GetString("CreateTask");
			ContentText.Text = Utils.GetString("TutorialForth");
			ResetSetIndicator();
			ForthIndicator.Opacity = 1.0;
			ActionButton.Content = Utils.GetString("StartUse");
			SkipText.Visibility = Visibility.Collapsed;
			break;
		}
	}

	private void ResetSetIndicator()
	{
		FirstIndicator.Opacity = 0.36000001430511475;
		SecondIndicator.Opacity = 0.36000001430511475;
		ThirdIndicator.Opacity = 0.36000001430511475;
		ForthIndicator.Opacity = 0.36000001430511475;
	}

	private void OnNextClick(object sender, RoutedEventArgs e)
	{
		switch (Step)
		{
		case 1:
			MoveToSecondStep();
			break;
		case 2:
			MoveToThirdStep();
			break;
		case 3:
			MoveToForthStep();
			break;
		case 4:
			Finish();
			break;
		}
	}

	private void Finish()
	{
		Utils.FindParent<MainWindow>(this)?.FinishTutorial();
	}

	public void MoveToSecondStep()
	{
		FirstBorder.Visibility = Visibility.Collapsed;
		SecondBorder.Visibility = Visibility.Visible;
		Step = 2;
		TPopup.Margin = new Thickness(0.0, 92.0, 0.0, 0.0);
	}

	public void MoveToThirdStep()
	{
		SecondBorder.Visibility = Visibility.Collapsed;
		ThirdBorder.Visibility = Visibility.Visible;
		Step = 3;
		TPopup.Margin = new Thickness(0.0, 200.0, 0.0, 0.0);
	}

	public void MoveToForthStep()
	{
		ThirdBorder.Visibility = Visibility.Collapsed;
		TPopup.Margin = new Thickness(257.0, 118.0, 0.0, 0.0);
		Step = 4;
	}

	private void OnSkipClick(object sender, MouseButtonEventArgs e)
	{
		Finish();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/tutorialpopup.xaml", UriKind.Relative);
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
			FirstBorder = (Border)target;
			break;
		case 2:
			SecondBorder = (Border)target;
			break;
		case 3:
			ThirdBorder = (Border)target;
			break;
		case 4:
			TPopup = (Grid)target;
			break;
		case 5:
			TitleText = (TextBlock)target;
			break;
		case 6:
			ContentText = (TextBlock)target;
			break;
		case 7:
			FirstIndicator = (Ellipse)target;
			break;
		case 8:
			SecondIndicator = (Ellipse)target;
			break;
		case 9:
			ThirdIndicator = (Ellipse)target;
			break;
		case 10:
			ForthIndicator = (Ellipse)target;
			break;
		case 11:
			ActionButton = (Button)target;
			ActionButton.Click += OnNextClick;
			break;
		case 12:
			SkipText = (TextBlock)target;
			SkipText.MouseLeftButtonUp += OnSkipClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
