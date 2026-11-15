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

namespace ticktick_WPF.Views;

public class ManageCancelProSubsWindow : Window, IComponentConnector
{
	internal Border CloseButton;

	private bool _contentLoaded;

	public bool IsCompleteCanceled { get; set; }

	private ManageCancelProSubsViewModel viewModel { get; set; }

	public ManageCancelProSubsWindow(UserModel user)
	{
		InitializeComponent();
		base.DataContext = (viewModel = new ManageCancelProSubsViewModel());
		InitData(user);
	}

	private void InitData(UserModel user)
	{
		viewModel.Step = ManageCancelProSubsViewModel.StepPage.Manage;
		viewModel.Title = Utils.GetString("ManageSubscriptions");
		string subscribeFreq = user.subscribeFreq;
		if (!(subscribeFreq == "Month"))
		{
			if (subscribeFreq == "Year")
			{
				viewModel.FreqPlan = Utils.GetString("PremiumYearly");
			}
		}
		else
		{
			viewModel.FreqPlan = Utils.GetString("PremiumMonthly");
		}
		viewModel.RawSubsType = user.subscribeType;
		viewModel.SubsTypeText = PremiumUtils.GetSubscribeTypeText(user.subscribeType);
		viewModel.CancelPremiumDesc = string.Format(Utils.GetString("CancelPremiumDesc"), viewModel.SubsTypeText);
	}

	private void ToCancelButton_Click(object sender, RoutedEventArgs e)
	{
		if (viewModel.RawSubsType == "apple")
		{
			viewModel.Step = ManageCancelProSubsViewModel.StepPage.Apple;
			viewModel.Title = Utils.GetString("CancelAppleSubsTitle");
		}
		else
		{
			viewModel.Step = ManageCancelProSubsViewModel.StepPage.CancelSubs;
			viewModel.Title = Utils.GetString("CancelSubscription");
		}
	}

	private async void ConfirmCancelButton_Click(object sender, RoutedEventArgs e)
	{
		if (await Communicator.CancelPaymentSubs(viewModel.RawSubsType))
		{
			viewModel.Step = ManageCancelProSubsViewModel.StepPage.Canceled;
			viewModel.Title = Utils.GetString("SubscriptionCanceledTitle");
			IsCompleteCanceled = true;
		}
	}

	private void OnCloseClick(object sender, MouseButtonEventArgs e)
	{
		Close();
	}

	private void CloseButton_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/managecancelprosubswindow.xaml", UriKind.Relative);
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
			((Button)target).Click += ToCancelButton_Click;
			break;
		case 2:
			((Button)target).Click += ConfirmCancelButton_Click;
			break;
		case 3:
			((Button)target).Click += CloseButton_Click;
			break;
		case 4:
			((Button)target).Click += CloseButton_Click;
			break;
		case 5:
			((Button)target).Click += CloseButton_Click;
			break;
		case 6:
			CloseButton = (Border)target;
			CloseButton.MouseLeftButtonUp += OnCloseClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
