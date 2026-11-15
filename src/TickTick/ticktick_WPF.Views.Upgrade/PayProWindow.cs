using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Upgrade;

public class PayProWindow : MyWindow, IOkCancelWindow, IComponentConnector
{
	private string _from;

	private PayViewModel _model;

	public bool PayClick;

	private string _freq = "year";

	public static readonly DependencyProperty ShowCountProperty = DependencyProperty.Register("ShowCount", typeof(bool), typeof(PayProWindow));

	public static readonly DependencyProperty ShowWechatMethodProperty = DependencyProperty.Register("ShowWechatMethod", typeof(bool), typeof(PayProWindow));

	public static readonly DependencyProperty ShowMobileWidgetProperty = DependencyProperty.Register("ShowMobileWidget", typeof(bool), typeof(PayProWindow));

	public static readonly DependencyProperty ShowAlipaySubscribeProperty = DependencyProperty.Register("ShowAlipaySubscribe", typeof(bool), typeof(PayProWindow));

	public static readonly DependencyProperty DidaMethodCountProperty = DependencyProperty.Register("DidaMethodCount", typeof(int), typeof(PayProWindow));

	public static readonly DependencyProperty IsDidaProperty = DependencyProperty.Register("IsDida", typeof(bool), typeof(PayProWindow));

	public static readonly DependencyProperty TotalPriceProperty = DependencyProperty.Register("TotalPrice", typeof(string), typeof(PayProWindow));

	public static readonly DependencyProperty TermContent1Property = DependencyProperty.Register("TermContent1", typeof(string), typeof(PayProWindow));

	public static readonly DependencyProperty TermContent2Property = DependencyProperty.Register("TermContent2", typeof(string), typeof(PayProWindow));

	public static readonly DependencyProperty TermContent3Property = DependencyProperty.Register("TermContent3", typeof(string), typeof(PayProWindow));

	public static readonly DependencyProperty IsSubscribeProperty = DependencyProperty.Register("IsSubscribe", typeof(bool), typeof(PayProWindow));

	public static readonly DependencyProperty UnitProperty = DependencyProperty.Register("Unit", typeof(string), typeof(PayProWindow));

	internal PayProWindow Root;

	internal ColumnDefinition SecondColumn;

	internal RowDefinition FirstRow;

	internal LinearGradientBrush LeftBrush;

	internal Image PayImage;

	internal StackPanel FeaturePanel;

	internal StackPanel TitlePanel;

	internal TextBlock UpdateTitleTextBlock;

	internal TextBlock UpdateDesc;

	internal StackPanel OptionPanel;

	internal Border PayDetailItem;

	internal PayDetailLabel SelectedPayItem;

	internal EscPopup PayOptionPopup;

	internal PayDetailLabel PayLabel1;

	internal PayDetailLabel PayLabel2;

	internal PayDetailLabel PayLabel3;

	internal StackPanel DidaPayPanel;

	internal Grid AmountCount;

	internal CountSpinner CountSpinner;

	internal Grid AliWeChatPay;

	internal PurchaseWechatBtn WechatBtn;

	internal PurchaseAlipayBtn AlipayBtn;

	internal Border TickPayDetailItem;

	internal PayDetailLabel TickSelectedPayItem;

	internal EscPopup TickPayOptionPopup;

	internal PayDetailLabel TickSubByYearLabel;

	internal PayDetailLabel TickSubByMonthLabel;

	internal Grid TickPay;

	internal PurchaseStripeBtn StripeBtn;

	internal PurchasePaypalBtn PaypalBtn;

	internal Button UpdateNowButton;

	internal TextBlock TotalPriceText;

	internal Button TickUpdateNowButton;

	internal CheckBox AgreeCheckBox;

	internal Popup AgreeNotifyPopup;

	internal Button CloseButton;

	private bool _contentLoaded;

	public static bool NavigatePayProcess { get; set; }

	public static string PayType { get; set; }

	public static DateTime PayTime { get; set; }

	public bool IsDida
	{
		get
		{
			return (bool)GetValue(IsDidaProperty);
		}
		set
		{
			SetValue(IsDidaProperty, value);
		}
	}

