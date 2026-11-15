using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using QRCoder;
using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Upgrade;

public class WechatPayQrCodeWindow : Window, IComponentConnector
{
	private readonly PaymentModelV1 _model;

	private Timer _payCheckTimer;

	private DateTime _startCheckTime;

	private string _productId;

	private string _from;

	internal System.Windows.Controls.Image QrGrid;

	internal TextBlock PriceText;

	private bool _contentLoaded;

	public WechatPayQrCodeWindow(PaymentModelV1 paymentModel, string price, string from, string productId)
	{
		InitializeComponent();
		PriceText.Text = price;
		_model = paymentModel;
		_from = from;
		_productId = productId;
		GenerateQrCode(paymentModel.redirectUrl);
		StartWatchPayTask();
	}

	private void StartWatchPayTask()
	{
		_payCheckTimer = new Timer(5000.0);
		_payCheckTimer.Elapsed -= CheckPayStatus;
		_payCheckTimer.Elapsed += CheckPayStatus;
		_startCheckTime = DateTime.Now;
		_payCheckTimer.Start();
	}

	private void StopCheckPayTask()
	{
		_payCheckTimer.Stop();
		_payCheckTimer.Elapsed -= CheckPayStatus;
		base.Dispatcher.Invoke(delegate
		{
			Close();
		});
	}

	private async void CheckPayStatus(object sender, ElapsedEventArgs e)
	{
		if (_model == null || string.IsNullOrEmpty(_model.orderId))
		{
			return;
		}
		PaymentStatus result = await Communicator.CheckPaymentOrder(_model.orderId);
		if (result == null)
		{
			return;
		}
		if (result.status == "COMPLETED")
		{
			base.Dispatcher.Invoke((Func<Task>)async delegate
			{
				UserActCollectUtils.SendUpgradePurchaseSuccessData(_from, _productId);
				StopCheckPayTask();
				await App.Window.HandlePaymentUpdated();
				DateTime value = Utils.TimeStampToDateTime(result.endDate);
				ShowPaySuccessWindow(value);
			});
		}
		else if (result.status == "PENDING")
		{
			base.Dispatcher.Invoke(delegate
			{
				if ((DateTime.Now - _startCheckTime).TotalSeconds >= 600.0)
				{
					StopCheckPayTask();
				}
			});
		}
		else
		{
			base.Dispatcher.Invoke(delegate
			{
				StopCheckPayTask();
				ShowPayFailureWindow();
			});
		}
	}

	private void ShowPayFailureWindow()
	{
		PayFailureWindow payFailureWindow = new PayFailureWindow();
		payFailureWindow.Owner = App.Window;
		payFailureWindow.ShowDialog();
	}

	private void ShowPaySuccessWindow(DateTime? endDate)
	{
		PaySuccessWindow paySuccessWindow = new PaySuccessWindow(endDate ?? DateTime.Today);
		paySuccessWindow.Owner = App.Window;
		paySuccessWindow.ShowDialog();
	}

	private void GenerateQrCode(string qrCodeUrl)
	{
		Bitmap graphic = new QRCode(new QRCodeGenerator().CreateQrCode(qrCodeUrl, QRCodeGenerator.ECCLevel.Q)).GetGraphic(30);
		BitmapImage bitmapImage = new BitmapImage();
		using (MemoryStream memoryStream = new MemoryStream())
		{
			graphic.Save(memoryStream, ImageFormat.Bmp);
			memoryStream.Position = 0L;
			bitmapImage.BeginInit();
			bitmapImage.StreamSource = memoryStream;
			bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
			bitmapImage.EndInit();
		}
		QrGrid.Source = bitmapImage;
	}

	private void CloseButton_Click(object sender, RoutedEventArgs e)
	{
		StopCheckPayTask();
		Close();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/upgrade/wechatpayqrcodewindow.xaml", UriKind.Relative);
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
			QrGrid = (System.Windows.Controls.Image)target;
			break;
		case 2:
			PriceText = (TextBlock)target;
			break;
		case 3:
			((Button)target).Click += CloseButton_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
