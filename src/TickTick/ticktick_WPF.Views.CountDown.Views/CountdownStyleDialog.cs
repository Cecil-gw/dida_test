using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CountDown.ViewModels;

namespace ticktick_WPF.Views.CountDown.Views;

public class CountdownStyleDialog : Window, IOkCancelWindow, IComponentConnector
{
	internal Button CancelButton;

	private bool _contentLoaded;

	private CountDownStyleSetViewModel viewModel { get; set; } = new CountDownStyleSetViewModel();

	public event EventHandler<StyleSelectionResult> StyleSelected;

	public CountdownStyleDialog()
	{
		InitializeComponent();
		base.DataContext = viewModel;
		CountDownStyleSetViewModel countDownStyleSetViewModel = viewModel;
		countDownStyleSetViewModel.StyleSelected = (Action<StyleSelectionResult>)Delegate.Combine(countDownStyleSetViewModel.StyleSelected, new Action<StyleSelectionResult>(OnStyleSelected));
	}

	public CountdownStyleDialog(CountdownModel model, bool fromAdd = false)
	{
		InitializeComponent();
		base.DataContext = viewModel;
		viewModel.SetParentWindow(this);
		CountDownStyleSetViewModel countDownStyleSetViewModel = viewModel;
		countDownStyleSetViewModel.StyleSelected = (Action<StyleSelectionResult>)Delegate.Combine(countDownStyleSetViewModel.StyleSelected, new Action<StyleSelectionResult>(OnStyleSelected));
		viewModel.Init(model, fromAdd);
		CancelButton.Content = Utils.GetString(fromAdd ? "LoginReturn" : "Cancel");
	}

	private void OnStyleSelected(StyleSelectionResult result)
	{
		this.StyleSelected?.Invoke(this, result);
	}

	private void OnMainGridClick(object sender, MouseButtonEventArgs e)
	{
		viewModel.CloseSelectImageWindow();
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		base.Owner?.Activate();
		base.OnClosing(e);
	}

	private void CancelButton_Click(object sender, RoutedEventArgs e)
	{
		viewModel.Result = null;
		Close();
	}

	private async void ConfirmButton_Click(object sender, RoutedEventArgs e)
	{
		await viewModel.Apply();
		Close();
	}

	public void OnCancel()
	{
		CancelButton_Click(null, null);
	}

	public void Ok()
	{
		ConfirmButton_Click(null, null);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/countdown/views/countdownstyledialog.xaml", UriKind.Relative);
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
			((StackPanel)target).MouseLeftButtonUp += OnMainGridClick;
			break;
		case 2:
			((Button)target).Click += ConfirmButton_Click;
			break;
		case 3:
			CancelButton = (Button)target;
			CancelButton.Click += CancelButton_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