	public bool ShowWechatMethod
	{
		get
		{
			return (bool)GetValue(ShowWechatMethodProperty);
		}
		set
		{
			SetValue(ShowWechatMethodProperty, value);
		}
	}

	public bool ShowMobileWidget
	{
		get
		{
			return (bool)GetValue(ShowMobileWidgetProperty);
		}
		set
		{
			SetValue(ShowMobileWidgetProperty, value);
		}
	}

	public int DidaMethodCount
	{
		get
		{
			return (int)GetValue(DidaMethodCountProperty);
		}
		set
		{
			SetValue(DidaMethodCountProperty, value);
		}
	}

	public bool ShowAlipaySubscribe
	{
		get
		{
			return (bool)GetValue(ShowAlipaySubscribeProperty);
		}
		set
		{
			SetValue(ShowAlipaySubscribeProperty, value);
		}
	}

	public bool ShowCount
	{
		get
		{
			return (bool)GetValue(ShowCountProperty);
		}
		set
		{
			SetValue(ShowCountProperty, value);
		}
	}

	public string TotalPrice
	{
		get
		{
			return (string)GetValue(TotalPriceProperty);
		}
		set
		{
			SetValue(TotalPriceProperty, value);
		}
	}

	public string TermContent1
	{
		get
		{
			return (string)GetValue(TermContent1Property);
		}
		set
		{
			SetValue(TermContent1Property, value);
		}
	}

	public string TermContent2
	{
		get
		{
			return (string)GetValue(TermContent2Property);
		}
		set
		{
			SetValue(TermContent2Property, value);
		}
	}

	public string TermContent3
	{
		get
		{
			return (string)GetValue(TermContent3Property);
		}
		set
		{
			SetValue(TermContent3Property, value);
		}
	}

	public bool IsSubscribe
	{
		get
		{
			return (bool)GetValue(IsSubscribeProperty);
		}
		set
		{
			SetValue(IsSubscribeProperty, value);
		}
	}

	private int Count { get; set; } = 1;

	private string Freq
	{
		get
		{
			return _freq;
		}
		set
		{
			_freq = value;
			TotalPrice = GetTotalPrice();
			GetTermContent();
			GetUnit();
			IsSubscribe = _freq == "subscribe";
		}
	}

	public string Unit
	{
		get
		{
			return (string)GetValue(UnitProperty);
		}
		set
		{
			SetValue(UnitProperty, value);
		}
	}

	public PayProWindow(string from, PayViewModel model)
	{
		InitializeComponent();
		_from = from;
		_model = model;
		IsDida = Utils.IsDida() || Utils.IsZhCn();
		ShowWechatMethod = Utils.IsDida();
		ShowAlipaySubscribe = Utils.IsDida();
		DidaMethodCount = (Utils.IsDida() ? 3 : 2);
		ShowCount = IsDida;
		IsSubscribe = !IsDida;
		if (!ABTestManager.UsePremiumV2PayStyle())
		{
			ShowMobileWidget = true;
			SetPlanO();
		}
		if (IsDida)
		{
			string text = "0.0";
			if (TryParsePrice(model.OrderByYear, out var price))
			{
				text = (price / 12m).ToString("0.0", CultureInfo.CurrentCulture);
			}
			string adText = " ￥" + text + Utils.GetString("each_month");
			SelectedPayItem.Price = model.OrderByYear;
			SelectedPayItem.AdText2 = adText;
			PayLabel1.AdText2 = adText;
			PayLabel1.Price = model.OrderByYear;
			PayLabel2.Price = model.AlipaySubsMonthly;
			PayLabel3.Price = model.OrderByMonth;
			WechatBtn.Selected = ShowWechatMethod;
			AlipayBtn.Selected = !ShowWechatMethod;
			_freq = "year";
		}
		else
		{
			_freq = "subscribe";
			RefreshTickPayLabels();
		}
		base.DataContext = this;
		TotalPrice = GetTotalPrice();
		GetUnit();
		GetTermContent();
		CountSpinner.OnCountChanged += OnCountChanged;
		UserActCollectUtils.SendUpgradeShowData(_from);
		if (ThemeUtil.IsDark())
		{
			PayImage.Source = ImageUtils.GetResourceImage("pack://application:,,,/Assets/ImageSource/dark/PayDetail.png");
			LeftBrush.GradientStops.Clear();
			LeftBrush.GradientStops.Add(new GradientStop
			{
				Color = (Color)ColorConverter.ConvertFromString("#46423C"),
				Offset = 0.0
			});
			LeftBrush.GradientStops.Add(new GradientStop
			{
				Color = (Color)ColorConverter.ConvertFromString("#2C2C2C"),
				Offset = 1.0
			});
		}
		if (UserDao.IsPro())
		{
			UpdateTitleTextBlock.Text = Utils.GetString("RenewPremiumAccount");
		}
		if (Utils.IsTickPackage() && !RegionInfo.CurrentRegion.TwoLetterISORegionName.Equals("US", StringComparison.OrdinalIgnoreCase))
		{
			AgreeCheckBox.Visibility = Visibility.Collapsed;
		}
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		base.OnClosing(e);
	}

