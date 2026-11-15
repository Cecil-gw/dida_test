namespace ticktick_WPF.Util;

internal class PremiumUtils
{
	public static string GetSubscribeTypeText(string subscribeType)
	{
		return subscribeType switch
		{
			"paypal_subscribe" => Utils.GetString("PayPal"), 
			"google" => Utils.GetString("GooglePlay"), 
			"stripe_subscribe" => Utils.GetString("Stripe"), 
			"alipay_subscribe" => Utils.GetString("Alipay"), 
			"wxpay_subscribe" => Utils.GetString("WeChatPay"), 
			"apple" => Utils.GetString("Apple"), 
			_ => subscribeType, 
		};
	}
}