	private void OnCountChanged(object sender, int count)
	{
		Count = count;
		TotalPrice = GetTotalPrice();
	}

	private void CloseButton_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}

	public void OnCancel()
	{
		Close();
	}

	public void Ok()
	{
		Close();
	}

	private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (e.LeftButton == MouseButtonState.Pressed)
		{
			DragMove();
			e.Handled = false;
		}
	}

	private void SwitchAlipay()
	{
		AlipayBtn.Selected = true;
		WechatBtn.Selected = false;
	}

	private void SwitchWechatPay()
	{
		if (ShowWechatMethod)
		{
			AlipayBtn.Selected = false;
			WechatBtn.Selected = true;
		}
	}

	private void OnAlipayClick(object sender, MouseButtonEventArgs e)
	{
		WechatBtn.Selected = false;
		AlipayBtn.Selected = true;
	}

	private void OnWechatPayClick(object sender, MouseButtonEventArgs e)
	{
		WechatBtn.Selected = true;
		AlipayBtn.Selected = false;
	}

	private async void OnPayDetailClick(object sender, MouseButtonEventArgs e)
	{
		if (PayLabel1.Selected)
		{
			if (ShowAlipaySubscribe)
			{
				PayOptionPopup.VerticalOffset = 71.0;
			}
			else
			{
				PayOptionPopup.VerticalOffset = 36.0;
			}
		}
		if (PayLabel2.Selected)
		{
			PayOptionPopup.VerticalOffset = 2.0;
		}
		if (PayLabel3.Selected)
		{
			if (ShowAlipaySubscribe)
			{
				PayOptionPopup.VerticalOffset = -67.0;
			}
			else
			{
				PayOptionPopup.VerticalOffset = -32.0;
			}
		}
		PayOptionPopup.IsOpen = true;
	}

	private void OnPayLabelClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		if (!(sender is PayDetailLabel payDetailLabel))
		{
			return;
		}
		if (payDetailLabel.Selected)
		{
			PayOptionPopup.IsOpen = false;
			return;
		}
		PayLabel1.Selected = false;
		PayLabel2.Selected = false;
		PayLabel3.Selected = false;
		payDetailLabel.Selected = true;
		SelectedPayItem.AdText2 = payDetailLabel.AdText2;
		SelectedPayItem.AdText3 = payDetailLabel.AdText3;
		SelectedPayItem.Currency = payDetailLabel.Currency;
		SelectedPayItem.Unit = payDetailLabel.Unit;
		SelectedPayItem.Price = payDetailLabel.Price;
		SelectedPayItem.ShowUnit = payDetailLabel.ShowUnit;
		ShowCount = !PayLabel2.Selected;
		PayOptionPopup.IsOpen = false;
		if (PayLabel2.Selected)
		{
			SwitchAlipay();
			Freq = "subscribe";
		}
		else if (PayLabel1.Selected)
		{
			SwitchWechatPay();
			Freq = "year";
		}
		else if (PayLabel3.Selected)
		{
			SwitchWechatPay();
			Freq = "month";
		}
	}

	private async Task<bool> CheckPaymentBlackList()
	{
		if (!(await Communicator.CheckPaymentBlackList()))
		{
			return true;
		}
		bool? flag = new CustomerDialog(Utils.GetString("PayBlacklistWarnTitle"), Utils.GetString("PayBlacklistWarnDesc"), Utils.GetString("ContinueUpgrading"), Utils.GetString("Cancel"), this).ShowDialog();
		return flag.HasValue && flag.Value;
	}

	private async void UpdateNowButton_Click(object sender, RoutedEventArgs e)
	{
		if (AgreeCheckBox.Visibility == Visibility.Visible && AgreeCheckBox.IsChecked != true)
		{
			AgreeNotifyPopup.IsOpen = true;
		}
		else
		{
			if (!(await CheckPaymentBlackList()))
			{
				return;
			}
			if (WechatBtn.Selected)
			{
				PerformWechatPayment();
			}
			else if (AlipayBtn.Selected)
			{
				MarkNavigatePayProcess();
				if (PayLabel2.Selected)
				{
					PerformAlipaySubscribe();
				}
				else
				{
					PerformAlipayPayment();
				}
			}
		}
	}

	private async void PerformAlipaySubscribe()
	{
		PayClick = true;
		Close();
		PayType = "alipay_subscribe";
		PayTime = DateTime.Now;
		UserActCollectUtils.SendUpgradePurchaseData(_from, "alipay_monthly");
		Utils.StartUpgradeOnWebV2("alipay_subscribe", _from, "month", "1");
	}

	private async void PerformAlipayPayment()
	{
		PayClick = true;
		Close();
		PayType = "alipay";
		PayTime = DateTime.Now;
		UserActCollectUtils.SendUpgradePurchaseData(_from, (_freq == "year") ? "alipay_1_year" : "alipay_1_month");
		Utils.StartUpgradeOnWebV2("alipay", _from, Freq, CountSpinner.Count.ToString() ?? "");
	}

	private async void PerformWechatPayment()
	{
		string productId = ((_freq == "year") ? "wechat_1_year" : "wechat_1_month");
		UserActCollectUtils.SendUpgradePurchaseData(_from, productId);
		PaymentModelV1 paymentModelV = await Communicator.PaymentV1(new PayModel
		{
			type = "wxpay",
			freq = Freq,
			count = CountSpinner.Count
		});
		if (paymentModelV != null && paymentModelV.redirectUrl != null)
		{
			PayClick = true;
			Close();
			try
			{
				WechatPayQrCodeWindow wechatPayQrCodeWindow = new WechatPayQrCodeWindow(paymentModelV, GetTotalPrice(), _from, productId);
				wechatPayQrCodeWindow.Owner = App.Window;
				wechatPayQrCodeWindow.ShowDialog();
			}
			catch (Exception)
			{
				UtilLog.Error("show wechat pay window error");
			}
		}
	}

	private void GetUnit()
	{
		Unit = Utils.GetString(IsTickSubscribeByMonth() ? "sum_monthly" : "sum_yearly");
	}

	private void GetTermContent()
	{
		string text = Utils.GetString("IHaveAcceptSubscribePro");
		if (Freq == "subscribe")
		{
			string termContent = text.Substring(0, text.IndexOf("{", StringComparison.Ordinal));
			string termContent2 = text.Substring(text.IndexOf("}", StringComparison.Ordinal) + 1);
			TermContent1 = termContent;
			TermContent2 = termContent2;
			TermContent3 = Utils.GetString("pricing_terms");
		}
		else
		{
			text = Utils.GetString("IHaveAcceptUpdatePro");
			string termContent3 = text.Substring(0, text.IndexOf("{", StringComparison.Ordinal));
			string termContent4 = text.Substring(text.IndexOf("}", StringComparison.Ordinal) + 1);
			TermContent1 = termContent3;
			TermContent2 = termContent4;
			TermContent3 = Utils.GetString("pricing_terms");
		}
	}

	private string GetTotalPrice()
	{
		if (IsDida && TryParsePrice(_model.OrderByMonth, out var price) && TryParsePrice(_model.OrderByYear, out var price2))
		{
			decimal price3 = default(decimal);
			TryParsePrice(_model.AlipaySubsMonthly, out price3);
			return Freq switch
			{
				"month" => FormatPrice(price * (decimal)Count), 
				"year" => FormatPrice(price2 * (decimal)Count), 
				"subscribe" => FormatPrice(price3), 
				_ => "", 
			};
		}
		if (TryParsePrice(IsTickSubscribeByMonth() ? GetCurrentTickMonthlyPrice() : GetCurrentTickYearlyPrice(), out var price4))
		{
			return FormatPrice(price4);
		}
		return string.Empty;
	}

	private void RefreshTickPayLabels()
	{
		TickSubByMonthLabel.Price = GetCurrentTickMonthlyPrice();
		string currentTickYearlyPrice = GetCurrentTickYearlyPrice();
		TickSubByYearLabel.Price = currentTickYearlyPrice;
		TickSubByYearLabel.AdText2 = GetTickYearlyMonthlyPriceText(currentTickYearlyPrice);
		SyncTickSelectedPayItem();
		TotalPrice = GetTotalPrice();
	}

	private void SyncTickSelectedPayItem()
	{
		PayDetailLabel payDetailLabel = (TickSubByMonthLabel.Selected ? TickSubByMonthLabel : TickSubByYearLabel);
		TickSelectedPayItem.AdText2 = payDetailLabel.AdText2;
		TickSelectedPayItem.AdText3 = payDetailLabel.AdText3;
		TickSelectedPayItem.Currency = payDetailLabel.Currency;
		TickSelectedPayItem.Unit = payDetailLabel.Unit;
		TickSelectedPayItem.Price = payDetailLabel.Price;
		TickSelectedPayItem.ShowUnit = payDetailLabel.ShowUnit;
	}

	private string GetCurrentTickMonthlyPrice()
	{
		if (!StripeBtn.Selected)
		{
			return FirstNonEmpty(_model.PaypalSubsMonthly, _model.StripeSubsMonthly);
		}
		return FirstNonEmpty(_model.StripeSubsMonthly, _model.PaypalSubsMonthly);
	}

	private string GetCurrentTickYearlyPrice()
	{
		if (!StripeBtn.Selected)
		{
			return FirstNonEmpty(_model.PaypalSubsYearly, _model.StripeSubsYearly);
		}
		return FirstNonEmpty(_model.StripeSubsYearly, _model.PaypalSubsYearly);
	}

	private static string FirstNonEmpty(params string[] values)
	{
		foreach (string text in values)
		{
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text;
			}
		}
		return string.Empty;
	}

	private static string GetTickYearlyMonthlyPriceText(string yearlyPrice)
	{
		if (!TryParsePrice(yearlyPrice, out var price))
		{
			return string.Empty;
		}
		return " $" + FormatPrice(price / 12m) + "/M";
	}

	private static bool TryParsePrice(string priceText, out decimal price)
	{
		price = default(decimal);
		if (string.IsNullOrWhiteSpace(priceText))
		{
			return false;
		}
		string text = priceText.Trim().Replace(" ", string.Empty).Replace("\u00a0", string.Empty);
		if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out price) || decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out price))
		{
			return true;
		}
		text = text.Replace(',', '.');
		return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out price);
	}

	private static string FormatPrice(decimal price)
	{
		return price.ToString("0.##", CultureInfo.CurrentCulture);
	}

	private void ProItemLabel_OnOnClick(object sender, EventArgs e)
	{
		Utils.StartUpgradeOnWeb("upgrade_more_features");
	}

	private void OnTermClick(object sender, MouseButtonEventArgs e)
	{
		Utils.TryProcessStartUrl(Utils.IsDida() ? "https://dida365.com/terms/subscription" : "https://ticktick.com/terms/subscription");
	}

	private void OnPaypalClick(object sender, MouseButtonEventArgs e)
	{
		PaypalBtn.Selected = true;
		StripeBtn.Selected = false;
		RefreshTickPayLabels();
	}

	private void OnStripeClick(object sender, MouseButtonEventArgs e)
	{
		PaypalBtn.Selected = false;
		StripeBtn.Selected = true;
		RefreshTickPayLabels();
	}

	private void OnTickPayDetailClick(object sender, MouseButtonEventArgs e)
	{
		if (TickSubByYearLabel.Selected)
		{
			TickPayOptionPopup.VerticalOffset = 147.0;
		}
		if (TickSubByMonthLabel.Selected)
		{
			TickPayOptionPopup.VerticalOffset = 78.0;
		}
		TickPayOptionPopup.IsOpen = true;
		e.Handled = true;
	}

	private void OnTickPayLabelClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is PayDetailLabel payDetailLabel)
		{
			TickSubByYearLabel.Selected = false;
			TickSubByMonthLabel.Selected = false;
			payDetailLabel.Selected = true;
			SyncTickSelectedPayItem();
			TickPayOptionPopup.IsOpen = false;
			TotalPrice = GetTotalPrice();
			Unit = Utils.GetString(IsTickSubscribeByMonth() ? "sum_monthly" : "sum_yearly");
		}
		e.Handled = true;
	}

	private bool IsTickSubscribeByMonth()
	{
		return string.Equals(GetTickSubscribeFreq(), "month");
	}

	private string GetTickSubscribeFreq()
	{
		if (TickSubByMonthLabel.Selected)
		{
			return "month";
		}
		_ = TickSubByYearLabel.Selected;
		return "year";
	}

	private void MarkNavigatePayProcess()
	{
		NavigatePayProcess = true;
	}

	private async void TickUpdateNowButton_Click(object sender, RoutedEventArgs e)
	{
		if (AgreeCheckBox.Visibility == Visibility.Visible && AgreeCheckBox.IsChecked != true)
		{
			AgreeNotifyPopup.IsOpen = true;
		}
		else if (await CheckPaymentBlackList())
		{
			MarkNavigatePayProcess();
			PayClick = true;
			Close();
			string tickSubscribeFreq = GetTickSubscribeFreq();
			if (PaypalBtn.Selected)
			{
				PayType = "paypal_subscribe";
				PayTime = DateTime.Now;
				UserActCollectUtils.SendUpgradePurchaseData(_from, (tickSubscribeFreq == "year") ? "paypal_yearly" : "paypal_monthly");
				Utils.StartUpgradeOnWebV2("paypal_subscribe", _from, tickSubscribeFreq, "1");
			}
			else
			{
				PayType = "stripe_subscribe";
				PayTime = DateTime.Now;
				UserActCollectUtils.SendUpgradePurchaseData(_from, (tickSubscribeFreq == "year") ? "stripe_yearly" : "stripe_monthly");
				Utils.StartUpgradeOnWebV2("stripe_subscribe", _from, tickSubscribeFreq, "1");
			}
		}
	}

	private void SetPlanO()
	{
		base.Width = 490.0;
		SecondColumn.Width = new GridLength(0.0);
		FirstRow.Height = GridLength.Auto;
		CloseButton.SetValue(Grid.ColumnProperty, 0);
		FeaturePanel.SetValue(Grid.ColumnProperty, 0);
		FeaturePanel.SetValue(Grid.RowProperty, 1);
		FeaturePanel.SetValue(Grid.RowSpanProperty, 1);
		SolidColorBrush color = ThemeUtil.GetColor("ThemeBlue");
		foreach (object child in FeaturePanel.Children)
		{
			if (child is ProItemLabel proItemLabel)
			{
				proItemLabel.IconSize = 18.0;
				proItemLabel.FontSize = 14.0;
				proItemLabel.CheckIcon.Fill = color;
			}
		}
		UpdateTitleTextBlock.FontSize = 32.0;
		TitlePanel.Margin = new Thickness(36.0, 32.0, 36.0, 0.0);
		OptionPanel.Margin = new Thickness(36.0, 0.0, 36.0, 32.0);
		FeaturePanel.Margin = new Thickness(36.0, 16.0, 36.0, 24.0);
		UpdateNowButton.Margin = new Thickness(0.0, 12.0, 0.0, 12.0);
		TickUpdateNowButton.Margin = new Thickness(0.0, 12.0, 0.0, 12.0);
		DidaPayPanel.Margin = new Thickness(0.0, 12.0, 0.0, 0.0);
		UpdateNowButton.Height = 48.0;
		TickUpdateNowButton.Height = 48.0;
		AmountCount.Height = 40.0;
		AliWeChatPay.Height = 40.0;
		TickPay.Height = 40.0;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/upgrade/payprowindow.xaml", UriKind.Relative);
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
			Root = (PayProWindow)target;
			break;
		case 2:
			SecondColumn = (ColumnDefinition)target;
			break;
		case 3:
			FirstRow = (RowDefinition)target;
			break;
		case 4:
			LeftBrush = (LinearGradientBrush)target;
			break;
		case 5:
			PayImage = (Image)target;
			break;
		case 6:
			FeaturePanel = (StackPanel)target;
			break;
		case 7:
			TitlePanel = (StackPanel)target;
			break;
		case 8:
			UpdateTitleTextBlock = (TextBlock)target;
			break;
		case 9:
			UpdateDesc = (TextBlock)target;
			break;
		case 10:
			OptionPanel = (StackPanel)target;
			break;
		case 11:
			PayDetailItem = (Border)target;
			PayDetailItem.MouseLeftButtonUp += OnPayDetailClick;
			break;
		case 12:
			SelectedPayItem = (PayDetailLabel)target;
			break;
		case 13:
			PayOptionPopup = (EscPopup)target;
			break;
		case 14:
			PayLabel1 = (PayDetailLabel)target;
			break;
		case 15:
			PayLabel2 = (PayDetailLabel)target;
			break;
		case 16:
			PayLabel3 = (PayDetailLabel)target;
			break;
		case 17:
			DidaPayPanel = (StackPanel)target;
			break;
		case 18:
			AmountCount = (Grid)target;
			break;
		case 19:
			CountSpinner = (CountSpinner)target;
			break;
		case 20:
			AliWeChatPay = (Grid)target;
			break;
		case 21:
			WechatBtn = (PurchaseWechatBtn)target;
			break;
		case 22:
			AlipayBtn = (PurchaseAlipayBtn)target;
			break;
		case 23:
			TickPayDetailItem = (Border)target;
			TickPayDetailItem.PreviewMouseLeftButtonUp += OnTickPayDetailClick;
			break;
		case 24:
			TickSelectedPayItem = (PayDetailLabel)target;
			break;
		case 25:
			TickPayOptionPopup = (EscPopup)target;
			break;
		case 26:
			TickSubByYearLabel = (PayDetailLabel)target;
			break;
		case 27:
			TickSubByMonthLabel = (PayDetailLabel)target;
			break;
		case 28:
			TickPay = (Grid)target;
			break;
		case 29:
			StripeBtn = (PurchaseStripeBtn)target;
			break;
		case 30:
			PaypalBtn = (PurchasePaypalBtn)target;
			break;
		case 31:
			UpdateNowButton = (Button)target;
			UpdateNowButton.Click += UpdateNowButton_Click;
			break;
		case 32:
			TotalPriceText = (TextBlock)target;
			break;
		case 33:
			TickUpdateNowButton = (Button)target;
			TickUpdateNowButton.Click += TickUpdateNowButton_Click;
			break;
		case 34:
			AgreeCheckBox = (CheckBox)target;
			break;
		case 35:
			((Run)target).MouseLeftButtonUp += OnTermClick;
			break;
		case 36:
			AgreeNotifyPopup = (Popup)target;
			break;
		case 37:
			((Border)target).PreviewMouseDown += Window_MouseLeftButtonDown;
			break;
		case 38:
			CloseButton = (Button)target;
			CloseButton.Click += CloseButton_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
